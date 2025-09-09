using CertVal.Application.Common.Validation;

namespace CertVal.Application.Common.Exceptions;

public class ValidationException : ApplicationException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors)
        : base($"Validation failed: {string.Join(", ", errors.Select(e => e.ErrorMessage))}")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this([new ValidationError(propertyName, errorMessage)])
    {
    }
}
