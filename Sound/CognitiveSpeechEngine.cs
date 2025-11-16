using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Moba.Sound;

// https://learn.microsoft.com/de-de/azure/ai-services/speech-service/get-started-text-to-speech?tabs=windows%2Cterminal&pivots=programming-language-csharp
public class CognitiveSpeechEngine : ISpeakerEngine
{
    public CognitiveSpeechEngine() { }

    public string Name { get; set; } = "Microsoft.CognitiveServices.Speech";

    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        // Get credentials from environment variables (DO NOT hardcode in source!)
        string? speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new InvalidOperationException(
                "Please set the environment variables SPEECH_KEY and SPEECH_REGION before running the application.\n" +
                "You can set them in Windows:\n" +
                "  setx SPEECH_KEY \"your-key-here\"\n" +
                "  setx SPEECH_REGION \"germanywestcentral\"");
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
            
            Console.WriteLine($"🔊 Synthesizing speech via Azure: [{message}]");
            var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(ssml);
            OutputSpeechSynthesisResult(speechSynthesisResult, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Azure Speech Service Error: {ex.Message}");
            Console.WriteLine($"   This might be caused by:");
            Console.WriteLine($"   - Invalid or expired API key");
            Console.WriteLine($"   - Network/Firewall blocking Azure services");
            Console.WriteLine($"   - Incorrect region (should be: {speechRegion})");
            throw;
        }
    }

    private static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"✅ Speech synthesized for text: [{text}]");
                break;

            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"❌ CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"   ErrorCode: {cancellation.ErrorCode}");
                    Console.WriteLine($"   ErrorDetails: {cancellation.ErrorDetails}");
                    
                    // Provide helpful troubleshooting hints based on error code
                    var errorCodeString = cancellation.ErrorCode.ToString();
                    if (errorCodeString.Contains("Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"   💡 Check your internet connection and firewall settings");
                    }
                    else if (errorCodeString.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) || 
                             errorCodeString.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"   💡 Check your SPEECH_KEY - it might be invalid or expired");
                    }
                }
                break;
        }
    }
}