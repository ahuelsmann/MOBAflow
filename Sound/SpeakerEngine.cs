using Microsoft.CognitiveServices.Speech;

using Moba.Common.Interface;

namespace Moba.Sound;

// https://learn.microsoft.com/de-de/azure/ai-services/speech-service/get-started-text-to-speech?tabs=windows%2Cterminal&pivots=programming-language-csharp    public class Speaker2 : ISpeaker
public class SpeakerEngine : ISpeakerEngine
{
    // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
    // setx SPEECH_KEY your-key
    // setx SPEECH_REGION your-region
    // and
    // set SPEECH_KEY your-key
    // set SPEECH_REGION your-region
    private static readonly string? SpeechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
    private static readonly string? SpeechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

    public string Name { get; set; } = "Microsoft.CognitiveServices.Speech";

    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrEmpty(SpeechKey) || string.IsNullOrEmpty(SpeechRegion))
        {
            throw new InvalidOperationException("Please set the environment variables SPEECH_KEY and SPEECH_REGION.");
        }

        var speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
        speechConfig.SpeechSynthesisLanguage = "de-DE";

        //KatjaNeural       (weiblich) 2. Platz weiblich
        //ConradNeural      (männlich) 1. Platz männlich
        //AmalaNeural       (weiblich)
        //BerndNeural       (männlich)
        //ChristophNeural   (männlich)
        //ElkeNeural        (weiblich)  1. Platz weiblich
        //FlorianMultilingualNeural (Männlich)
        //GiselaNeural      (Weiblich, Kind) <- Witzig
        //KasperNeural      (männlich)
        //KillianNeural     (männlich)
        //KlarissaNeural    (weiblich)  5. Platz
        //KlausNeural       (männlich)
        //LouisaNeural      (weiblich)  4. Platz
        //MajaNeural        (weiblich)  3. Place
        //RalfNeural        (männlich)
        //TanjaNeural       (weiblich)  Stimme könnte passend für Ansange am Bahnsteig sein.

        string ssml = string.IsNullOrEmpty(voiceName)
            ? $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='de-DE'>
                            <voice name='de-DE-ElkeNeuralNeural'>
                                <prosody rate='-15%'>{message}</prosody>
                            </voice>
                          </speak>"
            : $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='de-DE'>
                              <voice name='de-DE-{voiceName}Neural'>
                                <prosody rate='-15%'>{message}</prosody>
                              </voice>
                          </speak>";
        // https://learn.microsoft.com/de-de/azure/ai-services/speech-service/language-support?tabs=tts#prebuilt-neural-voices

        using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
        var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(ssml);
        OutputSpeechSynthesisResult(speechSynthesisResult, message);
    }

    private static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
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