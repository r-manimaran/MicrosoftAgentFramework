using AgenticRAGWebApi.Data;
using AgenticRAGWebApi.Data.Entities;
using AgenticRAGWebApi.Models;
using AgenticRAGWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgenticRAGWebApi.Services;

public class TicketRepository(IDbContextFactory<ITSupportDbContext> factory) : ITicketRepository
{
    // --- count open tickets for a user (used by agent reflection) ----------
    public async Task<int> CountOpenByUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Tickets
            .AsNoTracking()
            .CountAsync(t => t.UserId == userId
            && t.Status != "Resolved"
            && t.Status != "Closed", ct);
    }

    // ------ Create 
    public async Task<SupportTicket> CreateAsync(SupportTicket ticket, 
        string priority = "Medium", 
        string? category = null, 
        string? runbookRef = null, 
        CancellationToken ct = default)
    {
        ValidatePriority(priority);

        await using var db = await factory.CreateDbContextAsync(ct);

        var entity = new TicketEntity
        {
            UserId = ticket.UserId,
            Description = ticket.Description,
            Status = "Open",
            Priority = priority,
            Category = category,
            RunbookReference = runbookRef,
            CreatedAt = DateTime.UtcNow,
        };

        db.Tickets.Add(entity);
        await db.SaveChangesAsync(ct);

        return MapToModel(entity);
    }

    // ----- Get by ID -----------
    public async Task<SupportTicket?> GetByIdAsync(int ticketId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var entity = await  db.Tickets.AsNoTracking().FirstOrDefaultAsync(t=>t.TicketId == ticketId , ct);

        return entity is null? null: MapToModel(entity);
    }

    // -- Tickets by status ( for IT dashboard / queue view)----------
    public async Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(string status, int pageSize = 20, CancellationToken ct = default)
    {
        ValidateStatus(status);

        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Tickets
            .AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .Take(pageSize)
            .Select(t => MapToModel(t))
            .ToListAsync(ct);
    }
    
    // -- Recent ticket for a user (used by GetTicketHistoryAsync tool) ----
    public async Task<IReadOnlyList<SupportTicket>> GetRecentAsync(string userId, int count = 5, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 10); // guard agent from large requests

        await using var db =await factory.CreateDbContextAsync(ct);

        return await db.Tickets
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t => MapToModel(t))
            .ToListAsync(ct);
    }

    // -- Recuring issue detection (agent uses this to flag escalation) ----
    // Returns true if the user had > threshold tickets of the same category
    // within the last withinDays days - signals a chronic problem.
    public async Task<bool> HasRecurringIssueAsync(string userId, string category, int withinDays = 30, int threshold = 2, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var since = DateTime.UtcNow.AddDays(-withinDays);

        var count = await db.Tickets
            .AsNoTracking()
            .CountAsync(t =>
                t.UserId == userId &&
                t.Category == category &&
                t.CreatedAt >= since, ct);

        return count >= threshold;
    }

    // -- Full-text keyword search across Description + ResolutionNotes -----
    // used by the agent to find similar post resolutions
    public async Task<IReadOnlyList<SupportTicket>> SearchByKeywordAsync(string keyword, int topK = 10, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        // EF.Functions.Like is translated to SQL LIKE — works on all SQL Server tiers.
        // For production with FTS enabled, swap to EF.Functions.Contains().
        var pattern = $"%{keyword.Replace("[", "[[]").Replace("%", "[%")}%";

        return await db.Tickets
            .AsNoTracking()
            .Where(t =>
                EF.Functions.Like(t.Description, pattern) ||
                EF.Functions.Like(t.ResolutionNotes!, pattern) ||
                EF.Functions.Like(t.Category!, pattern))
            .OrderByDescending(t => t.CreatedAt)
            .Take(topK)
            .Select(t => MapToModel(t))
            .ToListAsync(ct);
    }


    // -- Update status (Open --> InProgress --> Resolved --> Closed -----------
    public async Task<SupportTicket> UpdateStatusAsync(int ticketId, string status, string? resolutionNotes = null, CancellationToken ct = default)
    {
        ValidateStatus(status);

        await using var db = await factory.CreateDbContextAsync(ct);

        var entity = await db.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, ct) ??
            throw new KeyNotFoundException($"Ticket #{ticketId} not found.");

        entity.Status = status;
        entity.ResolutionNotes = resolutionNotes ?? entity.ResolutionNotes;

        if(status is "Resolved" or "Closed")
            entity.ResolvedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapToModel(entity);

    }


    // ---Mapping: entity --> domain model ------
    private static SupportTicket MapToModel(TicketEntity e) =>
        new(e.TicketId, e.UserId, e.Description, e.CreatedAt, e.Status);

    // -- Validation helpers
    private static readonly HashSet<string> ValidStatuses =
        ["Open", "InProgress", "Resolved", "Closed"];

    private static readonly HashSet<string> ValidPriorities =
        ["Low", "Medium", "High", "Critical"];
    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
        {
            throw new ArgumentException(
                $"Invalid status '{status}'. Valid:{string.Join(", ", ValidStatuses)}");
        }
    }

    private static void ValidatePriority(string priority)
    {
        if (!ValidPriorities.Contains(priority))
        {
            throw new ArgumentException($"Invalid priority '{priority}'. Valid:{string.Join(", ", ValidPriorities)}");
        }
    }
}
