namespace Moba.Backend.Converter;

using Moba.Backend.Model.Action;
using Moba.Backend.Model.Enum;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ActionConverter : JsonConverter<Base>
{
    public override Base? ReadJson(JsonReader reader, Type objectType, Base? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        ActionType? type = jo["Type"]?.ToObject<ActionType>();

        var newSerializer = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            Formatting = serializer.Formatting,
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling
        };

        foreach (var converter in serializer.Converters)
        {
            if (converter.GetType() != typeof(ActionConverter))
            {
                newSerializer.Converters.Add(converter);
            }
        }

        Base? action = type switch
        {
            ActionType.Sound => jo.ToObject<Audio>(newSerializer),
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

        var newSerializer = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            Formatting = serializer.Formatting,
            NullValueHandling = serializer.NullValueHandling,
            DefaultValueHandling = serializer.DefaultValueHandling
        };

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