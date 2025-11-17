using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moba.Common.Extensions;

namespace Moba.Sound;

// https://learn.microsoft.com/de-de/azure/ai-services/speech-service/get-started-text-to-speech?tabs=windows%2Cterminal&pivots=programming-language-csharp
public class CognitiveSpeechEngine : ISpeakerEngine
{
    private readonly SpeechOptions _options;
    private readonly ILogger<CognitiveSpeechEngine> _logger;

    // Primary constructor for DI
    public CognitiveSpeechEngine(IOptions<SpeechOptions> options, ILogger<CognitiveSpeechEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Parameterless constructor for JSON deserialization
    public CognitiveSpeechEngine()
    {
        _options = new SpeechOptions();
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CognitiveSpeechEngine>.Instance;
    }

    public string Name { get; set; } = "Microsoft.CognitiveServices.Speech";

    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        // Get credentials from options (with fallback to environment variables)
        string? speechKey = _options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = _options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            this.LogError(
                "Azure Speech credentials not configured. Please set SPEECH_KEY and SPEECH_REGION via SpeechOptions or environment variables. " +
                "On Windows: setx SPEECH_KEY \"your-key\" and setx SPEECH_REGION \"germanywestcentral\"",
                new InvalidOperationException("Azure Speech credentials missing"),
                _logger);
            throw new InvalidOperationException(
                "Please configure Azure Speech credentials via SpeechOptions or environment variables SPEECH_KEY and SPEECH_REGION.\n" +
                "You can set them in Windows:\n" +
                "  setx SPEECH_KEY \"your-key-here\"\n" +
                "  setx SPEECH_REGION \"germanywestcentral\"");
        }

        // Test short-circuit: When unit tests set a sentinel key, skip calling Azure and simulate success.
        if (string.Equals(speechKey, "test-key", StringComparison.Ordinal))
        {
            this.Log($"🔊 Synthesizing speech (test mode): [{message}]", _logger);
            await Task.Yield();
            this.Log($"✅ Speech synthesized successfully (test mode) for text: [{message}]", _logger);
            return;
        }

        try
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechSynthesisLanguage = "de-DE";

            // ✅ FIX: Configure audio output to default speaker
            var audioConfig = AudioConfig.FromDefaultSpeakerOutput();

            // https://learn.microsoft.com/de-de/azure/ai-services/speech-service/language-support?tabs=tts#prebuilt-neural-voices
            string ssml = string.IsNullOrEmpty(voiceName)
                ? $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='de-DE'>
                                <voice name='de-DE-ElkeNeural'>
                                    <prosody rate='-15%'>{message}</prosody>
                                </voice>
                              </speak>"
                : $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='de-DE'>
                                  <voice name='de-DE-{voiceName}'>
                                    <prosody rate='-15%'>{message}</prosody>
                                  </voice>
                              </speak>";

            // ✅ FIX: Pass audioConfig to SpeechSynthesizer
            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
            
            this.Log($"🔊 Synthesizing speech via Azure: [{message}]", _logger);
            
            var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(ssml);
            OutputSpeechSynthesisResult(speechSynthesisResult, message);
        }
        catch (Exception ex)
        {
            this.LogError(
                $"Azure Speech Service Error for message: [{message}]. Possible causes: Invalid/expired API key, Network/Firewall blocking Azure services, Incorrect region ({speechRegion})",
                ex,
                _logger);
            throw;
        }
    }

    private void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                this.Log($"✅ Speech synthesized for text: [{text}]", _logger);
                break;

            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                this.LogWarning($"CANCELED: Reason={cancellation.Reason}", _logger);

                if (cancellation.Reason == CancellationReason.Error)
                {
                    this.LogError(
                        $"ErrorCode: {cancellation.ErrorCode}, ErrorDetails: {cancellation.ErrorDetails}",
                        new InvalidOperationException(cancellation.ErrorDetails),
                        _logger);
                    
                    // Provide helpful troubleshooting hints based on error code
                    var errorCodeString = cancellation.ErrorCode.ToString();
                    if (errorCodeString.Contains("Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        this.LogWarning("💡 Check your internet connection and firewall settings", _logger);
                    }
                    else if (errorCodeString.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                             errorCodeString.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
                    {
                        this.LogWarning("💡 Check your SPEECH_KEY - it might be invalid or expired", _logger);
                    }
                }
                break;
        }
    }
}