namespace Moba.Sound;

/// <summary>
/// Interface for playing sound files.
/// Allows for platform-specific implementations and testability.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    /// Plays a wave file from the specified path.
    /// </summary>
    /// <param name="waveFile">Full path to the .wav file to play</param>
    void Play(string waveFile);
}
