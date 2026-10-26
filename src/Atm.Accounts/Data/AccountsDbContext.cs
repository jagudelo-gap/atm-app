using Atm.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atm.Accounts.Data;

internal class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Owned by Atm.Users: mapped only so the migration can declare the Account.UserId foreign key.
        b.Entity<UserRef>(e =>
        {
            e.ToTable("Users", t => t.ExcludeFromMigrations());
            e.HasKey(u => u.Id);
        });

        b.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Type).IsRequired().HasMaxLength(20);
            e.Property(a => a.Version).IsConcurrencyToken();
            e.HasIndex(a => new { a.UserId, a.Type }).IsUnique();
            e.HasOne<UserRef>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

internal class UserRef
{
    public Guid Id { get; set; }
}
