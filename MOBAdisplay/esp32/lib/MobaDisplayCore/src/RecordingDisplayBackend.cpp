#include "RecordingDisplayBackend.h"

#include <cstring>

namespace MobaDisplay
{
namespace Core
{
RecordingDisplayBackend::RecordingDisplayBackend(
    const DisplayCapabilities& capabilities,
    uint8_t* presentedFrameBuffer,
    size_t presentedFrameBufferLength) noexcept
    : _capabilities(capabilities),
      _presentedFrameBuffer(presentedFrameBuffer),
      _presentedFrameBufferLength(presentedFrameBufferLength),
      _initializeCallCount(0),
      _presentCallCount(0),
      _presentedFrameByteCount(0),
      _clearCallCount(0),
      _lastClearColor(0),
      _brightnessCallCount(0),
      _lastBrightnessPercentage(0),
      _testPatternCallCount(0),
      _lastTestPattern(TestPattern::Conformance)
{
}

const DisplayCapabilities& RecordingDisplayBackend::GetCapabilities() const noexcept
{
    return _capabilities;
}

DisplayResult RecordingDisplayBackend::Initialize() noexcept
{
    ++_initializeCallCount;
    return MakeResult(ResultCode::Ok);
}

DisplayResult RecordingDisplayBackend::Present(const uint8_t* frameBytes, size_t frameByteCount) noexcept
{
    if (!frameBytes || !_presentedFrameBuffer || frameByteCount == 0 || frameByteCount > _presentedFrameBufferLength)
        return MakeResult(ResultCode::HardwareFailure);

    std::memcpy(_presentedFrameBuffer, frameBytes, frameByteCount);
    _presentedFrameByteCount = frameByteCount;
    ++_presentCallCount;
    return MakeResult(ResultCode::Ok, ResultFlagPresented);
}

DisplayResult RecordingDisplayBackend::Clear(uint16_t rgb565Color) noexcept
{
    if (!Supports(OptionalCommandClear))
        return MakeResult(ResultCode::Unsupported);

    _lastClearColor = rgb565Color;
    ++_clearCallCount;
    return MakeResult(ResultCode::Ok);
}

DisplayResult RecordingDisplayBackend::SetBrightness(uint8_t percentage) noexcept
{
    if (!Supports(OptionalCommandSetBrightness))
        return MakeResult(ResultCode::Unsupported);

    if (percentage > 100)
        return MakeResult(ResultCode::Invalid);

    _lastBrightnessPercentage = percentage;
    ++_brightnessCallCount;
    return MakeResult(ResultCode::Ok);
}

DisplayResult RecordingDisplayBackend::RenderTestPattern(TestPattern pattern) noexcept
{
    if (!Supports(OptionalCommandRenderTestPattern))
        return MakeResult(ResultCode::Unsupported);

    if (pattern != TestPattern::Conformance)
        return MakeResult(ResultCode::Invalid);

    _lastTestPattern = pattern;
    ++_testPatternCallCount;
    return MakeResult(ResultCode::Ok);
}

size_t RecordingDisplayBackend::InitializeCallCount() const noexcept
{
    return _initializeCallCount;
}

size_t RecordingDisplayBackend::PresentCallCount() const noexcept
{
    return _presentCallCount;
}

size_t RecordingDisplayBackend::PresentedFrameByteCount() const noexcept
{
    return _presentedFrameByteCount;
}

size_t RecordingDisplayBackend::ClearCallCount() const noexcept
{
    return _clearCallCount;
}

uint16_t RecordingDisplayBackend::LastClearColor() const noexcept
{
    return _lastClearColor;
}

size_t RecordingDisplayBackend::BrightnessCallCount() const noexcept
{
    return _brightnessCallCount;
}

uint8_t RecordingDisplayBackend::LastBrightnessPercentage() const noexcept
{
    return _lastBrightnessPercentage;
}

size_t RecordingDisplayBackend::TestPatternCallCount() const noexcept
{
    return _testPatternCallCount;
}

TestPattern RecordingDisplayBackend::LastTestPattern() const noexcept
{
    return _lastTestPattern;
}

bool RecordingDisplayBackend::Supports(uint8_t flag) const noexcept
{
    return (_capabilities.optionalCommands & flag) != 0;
}
}
}
