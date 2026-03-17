using AgenticRAGWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace AgenticRAGWebApi.Data;

public class ITSupportDbContext(DbContextOptions<ITSupportDbContext> options) :DbContext(options)
{
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketEntity>(e =>
        {
            // Indexes for the queries the agent tools actually run
            e.HasIndex(t=>t.UserId);   // GetRecentAsync
            e.HasIndex(t=>t.Status);   // GetByStatusAsync
            e.HasIndex(t => t.CreatedAt);  // Ordering
            e.HasIndex(t => new { t.UserId, t.CreatedAt }); //user history sorted

            // Enum-like string columns: constrain at DB level
            e.Property(t => t.Status).HasDefaultValue("Open");

            e.Property(t => t.Priority).HasDefaultValue("Medium");

            // Auto-update UpdatedAt via SaveChanges override below
            e.Property(t => t.UpdatedAt).ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("GETUTCDATE()");
        });        
         
    }

    // Auto-stamp UpdatedAt on every save
    public override int SaveChanges()
    {
        StampUpdatedAt(); 
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampUpdatedAt();
        return SaveChangesAsync(cancellationToken);
    }

    private void StampUpdatedAt()
    {
        foreach(var entry in ChangeTracker.Entries<TicketEntity>()
            .Where(e=>e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
