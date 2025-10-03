namespace Moba.Backend.Model;

public class Setting
{
    private uint _speechSynthesizerVolume = 90;

    public Setting()
    {
        Name = "Standard";
        SpeechSynthesizerRate = -1;
        SpeechSynthesizerVolume = 90;
    }

    public string Name { get; set; }
    public int SpeechSynthesizerRate { get; set; }
    
    public uint SpeechSynthesizerVolume
    {
        get => _speechSynthesizerVolume;
        set => _speechSynthesizerVolume = Math.Clamp(value, 0, 100);
    }
    
    public string? SpeakerEngineName { get; set; }
    public string? VoiceName { get; set; }
    public string? JourneyName { get; set; }
    public bool IsResetWindowLayoutOnStart { get; set; }
}