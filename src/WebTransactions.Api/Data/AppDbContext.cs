using Microsoft.EntityFrameworkCore;
using WebTransactions.Api.Models;

namespace WebTransactions.Api.Data;

/// <summary>
/// EF Core database context for WebTransactions.
/// Manages access to the SQLite database and enforces schema constraints.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Description).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Amount).IsRequired().HasPrecision(18, 2);
            entity.Property(t => t.TransactionDate).IsRequired();
        });
    }
}