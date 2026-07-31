#include "UdpPacketParser.h"

#include <cstring>

namespace MobaDisplay
{
namespace Udp
{
namespace
{
constexpr uint8_t kVersionedMagic[] = {0x4D, 0x4F, 0x42, 0x41};
constexpr size_t kVersionedMagicLength = sizeof(kVersionedMagic);
constexpr size_t kVersionedHeaderLength = 32;

PacketView MakePacket(PacketKind kind) noexcept
{
    return {kind, nullptr, 0};
}

uint16_t ReadUInt16(const uint8_t* bytes) noexcept
{
    return static_cast<uint16_t>(
        (static_cast<uint16_t>(bytes[0]) << 8U) | bytes[1]);
}

bool HasVersionedMagicPrefix(const uint8_t* buffer, size_t datagramLength) noexcept
{
    const size_t prefixLength =
        datagramLength < kVersionedMagicLength ? datagramLength : kVersionedMagicLength;
    return std::memcmp(buffer, kVersionedMagic, prefixLength) == 0;
}

bool IsVersionedEnvelope(const uint8_t* buffer, size_t datagramLength) noexcept
{
    if (datagramLength < kVersionedHeaderLength
        || std::memcmp(buffer, kVersionedMagic, kVersionedMagicLength) != 0)
    {
        return false;
    }

    const uint16_t headerLength = ReadUInt16(buffer + 8);
    const uint16_t payloadLength = ReadUInt16(buffer + 10);
    return headerLength == kVersionedHeaderLength
        && datagramLength == static_cast<size_t>(headerLength) + payloadLength;
}
}

PacketView ClassifyPacket(const uint8_t* buffer, size_t copiedLength, size_t datagramLength) noexcept
{
    if (datagramLength == 0)
        return MakePacket(copiedLength == 0 ? PacketKind::Empty : PacketKind::Malformed);

    if (datagramLength > kMaxPacketBytes)
        return MakePacket(PacketKind::Oversized);

    if (!buffer)
        return MakePacket(PacketKind::Malformed);

    if (copiedLength < datagramLength)
        return MakePacket(PacketKind::Truncated);

    if (copiedLength > datagramLength)
        return MakePacket(PacketKind::Malformed);

    if (IsVersionedEnvelope(buffer, datagramLength))
        return {PacketKind::Versioned, buffer, datagramLength};

    if (HasVersionedMagicPrefix(buffer, datagramLength))
    {
        return MakePacket(
            datagramLength < kVersionedHeaderLength
                ? PacketKind::Truncated
                : PacketKind::Malformed);
    }

    return MakePacket(PacketKind::Unknown);
}
}
}
