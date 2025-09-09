namespace CertVal.Application.Common.Validation;

public class ValidationResult
{
    public bool IsValid { get; }
    public List<ValidationError> Errors { get; }

    public ValidationResult(bool isValid = true, List<ValidationError>? errors = null)
    {
        IsValid = isValid;
        Errors = errors ?? [];
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(params ValidationError[] errors) => new(false, errors.ToList());
    public static ValidationResult Failure(List<ValidationError> errors) => new(false, errors);
}
