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

## Verhalten ESP32

The current `MobaDisplay.ino` is a TFT_eSPI smoke test only. It initializes the
display and cycles red, green, blue, and white screens. UDP receive handling is
not implemented there yet.

Required firmware behavior for the line protocol:

- Listen on UDP port `4210`.
- Accept optional `HOST_VER` and `DISPLAY_META` packets.
- Allocate or validate a framebuffer matching `width * height * 2`.
- On `FRAME_START`, reset frame assembly state.
- For each row packet, copy the RGB565 payload into `framebuffer[row]`.
- On `FRAME_DONE`, flush the framebuffer to the TFT.
