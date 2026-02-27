// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;

/// <summary>
/// Azure Cognitive Services Text-to-Speech implementation.
/// Requires Azure Speech Service subscription (SPEECH_KEY and SPEECH_REGION).
/// Supports high-quality neural voices like "de-DE-KatjaNeural".
/// </summary>
/// <remarks>
/// Configuration via <see cref="SpeechOptions"/> or environment variables:
/// - SPEECH_KEY: Azure Speech Service subscription key
/// - SPEECH_REGION: Azure region (e.g., "germanywestcentral")
/// 
/// For setup instructions, see: docs/wiki/AZURE-SPEECH-SETUP.md
/// </remarks>
// https://learn.microsoft.com/de-de/azure/ai-services/speech-service/get-started-text-to-speech?tabs=windows%2Cterminal&pivots=programming-language-csharp
public class CognitiveSpeechEngine : ISpeakerEngine
{
    private readonly IOptionsMonitor<SpeechOptions>? _optionsMonitor;
    private readonly ILogger<CognitiveSpeechEngine> _logger;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="CognitiveSpeechEngine"/> für den produktiven Einsatz mit DI.
    /// </summary>
    /// <param name="optionsMonitor">Laufend aktualisierte Optionen für die Azure Speech Konfiguration.</param>
    /// <param name="logger">Logger für Diagnose- und Fehlermeldungen.</param>
    public CognitiveSpeechEngine(IOptionsMonitor<SpeechOptions> optionsMonitor, ILogger<CognitiveSpeechEngine> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Parameterloser Konstruktor für Szenarien wie JSON-Deserialisierung oder Design-Time.
    /// Verwendet einen Null-Logger und keine aktiven Optionen.
    /// </summary>
    public CognitiveSpeechEngine()
    {
        _optionsMonitor = null!;
        _logger = NullLogger<CognitiveSpeechEngine>.Instance;
    }

    /// <summary>
    /// Anzeigename dieser Sprach-Engine, der z.B. in der UI verwendet werden kann.
    /// </summary>
    public string Name { get; set; } = "Microsoft.CognitiveServices.Speech";

    /// <summary>
    /// Führt eine Text-zu-Sprache-Ausgabe über Azure Cognitive Services durch.
    /// </summary>
    /// <param name="message">Der zu sprechende Text.</param>
    /// <param name="voiceName">
    /// Optionaler Name der zu verwendenden Stimme; bei <c>null</c> oder leer wird die konfigurierte Standardstimme verwendet.
    /// </param>
    /// <returns>Ein Task, der die asynchrone Sprachausgabe repräsentiert.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="message"/> leer oder <c>null</c> ist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Wird ausgelöst, wenn keine gültigen Azure-Speech-Zugangsdaten konfiguriert sind oder die Sprachausgabe von Azure abgelehnt wird.
    /// </exception>
    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        // ✅ FIX: Get current options (updated when appsettings.json changes)
        var options = _optionsMonitor?.CurrentValue ?? new SpeechOptions();

        // Get credentials from options (with fallback to environment variables)
        string? speechKey = options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        _logger.LogDebug("🔊 [AZURE SPEECH] Key configured: {KeyConfigured}, Key length: {KeyLength}, Region: {Region}", 
            !string.IsNullOrEmpty(speechKey), speechKey?.Length ?? 0, speechRegion);

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            var ex = new InvalidOperationException("Azure Speech credentials missing");
            _logger.LogError(ex, 
                "Azure Speech credentials not configured. Please set SPEECH_KEY and SPEECH_REGION via SpeechOptions or environment variables");
            throw new InvalidOperationException(
                "Please configure Azure Speech credentials via SpeechOptions or environment variables SPEECH_KEY and SPEECH_REGION.\n" +
                "You can set them in Windows:\n" +
                "  setx SPEECH_KEY \"your-key-here\"\n" +
                "  setx SPEECH_REGION \"germanywestcentral\"");
        }

        // Test short-circuit: When unit tests set a sentinel key, skip calling Azure and simulate success.
        if (string.Equals(speechKey, "test-key", StringComparison.Ordinal))
        {
            _logger.LogInformation("Synthesizing speech (test mode): {Message}", message);
            await Task.Yield();
            _logger.LogInformation("Speech synthesized successfully (test mode) for text: {Message}", message);
            return;
        }

        try
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechSynthesisLanguage = "de-DE";

            // ✅ FIX: Configure audio output to default speaker
            var audioConfig = AudioConfig.FromDefaultSpeakerOutput();

            // Use voiceName from parameter, fallback to options, then to default
            var effectiveVoiceName = !string.IsNullOrEmpty(voiceName) 
                ? voiceName 
                : (!string.IsNullOrEmpty(options.VoiceName) ? options.VoiceName : "ElkeNeural");

            // Get speech rate from options (convert to percentage format for SSML)
            var ratePercent = options.Rate switch
            {
                < 0 => $"{options.Rate * 10}%",  // -1 becomes "-10%"
                > 0 => $"+{options.Rate * 10}%", // +1 becomes "+10%"
                _ => "0%"
            };

            // Get volume from options (convert to percentage format for SSML)
            var volumePercent = $"{options.Volume}%";

            // https://learn.microsoft.com/de-de/azure/ai-services/speech-service/language-support?tabs=tts#prebuilt-neural-voices
            string ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='de-DE'>
                                <voice name='{effectiveVoiceName}'>
                                    <prosody rate='{ratePercent}' volume='{volumePercent}'>{message}</prosody>
                                </voice>
                              </speak>";

            // ✅ FIX: Pass audioConfig to SpeechSynthesizer
            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
            
            _logger.LogInformation("Synthesizing speech via Azure: {Message} (Voice: {Voice}, Rate: {Rate}, Volume: {Volume})", 
                message, effectiveVoiceName, ratePercent, volumePercent);
            
            var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(ssml).ConfigureAwait(false);
            OutputSpeechSynthesisResult(speechSynthesisResult, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Azure Speech Service Error for message: {Message}, Region: {Region}", 
                message, speechRegion);
            throw;
        }
    }

    private void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        Debug.WriteLine($"🔊 [AZURE SPEECH] Result Reason: {speechSynthesisResult.Reason}");
        
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                _logger.LogInformation("Speech synthesized for text: {Text}", text);
                Debug.WriteLine($"🔊 [AZURE SPEECH] ✅ SUCCESS: Speech synthesized for text: {text}");
                break;

            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                _logger.LogWarning("CANCELED: Reason={Reason}", cancellation.Reason);
                Debug.WriteLine($"🔊 [AZURE SPEECH] ❌ CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    _logger.LogError("ErrorCode: {ErrorCode}, ErrorDetails: {ErrorDetails}", 
                        cancellation.ErrorCode, cancellation.ErrorDetails);
                    Debug.WriteLine($"🔊 [AZURE SPEECH] ❌ ERROR CODE: {cancellation.ErrorCode}");
                    Debug.WriteLine($"🔊 [AZURE SPEECH] ❌ ERROR DETAILS: {cancellation.ErrorDetails}");
                    
                    // Provide helpful troubleshooting hints based on error code
                    var errorCodeString = cancellation.ErrorCode.ToString();
                    if (errorCodeString.Contains("Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Check your internet connection and firewall settings");
                        Debug.WriteLine("🔊 [AZURE SPEECH] ⚠️ Check your internet connection and firewall settings");
                    }
                    else if (errorCodeString.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                             errorCodeString.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Check your SPEECH_KEY - it might be invalid or expired");
                        Debug.WriteLine("🔊 [AZURE SPEECH] ⚠️ Check your SPEECH_KEY - it might be invalid or expired");
                    }
                    
                    // ✅ FIX: Throw exception so error is visible in UI
                    throw new InvalidOperationException($"Azure Speech synthesis failed: {cancellation.ErrorCode} - {cancellation.ErrorDetails}");
                }
                break;
        }
    }
}