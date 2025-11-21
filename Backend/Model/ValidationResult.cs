namespace Moba.Backend.Model;

/// <summary>
/// Represents the result of a validation operation.
/// Used to indicate whether an operation (e.g., delete) is allowed and provide error messages if not.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new ValidationResult { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    public static ValidationResult Failure(string errorMessage) => new ValidationResult 
    { 
        IsValid = false, 
        ErrorMessage = errorMessage 
    };
}
