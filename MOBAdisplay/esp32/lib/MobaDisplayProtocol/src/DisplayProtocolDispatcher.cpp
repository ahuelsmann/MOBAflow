#include "DisplayProtocolDispatcher.h"

#include <cstring>

namespace MobaDisplay
{
namespace Protocol
{
namespace
{
constexpr uint32_t kCrc32Polynomial = 0xEDB88320U;
constexpr size_t kCapabilitiesFixedLength = 20;
constexpr size_t kFrameRegionMetadataLength = 16;
constexpr size_t kMaximumCapabilitiesPayloadLength = 512;
constexpr uint32_t kRetryAfterMilliseconds = 25;

uint16_t ReadUInt16(const uint8_t* bytes) noexcept
{
    return static_cast<uint16_t>(
        (static_cast<uint16_t>(bytes[0]) << 8U) | bytes[1]);
}

uint32_t ReadUInt32(const uint8_t* bytes) noexcept
{
    return (static_cast<uint32_t>(bytes[0]) << 24U)
        | (static_cast<uint32_t>(bytes[1]) << 16U)
        | (static_cast<uint32_t>(bytes[2]) << 8U)
        | bytes[3];
}

void WriteUInt16(uint8_t* bytes, uint16_t value) noexcept
{
    bytes[0] = static_cast<uint8_t>(value >> 8U);
    bytes[1] = static_cast<uint8_t>(value);
}

void WriteUInt32(uint8_t* bytes, uint32_t value) noexcept
{
    bytes[0] = static_cast<uint8_t>(value >> 24U);
    bytes[1] = static_cast<uint8_t>(value >> 16U);
    bytes[2] = static_cast<uint8_t>(value >> 8U);
    bytes[3] = static_cast<uint8_t>(value);
}

uint32_t ComputeCrc32(const uint8_t* bytes, size_t byteCount) noexcept
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

bool IsKnownMessageType(MessageType messageType) noexcept
{
    switch (messageType)
    {
    case MessageType::HelloRequest:
    case MessageType::CapabilitiesResponse:
    case MessageType::HealthRequest:
    case MessageType::HealthResponse:
    case MessageType::BeginFrame:
    case MessageType::FrameRegion:
    case MessageType::CompleteFrame:
    case MessageType::AbortFrame:
    case MessageType::Clear:
    case MessageType::SetBrightness:
    case MessageType::RenderTestPattern:
    case MessageType::Result:
        return true;
    }

    return false;
}

bool BytesAreZero(const uint8_t* bytes, size_t byteCount) noexcept
{
    for (size_t index = 0; index < byteCount; ++index)
    {
        if (bytes[index] != 0)
            return false;
    }

    return true;
}

bool IsFrameMessage(MessageType messageType) noexcept
{
    return messageType == MessageType::BeginFrame
        || messageType == MessageType::FrameRegion
        || messageType == MessageType::CompleteFrame
        || messageType == MessageType::AbortFrame;
}

bool IsCommandMessage(MessageType messageType) noexcept
{
    return messageType == MessageType::Clear
        || messageType == MessageType::SetBrightness
        || messageType == MessageType::RenderTestPattern;
}

bool IsVersionSupported(
    uint8_t minimumMajor,
    uint8_t minimumMinor,
    uint8_t maximumMajor,
    uint8_t maximumMinor,
    uint8_t currentMajor,
    uint8_t currentMinor) noexcept
{
    return minimumMajor == currentMajor
        && maximumMajor == currentMajor
        && minimumMinor <= currentMinor
        && maximumMinor >= currentMinor;
}

Core::Rotation ToRotation(uint8_t value) noexcept
{
    switch (value)
    {
    case 1:
        return Core::Rotation::Degrees90;
    case 2:
        return Core::Rotation::Degrees180;
    case 3:
        return Core::Rotation::Degrees270;
    default:
        return Core::Rotation::Degrees0;
    }
}

DispatchResult NoResponse(bool recognized = true) noexcept
{
    return {recognized, false, 0};
}

bool TryWriteString(
    uint8_t* destination,
    size_t destinationLength,
    size_t* offset,
    const char* value) noexcept
{
    if (!destination || !offset || !value)
        return false;

    size_t length = 0;
    while (length <= UINT8_MAX && value[length] != '\0')
        ++length;
    if (length == 0 || length > UINT8_MAX || *offset > destinationLength
        || destinationLength - *offset < length + 1U)
    {
        return false;
    }

    destination[(*offset)++] = static_cast<uint8_t>(length);
    std::memcpy(destination + *offset, value, length);
    *offset += length;
    return true;
}
}

DisplayProtocolDispatcher::DisplayProtocolDispatcher(
    Core::FrameAssembler& frameAssembler,
    Core::IDisplayBackend& displayBackend,
    uint32_t sessionId,
    uint16_t maximumDatagramLength,
    const char* deviceIdentity,
    const char* firmwareVersion) noexcept
    : _frameAssembler(frameAssembler),
      _displayBackend(displayBackend),
      _sessionId(sessionId == 0 ? 1 : sessionId),
      _deviceMaximumDatagramLength(
          maximumDatagramLength < kHeaderLength ? kHeaderLength : maximumDatagramLength),
      _negotiatedMaximumDatagramLength(
          maximumDatagramLength < kHeaderLength ? kHeaderLength : maximumDatagramLength),
      _deviceIdentity(deviceIdentity),
      _firmwareVersion(firmwareVersion)
{
}

DispatchResult DisplayProtocolDispatcher::Dispatch(
    const uint8_t* datagram,
    size_t datagramLength,
    uint32_t nowMilliseconds,
    const DeviceDiagnostics& diagnostics,
    uint8_t* responseBuffer,
    size_t responseBufferLength) noexcept
{
    PacketHeader header{};
    const uint8_t* payload = nullptr;
    if (!TryDecodePacket(datagram, datagramLength, &header, &payload))
    {
        const bool hasMagic = datagram && datagramLength >= sizeof(uint32_t)
            && ReadUInt32(datagram) == kMagic;
        return NoResponse(hasMagic);
    }

    if ((header.flags & FlagResponse) != 0)
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::Invalid),
            responseBuffer,
            responseBufferLength);

    if (header.majorVersion != kCurrentMajorVersion
        || header.minorVersion > kCurrentMinorVersion)
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::UnsupportedVersion),
            responseBuffer,
            responseBufferLength);
    }

    if (header.messageType == MessageType::HelloRequest)
        return HandleHello(header, payload, responseBuffer, responseBufferLength);

    return HandleNegotiatedRequest(
        header,
        payload,
        nowMilliseconds,
        diagnostics,
        responseBuffer,
        responseBufferLength);
}

Core::DisplayResult DisplayProtocolDispatcher::Tick(uint32_t nowMilliseconds) noexcept
{
    const Core::DisplayResult result = _frameAssembler.Tick(nowMilliseconds);
    if (result.code != Core::ResultCode::Ok)
        _lastOperationResult = result;
    return result;
}

void DisplayProtocolDispatcher::ResetForReboot(uint32_t sessionId) noexcept
{
    _sessionId = sessionId == 0 ? 1 : sessionId;
    _frameAssembler.ResetForReboot();
    _negotiatedMaximumDatagramLength = _deviceMaximumDatagramLength;
    _lastOperationResult = Core::MakeResult(Core::ResultCode::Ok);
    _isNegotiated = false;
    ResetRequestHistory();
}

uint32_t DisplayProtocolDispatcher::SessionId() const noexcept
{
    return _sessionId;
}

bool DisplayProtocolDispatcher::IsNegotiated() const noexcept
{
    return _isNegotiated;
}

bool DisplayProtocolDispatcher::TryDecodePacket(
    const uint8_t* datagram,
    size_t datagramLength,
    PacketHeader* header,
    const uint8_t** payload) const noexcept
{
    const uint16_t maximumDatagramLength = _isNegotiated
        ? _negotiatedMaximumDatagramLength
        : _deviceMaximumDatagramLength;
    if (!datagram || !header || !payload || datagramLength < kHeaderLength
        || datagramLength > maximumDatagramLength || ReadUInt32(datagram) != kMagic
        || ReadUInt16(datagram + 8) != kHeaderLength)
    {
        return false;
    }

    const uint16_t payloadLength = ReadUInt16(datagram + 10);
    if (datagramLength != static_cast<size_t>(kHeaderLength) + payloadLength)
        return false;

    header->majorVersion = datagram[4];
    header->minorVersion = datagram[5];
    header->messageType = static_cast<MessageType>(datagram[6]);
    header->flags = datagram[7];
    header->payloadLength = payloadLength;
    header->requestId = ReadUInt32(datagram + 12);
    header->frameId = ReadUInt32(datagram + 16);
    header->sessionId = ReadUInt32(datagram + 20);
    header->packetIndex = ReadUInt16(datagram + 24);
    header->packetCount = ReadUInt16(datagram + 26);
    header->payloadCrc32 = ReadUInt32(datagram + 28);
    *payload = datagram + kHeaderLength;

    constexpr uint8_t supportedFlags = FlagResponse
        | FlagAcknowledgementRequired
        | FlagRetry
        | FlagFinalPacket;
    return header->majorVersion != 0
        && header->requestId != 0
        && IsKnownMessageType(header->messageType)
        && (header->flags & static_cast<uint8_t>(~supportedFlags)) == 0
        && header->packetCount != 0
        && header->packetIndex < header->packetCount
        && ComputeCrc32(*payload, payloadLength) == header->payloadCrc32;
}

DisplayProtocolDispatcher::RequestFingerprintState
DisplayProtocolDispatcher::InspectRequestFingerprint(
    const PacketHeader& header,
    RequestFingerprint** fingerprint,
    bool recordWhenNew) noexcept
{
    if (!fingerprint)
        return RequestFingerprintState::Conflict;

    const uint8_t logicalFlags =
        header.flags & static_cast<uint8_t>(FlagAcknowledgementRequired | FlagFinalPacket);
    for (RequestFingerprint& previous : _requestHistory)
    {
        if (!previous.occupied || previous.requestId != header.requestId)
            continue;

        *fingerprint = &previous;
        const bool matches = previous.frameId == header.frameId
            && previous.sessionId == header.sessionId
            && previous.payloadCrc32 == header.payloadCrc32
            && previous.packetIndex == header.packetIndex
            && previous.packetCount == header.packetCount
            && previous.messageType == header.messageType
            && previous.logicalFlags == logicalFlags;
        return matches
            ? RequestFingerprintState::Duplicate
            : RequestFingerprintState::Conflict;
    }

    for (const RequestFingerprint& previous : _evictedRequestHistory)
    {
        if (previous.occupied && previous.requestId == header.requestId)
        {
            *fingerprint = nullptr;
            return RequestFingerprintState::Conflict;
        }
    }

    if (!recordWhenNew)
    {
        *fingerprint = nullptr;
        return RequestFingerprintState::New;
    }

    RequestFingerprint& newFingerprint = _requestHistory[_nextRequestHistoryIndex];
    if (newFingerprint.occupied)
    {
        _evictedRequestHistory[_nextEvictedRequestHistoryIndex] = newFingerprint;
        _nextEvictedRequestHistoryIndex =
            (_nextEvictedRequestHistoryIndex + 1U) % kEvictedRequestHistoryLength;
    }

    newFingerprint = {
        header.requestId,
        header.frameId,
        header.sessionId,
        header.payloadCrc32,
        header.packetIndex,
        header.packetCount,
        header.messageType,
        logicalFlags,
        Core::MakeResult(Core::ResultCode::Ok),
        false,
        true};
    *fingerprint = &newFingerprint;
    _nextRequestHistoryIndex = (_nextRequestHistoryIndex + 1U) % kRequestHistoryLength;
    return RequestFingerprintState::New;
}

void DisplayProtocolDispatcher::ResetRequestHistory() noexcept
{
    _requestHistory.fill(RequestFingerprint{});
    _evictedRequestHistory.fill(RequestFingerprint{});
    _nextRequestHistoryIndex = 0;
    _nextEvictedRequestHistoryIndex = 0;
}

DispatchResult DisplayProtocolDispatcher::HandleHello(
    const PacketHeader& header,
    const uint8_t* payload,
    uint8_t* responseBuffer,
    size_t responseBufferLength) noexcept
{
    const bool validEnvelope = header.frameId == 0
        && header.sessionId == 0
        && header.packetIndex == 0
        && header.packetCount == 1
        && (header.flags & FlagFinalPacket) == 0;
    const bool validPayload = header.payloadLength == 8
        && BytesAreZero(payload + 6, 2)
        && ReadUInt16(payload + 4) > kHeaderLength;
    if (!validEnvelope || !validPayload)
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::Invalid),
            responseBuffer,
            responseBufferLength);
    }

    const uint8_t minimumMajor = payload[0];
    const uint8_t minimumMinor = payload[1];
    const uint8_t maximumMajor = payload[2];
    const uint8_t maximumMinor = payload[3];
    const bool versionSupported = IsVersionSupported(
        minimumMajor,
        minimumMinor,
        maximumMajor,
        maximumMinor,
        kCurrentMajorVersion,
        kCurrentMinorVersion);
    if (!versionSupported)
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::UnsupportedVersion),
            responseBuffer,
            responseBufferLength);
    }

    const uint16_t hostMaximumDatagramLength = ReadUInt16(payload + 4);
    RequestFingerprint* fingerprint = nullptr;
    const RequestFingerprintState fingerprintState =
        InspectRequestFingerprint(header, &fingerprint, false);
    if (fingerprintState == RequestFingerprintState::Conflict
        || ((header.flags & FlagRetry) != 0
            && fingerprintState != RequestFingerprintState::Duplicate))
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(
                fingerprintState == RequestFingerprintState::Conflict
                    ? Core::ResultCode::Conflict
                    : Core::ResultCode::Invalid),
            responseBuffer,
            responseBufferLength);
    }

    if (fingerprintState == RequestFingerprintState::Duplicate)
        return WriteCapabilitiesResponse(header, responseBuffer, responseBufferLength);

    const uint16_t previousMaximumDatagramLength = _negotiatedMaximumDatagramLength;
    _negotiatedMaximumDatagramLength =
        hostMaximumDatagramLength < _deviceMaximumDatagramLength
        ? hostMaximumDatagramLength
        : _deviceMaximumDatagramLength;
    const DispatchResult response =
        WriteCapabilitiesResponse(header, responseBuffer, responseBufferLength);
    if (!response.hasResponse)
    {
        _negotiatedMaximumDatagramLength = previousMaximumDatagramLength;
        return response;
    }

    _frameAssembler.ResetFrameEpoch();
    ResetRequestHistory();
    InspectRequestFingerprint(header, &fingerprint, true);

    _isNegotiated = true;
    return response;
}

DispatchResult DisplayProtocolDispatcher::HandleNegotiatedRequest(
    const PacketHeader& header,
    const uint8_t* payload,
    uint32_t nowMilliseconds,
    const DeviceDiagnostics& diagnostics,
    uint8_t* responseBuffer,
    size_t responseBufferLength) noexcept
{
    if (header.sessionId != _sessionId)
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::WrongSession),
            responseBuffer,
            responseBufferLength);
    }

    RequestFingerprint* fingerprint = nullptr;
    const RequestFingerprintState fingerprintState =
        InspectRequestFingerprint(header, &fingerprint, true);
    if (fingerprintState == RequestFingerprintState::Conflict)
    {
        return WriteResultResponse(
            header,
            Core::MakeResult(Core::ResultCode::Conflict),
            responseBuffer,
            responseBufferLength);
    }

    const bool duplicate = fingerprintState == RequestFingerprintState::Duplicate;
    if (header.messageType == MessageType::HealthRequest)
    {
        const bool valid = header.frameId == 0
            && header.payloadLength == 0
            && header.packetIndex == 0
            && header.packetCount == 1
            && (header.flags & FlagFinalPacket) == 0;
        if (valid)
            return WriteHealthResponse(header, diagnostics, responseBuffer, responseBufferLength);

        const Core::DisplayResult result = ResolveTrackedResult(
            *fingerprint,
            duplicate,
            Core::MakeResult(Core::ResultCode::Invalid));
        return WriteResultResponse(header, result, responseBuffer, responseBufferLength);
    }

    const bool expectedFrameId = IsFrameMessage(header.messageType)
        ? header.frameId != 0
        : IsCommandMessage(header.messageType) && header.frameId == 0;
    if (!expectedFrameId)
    {
        const Core::DisplayResult result = ResolveTrackedResult(
            *fingerprint,
            duplicate,
            Core::MakeResult(Core::ResultCode::Invalid));
        return WriteResultResponse(
            header,
            result,
            responseBuffer,
            responseBufferLength);
    }

    const Core::DisplayResult result = ResolveTrackedResult(
        *fingerprint,
        duplicate,
        duplicate && fingerprint->hasCachedResult
            ? fingerprint->cachedResult
            : DispatchFrameOrCommand(header, payload, nowMilliseconds));
    if (header.messageType == MessageType::FrameRegion
        && result.code == Core::ResultCode::Ok
        && (header.flags & FlagAcknowledgementRequired) == 0)
    {
        return NoResponse();
    }

    return WriteResultResponse(header, result, responseBuffer, responseBufferLength);
}

Core::DisplayResult DisplayProtocolDispatcher::DispatchFrameOrCommand(
    const PacketHeader& header,
    const uint8_t* payload,
    uint32_t nowMilliseconds) noexcept
{
    const bool isSinglePacket = header.packetIndex == 0
        && header.packetCount == 1
        && (header.flags & FlagFinalPacket) == 0;
    switch (header.messageType)
    {
    case MessageType::BeginFrame:
        if (!isSinglePacket || header.payloadLength != 16
            || !BytesAreZero(payload + 6, 2) || payload[4] != 1 || payload[5] > 3)
        {
            return Core::MakeResult(Core::ResultCode::Invalid);
        }
        return _frameAssembler.BeginFrame(
            header.frameId,
            {
                ReadUInt16(payload),
                ReadUInt16(payload + 2),
                Core::PixelFormat::Rgb565BigEndian,
                ToRotation(payload[5]),
                ReadUInt32(payload + 8),
                ReadUInt32(payload + 12)},
            nowMilliseconds);

    case MessageType::FrameRegion:
    {
        if (header.payloadLength < kFrameRegionMetadataLength)
            return Core::MakeResult(Core::ResultCode::Invalid);

        const uint32_t pixelByteCount = ReadUInt32(payload + 12);
        if (pixelByteCount != header.payloadLength - kFrameRegionMetadataLength)
            return Core::MakeResult(Core::ResultCode::Invalid);

        return _frameAssembler.WriteRegion(
            header.frameId,
            {
                ReadUInt16(payload),
                ReadUInt16(payload + 2),
                ReadUInt16(payload + 4),
                ReadUInt16(payload + 6),
                ReadUInt32(payload + 8),
                payload + kFrameRegionMetadataLength,
                pixelByteCount,
                header.packetIndex,
                header.packetCount,
                (header.flags & FlagFinalPacket) != 0},
            nowMilliseconds);
    }

    case MessageType::CompleteFrame:
        return isSinglePacket && header.payloadLength == 4
            ? _frameAssembler.CompleteFrame(
                header.frameId,
                ReadUInt32(payload),
                nowMilliseconds)
            : Core::MakeResult(Core::ResultCode::Invalid);

    case MessageType::AbortFrame:
        return isSinglePacket && header.payloadLength == 4
                && payload[0] <= 2 && BytesAreZero(payload + 1, 3)
            ? _frameAssembler.AbortFrame(header.frameId)
            : Core::MakeResult(Core::ResultCode::Invalid);

    case MessageType::Clear:
        return isSinglePacket && header.payloadLength == 2
            ? _displayBackend.Clear(ReadUInt16(payload))
            : Core::MakeResult(Core::ResultCode::Invalid);

    case MessageType::SetBrightness:
        return isSinglePacket && header.payloadLength == 1 && payload[0] <= 100
            ? _displayBackend.SetBrightness(payload[0])
            : Core::MakeResult(Core::ResultCode::Invalid);

    case MessageType::RenderTestPattern:
        return isSinglePacket && header.payloadLength == 4
                && payload[0] == 1 && BytesAreZero(payload + 1, 3)
            ? _displayBackend.RenderTestPattern(Core::TestPattern::Conformance)
            : Core::MakeResult(Core::ResultCode::Invalid);

    default:
        return Core::MakeResult(Core::ResultCode::Unsupported);
    }
}

Core::DisplayResult DisplayProtocolDispatcher::ResolveTrackedResult(
    RequestFingerprint& fingerprint,
    bool duplicate,
    const Core::DisplayResult& currentResult) noexcept
{
    if (duplicate && fingerprint.hasCachedResult)
    {
        Core::DisplayResult result = fingerprint.cachedResult;
        result.flags |= Core::ResultFlagDuplicate;
        return result;
    }

    _lastOperationResult = currentResult;
    const bool transientResult = currentResult.code == Core::ResultCode::Busy
        || currentResult.code == Core::ResultCode::Incomplete
        || (currentResult.flags & Core::ResultFlagRetryable) != 0;
    if (!transientResult)
    {
        fingerprint.cachedResult = currentResult;
        fingerprint.hasCachedResult = true;
    }
    return currentResult;
}

DispatchResult DisplayProtocolDispatcher::WriteCapabilitiesResponse(
    const PacketHeader& request,
    uint8_t* responseBuffer,
    size_t responseBufferLength) const noexcept
{
    uint8_t payload[kMaximumCapabilitiesPayloadLength] = {};
    const Core::DisplayCapabilities& capabilities = _displayBackend.GetCapabilities();
    payload[0] = kCurrentMajorVersion;
    payload[1] = kCurrentMinorVersion;
    WriteUInt16(payload + 2, capabilities.width);
    WriteUInt16(payload + 4, capabilities.height);
    WriteUInt16(payload + 6, _negotiatedMaximumDatagramLength);
    const uint16_t envelopeLimitedRegionLength =
        _negotiatedMaximumDatagramLength > kHeaderLength + kFrameRegionMetadataLength
        ? static_cast<uint16_t>(
            _negotiatedMaximumDatagramLength - kHeaderLength - kFrameRegionMetadataLength)
        : 0;
    const uint16_t maximumRegionPayloadLength =
        capabilities.maximumRegionPayloadLength < envelopeLimitedRegionLength
        ? capabilities.maximumRegionPayloadLength
        : envelopeLimitedRegionLength;
    WriteUInt16(payload + 8, maximumRegionPayloadLength);
    WriteUInt16(payload + 10, capabilities.pixelFormats);
    payload[12] = capabilities.rotations;
    payload[13] = capabilities.optionalCommands;
    payload[14] = capabilities.frameCapabilities;
    payload[15] = 0;
    WriteUInt32(payload + 16, _sessionId);

    size_t payloadLength = kCapabilitiesFixedLength;
    if (maximumRegionPayloadLength == 0
        || !TryWriteString(payload, sizeof(payload), &payloadLength, _deviceIdentity)
        || !TryWriteString(payload, sizeof(payload), &payloadLength, _firmwareVersion)
        || !TryWriteString(
            payload,
            sizeof(payload),
            &payloadLength,
            capabilities.adapterIdentity))
    {
        return NoResponse();
    }

    return WriteResponse(
        request,
        MessageType::CapabilitiesResponse,
        0,
        payload,
        payloadLength,
        responseBuffer,
        responseBufferLength);
}

DispatchResult DisplayProtocolDispatcher::WriteHealthResponse(
    const PacketHeader& request,
    const DeviceDiagnostics& diagnostics,
    uint8_t* responseBuffer,
    size_t responseBufferLength) const noexcept
{
    uint8_t payload[24] = {};
    uint8_t healthState = 0;
    if (_frameAssembler.HasActiveFrame())
        healthState = 1;
    else if (_lastOperationResult.code == Core::ResultCode::HardwareFailure)
        healthState = 2;
    payload[0] = healthState;
    payload[1] = static_cast<uint8_t>(_lastOperationResult.code);
    WriteUInt32(payload + 4, diagnostics.uptimeSeconds);
    WriteUInt32(payload + 8, diagnostics.freeHeapBytes);
    WriteUInt32(payload + 12, _frameAssembler.AcceptedFrameCount());
    WriteUInt32(payload + 16, _frameAssembler.RejectedFrameCount());
    WriteUInt32(payload + 20, _frameAssembler.LastCompletedFrameId());
    return WriteResponse(
        request,
        MessageType::HealthResponse,
        _sessionId,
        payload,
        sizeof(payload),
        responseBuffer,
        responseBufferLength);
}

DispatchResult DisplayProtocolDispatcher::WriteResultResponse(
    const PacketHeader& request,
    const Core::DisplayResult& result,
    uint8_t* responseBuffer,
    size_t responseBufferLength) const noexcept
{
    uint8_t payload[16] = {};
    payload[0] = static_cast<uint8_t>(result.code);
    payload[1] = result.flags;
    uint32_t missingByteCount = result.missingByteCount;
    if (result.code == Core::ResultCode::Incomplete && missingByteCount == 0)
    {
        WriteUInt16(payload + 2, 1);
        missingByteCount = 1;
    }

    if ((result.flags & Core::ResultFlagRetryable) != 0
        || result.code == Core::ResultCode::Busy)
    {
        WriteUInt32(payload + 4, kRetryAfterMilliseconds);
    }

    WriteUInt32(payload + 8, result.firstMissingByteOffset);
    WriteUInt32(payload + 12, missingByteCount);
    const uint32_t responseSessionId =
        request.messageType == MessageType::HelloRequest ? 0 : request.sessionId;
    return WriteResponse(
        request,
        MessageType::Result,
        responseSessionId,
        payload,
        sizeof(payload),
        responseBuffer,
        responseBufferLength);
}

DispatchResult DisplayProtocolDispatcher::WriteResponse(
    const PacketHeader& request,
    MessageType responseType,
    uint32_t responseSessionId,
    const uint8_t* payload,
    size_t payloadLength,
    uint8_t* responseBuffer,
    size_t responseBufferLength) const noexcept
{
    const size_t datagramLength = kHeaderLength + payloadLength;
    if (!responseBuffer || (!payload && payloadLength != 0)
        || payloadLength > UINT16_MAX
        || datagramLength > responseBufferLength
        || datagramLength > _negotiatedMaximumDatagramLength)
    {
        return NoResponse();
    }

    WriteUInt32(responseBuffer, kMagic);
    responseBuffer[4] = kCurrentMajorVersion;
    responseBuffer[5] = kCurrentMinorVersion;
    responseBuffer[6] = static_cast<uint8_t>(responseType);
    responseBuffer[7] = FlagResponse;
    WriteUInt16(responseBuffer + 8, kHeaderLength);
    WriteUInt16(responseBuffer + 10, static_cast<uint16_t>(payloadLength));
    WriteUInt32(responseBuffer + 12, request.requestId);
    WriteUInt32(responseBuffer + 16, request.frameId);
    WriteUInt32(responseBuffer + 20, responseSessionId);
    WriteUInt16(responseBuffer + 24, 0);
    WriteUInt16(responseBuffer + 26, 1);
    WriteUInt32(responseBuffer + 28, ComputeCrc32(payload, payloadLength));
    if (payloadLength != 0)
        std::memcpy(responseBuffer + kHeaderLength, payload, payloadLength);
    return {true, true, datagramLength};
}
}
}
