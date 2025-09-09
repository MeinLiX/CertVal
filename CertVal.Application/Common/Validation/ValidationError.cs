namespace CertVal.Application.Common.Validation;

public record ValidationError(string PropertyName, string ErrorMessage, object? AttemptedValue = null);
