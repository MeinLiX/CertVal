namespace CertVal.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = [];

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    protected Result(bool isSuccess, List<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        Error = string.Join(", ", errors);
    }

    public static Result Success() => new(true, string.Empty);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, errors);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);
    public static Result<T> Failure<T>(List<string> errors) => new(default!, false, errors);
}

public class Result<T> : Result
{
    public T Value { get; protected set; }

    internal Result(T value, bool isSuccess, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    internal Result(T value, bool isSuccess, List<string> errors) : base(isSuccess, errors)
    {
        Value = value;
    }
}
