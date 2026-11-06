// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Strict JSON reading helpers for recording artifacts: required properties and value kinds, stable keys,
/// duplicate and unknown properties. Violations raise <see cref="InvalidRecordingException"/>.
/// </summary>
internal static class RecordingJsonReader
{
    private static readonly TimeSpan StableKeyRegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex StableKeyPattern = new(
        "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        StableKeyRegexTimeout);

    internal static JsonElement RequireProperty(JsonElement element, string propertyName, string parentPath)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw Invalid("missing-property", $"{parentPath}.{propertyName}", $"Required property '{propertyName}' is missing.");
        }

        return value;
    }

    internal static void RequireString(JsonElement element, string propertyName, string parentPath, int maxLength, out string value)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw Invalid("invalid-string", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a string.");
        }

        value = property.GetString()!;
        if (value.Length > maxLength)
        {
            throw Invalid("string-too-long", $"{parentPath}.{propertyName}", $"Property '{propertyName}' exceeds {maxLength} characters.");
        }
    }

    internal static string RequireStableKey(JsonElement element, string propertyName, string parentPath, int maxLength)
    {
        RequireString(element, propertyName, parentPath, maxLength, out var value);
        if (!IsStableKey(value, maxLength))
        {
            throw Invalid("invalid-key", $"{parentPath}.{propertyName}", $"Property '{propertyName}' is not a lowercase stable key.");
        }

        return value;
    }

    internal static bool IsStableKey(string value, int maxLength)
    {
        return value.Length is > 0 && value.Length <= maxLength && StableKeyPattern.IsMatch(value);
    }

    internal static int RequireInt32(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw Invalid("invalid-number", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a 32-bit integer.");
        }

        return value;
    }

    internal static long RequireInt64(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value))
        {
            throw Invalid("invalid-number", $"{parentPath}.{propertyName}", $"Property '{propertyName}' must be a 64-bit integer.");
        }

        return value;
    }

    internal static Guid RequireGuid(JsonElement element, string propertyName, string parentPath)
    {
        var property = RequireProperty(element, propertyName, parentPath);
        return ReadGuid(property, $"{parentPath}.{propertyName}");
    }

    internal static Guid ReadGuid(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.String
            || !Guid.TryParseExact(element.GetString(), "D", out var value)
            || value == Guid.Empty)
        {
            throw Invalid("invalid-id", path, "Identifier must be a non-empty canonical GUID.");
        }

        return value;
    }

    internal static Guid? ReadNullableGuid(JsonElement element, string path)
    {
        return element.ValueKind == JsonValueKind.Null ? null : ReadGuid(element, path);
    }

    internal static DateTimeOffset RequireTimestamp(JsonElement element, string propertyName, string parentPath)
    {
        RequireString(element, propertyName, parentPath, 32, out var value);
        if (!DateTimeOffset.TryParseExact(
                value,
                RecordingArtifactSerializer.TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp)
            || timestamp.Offset != TimeSpan.Zero)
        {
            throw Invalid("invalid-timestamp", $"{parentPath}.{propertyName}", "Timestamp must be canonical UTC ISO-8601.");
        }

        return timestamp;
    }

    internal static void RequireKind(JsonElement element, JsonValueKind kind, string path, string message)
    {
        if (element.ValueKind != kind)
        {
            throw Invalid("invalid-kind", path, message);
        }
    }

    internal static void RejectDuplicateProperties(JsonElement element, string path, bool recursive)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw Invalid("duplicate-property", $"{path}.{property.Name}", $"Duplicate property '{property.Name}' is not allowed.");
                }

                if (recursive)
                {
                    RejectDuplicateProperties(property.Value, $"{path}.{property.Name}", recursive: true);
                }
            }
        }
        else if (recursive && element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item, $"{path}[{index}]", recursive: true);
                index++;
            }
        }
    }

    internal static void RequireOnlyProperties(JsonElement element, string path, FrozenSet<string> allowedProperties)
    {
        var unknownProperty = element
            .EnumerateObject()
            .Select(property => property.Name)
            .FirstOrDefault(propertyName => !allowedProperties.Contains(propertyName));
        if (unknownProperty is null) return;

        throw Invalid(
            "unknown-property",
            $"{path}.{unknownProperty}",
            $"Property '{unknownProperty}' is not part of recording format 1.0.");
    }

    internal static FrozenSet<string> CreatePropertySet(params string[] properties)
    {
        return properties.ToFrozenSet(StringComparer.Ordinal);
    }

    internal static InvalidRecordingException Invalid(string code, string path, string message) => new(code, path, message);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "S3871:Exception types should be public",
        Justification = "This internal exception is a validation control-flow carrier that the serializer always converts to a public result.")]
    internal sealed class InvalidRecordingException(string code, string path, string message) : Exception(message)
    {
        public string Code { get; } = code;

        public string Path { get; } = path;
    }
}
