/**
 * MOBAdisplay firmware for ESP32-S3 + ST7789 (240x280, RGB565 over SPI).
 *
 * Receives full frames via UDP line protocol matching UdpLineFrameSender (MOBAflow).
 * Serial is diagnostics only; Wi-Fi credentials are provisioned and stored in NVS.
 */

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <WebServer.h>
#include <Preferences.h>
#include <TFT_eSPI.h>
#include <esp_heap_caps.h>
#include <esp_log.h>
#include <inttypes.h>
#include <UdpPacketParser.h>

#include <cstdio>
#include <cstring>

#ifndef WIFI_SSID
#define WIFI_SSID "KBS"
#endif

#ifndef WIFI_PASSWORD
#define WIFI_PASSWORD ""
#endif

#ifndef APP_VERSION
#define APP_VERSION "dev"
#endif

static constexpr uint32_t kDiagnosticSerialBaud = 115200;
static constexpr uint16_t kConfigHttpPort = 80;
static constexpr uint32_t kWifiConnectTimeoutMs = 20000;
static constexpr char kWifiPrefsNamespace[] = "wifi";
static constexpr char kWifiPrefsSsidKey[] = "ssid";
static constexpr char kWifiPrefsPasswordKey[] = "pwd";

constexpr int kTftWidth = 240;
constexpr int kTftHeight = 280;
constexpr int kUdpPort = 4210;

constexpr uint16_t kLineBytes = static_cast<uint16_t>(kTftWidth * 2);
constexpr uint16_t kIndexedLineBytes = static_cast<uint16_t>(kLineBytes + 2);
constexpr uint32_t kFramePixels = static_cast<uint32_t>(kTftWidth * kTftHeight);
constexpr size_t kFrameBytes = static_cast<size_t>(kFramePixels * sizeof(uint16_t));
static_assert(kLineBytes == MobaDisplay::Udp::kLegacyLineBytes, "Legacy row size must match the UDP parser.");
static_assert(kIndexedLineBytes == MobaDisplay::Udp::kIndexedLineBytes, "Indexed row size must match the UDP parser.");
static_assert(kTftHeight == MobaDisplay::Udp::kDisplayHeight, "Display height must match the UDP parser.");

TFT_eSPI tft;
WiFiUDP udp;
WebServer server(kConfigHttpPort);
Preferences preferences;

uint16_t* gFb = nullptr;
uint8_t pktBuf[MobaDisplay::Udp::kMaxPacketBytes];

volatile uint16_t linesReceivedCurrentFrame = 0;
volatile uint32_t framesOk = 0;
volatile uint32_t framesIncomplete = 0;
uint8_t gRowReceived[kTftHeight] = {0};

bool gIsApMode = false;
bool gUdpReady = false;
String gConnectedSsid;
String gConfigApSsid;
int gLastWifiStatus = WL_IDLE_STATUS;
String gHostProjectVersion = "n/a";

void splashStatic();
void drawBootScreen(const char* line1, const char* line2, const char* line3);
void drawListeningScreen(const char* modeLine, const char* ssidLine, const char* ipLine, uint16_t udpPort);

bool allocateFramebuffer();
inline void unpackLineBigEndianRgb565IntoRow(const uint8_t* packet480, uint16_t* rowOut240);

bool loadSavedWifiCredentials(String* ssidOut, String* passwordOut);
void saveWifiCredentials(const String& ssid, const String& password);
bool connectWifiStation(const String& ssid, const String& password);
void startWifiSetupAp();
void startConfigAccessPoint();
void connectWifiOrEnterSetupMode();
String getCurrentIpString();

void configureHttpApi();
void handleWifiStatus();
void handleWifiConfigPost();
const char* wifiStatusToText(int status);

bool allocateFramebuffer()
{
    void* heapPtr = heap_caps_aligned_alloc(4, kFrameBytes, MALLOC_CAP_SPIRAM | MALLOC_CAP_8BIT);
    if (!heapPtr)
        heapPtr = heap_caps_aligned_alloc(4, kFrameBytes, MALLOC_CAP_INTERNAL | MALLOC_CAP_8BIT);
    if (!heapPtr)
        return false;

    gFb = static_cast<uint16_t*>(heapPtr);
    return true;
}

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
    tft.setTextColor(TFT_RED, 0x18E7);
    tft.drawCentreString("MOBAdisplay", tft.width() / 2, 40, 4);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    String fwLine = String("FW: ") + APP_VERSION;
    tft.drawCentreString(fwLine.c_str(), tft.width() / 2, 68, 2);
    String hostLine = String("Host: ") + gHostProjectVersion;
    tft.drawCentreString(hostLine.c_str(), tft.width() / 2, 90, 2);
}

void drawBootScreen(const char* line1, const char* line2, const char* line3)
{
    splashStatic();
    tft.setTextDatum(MC_DATUM);
    tft.setTextColor(TFT_CYAN, 0x18E7);
    tft.drawString(line1 ? line1 : "", tft.width() / 2, 170, 2);
    tft.setTextColor(TFT_WHITE, 0x18E7);
    tft.drawString(line2 ? line2 : "", tft.width() / 2, 206, 2);
    tft.setTextColor(0x7BEF, 0x18E7);
    tft.drawString(line3 ? line3 : "", tft.width() / 2, 246, 1);
}

void drawListeningScreen(const char* modeLine, const char* ssidLine, const char* ipLine, uint16_t udpPort)
{
    splashStatic();
    tft.setTextDatum(MC_DATUM);
    tft.setTextColor(TFT_YELLOW, 0x18E7);
    tft.drawCentreString("Waiting for MOBAflow...", tft.width() / 2, 140, 2);
    tft.setTextColor(TFT_CYAN, 0x18E7);
    tft.drawString(modeLine ? modeLine : "", tft.width() / 2, 170, 2);
    tft.setTextColor(0xC618, 0x18E7);
    tft.drawString(ssidLine ? ssidLine : "", tft.width() / 2, 194, 2);
    tft.setTextColor(TFT_GREEN, 0x18E7);
    tft.drawString(ipLine ? ipLine : "", tft.width() / 2, 218, 2);

    char portLine[28];
    snprintf(portLine, sizeof(portLine), "UDP port %" PRIu16, udpPort);
    tft.setTextColor(TFT_GREEN, 0x18E7);
    tft.drawString(portLine, tft.width() / 2, 242, 2);

    tft.setTextColor(0x7BEF, 0x18E7);
    tft.drawString("MOBAflow: Display > Start", tft.width() / 2, 266, 1);
}

bool loadSavedWifiCredentials(String* ssidOut, String* passwordOut)
{
    preferences.begin(kWifiPrefsNamespace, true);
    const String ssid = preferences.getString(kWifiPrefsSsidKey, "");
    const String password = preferences.getString(kWifiPrefsPasswordKey, "");
    preferences.end();

    if (ssidOut)
        *ssidOut = ssid;

    if (passwordOut)
        *passwordOut = password;

    return ssid.length() > 0;
}

void saveWifiCredentials(const String& ssid, const String& password)
{
    preferences.begin(kWifiPrefsNamespace, false);
    preferences.putString(kWifiPrefsSsidKey, ssid);
    preferences.putString(kWifiPrefsPasswordKey, password);
    preferences.end();
}

bool connectWifiStation(const String& ssid, const String& password)
{
    if (ssid.length() == 0)
        return false;

    WiFi.mode(WIFI_AP_STA);
    WiFi.begin(ssid.c_str(), password.c_str());

    const uint32_t startMs = millis();
    while (WiFi.status() != WL_CONNECTED && (millis() - startMs) < kWifiConnectTimeoutMs)
    {
        delay(250);
        Serial.print(".");
    }
    Serial.println();
    gLastWifiStatus = WiFi.status();

    if (WiFi.status() != WL_CONNECTED)
    {
        Serial.printf("Wi-Fi connect failed with status %d (%s)\r\n", gLastWifiStatus, wifiStatusToText(gLastWifiStatus));
        WiFi.disconnect(true, true);
        delay(100);
        return false;
    }

    WiFi.setSleep(false);
    gIsApMode = false;
    gConnectedSsid = ssid;
    startConfigAccessPoint();
    return true;
}

void startConfigAccessPoint()
{
    const uint32_t chipSuffix = static_cast<uint32_t>(ESP.getEfuseMac() & 0xFFFFFFu);

    char apName[32];
    snprintf(apName, sizeof(apName), "MOBAflow-Setup-%06lX", static_cast<unsigned long>(chipSuffix));
    WiFi.softAP(apName);
    gConfigApSsid = apName;
    Serial.printf("Config AP: %s @ %s\r\n", apName, WiFi.softAPIP().toString().c_str());
}

void startWifiSetupAp()
{
    WiFi.mode(WIFI_AP);
    startConfigAccessPoint();
    gIsApMode = true;
    gConnectedSsid = gConfigApSsid;

    const String apIp = WiFi.softAPIP().toString();
    drawBootScreen("Setup AP active", gConfigApSsid.c_str(), apIp.c_str());
}

void connectWifiOrEnterSetupMode()
{
    String savedSsid;
    String savedPassword;
    const bool hasSavedCredentials = loadSavedWifiCredentials(&savedSsid, &savedPassword);

    if (hasSavedCredentials)
    {
        drawBootScreen("Connecting saved", savedSsid.c_str(), nullptr);
        if (connectWifiStation(savedSsid, savedPassword))
            return;

        // Saved credentials exist but failed: stay in setup mode so the user can fix Wi-Fi config.
        startWifiSetupAp();
        return;
    }

    const String fallbackSsid = WIFI_SSID;
    const String fallbackPassword = WIFI_PASSWORD;
    if (fallbackSsid.length() > 0)
    {
        drawBootScreen("Connecting default", fallbackSsid.c_str(), nullptr);
        if (connectWifiStation(fallbackSsid, fallbackPassword))
        {
            saveWifiCredentials(fallbackSsid, fallbackPassword);
            return;
        }
    }

    startWifiSetupAp();
}

String getCurrentIpString()
{
    return gIsApMode ? WiFi.softAPIP().toString() : WiFi.localIP().toString();
}

void handleWifiStatus()
{
    const wifi_mode_t mode = WiFi.getMode();
    const char* modeText = "sta";
    if (mode == WIFI_MODE_AP)
        modeText = "ap";
    else if (mode == WIFI_MODE_APSTA)
        modeText = "ap+sta";

    String payload = "{";
    payload += "\"mode\":\"";
    payload += modeText;
    payload += "\",\"ssid\":\"";
    payload += gConnectedSsid;
    payload += "\",\"ip\":\"";
    payload += getCurrentIpString();
    payload += "\",\"stationIp\":\"";
    payload += WiFi.localIP().toString();
    payload += "\",\"configApSsid\":\"";
    payload += gConfigApSsid;
    payload += "\",\"configApIp\":\"";
    payload += WiFi.softAPIP().toString();
    payload += "\",\"lastWifiStatusCode\":";
    payload += String(gLastWifiStatus);
    payload += ",\"lastWifiStatus\":\"";
    payload += wifiStatusToText(gLastWifiStatus);
    payload += "\",\"udpPort\":";
    payload += String(kUdpPort);
    payload += ",\"configPort\":";
    payload += String(kConfigHttpPort);
    payload += "}";

    server.send(200, "application/json", payload);
}

const char* wifiStatusToText(int status)
{
    switch (status)
    {
    case WL_IDLE_STATUS:
        return "WL_IDLE_STATUS";
    case WL_NO_SSID_AVAIL:
        return "WL_NO_SSID_AVAIL";
    case WL_SCAN_COMPLETED:
        return "WL_SCAN_COMPLETED";
    case WL_CONNECTED:
        return "WL_CONNECTED";
    case WL_CONNECT_FAILED:
        return "WL_CONNECT_FAILED";
    case WL_CONNECTION_LOST:
        return "WL_CONNECTION_LOST";
    case WL_DISCONNECTED:
        return "WL_DISCONNECTED";
    default:
        return "WL_UNKNOWN";
    }
}

void handleWifiConfigPost()
{
    String ssid = server.hasArg("ssid") ? server.arg("ssid") : String();
    String password = server.hasArg("password") ? server.arg("password") : String();
    ssid.trim();

    if (ssid.length() == 0)
    {
        server.send(400, "application/json", "{\"ok\":false,\"message\":\"SSID is required.\"}");
        return;
    }

    saveWifiCredentials(ssid, password);
    server.send(200, "application/json", "{\"ok\":true,\"message\":\"Saved. Device will reboot.\"}");
    delay(300);
    ESP.restart();
}

void configureHttpApi()
{
    server.on("/api/wifi/status", HTTP_GET, handleWifiStatus);
    server.on("/api/wifi/config", HTTP_POST, handleWifiConfigPost);
    server.begin();
    Serial.printf("Config API ready on :%u\r\n", static_cast<unsigned>(kConfigHttpPort));
}

void setup()
{
    Serial.begin(kDiagnosticSerialBaud);
    delay(300);
#ifdef ARDUINO_ARCH_ESP32
    Serial.setDebugOutput(false);
    esp_log_level_set("*", ESP_LOG_ERROR);
#endif

    Serial.println();
    Serial.println(F("--- MOBAdisplay ESP32-S3 ---"));

    tft.init();
    tft.setRotation(0);
    tft.setSwapBytes(true);
    splashStatic();

    const size_t psram = ESP.getPsramSize();
    Serial.printf("PSRAM: %zu bytes\r\n", static_cast<size_t>(psram));
    if (psram == 0)
        Serial.println(F("Warning: board reports no PSRAM; framebuffer may allocate from SRAM."));

    if (!allocateFramebuffer())
    {
        drawBootScreen("Out of RAM", "Framebuffer", "");
        ESP.restart();
        return;
    }

    connectWifiOrEnterSetupMode();
    configureHttpApi();

    if (!udp.begin(kUdpPort))
    {
        drawBootScreen("UDP bind failed", "Port already used?", "");
        ESP.restart();
        return;
    }

    gUdpReady = true;
    const String ip = getCurrentIpString();
    if (!gIsApMode)
    {
        Serial.print(F("Wi-Fi OK, IP address: "));
        Serial.println(ip);
    }
    else
    {
        Serial.print(F("AP mode IP address: "));
        Serial.println(ip);
    }
    Serial.printf("UDP frame port: %u\r\n", static_cast<unsigned>(kUdpPort));
        const char* modeText = gIsApMode ? "AP Setup Mode" : "Wi-Fi Client Mode";
        drawListeningScreen(modeText, gConnectedSsid.c_str(), ip.c_str(), static_cast<uint16_t>(kUdpPort));
}

void resetCapture()
{
    linesReceivedCurrentFrame = 0;
    memset(gRowReceived, 0, sizeof(gRowReceived));
}

void presentCapturedFrameIfAny()
{
    if (linesReceivedCurrentFrame == 0 || !gFb)
        return;

    tft.pushImage(0, 0, kTftWidth, kTftHeight, gFb);
    if (linesReceivedCurrentFrame == kTftHeight)
    {
        ++framesOk;
        if ((framesOk & 0x01FFU) == 0U)
            Serial.printf("Frames OK: %" PRIu32 "\r\n", framesOk);
    }
    else
    {
        ++framesIncomplete;
    }
}

void loop()
{
    server.handleClient();

    if (!gUdpReady)
    {
        delay(2);
        return;
    }

    const int pktSize = udp.parsePacket();
    if (pktSize <= 0)
        return;

    const int cap = pktSize > static_cast<int>(sizeof(pktBuf)) ? static_cast<int>(sizeof(pktBuf)) : pktSize;
    const int rd = udp.read(reinterpret_cast<unsigned char*>(pktBuf), cap);
    if (rd <= 0)
        return;

    // A partial WiFiUDP read retains the rest of the packet and blocks parsePacket() until it is consumed.
    while (udp.available() > 0)
        udp.read();

    const MobaDisplay::Udp::PacketView packet = MobaDisplay::Udp::ClassifyPacket(
        pktBuf,
        static_cast<size_t>(rd),
        static_cast<size_t>(pktSize));

    if (packet.kind == MobaDisplay::Udp::PacketKind::FrameStart)
    {
        resetCapture();
        return;
    }

    if (packet.kind == MobaDisplay::Udp::PacketKind::HostVersion)
    {
        const size_t displayedLength = packet.payloadLength > MobaDisplay::Udp::kDisplayedHostVersionBytes
            ? MobaDisplay::Udp::kDisplayedHostVersionBytes
            : packet.payloadLength;
        char displayedVersion[MobaDisplay::Udp::kDisplayedHostVersionBytes + 1];
        memcpy(displayedVersion, packet.payload, displayedLength);
        displayedVersion[displayedLength] = '\0';
        gHostProjectVersion = String(displayedVersion);
        // Keep waiting screen untouched here to avoid misleading flicker when only meta packets arrive.
        return;
    }

    if (packet.kind == MobaDisplay::Udp::PacketKind::FrameDone)
    {
        presentCapturedFrameIfAny();
        resetCapture();
        return;
    }

    if (packet.kind == MobaDisplay::Udp::PacketKind::LegacyLine)
    {
        // Legacy packet format: pure 480-byte line stream (order dependent).
        if (linesReceivedCurrentFrame < kTftHeight && gFb)
        {
            uint16_t* row = gFb + (static_cast<uint32_t>(linesReceivedCurrentFrame) * kTftWidth);
            unpackLineBigEndianRgb565IntoRow(pktBuf, row);
            ++linesReceivedCurrentFrame;
        }
        return;
    }

    if (packet.kind != MobaDisplay::Udp::PacketKind::IndexedLine)
        return;

    if (gFb)
    {
        const uint16_t rowIndex = packet.rowIndex;

        // Row 0 is a strong start-of-frame signal for indexed packets, even if FRAME_START was dropped.
        if (rowIndex == 0 && linesReceivedCurrentFrame > 0)
        {
            presentCapturedFrameIfAny();
            resetCapture();
        }

        uint16_t* row = gFb + (static_cast<uint32_t>(rowIndex) * kTftWidth);
        unpackLineBigEndianRgb565IntoRow(packet.payload, row);
        if (gRowReceived[rowIndex] == 0)
        {
            gRowReceived[rowIndex] = 1;
            ++linesReceivedCurrentFrame;
            if (linesReceivedCurrentFrame == kTftHeight)
            {
                presentCapturedFrameIfAny();
                resetCapture();
            }
        }
    }
}
