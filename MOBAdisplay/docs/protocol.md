# MOBAdisplay Frame Protocol

## Transport
UDP, Port 4210

## Frame Format
- RGB565
- 240x280
- 240 * 280 * 2 = 134400 bytes

## Transmission
- Daten werden in 1024-Byte-Chunks gesendet
- Nach letztem Chunk folgt:

## Verhalten ESP32
- Chunks werden in framebuffer[] geschrieben
- Bei FRAME_DONE → Display aktualisieren
