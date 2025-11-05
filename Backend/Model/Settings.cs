namespace Moba.Backend.Model;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// This class represents general MOBA project related settings.
/// </summary>
public class Settings
{
    private uint _speechSynthesizerVolume = 90;

    public Settings()
    {
        SpeechSynthesizerRate = -1;
        SpeechSynthesizerVolume = 90;
    }

    [Display(Name = "Speech Key")]
    public string? SpeechKey { get; set; }

    [Display(Name = "Speech Region")]
    public string? SpeechRegion { get; set; }

    [Display(Name = "Speech Synthesizer Rate")]
    public int SpeechSynthesizerRate { get; set; }
    
    [Display(Name = "Speech Synthesizer Volume")]
    public uint SpeechSynthesizerVolume
    {
        get => _speechSynthesizerVolume;
        set => _speechSynthesizerVolume = Math.Clamp(value, 0, 100);
    }
    
    [Display(Name = "Speaker Engine Name")]
    public string? SpeakerEngineName { get; set; }
    
    [Display(Name = "Voice Name")]
    public string? VoiceName { get; set; }
    
    [Display(Name = "Journey Name")]
    public string? JourneyName { get; set; }
    
    [Display(Name = "Reset Window Layout On Start")]
    public bool IsResetWindowLayoutOnStart { get; set; }
}