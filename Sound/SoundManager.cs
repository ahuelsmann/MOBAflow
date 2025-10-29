using System.Media;
using System.Runtime.Versioning;

namespace Moba.Sound;

public static class SoundManager
{
    [SupportedOSPlatform("windows")]
    public static void Play(string waveFile)
    {
        SoundPlayer player = new()
        {
            SoundLocation = waveFile
        };
        player.Play();
        Thread.Sleep(1500);
    }
}