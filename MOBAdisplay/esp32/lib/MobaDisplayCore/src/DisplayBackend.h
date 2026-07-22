#pragma once

#include <cstddef>
#include <cstdint>

namespace MobaDisplay
{
namespace Core
{
enum class PixelFormat : uint8_t
{
    Rgb565BigEndian = 1
};

enum class Rotation : uint8_t
{
    Degrees0 = 0,
    Degrees90 = 1,
    Degrees180 = 2,
    Degrees270 = 3
};

enum PixelFormatFlags : uint16_t
{
    PixelFormatNone = 0,
    PixelFormatRgb565BigEndian = 1U << 0
};

enum RotationFlags : uint8_t
{
    RotationNone = 0,
    RotationDegrees0 = 1U << 0,
    RotationDegrees90 = 1U << 1,
    RotationDegrees180 = 1U << 2,
    RotationDegrees270 = 1U << 3
};

enum OptionalCommandFlags : uint8_t
{
    OptionalCommandNone = 0,
    OptionalCommandClear = 1U << 0,
    OptionalCommandSetBrightness = 1U << 1,
    OptionalCommandRenderTestPattern = 1U << 2
};

enum FrameCapabilityFlags : uint8_t
{
    FrameCapabilityNone = 0,
    FrameCapabilityFullFrameStaging = 1U << 0,
    FrameCapabilityRegionTransfer = 1U << 1,
    FrameCapabilityAtomicPresentation = 1U << 2
};

enum class TestPattern : uint8_t
{
    Conformance = 1
};

enum class ResultCode : uint8_t
{
    Ok = 0x00,
    Invalid = 0x01,
    Unsupported = 0x02,
    UnsupportedVersion = 0x03,
    Busy = 0x04,
    Incomplete = 0x05,
    ChecksumMismatch = 0x06,
    Timeout = 0x07,
    HardwareFailure = 0x08,
    WrongSession = 0x09,
    Conflict = 0x0A
};

enum ResultFlags : uint8_t
{
    ResultFlagNone = 0,
    ResultFlagPresented = 1U << 0,
    ResultFlagDuplicate = 1U << 1,
    ResultFlagRetryable = 1U << 2
};

struct DisplayResult
{
    ResultCode code;
    uint8_t flags;
    uint32_t firstMissingByteOffset;
    uint32_t missingByteCount;
};

inline DisplayResult MakeResult(
    ResultCode code,
    uint8_t flags = ResultFlagNone,
    uint32_t firstMissingByteOffset = 0,
    uint32_t missingByteCount = 0) noexcept
{
    return {code, flags, firstMissingByteOffset, missingByteCount};
}

struct DisplayCapabilities
{
    uint16_t width;
    uint16_t height;
    uint16_t maximumRegionPayloadLength;
    uint16_t pixelFormats;
    uint8_t rotations;
    uint8_t optionalCommands;
    uint8_t frameCapabilities;
    const char* adapterIdentity;
};

class IDisplayBackend
{
public:
    virtual ~IDisplayBackend() = default;

    virtual const DisplayCapabilities& GetCapabilities() const noexcept = 0;
    virtual DisplayResult Initialize() noexcept = 0;
    virtual DisplayResult Present(const uint8_t* frameBytes, size_t frameByteCount) noexcept = 0;

    virtual DisplayResult Clear(uint16_t) noexcept
    {
        return MakeResult(ResultCode::Unsupported);
    }

    virtual DisplayResult SetBrightness(uint8_t) noexcept
    {
        return MakeResult(ResultCode::Unsupported);
    }

    virtual DisplayResult RenderTestPattern(TestPattern) noexcept
    {
        return MakeResult(ResultCode::Unsupported);
    }
};
}
}
