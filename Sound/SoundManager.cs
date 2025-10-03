using System.Media;
using System.Runtime.Versioning;

namespace Moba.Sound;

public static class SoundManager
{
    [SupportedOSPlatform("windows")]
    public static void Gong()
    {
        SoundPlayer player = new()
        {
            SoundLocation = "Ton.wav"
        };
        player.Play();
        Thread.Sleep(1500);
    }
}