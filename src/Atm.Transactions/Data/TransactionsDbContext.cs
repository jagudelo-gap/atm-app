using Atm.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atm.Transactions.Data;

internal class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : DbContext(options)
{
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Owned by Atm.Accounts: mapped only so the migration can declare the AccountId/ToAccountId foreign keys.
        b.Entity<AccountRef>(e =>
        {
            e.ToTable("Accounts", t => t.ExcludeFromMigrations());
            e.HasKey(a => a.Id);
        });

        b.Entity<TransactionRecord>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(t => t.Id);
            e.Property(t => t.Type).IsRequired().HasMaxLength(20);
            e.Property(t => t.Status).IsRequired().HasMaxLength(20);
            e.Property(t => t.FailureReason).HasMaxLength(200);
            e.HasIndex(t => t.AccountId);
            e.HasIndex(t => t.ToAccountId);
            e.HasOne<AccountRef>().WithMany().HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AccountRef>().WithMany().HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

internal class AccountRef
{
    public Guid Id { get; set; }
}
