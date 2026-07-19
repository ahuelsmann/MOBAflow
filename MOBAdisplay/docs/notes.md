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

The PlatformIO memory settings currently target an ESP32-S3 module with 8 MB
flash and octal PSRAM. Match `platformio.ini` to the exact module marking before
flashing another board variant.
