namespace Moba.Backend;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Provides a type-safe alternative to throwing exceptions for expected errors.
/// Use this pattern when an operation can fail for business reasons (not technical errors).
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
/// <example>
/// <code>
/// public Result&lt;User&gt; GetUser(int userId)
/// {
///     if (userId &lt;= 0)
///         return Result&lt;User&gt;.Failure("Invalid user ID");
///     
///     var user = _repository.FindById(userId);
///     return user != null
///         ? Result&lt;User&gt;.Success(user)
///         : Result&lt;User&gt;.Failure("User not found");
/// }
/// </code>
/// </example>
public record Result<T>
{
    /// <summary>
    /// The value if the operation succeeded.
    /// Will be null if <see cref="IsSuccess"/> is false.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// The error message if the operation failed.
    /// Will be null if <see cref="IsSuccess"/> is true.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// True when <see cref="Error"/> is null.
    /// </summary>
    public bool IsSuccess => Error == null;

    /// <summary>
    /// Indicates whether the operation failed.
    /// True when <see cref="Error"/> is not null.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A successful Result containing the value</returns>
    public static Result<T> Success(T value) => new() { Value = value };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message describing why the operation failed</param>
    /// <returns>A failed Result containing the error message</returns>
    public static Result<T> Failure(string error) => new() { Error = error };

    /// <summary>
    /// Maps the result value to a new type if successful.
    /// If the result is a failure, propagates the error to the new result type.
    /// </summary>
    /// <typeparam name="TNew">The type to map to</typeparam>
    /// <param name="mapper">The function to transform the value</param>
    /// <returns>A new Result with the mapped value or the same error</returns>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess && Value != null
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(Error ?? "Unknown error");
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute with the success value</param>
    /// <returns>The same Result instance for method chaining</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value != null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute with the error message</param>
    /// <returns>The same Result instance for method chaining</returns>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure && Error != null)
        {
            action(Error);
        }
        return this;
    }
}

/// <summary>
/// Non-generic result for operations that don't return a value.
/// Use this when you only need to indicate success or failure without a result value.
/// </summary>
/// <example>
/// <code>
/// public Result DeleteUser(int userId)
/// {
///     if (userId &lt;= 0)
///         return Result.Failure("Invalid user ID");
///     
///     _repository.Delete(userId);
///     return Result.Success();
/// }
/// </code>
/// </example>
public record Result
{
    /// <summary>
    /// The error message if the operation failed.
    /// Will be null if <see cref="IsSuccess"/> is true.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Error == null;
    
    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result</returns>
    public static Result Success() => new();
    
    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message describing why the operation failed</param>
    /// <returns>A failed Result containing the error message</returns>
    public static Result Failure(string error) => new() { Error = error };
}
