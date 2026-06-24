// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Security;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Compact pairing payload encoded into the MOBAflow QR code for MOBAsmart.
/// </summary>
public sealed class MobaPairingPayload
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("v")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("h")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("p")]
    public int Port { get; set; }

    [JsonPropertyName("k")]
    public string ApiKey { get; set; } = string.Empty;

    public static MobaPairingPayload Create(string host, int port, string apiKey)
    {
        return new MobaPairingPayload
        {
            Version = CurrentVersion,
            Host = host.Trim(),
            Port = port,
            ApiKey = apiKey.Trim()
        };
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public bool IsValid()
    {
        return Version == CurrentVersion
               && !string.IsNullOrWhiteSpace(Host)
               && Port is > 0 and <= 65535
               && !string.IsNullOrWhiteSpace(ApiKey);
    }

    public static bool TryParse(string? raw, out MobaPairingPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        try
        {
            var parsed = JsonSerializer.Deserialize<MobaPairingPayload>(trimmed, JsonOptions);
            if (parsed == null || !parsed.IsValid())
            {
                return false;
            }

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
