namespace Sigma.Application.Contracts;

public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string message) =>
        new("NOT_FOUND", message, ErrorType.NotFound);

    public static Error Validation(string message) =>
        new("VALIDATION_ERROR", message, ErrorType.Validation);

    public static Error Conflict(string message) =>
        new("CONFLICT", message, ErrorType.Conflict);

    public static Error Unauthorized(string message) =>
        new("UNAUTHORIZED", message, ErrorType.Unauthorized);

    public static Error Forbidden(string message) =>
        new("FORBIDDEN", message, ErrorType.Forbidden);

    public static Error Internal(string message) =>
        new("INTERNAL_ERROR", message, ErrorType.Internal);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Internal = 6
}