#include "TftEsPiDisplayBackend.h"

namespace MobaDisplay
{
namespace Esp32
{
namespace
{
constexpr char kAdapterIdentity[] = "tft-espi-st7789";
}

TftEsPiDisplayBackend::TftEsPiDisplayBackend(
    TFT_eSPI& display,
    uint16_t width,
    uint16_t height,
    uint16_t maximumRegionPayloadLength) noexcept
    : _display(display),
      _capabilities{
          width,
          height,
          maximumRegionPayloadLength,
          Core::PixelFormatRgb565BigEndian,
          Core::RotationDegrees0,
          static_cast<uint8_t>(
              Core::OptionalCommandClear | Core::OptionalCommandRenderTestPattern),
          static_cast<uint8_t>(
              Core::FrameCapabilityFullFrameStaging
              | Core::FrameCapabilityRegionTransfer
              | Core::FrameCapabilityAtomicPresentation),
          kAdapterIdentity}
{
}

const Core::DisplayCapabilities& TftEsPiDisplayBackend::GetCapabilities() const noexcept
{
    return _capabilities;
}

Core::DisplayResult TftEsPiDisplayBackend::Initialize() noexcept
{
    _display.init();
    _display.setRotation(0);
    _display.setSwapBytes(true);
    _initialized = true;
    return Core::MakeResult(Core::ResultCode::Ok);
}

Core::DisplayResult TftEsPiDisplayBackend::Present(
    const uint8_t* frameBytes,
    size_t frameByteCount,
    Core::Rotation rotation) noexcept
{
    const size_t expectedByteCount =
        static_cast<size_t>(_capabilities.width) * _capabilities.height * 2U;
    if (!_initialized || !frameBytes || frameByteCount != expectedByteCount
        || rotation != Core::Rotation::Degrees0)
    {
        return Core::MakeResult(Core::ResultCode::HardwareFailure);
    }

    _display.pushImage(
        0,
        0,
        _capabilities.width,
        _capabilities.height,
        reinterpret_cast<const uint16_t*>(frameBytes));
    return Core::MakeResult(Core::ResultCode::Ok, Core::ResultFlagPresented);
}

Core::DisplayResult TftEsPiDisplayBackend::Clear(uint16_t rgb565Color) noexcept
{
    if (!_initialized)
        return Core::MakeResult(Core::ResultCode::HardwareFailure);

    _display.fillScreen(rgb565Color);
    return Core::MakeResult(Core::ResultCode::Ok);
}

Core::DisplayResult TftEsPiDisplayBackend::RenderTestPattern(
    Core::TestPattern pattern) noexcept
{
    if (!_initialized)
        return Core::MakeResult(Core::ResultCode::HardwareFailure);

    if (pattern != Core::TestPattern::Conformance)
        return Core::MakeResult(Core::ResultCode::Unsupported);

    constexpr uint16_t topColors[] = {0xF800, 0x07E0, 0x001F};
    constexpr uint16_t bottomColors[] = {0xFFFF, 0x0000};
    const int32_t topHeight = (_capabilities.height + 1) / 2;
    FillBands(0, topHeight, topColors, sizeof(topColors) / sizeof(topColors[0]));
    FillBands(
        topHeight,
        _capabilities.height - topHeight,
        bottomColors,
        sizeof(bottomColors) / sizeof(bottomColors[0]));
    return Core::MakeResult(Core::ResultCode::Ok);
}

void TftEsPiDisplayBackend::FillBands(
    int32_t y,
    int32_t height,
    const uint16_t* colors,
    size_t colorCount) noexcept
{
    int32_t x = 0;
    const int32_t baseWidth = _capabilities.width / static_cast<int32_t>(colorCount);
    const int32_t remainder = _capabilities.width % static_cast<int32_t>(colorCount);
    for (size_t index = 0; index < colorCount; ++index)
    {
        const int32_t width = baseWidth
            + (static_cast<int32_t>(index) < remainder ? 1 : 0);
        _display.fillRect(x, y, width, height, colors[index]);
        x += width;
    }
}
}
}
