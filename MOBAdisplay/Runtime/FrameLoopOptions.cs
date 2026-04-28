namespace Moba.Display.Runtime;

public sealed class FrameLoopOptions
{
    /// <summary>UDP target or unused when transport is serial.</summary>
    public DisplayTransportKind Transport { get; set; } = DisplayTransportKind.Udp;

    /// <summary>UDP: device IP.</summary>
    public string IpAddress { get; set; } = "192.168.0.82";

    /// <summary>UDP destination port.</summary>
    public int Port { get; set; } = 4210;

    /// <summary>COM port name (Windows: COM7).</summary>
    public string SerialPortName { get; set; } = string.Empty;

    /// <summary>UART baud rate when using USB-serial framing.</summary>
    public int SerialBaudRate { get; set; } = 921_600;

    public int RefreshHz { get; set; } = 1;
}
