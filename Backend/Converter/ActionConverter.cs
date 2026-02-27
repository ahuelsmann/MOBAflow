// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Converter;

using Domain;
using Domain.Enum;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for <see cref="WorkflowAction"/> that handles serialization and
/// deserialization of the <c>Parameters</c> dictionary while keeping the JSON structure flexible.
/// </summary>
public class ActionJsonConverter : JsonConverter<WorkflowAction>
{
    /// <summary>
    /// Reads a <see cref="WorkflowAction"/> instance from JSON.
    /// </summary>
    /// <param name="reader">The UTF‑8 JSON reader positioned at the value to convert.</param>
    /// <param name="typeToConvert">The type being converted (ignored, always <see cref="WorkflowAction"/>).</param>
    /// <param name="options">Serializer options used for nested values.</param>
    /// <returns>The deserialized <see cref="WorkflowAction"/> instance or <c>null</c> for JSON <c>null</c>.</returns>
    public override WorkflowAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var action = new WorkflowAction
        {
            Id = root.TryGetProperty("Id", out var idElement) 
                ? idElement.GetGuid() 
                : Guid.NewGuid(),
            Name = root.TryGetProperty("Name", out var nameElement) 
                ? nameElement.GetString() ?? string.Empty 
                : string.Empty,
            Number = root.TryGetProperty("Number", out var numberElement) 
                ? numberElement.GetUInt32() 
                : 0,
            Type = root.TryGetProperty("Type", out var typeElement) 
                ? Enum.Parse<ActionType>(typeElement.GetString() ?? "Command", ignoreCase: true) 
                : ActionType.Command,
            Parameters = null
        };

        // Read parameters as dictionary (preserves JSON structure)
        if (root.TryGetProperty("Parameters", out var paramElement) && paramElement.ValueKind != JsonValueKind.Null)
        {
            var parameters = new Dictionary<string, object>();
            foreach (var prop in paramElement.EnumerateObject())
            {
                parameters[prop.Name] = prop.Value.Clone();
            }
            action.Parameters = parameters;
        }

        return action;
    }

    /// <summary>
    /// Writes a <see cref="WorkflowAction"/> instance to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer to write to.</param>
    /// <param name="value">The workflow action value to convert.</param>
    /// <param name="options">Serializer options used for nested values.</param>
    public override void Write(Utf8JsonWriter writer, WorkflowAction? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WriteString("Id", value.Id);
        writer.WriteString("Name", value.Name);
        writer.WriteNumber("Number", value.Number);
        writer.WriteString("Type", value.Type.ToString());

        if (value.Parameters != null)
        {
            writer.WritePropertyName("Parameters");
            JsonSerializer.Serialize(writer, value.Parameters, options);
        }
        else
        {
            writer.WriteNull("Parameters");
        }

        writer.WriteEndObject();
    }
}
