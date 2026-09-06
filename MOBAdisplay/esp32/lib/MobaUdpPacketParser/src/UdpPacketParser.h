#pragma once

#include <cstddef>
#include <cstdint>

namespace MobaDisplay
{
namespace Udp
{
constexpr size_t kMaxPacketBytes = 1232;

enum class PacketKind : uint8_t
{
    Empty,
    Versioned,
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
};

PacketView ClassifyPacket(const uint8_t* buffer, size_t copiedLength, size_t datagramLength) noexcept;
}
}
