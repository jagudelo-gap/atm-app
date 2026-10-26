using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Common.Cqrs;

internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    public Task<Result<T>> SendAsync<T>(ICommand<T> c, CancellationToken ct = default) =>
        Dispatch<T>(c, ct);

    public Task<Result<T>> QueryAsync<T>(IQuery<T> q, CancellationToken ct = default) =>
        Dispatch<T>(q, ct);

    private Task<Result<T>> Dispatch<T>(object request, CancellationToken ct)
    {
        var run = typeof(Dispatcher)
            .GetMethod(nameof(Run), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(request.GetType(), typeof(T));
        return (Task<Result<T>>)run.Invoke(this, [request, ct])!;
    }

    private async Task<Result<TResp>> Run<TReq, TResp>(TReq request, CancellationToken ct)
    {
        var validator = services.GetService<IValidator<TReq>>();
        if (validator is not null)
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Result<TResp>.Fail(validation.Errors.Select(ToError).ToArray());
        }

        var handler = services.GetRequiredService<IHandler<TReq, TResp>>();
        return await handler.HandleAsync(request, ct);
    }

    private static Error ToError(FluentValidation.Results.ValidationFailure f) =>
        new(f.ErrorCode == "business" ? ErrorKind.Business : ErrorKind.Validation, f.ErrorMessage, f.PropertyName);
}
