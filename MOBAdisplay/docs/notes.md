# MOBAdisplay hardware notes

The current PlatformIO target is an ESP32-S3 with an ST7789 display configured
for 240x280 pixels. The authoritative pin and driver settings are in
`MOBAdisplay/esp32/lib/TFT_eSPI/User_Setup.h`.

Current SPI mapping:

| Signal | ESP32-S3 GPIO |
| --- | --- |
| MOSI / DIN | 11 |
| SCLK / CLK | 12 |
| CS | 10 |
| DC | 9 |
| RST | 13 |

The current target assumes a display with a fixed backlight connection; no
`TFT_BL` pin is configured. Do not reuse the older GPIO 23/18/5/2/4 wiring notes
with this firmware without also changing `User_Setup.h`.

## Module and flash layout

The minimum supported module is the ESP32-S3 N16R8 (16 MB flash, 8 MB octal
PSRAM). `platformio.ini` and `sdkconfig.defaults` configure 16 MB flash and the
custom partition table `MOBAdisplay/esp32/partitions.csv`:

| Partition | Offset | Size |
| --- | --- | --- |
| `nvs` | `0x9000` | 24 KB |
| `phy_init` | `0xf000` | 4 KB |
| `factory` (app) | `0x10000` | 4 MB |

`nvs` and `phy_init` keep the ESP-IDF default offsets, so provisioned Wi-Fi
credentials survive reflashing. The flash after the app partition stays
unallocated for later use.

After changing the flash size or partition table, delete the generated
`MOBAdisplay/esp32/sdkconfig.esp32s3` and `MOBAdisplay/esp32/.pio` before the
next build, because PlatformIO keeps the old values otherwise. Flash the
complete bundle, not only the app: `bootloader.bin` at `0x0`, `partitions.bin`
at `0x8000` and `firmware.bin` at `0x10000`. The CI artifact contains these
three files with `flash-offsets.txt` and `flash-bundle.sha256`.

## Current-hardware acceptance record

Use the ESP32-S3/ST7789 reference device for the final Issue #36 acceptance. Do
not copy results from a simulator or an older firmware image. Before the run,
the maintainer must approve the sustained-refresh duration and the maximum
allowed dropped or rejected frames.

Record these identity fields in Issue #36:

- tested Git commit and the SHA-256 values from `flash-bundle.sha256`;
- board module marking, flash/PSRAM configuration, and display controller;
- display resolution and relevant pin configuration;
- negotiated protocol version, firmware version, device identity, and adapter
  identity;
- exact build, flash, monitor, host-test, native-test, and acceptance commands;
- approved refresh duration and dropped/rejected-frame thresholds.

Run and record each result separately:

1. negotiate from a fresh host session and query health;
2. present the standard host-rendered test pattern;
3. run every optional command that the device advertises and confirm unsupported
   commands remain disabled;
4. inject malformed, duplicate, reordered, and conflicting packets without a
   partial display update;
5. interrupt an incomplete transfer and confirm the previous frame remains
   visible;
6. reconnect after an endpoint/session reset;
7. reboot the device and confirm the stale host session is rejected until a new
   negotiation succeeds;
8. run the approved sustained-refresh interval and compare accepted, rejected,
   and dropped-frame evidence with the approved thresholds.

Issue #36 remains open until this record contains evidence from the current
hardware and the exact firmware image under review.
