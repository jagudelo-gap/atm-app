using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Users.Contract;
using Atm.Users.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Users;

public static class UsersModuleRegistration
{
    public static IServiceCollection AddUsersModule(this IServiceCollection s)
    {
        s.AddCqrs(typeof(UsersModuleRegistration).Assembly);
        s.AddModuleDbContext<UsersDbContext>("__EFMigrations_Users");
        s.AddScoped<IUsersModule, UsersModule>();
        return s;
    }

    /// <summary>Applies this module's migrations. Call on a scoped provider.</summary>
    public static Task MigrateUsersModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<UsersDbContext>().Database.MigrateAsync(ct);

    /// <summary>Runs a trivial query on this module's table; throws when it is not reachable.</summary>
    public static Task CheckUsersModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<UsersDbContext>().Users.AnyAsync(ct);
}
