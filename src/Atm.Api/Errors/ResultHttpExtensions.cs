using Atm.Common.Cqrs;

namespace Atm.Api.Errors;

internal static class ResultHttpExtensions
{
    /// <summary>200 with the mapped value, or ProblemDetails for the errors.</summary>
    public static IResult ToHttp<T>(this Result<T> result, Func<T, object> map) =>
        result.IsSuccess ? Results.Ok(map(result.Value)) : ProblemDetailsMapper.Map(result.Errors);
}
