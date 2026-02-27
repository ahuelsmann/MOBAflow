// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Validation;

using System.Text.Json;

/// <summary>
/// Service for validating JSON files before deserialization.
/// Prevents loading of incompatible, corrupted, or malformed JSON files.
/// </summary>
public abstract class JsonValidationService
{
    /// <summary>
    /// Validates JSON structure and content before deserialization.
    /// </summary>
    /// <param name="json">Raw JSON string to validate.</param>
    /// <param name="requiredSchemaVersion">Expected schema version (optional).</param>
    /// <returns>ValidationResult with success status and error details.</returns>
    public static JsonValidationResult Validate(string json, int? requiredSchemaVersion = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonValidationResult.Failure("JSON content is empty or whitespace.");
        }

        // Step 1: Parse JSON structure
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return JsonValidationResult.Failure($"Invalid JSON format: {ex.Message}");
        }

        using (document)
        {
            var root = document.RootElement;

            // Step 2: Validate root element is an object
            if (root.ValueKind != JsonValueKind.Object)
            {
                return JsonValidationResult.Failure("JSON root must be an object.");
            }

            // Step 3: Check for required properties (name and projects for Solution)
            if (!root.TryGetProperty("name", out _))
            {
                return JsonValidationResult.Failure("Missing required property: 'name'.");
            }

            if (!root.TryGetProperty("projects", out var projectsElement))
            {
                return JsonValidationResult.Failure("Missing required property: 'projects'.");
            }

            // Step 4: Validate projects is an array
            if (projectsElement.ValueKind != JsonValueKind.Array)
            {
                return JsonValidationResult.Failure("Property 'projects' must be an array.");
            }

            // Step 5: Optional schema version check
            if (requiredSchemaVersion.HasValue)
            {
                if (!root.TryGetProperty("schemaVersion", out var versionElement))
                {
                    return JsonValidationResult.Failure($"Missing schema version. Expected version {requiredSchemaVersion.Value}.");
                }

                if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var actualVersion))
                {
                    return JsonValidationResult.Failure("Schema version must be a number.");
                }

                if (actualVersion != requiredSchemaVersion.Value)
                {
                    return JsonValidationResult.Failure($"Incompatible schema version. Expected {requiredSchemaVersion.Value}, found {actualVersion}.");
                }
            }

            // Step 6: Validate project structure (basic check)
            var projectCount = 0;
            foreach (var project in projectsElement.EnumerateArray())
            {
                projectCount++;
                if (project.ValueKind != JsonValueKind.Object)
                {
                    return JsonValidationResult.Failure($"Project at index {projectCount - 1} is not an object.");
                }

                if (!project.TryGetProperty("name", out _))
                {
                    return JsonValidationResult.Failure($"Project at index {projectCount - 1} is missing 'name' property.");
                }
            }
        }

        return JsonValidationResult.Success();
    }
}

/// <summary>
/// Result of JSON validation.
/// </summary>
public class JsonValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the JSON passed validation.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error message when <see cref="IsValid"/> is false; otherwise null.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private JsonValidationResult() { }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static JsonValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    public static JsonValidationResult Failure(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}
