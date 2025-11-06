namespace CampusEats.Backend.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T? value, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    // Success
    public static Result<T> Success(T value) => new(true, value, null);

    // Single error
    public static Result<T> Failure(string error) => new(false, default, error);

    // Multiple errors (for validation)
    public static Result<T> Failure(List<string> errors) => new(false, default, null, errors);

    // Implicit conversion to bool (for easy checking)
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}

// Non-generic version for operations that don't return data
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, null, errors);

    public static implicit operator bool(Result result) => result.IsSuccess;
}