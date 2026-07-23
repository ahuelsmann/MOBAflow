#pragma once

#include "DisplayBackend.h"

#include <TFT_eSPI.h>

namespace MobaDisplay
{
namespace Esp32
{
class TftEsPiDisplayBackend final : public Core::IDisplayBackend
{
public:
    TftEsPiDisplayBackend(
        TFT_eSPI& display,
        uint16_t width,
        uint16_t height,
        uint16_t maximumRegionPayloadLength) noexcept;

    const Core::DisplayCapabilities& GetCapabilities() const noexcept override;
    Core::DisplayResult Initialize() noexcept override;
    Core::DisplayResult Present(
        const uint8_t* frameBytes,
        size_t frameByteCount,
        Core::Rotation rotation) noexcept override;
    Core::DisplayResult Clear(uint16_t rgb565Color) noexcept override;
    Core::DisplayResult RenderTestPattern(Core::TestPattern pattern) noexcept override;

private:
    void FillBands(
        int32_t y,
        int32_t height,
        const uint16_t* colors,
        size_t colorCount) noexcept;

    TFT_eSPI& _display;
    Core::DisplayCapabilities _capabilities;
    bool _initialized = false;
};
}
}
