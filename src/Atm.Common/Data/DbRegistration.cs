using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atm.Common.Data;

public static class DbRegistration
{
    public static IServiceCollection AddSharedSqlite(this IServiceCollection s, string connectionString)
    {
        s.AddScoped(_ =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        });
        s.AddScoped<UnitOfWork>();
        s.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());
        return s;
    }

    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection s, string migrationsHistoryTable) where TContext : DbContext
    {
        s.AddDbContext<TContext>((sp, o) => o.UseSqlite(
            sp.GetRequiredService<SqliteConnection>(),
            sql => sql.MigrationsHistoryTable(migrationsHistoryTable)));

        // Replace the default context factory so each created context joins the unit of work.
        s.Replace(ServiceDescriptor.Scoped(sp =>
        {
            var context = ActivatorUtilities.CreateInstance<TContext>(sp, sp.GetRequiredService<DbContextOptions<TContext>>());
            sp.GetRequiredService<UnitOfWork>().Enlist(context);
            return context;
        }));
        return s;
    }
}
