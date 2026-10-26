using System.Net;
using System.Net.Http.Json;

namespace Atm.Api.Tests;

public class TransactionEndpointTests : IAsyncLifetime
{
    private readonly AtmApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Startup_seeds_user_1_with_checking_1500_and_savings_5000()
    {
        var (_, checking, savings) = await _factory.SeedStateAsync(_client);

        Assert.Equal(1500.00m, checking.Balance);
        Assert.Equal(5000.00m, savings.Balance);
    }

    [Fact]
    public async Task Deposit_endpoint_returns_operation_result()
    {
        var (userId, checking, _) = await _factory.SeedStateAsync(_client);

        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/deposit",
            new { accountId = checking.Id, amount = 100.25m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"transactionId\"", json);
        Assert.Contains("\"balances\"", json);
        var result = await response.Content.ReadFromJsonAsync<OperationView>();
        Assert.Equal("Deposit", result!.Type);
        Assert.NotEqual(Guid.Empty, result.TransactionId);
        Assert.Equal(1600.25m, result.Balances.Single(b => b.Id == checking.Id).Balance);
    }

    [Fact]
    public async Task Withdraw_over_balance_returns_422_with_funds_message()
    {
        var (userId, checking, _) = await _factory.SeedStateAsync(_client);

        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/withdraw",
            new { accountId = checking.Id, amount = 1501m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal("Account doesn't have enough funds", problem.Detail);
        Assert.Equal(1500.00m, (await _factory.SeedStateAsync(_client)).Checking.Balance);
    }

    [Theory]
    [InlineData("1.005", "Amount can have at most 2 decimals")]
    [InlineData("0", "Amount must be greater than 0")]
    [InlineData("-5", "Amount must be greater than 0")]
    [InlineData("10000000", "Amount can't be more than 1,000,000.00")]
    public async Task Deposit_with_three_decimals_returns_400_with_field_errors(string amount, string message)
    {
        var (userId, checking, _) = await _factory.SeedStateAsync(_client);
        var body = new StringContent($"{{\"accountId\":\"{checking.Id}\",\"amount\":{amount}}}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/users/{userId}/transactions/deposit", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal(message, problem!.Detail);
        Assert.Contains(message, problem.Errors!["amount"]);
    }

    [Fact]
    public async Task Transfer_same_account_returns_400()
    {
        var (userId, checking, _) = await _factory.SeedStateAsync(_client);

        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/transfer",
            new { fromAccountId = checking.Id, toAccountId = checking.Id, amount = 10m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemView>();
        Assert.Equal("Source and destination accounts must differ", problem!.Detail);
        Assert.Contains("Source and destination accounts must differ", problem.Errors!["toAccountId"]);
    }

    [Fact]
    public async Task Concurrent_withdrawals_of_full_balance_never_go_negative()
    {
        var (userId, checking, _) = await _factory.SeedStateAsync(_client);
        var url = $"/api/users/{userId}/transactions/withdraw";

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync(url, new { accountId = checking.Id, amount = 1500m }),
            _client.PostAsJsonAsync(url, new { accountId = checking.Id, amount = 1500m }));

        var codes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToList();
        Assert.Equal(HttpStatusCode.OK, codes[0]);
        Assert.Contains(codes[1], new[] { HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity });
        Assert.Equal(0m, (await _factory.SeedStateAsync(_client)).Checking.Balance);
    }

    [Fact]
    public async Task History_endpoint_lists_newest_first()
    {
        var (userId, checking, savings) = await _factory.SeedStateAsync(_client);
        await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/deposit", new { accountId = checking.Id, amount = 10m });
        await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/withdraw", new { accountId = checking.Id, amount = 20m });
        await _client.PostAsJsonAsync($"/api/users/{userId}/transactions/transfer",
            new { fromAccountId = checking.Id, toAccountId = savings.Id, amount = 30m });

        var history = await _client.GetFromJsonAsync<List<TransactionView>>($"/api/users/{userId}/transactions");

        Assert.Equal(["Transfer", "Withdraw", "Deposit"], history!.Select(t => t.Type));
        Assert.Equal("Checking", history![0].AccountType);
        Assert.Equal(savings.Id, history[0].ToAccountId);
        Assert.Equal("Savings", history[0].ToAccountType);
        Assert.Equal("Completed", history[0].Status);
    }
}
