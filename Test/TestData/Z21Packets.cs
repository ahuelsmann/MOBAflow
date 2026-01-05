// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TestData;

public static class Z21Packets
{
    public static readonly byte[] RBusFeedback_InPort5 =
    [
        0x0F, 0x00, 0x80, 0x00, // Length + Header (LAN_RMBUS_DATACHANGED)
        0x00,                   // Group number (module 0)
        0x10,                   // Data byte 0: bit4 set => InPort 5 (1-based)
        0x00, 0x00, 0x00, 0x00, // Remaining data bytes (no other bits set)
        0x00, 0x00, 0x00, 0x00, // Padding/XOR (ignored by parser in tests)
        0x00
    ];
    public static readonly byte[] XBus_StatusChanged_AllFlags = [0x07,0x00,0x40,0x00, 0x62, 0x00, 0x07];
    public static readonly byte[] SystemState_MinPayload = [0x14,0x00,0x84,0x00, 0x10,0x00, 0x20,0x00, 0x30,0x00, 0x40,0x00, 0x50,0x00, 0xAA, 0xBB];
}
