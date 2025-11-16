namespace Moba.Backend.Model.Action;

using Enum;

using Sound;

using System.Runtime.Versioning;

/// <summary>
/// This class can be used to play a wave sound file.
/// </summary>
public class Audio : Base
{
    public Audio(string waveFile)
    {
        WaveFile = waveFile;
        Name = "New Wave Output";
    }

    /// <summary>
    /// Gets the action type, always returns ActionType.Sound.
    /// </summary>
    public override ActionType Type => ActionType.Sound;

    /// <summary>
    /// String representation of the wave file that is to be played.
    /// </summary>
    public string WaveFile { get; set; }

    [SupportedOSPlatform("windows")]
    public override Task ExecuteAsync(ActionExecutionContext context)
    {
        if (!string.IsNullOrEmpty(WaveFile) && context.SoundPlayer != null)
        {
            context.SoundPlayer.Play(WaveFile);
        }
        return Task.CompletedTask;
    }
}