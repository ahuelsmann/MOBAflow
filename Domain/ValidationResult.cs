// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents the result of a validation operation.
/// Used to indicate whether an operation (e.g., delete) is allowed and provide error messages if not.
/// </summary>
public class ValidationResult
{
    public ValidationResult()
    {
        Errors = new List<string>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of error messages if validation failed.
    /// </summary>
    public List<string> Errors { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed (for backward compatibility).
    /// </summary>
    public string? ErrorMessage 
    { 
        get => Errors.FirstOrDefault();
        set { if (!string.IsNullOrEmpty(value)) Errors.Add(value); }
    }

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
        Errors = new List<string> { errorMessage }
    };
}
