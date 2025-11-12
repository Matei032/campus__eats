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
        
        // ✅ FIX: Properly populate Errors list
        if (errors != null && errors.Count > 0)
        {
            Errors = errors;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            Errors = new List<string> { error };  // ✅ Add single error to list
        }
        else
        {
            Errors = new List<string>();
        }
    }

    // Success
    public static Result<T> Success(T value) => new(true, value, null, null);

    // Single error
    public static Result<T> Failure(string error) => 
        new(false, default, error, new List<string> { error });  // ✅ Pass error to list

    // Multiple errors (for validation)
    public static Result<T> Failure(List<string> errors) => 
        new(false, default, errors.FirstOrDefault(), errors);  // ✅ Set Error + Errors

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
        
        // ✅ FIX: Properly populate Errors list
        if (errors != null && errors.Count > 0)
        {
            Errors = errors;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            Errors = new List<string> { error };  // ✅ Add single error to list
        }
        else
        {
            Errors = new List<string>();
        }
    }

    public static Result Success() => new(true, null, null);
    
    public static Result Failure(string error) => 
        new(false, error, new List<string> { error });  // ✅ Pass error to list
    
    public static Result Failure(List<string> errors) => 
        new(false, errors.FirstOrDefault(), errors);  // ✅ Set Error + Errors

    public static implicit operator bool(Result result) => result.IsSuccess;
}