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

void TestCopiedAndDatagramLengthBoundaries()
{
    std::vector<uint8_t> packet(MobaDisplay::Udp::kMaxPacketBytes + 1, 'a');

    AssertKind(
        PacketKind::Oversized,
        ClassifyPacket(
            packet.data(),
            MobaDisplay::Udp::kMaxPacketBytes,
            MobaDisplay::Udp::kMaxPacketBytes + 1));
    AssertKind(PacketKind::Truncated, ClassifyPacket(packet.data(), 10, 11));
    AssertKind(PacketKind::Malformed, ClassifyPacket(packet.data(), 11, 10));
}

void TestVersionedEnvelopeBoundaries()
{
    const std::vector<uint8_t> magic = {0x4D, 0x4F, 0x42, 0x41};
    const std::vector<uint8_t> packet = MakeVersionedEnvelope(32);
    const std::vector<uint8_t> maximumPacket =
        MakeVersionedEnvelope(MobaDisplay::Udp::kMaxPacketBytes);
    std::vector<uint8_t> invalidHeader = packet;
    invalidHeader[9] = 31;

    AssertKind(PacketKind::Truncated, Classify(magic));
    const PacketView result = Classify(packet);
    AssertKind(PacketKind::Versioned, result);
    TEST_ASSERT_EQUAL_PTR(packet.data(), result.payload);
    TEST_ASSERT_EQUAL_size_t(packet.size(), result.payloadLength);
    AssertKind(PacketKind::Versioned, Classify(maximumPacket));
    AssertKind(PacketKind::Malformed, Classify(invalidHeader));
    AssertKind(PacketKind::Truncated, Classify(std::vector<uint8_t>{0x4D}));
    AssertKind(PacketKind::Truncated, Classify(std::vector<uint8_t>{0x4D, 0x4F, 0x42}));
}

void TestLegacyDisplayDatagramsAreUnknown()
{
    const std::vector<std::string> controls = {
        "HOST_VER:1.2.3",
        "FRAME_START",
        "FRAME_DONE"};
    for (const std::string& control : controls)
    {
        AssertKind(
            PacketKind::Unknown,
            Classify(std::vector<uint8_t>(control.begin(), control.end())));
    }

    AssertKind(PacketKind::Unknown, Classify(std::vector<uint8_t>(480, 0)));
    AssertKind(PacketKind::Unknown, Classify(std::vector<uint8_t>(482, 0)));
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
    RUN_TEST(TestCopiedAndDatagramLengthBoundaries);
    RUN_TEST(TestVersionedEnvelopeBoundaries);
    RUN_TEST(TestLegacyDisplayDatagramsAreUnknown);
    RUN_TEST(TestArbitraryPacketsRespectBounds);
    return UNITY_END();
}
