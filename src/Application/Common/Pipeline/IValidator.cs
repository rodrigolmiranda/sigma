namespace Sigma.Application.Common.Pipeline;

public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public IReadOnlyList<ValidationError> Errors { get; private set; }

    public ValidationResult()
    {
        IsValid = true;
        Errors = new List<ValidationError>();
    }

    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        IsValid = false;
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(params ValidationError[] errors) => new(errors);
    public static ValidationResult Failure(string propertyName, string message)
        => new(new[] { new ValidationError(propertyName, message) });
}

public class ValidationError
{
    public string PropertyName { get; }
    public string Message { get; }

    public ValidationError(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }
}