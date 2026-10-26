using System.Text.Json;
using Atm.Common.Cqrs;

namespace Atm.Api.Errors;

/// <summary>Turns module errors into ProblemDetails: {status, title, detail, errors: {field: [messages]}}.</summary>
public static class ProblemDetailsMapper
{
    public const string ConflictMessage = "Please try again";
    public const string UnexpectedMessage = "An unexpected error occurred";

    public static IResult Map(IReadOnlyList<Error> errors)
    {
        // Input problems win over business ones, so a bad amount is reported before "Account not found".
        var kind = new[] { ErrorKind.Unexpected, ErrorKind.Conflict, ErrorKind.Validation, ErrorKind.Business }
            .FirstOrDefault(k => errors.Any(e => e.Kind == k), ErrorKind.Unexpected);

        var (status, title, detail) = kind switch
        {
            ErrorKind.Validation => (400, "Validation failed", First(errors, kind)),
            ErrorKind.Business => (422, "Request could not be completed", First(errors, kind)),
            ErrorKind.Conflict => (409, "Conflict", ConflictMessage),
            _ => (500, "Server error", UnexpectedMessage),
        };

        var fieldErrors = kind is ErrorKind.Unexpected
            ? new Dictionary<string, string[]>()
            : errors.Where(e => e.Field is not null)
                .GroupBy(e => CamelCase(e.Field!))
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).Distinct().ToArray());

        return Results.Problem(detail: detail, statusCode: status, title: title,
            extensions: new Dictionary<string, object?> { ["errors"] = fieldErrors });
    }

    public static IResult Map(ErrorKind kind, string message) => Map([new Error(kind, message)]);

    private static string First(IReadOnlyList<Error> errors, ErrorKind kind) => errors.First(e => e.Kind == kind).Message;

    private static string CamelCase(string field) =>
        string.Join('.', field.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
