using System.ComponentModel.DataAnnotations;

namespace MCPServer.Models;

public class Ticket
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.P3;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    [MaxLength(100)]
    public string? AssignmentGroup { get; set; }
    [MaxLength(100)]
    public string? AssignedTo { get; set; }
    [MaxLength(100)]
    public required string CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAt { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public ICollection<TicketAuditEntry> AuditHistory { get; set; } = [];
}

public class TicketAuditEntry
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    [Required, MaxLength(100)]
    public required string ChangedBy { get; set; }

    [Required, MaxLength(500)]
    public required string ChangeDescription { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Ticket? Ticket { get; set; }
}

// Enums for Priority and Status
public enum TicketPriority
{
    P1 = 1, // Critical - Service down
    P2 = 2, // High - Significant impact, no workaround
    P3 = 3, // Medium - Moderate impact, workaround available
    P4 = 4  // Low - Minor issue, no significant impact
}

public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    Reopened,
    PendingUser
}

// -- MCP Tool response DTOs (returned as Json strings to the agent)
public record TicketSummary(int Id, string Title, string Priority, string Status, string? AssignmentGroup, string AssignedTo, DateTimeOffset CreatedAt);

public record TicketDetail(int Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    string? AssignedTeam,
    string? AssignedTo,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes,
    IList<AuditSummary> History);

public record AuditSummary(
    string ChangedBy,
    string ChangeDescription,
    DateTimeOffset ChangedAt);