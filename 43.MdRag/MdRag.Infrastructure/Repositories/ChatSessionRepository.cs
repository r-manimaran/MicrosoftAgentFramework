using MdRag.Infrastructure.Data;
using MdRag.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Infrastructure.Repositories;

public interface IChatSessionRepository
{
    Task<IReadOnlyList<ChatTurn>> GetTurnsAsync(Guid sessionId, int maxTurns, CancellationToken ct = default);
    Task AppendTurnAsync(Guid sessionId, string role, string content, CancellationToken ct = default);
    Task ClearSessionAsync(Guid sessionId, CancellationToken ct = default);
}
public sealed class ChatSessionRepository : IChatSessionRepository
{

    private readonly RagDbContext _db;

    public ChatSessionRepository(RagDbContext db) => _db = db;
    /// <summary>
    /// Appends a new turn and updates the session's LastActiveUtc timestamp.
    /// </summary>
    public async Task AppendTurnAsync(Guid sessionId, string role, string content, CancellationToken ct = default)
    {
        _db.ChatTurns.Add(new ChatTurn
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _db.ChatSessions
            .Where(s => s.SessionId == sessionId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.LastActiveUtc, DateTime.UtcNow), ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes all turns for the session (user-initiated reset).</summary>
    public async Task ClearSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _db.ChatTurns
         .Where(t => t.SessionId == sessionId)
         .ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Returns the most recent <paramref name="maxTurns"/> turns for the session,
    /// ordered oldest-first so they can be fed directly into the agent's message list.
    /// Creates the session row if it does not yet exist.
    /// </summary>
    public async Task<IReadOnlyList<ChatTurn>> GetTurnsAsync(Guid sessionId, int maxTurns, CancellationToken ct = default)
    {
        // Ensure session row exists (first message in a new conversation)
        if (!await _db.ChatSessions.AnyAsync(s => s.SessionId == sessionId, ct))
        {
            _db.ChatSessions.Add(new ChatSession { SessionId = sessionId });
            await _db.SaveChangesAsync(ct);
        }

        // Load the last N turns, oldest-first for the agent
        return await _db.ChatTurns
            .AsNoTracking()
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(maxTurns)
            .OrderBy(t => t.CreatedAtUtc)   // re-order oldest-first for the agent
            .ToListAsync(ct);
    }
}
