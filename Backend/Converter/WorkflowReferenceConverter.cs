namespace Moba.Backend.Converter;

using Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Custom JSON Converter für Workflow-Referenzen in Stations.
/// Serialisiert nur die Workflow-GUID statt das gesamte Objekt.
/// </summary>
public class WorkflowReferenceConverter : JsonConverter<Workflow?>
{
    // ❌ PROBLEM: CurrentProject ist während Deserialisierung immer null!
    // ✅ LÖSUNG: Speichere temporäres Workflow-Objekt mit nur der GUID
    public static Project? CurrentProject { get; set; }

    public override void WriteJson(JsonWriter writer, Workflow? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            // ✅ Speichere nur die GUID, nicht das ganze Objekt
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

            // ✅ Parse GUID
            if (Guid.TryParse(workflowIdString, out Guid workflowId))
            {
                // ✅ NEUE STRATEGIE: Erstelle temporäres Workflow-Objekt mit nur der GUID
                // Das wird später in RestoreWorkflowReferences durch die echte Referenz ersetzt
                return new Workflow { Id = workflowId };
            }

            // Fallback: Alte Dateien verwenden noch Namen
            // Erstelle temporäres Workflow-Objekt mit nur dem Namen
            return new Workflow { Name = workflowIdString };
        }

        // Fallback: Wenn es ein vollständiges Objekt ist (alte JSON-Dateien)
        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            return obj.ToObject<Workflow>(serializer);
        }

        return null;
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
}