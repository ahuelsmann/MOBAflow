// Test value calculator for Z21Command.BuildSetTurnout
int decoderAddress = 203;
int output = 0;
bool activate = false;

// Calculate FAdr (corrected)
int fAdr = decoderAddress - 1;
byte adrMsb = (byte)((fAdr >> 8) & 0xFF);
byte adrLsb = (byte)(fAdr & 0xFF);

// Calculate command byte: 10Q0A00P
byte cmdByte = (byte)(
    0x80 |                              // 10XXXXXX
    (false ? 0x20 : 0x00) |            // Q flag (queue)
    (activate ? 0x08 : 0x00) |         // A flag
    (output & 0x01)                    // P flag
);

// Calculate XOR
byte xor = (byte)(0x53 ^ adrMsb ^ adrLsb ^ cmdByte);

// Build packet
byte[] packet = [0x0A, 0x00, 0x40, 0x00, 0x53, adrMsb, adrLsb, cmdByte, xor];

Console.WriteLine($"DecoderAddress: {decoderAddress}, Output: {output}, Activate: {activate}");
Console.WriteLine($"FAdr: {fAdr} (0x{fAdr:X})");
Console.WriteLine($"Packet: {BitConverter.ToString(packet)}");
Console.WriteLine();

// Test case 2
activate = true;
cmdByte = (byte)(0x80 | (activate ? 0x08 : 0x00) | (output & 0x01));
xor = (byte)(0x53 ^ adrMsb ^ adrLsb ^ cmdByte);
packet = [0x0A, 0x00, 0x40, 0x00, 0x53, adrMsb, adrLsb, cmdByte, xor];

Console.WriteLine($"DecoderAddress: {decoderAddress}, Output: {output}, Activate: {activate}");
Console.WriteLine($"FAdr: {fAdr} (0x{fAdr:X})");
Console.WriteLine($"Packet: {BitConverter.ToString(packet)}");
Console.WriteLine();

// Test GetTurnoutInfo
xor = (byte)(0x43 ^ adrMsb ^ adrLsb);
packet = [0x09, 0x00, 0x40, 0x00, 0x43, adrMsb, adrLsb, xor];

Console.WriteLine($"GetTurnoutInfo - DecoderAddress: {decoderAddress}");
Console.WriteLine($"FAdr: {fAdr} (0x{fAdr:X})");
Console.WriteLine($"Packet: {BitConverter.ToString(packet)}");
