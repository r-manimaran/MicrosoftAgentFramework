using MdRag.Contracts.Ingestion;
using MdRag.Ingestion.Models;
using MdRag.Shared.Telemetry;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace MdRag.Ingestion.Services;
/// <summary>
/// Consumes IngestionWorkItems from the bounded channel and processes each
/// file through the full pipeline (parse → chunk → embed → upsert).
///
/// Concurrency is controlled by a SemaphoreSlim so MaxConcurrentIngestions
/// files can be processed in parallel without overwhelming the embedding API.
///
/// Error handling:
///   - Transient errors (network, throttle) are retried by EmbeddingService.
///   - Non-retryable errors mark the file as Failed in SQL and log the error.
///     The file will not be retried until manually triggered or the file changes.
/// </summary>
public class IngestionWorkerService : BackgroundService
{
    private readonly Channel<IngestionWorkItem> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionOptions _options;
    private readonly ILogger<IngestionWorkerService> _logger;

    public IngestionWorkerService(Channel<IngestionWorkItem> channel,
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> options,
        ILogger<IngestionWorkerService> logger
        )
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
           "IngestionWorkerService started. MaxConcurrent={Max}", _options.MaxConcurrentIngestions);

        var semaphore = new SemaphoreSlim(_options.MaxConcurrentIngestions);
        var tasks = new List<Task>();

        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ProcessItemAsync(item, stoppingToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);

            tasks.Add(task);

            // Clean up completed tasks to avoid unbounded list growth
            tasks.RemoveAll(t => t.IsCompleted);
        }

        // Drain any in-flight tasks before shutdown
        if (tasks.Count > 0)
        {
            _logger.LogInformation("Draining {Count} in-flight ingestion tasks...", tasks.Count);
            await Task.WhenAll(tasks);
        }
    }

    private async Task ProcessItemAsync(IngestionWorkItem item, CancellationToken ct)
    {
        using var activity = ActivitySources.Ingestion
            .StartActivity("IngestionWorker.ProcessFile");
        activity?.SetTag("file.path", item.FilePath);

        using var scope = _scopeFactory.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IngestionPipelineService>();
        var fileIndexRepo = scope.ServiceProvider.GetRequiredService<IFileIndexRepository>();

        _logger.LogInformation("Processing ingestion for: {Path}", item.FilePath);

        try
        {
            await pipeline.RunAsync(item, ct);

            RagMeters.FilesIngested.Add(1,new KeyValuePair<string, object?>("result", "success"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ingestion cancelled for: {Path}", item.FilePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for: {Path}", item.FilePath);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);

            RagMeters.FilesIngested.Add(1,
                new KeyValuePair<string, object?>("result", "failed"));

            // Persist failure in SQL so the admin UI can surface it
            var entry = await fileIndexRepo.GetByPathAsync(item.FilePath, ct);
            if (entry is not null)
            {
                await fileIndexRepo.UpdateStatusAsync(
                    entry.FileId,
                    IngestionStage.Failed,
                    errorMessage: ex.Message,
                    ct: ct);
            }
        }
    }
}
