#include "UdpPacketParser.h"

#include <cstring>

namespace MobaDisplay
{
namespace Udp
{
namespace
{
constexpr char kFrameStart[] = "FRAME_START";
constexpr char kFrameDone[] = "FRAME_DONE";
constexpr char kHostVersionPrefix[] = "HOST_VER:";
constexpr uint8_t kVersionedMagic[] = {0x4D, 0x4F, 0x42, 0x41};

constexpr size_t kFrameStartLength = sizeof(kFrameStart) - 1;
constexpr size_t kFrameDoneLength = sizeof(kFrameDone) - 1;
constexpr size_t kHostVersionPrefixLength = sizeof(kHostVersionPrefix) - 1;
constexpr size_t kVersionedMagicLength = sizeof(kVersionedMagic);

PacketView MakePacket(PacketKind kind) noexcept
{
    return {kind, nullptr, 0, 0};
}

bool Matches(const uint8_t* buffer, size_t length, const char* expected, size_t expectedLength) noexcept
{
    return length == expectedLength && std::memcmp(buffer, expected, expectedLength) == 0;
}

bool StartsWith(const uint8_t* buffer, size_t length, const char* expected, size_t expectedLength) noexcept
{
    return length >= expectedLength && std::memcmp(buffer, expected, expectedLength) == 0;
}

bool IsStrictPrefix(const uint8_t* buffer, size_t length, const char* expected, size_t expectedLength) noexcept
{
    return length > 0 && length < expectedLength && std::memcmp(buffer, expected, length) == 0;
}

bool IsPrintableAscii(const uint8_t* buffer, size_t length) noexcept
{
    for (size_t index = 0; index < length; ++index)
    {
        if (buffer[index] < 0x20 || buffer[index] > 0x7E)
            return false;
    }

    return true;
}
}

PacketView ClassifyPacket(const uint8_t* buffer, size_t copiedLength, size_t datagramLength) noexcept
{
    if (datagramLength == 0)
        return MakePacket(copiedLength == 0 ? PacketKind::Empty : PacketKind::Malformed);

    // Reject by the original datagram length before examining a potentially truncated receive buffer.
    if (datagramLength > kMaxPacketBytes)
        return MakePacket(PacketKind::Oversized);

    if (!buffer)
        return MakePacket(PacketKind::Malformed);

    if (copiedLength < datagramLength)
        return MakePacket(PacketKind::Truncated);

    if (copiedLength > datagramLength)
        return MakePacket(PacketKind::Malformed);

    if (datagramLength >= kVersionedMagicLength
        && std::memcmp(buffer, kVersionedMagic, kVersionedMagicLength) == 0)
    {
        return {PacketKind::Versioned, buffer, datagramLength, 0};
    }

    if (datagramLength < kVersionedMagicLength
        && std::memcmp(buffer, kVersionedMagic, datagramLength) == 0)
    {
        return MakePacket(PacketKind::Truncated);
    }

    if (datagramLength > kLegacyMaxPacketBytes)
        return MakePacket(PacketKind::Oversized);

    if (Matches(buffer, datagramLength, kFrameStart, kFrameStartLength))
        return MakePacket(PacketKind::FrameStart);

    if (Matches(buffer, datagramLength, kFrameDone, kFrameDoneLength))
        return MakePacket(PacketKind::FrameDone);

    if (StartsWith(buffer, datagramLength, kHostVersionPrefix, kHostVersionPrefixLength))
    {
        const uint8_t* payload = buffer + kHostVersionPrefixLength;
        const size_t payloadLength = datagramLength - kHostVersionPrefixLength;
        if (payloadLength == 0 || !IsPrintableAscii(payload, payloadLength))
            return MakePacket(PacketKind::Malformed);

        return {PacketKind::HostVersion, payload, payloadLength, 0};
    }

    if (IsStrictPrefix(buffer, datagramLength, kFrameStart, kFrameStartLength)
        || IsStrictPrefix(buffer, datagramLength, kFrameDone, kFrameDoneLength)
        || IsStrictPrefix(buffer, datagramLength, kHostVersionPrefix, kHostVersionPrefixLength))
    {
        return MakePacket(PacketKind::Truncated);
    }

    if (datagramLength == kLegacyLineBytes)
        return MakePacket(PacketKind::LegacyLine);

    if (datagramLength == kIndexedLineBytes)
    {
        const uint16_t rowIndex = static_cast<uint16_t>((static_cast<uint16_t>(buffer[0]) << 8) | buffer[1]);
        if (rowIndex >= kDisplayHeight)
            return MakePacket(PacketKind::Malformed);

        return {PacketKind::IndexedLine, buffer + 2, kLegacyLineBytes, rowIndex};
    }

    return MakePacket(PacketKind::Unknown);
}
}
}
