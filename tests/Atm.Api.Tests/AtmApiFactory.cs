using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Api.Tests;

/// <summary>Runs the API in memory over a temp-file SQLite database.</summary>
public sealed class AtmApiFactory(Action<IServiceCollection>? configure = null) : WebApplicationFactory<Program>
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"atm-api-{Guid.NewGuid():N}");

    public string DbFile => Path.Combine(_folder, "atm.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Atm", $"Data Source={DbFile};Pooling=False");
        if (configure is not null) builder.ConfigureServices(configure);
    }

    public async Task<(Guid UserId, AccountView Checking, AccountView Savings)> SeedStateAsync(HttpClient client)
    {
        var user = await client.GetFromJsonAsync<UserView>("/api/users/by-name/user-1");
        var accounts = await client.GetFromJsonAsync<List<AccountView>>($"/api/users/{user!.Id}/accounts");
        return (user.Id, accounts!.Single(a => a.Type == "Checking"), accounts!.Single(a => a.Type == "Savings"));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

public record UserView(Guid Id, string Name);

public record AccountView(Guid Id, string Type, decimal Balance);

public record OperationView(Guid TransactionId, string Type, List<AccountView> Balances);

public record TransactionView(
    Guid Id, string Type, string Status, decimal Amount, Guid AccountId, string AccountType,
    Guid? ToAccountId, string? ToAccountType, string? FailureReason, DateTime CreatedAt);

public record ProblemView(int Status, string? Title, string? Detail, Dictionary<string, string[]>? Errors);
