// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>A provider-options snapshot and its synthesis operation, captured before cache lookup.</summary>
public sealed class PreparedSpeechSynthesis(string optionsFingerprint, Func<CancellationToken, Task<SpeechResult>> synthesize)
{
    /// <summary>Stable identity of synthesis-affecting options; never includes a credential.</summary>
    public string OptionsFingerprint { get; } = optionsFingerprint;

    /// <summary>Synthesizes with the captured options, independently of later settings changes.</summary>
    public Task<SpeechResult> SynthesizeAsync(CancellationToken cancellationToken = default) => synthesize(cancellationToken);
}
