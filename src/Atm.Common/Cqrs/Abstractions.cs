namespace Atm.Common.Cqrs;

public interface ICommand<TResponse>;

public interface IQuery<TResponse>;

public interface IHandler<TRequest, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken ct);
}

public interface IDispatcher
{
    Task<Result<T>> SendAsync<T>(ICommand<T> c, CancellationToken ct = default);
    Task<Result<T>> QueryAsync<T>(IQuery<T> q, CancellationToken ct = default);
}
