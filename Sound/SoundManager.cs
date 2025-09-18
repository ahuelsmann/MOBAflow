using System.Media;
using System.Runtime.Versioning;

namespace Moba.Sound;

public class SoundManager
{
    public SoundManager() { }

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