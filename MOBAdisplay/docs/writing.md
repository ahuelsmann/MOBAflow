# MOBAdisplay wiring quick reference

For the current PlatformIO configuration:

```text
Display  -> ESP32-S3
VCC      -> 3V3
GND      -> GND
DIN      -> GPIO11
CLK      -> GPIO12
CS       -> GPIO10
DC       -> GPIO9
RST      -> GPIO13
```

Backlight wiring depends on the display board and is not controlled by the
current firmware. Verify the board schematic and
`MOBAdisplay/esp32/lib/TFT_eSPI/User_Setup.h` before applying power.
