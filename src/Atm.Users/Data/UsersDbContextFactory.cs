using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Atm.Users.Data;

// Design-time only (dotnet ef migrations).
internal class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlite("Data Source=design-time.db", o => o.MigrationsHistoryTable("__EFMigrations_Users"))
            .Options);
}
