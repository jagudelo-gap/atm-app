using Atm.Accounts.Contract;
using Atm.Api.Errors;
using Atm.Users.Contract;

namespace Atm.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users");

        users.MapGet("/by-name/{name}", async (string name, IUsersModule module, CancellationToken ct) =>
            (await module.GetByNameAsync(name, ct)).ToHttp(u => u));

        // No route constraint on userId: a malformed id is a 400 bad request, not a 404.
        users.MapGet("/{userId}/accounts", async (Guid userId, IAccountsModule module, CancellationToken ct) =>
            Results.Ok((await module.GetAccountsAsync(userId, ct)).Select(AccountResponse.From)));

        return app;
    }
}
