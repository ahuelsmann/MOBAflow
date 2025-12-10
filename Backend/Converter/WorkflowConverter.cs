// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Converter;

using Domain;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Custom JSON converter for workflow references in stations.
/// Serializes only the workflow GUID instead of the entire object.
/// </summary>
public class WorkflowConverter : JsonConverter<Workflow?>
{
    // Set during deserialization
    public static Project? CurrentProject { get; set; }

    public override void WriteJson(JsonWriter writer, Workflow? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            // Save only the GUID, not the entire object
            writer.WriteValue(value.Id.ToString());
        }
    }

    public override Workflow? ReadJson(JsonReader reader, Type objectType, Workflow? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            string? workflowIdString = reader.Value?.ToString();

            if (string.IsNullOrEmpty(workflowIdString))
            {
                return null;
            }

            // Parse GUID
            if (Guid.TryParse(workflowIdString, out Guid workflowId))
            {
                // NEW STRATEGY: Create temporary workflow object with only the GUID
                // This will be replaced later in RestoreWorkflowReferences with the real reference
                return new Workflow { Id = workflowId };
            }

            // Fallback: Old files still use names
            // Create temporary workflow object with only the name
            return new Workflow { Name = workflowIdString };
        }

        // Fallback: If it's a complete object (old JSON files)
        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            return obj.ToObject<Workflow>(serializer);
        }

        return null;
    }

    public override bool CanRead => true;
}
