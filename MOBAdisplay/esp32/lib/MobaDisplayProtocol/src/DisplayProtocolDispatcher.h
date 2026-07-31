#pragma once

#include "DisplayBackend.h"
#include "FrameAssembler.h"

#include <cstddef>
#include <cstdint>

namespace MobaDisplay
{
namespace Protocol
{
constexpr uint32_t kMagic = 0x4D4F4241U;
constexpr uint16_t kHeaderLength = 32;
constexpr uint16_t kDefaultMaximumDatagramLength = 1232;
constexpr uint8_t kCurrentMajorVersion = 1;
constexpr uint8_t kCurrentMinorVersion = 0;

enum class MessageType : uint8_t
{
    HelloRequest = 0x01,
    CapabilitiesResponse = 0x02,
    HealthRequest = 0x03,
    HealthResponse = 0x04,
    BeginFrame = 0x10,
    FrameRegion = 0x11,
    CompleteFrame = 0x12,
    AbortFrame = 0x13,
    Clear = 0x20,
    SetBrightness = 0x21,
    RenderTestPattern = 0x22,
    Result = 0x7F
};

constexpr uint8_t FlagNone = 0;
constexpr uint8_t FlagResponse = 1U << 0;
constexpr uint8_t FlagAcknowledgementRequired = 1U << 1;
constexpr uint8_t FlagRetry = 1U << 2;
constexpr uint8_t FlagFinalPacket = 1U << 3;

struct DeviceDiagnostics
{
    uint32_t uptimeSeconds;
    uint32_t freeHeapBytes;
};

struct DispatchResult
{
    bool recognized;
    bool hasResponse;
    size_t responseLength;
};

class DisplayProtocolDispatcher final
{
public:
    DisplayProtocolDispatcher(
        Core::FrameAssembler& frameAssembler,
        Core::IDisplayBackend& displayBackend,
        uint32_t sessionId,
        uint16_t maximumDatagramLength,
        const char* deviceIdentity,
        const char* firmwareVersion) noexcept;

    DispatchResult Dispatch(
        const uint8_t* datagram,
        size_t datagramLength,
        uint32_t nowMilliseconds,
        const DeviceDiagnostics& diagnostics,
        uint8_t* responseBuffer,
        size_t responseBufferLength) noexcept;

    Core::DisplayResult Tick(uint32_t nowMilliseconds) noexcept;
    void ResetForReboot(uint32_t sessionId) noexcept;
    uint32_t SessionId() const noexcept;
    bool IsNegotiated() const noexcept;

private:
    struct PacketHeader
    {
        uint8_t majorVersion;
        uint8_t minorVersion;
        MessageType messageType;
        uint8_t flags;
        uint16_t payloadLength;
        uint32_t requestId;
        uint32_t frameId;
        uint32_t sessionId;
        uint16_t packetIndex;
        uint16_t packetCount;
        uint32_t payloadCrc32;
    };

    struct RequestFingerprint
    {
        uint32_t requestId;
        uint32_t frameId;
        uint32_t sessionId;
        uint32_t payloadCrc32;
        uint16_t packetIndex;
        uint16_t packetCount;
        MessageType messageType;
        uint8_t logicalFlags;
        Core::DisplayResult cachedResult;
        bool hasCachedResult;
        bool occupied;
    };

    enum class RequestFingerprintState : uint8_t
    {
        New,
        Duplicate,
        Conflict
    };

    static constexpr size_t kRequestHistoryLength = 16;
    static constexpr size_t kEvictedRequestHistoryLength = 64;

    bool TryDecodePacket(
        const uint8_t* datagram,
        size_t datagramLength,
        PacketHeader* header,
        const uint8_t** payload) const noexcept;
    RequestFingerprintState InspectRequestFingerprint(
        const PacketHeader& header,
        RequestFingerprint** fingerprint,
        bool recordWhenNew) noexcept;
    void ResetRequestHistory() noexcept;
    DispatchResult HandleHello(
        const PacketHeader& header,
        const uint8_t* payload,
        uint8_t* responseBuffer,
        size_t responseBufferLength) noexcept;
    DispatchResult HandleNegotiatedRequest(
        const PacketHeader& header,
        const uint8_t* payload,
        uint32_t nowMilliseconds,
        const DeviceDiagnostics& diagnostics,
        uint8_t* responseBuffer,
        size_t responseBufferLength) noexcept;
    Core::DisplayResult DispatchFrameOrCommand(
        const PacketHeader& header,
        const uint8_t* payload,
        uint32_t nowMilliseconds) noexcept;
    Core::DisplayResult ResolveTrackedResult(
        RequestFingerprint& fingerprint,
        bool duplicate,
        const Core::DisplayResult& currentResult) noexcept;
    DispatchResult WriteCapabilitiesResponse(
        const PacketHeader& request,
        uint8_t* responseBuffer,
        size_t responseBufferLength) const noexcept;
    DispatchResult WriteHealthResponse(
        const PacketHeader& request,
        const DeviceDiagnostics& diagnostics,
        uint8_t* responseBuffer,
        size_t responseBufferLength) const noexcept;
    DispatchResult WriteResultResponse(
        const PacketHeader& request,
        const Core::DisplayResult& result,
        uint8_t* responseBuffer,
        size_t responseBufferLength) const noexcept;
    DispatchResult WriteResponse(
        const PacketHeader& request,
        MessageType responseType,
        uint32_t responseSessionId,
        const uint8_t* payload,
        size_t payloadLength,
        uint8_t* responseBuffer,
        size_t responseBufferLength) const noexcept;

    Core::FrameAssembler& _frameAssembler;
    Core::IDisplayBackend& _displayBackend;
    uint32_t _sessionId;
    uint16_t _deviceMaximumDatagramLength;
    uint16_t _negotiatedMaximumDatagramLength;
    const char* _deviceIdentity;
    const char* _firmwareVersion;
    Core::DisplayResult _lastOperationResult = Core::MakeResult(Core::ResultCode::Ok);
    bool _isNegotiated = false;
    RequestFingerprint _requestHistory[kRequestHistoryLength]{};
    RequestFingerprint _evictedRequestHistory[kEvictedRequestHistoryLength]{};
    size_t _nextRequestHistoryIndex = 0;
    size_t _nextEvictedRequestHistoryIndex = 0;
};
}
}
