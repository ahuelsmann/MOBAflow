#pragma once

#include <cstdint>

// ESP32-S3-DevKitC-1 uses GPIO0 for the BOOT button. Override these values in
// the board-specific build configuration after verifying the enclosure wiring.
#ifndef MOBAFLOW_BOOT_BUTTON_GPIO
#define MOBAFLOW_BOOT_BUTTON_GPIO 0
#endif

#ifndef MOBAFLOW_BOOT_BUTTON_ACTIVE_LOW
#define MOBAFLOW_BOOT_BUTTON_ACTIVE_LOW 1
#endif

namespace MobaDisplay::Board
{
constexpr uint8_t kBootButtonPin = static_cast<uint8_t>(MOBAFLOW_BOOT_BUTTON_GPIO);
constexpr bool kBootButtonActiveLow = MOBAFLOW_BOOT_BUTTON_ACTIVE_LOW != 0;

static_assert(kBootButtonPin <= 48, "The ESP32-S3 GPIO must be a valid board input.");
}
