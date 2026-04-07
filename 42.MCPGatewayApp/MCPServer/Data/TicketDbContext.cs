using MCPServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MCPServer.Data;

public class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options): base(options) { }
    
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAuditEntry> AuditHistory => Set<TicketAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Priority).HasConversion<string>();
            e.Property(t => t.Status).HasConversion<string>();
            e.HasMany(t => t.AuditHistory)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed some initial data
            e.HasData(
                new Ticket
                {
                    Id = 1,
                    Title = "VPN gateway unreachable for NYC office",
                    Description = "All NYC staff unable to connect to VPN since 08:30 EST. Affects ~200 users. Remote work blocked.",
                    Priority = TicketPriority.P1,
                    Status = TicketStatus.Open,
                    AssignmentGroup = "Network",
                    CreatedBy = "helpdesk@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new Ticket
                {
                    Id = 2,
                    Title = "Outlook calendar sync broken on iOS",
                    Description = "Calendar events not syncing to iPhone for multiple users after iOS 18 update.",
                    Priority = TicketPriority.P2,
                    Status = TicketStatus.InProgress,
                    AssignmentGroup = "EndUser",
                    AssignedTo = "alice@corp.com",
                    CreatedBy = "bob@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-5),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
                },
                new Ticket
                {
                    Id = 3,
                    Title = "Printer on Floor 3 offline",
                    Description = "HP LaserJet showing offline. Tried restarting — no change.",
                    Priority = TicketPriority.P3,
                    Status = TicketStatus.Open,
                    AssignmentGroup = "Facilities",
                    CreatedBy = "carol@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-8),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-8)
                },
                new Ticket
                {
                    Id = 4,
                    Title = "CI pipeline failing on main branch",
                    Description = "GitHub Actions failing at docker build step since 14:00. All PRs blocked.",
                    Priority = TicketPriority.P1,
                    Status = TicketStatus.InProgress,
                    AssignmentGroup = "DevOps",
                    AssignedTo = "dave@corp.com",
                    CreatedBy = "dave@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                    UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                },
                new Ticket
                {
                    Id = 5,
                    Title = "Request: new MacBook Pro for design hire",
                    Description = "New designer starting Monday. Need 14-inch MBP M4 Pro provisioned by Friday.",
                    Priority = TicketPriority.P3,
                    Status = TicketStatus.PendingUser,
                    AssignmentGroup = "IT Procurement",
                    CreatedBy = "hr@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new Ticket
                {
                    Id = 6,
                    Title = "Database connection pool exhausted on prod",
                    Description = "API returning 503 intermittently. Connection pool hitting max limit under load.",
                    Priority = TicketPriority.P2,
                    Status = TicketStatus.Resolved,
                    AssignmentGroup = "Backend",
                    AssignedTo = "eve@corp.com",
                    CreatedBy = "monitoring@corp.com",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-3),
                    ResolvedAt = DateTimeOffset.UtcNow.AddHours(-3),
                    ResolutionNotes = "Increased pool size from 100 to 250. Added connection timeout of 30s. Root cause: slow query in /api/reports."
                }
            );

        });

        modelBuilder.Entity<TicketAuditEntry>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasData(
                new TicketAuditEntry
                {
                    Id = 1,
                    TicketId = 2,
                    ChangedBy = "alice@corp.com",
                    ChangeDescription = "Status changed from Open to InProgress. Investigating MDM profile.",
                    ChangedAt = DateTimeOffset.UtcNow.AddHours(-1)
                },
                 new TicketAuditEntry
                 {
                     Id = 2,
                     TicketId = 4,
                     ChangedBy = "dave@corp.com",
                     ChangeDescription = "Status changed from Open to InProgress. Identified: base Docker image pull rate-limited.",
                     ChangedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                 },
                new TicketAuditEntry
                {
                    Id = 3,
                    TicketId = 6,
                    ChangedBy = "eve@corp.com",
                    ChangeDescription = "Status changed from InProgress to Resolved. Pool size increased.",
                    ChangedAt = DateTimeOffset.UtcNow.AddHours(-3)
                }
            );
        });
    }
}
