namespace WorkflowEngine.Models;

public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationResult(bool isValid, IEnumerable<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new(true, Array.Empty<ValidationError>());
}

public class ValidationError
{
    public string Message { get; }
    public ValidationSeverity Severity { get; }

    public ValidationError(string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        Message = message;
        Severity = severity;
    }
}

public enum ValidationSeverity
{
    Warning,
    Error
}