// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Converter;

using Domain;
using Domain.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JSON converter for WorkflowAction - handles Parameters dictionary serialization.
/// </summary>
public class ActionConverter : JsonConverter<WorkflowAction>
{
    public override WorkflowAction? ReadJson(JsonReader reader, Type objectType, WorkflowAction? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);

        var action = new WorkflowAction
        {
            Id = jo["Id"]?.ToObject<Guid>(serializer) ?? Guid.NewGuid(),
            Name = jo["Name"]?.ToObject<string>(serializer) ?? string.Empty,
            Number = jo["Number"]?.ToObject<uint>(serializer) ?? 0,
            Type = jo["Type"]?.ToObject<ActionType>(serializer) ?? ActionType.Command,
            Parameters = null
        };

        // Read parameters as JToken dictionary first (preserves JSON structure)
        var paramTokens = jo["Parameters"]?.ToObject<Dictionary<string, JToken>>(serializer);
        if (paramTokens != null)
        {
            action.Parameters = paramTokens.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        }

        return action;
    }

    public override void WriteJson(JsonWriter writer, WorkflowAction? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var jo = new JObject
        {
            ["Id"] = JToken.FromObject(value.Id),
            ["Name"] = value.Name,
            ["Number"] = value.Number,
            ["Type"] = JToken.FromObject(value.Type),
            ["Parameters"] = value.Parameters != null ? JToken.FromObject(value.Parameters) : null
        };

        jo.WriteTo(writer);
    }
}
