namespace Moba.Display.Runtime;

/// <summary>
/// How rendered frames are transmitted to the ESP32 display firmware.
/// </summary>
public enum DisplayTransportKind
{
    /// <summary>UDP (device on the same LAN as this PC).</summary>
    Udp,

    /// <summary>USB/UART (same line protocol as UDP, written to a COM port).</summary>
    Serial,
}
