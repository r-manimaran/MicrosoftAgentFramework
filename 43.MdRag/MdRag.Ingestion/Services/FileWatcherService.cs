using MdRag.Ingestion.Models;
using MdRag.Shared.Telemetry;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MdRag.Ingestion.Services;

public sealed class FileWatcherService : IHostedService, IDisposable
{
    private readonly IngestionOptions _options;
    private readonly Channel<IngestionWorkItem> _channel;
    private readonly ILogger<FileWatcherService> _logger;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;

    // path → time of most recent file-system event
    private readonly ConcurrentDictionary<string, DateTime> _pending = new();
    public FileWatcherService(IOptions<IngestionOptions> options,
        Channel<IngestionWorkItem> channel,
        ILogger<FileWatcherService> logger)
    {
        _options = options.Value;
        _channel = channel;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(_options.MdFilesRootPath);
        Directory.CreateDirectory(root);
        _logger.LogInformation("FileWatcherService starting. Watching: {Root}", root);

        // Queue all existing files at startup so a cold deployment indexes everything
        EnqueueExistingFiles(root);

        _watcher = new FileSystemWatcher(root, "*.md")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
        };

        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;

        // Debounce flush every 500ms
        _debounceTimer = new Timer(FlushDebounced, null,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));

        return Task.CompletedTask;

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
        _channel.Writer.TryComplete();
        _logger.LogInformation("FileWatcherService stopped.");
        return Task.CompletedTask;
    }


    // -----------------------------------------------------------------------
    // Event handlers
    // -----------------------------------------------------------------------

    private void OnFileEvent(object _, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return;
        _pending[e.FullPath] = DateTime.UtcNow;
        _logger.LogDebug("File event queued for debounce: {Path}", e.FullPath);
    }

    private void OnRenamed(object _, RenamedEventArgs e)
    {
        // Remove old path from pending if present; queue new path
        _pending.TryRemove(e.OldFullPath, out _);
        if (e.FullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            _pending[e.FullPath] = DateTime.UtcNow;
    }

    private void OnError(object _, ErrorEventArgs e)
        => _logger.LogError(e.GetException(), "FileSystemWatcher error.");

    // -----------------------------------------------------------------------
    // Debounce flush — runs on timer every 500ms
    // -----------------------------------------------------------------------

    private void FlushDebounced(object? _)
    {
        var cutoff = DateTime.UtcNow - _options.FileChangeDebounce;

        foreach (var kvp in _pending)
        {
            if (kvp.Value > cutoff) continue;   // still within debounce window

            if (!_pending.TryRemove(kvp.Key, out _)) continue;

            var relativePath = Path.GetRelativePath(_options.MdFilesRootPath, kvp.Key);
            var item = new IngestionWorkItem { FilePath = relativePath };

            if (_channel.Writer.TryWrite(item))
            {
                using var activity = ActivitySources.Ingestion.StartActivity("FileWatcher.Enqueue");
                activity?.SetTag("file.path", relativePath);
                _logger.LogInformation("Enqueued for ingestion: {Path}", relativePath);
            }
            else
            {
                _logger.LogWarning(
                    "Ingestion channel full — dropping work item for {Path}. " +
                    "Consider increasing channel capacity or worker concurrency.", relativePath);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Startup: enqueue all existing files
    // -----------------------------------------------------------------------

    private void EnqueueExistingFiles(string root)
    {
        var files = Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories);
        int count = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(root, file);
            _channel.Writer.TryWrite(new IngestionWorkItem { FilePath = relativePath });
            count++;
        }

        _logger.LogInformation(
            "Enqueued {Count} existing .md files for startup ingestion.", count);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}
