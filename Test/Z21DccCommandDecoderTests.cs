// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test;

using Moba.SharedUI.Helper;

/// <summary>
/// Tests for Z21 DCC command byte decoding.
/// Verifies that raw Z21 bytes are correctly decoded into Address, Speed, and Direction.
/// </summary>
[TestFixture]
public class Z21DccCommandDecoderTests
{
    [Test]
    public void DecodeLocoCommand_WithValidBytes_DecodesCorrectly()
    {
        // Arrange: "Stop Loco 101" command from JSON
        // Base64: CgBAAOQTAGWAEg== → Hex: 0A 00 40 00 E4 13 00 65 80 12
        // 
        // Byte structure (Z21 LAN_X_SET_LOCO_DRIVE):
        // [0-1]  0A 00 = Length (10 bytes)
        // [2-3]  40 00 = Header (LAN_X_HEADER)
        // [4]    E4    = X-Header (SET_LOCO_DRIVE)
        // [5]    13    = DB1 (Speed step format)
        // [6]    00    = DB2 (Address high byte)
        // [7]    65    = DB3 (Address low byte) = 101 decimal
        // [8]    80    = DB4 (Speed + Direction: 1 0000000 = Backward, Speed 0)
        // [9]    12    = XOR checksum
        byte[] bytes = [0x0A, 0x00, 0x40, 0x00, 0xE4, 0x13, 0x00, 0x65, 0x80, 0x12];

        // Act
        var result = Z21DccCommandDecoder.DecodeLocoCommand(bytes);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Address, Is.EqualTo(101));  // DCC Address 101
        Assert.That(result.Speed, Is.EqualTo(0));      // Speed 0 (stop)
        Assert.That(result.Direction, Is.EqualTo("Backward"));
    }

    [Test]
    public void EncodeLocoCommand_WithValidParameters_EncodesCorrectly()
    {
        // Arrange
        int address = 101;
        int speed = 0;
        string direction = "Backward";

        // Expected bytes: 0A 00 40 00 E4 13 00 65 80 12
        byte[] expectedBytes = [0x0A, 0x00, 0x40, 0x00, 0xE4, 0x13, 0x00, 0x65, 0x80, 0x12];

        // Act
        var result = Z21DccCommandDecoder.EncodeLocoCommand(address, speed, direction);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(10));
        Assert.That(result, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void EncodeLocoCommand_RoundTrip_PreservesValues()
    {
        // Arrange
        int originalAddress = 101;
        int originalSpeed = 64;
        string originalDirection = "Forward";

        // Act: Encode → Decode
        var encoded = Z21DccCommandDecoder.EncodeLocoCommand(originalAddress, originalSpeed, originalDirection);
        var decoded = Z21DccCommandDecoder.DecodeLocoCommand(encoded);

        // Assert
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Address, Is.EqualTo(originalAddress));
        Assert.That(decoded.Speed, Is.EqualTo(originalSpeed));
        Assert.That(decoded.Direction, Is.EqualTo(originalDirection));
    }

    [Test]
    public void DecodeLocoCommand_WithEmptyBytes_ReturnsInvalid()
    {
        // Arrange
        byte[] bytes = [];

        // Act
        var result = Z21DccCommandDecoder.DecodeLocoCommand(bytes);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void DecodeLocoCommand_WithNullBytes_ReturnsInvalid()
    {
        // Arrange
        byte[]? bytes = null;

        // Act
        var result = Z21DccCommandDecoder.DecodeLocoCommand(bytes);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void FormatBytesAsHex_FormatsCorrectly()
    {
        // Arrange
        byte[] bytes = [0x0A, 0x00, 0x80, 0x00, 0xE4];

        // Act
        var hex = Z21DccCommandDecoder.FormatBytesAsHex(bytes);

        // Assert
        Assert.That(hex, Is.EqualTo("0A 00 80 00 E4"));
    }

    [Test]
    public void FormatDccCommand_WithValidCommand_FormatsReadable()
    {
        // Arrange
        var command = new Z21DccCommandDecoder.DccCommand
        {
            Address = 101,
            Speed = 127,
            Direction = "Forward",
            IsValid = true
        };

        // Act
        var formatted = Z21DccCommandDecoder.FormatDccCommand(command);

        // Assert
        Assert.That(formatted, Contains.Substring("101"));
        Assert.That(formatted, Contains.Substring("127"));
        Assert.That(formatted, Contains.Substring("Forward"));
    }
}
