#pragma once

#include "DisplayBackend.h"

namespace MobaDisplay
{
namespace Core
{
struct FrameMetadata
{
    uint16_t width;
    uint16_t height;
    PixelFormat pixelFormat;
    Rotation rotation;
    uint32_t expectedPixelByteCount;
    uint32_t frameCrc32;
};

struct FrameRegion
{
    uint16_t x;
    uint16_t y;
    uint16_t width;
    uint16_t height;
    uint32_t frameByteOffset;
    const uint8_t* pixelBytes;
    size_t pixelByteCount;
    uint16_t packetIndex;
    uint16_t packetCount;
    bool finalPacket;
};

class FrameAssembler final
{
public:
    FrameAssembler(
        IDisplayBackend& backend,
        uint8_t* stagingBuffer,
        size_t stagingBufferLength,
        uint8_t* coverageBuffer,
        size_t coverageBufferLength,
        uint32_t inactivityTimeoutMilliseconds) noexcept;

    static size_t FrameByteCount(uint16_t width, uint16_t height) noexcept;
    static size_t CoverageByteCount(uint16_t width, uint16_t height) noexcept;
    static size_t TrackingByteCount(uint16_t width, uint16_t height) noexcept;
    static uint32_t ComputeCrc32(const uint8_t* bytes, size_t byteCount) noexcept;

    DisplayResult BeginFrame(uint32_t frameId, const FrameMetadata& metadata, uint32_t nowMilliseconds) noexcept;
    DisplayResult WriteRegion(uint32_t frameId, const FrameRegion& region, uint32_t nowMilliseconds) noexcept;
    DisplayResult CompleteFrame(uint32_t frameId, uint32_t frameCrc32, uint32_t nowMilliseconds) noexcept;
    DisplayResult AbortFrame(uint32_t frameId) noexcept;
    DisplayResult Tick(uint32_t nowMilliseconds) noexcept;
    void ResetForReboot() noexcept;

    bool HasActiveFrame() const noexcept;
    uint32_t ActiveFrameId() const noexcept;
    uint32_t CoveredPixelCount() const noexcept;
    uint32_t AcceptedFrameCount() const noexcept;
    uint32_t RejectedFrameCount() const noexcept;
    const DisplayResult& LastResult() const noexcept;

private:
    bool IsMetadataSupported(const FrameMetadata& metadata) const noexcept;
    static bool MetadataMatches(const FrameMetadata& left, const FrameMetadata& right) noexcept;
    static bool IsNewerFrameId(uint32_t candidate, uint32_t reference) noexcept;
    bool ValidateRegion(const FrameRegion& region) const noexcept;
    bool IsPixelCovered(size_t pixelIndex) const noexcept;
    void MarkPixelCovered(size_t pixelIndex) noexcept;
    bool IsPacketReceived(uint16_t packetIndex) const noexcept;
    void MarkPacketReceived(uint16_t packetIndex) noexcept;
    bool RegionContainsUncoveredPixel(const FrameRegion& region) const noexcept;
    bool TryGetMissingRange(uint32_t* byteOffset, uint32_t* byteCount) const noexcept;
    bool ExpireIfNeeded(uint32_t nowMilliseconds) noexcept;
    DisplayResult Record(const DisplayResult& result) noexcept;
    void DiscardActiveFrame() noexcept;

    IDisplayBackend& _backend;
    uint8_t* _stagingBuffer;
    size_t _stagingBufferLength;
    uint8_t* _coverageBuffer;
    size_t _coverageBufferLength;
    uint32_t _inactivityTimeoutMilliseconds;
    FrameMetadata _metadata{};
    FrameMetadata _lastCompletedMetadata{};
    uint32_t _activeFrameId = 0;
    uint32_t _lastActivityMilliseconds = 0;
    uint32_t _coveredPixelCount = 0;
    uint32_t _receivedPacketCount = 0;
    uint16_t _packetCount = 0;
    uint32_t _lastCompletedFrameId = 0;
    uint32_t _lastCompletedFrameCrc32 = 0;
    uint32_t _acceptedFrameCount = 0;
    uint32_t _rejectedFrameCount = 0;
    DisplayResult _lastResult = MakeResult(ResultCode::Ok);
    bool _hasActiveFrame = false;
    bool _hasCompletedFrame = false;
};
}
}
