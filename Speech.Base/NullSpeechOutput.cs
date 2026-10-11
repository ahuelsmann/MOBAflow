// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Disabled speech output, used by hosts without synthesis providers.</summary>
public sealed class NullSpeechOutput : ISpeechOutput
{
    /// <inheritdoc />
    public Task<SpeechResult> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SpeechResult.Success([]));
    }
}
