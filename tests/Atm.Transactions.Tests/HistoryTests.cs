using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Atm.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

public sealed class HistoryTests : IDisposable
{
    private readonly TransactionsFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task History_returns_newest_first_including_failed_and_transfers()
    {
        var (userId, checking, savings) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();
        var tx = s.ServiceProvider.GetRequiredService<ITransactionsModule>();

        // Created back to back so timestamps are within microseconds of each other.
        await tx.DepositAsync(userId, checking.Id, 10m);
        await tx.WithdrawAsync(userId, checking.Id, 999_999m); // fails: insufficient funds
        await tx.TransferAsync(userId, checking.Id, savings.Id, 20m);
        await tx.DepositAsync(userId, savings.Id, 30m);

        var history = await tx.GetHistoryAsync(userId);

        Assert.Equal(4, history.Count);
        Assert.Equal(new[] { "Deposit", "Transfer", "Withdraw", "Deposit" }, history.Select(h => h.Type));
        Assert.Equal(history.OrderByDescending(h => h.CreatedAt).Select(h => h.Id), history.Select(h => h.Id));

        var failed = history[2];
        Assert.Equal("Failed", failed.Status);
        Assert.NotNull(failed.FailureReason);

        var transfer = history[1];
        Assert.Equal("Completed", transfer.Status);
        Assert.Equal(checking.Id, transfer.AccountId);
        Assert.Equal("Checking", transfer.AccountType);
        Assert.Equal(savings.Id, transfer.ToAccountId);
        Assert.Equal("Savings", transfer.ToAccountType);
        Assert.Equal(20m, transfer.Amount);

        var deposit = history[0];
        Assert.Equal("Savings", deposit.AccountType);
        Assert.Null(deposit.ToAccountId);
        Assert.Null(deposit.ToAccountType);
    }

    [Fact]
    public async Task History_order_is_deterministic_for_identical_timestamps_and_sub_second_gaps()
    {
        var (userId, checking, _) = await _fx.SeedAsync("alice");
        using var s = _fx.Root.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var t = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var ids = new List<Guid>();
        // Newest has a fractional second that is "smaller" as text than a whole-second neighbour would be if formatted loosely.
        foreach (var offset in new[] { TimeSpan.Zero, TimeSpan.FromTicks(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1) })
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            db.Transactions.Add(new TransactionRecord
            {
                Id = id, AccountId = checking.Id, Type = "Deposit", Amount = 1m, Status = "Completed", CreatedAt = t + offset,
            });
        }
        // Two rows with exactly the same timestamp.
        var tie = t.AddMinutes(1);
        for (var i = 0; i < 2; i++)
            db.Transactions.Add(new TransactionRecord
            {
                Id = Guid.NewGuid(), AccountId = checking.Id, Type = "Deposit", Amount = 1m, Status = "Completed", CreatedAt = tie,
            });
        await db.SaveChangesAsync();

        var tx = s.ServiceProvider.GetRequiredService<ITransactionsModule>();
        var first = (await tx.GetHistoryAsync(userId)).ToList();
        var second = (await tx.GetHistoryAsync(userId)).ToList();

        Assert.Equal(first.Select(h => h.Id), second.Select(h => h.Id));
        Assert.Equal(first.Select(h => h.CreatedAt).OrderByDescending(x => x), first.Select(h => h.CreatedAt));
        Assert.Equal(new[] { ids[3], ids[2], ids[1], ids[0] }, first.Skip(2).Select(h => h.Id));
    }

    [Fact]
    public async Task History_excludes_other_users_transactions()
    {
        var (aliceId, aliceChecking, _) = await _fx.SeedAsync("alice");
        var (bobId, bobChecking, _) = await _fx.SeedAsync("bob");
        using var s = _fx.Root.CreateScope();
        var tx = s.ServiceProvider.GetRequiredService<ITransactionsModule>();

        await tx.DepositAsync(aliceId, aliceChecking.Id, 10m);
        await tx.DepositAsync(bobId, bobChecking.Id, 20m);

        var alice = await tx.GetHistoryAsync(aliceId);
        var bob = await tx.GetHistoryAsync(bobId);

        Assert.Equal(10m, Assert.Single(alice).Amount);
        Assert.Equal(20m, Assert.Single(bob).Amount);
    }

    [Fact]
    public async Task Clear_removes_all_transactions()
    {
        var (aliceId, aliceChecking, _) = await _fx.SeedAsync("alice");
        var (bobId, bobChecking, _) = await _fx.SeedAsync("bob");
        using var s = _fx.Root.CreateScope();
        var tx = s.ServiceProvider.GetRequiredService<ITransactionsModule>();
        await tx.DepositAsync(aliceId, aliceChecking.Id, 10m);
        await tx.DepositAsync(bobId, bobChecking.Id, 20m);

        await tx.ClearAsync();

        Assert.Empty(await tx.GetHistoryAsync(aliceId));
        Assert.Empty(await tx.GetHistoryAsync(bobId));
        Assert.Empty(await s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Transactions.ToListAsync());
    }
}
