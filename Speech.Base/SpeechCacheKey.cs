// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Unambiguous, culture-independent cache identities without request text in file names.</summary>
public static class SpeechCacheKey
{
    /// <summary>Hashes provider identity, captured options and every shared request option.</summary>
    public static string Create(string providerId, string optionsFingerprint, SpeechRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Provider = providerId,
            Options = optionsFingerprint,
            request.VoiceName,
            request.Language,
            request.Rate,
            request.Volume,
            Text = request.Text.Normalize()
        })));
    }

    /// <summary>Hashes a provider's ordered, non-secret synthesis options.</summary>
    public static string Fingerprint(IEnumerable<string> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(options.ToArray())));
    }
}
