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
    DisplayResult Present(const uint8_t* frameBytes, size_t frameByteCount) noexcept override;
    DisplayResult Clear(uint16_t rgb565Color) noexcept override;
    DisplayResult SetBrightness(uint8_t percentage) noexcept override;
    DisplayResult RenderTestPattern(TestPattern pattern) noexcept override;

    size_t InitializeCallCount() const noexcept;
    size_t PresentCallCount() const noexcept;
    size_t PresentedFrameByteCount() const noexcept;
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
    size_t _initializeCallCount;
    size_t _presentCallCount;
    size_t _presentedFrameByteCount;
    size_t _clearCallCount;
    uint16_t _lastClearColor;
    size_t _brightnessCallCount;
    uint8_t _lastBrightnessPercentage;
    size_t _testPatternCallCount;
    TestPattern _lastTestPattern;
};
}
}
