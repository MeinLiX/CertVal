namespace CertVal.Application.Common.Validation;

public abstract class AbstractValidator<T> : IValidator<T>
{
    protected readonly List<ValidationError> _errors = [];

    public virtual ValidationResult Validate(T instance)
    {
        _errors.Clear();
        ValidateInternal(instance);
        return _errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(_errors);
    }

    public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    protected abstract void ValidateInternal(T instance);

    protected void AddError(string propertyName, string errorMessage, object? attemptedValue = null)
    {
        _errors.Add(new ValidationError(propertyName, errorMessage, attemptedValue));
    }

    protected void ValidateRequired(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            AddError(propertyName, $"{propertyName} is required", value);
    }

    protected void ValidateRequired<TValue>(string propertyName, TValue? value) where TValue : struct
    {
        if (!value.HasValue)
            AddError(propertyName, $"{propertyName} is required", value);
    }

    protected void ValidateEmail(string propertyName, string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern))
            AddError(propertyName, $"{propertyName} must be a valid email address", email);
    }

    protected void ValidateMaxLength(string propertyName, string? value, int maxLength)
    {
        if (value?.Length > maxLength)
            AddError(propertyName, $"{propertyName} must not exceed {maxLength} characters", value);
    }

    protected void ValidateMinLength(string propertyName, string? value, int minLength)
    {
        if (!string.IsNullOrEmpty(value) && value.Length < minLength)
            AddError(propertyName, $"{propertyName} must be at least {minLength} characters", value);
    }

    protected void ValidateRange<TValue>(string propertyName, TValue value, TValue min, TValue max)
        where TValue : IComparable<TValue>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            AddError(propertyName, $"{propertyName} must be between {min} and {max}", value);
    }
}