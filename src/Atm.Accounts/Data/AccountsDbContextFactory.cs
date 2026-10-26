using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Atm.Accounts.Data;

// Design-time only (dotnet ef migrations).
internal class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=design-time.db", o => o.MigrationsHistoryTable("__EFMigrations_Accounts"))
            .Options);
}
