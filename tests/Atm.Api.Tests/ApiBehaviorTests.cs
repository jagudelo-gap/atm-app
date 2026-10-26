using System.Net;
using System.Net.Http.Json;
using System.Text;
using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Api.Tests;

public class ApiBehaviorTests
{
    [Fact]
    public async Task Unexpected_error_returns_500_generic_and_rolls_back()
    {
        await using var factory = new AtmApiFactory(ThrowingAccounts.Replace);
        var client = factory.CreateClient();
        var (userId, checking, savings) = await factory.SeedStateAsync(client);

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/transactions/transfer",
            new { fromAccountId = checking.Id, toAccountId = savings.Id, amount = 100m });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal("An unexpected error occurred", problem!.Detail);
        Assert.DoesNotContain("credit boom", await response.Content.ReadAsStringAsync());
        var after = await factory.SeedStateAsync(client);
        Assert.Equal(1500.00m, after.Checking.Balance);
        Assert.Equal(5000.00m, after.Savings.Balance);
        Assert.Empty((await client.GetFromJsonAsync<List<TransactionView>>($"/api/users/{userId}/transactions"))!);
    }

    [Fact]
    public async Task Concurrency_conflict_on_withdraw_returns_409_try_again_and_changes_nothing()
    {
        await using var factory = new AtmApiFactory(s =>
            ThrowingAccounts.Replace(s, new DbUpdateConcurrencyException("stale"), throwOnDebit: true));
        var client = factory.CreateClient();
        var (userId, checking, _) = await factory.SeedStateAsync(client);

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/transactions/withdraw",
            new { accountId = checking.Id, amount = 100m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal("Please try again", problem!.Detail);
        var after = await factory.SeedStateAsync(client);
        Assert.Equal(1500.00m, after.Checking.Balance);
        Assert.Equal(5000.00m, after.Savings.Balance);
        Assert.Empty((await client.GetFromJsonAsync<List<TransactionView>>($"/api/users/{userId}/transactions"))!);
    }

    [Fact]
    public async Task Reset_restores_seed_and_clears_history()
    {
        await using var factory = new AtmApiFactory();
        var client = factory.CreateClient();
        var (userId, checking, savings) = await factory.SeedStateAsync(client);
        await client.PostAsJsonAsync($"/api/users/{userId}/transactions/deposit", new { accountId = checking.Id, amount = 10m });
        await client.PostAsJsonAsync($"/api/users/{userId}/transactions/withdraw", new { accountId = savings.Id, amount = 99999m });

        var response = await client.PostAsJsonAsync("/api/demo/reset", new { userId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await factory.SeedStateAsync(client);
        Assert.Equal(1500.00m, after.Checking.Balance);
        Assert.Equal(5000.00m, after.Savings.Balance);
        Assert.Empty((await client.GetFromJsonAsync<List<TransactionView>>($"/api/users/{userId}/transactions"))!);
    }

    [Fact]
    public async Task Health_returns_200_when_ok()
    {
        await using var factory = new AtmApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        foreach (var check in new[] { "database", "users", "accounts", "transactions" })
            Assert.Contains($"\"{check}\"", json);
    }

    [Fact]
    public async Task Health_returns_503_when_a_module_check_fails()
    {
        await using var factory = new AtmApiFactory();
        var client = factory.CreateClient();
        await using (var connection = new SqliteConnection($"Data Source={factory.DbFile};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var drop = connection.CreateCommand();
            drop.CommandText = "DROP TABLE Transactions";
            await drop.ExecuteNonQueryAsync();
        }

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("\"Unhealthy\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{\"accountId\":\"not-a-guid\",\"amount\":10}")]
    [InlineData("{\"accountId\":")]
    [InlineData("")]
    [InlineData("{\"accountId\":\"00000000-0000-0000-0000-000000000001\",\"amount\":\"ten\"}")]
    [InlineData("{\"accountId\":\"00000000-0000-0000-0000-000000000001\",\"amount\":1e40}")]
    public async Task Malformed_body_returns_400_not_500(string body)
    {
        await using var factory = new AtmApiFactory();
        var client = factory.CreateClient();
        var (userId, _, _) = await factory.SeedStateAsync(client);

        var response = await client.PostAsync($"/api/users/{userId}/transactions/deposit",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.False(string.IsNullOrEmpty(problem!.Detail));
    }

    [Fact]
    public async Task Bad_user_id_in_route_returns_400()
    {
        await using var factory = new AtmApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/not-a-guid/accounts");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_400_bad_request_keeps_problem_shape_with_empty_errors()
    {
        await using var factory = new AtmApiFactory(s =>
            ThrowingAccounts.Replace(s, new BadHttpRequestException("too big", StatusCodes.Status413PayloadTooLarge)));
        var client = factory.CreateClient();
        var (userId, checking, _) = await factory.SeedStateAsync(client);

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/transactions/deposit",
            new { accountId = checking.Id, amount = 10m });

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal(413, problem!.Status);
        Assert.NotNull(problem.Errors);
        Assert.Empty(problem.Errors);
    }
}

/// <summary>IAccountsModule decorator whose Credit throws, to simulate an unexpected failure mid-operation.</summary>
internal sealed class ThrowingAccounts(IAccountsModule inner, Exception failure, bool throwOnDebit = false) : IAccountsModule
{
    public static void Replace(IServiceCollection services) => Replace(services, new InvalidOperationException("credit boom"));

    public static void Replace(IServiceCollection services, Exception failure, bool throwOnDebit = false)
    {
        var original = services.Last(d => d.ServiceType == typeof(IAccountsModule));
        services.Remove(original);
        services.AddScoped<IAccountsModule>(sp => new ThrowingAccounts(
            (IAccountsModule)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!), failure, throwOnDebit));
    }

    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid userId, CancellationToken ct = default) => inner.GetAccountsAsync(userId, ct);
    public Task<Result<AccountDto>> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        throwOnDebit ? throw failure : inner.DebitAsync(accountId, amount, ct);
    public Task<Result<AccountDto>> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        throwOnDebit ? inner.CreditAsync(accountId, amount, ct) : throw failure;
    public Task ResetToSeedAsync(Guid userId, CancellationToken ct = default) => inner.ResetToSeedAsync(userId, ct);
}
