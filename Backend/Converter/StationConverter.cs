// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Converter;

using Moba.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Custom JSON converter for Station class.
/// Handles the Flow property specially: serializes only the Workflow GUID instead of the entire object.
/// This prevents circular references and reduces JSON file size.
/// </summary>
public class StationConverter : JsonConverter<Station>
{
    public override Station ReadJson(JsonReader reader, Type objectType, Station? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        
        // Handle Flow property specially
        var flowToken = jObject["Flow"];
        Workflow? tempWorkflow = null;
        
        if (flowToken != null && flowToken.Type == JTokenType.String)
        {
            string? workflowIdString = flowToken.Value<string>();
            
            if (!string.IsNullOrEmpty(workflowIdString))
            {
                // Parse GUID
                if (Guid.TryParse(workflowIdString, out Guid workflowId))
                {
                    // Create temporary workflow object with only the GUID
                    // This will be replaced later in RestoreWorkflowReferences
                    tempWorkflow = new Workflow { Id = workflowId };
                }
                else
                {
                    // Fallback: Old files use workflow names
                    tempWorkflow = new Workflow { Name = workflowIdString };
                }
            }
            
            // Remove Flow from JObject so default deserialization doesn't process it
            jObject.Remove("Flow");
        }
        else if (flowToken != null && flowToken.Type == JTokenType.Object)
        {
            // Old JSON files have complete Workflow object - deserialize normally
            tempWorkflow = flowToken.ToObject<Workflow>(serializer);
            jObject.Remove("Flow");
        }
        
        // Handle legacy "Flow" → "WorkflowId" migration
        if (jObject["WorkflowId"] != null && tempWorkflow == null)
        {
            var workflowIdToken = jObject["WorkflowId"];
            if (workflowIdToken.Type == JTokenType.String || workflowIdToken.Type == JTokenType.Guid)
            {
                if (Guid.TryParse(workflowIdToken.Value<string>(), out Guid guid))
                {
                    tempWorkflow = new Workflow { Id = guid };
                }
            }
            jObject.Remove("WorkflowId");
        }
        
        // Deserialize the rest of Station normally
        var station = new Station();
        serializer.Populate(jObject.CreateReader(), station);
        
        // Set the temporary workflow reference
        station.Flow = tempWorkflow;
        station.WorkflowId = tempWorkflow?.Id;
        
        return station;
    }

    public override void WriteJson(JsonWriter writer, Station? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        
        writer.WriteStartObject();
        
        // Serialize all properties manually (except Flow)
        writer.WritePropertyName("Name");
        serializer.Serialize(writer, value.Name);
        
        if (value.Description != null)
        {
            writer.WritePropertyName("Description");
            serializer.Serialize(writer, value.Description);
        }
        
        writer.WritePropertyName("Platforms");
        serializer.Serialize(writer, value.Platforms);
        
        writer.WritePropertyName("InPort");
        serializer.Serialize(writer, value.InPort);
        
        writer.WritePropertyName("NumberOfLapsToStop");
        serializer.Serialize(writer, value.NumberOfLapsToStop);
        
        // Write WorkflowId instead of Flow
        if (value.Flow != null)
        {
            writer.WritePropertyName("WorkflowId");
            serializer.Serialize(writer, value.Flow.Id);
        }
        else if (value.WorkflowId.HasValue)
        {
            writer.WritePropertyName("WorkflowId");
            serializer.Serialize(writer, value.WorkflowId.Value);
        }
        
        // Phase 1 simplified properties
        if (value.Track.HasValue)
        {
            writer.WritePropertyName("Track");
            serializer.Serialize(writer, value.Track.Value);
        }
        
        if (value.Arrival.HasValue)
        {
            writer.WritePropertyName("Arrival");
            serializer.Serialize(writer, value.Arrival.Value);
        }
        
        if (value.Departure.HasValue)
        {
            writer.WritePropertyName("Departure");
            serializer.Serialize(writer, value.Departure.Value);
        }
        
        writer.WritePropertyName("IsExitOnLeft");
        serializer.Serialize(writer, value.IsExitOnLeft);
        
        writer.WriteEndObject();
    }
}
