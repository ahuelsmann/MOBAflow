namespace Moba.Display.Runtime;

public sealed class FrameLoopOptions
{
    /// <summary>Wi-Fi target ESP32 address (UDP).</summary>
    public string IpAddress { get; set; } = "192.168.0.82";

    /// <summary>UDP destination port.</summary>
    public int Port { get; set; } = 4210;

    public int RefreshHz { get; set; } = 1;
}
