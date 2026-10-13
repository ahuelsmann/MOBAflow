// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Checks the currently selected provider without synthesis or playback.</summary>
public sealed class SpeechHealthCheck(SpeechOutput output)
{
    /// <summary>Gets the provider used by the current selection.</summary>
    public string? ProviderId => output.ResolveProvider()?.Id;

    /// <summary>Checks availability through the same selection used by announcements.</summary>
    public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var provider = output.ResolveProvider();
        return provider is not null &&
            (await provider.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false)).IsAvailable;
    }
}
