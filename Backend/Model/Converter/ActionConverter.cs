namespace Moba.Backend.Model.Converter;

using Action;

using Enum;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ActionConverter : JsonConverter<Base>
{
    public override Base? ReadJson(JsonReader reader, Type objectType, Base? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        ActionType type = jo["Type"]?.ToObject<ActionType>() ?? ActionType.Unknown;

        // Erstelle einen neuen Serializer ohne diesen Converter, um Endlosschleifen zu vermeiden
        var newSerializer = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            Formatting = serializer.Formatting,
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling
        };

        // Kopiere alle Converters außer diesem
        foreach (var converter in serializer.Converters)
        {
            if (converter.GetType() != typeof(ActionConverter))
            {
                newSerializer.Converters.Add(converter);
            }
        }

        Base? action = type switch
        {
            ActionType.Gong => jo.ToObject<Gong>(newSerializer),
            ActionType.Command => jo.ToObject<Command>(newSerializer),
            ActionType.Announcement => jo.ToObject<Announcement>(newSerializer),
            _ => null
        };

        return action;
    }

    public override void WriteJson(JsonWriter writer, Base? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Erstelle einen neuen Serializer ohne diesen Converter, um Endlosschleifen zu vermeiden
        var newSerializer = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            Formatting = serializer.Formatting,
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling
        };

        // Kopiere alle Converters außer diesem
        foreach (var converter in serializer.Converters)
        {
            if (converter.GetType() != typeof(ActionConverter))
            {
                newSerializer.Converters.Add(converter);
            }
        }

        newSerializer.Serialize(writer, value);
    }
}