namespace Moba.Backend.Model;

using System.ComponentModel.DataAnnotations;

public class Setting
{
    private uint _speechSynthesizerVolume = 90;

    public Setting()
    {
        SpeechSynthesizerRate = -1;
        SpeechSynthesizerVolume = 90;
    }

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