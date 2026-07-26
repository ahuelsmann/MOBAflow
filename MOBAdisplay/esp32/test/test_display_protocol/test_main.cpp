#include <DisplayProtocolDispatcher.h>
#include <RecordingDisplayBackend.h>
#include <Rgb565BigEndianPresenter.h>
#include <unity.h>

#include <algorithm>
#include <array>
#include <cstdint>
#include <vector>

using MobaDisplay::Core::DisplayCapabilities;
using MobaDisplay::Core::FrameAssembler;
using MobaDisplay::Core::RecordingDisplayBackend;
using MobaDisplay::Protocol::DeviceDiagnostics;
using MobaDisplay::Protocol::DispatchResult;
using MobaDisplay::Protocol::DisplayProtocolDispatcher;
using MobaDisplay::Protocol::MessageType;

void setUp()
{
    // Unity requires this hook even when the fixture has no per-test setup.
}

void tearDown()
{
    // Unity requires this hook even when the fixture has no per-test cleanup.
}

namespace
{
struct RecordingTftDriver
{
    bool swapBytes = true;
    bool swapBytesDuringPush = true;
    std::array<uint8_t, 4> pushedBytes{};

    bool getSwapBytes() const noexcept
    {
        return swapBytes;
    }

    void setSwapBytes(bool value) noexcept
    {
        swapBytes = value;
    }

    void pushImage(int32_t, int32_t, uint16_t, uint16_t, uint16_t* pixels) noexcept
    {
        swapBytesDuringPush = swapBytes;
        const uint8_t* bytes = reinterpret_cast<const uint8_t*>(pixels);
        std::copy(bytes, bytes + pushedBytes.size(), pushedBytes.begin());
    }
};

constexpr uint16_t kWidth = 4;
constexpr uint16_t kHeight = 2;
constexpr uint32_t kSessionId = 0x10203040U;
constexpr uint8_t kAcknowledgementRequired =
    MobaDisplay::Protocol::FlagAcknowledgementRequired;
constexpr std::array<uint8_t, 16> kFrameBytes = {{
    0xF8, 0x00, 0x07, 0xE0, 0x00, 0x1F, 0xFF, 0xFF,
    0x00, 0x00, 0xFF, 0xFF, 0xF8, 0x00, 0x07, 0xE0}};

uint16_t ReadUInt16(const uint8_t* bytes)
{
    return static_cast<uint16_t>(
        (static_cast<uint16_t>(bytes[0]) << 8U) | bytes[1]);
}

uint32_t ReadUInt32(const uint8_t* bytes)
{
    return (static_cast<uint32_t>(bytes[0]) << 24U)
        | (static_cast<uint32_t>(bytes[1]) << 16U)
        | (static_cast<uint32_t>(bytes[2]) << 8U)
        | bytes[3];
}

void WriteUInt16(uint8_t* bytes, uint16_t value)
{
    bytes[0] = static_cast<uint8_t>(value >> 8U);
    bytes[1] = static_cast<uint8_t>(value);
}

void WriteUInt32(uint8_t* bytes, uint32_t value)
{
    bytes[0] = static_cast<uint8_t>(value >> 24U);
    bytes[1] = static_cast<uint8_t>(value >> 16U);
    bytes[2] = static_cast<uint8_t>(value >> 8U);
    bytes[3] = static_cast<uint8_t>(value);
}

std::vector<uint8_t> MakePacket(
    MessageType messageType,
    uint32_t requestId,
    uint32_t frameId,
    uint32_t sessionId,
    const std::vector<uint8_t>& payload,
    uint8_t flags = kAcknowledgementRequired,
    uint16_t packetIndex = 0,
    uint16_t packetCount = 1)
{
    std::vector<uint8_t> datagram(MobaDisplay::Protocol::kHeaderLength + payload.size(), 0);
    WriteUInt32(datagram.data(), MobaDisplay::Protocol::kMagic);
    datagram[4] = MobaDisplay::Protocol::kCurrentMajorVersion;
    datagram[5] = MobaDisplay::Protocol::kCurrentMinorVersion;
    datagram[6] = static_cast<uint8_t>(messageType);
    datagram[7] = flags;
    WriteUInt16(datagram.data() + 8, MobaDisplay::Protocol::kHeaderLength);
    WriteUInt16(datagram.data() + 10, static_cast<uint16_t>(payload.size()));
    WriteUInt32(datagram.data() + 12, requestId);
    WriteUInt32(datagram.data() + 16, frameId);
    WriteUInt32(datagram.data() + 20, sessionId);
    WriteUInt16(datagram.data() + 24, packetIndex);
    WriteUInt16(datagram.data() + 26, packetCount);
    WriteUInt32(
        datagram.data() + 28,
        FrameAssembler::ComputeCrc32(
            payload.empty() ? nullptr : payload.data(),
            payload.size()));
    if (!payload.empty())
        std::copy(payload.begin(), payload.end(), datagram.begin() + MobaDisplay::Protocol::kHeaderLength);
    return datagram;
}

std::vector<uint8_t> MakeHelloPayload(
    uint8_t minimumMajor = 1,
    uint8_t maximumMajor = 1,
    uint16_t hostMaximumDatagramLength =
        MobaDisplay::Protocol::kDefaultMaximumDatagramLength)
{
    std::vector<uint8_t> payload = {minimumMajor, 0, maximumMajor, 0, 0, 0, 0, 0};
    WriteUInt16(payload.data() + 4, hostMaximumDatagramLength);
    return payload;
}

std::vector<uint8_t> MakeBeginPayload()
{
    std::vector<uint8_t> payload(16, 0);
    WriteUInt16(payload.data(), kWidth);
    WriteUInt16(payload.data() + 2, kHeight);
    payload[4] = 1;
    payload[5] = 0;
    WriteUInt32(payload.data() + 8, kFrameBytes.size());
    WriteUInt32(
        payload.data() + 12,
        FrameAssembler::ComputeCrc32(kFrameBytes.data(), kFrameBytes.size()));
    return payload;
}

std::vector<uint8_t> MakeRegionPayload(uint16_t row)
{
    constexpr size_t rowByteCount = kWidth * 2U;
    std::vector<uint8_t> payload(16 + rowByteCount, 0);
    WriteUInt16(payload.data(), 0);
    WriteUInt16(payload.data() + 2, row);
    WriteUInt16(payload.data() + 4, kWidth);
    WriteUInt16(payload.data() + 6, 1);
    WriteUInt32(payload.data() + 8, static_cast<uint32_t>(row) * rowByteCount);
    WriteUInt32(payload.data() + 12, rowByteCount);
    const size_t sourceOffset = static_cast<size_t>(row) * rowByteCount;
    std::copy(
        kFrameBytes.begin() + sourceOffset,
        kFrameBytes.begin() + sourceOffset + rowByteCount,
        payload.begin() + 16);
    return payload;
}

std::vector<uint8_t> MakeCompletePayload()
{
    std::vector<uint8_t> payload(4, 0);
    WriteUInt32(
        payload.data(),
        FrameAssembler::ComputeCrc32(kFrameBytes.data(), kFrameBytes.size()));
    return payload;
}

void AssertResponse(
    MessageType expectedType,
    uint32_t expectedRequestId,
    uint32_t expectedFrameId,
    uint32_t expectedSessionId,
    const DispatchResult& result,
    const std::array<uint8_t, MobaDisplay::Protocol::kDefaultMaximumDatagramLength>& response)
{
    TEST_ASSERT_TRUE(result.recognized);
    TEST_ASSERT_TRUE(result.hasResponse);
    TEST_ASSERT_TRUE(result.responseLength >= MobaDisplay::Protocol::kHeaderLength);
    TEST_ASSERT_EQUAL_HEX32(MobaDisplay::Protocol::kMagic, ReadUInt32(response.data()));
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(expectedType), response[6]);
    TEST_ASSERT_EQUAL_UINT8(MobaDisplay::Protocol::FlagResponse, response[7]);
    TEST_ASSERT_EQUAL_UINT32(expectedRequestId, ReadUInt32(response.data() + 12));
    TEST_ASSERT_EQUAL_UINT32(expectedFrameId, ReadUInt32(response.data() + 16));
    TEST_ASSERT_EQUAL_UINT32(expectedSessionId, ReadUInt32(response.data() + 20));
    TEST_ASSERT_EQUAL_UINT16(0, ReadUInt16(response.data() + 24));
    TEST_ASSERT_EQUAL_UINT16(1, ReadUInt16(response.data() + 26));
    const uint16_t payloadLength = ReadUInt16(response.data() + 10);
    TEST_ASSERT_EQUAL_size_t(
        MobaDisplay::Protocol::kHeaderLength + payloadLength,
        result.responseLength);
    TEST_ASSERT_EQUAL_HEX32(
        ReadUInt32(response.data() + 28),
        FrameAssembler::ComputeCrc32(
            response.data() + MobaDisplay::Protocol::kHeaderLength,
            payloadLength));
}

struct Fixture
{
    std::array<uint8_t, kFrameBytes.size()> staging{};
    std::array<uint8_t, 2> tracking{};
    std::array<uint8_t, kFrameBytes.size()> presented{};
    std::array<uint8_t, MobaDisplay::Protocol::kDefaultMaximumDatagramLength> response{};
    DisplayCapabilities capabilities = {
        kWidth,
        kHeight,
        64,
        MobaDisplay::Core::PixelFormatRgb565BigEndian,
        MobaDisplay::Core::RotationDegrees0,
        static_cast<uint8_t>(
            MobaDisplay::Core::OptionalCommandClear
            | MobaDisplay::Core::OptionalCommandRenderTestPattern),
        static_cast<uint8_t>(
            MobaDisplay::Core::FrameCapabilityFullFrameStaging
            | MobaDisplay::Core::FrameCapabilityRegionTransfer
            | MobaDisplay::Core::FrameCapabilityAtomicPresentation),
        "recording-4x2"};
    RecordingDisplayBackend backend =
        RecordingDisplayBackend(capabilities, presented.data(), presented.size());
    FrameAssembler assembler = FrameAssembler(
        backend,
        staging.data(),
        staging.size(),
        tracking.data(),
        tracking.size(),
        1000);
    DisplayProtocolDispatcher dispatcher = DisplayProtocolDispatcher(
        assembler,
        backend,
        kSessionId,
        MobaDisplay::Protocol::kDefaultMaximumDatagramLength,
        "esp32s3-test",
        "v1-test");

    DispatchResult Dispatch(const std::vector<uint8_t>& packet, uint32_t nowMilliseconds = 0)
    {
        response.fill(0);
        return dispatcher.Dispatch(
            packet.data(),
            packet.size(),
            nowMilliseconds,
            DeviceDiagnostics{nowMilliseconds / 1000U, 123456U},
            response.data(),
            response.size());
    }
};

void TestHelloNegotiatesCapabilitiesWithoutLeakingSessionIntoEnvelope()
{
    Fixture fixture;
    TEST_ASSERT_FALSE(fixture.dispatcher.IsNegotiated());
    const DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 1, 0, 0, MakeHelloPayload()));

    AssertResponse(
        MessageType::CapabilitiesResponse,
        1,
        0,
        0,
        result,
        fixture.response);
    const uint8_t* payload = fixture.response.data() + MobaDisplay::Protocol::kHeaderLength;
    TEST_ASSERT_EQUAL_UINT8(1, payload[0]);
    TEST_ASSERT_EQUAL_UINT8(0, payload[1]);
    TEST_ASSERT_EQUAL_UINT16(kWidth, ReadUInt16(payload + 2));
    TEST_ASSERT_EQUAL_UINT16(kHeight, ReadUInt16(payload + 4));
    TEST_ASSERT_EQUAL_UINT16(64, ReadUInt16(payload + 8));
    TEST_ASSERT_EQUAL_UINT32(kSessionId, ReadUInt32(payload + 16));
    TEST_ASSERT_EQUAL_UINT8(12, payload[20]);
    TEST_ASSERT_EQUAL_MEMORY("esp32s3-test", payload + 21, 12);
    TEST_ASSERT_TRUE(fixture.dispatcher.IsNegotiated());
}

void TestFullFrameTransactionPresentsExactlyOnce()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 10, 100, kSessionId, MakeBeginPayload()),
        1);
    AssertResponse(MessageType::Result, 10, 100, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Ok),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);

    result = fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            11,
            100,
            kSessionId,
            MakeRegionPayload(0),
            kAcknowledgementRequired,
            0,
            2),
        2);
    AssertResponse(MessageType::Result, 11, 100, kSessionId, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            12,
            100,
            kSessionId,
            MakeRegionPayload(1),
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagFinalPacket),
            1,
            2),
        3);
    AssertResponse(MessageType::Result, 12, 100, kSessionId, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(MessageType::CompleteFrame, 13, 100, kSessionId, MakeCompletePayload()),
        4);
    AssertResponse(MessageType::Result, 13, 100, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        MobaDisplay::Core::ResultFlagPresented,
        fixture.response[MobaDisplay::Protocol::kHeaderLength + 1]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
    TEST_ASSERT_EQUAL_MEMORY(kFrameBytes.data(), fixture.presented.data(), kFrameBytes.size());

    result = fixture.Dispatch(
        MakePacket(
            MessageType::CompleteFrame,
            13,
            100,
            kSessionId,
            MakeCompletePayload(),
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)),
        5);
    AssertResponse(MessageType::Result, 13, 100, kSessionId, result, fixture.response);
    TEST_ASSERT_BITS_HIGH(
        MobaDisplay::Core::ResultFlagDuplicate,
        fixture.response[MobaDisplay::Protocol::kHeaderLength + 1]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
}

void TestWrongSessionAndChangedRequestIdReuseFailClosed()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 20, 0, kSessionId + 1, {}));
    AssertResponse(MessageType::Result, 20, 0, kSessionId + 1, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::WrongSession),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);

    result = fixture.Dispatch(
        MakePacket(MessageType::Clear, 21, 0, kSessionId, {0x12, 0x34}));
    AssertResponse(MessageType::Result, 21, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());

    result = fixture.Dispatch(
        MakePacket(
            MessageType::Clear,
            21,
            0,
            kSessionId,
            {0x56, 0x78},
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)));
    AssertResponse(MessageType::Result, 21, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Invalid),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());
}

void TestIdenticalCommandRetryReturnsCachedResultWithoutExecutingAgain()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::Clear, 22, 0, kSessionId, {0x12, 0x34}));
    AssertResponse(MessageType::Result, 22, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());

    result = fixture.Dispatch(
        MakePacket(
            MessageType::Clear,
            22,
            0,
            kSessionId,
            {0x12, 0x34},
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)));
    AssertResponse(MessageType::Result, 22, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_BITS_HIGH(
        MobaDisplay::Core::ResultFlagDuplicate,
        fixture.response[MobaDisplay::Protocol::kHeaderLength + 1]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());
}

void TestFreshHelloStartsNewRequestEpochAndRestoresDeviceDatagramLimit()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(
            MessageType::HelloRequest,
            1,
            0,
            0,
            MakeHelloPayload(1, 1, 256)));
    AssertResponse(MessageType::CapabilitiesResponse, 1, 0, 0, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT16(
        256,
        ReadUInt16(fixture.response.data() + MobaDisplay::Protocol::kHeaderLength + 6));

    result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 2, 0, kSessionId, {}));
    AssertResponse(MessageType::HealthResponse, 2, 0, kSessionId, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 100, 0, 0, MakeHelloPayload()));
    AssertResponse(MessageType::CapabilitiesResponse, 100, 0, 0, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT16(
        MobaDisplay::Protocol::kDefaultMaximumDatagramLength,
        ReadUInt16(fixture.response.data() + MobaDisplay::Protocol::kHeaderLength + 6));

    result = fixture.Dispatch(
        MakePacket(MessageType::Clear, 2, 0, kSessionId, {0x12, 0x34}));
    AssertResponse(MessageType::Result, 2, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());
}

void TestDuplicatedHelloPreservesTheCurrentRequestHistory()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 1, 0, 0, MakeHelloPayload()));
    AssertResponse(MessageType::CapabilitiesResponse, 1, 0, 0, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 2, 0, kSessionId, {}));
    AssertResponse(MessageType::HealthResponse, 2, 0, kSessionId, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 1, 0, 0, MakeHelloPayload()));
    AssertResponse(MessageType::CapabilitiesResponse, 1, 0, 0, result, fixture.response);

    result = fixture.Dispatch(
        MakePacket(MessageType::Clear, 2, 0, kSessionId, {0x12, 0x34}));
    AssertResponse(MessageType::Result, 2, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Invalid),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.ClearCallCount());
}

void TestOptionalCommandsReflectBackendCapabilities()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::SetBrightness, 30, 0, kSessionId, {50}));
    AssertResponse(MessageType::Result, 30, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Unsupported),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);

    result = fixture.Dispatch(
        MakePacket(MessageType::RenderTestPattern, 31, 0, kSessionId, {1, 0, 0, 0}));
    AssertResponse(MessageType::Result, 31, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Ok),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.TestPatternCallCount());
}

void TestMalformedPacketsAreDroppedAndUnsupportedVersionIsStructured()
{
    Fixture fixture;
    std::vector<uint8_t> packet =
        MakePacket(MessageType::HealthRequest, 40, 0, kSessionId, {});
    packet[28] ^= 1;
    DispatchResult result = fixture.Dispatch(packet);
    TEST_ASSERT_TRUE(result.recognized);
    TEST_ASSERT_FALSE(result.hasResponse);

    packet = MakePacket(MessageType::HealthRequest, 41, 0, kSessionId, {});
    packet.push_back(0);
    result = fixture.Dispatch(packet);
    TEST_ASSERT_TRUE(result.recognized);
    TEST_ASSERT_FALSE(result.hasResponse);

    result = fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 42, 0, 0, MakeHelloPayload(2, 2)));
    AssertResponse(MessageType::Result, 42, 0, 0, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::UnsupportedVersion),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
}

void TestRegionAcknowledgementCanBeOmittedOnlyAfterSuccess()
{
    Fixture fixture;
    fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 50, 200, kSessionId, MakeBeginPayload()),
        1);

    const DispatchResult result = fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            51,
            200,
            kSessionId,
            MakeRegionPayload(0),
            MobaDisplay::Protocol::FlagNone,
            0,
            2),
        2);
    TEST_ASSERT_TRUE(result.recognized);
    TEST_ASSERT_FALSE(result.hasResponse);

    const DispatchResult invalid = fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            52,
            200,
            kSessionId,
            {0},
            MobaDisplay::Protocol::FlagNone,
            1,
            2),
        3);
    AssertResponse(MessageType::Result, 52, 200, kSessionId, invalid, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Invalid),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
}

void TestIncompleteCompletionRetryReevaluatesAfterMissingRegionArrives()
{
    Fixture fixture;
    fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 70, 400, kSessionId, MakeBeginPayload()),
        1);
    fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            71,
            400,
            kSessionId,
            MakeRegionPayload(0),
            kAcknowledgementRequired,
            0,
            2),
        2);

    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::CompleteFrame, 72, 400, kSessionId, MakeCompletePayload()),
        3);
    AssertResponse(MessageType::Result, 72, 400, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Incomplete),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);

    fixture.Dispatch(
        MakePacket(
            MessageType::FrameRegion,
            73,
            400,
            kSessionId,
            MakeRegionPayload(1),
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagFinalPacket),
            1,
            2),
        4);
    result = fixture.Dispatch(
        MakePacket(
            MessageType::CompleteFrame,
            72,
            400,
            kSessionId,
            MakeCompletePayload(),
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)),
        5);

    AssertResponse(MessageType::Result, 72, 400, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Ok),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_BITS_HIGH(
        MobaDisplay::Core::ResultFlagPresented,
        fixture.response[MobaDisplay::Protocol::kHeaderLength + 1]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
}

void TestBusyBeginRetryReevaluatesAfterActiveFrameIsAborted()
{
    Fixture fixture;
    fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 80, 500, kSessionId, MakeBeginPayload()),
        1);

    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 81, 501, kSessionId, MakeBeginPayload()),
        2);
    AssertResponse(MessageType::Result, 81, 501, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Busy),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);

    fixture.Dispatch(
        MakePacket(MessageType::AbortFrame, 82, 500, kSessionId, {0, 0, 0, 0}),
        3);
    result = fixture.Dispatch(
        MakePacket(
            MessageType::BeginFrame,
            81,
            501,
            kSessionId,
            MakeBeginPayload(),
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)),
        4);

    AssertResponse(MessageType::Result, 81, 501, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Ok),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_UINT32(501, fixture.assembler.ActiveFrameId());
}

void TestEvictedRequestIdCannotExecuteAgain()
{
    Fixture fixture;
    fixture.Dispatch(
        MakePacket(MessageType::HelloRequest, 1, 0, 0, MakeHelloPayload()));
    fixture.Dispatch(
        MakePacket(MessageType::Clear, 2, 0, kSessionId, {0x12, 0x34}));
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());

    for (uint32_t requestId = 3; requestId <= 18; ++requestId)
    {
        fixture.Dispatch(
            MakePacket(MessageType::HealthRequest, requestId, 0, kSessionId, {}));
    }

    const DispatchResult result = fixture.Dispatch(
        MakePacket(
            MessageType::Clear,
            2,
            0,
            kSessionId,
            {0x12, 0x34},
            static_cast<uint8_t>(
                kAcknowledgementRequired | MobaDisplay::Protocol::FlagRetry)));
    AssertResponse(MessageType::Result, 2, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Invalid),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.ClearCallCount());
}

void TestOutOfOrderRequestWithinReplayWindowRemainsValid()
{
    Fixture fixture;
    fixture.Dispatch(
        MakePacket(MessageType::Clear, 105, 0, kSessionId, {0x12, 0x34}));
    const DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::Clear, 104, 0, kSessionId, {0x56, 0x78}));

    AssertResponse(MessageType::Result, 104, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Ok),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_size_t(2, fixture.backend.ClearCallCount());
}

void TestHealthAndRebootExposeOnlySafeSessionState()
{
    Fixture fixture;
    DispatchResult result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 60, 0, kSessionId, {}),
        5000);
    AssertResponse(MessageType::HealthResponse, 60, 0, kSessionId, result, fixture.response);
    const uint8_t* payload = fixture.response.data() + MobaDisplay::Protocol::kHeaderLength;
    TEST_ASSERT_EQUAL_UINT8(0, payload[0]);
    TEST_ASSERT_EQUAL_UINT32(5, ReadUInt32(payload + 4));
    TEST_ASSERT_EQUAL_UINT32(123456, ReadUInt32(payload + 8));

    result = fixture.Dispatch(
        MakePacket(MessageType::SetBrightness, 62, 0, kSessionId, {50}),
        5001);
    AssertResponse(MessageType::Result, 62, 0, kSessionId, result, fixture.response);
    result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 63, 0, kSessionId, {}),
        5002);
    AssertResponse(MessageType::HealthResponse, 63, 0, kSessionId, result, fixture.response);
    payload = fixture.response.data() + MobaDisplay::Protocol::kHeaderLength;
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Unsupported),
        payload[1]);

    result = fixture.Dispatch(
        MakePacket(MessageType::BeginFrame, 64, 300, kSessionId, MakeBeginPayload()),
        5003);
    AssertResponse(MessageType::Result, 64, 300, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Timeout),
        static_cast<uint8_t>(fixture.dispatcher.Tick(6004).code));
    result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 65, 0, kSessionId, {}),
        6005);
    AssertResponse(MessageType::HealthResponse, 65, 0, kSessionId, result, fixture.response);
    payload = fixture.response.data() + MobaDisplay::Protocol::kHeaderLength;
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::Timeout),
        payload[1]);

    constexpr uint32_t newSessionId = 0x55667788U;
    fixture.dispatcher.ResetForReboot(newSessionId);
    TEST_ASSERT_FALSE(fixture.dispatcher.IsNegotiated());
    result = fixture.Dispatch(
        MakePacket(MessageType::HealthRequest, 61, 0, kSessionId, {}),
        6000);
    AssertResponse(MessageType::Result, 61, 0, kSessionId, result, fixture.response);
    TEST_ASSERT_EQUAL_UINT8(
        static_cast<uint8_t>(MobaDisplay::Core::ResultCode::WrongSession),
        fixture.response[MobaDisplay::Protocol::kHeaderLength]);
    TEST_ASSERT_EQUAL_UINT32(newSessionId, fixture.dispatcher.SessionId());
}

void TestTftPresenterPushesNetworkOrderBytesWithoutSwappingAgain()
{
    RecordingTftDriver display;
    alignas(uint16_t) const std::array<uint8_t, 4> frame = {0xF8, 0x00, 0x07, 0xE0};

    MobaDisplay::Esp32::PushRgb565BigEndian(display, 2, 1, frame.data());

    TEST_ASSERT_FALSE(display.swapBytesDuringPush);
    TEST_ASSERT_TRUE(display.swapBytes);
    TEST_ASSERT_EQUAL_UINT8_ARRAY(frame.data(), display.pushedBytes.data(), frame.size());
}
}

int main(int, char**)
{
    UNITY_BEGIN();
    RUN_TEST(TestHelloNegotiatesCapabilitiesWithoutLeakingSessionIntoEnvelope);
    RUN_TEST(TestFullFrameTransactionPresentsExactlyOnce);
    RUN_TEST(TestWrongSessionAndChangedRequestIdReuseFailClosed);
    RUN_TEST(TestIdenticalCommandRetryReturnsCachedResultWithoutExecutingAgain);
    RUN_TEST(TestFreshHelloStartsNewRequestEpochAndRestoresDeviceDatagramLimit);
    RUN_TEST(TestDuplicatedHelloPreservesTheCurrentRequestHistory);
    RUN_TEST(TestOptionalCommandsReflectBackendCapabilities);
    RUN_TEST(TestMalformedPacketsAreDroppedAndUnsupportedVersionIsStructured);
    RUN_TEST(TestRegionAcknowledgementCanBeOmittedOnlyAfterSuccess);
    RUN_TEST(TestIncompleteCompletionRetryReevaluatesAfterMissingRegionArrives);
    RUN_TEST(TestBusyBeginRetryReevaluatesAfterActiveFrameIsAborted);
    RUN_TEST(TestEvictedRequestIdCannotExecuteAgain);
    RUN_TEST(TestOutOfOrderRequestWithinReplayWindowRemainsValid);
    RUN_TEST(TestHealthAndRebootExposeOnlySafeSessionState);
    RUN_TEST(TestTftPresenterPushesNetworkOrderBytesWithoutSwappingAgain);
    return UNITY_END();
}
