#pragma once

#include <cstddef>
#include <cstdint>

namespace MobaDisplay
{
namespace Udp
{
constexpr size_t kMaxPacketBytes = 768;
constexpr size_t kLegacyLineBytes = 480;
constexpr size_t kIndexedLineBytes = 482;
constexpr uint16_t kDisplayHeight = 280;
constexpr size_t kDisplayedHostVersionBytes = 28;

enum class PacketKind : uint8_t
{
    Empty,
    FrameStart,
    FrameDone,
    HostVersion,
    LegacyLine,
    IndexedLine,
    Truncated,
    Oversized,
    Malformed,
    Unknown
};

struct PacketView
{
    PacketKind kind;
    const uint8_t* payload;
    size_t payloadLength;
    uint16_t rowIndex;
};

PacketView ClassifyPacket(const uint8_t* buffer, size_t copiedLength, size_t datagramLength) noexcept;
}
}
