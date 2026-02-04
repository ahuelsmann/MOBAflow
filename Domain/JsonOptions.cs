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
    public static readonly JsonSerializerOptions Default;
    public static readonly JsonSerializerOptions Compact;

    static JsonOptions()
    {
        Default = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Compact = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Initializes JSON converters for custom types.
    /// Must be called at application startup before serializing polymorphic types.
    /// </summary>
    public static void InitializeConverters(JsonConverter[] converters)
    {
        foreach (var converter in converters)
        {
            Default.Converters.Add(converter);
            Compact.Converters.Add(converter);
        }
    }
}
