#include <DisplayBackend.h>
#include <FrameAssembler.h>
#include <RecordingDisplayBackend.h>
#include <unity.h>

#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>

using MobaDisplay::Core::DisplayCapabilities;
using MobaDisplay::Core::DisplayResult;
using MobaDisplay::Core::FrameAssembler;
using MobaDisplay::Core::FrameMetadata;
using MobaDisplay::Core::FrameRegion;
using MobaDisplay::Core::IDisplayBackend;
using MobaDisplay::Core::PixelFormat;
using MobaDisplay::Core::RecordingDisplayBackend;
using MobaDisplay::Core::ResultCode;
using MobaDisplay::Core::Rotation;
using MobaDisplay::Core::TestPattern;

void setUp()
{
    // Unity requires this lifecycle hook even when no setup is needed.
}

void tearDown()
{
    // Unity requires this lifecycle hook even when no cleanup is needed.
}

namespace
{
constexpr uint16_t kWidth = 4;
constexpr uint16_t kHeight = 2;
constexpr uint32_t kTimeoutMilliseconds = 100;
constexpr size_t kTrackingByteCount = 2;
constexpr std::array<uint8_t, 16> kFrameBytes = {{
    0xF8, 0x00, 0x07, 0xE0, 0x00, 0x1F, 0xFF, 0xFF,
    0x00, 0x00, 0xFF, 0xFF, 0xF8, 0x1F, 0x07, 0xFF}};

DisplayCapabilities MakeCapabilities(uint8_t optionalCommands, const char* identity)
{
    return {
        kWidth,
        kHeight,
        8,
        MobaDisplay::Core::PixelFormatRgb565BigEndian,
        MobaDisplay::Core::RotationDegrees0,
        optionalCommands,
        static_cast<uint8_t>(MobaDisplay::Core::FrameCapabilityFullFrameStaging
            | MobaDisplay::Core::FrameCapabilityRegionTransfer
            | MobaDisplay::Core::FrameCapabilityAtomicPresentation),
        identity};
}

class MinimalDisplayBackend final : public IDisplayBackend
{
public:
    explicit MinimalDisplayBackend(const DisplayCapabilities& capabilities) noexcept
        : _capabilities(capabilities)
    {
    }

    const DisplayCapabilities& GetCapabilities() const noexcept override
    {
        return _capabilities;
    }

    DisplayResult Initialize() noexcept override
    {
        return MobaDisplay::Core::MakeResult(ResultCode::Ok);
    }

    DisplayResult Present(
        const uint8_t* frameBytes,
        size_t frameByteCount,
        Rotation rotation) noexcept override
    {
        if (!frameBytes || frameByteCount != kFrameBytes.size())
            return MobaDisplay::Core::MakeResult(ResultCode::HardwareFailure);

        _lastPresentedRotation = rotation;
        ++_presentCallCount;
        return MobaDisplay::Core::MakeResult(ResultCode::Ok, MobaDisplay::Core::ResultFlagPresented);
    }

    size_t PresentCallCount() const noexcept
    {
        return _presentCallCount;
    }

    Rotation LastPresentedRotation() const noexcept
    {
        return _lastPresentedRotation;
    }

private:
    DisplayCapabilities _capabilities;
    size_t _presentCallCount = 0;
    Rotation _lastPresentedRotation = Rotation::Degrees0;
};

class RetryableDisplayBackend final : public IDisplayBackend
{
public:
    explicit RetryableDisplayBackend(const DisplayCapabilities& capabilities) noexcept
        : _capabilities(capabilities)
    {
    }

    const DisplayCapabilities& GetCapabilities() const noexcept override
    {
        return _capabilities;
    }

    DisplayResult Initialize() noexcept override
    {
        return MobaDisplay::Core::MakeResult(ResultCode::Ok);
    }

    DisplayResult Present(
        const uint8_t* frameBytes,
        size_t frameByteCount,
        Rotation rotation) noexcept override
    {
        if (!frameBytes || frameByteCount != kFrameBytes.size())
            return MobaDisplay::Core::MakeResult(ResultCode::HardwareFailure);

        _lastPresentedRotation = rotation;
        ++_presentCallCount;
        return _presentCallCount == 1
            ? MobaDisplay::Core::MakeResult(ResultCode::Busy, MobaDisplay::Core::ResultFlagRetryable)
            : MobaDisplay::Core::MakeResult(ResultCode::Ok, MobaDisplay::Core::ResultFlagPresented);
    }

    size_t PresentCallCount() const noexcept
    {
        return _presentCallCount;
    }

    Rotation LastPresentedRotation() const noexcept
    {
        return _lastPresentedRotation;
    }

private:
    DisplayCapabilities _capabilities;
    size_t _presentCallCount = 0;
    Rotation _lastPresentedRotation = Rotation::Degrees0;
};

FrameMetadata MakeMetadata()
{
    return {
        kWidth,
        kHeight,
        PixelFormat::Rgb565BigEndian,
        Rotation::Degrees0,
        kFrameBytes.size(),
        FrameAssembler::ComputeCrc32(kFrameBytes.data(), kFrameBytes.size())};
}

FrameRegion MakeRow(
    uint16_t row,
    uint16_t packetIndex,
    bool finalPacket,
    uint16_t packetCount = 2)
{
    return {
        0,
        row,
        kWidth,
        1,
        static_cast<uint32_t>(row) * kWidth * 2U,
        kFrameBytes.data() + static_cast<size_t>(row) * kWidth * 2U,
        kWidth * 2U,
        packetIndex,
        packetCount,
        finalPacket};
}

void AssertCode(ResultCode expected, const DisplayResult& result)
{
    TEST_ASSERT_TRUE(expected == result.code);
}

struct Fixture
{
    std::array<uint8_t, kFrameBytes.size()> staging{};
    std::array<uint8_t, kTrackingByteCount> coverage{};
    std::array<uint8_t, kFrameBytes.size()> presented{};
    DisplayCapabilities capabilities = MakeCapabilities(
        static_cast<uint8_t>(MobaDisplay::Core::OptionalCommandClear
            | MobaDisplay::Core::OptionalCommandSetBrightness
            | MobaDisplay::Core::OptionalCommandRenderTestPattern),
        "recording-4x2");
    RecordingDisplayBackend backend = RecordingDisplayBackend(capabilities, presented.data(), presented.size());
    FrameAssembler assembler = FrameAssembler(
        backend,
        staging.data(),
        staging.size(),
        coverage.data(),
        coverage.size(),
        kTimeoutMilliseconds);
};

void TestCrc32MatchesGoldenProtocolVector()
{
    constexpr std::array<uint8_t, 8> goldenFrame = {{
        0xF8, 0x00, 0x07, 0xE0, 0x00, 0x1F, 0xFF, 0xFF}};

    TEST_ASSERT_EQUAL_HEX32(
        0xD521D143U,
        FrameAssembler::ComputeCrc32(goldenFrame.data(), goldenFrame.size()));
}

void TestDistinctBackendProfilesHonorTheSameContract()
{
    std::array<uint8_t, kFrameBytes.size()> fullFrame{};
    const DisplayCapabilities fullCapabilities = MakeCapabilities(
        static_cast<uint8_t>(MobaDisplay::Core::OptionalCommandClear
            | MobaDisplay::Core::OptionalCommandSetBrightness
            | MobaDisplay::Core::OptionalCommandRenderTestPattern),
        "full-memory");
    const DisplayCapabilities limitedCapabilities = MakeCapabilities(
        MobaDisplay::Core::OptionalCommandClear,
        "limited-memory");
    RecordingDisplayBackend fullBackend(fullCapabilities, fullFrame.data(), fullFrame.size());
    MinimalDisplayBackend limitedBackend(limitedCapabilities);

    AssertCode(ResultCode::Ok, fullBackend.Initialize());
    AssertCode(ResultCode::Ok, limitedBackend.Initialize());
    AssertCode(ResultCode::Ok, fullBackend.Present(kFrameBytes.data(), kFrameBytes.size(), Rotation::Degrees0));
    AssertCode(ResultCode::Ok, limitedBackend.Present(kFrameBytes.data(), kFrameBytes.size(), Rotation::Degrees0));
    AssertCode(ResultCode::Ok, fullBackend.SetBrightness(60));
    AssertCode(ResultCode::Unsupported, limitedBackend.SetBrightness(60));
    AssertCode(ResultCode::Ok, fullBackend.RenderTestPattern(TestPattern::Conformance));
    AssertCode(ResultCode::Unsupported, limitedBackend.RenderTestPattern(TestPattern::Conformance));
    AssertCode(ResultCode::Ok, fullBackend.Clear(0x1234));
    AssertCode(ResultCode::Unsupported, limitedBackend.Clear(0x5678));

    TEST_ASSERT_EQUAL_STRING("full-memory", fullBackend.GetCapabilities().adapterIdentity);
    TEST_ASSERT_EQUAL_STRING("limited-memory", limitedBackend.GetCapabilities().adapterIdentity);
    TEST_ASSERT_EQUAL_size_t(1, fullBackend.PresentCallCount());
    TEST_ASSERT_EQUAL_size_t(1, limitedBackend.PresentCallCount());
}

void TestBeginFrameValidatesIdentityMetadataAndCapabilities()
{
    Fixture fixture;
    FrameMetadata metadata = MakeMetadata();

    AssertCode(ResultCode::Invalid, fixture.assembler.BeginFrame(0, metadata, 0));
    metadata.width++;
    AssertCode(ResultCode::Invalid, fixture.assembler.BeginFrame(1, metadata, 0));
    metadata = MakeMetadata();
    metadata.rotation = Rotation::Degrees90;
    AssertCode(ResultCode::Invalid, fixture.assembler.BeginFrame(1, metadata, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(1, MakeMetadata(), 0));
    const DisplayResult duplicate = fixture.assembler.BeginFrame(1, MakeMetadata(), 1);
    AssertCode(ResultCode::Ok, duplicate);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagDuplicate, duplicate.flags);
}

void TestOutOfOrderAndDuplicateRegionsPresentExactlyOnce()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();
    const FrameRegion bottomRow = MakeRow(1, 1, true);
    const FrameRegion topRow = MakeRow(0, 0, false);

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(10, metadata, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(10, bottomRow, 1));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(10, bottomRow, 2));
    TEST_ASSERT_EQUAL_UINT32(kWidth, fixture.assembler.CoveredPixelCount());
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(10, topRow, 3));
    TEST_ASSERT_EQUAL_UINT32(kWidth * kHeight, fixture.assembler.CoveredPixelCount());

    const DisplayResult complete = fixture.assembler.CompleteFrame(10, metadata.frameCrc32, 4);
    AssertCode(ResultCode::Ok, complete);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagPresented, complete.flags);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
    TEST_ASSERT_EQUAL_MEMORY(kFrameBytes.data(), fixture.presented.data(), kFrameBytes.size());

    const DisplayResult duplicateComplete = fixture.assembler.CompleteFrame(10, metadata.frameCrc32, 5);
    AssertCode(ResultCode::Ok, duplicateComplete);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagDuplicate, duplicateComplete.flags);
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
}

void TestIncompleteFrameReportsFirstGapWithoutPresentation()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(20, metadata, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(20, MakeRow(0, 0, false), 1));
    const DisplayResult incomplete = fixture.assembler.CompleteFrame(20, metadata.frameCrc32, 2);

    AssertCode(ResultCode::Incomplete, incomplete);
    TEST_ASSERT_EQUAL_UINT32(8, incomplete.firstMissingByteOffset);
    TEST_ASSERT_EQUAL_UINT32(8, incomplete.missingByteCount);
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.PresentCallCount());
    TEST_ASSERT_TRUE(fixture.assembler.HasActiveFrame());

    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(20, MakeRow(1, 1, true), 3));
    AssertCode(ResultCode::Ok, fixture.assembler.CompleteFrame(20, metadata.frameCrc32, 4));
    TEST_ASSERT_EQUAL_size_t(1, fixture.backend.PresentCallCount());
}

void TestConflictingOverlapInvalidatesStaging()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();
    const std::array<uint8_t, 2> conflictingPixel = {{0x00, 0x00}};
    const FrameRegion conflict = {
        0, 0, 1, 1, 0, conflictingPixel.data(), conflictingPixel.size(), 0, 1, true};

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(30, metadata, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(30, MakeRow(0, 0, false), 1));
    AssertCode(ResultCode::Conflict, fixture.assembler.WriteRegion(30, conflict, 2));

    TEST_ASSERT_FALSE(fixture.assembler.HasActiveFrame());
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.PresentCallCount());
    TEST_ASSERT_EQUAL_UINT32(1, fixture.assembler.RejectedFrameCount());
}

void TestAbortAllowsExplicitReplacement()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(40, metadata, 0));
    const DisplayResult busy = fixture.assembler.BeginFrame(41, metadata, 1);
    AssertCode(ResultCode::Busy, busy);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagRetryable, busy.flags);
    AssertCode(ResultCode::Ok, fixture.assembler.AbortFrame(40));
    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(41, metadata, 2));
    TEST_ASSERT_EQUAL_UINT32(41, fixture.assembler.ActiveFrameId());
}

void TestTimeoutAndRebootDiscardStaging()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(50, metadata, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.Tick(kTimeoutMilliseconds - 1));
    AssertCode(ResultCode::Timeout, fixture.assembler.Tick(kTimeoutMilliseconds));
    TEST_ASSERT_FALSE(fixture.assembler.HasActiveFrame());
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.PresentCallCount());

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(51, metadata, 200));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(51, MakeRow(0, 0, false), 201));
    fixture.assembler.ResetForReboot();
    AssertCode(ResultCode::Invalid, fixture.assembler.CompleteFrame(51, metadata.frameCrc32, 202));
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.PresentCallCount());
}

void TestRegionMetadataIsStrictlyValidated()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();
    FrameRegion region = MakeRow(0, 0, false);

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(60, metadata, 0));
    region.frameByteOffset = 2;
    AssertCode(ResultCode::Invalid, fixture.assembler.WriteRegion(60, region, 1));
    region = MakeRow(0, 0, true);
    AssertCode(ResultCode::Invalid, fixture.assembler.WriteRegion(60, region, 2));
    region = MakeRow(0, 0, false);
    region.pixelByteCount--;
    AssertCode(ResultCode::Invalid, fixture.assembler.WriteRegion(60, region, 3));
    TEST_ASSERT_EQUAL_size_t(0, fixture.backend.PresentCallCount());
    TEST_ASSERT_FALSE(fixture.assembler.HasActiveFrame());
}

void TestCompletedFrameIdsCannotBeReopenedOrReplayed()
{
    Fixture fixture;
    const FrameMetadata metadata = MakeMetadata();

    AssertCode(ResultCode::Invalid, fixture.assembler.CompleteFrame(0, 0, 0));
    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(80, metadata, 1));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(80, MakeRow(0, 0, false), 2));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(80, MakeRow(1, 1, true), 3));
    AssertCode(ResultCode::Ok, fixture.assembler.CompleteFrame(80, metadata.frameCrc32, 4));

    const DisplayResult duplicateBegin = fixture.assembler.BeginFrame(80, metadata, 5);
    AssertCode(ResultCode::Ok, duplicateBegin);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagPresented, duplicateBegin.flags);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagDuplicate, duplicateBegin.flags);
    TEST_ASSERT_FALSE(fixture.assembler.HasActiveFrame());

    AssertCode(ResultCode::Ok, fixture.assembler.BeginFrame(81, metadata, 6));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(81, MakeRow(0, 0, false), 7));
    AssertCode(ResultCode::Ok, fixture.assembler.WriteRegion(81, MakeRow(1, 1, true), 8));
    AssertCode(ResultCode::Ok, fixture.assembler.CompleteFrame(81, metadata.frameCrc32, 9));
    AssertCode(ResultCode::Conflict, fixture.assembler.BeginFrame(80, metadata, 10));
    AssertCode(ResultCode::Conflict, fixture.assembler.CompleteFrame(80, metadata.frameCrc32, 11));
    TEST_ASSERT_EQUAL_size_t(2, fixture.backend.PresentCallCount());
}

void TestPacketIndexSetMustBeCompleteAndUnique()
{
    Fixture duplicateFixture;
    const FrameMetadata metadata = MakeMetadata();
    AssertCode(ResultCode::Ok, duplicateFixture.assembler.BeginFrame(90, metadata, 0));
    AssertCode(ResultCode::Ok, duplicateFixture.assembler.WriteRegion(90, MakeRow(0, 0, false), 1));
    AssertCode(ResultCode::Conflict,
        duplicateFixture.assembler.WriteRegion(90, MakeRow(1, 0, false), 2));
    TEST_ASSERT_FALSE(duplicateFixture.assembler.HasActiveFrame());

    Fixture missingFixture;
    AssertCode(ResultCode::Ok, missingFixture.assembler.BeginFrame(91, metadata, 0));
    AssertCode(ResultCode::Ok, missingFixture.assembler.WriteRegion(91, MakeRow(0, 0, false, 3), 1));
    AssertCode(ResultCode::Ok, missingFixture.assembler.WriteRegion(91, MakeRow(1, 2, true, 3), 2));
    AssertCode(ResultCode::Incomplete, missingFixture.assembler.CompleteFrame(91, metadata.frameCrc32, 3));
    TEST_ASSERT_TRUE(missingFixture.assembler.HasActiveFrame());
    TEST_ASSERT_EQUAL_size_t(0, missingFixture.backend.PresentCallCount());

    AssertCode(ResultCode::Ok, missingFixture.assembler.WriteRegion(91, MakeRow(0, 1, false, 3), 4));
    AssertCode(ResultCode::Ok, missingFixture.assembler.CompleteFrame(91, metadata.frameCrc32, 5));
    TEST_ASSERT_EQUAL_size_t(1, missingFixture.backend.PresentCallCount());
}

void TestRotationAndRetryableBackendResultArePreserved()
{
    std::array<uint8_t, kFrameBytes.size()> staging{};
    std::array<uint8_t, kTrackingByteCount> coverage{};
    DisplayCapabilities capabilities = MakeCapabilities(MobaDisplay::Core::OptionalCommandNone, "retrying-memory");
    capabilities.rotations = static_cast<uint8_t>(
        MobaDisplay::Core::RotationDegrees0 | MobaDisplay::Core::RotationDegrees90);
    RetryableDisplayBackend backend(capabilities);
    FrameAssembler assembler(
        backend,
        staging.data(),
        staging.size(),
        coverage.data(),
        coverage.size(),
        kTimeoutMilliseconds);
    FrameMetadata metadata = MakeMetadata();
    metadata.rotation = Rotation::Degrees90;

    AssertCode(ResultCode::Ok, assembler.BeginFrame(100, metadata, 0));
    AssertCode(ResultCode::Ok, assembler.WriteRegion(100, MakeRow(0, 0, false), 1));
    AssertCode(ResultCode::Ok, assembler.WriteRegion(100, MakeRow(1, 1, true), 2));
    const DisplayResult retryable = assembler.CompleteFrame(100, metadata.frameCrc32, 3);
    AssertCode(ResultCode::Busy, retryable);
    TEST_ASSERT_BITS_HIGH(MobaDisplay::Core::ResultFlagRetryable, retryable.flags);
    TEST_ASSERT_TRUE(assembler.HasActiveFrame());
    TEST_ASSERT_TRUE(backend.LastPresentedRotation() == Rotation::Degrees90);

    AssertCode(ResultCode::Ok, assembler.CompleteFrame(100, metadata.frameCrc32, 4));
    TEST_ASSERT_FALSE(assembler.HasActiveFrame());
    TEST_ASSERT_EQUAL_size_t(2, backend.PresentCallCount());
}

void TestZeroTimeoutStillExpiresStaging()
{
    std::array<uint8_t, kFrameBytes.size()> staging{};
    std::array<uint8_t, kTrackingByteCount> coverage{};
    std::array<uint8_t, kFrameBytes.size()> presented{};
    const DisplayCapabilities capabilities =
        MakeCapabilities(MobaDisplay::Core::OptionalCommandNone, "bounded-timeout");
    RecordingDisplayBackend backend(capabilities, presented.data(), presented.size());
    FrameAssembler assembler(
        backend,
        staging.data(),
        staging.size(),
        coverage.data(),
        coverage.size(),
        0);

    AssertCode(ResultCode::Ok, assembler.BeginFrame(110, MakeMetadata(), 0));
    AssertCode(ResultCode::Ok, assembler.Tick(0));
    AssertCode(ResultCode::Timeout, assembler.Tick(1));
    TEST_ASSERT_FALSE(assembler.HasActiveFrame());
}

void TestChecksumAndBackendFailureNeverPublishPartialData()
{
    Fixture checksumFixture;
    const FrameMetadata metadata = MakeMetadata();
    AssertCode(ResultCode::Ok, checksumFixture.assembler.BeginFrame(70, metadata, 0));
    AssertCode(ResultCode::Ok, checksumFixture.assembler.WriteRegion(70, MakeRow(0, 0, false), 1));
    AssertCode(ResultCode::Ok, checksumFixture.assembler.WriteRegion(70, MakeRow(1, 1, true), 2));
    AssertCode(ResultCode::ChecksumMismatch,
        checksumFixture.assembler.CompleteFrame(70, metadata.frameCrc32 ^ 1U, 3));
    TEST_ASSERT_EQUAL_size_t(0, checksumFixture.backend.PresentCallCount());

    std::array<uint8_t, kFrameBytes.size()> staging{};
    std::array<uint8_t, kTrackingByteCount> coverage{};
    const DisplayCapabilities capabilities = MakeCapabilities(MobaDisplay::Core::OptionalCommandNone, "failing-memory");
    RecordingDisplayBackend failingBackend(capabilities, nullptr, 0);
    FrameAssembler failingAssembler(
        failingBackend,
        staging.data(),
        staging.size(),
        coverage.data(),
        coverage.size(),
        kTimeoutMilliseconds);
    AssertCode(ResultCode::Ok, failingAssembler.BeginFrame(71, metadata, 0));
    AssertCode(ResultCode::Ok, failingAssembler.WriteRegion(71, MakeRow(0, 0, false), 1));
    AssertCode(ResultCode::Ok, failingAssembler.WriteRegion(71, MakeRow(1, 1, true), 2));
    AssertCode(ResultCode::HardwareFailure, failingAssembler.CompleteFrame(71, metadata.frameCrc32, 3));
    TEST_ASSERT_FALSE(failingAssembler.HasActiveFrame());
}
}

int main(int, char**)
{
    UNITY_BEGIN();
    RUN_TEST(TestCrc32MatchesGoldenProtocolVector);
    RUN_TEST(TestDistinctBackendProfilesHonorTheSameContract);
    RUN_TEST(TestBeginFrameValidatesIdentityMetadataAndCapabilities);
    RUN_TEST(TestOutOfOrderAndDuplicateRegionsPresentExactlyOnce);
    RUN_TEST(TestIncompleteFrameReportsFirstGapWithoutPresentation);
    RUN_TEST(TestConflictingOverlapInvalidatesStaging);
    RUN_TEST(TestAbortAllowsExplicitReplacement);
    RUN_TEST(TestTimeoutAndRebootDiscardStaging);
    RUN_TEST(TestRegionMetadataIsStrictlyValidated);
    RUN_TEST(TestCompletedFrameIdsCannotBeReopenedOrReplayed);
    RUN_TEST(TestPacketIndexSetMustBeCompleteAndUnique);
    RUN_TEST(TestRotationAndRetryableBackendResultArePreserved);
    RUN_TEST(TestZeroTimeoutStillExpiresStaging);
    RUN_TEST(TestChecksumAndBackendFailureNeverPublishPartialData);
    return UNITY_END();
}
