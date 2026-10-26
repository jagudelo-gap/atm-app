using Atm.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atm.Users.Data;

internal class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Name).IsUnique();
        });
    }
}
