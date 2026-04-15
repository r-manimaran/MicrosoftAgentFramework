using MdRag.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Infrastructure.Data;

public sealed class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options) 
    {
        
    }

    public DbSet<FileIndexEntry> FileIndex => Set<FileIndexEntry>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatTurn> ChatTurns => Set<ChatTurn>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ------------------
        // FileIndexEntry
        // -----------------

        modelBuilder.Entity<FileIndexEntry>(e =>
        {
            e.HasKey(x => x.FileId);

            e.Property(x => x.FilePath)
              .HasMaxLength(1000)
              .IsRequired();

            e.Property(x => x.ContentHash)
              .HasMaxLength(64)
              .IsRequired();

            e.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

            e.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);


            // Unique constraint
            e.HasIndex(x => x.FilePath).IsUnique();

            // Query Pattern filter by status to find pending/failed files
            e.HasIndex(x => x.Status);

        });

        // -----------------------------------------------------------------------
        // ChatSession / ChatTurn
        // -----------------------------------------------------------------------
        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasKey(x => x.SessionId);
            e.HasMany(x => x.Turns)
             .WithOne(t => t.Session)
             .HasForeignKey(t => t.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatTurn>(e =>
        {
            e.HasKey(x => x.TurnId);
            e.Property(x => x.TurnId).ValueGeneratedOnAdd();

            e.Property(x => x.Role)
             .HasMaxLength(20)
             .IsRequired();

            e.Property(x => x.Content)
             .HasMaxLength(32_000)  // generous cap; trim in repository if needed
             .IsRequired();

            // Ordered retrieval of turns within a session
            e.HasIndex(x => new { x.SessionId, x.CreatedAtUtc });
        });
    }


}
