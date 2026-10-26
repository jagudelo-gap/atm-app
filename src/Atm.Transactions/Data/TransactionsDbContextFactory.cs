using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Atm.Transactions.Data;

// Design-time only (dotnet ef migrations).
internal class TransactionsDbContextFactory : IDesignTimeDbContextFactory<TransactionsDbContext>
{
    public TransactionsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseSqlite("Data Source=design-time.db", o => o.MigrationsHistoryTable("__EFMigrations_Transactions"))
            .Options);
}
