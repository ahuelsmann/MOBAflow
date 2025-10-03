namespace Moba.Backend;

using System;

/// <summary>
/// 7.1 LAN_RMBUS_DATACHANGED
/// </summary>
public class FeedbackResult(byte[] content)
{
    /// <summary>
    /// In port on feedback module (R-BUS)
    /// </summary>
    public int InPort { get; } = content.Length >= 6 ? content[5] : throw new ArgumentException("Invalid content.");
}