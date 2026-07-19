# MOBAdisplay Frame Protocol

## Transport

UDP, default port `4210`.

## Frame Format

- Pixel format: RGB565
- Bytes per pixel: 2
- Frame byte count: `width * height * 2`
- Default 1.69 inch LCD: `240 * 280 * 2 = 134400` bytes

## Current line-based transmission

`Moba.Display.Transport.UdpLineFrameSender` sends frames as one UDP packet per
display row:

1. `HOST_VER:<version>` once per endpoint after the endpoint changes.
2. `DISPLAY_META:<width>:<height>:<displayModel>:<rotation>` before each frame.
3. `FRAME_START`.
4. One packet per row:
   - byte 0: high byte of row index.
   - byte 1: low byte of row index.
   - remaining bytes: complete RGB565 row data.
5. `FRAME_DONE`.

## Legacy chunk transmission

`Moba.Display.FrameSender` still contains a simple legacy sender:

- Sends 1024-byte chunks without row indexes.
- Sleeps briefly between chunks.
- Sends `FRAME_DONE` after the last chunk.

New firmware should prefer the line-based protocol because it carries row
positions and display metadata.

## ESP32 implementation

The current PlatformIO firmware in `MOBAdisplay/esp32/src/main.cpp` listens on
UDP port `4210`, allocates a 240x280 RGB565 framebuffer, accepts indexed line
packets and presents complete or explicitly finished frames on the ST7789 TFT.
It also accepts legacy unindexed 480-byte rows.

`HOST_VER` is shown as host-version metadata. `FRAME_START` resets capture and
`FRAME_DONE` presents the captured frame. `DISPLAY_META` is currently ignored;
the firmware dimensions are compiled for 240x280.

The same firmware exposes a local HTTP Wi-Fi setup/status API on port `80` and
stores credentials in ESP32 preferences/NVS. The older
`MOBAdisplay/MobaDisplay/MobaDisplay.ino` file remains a TFT color-cycle smoke
test without networking.
