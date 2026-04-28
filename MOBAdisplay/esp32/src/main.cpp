/**
 * MOBAdisplay firmware for ESP32-S3 + ST7789 (240×280, RGB565 over SPI).
 *
 * Receives full frames via UDP line protocol matching UdpLineFrameSender (MOBAflow), or the same
 * byte sequence over USB serial (SerialLineFrameSender). Protocol:
 *   FRAME_START → 280 × 480 bytes (one RGB565 scanline BE per line) → FRAME_DONE → push to TFT.
 *
 * Configure Wi-Fi below or via PlatformIO build_flags (-DWIFI_SSID / -DWIFI_PASSWORD).
 */

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <TFT_eSPI.h>
#include <esp_heap_caps.h>
#include <esp_log.h>
#include <inttypes.h>

#include <algorithm>
#include <cstdio>
#include <cstring>
#include <vector>

// ----- Wi-Fi credentials (prefer build_flags override) -----
#ifndef WIFI_SSID
#define WIFI_SSID "KBS"
#endif

#ifndef WIFI_PASSWORD
#define WIFI_PASSWORD ""
#endif

// Match MOBAflow Display page default for USB / serial transport.
static constexpr uint32_t kUsbSerialBaud = 921600;

// -----------------------------------------------------------------------------
// Display frame size must match FrameDimensions.cs / MOBAflow renderer
// -----------------------------------------------------------------------------
constexpr int kTftWidth = 240;
constexpr int kTftHeight = 280;
constexpr int kUdpPort = 4210;

constexpr uint8_t kLineBytes = static_cast<uint8_t>(kTftWidth * 2);
constexpr uint32_t kFramePixels = static_cast<uint32_t>(kTftWidth * kTftHeight);
constexpr size_t kFrameBytes = static_cast<size_t>(kFramePixels * sizeof(uint16_t));

// Protocol markers (no trailing null sent from host)
constexpr const char kFrameStart[] = "FRAME_START";  // 11 chars
constexpr const char kFrameDone[] = "FRAME_DONE";    // 9 chars
constexpr uint8_t kFrameStartLen = 11;
constexpr uint8_t kFrameDoneLen = 9;

/// Full USB line-protocol frame size (matches PC SerialLineFrameSender).
static constexpr size_t kUsbFrameTotalBytes =
    static_cast<size_t>(kFrameStartLen) +
    static_cast<size_t>(kTftHeight) * static_cast<size_t>(kLineBytes) +
    static_cast<size_t>(kFrameDoneLen);

/// UART RX ring must hold at least one full frame; Arduino-ESP32 default RX is ~256 bytes otherwise.
static constexpr size_t kSerialRxBufferBytes = static_cast<size_t>(
    kUsbFrameTotalBytes + 65536u); // ~200 KiB margin for OS/driver jitter

TFT_eSPI tft;
WiFiUDP udp;

uint16_t* gFb = nullptr;

uint8_t pktBuf[768];

volatile uint16_t linesReceivedCurrentFrame = 0;
volatile uint32_t framesOk = 0;
volatile uint32_t framesIncomplete = 0;

/// True once any payload byte arrives on Serial (suppresses stray Serial logging that would corrupt framing).
static bool gSerialBusy;

static std::vector<uint8_t> s_serialRx;

void drawBootScreen(const char* line1, const char* line2, const char* line3);

/// After setup: show that we are listening (avoids a "dead" all-black screen when no host yet).
void drawListeningScreen(const char* ipLine, uint16_t udpPort);

bool allocateFramebuffer();

inline void unpackLineBigEndianRgb565IntoRow(const uint8_t* packet480, uint16_t* rowOut240);

void splashStatic();

void connectWifiBlocking();

bool findFrameStartIndex(size_t from, size_t* outIx);
void drainUsbSerialFifo();
bool tryConsumeOneSerialFrame();
void capSerialBuffer();

bool allocateFramebuffer()
{
    // Prefer PSRAM for ~131 KiB RGB565 buffer (SPIRAM-capable boards).
    void* heapPtr = heap_caps_aligned_alloc(4, kFrameBytes, MALLOC_CAP_SPIRAM | MALLOC_CAP_8BIT);
    if (!heapPtr)
        heapPtr = heap_caps_aligned_alloc(4, kFrameBytes, MALLOC_CAP_INTERNAL | MALLOC_CAP_8BIT);
    if (!heapPtr)
        return false;
    gFb = static_cast<uint16_t*>(heapPtr);
    return true;
}

// Host sends each RGB565 pixel as big-endian hi, lo (matches Bitmap/Rgb565 converter).
inline void unpackLineBigEndianRgb565IntoRow(const uint8_t* packet480, uint16_t* rowOut240)
{
    for (int x = 0; x < kTftWidth; ++x)
    {
        uint16_t p = static_cast<uint16_t>(packet480[2 * x]) << 8;
        p |= packet480[2 * x + 1];
        rowOut240[x] = p;
    }
}

void splashStatic()
{
    tft.fillScreen(0x18E7);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    tft.drawCentreString("MOBAflow", tft.width() / 2, 54, 4);
    tft.drawCentreString("Display", tft.width() / 2, 96, 4);
}

void drawBootScreen(const char* line1, const char* line2, const char* line3)
{
    splashStatic();
    tft.setTextDatum(MC_DATUM);
    tft.setTextColor(TFT_CYAN, 0x18E7);
    tft.drawString(line1 ?: "", tft.width() / 2, 170, 2);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    tft.drawString(line2 ?: "", tft.width() / 2, 206, 2);
    tft.setTextColor(0x7BEF, 0x18E7);
    tft.drawString(line3 ?: "", tft.width() / 2, 246, 1);
}

void drawListeningScreen(const char* ipLine, uint16_t udpPort)
{
    splashStatic();
    tft.setTextDatum(MC_DATUM);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    tft.drawCentreString("Waiting for stream", tft.width() / 2, 116, 4);
    tft.setTextColor(TFT_CYAN, 0x18E7);
    tft.drawString(ipLine ? ipLine : "", tft.width() / 2, 176, 2);
    char portLine[28];
    snprintf(portLine, sizeof(portLine), "UDP port %" PRIu16, udpPort);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    tft.drawString(portLine, tft.width() / 2, 216, 2);
    tft.setTextColor(0x7BEF, 0x18E7);
    tft.drawString("MOBAflow: Display > Start", tft.width() / 2, 252, 1);
}

void connectWifiBlocking()
{
    WiFi.mode(WIFI_STA);

    if (strlen(WIFI_SSID) == 0)
    {
        drawBootScreen(
            "Set WIFI_SSID in",
            "platformio.ini or sketch",
            "then reflash");
        for (;;)
            delay(1000);
    }

    drawBootScreen(
        "Connecting",
        WIFI_SSID,
        nullptr);

    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    uint8_t dot = 0;
    constexpr uint16_t connTimeoutSlots = 80U;
    uint16_t i = 0;
    while (WiFi.status() != WL_CONNECTED && i < connTimeoutSlots)
    {
        delay(250);
        Serial.print(".");
        ++dot;
        if (dot % 20 == 0)
            Serial.println();
        ++i;
    }
    Serial.println();

    if (WiFi.status() != WL_CONNECTED)
    {
        drawBootScreen(
            "Wi-Fi failed",
            "Check SSID/pass",
            "Reboot ESP32…");
        delay(4000);
        ESP.restart();
    }

    // Modem sleep can delay or drop incoming Wi-Fi traffic; disable for real-time UDP display frames.
    WiFi.setSleep(false);

    String ip = WiFi.localIP().toString();
    Serial.print(F("Wi-Fi OK, IP address: "));
    Serial.println(ip);
    Serial.printf("UDP frame port: %u — set MOBAflow Display to this IP.\r\n\r\n", static_cast<unsigned>(kUdpPort));

    String line3 = "UDP :";
    line3 += kUdpPort;
    drawBootScreen(
        "Ready",
        ip.c_str(),
        line3.c_str());
    delay(1200);
}

void capSerialBuffer()
{
    constexpr size_t kCapBytes = 300000;
    if (s_serialRx.size() <= kCapBytes)
        return;

    const size_t dropCount = std::max<size_t>(1024UL, s_serialRx.size() - kCapBytes / 3);
    if (dropCount >= s_serialRx.size())
        s_serialRx.clear();
    else
        s_serialRx.erase(s_serialRx.begin(), s_serialRx.begin() + static_cast<std::ptrdiff_t>(dropCount));
}

bool findFrameStartIndex(size_t from, size_t* outIx)
{
    capSerialBuffer();
    const size_t lim = s_serialRx.size();
    if (lim < kFrameStartLen + from)
        return false;
    const size_t end = lim - kFrameStartLen;
    for (size_t i = from; i <= end; ++i)
    {
        if (memcmp(s_serialRx.data() + i, kFrameStart, kFrameStartLen) == 0)
        {
            *outIx = i;
            return true;
        }
    }

    return false;
}

bool tryConsumeOneSerialFrame()
{
    const size_t payloadBytes = static_cast<size_t>(kTftHeight) * kLineBytes;
    const size_t need =
        static_cast<size_t>(kFrameStartLen) + payloadBytes + static_cast<size_t>(kFrameDoneLen);

    while (true)
    {
        size_t startIx = 0;
        if (!findFrameStartIndex(0, &startIx))
            return false;

        if (startIx > 0)
        {
            s_serialRx.erase(s_serialRx.begin(),
                s_serialRx.begin() + static_cast<std::ptrdiff_t>(startIx));
            continue;
        }

        if (s_serialRx.size() < need)
            return false;

        const uint8_t* doneProbe =
            s_serialRx.data() + static_cast<size_t>(kFrameStartLen) + payloadBytes;
        if (memcmp(doneProbe, kFrameDone, kFrameDoneLen) != 0)
        {
            s_serialRx.erase(s_serialRx.begin());
            continue;
        }

        if (gFb)
        {
            for (int y = 0; y < kTftHeight; ++y)
            {
                const uint8_t* lineSrc =
                    s_serialRx.data() + static_cast<size_t>(kFrameStartLen) +
                    static_cast<size_t>(y) * kLineBytes;
                uint16_t* row = gFb + static_cast<size_t>(y) * kTftWidth;
                unpackLineBigEndianRgb565IntoRow(lineSrc, row);
            }

            tft.pushImage(0, 0, kTftWidth, kTftHeight, gFb);
            ++framesOk;
        }

        s_serialRx.erase(s_serialRx.begin(),
            s_serialRx.begin() + static_cast<std::ptrdiff_t>(need));
        return true;
    }
}

void drainUsbSerialFifo()
{
    while (Serial.available())
    {
        const int c = Serial.read();
        if (c < 0)
            break;
        gSerialBusy = true;
        s_serialRx.push_back(static_cast<uint8_t>(c));
    }

    while (tryConsumeOneSerialFrame())
    {
    }
}

void setup()
{
    // IMPORTANT: Call *before* begin(); Arduino-ESP32 default RX is tiny (~256 B); one MOBAframe is ~134420 B.
    const size_t serialRxConfigured = Serial.setRxBufferSize(kSerialRxBufferBytes);

    Serial.begin(kUsbSerialBaud);
    delay(300);
#ifdef ARDUINO_ARCH_ESP32
    // Critical for USB-display mode: muxing ESP-IDF logs onto CDC UART inserts bytes between MOBA PROTO bytes.
    Serial.setDebugOutput(false);
    esp_log_level_set("*", ESP_LOG_ERROR);
#endif

    Serial.println();
    Serial.println(F("--- MOBAdisplay ESP32-S3 ---"));
    if (serialRxConfigured < kUsbFrameTotalBytes)
    {
        Serial.printf(
            "ERROR UART RX=%u B for frame=%u B — USB picture stream cannot work.\r\n",
            static_cast<unsigned>(serialRxConfigured),
            static_cast<unsigned>(kUsbFrameTotalBytes));
    }

    tft.init();
    tft.setRotation(0);
    tft.setSwapBytes(true);
    splashStatic();

    size_t psram = ESP.getPsramSize();
    Serial.printf("PSRAM: %zu bytes\r\n", static_cast<size_t>(psram));
    if (psram == 0)
        Serial.println(F("Warning: board reports no PSRAM; framebuffer may allocate from SRAM."));

    if (!allocateFramebuffer())
    {
        drawBootScreen(
            "Out of RAM",
            "Framebuffer",
            "");
        ESP.restart();
        return;
    }

    connectWifiBlocking();

    if (!udp.begin(kUdpPort))
    {
        drawBootScreen(
            "UDP bind failed",
            "Port already used?",
            "");
        ESP.restart();
        return;
    }

    s_serialRx.reserve(std::max(kSerialRxBufferBytes, kUsbFrameTotalBytes));
    Serial.printf(
        "Listening UDP %u; USB UART %u baud — RX buf %u bytes (frame ~%u B).\r\n",
        static_cast<unsigned>(kUdpPort),
        static_cast<unsigned>(kUsbSerialBaud),
        static_cast<unsigned>(serialRxConfigured),
        static_cast<unsigned>(kUsbFrameTotalBytes));
    String lip = WiFi.localIP().toString();
    drawListeningScreen(lip.c_str(), kUdpPort);
}

void resetCapture()
{
    linesReceivedCurrentFrame = 0;
}

void loop()
{
    drainUsbSerialFifo();

    const int pktSize = udp.parsePacket();
    if (pktSize <= 0)
        return;

    const int cap = pktSize > static_cast<int>(sizeof(pktBuf)) ? static_cast<int>(sizeof(pktBuf)) : pktSize;
    const int rd = udp.read(reinterpret_cast<unsigned char*>(pktBuf), cap);
    if (rd <= 0)
        return;

    if (rd == static_cast<int>(kFrameStartLen) && memcmp(pktBuf, kFrameStart, kFrameStartLen) == 0)
    {
        resetCapture();
        return;
    }

    if (rd == static_cast<int>(kFrameDoneLen) && memcmp(pktBuf, kFrameDone, kFrameDoneLen) == 0)
    {
        if (linesReceivedCurrentFrame == kTftHeight && gFb)
        {
            tft.pushImage(0, 0, kTftWidth, kTftHeight, gFb);
            ++framesOk;
            if ((framesOk & 0x01FFU) == 0 && !gSerialBusy)
                Serial.printf("Frames OK: %" PRIu32 "\r\n", framesOk);
        }
        else if (linesReceivedCurrentFrame > 0)
        {
            ++framesIncomplete;
        }
        resetCapture();
        return;
    }

    if (static_cast<size_t>(rd) != kLineBytes)
        return;

    if (linesReceivedCurrentFrame < kTftHeight && gFb)
    {
        uint16_t* row = gFb + (static_cast<uint32_t>(linesReceivedCurrentFrame) * kTftWidth);
        unpackLineBigEndianRgb565IntoRow(pktBuf, row);
        ++linesReceivedCurrentFrame;
    }
}
