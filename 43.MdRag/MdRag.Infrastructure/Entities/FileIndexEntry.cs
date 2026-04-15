using MdRag.Contracts.Ingestion;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Infrastructure.Entities;

public sealed class FileIndexEntry
{
    public Guid FileId { get; set; }
    /// <summary>Relative path from the MD files root folder.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>SHA-256 hash of the file content at last successful ingestion.</summary>
    public string ContentHash { get; set; } = string.Empty;

    public IngestionStage Status { get; set; } = IngestionStage.Queued;
    public int ChunkCount { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public DateTime? IndexedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persists conversation history per session to support multi-turn chat.
/// The Chat Agent loads the relevant session's turns on each request and
/// trims to the configured MaxTurns window to cap context size.
/// </summary>
public sealed class ChatSession
{
    public Guid SessionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChatTurn> Turns { get; set; } = new List<ChatTurn>();
}

/// <summary>
/// A single user/assistant exchange within a ChatSession.
/// </summary>
public sealed class ChatTurn
{
    public int TurnId { get; set; }   // identity, auto-increment
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;  // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ChatSession Session { get; set; } = null!;
}