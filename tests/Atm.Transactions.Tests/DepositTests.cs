using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

public sealed class DepositTests : IDisposable
{
    private readonly TransactionsFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task Deposit_adds_funds_records_completed_transaction_and_returns_new_balance()
    {
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .DepositAsync(userId, checking.Id, 100.50m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Deposit", result.Value.Type);
        Assert.Equal(1600.50m, result.Value.Balances.Single(a => a.Id == checking.Id).Balance);
        Assert.Equal(5000.00m, result.Value.Balances.Single(a => a.Id == savings.Id).Balance);

        var accounts = await s.ServiceProvider.GetRequiredService<IAccountsModule>().GetAccountsAsync(userId);
        Assert.Equal(1600.50m, accounts.Single(a => a.Id == checking.Id).Balance);

        var rows = await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal(result.Value.TransactionId, row.Id);
        Assert.Equal(checking.Id, row.AccountId);
        Assert.Null(row.ToAccountId);
        Assert.Equal("Deposit", row.Type);
        Assert.Equal(100.50m, row.Amount);
        Assert.Equal("Completed", row.Status);
        Assert.Null(row.FailureReason);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("10.001")]
    [InlineData("1000000.01")]
    public async Task Deposit_rejects_zero_negative_and_three_decimals(string amount)
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .DepositAsync(userId, checking.Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal(ErrorKind.Validation, e.Kind));
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
        var accounts = await s.ServiceProvider.GetRequiredService<IAccountsModule>().GetAccountsAsync(userId);
        Assert.Equal(1500.00m, accounts.Single(a => a.Id == checking.Id).Balance);
    }

    [Fact]
    public async Task Deposit_accepts_the_one_million_cap()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .DepositAsync(userId, checking.Id, 1_000_000.00m);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Deposit_rejects_account_of_another_user()
    {
        var (aliceId, _, _) = await _fx.SeedAsync("alice");
        var (_, bobChecking, _) = await _fx.SeedAsync("bob");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .DepositAsync(aliceId, bobChecking.Id, 10m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Business, Assert.Single(result.Errors).Kind);
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
    }
}
