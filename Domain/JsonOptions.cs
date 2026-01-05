// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Centralized JSON serialization options for System.Text.Json.
/// Includes converters for polymorphic types (Workflows, Actions).
/// </summary>
public static class JsonOptions
{
    private static readonly JsonSerializerOptions _default;
    private static readonly JsonSerializerOptions _compact;

    static JsonOptions()
    {
        _default = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _compact = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Default JSON serializer options with all custom converters configured.
    /// </summary>
    public static JsonSerializerOptions Default => _default;

    /// <summary>
    /// Compact JSON serializer options (no indentation).
    /// Useful for API responses and data transmission.
    /// </summary>
    public static JsonSerializerOptions Compact => _compact;

    /// <summary>
    /// Initializes JSON converters for custom types.
    /// Must be called at application startup before serializing polymorphic types.
    /// </summary>
    public static void InitializeConverters(JsonConverter[] converters)
    {
        foreach (var converter in converters)
        {
            _default.Converters.Add(converter);
            _compact.Converters.Add(converter);
        }
    }
}
