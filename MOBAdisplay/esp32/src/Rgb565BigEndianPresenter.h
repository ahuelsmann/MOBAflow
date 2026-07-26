#pragma once

#include <cstdint>

namespace MobaDisplay
{
namespace Esp32
{
template <typename TDisplay>
void PushRgb565BigEndian(
    TDisplay& display,
    uint16_t width,
    uint16_t height,
    const uint8_t* frameBytes) noexcept
{
    const bool previousSwapBytes = display.getSwapBytes();
    display.setSwapBytes(false);
    display.pushImage(
        0,
        0,
        width,
        height,
        reinterpret_cast<uint16_t*>(const_cast<uint8_t*>(frameBytes)));
    display.setSwapBytes(previousSwapBytes);
}
}
}
