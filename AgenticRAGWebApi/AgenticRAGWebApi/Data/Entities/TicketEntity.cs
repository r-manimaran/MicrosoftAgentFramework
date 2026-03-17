using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticRAGWebApi.Data.Entities;

[Table("SupportTickets")]
public class TicketEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TicketId { get; set; }

    [Required, MaxLength(100)]
    public string UserId { get; set; } = default!;

    [Required, MaxLength(500)]
    public string Description { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Open";  // Open | InProgress | Resolved | Closed

    [Required, MaxLength(20)]
    public string Priority { get; set; } = "Medium"; // Low | Medium | High | Critical

    [MaxLength(200)]
    public string? Category { get; set; }  // VPN | Email | Auth | OneDrive

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    [MaxLength(50)]
    public string? RunbookReference { get; set;  }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set;  }

    // Optimistic concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}
