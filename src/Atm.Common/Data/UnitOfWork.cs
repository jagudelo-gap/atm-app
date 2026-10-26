using System.Data;
using System.Data.Common;
using Atm.Common.Cqrs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Atm.Common.Data;

public interface IUnitOfWork
{
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> work, CancellationToken ct = default);
}

/// <summary>Scoped. Owns one transaction on the shared connection and enlists every module context in it.</summary>
public sealed class UnitOfWork(SqliteConnection connection) : IUnitOfWork
{
    private readonly List<DbContext> _contexts = [];
    private DbTransaction? _tx;

    internal void Enlist(DbContext context)
    {
        _contexts.Add(context);
        if (_tx is not null) context.Database.UseTransaction(_tx);
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> work, CancellationToken ct = default)
    {
        if (_tx is not null) throw new InvalidOperationException("A unit of work is already in progress.");
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);

        try
        {
            // Serializable => BEGIN IMMEDIATE in Microsoft.Data.Sqlite: the write lock is taken up front, so
            // writers serialize. A second writer waits up to the command timeout, then sees the committed data.
            _tx = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            foreach (var c in _contexts) c.Database.UseTransaction(_tx);

            var result = await work();
            if (result.IsSuccess) await _tx.CommitAsync(CancellationToken.None);
            else await RollbackAsync();
            return result;
        }
        catch
        {
            try { await RollbackAsync(); } catch { /* keep the original exception */ }
            throw;
        }
        finally
        {
            await EndAsync();
        }
    }

    private async Task RollbackAsync()
    {
        if (_tx is null) return;
        // Not cancellable: a rollback must always complete.
        await _tx.RollbackAsync(CancellationToken.None);
        foreach (var c in _contexts) c.ChangeTracker.Clear();
    }

    private async Task EndAsync()
    {
        var tx = _tx;
        _tx = null;
        foreach (var c in _contexts) await c.Database.UseTransactionAsync(null);
        if (tx is not null) await tx.DisposeAsync();
    }
}
