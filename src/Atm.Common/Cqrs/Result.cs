namespace Atm.Common.Cqrs;

public enum ErrorKind { Validation, Business, Conflict, Unexpected }

public record Error(ErrorKind Kind, string Message, string? Field = null);

public class Result<T>
{
    private readonly T? _value;

    private Result(T? value, IReadOnlyList<Error> errors)
    {
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read Value of a failed result.");

    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Ok(T value) => new(value, []);

    public static Result<T> Fail(params Error[] errors) => new(default, errors);
}
