using Atm.Accounts.Contract;
using Atm.Accounts.Data;
using Atm.Common.Cqrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Accounts.Tests;

public sealed class AccountsModuleTests : IDisposable
{
    private readonly AccountsFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    private static IAccountsModule Module(IServiceScope s) => s.ServiceProvider.GetRequiredService<IAccountsModule>();

    private async Task<(Guid UserId, AccountDto Checking, AccountDto Savings)> SeededAsync(string name = "alice")
    {
        var userId = await _fx.SeedUserAsync(name);
        using var s = _fx.Root.CreateScope();
        await Module(s).ResetToSeedAsync(userId);
        var accounts = await Module(s).GetAccountsAsync(userId);
        return (userId, accounts.Single(a => a.Type == "Checking"), accounts.Single(a => a.Type == "Savings"));
    }

    [Fact]
    public async Task ResetToSeed_creates_checking_1500_and_savings_5000()
    {
        var userId = await _fx.SeedUserAsync("alice");
        using var s = _fx.Root.CreateScope();
        await Module(s).ResetToSeedAsync(userId);

        var accounts = await Module(s).GetAccountsAsync(userId);

        Assert.Equal(2, accounts.Count);
        Assert.Equal(1500.00m, accounts.Single(a => a.Type == "Checking").Balance);
        Assert.Equal(5000.00m, accounts.Single(a => a.Type == "Savings").Balance);
    }

    [Fact]
    public async Task ResetToSeed_restores_balances_after_changes()
    {
        var (userId, checking, savings) = await SeededAsync();
        using var s = _fx.Root.CreateScope();
        await Module(s).DebitAsync(checking.Id, 100m);
        await Module(s).CreditAsync(savings.Id, 250m);

        await Module(s).ResetToSeedAsync(userId);

        var accounts = await Module(s).GetAccountsAsync(userId);
        Assert.Equal(2, accounts.Count);
        Assert.Equal(1500.00m, accounts.Single(a => a.Type == "Checking").Balance);
        Assert.Equal(5000.00m, accounts.Single(a => a.Type == "Savings").Balance);
        Assert.Equal(checking.Id, accounts.Single(a => a.Type == "Checking").Id);
    }

    [Fact]
    public async Task Debit_reduces_balance()
    {
        var (_, checking, _) = await SeededAsync();
        using var s = _fx.Root.CreateScope();

        var r = await Module(s).DebitAsync(checking.Id, 200.25m);

        Assert.True(r.IsSuccess);
        Assert.Equal(1299.75m, r.Value.Balance);
    }

    [Fact]
    public async Task Debit_of_exact_balance_leaves_zero()
    {
        var (_, checking, _) = await SeededAsync();
        using var s = _fx.Root.CreateScope();

        var r = await Module(s).DebitAsync(checking.Id, 1500m);

        Assert.True(r.IsSuccess);
        Assert.Equal(0m, r.Value.Balance);
    }

    [Fact]
    public async Task Debit_over_balance_returns_funds_error_and_leaves_balance()
    {
        var (userId, checking, _) = await SeededAsync();
        using var s = _fx.Root.CreateScope();

        var r = await Module(s).DebitAsync(checking.Id, 1500.01m);

        Assert.False(r.IsSuccess);
        var e = Assert.Single(r.Errors);
        Assert.Equal(ErrorKind.Business, e.Kind);
        Assert.Equal("Account doesn't have enough funds", e.Message);
        using var s2 = _fx.Root.CreateScope();
        var after = (await Module(s2).GetAccountsAsync(userId)).Single(a => a.Type == "Checking");
        Assert.Equal(1500m, after.Balance);
    }

    [Fact]
    public async Task Credit_increases_balance()
    {
        var (_, _, savings) = await SeededAsync();
        using var s = _fx.Root.CreateScope();

        var r = await Module(s).CreditAsync(savings.Id, 0.50m);

        Assert.True(r.IsSuccess);
        Assert.Equal(5000.50m, r.Value.Balance);
    }

    [Fact]
    public async Task GetAccounts_returns_only_that_users_accounts()
    {
        var (aliceId, _, _) = await SeededAsync("alice");
        var (bobId, _, _) = await SeededAsync("bob");
        using var s = _fx.Root.CreateScope();

        var accounts = await Module(s).GetAccountsAsync(aliceId);

        Assert.Equal(2, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(aliceId, a.UserId));
        Assert.NotEqual(aliceId, bobId);
    }

    [Fact]
    public async Task Two_contexts_debiting_same_account_one_throws_concurrency_exception()
    {
        var (_, checking, _) = await SeededAsync();
        using var s1 = _fx.Root.CreateScope();
        using var s2 = _fx.Root.CreateScope();
        var db1 = s1.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var db2 = s2.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var a1 = await db1.Accounts.SingleAsync(a => a.Id == checking.Id);
        var a2 = await db2.Accounts.SingleAsync(a => a.Id == checking.Id);
        Assert.Equal(a1.Version, a2.Version);

        a1.Debit(100m);
        await db1.SaveChangesAsync();
        a2.Debit(100m);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        using var s3 = _fx.Root.CreateScope();
        var stored = await s3.ServiceProvider.GetRequiredService<AccountsDbContext>().Accounts.AsNoTracking()
            .SingleAsync(a => a.Id == checking.Id);
        Assert.Equal(1400m, stored.Balance);
        Assert.Equal(a1.Version, stored.Version);
    }
}
