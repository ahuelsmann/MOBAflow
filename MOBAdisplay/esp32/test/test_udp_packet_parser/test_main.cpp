#include <UdpPacketParser.h>
#include <unity.h>

#include <algorithm>
#include <cstdint>
#include <string>
#include <vector>

using MobaDisplay::Udp::ClassifyPacket;
using MobaDisplay::Udp::PacketKind;
using MobaDisplay::Udp::PacketView;

void setUp()
{
}

void tearDown()
{
}

namespace
{
void AssertKind(PacketKind expected, const PacketView& packet)
{
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(expected), static_cast<uint8_t>(packet.kind));
}

std::vector<uint8_t> MakeBytes(const std::string& text)
{
    return {text.begin(), text.end()};
}

PacketView Classify(const std::vector<uint8_t>& bytes)
{
    return ClassifyPacket(bytes.empty() ? nullptr : bytes.data(), bytes.size(), bytes.size());
}

std::vector<uint8_t> MakeVersionedEnvelope(size_t totalLength)
{
    std::vector<uint8_t> packet(totalLength, 0);
    packet[0] = 0x4D;
    packet[1] = 0x4F;
    packet[2] = 0x42;
    packet[3] = 0x41;
    packet[8] = 0;
    packet[9] = 32;
    const auto payloadLength = static_cast<uint16_t>(totalLength - 32);
    packet[10] = static_cast<uint8_t>(payloadLength >> 8U);
    packet[11] = static_cast<uint8_t>(payloadLength);
    return packet;
}

void TestEmptyAndInvalidBuffers()
{
    uint8_t byte = 0;

    AssertKind(PacketKind::Empty, ClassifyPacket(nullptr, 0, 0));
    AssertKind(PacketKind::Malformed, ClassifyPacket(&byte, 1, 0));
    AssertKind(PacketKind::Malformed, ClassifyPacket(nullptr, 0, 1));
    AssertKind(PacketKind::Malformed, ClassifyPacket(nullptr, 1, 1));
}

void TestControlPrefixBoundaries()
{
    const auto frameStartPrefix = MakeBytes("FRAME_STAR");
    const auto frameStart = MakeBytes("FRAME_START");
    const auto frameStartExtra = MakeBytes("FRAME_START!");
    const auto frameDonePrefix = MakeBytes("FRAME_DON");
    const auto frameDone = MakeBytes("FRAME_DONE");
    const auto frameDoneExtra = MakeBytes("FRAME_DONE!");
    const auto hostVersionPrefix = MakeBytes("HOST_VER");

    AssertKind(PacketKind::Truncated, Classify(frameStartPrefix));
    AssertKind(PacketKind::FrameStart, Classify(frameStart));
    AssertKind(PacketKind::Unknown, Classify(frameStartExtra));
    AssertKind(PacketKind::Truncated, Classify(frameDonePrefix));
    AssertKind(PacketKind::FrameDone, Classify(frameDone));
    AssertKind(PacketKind::Unknown, Classify(frameDoneExtra));
    AssertKind(PacketKind::Truncated, Classify(hostVersionPrefix));
}

void TestHostVersionBoundaries()
{
    const auto emptyVersion = MakeBytes("HOST_VER:");
    const auto oneByteVersion = MakeBytes("HOST_VER:v");
    const auto displayLimitVersion = MakeBytes("HOST_VER:" + std::string(MobaDisplay::Udp::kDisplayedHostVersionBytes, 'a'));
    const auto longerVersion = MakeBytes("HOST_VER:" + std::string(MobaDisplay::Udp::kDisplayedHostVersionBytes + 1, 'b'));
    auto embeddedNull = MakeBytes("HOST_VER:abc");
    embeddedNull.push_back(0);
    auto nonAscii = MakeBytes("HOST_VER:abc");
    nonAscii.push_back(0x80);

    AssertKind(PacketKind::Malformed, Classify(emptyVersion));

    const PacketView oneBytePacket = Classify(oneByteVersion);
    AssertKind(PacketKind::HostVersion, oneBytePacket);
    TEST_ASSERT_EQUAL_size_t(1, oneBytePacket.payloadLength);
    TEST_ASSERT_EQUAL_UINT8('v', oneBytePacket.payload[0]);

    const PacketView displayLimitPacket = Classify(displayLimitVersion);
    AssertKind(PacketKind::HostVersion, displayLimitPacket);
    TEST_ASSERT_EQUAL_size_t(MobaDisplay::Udp::kDisplayedHostVersionBytes, displayLimitPacket.payloadLength);

    const PacketView longerPacket = Classify(longerVersion);
    AssertKind(PacketKind::HostVersion, longerPacket);
    TEST_ASSERT_EQUAL_size_t(MobaDisplay::Udp::kDisplayedHostVersionBytes + 1, longerPacket.payloadLength);

    AssertKind(PacketKind::Malformed, Classify(embeddedNull));
    AssertKind(PacketKind::Malformed, Classify(nonAscii));
}

void TestFixedWidthLineBoundaries()
{
    std::vector<uint8_t> packet479(479, 0);
    std::vector<uint8_t> packet480(MobaDisplay::Udp::kLegacyLineBytes, 0);
    std::vector<uint8_t> packet481(481, 0);
    std::vector<uint8_t> packet482(MobaDisplay::Udp::kIndexedLineBytes, 0);
    std::vector<uint8_t> packet483(483, 0);

    AssertKind(PacketKind::Unknown, Classify(packet479));
    AssertKind(PacketKind::LegacyLine, Classify(packet480));
    AssertKind(PacketKind::Unknown, Classify(packet481));
    AssertKind(PacketKind::IndexedLine, Classify(packet482));
    AssertKind(PacketKind::Unknown, Classify(packet483));
}

void TestIndexedRowBoundaries()
{
    std::vector<uint8_t> packet(MobaDisplay::Udp::kIndexedLineBytes, 0);

    packet[0] = 0;
    packet[1] = 0;
    PacketView result = Classify(packet);
    AssertKind(PacketKind::IndexedLine, result);
    TEST_ASSERT_EQUAL_UINT16(0, result.rowIndex);
    TEST_ASSERT_EQUAL_size_t(MobaDisplay::Udp::kLegacyLineBytes, result.payloadLength);

    packet[0] = 0x01;
    packet[1] = 0x17;
    result = Classify(packet);
    AssertKind(PacketKind::IndexedLine, result);
    TEST_ASSERT_EQUAL_UINT16(279, result.rowIndex);

    packet[0] = 0x01;
    packet[1] = 0x18;
    AssertKind(PacketKind::Malformed, Classify(packet));

    packet[0] = 0xFF;
    packet[1] = 0xFF;
    AssertKind(PacketKind::Malformed, Classify(packet));
}

void TestCopiedAndDatagramLengthBoundaries()
{
    std::vector<uint8_t> packet(MobaDisplay::Udp::kMaxPacketBytes + 1, 'a');

    AssertKind(PacketKind::Unknown,
        ClassifyPacket(
            packet.data(),
            MobaDisplay::Udp::kLegacyMaxPacketBytes,
            MobaDisplay::Udp::kLegacyMaxPacketBytes));
    AssertKind(PacketKind::Oversized,
        ClassifyPacket(
            packet.data(),
            MobaDisplay::Udp::kLegacyMaxPacketBytes + 1,
            MobaDisplay::Udp::kLegacyMaxPacketBytes + 1));
    AssertKind(PacketKind::Oversized,
        ClassifyPacket(
            packet.data(),
            MobaDisplay::Udp::kMaxPacketBytes,
            MobaDisplay::Udp::kMaxPacketBytes));
    AssertKind(PacketKind::Oversized,
        ClassifyPacket(packet.data(), MobaDisplay::Udp::kMaxPacketBytes, MobaDisplay::Udp::kMaxPacketBytes + 1));
    AssertKind(PacketKind::Truncated, ClassifyPacket(packet.data(), 10, 11));
    AssertKind(PacketKind::Malformed, ClassifyPacket(packet.data(), 11, 10));
}

void TestMetadataPrecedesRowClassification()
{
    std::vector<uint8_t> legacyLengthPacket(MobaDisplay::Udp::kLegacyLineBytes, 'a');
    std::vector<uint8_t> indexedLengthPacket(MobaDisplay::Udp::kIndexedLineBytes, 'b');
    const auto prefix = MakeBytes("HOST_VER:");
    std::copy(prefix.begin(), prefix.end(), legacyLengthPacket.begin());
    std::copy(prefix.begin(), prefix.end(), indexedLengthPacket.begin());

    AssertKind(PacketKind::HostVersion, Classify(legacyLengthPacket));
    AssertKind(PacketKind::HostVersion, Classify(indexedLengthPacket));
}

void TestVersionedMagicRoutesToLengthSafeDispatcher()
{
    const std::vector<uint8_t> magic = {0x4D, 0x4F, 0x42, 0x41};
    const std::vector<uint8_t> packet = MakeVersionedEnvelope(32);
    const std::vector<uint8_t> maximumPacket =
        MakeVersionedEnvelope(MobaDisplay::Udp::kMaxPacketBytes);
    std::vector<uint8_t> legacyRow(MobaDisplay::Udp::kLegacyLineBytes, 0);
    std::copy(magic.begin(), magic.end(), legacyRow.begin());
    const std::vector<uint8_t> versionedLegacyLength =
        MakeVersionedEnvelope(MobaDisplay::Udp::kLegacyLineBytes);

    AssertKind(PacketKind::Truncated, Classify(magic));
    const PacketView result = Classify(packet);
    AssertKind(PacketKind::Versioned, result);
    TEST_ASSERT_EQUAL_PTR(packet.data(), result.payload);
    TEST_ASSERT_EQUAL_size_t(packet.size(), result.payloadLength);
    AssertKind(PacketKind::Versioned, Classify(maximumPacket));
    AssertKind(PacketKind::LegacyLine, Classify(legacyRow));
    AssertKind(PacketKind::Versioned, Classify(versionedLegacyLength));

    AssertKind(PacketKind::Truncated, Classify(std::vector<uint8_t>{0x4D}));
    AssertKind(PacketKind::Truncated, Classify(std::vector<uint8_t>{0x4D, 0x4F, 0x42}));
}

uint32_t NextRandom(uint32_t* state)
{
    *state = (*state * 1664525U) + 1013904223U;
    return *state;
}

void TestArbitraryPacketsRespectBounds()
{
    uint32_t randomState = 0x49A11CEU;

    for (size_t iteration = 0; iteration < 50000; ++iteration)
    {
        const size_t datagramLength = NextRandom(&randomState) % 1400U;
        const size_t copiedLength = NextRandom(&randomState) % 1400U;
        std::vector<uint8_t> buffer(copiedLength);
        for (uint8_t& value : buffer)
            value = static_cast<uint8_t>(NextRandom(&randomState) >> 24);

        const uint8_t* data = buffer.empty() ? nullptr : buffer.data();
        const PacketView result = ClassifyPacket(data, copiedLength, datagramLength);

        if (datagramLength > MobaDisplay::Udp::kMaxPacketBytes)
            AssertKind(PacketKind::Oversized, result);

        if (result.kind == PacketKind::HostVersion)
        {
            TEST_ASSERT_NOT_NULL(result.payload);
            TEST_ASSERT_GREATER_THAN_size_t(0, result.payloadLength);
            TEST_ASSERT_TRUE(result.payload >= data);
            TEST_ASSERT_TRUE(result.payload + result.payloadLength <= data + copiedLength);
        }

        if (result.kind == PacketKind::IndexedLine)
        {
            TEST_ASSERT_LESS_THAN_UINT16(MobaDisplay::Udp::kDisplayHeight, result.rowIndex);
            TEST_ASSERT_EQUAL_size_t(MobaDisplay::Udp::kLegacyLineBytes, result.payloadLength);
        }

        if (result.kind == PacketKind::Versioned)
        {
            TEST_ASSERT_EQUAL_PTR(data, result.payload);
            TEST_ASSERT_EQUAL_size_t(datagramLength, result.payloadLength);
        }
    }
}
}

int main(int, char**)
{
    UNITY_BEGIN();
    RUN_TEST(TestEmptyAndInvalidBuffers);
    RUN_TEST(TestControlPrefixBoundaries);
    RUN_TEST(TestHostVersionBoundaries);
    RUN_TEST(TestFixedWidthLineBoundaries);
    RUN_TEST(TestIndexedRowBoundaries);
    RUN_TEST(TestCopiedAndDatagramLengthBoundaries);
    RUN_TEST(TestMetadataPrecedesRowClassification);
    RUN_TEST(TestVersionedMagicRoutesToLengthSafeDispatcher);
    RUN_TEST(TestArbitraryPacketsRespectBounds);
    return UNITY_END();
}
