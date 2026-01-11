using System;
using ExpensesTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpensesTracker.Models.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasDefaultValue("Expense");
            entity.HasIndex(e => e.Title).IsUnique();
            
            // Add any seed data here if needed, but removed problematic seed
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            entity.Property(e => e.Note)
                .HasMaxLength(500);
            entity.Property(e => e.Date)
                .IsRequired()
                .HasColumnType("date");
            
            // Relationships - SIMPLIFIED VERSION
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Indexes
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.CategoryId);
        });

        // Configure decimal precision for all decimal properties
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set default date for new transactions
        var entries = ChangeTracker.Entries<Transaction>()
            .Where(e => e.State == EntityState.Added && e.Entity.Date == default);

        foreach (var entry in entries)
        {
            entry.Entity.Date = DateTime.Today;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}