#include "FrameAssembler.h"

#include <cstring>

namespace MobaDisplay
{
namespace Core
{
namespace
{
constexpr uint32_t kCrc32Polynomial = 0xEDB88320U;

uint8_t RotationFlag(Rotation rotation) noexcept
{
    switch (rotation)
    {
    case Rotation::Degrees0:
        return RotationDegrees0;
    case Rotation::Degrees90:
        return RotationDegrees90;
    case Rotation::Degrees180:
        return RotationDegrees180;
    case Rotation::Degrees270:
        return RotationDegrees270;
    }

    return RotationNone;
}
}

FrameAssembler::FrameAssembler(
    IDisplayBackend& backend,
    uint8_t* stagingBuffer,
    size_t stagingBufferLength,
    uint8_t* coverageBuffer,
    size_t coverageBufferLength,
    uint32_t inactivityTimeoutMilliseconds) noexcept
    : _backend(backend),
      _stagingBuffer(stagingBuffer),
      _stagingBufferLength(stagingBufferLength),
      _coverageBuffer(coverageBuffer),
      _coverageBufferLength(coverageBufferLength),
      _inactivityTimeoutMilliseconds(inactivityTimeoutMilliseconds == 0 ? 1 : inactivityTimeoutMilliseconds)
{
}

size_t FrameAssembler::FrameByteCount(uint16_t width, uint16_t height) noexcept
{
    const uint64_t byteCount = static_cast<uint64_t>(width) * height * 2U;
    return byteCount > SIZE_MAX ? 0 : static_cast<size_t>(byteCount);
}

size_t FrameAssembler::CoverageByteCount(uint16_t width, uint16_t height) noexcept
{
    const uint64_t pixelCount = static_cast<uint64_t>(width) * height;
    const uint64_t byteCount = (pixelCount + 7U) / 8U;
    return byteCount > SIZE_MAX ? 0 : static_cast<size_t>(byteCount);
}

size_t FrameAssembler::TrackingByteCount(uint16_t width, uint16_t height) noexcept
{
    const size_t coverageBytes = CoverageByteCount(width, height);
    return coverageBytes > SIZE_MAX / 2U ? 0 : coverageBytes * 2U;
}

uint32_t FrameAssembler::ComputeCrc32(const uint8_t* bytes, size_t byteCount) noexcept
{
    if (!bytes && byteCount != 0)
        return 0;

    uint32_t crc = UINT32_MAX;
    for (size_t index = 0; index < byteCount; ++index)
    {
        crc ^= bytes[index];
        for (uint8_t bit = 0; bit < 8; ++bit)
            crc = (crc & 1U) != 0U ? (crc >> 1U) ^ kCrc32Polynomial : crc >> 1U;
    }

    return ~crc;
}

DisplayResult FrameAssembler::BeginFrame(
    uint32_t frameId,
    const FrameMetadata& metadata,
    uint32_t nowMilliseconds) noexcept
{
    ExpireIfNeeded(nowMilliseconds);
    if (frameId == 0 || !IsMetadataSupported(metadata))
        return Record(MakeResult(ResultCode::Invalid));

    if (_hasCompletedFrame)
    {
        if (frameId == _lastCompletedFrameId)
        {
            return Record(MetadataMatches(metadata, _lastCompletedMetadata)
                ? MakeResult(ResultCode::Ok, ResultFlagPresented | ResultFlagDuplicate)
                : MakeResult(ResultCode::Conflict));
        }

        if (!IsNewerFrameId(frameId, _lastCompletedFrameId))
            return Record(MakeResult(ResultCode::Conflict));
    }

    if (_hasActiveFrame)
    {
        if (_activeFrameId != frameId)
            return Record(MakeResult(ResultCode::Busy, ResultFlagRetryable));

        if (!MetadataMatches(metadata, _metadata))
            return Record(MakeResult(ResultCode::Conflict));

        _lastActivityMilliseconds = nowMilliseconds;
        return Record(MakeResult(ResultCode::Ok, ResultFlagDuplicate));
    }

    std::memset(_coverageBuffer, 0, TrackingByteCount(metadata.width, metadata.height));
    _metadata = metadata;
    _activeFrameId = frameId;
    _lastActivityMilliseconds = nowMilliseconds;
    _coveredPixelCount = 0;
    _receivedPacketCount = 0;
    _packetCount = 0;
    _hasActiveFrame = true;
    return Record(MakeResult(ResultCode::Ok));
}

DisplayResult FrameAssembler::WriteRegion(
    uint32_t frameId,
    const FrameRegion& region,
    uint32_t nowMilliseconds) noexcept
{
    if (ExpireIfNeeded(nowMilliseconds))
        return _lastResult;

    if (!_hasActiveFrame || frameId != _activeFrameId)
        return Record(MakeResult(ResultCode::Invalid));

    if (!ValidateRegion(region))
    {
        ++_rejectedFrameCount;
        DiscardActiveFrame();
        return Record(MakeResult(ResultCode::Invalid));
    }

    if (_packetCount != 0 && _packetCount != region.packetCount)
    {
        ++_rejectedFrameCount;
        DiscardActiveFrame();
        return Record(MakeResult(ResultCode::Conflict));
    }

    const bool duplicatePacket = IsPacketReceived(region.packetIndex);
    if (duplicatePacket && RegionContainsUncoveredPixel(region))
    {
        ++_rejectedFrameCount;
        DiscardActiveFrame();
        return Record(MakeResult(ResultCode::Conflict));
    }

    const size_t bytesPerRegionRow = static_cast<size_t>(region.width) * 2U;
    for (uint16_t row = 0; row < region.height; ++row)
    {
        const size_t destinationPixel =
            static_cast<size_t>(region.y + row) * _metadata.width + region.x;
        const size_t sourceOffset = static_cast<size_t>(row) * bytesPerRegionRow;
        for (uint16_t column = 0; column < region.width; ++column)
        {
            const size_t pixelIndex = destinationPixel + column;
            if (!IsPixelCovered(pixelIndex))
                continue;

            const size_t destinationOffset = pixelIndex * 2U;
            const size_t pixelSourceOffset = sourceOffset + static_cast<size_t>(column) * 2U;
            if (std::memcmp(_stagingBuffer + destinationOffset, region.pixelBytes + pixelSourceOffset, 2U) != 0)
            {
                ++_rejectedFrameCount;
                DiscardActiveFrame();
                return Record(MakeResult(ResultCode::Conflict));
            }
        }
    }

    for (uint16_t row = 0; row < region.height; ++row)
    {
        const size_t destinationPixel =
            static_cast<size_t>(region.y + row) * _metadata.width + region.x;
        const size_t destinationOffset = destinationPixel * 2U;
        const size_t sourceOffset = static_cast<size_t>(row) * bytesPerRegionRow;
        std::memcpy(_stagingBuffer + destinationOffset, region.pixelBytes + sourceOffset, bytesPerRegionRow);
        for (uint16_t column = 0; column < region.width; ++column)
        {
            const size_t pixelIndex = destinationPixel + column;
            if (!IsPixelCovered(pixelIndex))
            {
                MarkPixelCovered(pixelIndex);
                ++_coveredPixelCount;
            }
        }
    }

    _packetCount = region.packetCount;
    if (!duplicatePacket)
    {
        MarkPacketReceived(region.packetIndex);
        ++_receivedPacketCount;
    }

    _lastActivityMilliseconds = nowMilliseconds;
    return Record(MakeResult(ResultCode::Ok));
}

DisplayResult FrameAssembler::CompleteFrame(
    uint32_t frameId,
    uint32_t frameCrc32,
    uint32_t nowMilliseconds) noexcept
{
    if (frameId == 0)
        return Record(MakeResult(ResultCode::Invalid));

    if (_hasCompletedFrame && frameId == _lastCompletedFrameId)
    {
        return Record(frameCrc32 == _lastCompletedFrameCrc32
            ? MakeResult(ResultCode::Ok, ResultFlagPresented | ResultFlagDuplicate)
            : MakeResult(ResultCode::Conflict));
    }

    if (_hasCompletedFrame && !IsNewerFrameId(frameId, _lastCompletedFrameId))
        return Record(MakeResult(ResultCode::Conflict));

    if (ExpireIfNeeded(nowMilliseconds))
        return _lastResult;

    if (!_hasActiveFrame || frameId != _activeFrameId)
        return Record(MakeResult(ResultCode::Invalid));

    uint32_t missingByteOffset = 0;
    uint32_t missingByteCount = 0;
    if (TryGetMissingRange(&missingByteOffset, &missingByteCount))
    {
        ++_rejectedFrameCount;
        return Record(MakeResult(ResultCode::Incomplete, ResultFlagNone, missingByteOffset, missingByteCount));
    }

    if (_packetCount == 0 || _receivedPacketCount != _packetCount)
    {
        ++_rejectedFrameCount;
        return Record(MakeResult(ResultCode::Incomplete));
    }

    if (frameCrc32 != _metadata.frameCrc32
        || ComputeCrc32(_stagingBuffer, _metadata.expectedPixelByteCount) != frameCrc32)
    {
        ++_rejectedFrameCount;
        DiscardActiveFrame();
        return Record(MakeResult(ResultCode::ChecksumMismatch));
    }

    const DisplayResult presentResult =
        _backend.Present(_stagingBuffer, _metadata.expectedPixelByteCount, _metadata.rotation);
    if (presentResult.code != ResultCode::Ok)
    {
        if ((presentResult.flags & ResultFlagRetryable) != 0)
        {
            _lastActivityMilliseconds = nowMilliseconds;
            return Record(presentResult);
        }

        ++_rejectedFrameCount;
        DiscardActiveFrame();
        return Record(presentResult);
    }

    _lastCompletedFrameId = frameId;
    _lastCompletedFrameCrc32 = frameCrc32;
    _lastCompletedMetadata = _metadata;
    _hasCompletedFrame = true;
    ++_acceptedFrameCount;
    DiscardActiveFrame();
    return Record(MakeResult(ResultCode::Ok, ResultFlagPresented));
}

DisplayResult FrameAssembler::AbortFrame(uint32_t frameId) noexcept
{
    if (_hasActiveFrame && frameId == _activeFrameId)
        DiscardActiveFrame();

    return Record(MakeResult(ResultCode::Ok));
}

DisplayResult FrameAssembler::Tick(uint32_t nowMilliseconds) noexcept
{
    return ExpireIfNeeded(nowMilliseconds) ? _lastResult : MakeResult(ResultCode::Ok);
}

void FrameAssembler::ResetForReboot() noexcept
{
    DiscardActiveFrame();
    _lastCompletedFrameId = 0;
    _lastCompletedFrameCrc32 = 0;
    _lastCompletedMetadata = {};
    _hasCompletedFrame = false;
    _lastResult = MakeResult(ResultCode::Ok);
}

bool FrameAssembler::HasActiveFrame() const noexcept
{
    return _hasActiveFrame;
}

uint32_t FrameAssembler::ActiveFrameId() const noexcept
{
    return _hasActiveFrame ? _activeFrameId : 0;
}

uint32_t FrameAssembler::CoveredPixelCount() const noexcept
{
    return _coveredPixelCount;
}

uint32_t FrameAssembler::AcceptedFrameCount() const noexcept
{
    return _acceptedFrameCount;
}

uint32_t FrameAssembler::RejectedFrameCount() const noexcept
{
    return _rejectedFrameCount;
}

uint32_t FrameAssembler::LastCompletedFrameId() const noexcept
{
    return _hasCompletedFrame ? _lastCompletedFrameId : 0;
}

const DisplayResult& FrameAssembler::LastResult() const noexcept
{
    return _lastResult;
}

bool FrameAssembler::IsMetadataSupported(const FrameMetadata& metadata) const noexcept
{
    const DisplayCapabilities& capabilities = _backend.GetCapabilities();
    const size_t frameBytes = FrameByteCount(metadata.width, metadata.height);
    const size_t trackingBytes = TrackingByteCount(metadata.width, metadata.height);
    return metadata.width == capabilities.width
        && metadata.height == capabilities.height
        && metadata.pixelFormat == PixelFormat::Rgb565BigEndian
        && (capabilities.pixelFormats & PixelFormatRgb565BigEndian) != 0
        && (capabilities.rotations & RotationFlag(metadata.rotation)) != 0
        && frameBytes != 0
        && trackingBytes != 0
        && frameBytes == metadata.expectedPixelByteCount
        && frameBytes <= _stagingBufferLength
        && trackingBytes <= _coverageBufferLength
        && _stagingBuffer
        && _coverageBuffer;
}

bool FrameAssembler::MetadataMatches(const FrameMetadata& left, const FrameMetadata& right) noexcept
{
    return left.width == right.width
        && left.height == right.height
        && left.pixelFormat == right.pixelFormat
        && left.rotation == right.rotation
        && left.expectedPixelByteCount == right.expectedPixelByteCount
        && left.frameCrc32 == right.frameCrc32;
}

bool FrameAssembler::IsNewerFrameId(uint32_t candidate, uint32_t reference) noexcept
{
    const uint32_t difference = candidate - reference;
    return difference != 0 && difference < 0x80000000U;
}

bool FrameAssembler::ValidateRegion(const FrameRegion& region) const noexcept
{
    const uint32_t pixelCount = static_cast<uint32_t>(_metadata.width) * _metadata.height;
    if (!region.pixelBytes || region.width == 0 || region.height == 0
        || region.packetCount == 0 || region.packetIndex >= region.packetCount
        || region.packetCount > pixelCount
        || region.finalPacket != (region.packetIndex + 1U == region.packetCount))
    {
        return false;
    }

    const uint32_t right = static_cast<uint32_t>(region.x) + region.width;
    const uint32_t bottom = static_cast<uint32_t>(region.y) + region.height;
    const size_t expectedBytes = FrameByteCount(region.width, region.height);
    const uint32_t expectedOffset =
        (static_cast<uint32_t>(region.y) * _metadata.width + region.x) * 2U;
    return right <= _metadata.width
        && bottom <= _metadata.height
        && expectedBytes == region.pixelByteCount
        && expectedBytes <= _backend.GetCapabilities().maximumRegionPayloadLength
        && region.frameByteOffset == expectedOffset;
}

bool FrameAssembler::IsPixelCovered(size_t pixelIndex) const noexcept
{
    return (_coverageBuffer[pixelIndex / 8U] & static_cast<uint8_t>(1U << (pixelIndex % 8U))) != 0;
}

void FrameAssembler::MarkPixelCovered(size_t pixelIndex) noexcept
{
    _coverageBuffer[pixelIndex / 8U] |= static_cast<uint8_t>(1U << (pixelIndex % 8U));
}

bool FrameAssembler::IsPacketReceived(uint16_t packetIndex) const noexcept
{
    const size_t packetCoverageOffset = CoverageByteCount(_metadata.width, _metadata.height);
    return (_coverageBuffer[packetCoverageOffset + packetIndex / 8U]
        & static_cast<uint8_t>(1U << (packetIndex % 8U))) != 0;
}

void FrameAssembler::MarkPacketReceived(uint16_t packetIndex) noexcept
{
    const size_t packetCoverageOffset = CoverageByteCount(_metadata.width, _metadata.height);
    _coverageBuffer[packetCoverageOffset + packetIndex / 8U] |=
        static_cast<uint8_t>(1U << (packetIndex % 8U));
}

bool FrameAssembler::RegionContainsUncoveredPixel(const FrameRegion& region) const noexcept
{
    for (uint16_t row = 0; row < region.height; ++row)
    {
        const size_t firstPixel =
            static_cast<size_t>(region.y + row) * _metadata.width + region.x;
        for (uint16_t column = 0; column < region.width; ++column)
        {
            if (!IsPixelCovered(firstPixel + column))
                return true;
        }
    }

    return false;
}

bool FrameAssembler::TryGetMissingRange(uint32_t* byteOffset, uint32_t* byteCount) const noexcept
{
    const size_t pixelCount = static_cast<size_t>(_metadata.width) * _metadata.height;
    size_t firstMissingPixel = 0;
    while (firstMissingPixel < pixelCount && IsPixelCovered(firstMissingPixel))
        ++firstMissingPixel;

    if (firstMissingPixel == pixelCount)
        return false;

    size_t endPixel = firstMissingPixel;
    while (endPixel < pixelCount && !IsPixelCovered(endPixel))
        ++endPixel;

    *byteOffset = static_cast<uint32_t>(firstMissingPixel * 2U);
    *byteCount = static_cast<uint32_t>((endPixel - firstMissingPixel) * 2U);
    return true;
}

bool FrameAssembler::ExpireIfNeeded(uint32_t nowMilliseconds) noexcept
{
    if (!_hasActiveFrame
        || nowMilliseconds - _lastActivityMilliseconds < _inactivityTimeoutMilliseconds)
    {
        return false;
    }

    ++_rejectedFrameCount;
    DiscardActiveFrame();
    _lastResult = MakeResult(ResultCode::Timeout);
    return true;
}

DisplayResult FrameAssembler::Record(const DisplayResult& result) noexcept
{
    _lastResult = result;
    return result;
}

void FrameAssembler::DiscardActiveFrame() noexcept
{
    _hasActiveFrame = false;
    _activeFrameId = 0;
    _lastActivityMilliseconds = 0;
    _coveredPixelCount = 0;
    _receivedPacketCount = 0;
    _packetCount = 0;
}
}
}
