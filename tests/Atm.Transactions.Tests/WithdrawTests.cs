using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

public sealed class WithdrawTests : IDisposable
{
    private const string FundsMessage = "Account doesn't have enough funds";
    private TransactionsFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    private static async Task<decimal> BalanceAsync(IServiceScope s, Guid userId, Guid accountId) =>
        (await s.ServiceProvider.GetRequiredService<IAccountsModule>().GetAccountsAsync(userId))
            .Single(a => a.Id == accountId).Balance;

    [Fact]
    public async Task Withdraw_just_above_balance_with_cents_fails_with_funds_message_and_changes_nothing()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();
        var module = s.ServiceProvider.GetRequiredService<ITransactionsModule>();
        Assert.True((await module.DepositAsync(userId, checking.Id, 0.99m)).IsSuccess);
        Assert.Equal(1500.99m, await BalanceAsync(s, userId, checking.Id));

        var result = await module.WithdrawAsync(userId, checking.Id, 1501m);

        var error = Assert.Single(result.Errors);
        Assert.Equal(FundsMessage, error.Message);
        Assert.Equal(1500.99m, await BalanceAsync(s, userId, checking.Id));
        var rows = await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync();
        Assert.Single(rows, r => r.Status == "Failed" && r.Type == "Withdraw");
    }

    [Fact]
    public async Task Withdraw_non_funds_business_error_is_returned_as_is_with_no_row()
    {
        _fx.Dispose();
        _fx = new TransactionsFixture(FaultyAccounts.Replace(failDebit: true, failCredit: false));
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>().WithdrawAsync(userId, checking.Id, 10m);

        var error = Assert.Single(result.Errors);
        Assert.Equal(FaultyAccounts.NotFound, error);
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
    }

    [Fact]
    public async Task Withdraw_reduces_balance_and_records_completed()
    {
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .WithdrawAsync(userId, checking.Id, 200m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Withdraw", result.Value.Type);
        Assert.Equal(1300m, result.Value.Balances.Single(a => a.Id == checking.Id).Balance);
        Assert.Equal(5000m, result.Value.Balances.Single(a => a.Id == savings.Id).Balance);
        Assert.Equal(1300m, await BalanceAsync(s, userId, checking.Id));

        var row = Assert.Single(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync());
        Assert.Equal(result.Value.TransactionId, row.Id);
        Assert.Equal(checking.Id, row.AccountId);
        Assert.Equal("Withdraw", row.Type);
        Assert.Equal(200m, row.Amount);
        Assert.Equal("Completed", row.Status);
        Assert.Null(row.FailureReason);
    }

    [Fact]
    public async Task Withdraw_full_balance_succeeds_leaving_zero()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .WithdrawAsync(userId, checking.Id, 1500m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, await BalanceAsync(s, userId, checking.Id));
    }

    [Fact]
    public async Task Withdraw_more_than_balance_fails_with_funds_message_records_failed_and_leaves_balance()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();
        var module = s.ServiceProvider.GetRequiredService<ITransactionsModule>();

        var result = await module.WithdrawAsync(userId, checking.Id, 1501m);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorKind.Business, error.Kind);
        Assert.Equal(FundsMessage, error.Message);
        Assert.Equal(1500m, await BalanceAsync(s, userId, checking.Id));

        var row = Assert.Single(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync());
        Assert.Equal("Failed", row.Status);
        Assert.Equal("Withdraw", row.Type);
        Assert.Equal(1501m, row.Amount);
        Assert.Equal(FundsMessage, row.FailureReason);

        var cents = await module.WithdrawAsync(userId, checking.Id, 1500.01m);
        Assert.Equal(ErrorKind.Validation, Assert.Single(cents.Errors).Kind);
    }

    [Theory]
    [InlineData("100.50")]
    [InlineData("0")]
    [InlineData("-20")]
    public async Task Withdraw_rejects_cents_zero_and_negative(string amount)
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .WithdrawAsync(userId, checking.Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal(ErrorKind.Validation, e.Kind));
        if (amount == "100.50") Assert.Contains(result.Errors, e => e.Message == "Amount must be a whole dollar amount");
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
        Assert.Equal(1500m, await BalanceAsync(s, userId, checking.Id));
    }

    [Fact]
    public async Task Withdraw_rejects_account_of_another_user()
    {
        var (aliceId, _, _) = await _fx.SeedAsync("alice");
        var (_, bobChecking, _) = await _fx.SeedAsync("bob");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .WithdrawAsync(aliceId, bobChecking.Id, 10m);

        Assert.Equal(ErrorKind.Business, Assert.Single(result.Errors).Kind);
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
    }

    [Fact]
    public async Task Failed_withdraw_row_survives_the_rollback()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using (var s = _fx.Root.CreateScope())
            await s.ServiceProvider.GetRequiredService<ITransactionsModule>().WithdrawAsync(userId, checking.Id, 9999m);

        using var fresh = _fx.Root.CreateScope();
        var row = Assert.Single(await fresh.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync());
        Assert.Equal("Failed", row.Status);
        Assert.Equal(FundsMessage, row.FailureReason);
        Assert.Equal(1500m, await BalanceAsync(fresh, userId, checking.Id));
    }
}
