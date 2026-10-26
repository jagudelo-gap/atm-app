using Atm.Common.Cqrs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Atm.Api.Errors;

/// <summary>
/// Last line of defence. Bad request bodies become 400, concurrency/lock problems become 409
/// "Please try again"; anything else is logged and becomes a generic 500.
/// The unit of work has already rolled back by the time an exception reaches here.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        IResult result;
        if (exception is BadHttpRequestException bad)
        {
            logger.LogInformation("Rejected request {Method} {Path}: {Reason}",
                context.Request.Method, context.Request.Path, bad.Message);
            result = bad.StatusCode == StatusCodes.Status400BadRequest
                ? ProblemDetailsMapper.Map(ErrorKind.Validation, "The request is not valid")
                : Results.Problem(detail: "The request is not valid", statusCode: bad.StatusCode,
                    extensions: new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]>() });
        }
        else if (IsConflict(exception))
        {
            logger.LogWarning(exception, "Conflict on {Method} {Path}", context.Request.Method, context.Request.Path);
            result = ProblemDetailsMapper.Map(ErrorKind.Conflict, ProblemDetailsMapper.ConflictMessage);
        }
        else
        {
            logger.LogError(exception, "Unexpected error on {Method} {Path}", context.Request.Method, context.Request.Path);
            result = ProblemDetailsMapper.Map(ErrorKind.Unexpected, ProblemDetailsMapper.UnexpectedMessage);
        }

        await result.ExecuteAsync(context);
        return true;
    }

    private static bool IsConflict(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is DbUpdateConcurrencyException) return true;
            if (e is SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked }) return true;
        }
        return false;
    }
}
