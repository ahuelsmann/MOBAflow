#pragma once

#include "DisplayBackend.h"

namespace MobaDisplay
{
namespace Core
{
class RecordingDisplayBackend final : public IDisplayBackend
{
public:
    RecordingDisplayBackend(
        const DisplayCapabilities& capabilities,
        uint8_t* presentedFrameBuffer,
        size_t presentedFrameBufferLength) noexcept;

    const DisplayCapabilities& GetCapabilities() const noexcept override;
    DisplayResult Initialize() noexcept override;
    DisplayResult Present(
        const uint8_t* frameBytes,
        size_t frameByteCount,
        Rotation rotation) noexcept override;
    DisplayResult Clear(uint16_t rgb565Color) noexcept override;
    DisplayResult SetBrightness(uint8_t percentage) noexcept override;
    DisplayResult RenderTestPattern(TestPattern pattern) noexcept override;

    size_t InitializeCallCount() const noexcept;
    size_t PresentCallCount() const noexcept;
    size_t PresentedFrameByteCount() const noexcept;
    Rotation LastPresentedRotation() const noexcept;
    size_t ClearCallCount() const noexcept;
    uint16_t LastClearColor() const noexcept;
    size_t BrightnessCallCount() const noexcept;
    uint8_t LastBrightnessPercentage() const noexcept;
    size_t TestPatternCallCount() const noexcept;
    TestPattern LastTestPattern() const noexcept;

private:
    bool Supports(uint8_t flag) const noexcept;

    DisplayCapabilities _capabilities;
    uint8_t* _presentedFrameBuffer;
    size_t _presentedFrameBufferLength;
    size_t _initializeCallCount = 0;
    size_t _presentCallCount = 0;
    size_t _presentedFrameByteCount = 0;
    Rotation _lastPresentedRotation = Rotation::Degrees0;
    size_t _clearCallCount = 0;
    uint16_t _lastClearColor = 0;
    size_t _brightnessCallCount = 0;
    uint8_t _lastBrightnessPercentage = 0;
    size_t _testPatternCallCount = 0;
    TestPattern _lastTestPattern = TestPattern::Conformance;
};
}
}
