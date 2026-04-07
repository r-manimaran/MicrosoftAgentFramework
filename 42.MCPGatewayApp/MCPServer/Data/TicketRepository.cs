using MCPServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MCPServer.Data;

public class TicketRepository
{
    private readonly IDbContextFactory<TicketDbContext> _factory;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(IDbContextFactory<TicketDbContext> factory,
        ILogger<TicketRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<TicketSummary>> GetOpenTicketsAsync(string? priority= null,
        string? assignmentGroup =null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Tickets
            .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);

        if (!string.IsNullOrWhiteSpace(priority) &&
            Enum.TryParse<TicketPriority>(priority.ToUpper(), out var p))
        {
            query = query.Where(t => t.Priority == p);
        }

        if (!string.IsNullOrWhiteSpace(assignmentGroup))
        {
            query = query.Where(t =>
                t.AssignmentGroup != null &&
                t.AssignmentGroup.ToLower() == assignmentGroup.ToLower());
        }

        return await query
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new TicketSummary(
                t.Id, t.Title, t.Priority.ToString(),
                t.Status.ToString(), t.AssignmentGroup,
                t.AssignedTo, t.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>Full ticket detail including audit history.</summary>
    public async Task<TicketDetail?> GetTicketByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var ticket = await db.Tickets
            .Include(t => t.AuditHistory.OrderByDescending(a => a.ChangedAt))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return null;

        return new TicketDetail(
            ticket.Id, ticket.Title, ticket.Description,
            ticket.Priority.ToString(), ticket.Status.ToString(),
            ticket.AssignmentGroup, ticket.AssignedTo,
            ticket.CreatedBy, ticket.CreatedAt, ticket.UpdatedAt,
            ticket.ResolvedAt, ticket.ResolutionNotes,
            ticket.AuditHistory.Select(a => new AuditSummary(
                a.ChangedBy, a.ChangeDescription, a.ChangedAt)).ToList());
    }

    /// <summary>Tickets assigned to a specific person.</summary>
    public async Task<List<TicketSummary>> GetTicketsByAssigneeAsync(
        string assignee, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Tickets
            .Where(t => t.AssignedTo != null &&
                        t.AssignedTo.ToLower() == assignee.ToLower() &&
                        t.Status != TicketStatus.Closed)
            .OrderBy(t => t.Priority)
            .Select(t => new TicketSummary(
                t.Id, t.Title, t.Priority.ToString(),
                t.Status.ToString(), t.AssignmentGroup,
                t.AssignedTo, t.CreatedAt))
            .ToListAsync(ct);
    }
    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Update ticket status. Automatically sets ResolvedAt when moving to Resolved.
    /// Writes an audit entry for every status change.
    /// </summary>
    public async Task<TicketSummary?> UpdateTicketStatusAsync(
        int id,
        string newStatus,
        string changedBy,
        string notes,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<TicketStatus>(newStatus, ignoreCase: true, out var status))
            throw new ArgumentException($"Invalid status '{newStatus}'. Valid values: {string.Join(", ", Enum.GetNames<TicketStatus>())}");

        await using var db = await _factory.CreateDbContextAsync(ct);

        var ticket = await db.Tickets.FindAsync([id], ct);
        if (ticket is null) return null;

        var previousStatus = ticket.Status;
        ticket.Status = status;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;

        if (status == TicketStatus.Resolved && ticket.ResolvedAt is null)
        {
            ticket.ResolvedAt = DateTimeOffset.UtcNow;
            ticket.ResolutionNotes = notes;
        }

        db.AuditHistory.Add(new TicketAuditEntry
        {
            TicketId = id,
            ChangedBy = changedBy,
            ChangeDescription = $"Status changed from {previousStatus} to {status}. {notes}".Trim()
        });

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Ticket {Id} status: {From} → {To} by {User}",
            id, previousStatus, status, changedBy);

        return new TicketSummary(
            ticket.Id, ticket.Title, ticket.Priority.ToString(),
            ticket.Status.ToString(), ticket.AssignmentGroup,
            ticket.AssignedTo, ticket.CreatedAt);
    }
}
