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
        
        var jObject = JObject.FromObject(value, serializer);
        
        // Remove Flow property (it's a navigation property - don't serialize the entire object)
        jObject.Remove("Flow");
        
        // Write WorkflowId instead of Flow
        if (value.Flow != null)
        {
            jObject["WorkflowId"] = value.Flow.Id.ToString();
        }
        else if (value.WorkflowId.HasValue)
        {
            jObject["WorkflowId"] = value.WorkflowId.Value.ToString();
        }
        
        jObject.WriteTo(writer);
    }
}
