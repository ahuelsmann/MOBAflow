using Microsoft.CognitiveServices.Speech;

namespace Moba.Sound;

// https://learn.microsoft.com/de-de/azure/ai-services/speech-service/get-started-text-to-speech?tabs=windows%2Cterminal&pivots=programming-language-csharp
public class SpeakerEngine : ISpeakerEngine
{
    public SpeakerEngine() { }

    public string Name { get; set; } = "Microsoft.CognitiveServices.Speech";

    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        Environment.SetEnvironmentVariable("SPEECH_KEY", "b29427debf254c88bef939dbab94f162");
        Environment.SetEnvironmentVariable("SPEECH_REGION", "germanywestcentral");

        string? speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        //if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        //{
        //    throw new InvalidOperationException("Please set the environment variables SPEECH_KEY and SPEECH_REGION.");
        //}

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisLanguage = "de-DE";

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

        using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
        var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(ssml);
        OutputSpeechSynthesisResult(speechSynthesisResult, message);
    }

    private void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"Speech synthesized for text: [{text}]");
                break;

            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
        }
    }
}