using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

public sealed class TransferTests : IDisposable
{
    private const string FundsMessage = "Account doesn't have enough funds";
    private TransactionsFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    private static async Task<decimal> BalanceAsync(IServiceScope s, Guid userId, Guid accountId) =>
        (await s.ServiceProvider.GetRequiredService<IAccountsModule>().GetAccountsAsync(userId))
            .Single(a => a.Id == accountId).Balance;

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Transfer_non_funds_business_error_is_returned_as_is_with_no_row_and_no_balance_change(bool failDebit, bool failCredit)
    {
        _fx.Dispose();
        _fx = new TransactionsFixture(FaultyAccounts.Replace(failDebit, failCredit));
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .TransferAsync(userId, checking.Id, savings.Id, 100m);

        Assert.Equal(FaultyAccounts.NotFound, Assert.Single(result.Errors));
        Assert.Empty(await RowsAsync(s));
        using var fresh = _fx.Root.CreateScope();
        Assert.Equal(1500m, await BalanceAsync(fresh, userId, checking.Id));
        Assert.Equal(5000m, await BalanceAsync(fresh, userId, savings.Id));
    }

    private static async Task<List<Atm.Transactions.Domain.TransactionRecord>> RowsAsync(IServiceScope s) =>
        await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.AsNoTracking().ToListAsync();

    [Fact]
    public async Task Transfer_moves_funds_and_writes_one_completed_row_with_both_accounts()
    {
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .TransferAsync(userId, checking.Id, savings.Id, 100m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Transfer", result.Value.Type);
        Assert.Equal(1400m, result.Value.Balances.Single(a => a.Id == checking.Id).Balance);
        Assert.Equal(5100m, result.Value.Balances.Single(a => a.Id == savings.Id).Balance);
        Assert.Equal(1400m, await BalanceAsync(s, userId, checking.Id));
        Assert.Equal(5100m, await BalanceAsync(s, userId, savings.Id));

        var row = Assert.Single(await RowsAsync(s));
        Assert.Equal(result.Value.TransactionId, row.Id);
        Assert.Equal(checking.Id, row.AccountId);
        Assert.Equal(savings.Id, row.ToAccountId);
        Assert.Equal("Transfer", row.Type);
        Assert.Equal(100m, row.Amount);
        Assert.Equal("Completed", row.Status);
    }

    [Fact]
    public async Task Transfer_with_insufficient_funds_changes_nothing_and_records_failed()
    {
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .TransferAsync(userId, checking.Id, savings.Id, 1500.01m);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorKind.Business, error.Kind);
        Assert.Equal(FundsMessage, error.Message);
        Assert.Equal(1500m, await BalanceAsync(s, userId, checking.Id));
        Assert.Equal(5000m, await BalanceAsync(s, userId, savings.Id));

        var row = Assert.Single(await RowsAsync(s));
        Assert.Equal("Failed", row.Status);
        Assert.Equal("Transfer", row.Type);
        Assert.Equal(checking.Id, row.AccountId);
        Assert.Equal(savings.Id, row.ToAccountId);
        Assert.Equal(FundsMessage, row.FailureReason);
    }

    [Fact]
    public async Task Transfer_to_same_account_is_a_validation_error()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        var result = await s.ServiceProvider.GetRequiredService<ITransactionsModule>()
            .TransferAsync(userId, checking.Id, checking.Id, 10m);

        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal("Source and destination accounts must differ", error.Message);
        Assert.Empty(await RowsAsync(s));
        Assert.Equal(1500m, await BalanceAsync(s, userId, checking.Id));
    }

    [Fact]
    public async Task Transfer_rejects_invalid_amount_and_foreign_accounts()
    {
        var (aliceId, aliceChecking, aliceSavings) = await _fx.SeedAsync("alice");
        var (_, bobChecking, _) = await _fx.SeedAsync("bob");
        using var s = _fx.Root.CreateScope();
        var module = s.ServiceProvider.GetRequiredService<ITransactionsModule>();

        var zero = await module.TransferAsync(aliceId, aliceChecking.Id, aliceSavings.Id, 0m);
        Assert.False(zero.IsSuccess);
        Assert.All(zero.Errors, e => Assert.Equal(ErrorKind.Validation, e.Kind));

        var foreignTo = await module.TransferAsync(aliceId, aliceChecking.Id, bobChecking.Id, 10m);
        Assert.Equal(ErrorKind.Business, Assert.Single(foreignTo.Errors).Kind);
        var foreignFrom = await module.TransferAsync(aliceId, bobChecking.Id, aliceSavings.Id, 10m);
        Assert.Equal(ErrorKind.Business, Assert.Single(foreignFrom.Errors).Kind);

        Assert.Empty(await RowsAsync(s));
        Assert.Equal(1500m, await BalanceAsync(s, aliceId, aliceChecking.Id));
    }

    [Fact]
    public async Task Transfer_rolls_back_debit_when_credit_throws()
    {
        _fx.Dispose();
        _fx = new TransactionsFixture(services =>
        {
            var original = services.Last(d => d.ServiceType == typeof(IAccountsModule));
            services.Remove(original);
            services.AddScoped<IAccountsModule>(sp => new ThrowingCreditAccounts(
                (IAccountsModule)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!)));
        });
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.ServiceProvider.GetRequiredService<ITransactionsModule>().TransferAsync(userId, checking.Id, savings.Id, 100m));

        using var fresh = _fx.Root.CreateScope();
        Assert.Equal(1500m, await BalanceAsync(fresh, userId, checking.Id));
        Assert.Equal(5000m, await BalanceAsync(fresh, userId, savings.Id));
        Assert.DoesNotContain(await RowsAsync(fresh), r => r.Status == "Completed");
    }

    private sealed class ThrowingCreditAccounts(IAccountsModule inner) : IAccountsModule
    {
        public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid userId, CancellationToken ct = default) => inner.GetAccountsAsync(userId, ct);
        public Task<Result<AccountDto>> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default) => inner.DebitAsync(accountId, amount, ct);
        public Task<Result<AccountDto>> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default) => throw new InvalidOperationException("credit failed");
        public Task ResetToSeedAsync(Guid userId, CancellationToken ct = default) => inner.ResetToSeedAsync(userId, ct);
    }
}
