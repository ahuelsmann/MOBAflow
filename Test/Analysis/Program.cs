// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
// Test value calculator for Z21Command.BuildSetTurnout
const int decoderAddress = 203;
const int output = 0;

// Calculate FAdr (corrected)
int fAdr = decoderAddress - 1;
byte adrMsb = (byte)((fAdr >> 8) & 0xFF);
byte adrLsb = (byte)(fAdr & 0xFF);

foreach (var activate in new[] { false, true })
{
    // Calculate command byte: 10Q0A00P
    byte cmdByte = (byte)(
        0x80 |                              // 10XXXXXX
        (activate ? 0x08 : 0x00) |         // A flag
        (output & 0x01)                    // P flag
    );

    // Calculate XOR
    byte setXor = (byte)(0x53 ^ adrMsb ^ adrLsb ^ cmdByte);

    // Build packet
    byte[] setPacket = [0x0A, 0x00, 0x40, 0x00, 0x53, adrMsb, adrLsb, cmdByte, setXor];

    Console.WriteLine($"DecoderAddress: {decoderAddress}, Output: {output}, Activate: {activate}");
    Console.WriteLine($"FAdr: {fAdr} (0x{fAdr:X})");
    Console.WriteLine($"Packet: {BitConverter.ToString(setPacket)}");
    Console.WriteLine();
}

// Test GetTurnoutInfo
byte xor = (byte)(0x43 ^ adrMsb ^ adrLsb);
byte[] packet = [0x09, 0x00, 0x40, 0x00, 0x43, adrMsb, adrLsb, xor];

Console.WriteLine($"GetTurnoutInfo - DecoderAddress: {decoderAddress}");
Console.WriteLine($"FAdr: {fAdr} (0x{fAdr:X})");
Console.WriteLine($"Packet: {BitConverter.ToString(packet)}");
