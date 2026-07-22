/**
 * MOBAdisplay firmware for ESP32-S3 + ST7789 (240x280, RGB565 over SPI).
 *
 * Receives full frames via UDP line protocol matching UdpLineFrameSender (MOBAflow).
 * Serial is diagnostics only; Wi-Fi credentials are provisioned and stored in NVS.
 */

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <Preferences.h>
#include <TFT_eSPI.h>
#include <BoardConfig.h>
#include <esp_random.h>
#include <esp_heap_caps.h>
#include <esp_log.h>
#include <inttypes.h>
#include <UdpPacketParser.h>
#include <ProvisioningState.h>
#include <Security2Transport.h>

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <array>
#include <string>

#ifndef APP_VERSION
#define APP_VERSION "dev"
#endif

static constexpr uint32_t kDiagnosticSerialBaud = 115200;
static constexpr uint32_t kWifiConnectTimeoutMs = 20000;
static constexpr uint32_t kActivationHoldMs = 5000;
static constexpr uint32_t kFactoryResetHoldMs = 12000;
static constexpr uint8_t kSetupSecretBytes = 16;
static constexpr char kWifiPrefsNamespace[] = "wifi";
static constexpr char kWifiPrefsSsidKey[] = "ssid";
static constexpr char kWifiPrefsPasswordKey[] = "pwd";
static constexpr char kWifiPrefsOwnerKey[] = "owner";
static constexpr char kWifiPrefsOwnerPublicKey[] = "owner_pk";

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
Preferences preferences;

uint16_t* gFb = nullptr;
uint8_t pktBuf[MobaDisplay::Udp::kMaxPacketBytes];

volatile uint16_t linesReceivedCurrentFrame = 0;
volatile uint32_t framesOk = 0;
volatile uint32_t framesIncomplete = 0;
uint8_t gRowReceived[kTftHeight] = {0};

bool gUdpReady = false;
String gConnectedSsid;
int gLastWifiStatus = WL_IDLE_STATUS;
String gHostProjectVersion = "n/a";
struct ProvisioningRuntime
{
    MobaDisplay::Provisioning::StateMachine state;
    MobaDisplay::Provisioning::Security2Transport transport;
    String ssid;
    String passphrase;
    uint32_t buttonPressedAtMs = 0;
    bool buttonLongActionHandled = false;
};

ProvisioningRuntime& provisioningRuntime()
{
    static ProvisioningRuntime runtime;
    return runtime;
}

uint8_t StateValue(MobaDisplay::Provisioning::State value)
{
    if (value == MobaDisplay::Provisioning::State::Unprovisioned)
        return 0U;
    if (value == MobaDisplay::Provisioning::State::AwaitingActivation)
        return 1U;
    if (value == MobaDisplay::Provisioning::State::Operational)
        return 2U;
    if (value == MobaDisplay::Provisioning::State::WindowOpen)
        return 3U;
    if (value == MobaDisplay::Provisioning::State::PendingConnection)
        return 4U;
    if (value == MobaDisplay::Provisioning::State::AwaitingHandover)
        return 5U;
    if (value == MobaDisplay::Provisioning::State::PromotionPending)
        return 6U;
    if (value == MobaDisplay::Provisioning::State::Offline)
        return 7U;
    return 0U;
}

bool IsProvisioningWindowState(MobaDisplay::Provisioning::State value)
{
    const uint8_t stateCode = StateValue(value);
    return stateCode >= 3U && stateCode <= 6U;
}

void splashStatic();
void drawBootScreen(const char* line1, const char* line2, const char* line3);
void drawListeningScreen(const char* modeLine, const char* ssidLine, const char* ipLine, uint16_t udpPort);

bool allocateFramebuffer();
inline void unpackLineBigEndianRgb565IntoRow(const uint8_t* packet480, uint16_t* rowOut240);

bool loadSavedWifiCredentials(String* ssidOut, String* passwordOut);
void saveWifiCredentials(const String& ssid, const String& password);
bool loadOwnerBinding();
bool saveOwnerBinding(const uint8_t* publicKey, size_t publicKeyLength);
bool connectWifiStation(const String& ssid, const String& password);
void connectWifiOrWaitForActivation();
bool startProvisioningWindow();
void closeProvisioningWindow(bool keepActiveNetwork);
void updateActivationButton();
esp_err_t handleProvisioningRequest(uint32_t sessionId, const uint8_t* input, ssize_t inputLength,
    uint8_t** output, ssize_t* outputLength, void* privateData);
bool createSetupPassphrase(String* passphraseOut);
const char* wifiStatusToText(int status);
void handleUdpPacket(const MobaDisplay::Udp::PacketView& packet);
void processUdpPacket();

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

    WiFi.mode(WIFI_STA);
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
    gConnectedSsid = ssid;
    return true;
}

bool loadOwnerBinding()
{
    preferences.begin(kWifiPrefsNamespace, true);
    const bool ownerBound = preferences.getBool(kWifiPrefsOwnerKey, false);
    preferences.end();
    provisioningRuntime().state.Boot(loadSavedWifiCredentials(nullptr, nullptr), ownerBound);
    return ownerBound;
}

bool saveOwnerBinding(const uint8_t* publicKey, size_t publicKeyLength)
{
    if (publicKey == nullptr || publicKeyLength != 65)
        return false;

    preferences.begin(kWifiPrefsNamespace, false);
    const size_t written = preferences.putBytes(kWifiPrefsOwnerPublicKey, publicKey, publicKeyLength);
    preferences.putBool(kWifiPrefsOwnerKey, written == publicKeyLength);
    preferences.end();
    return written == publicKeyLength;
}

bool createSetupPassphrase(String* passphraseOut)
{
    if (passphraseOut == nullptr)
        return false;

    static const std::string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    std::array<uint8_t, kSetupSecretBytes> randomBytes = {};
    esp_fill_random(randomBytes.data(), randomBytes.size());

    String passphrase;
    passphrase.reserve(22);
    for (uint8_t index = 0; index < 20; ++index)
        passphrase += alphabet[randomBytes[index % randomBytes.size()] % alphabet.size()];

    volatile uint8_t* sensitiveBytes = randomBytes.data();
    for (size_t index = 0; index < randomBytes.size(); ++index)
        sensitiveBytes[index] = 0;
    *passphraseOut = passphrase;
    return passphrase.length() >= 16;
}

bool startProvisioningWindow()
{
    ProvisioningRuntime& runtime = provisioningRuntime();
    if (!runtime.state.BeginActivation(millis()))
        return false;

    if (!createSetupPassphrase(&runtime.passphrase))
    {
        closeProvisioningWindow(false);
        return false;
    }

    const uint32_t chipSuffix = static_cast<uint32_t>(ESP.getEfuseMac() & 0xFFFFFFu);
    auto suffix = String(chipSuffix, HEX);
    while (suffix.length() < 6)
        suffix = String("0") + suffix;
    suffix.toUpperCase();
    runtime.ssid = String("MOBAflow-Setup-") + suffix;
    WiFi.mode(WIFI_AP);
    if (!WiFi.softAP(runtime.ssid.c_str(), runtime.passphrase.c_str()))
    {
        closeProvisioningWindow(false);
        return false;
    }

    const char* setupSecret = runtime.passphrase.c_str();
    if (runtime.transport.Start(setupSecret, handleProvisioningRequest) != ESP_OK)
    {
        closeProvisioningWindow(false);
        return false;
    }

    drawBootScreen("Protected setup active", runtime.ssid.c_str(), runtime.passphrase.c_str());
    return true;
}

void closeProvisioningWindow(bool keepActiveNetwork)
{
    ProvisioningRuntime& runtime = provisioningRuntime();
    runtime.transport.Stop();
    WiFi.softAPdisconnect(true);
    WiFi.mode(keepActiveNetwork ? WIFI_STA : WIFI_OFF);
    runtime.passphrase = String();
    runtime.ssid = String();

    if (keepActiveNetwork)
    {
        drawBootScreen("Wi-Fi ready", gConnectedSsid.c_str(), "Protected setup closed");
        return;
    }

    drawBootScreen("Setup unavailable", "Hold BOOT for setup", "AP is off");
}

void connectWifiOrWaitForActivation()
{
    String savedSsid;
    String savedPassword;
    if (loadSavedWifiCredentials(&savedSsid, &savedPassword))
    {
        drawBootScreen("Connecting saved", "Wi-Fi credentials", nullptr);
        if (connectWifiStation(savedSsid, savedPassword))
        {
            drawListeningScreen("Wi-Fi client mode", gConnectedSsid.c_str(), WiFi.localIP().toString().c_str(),
                static_cast<uint16_t>(kUdpPort));
            return;
        }
    }

    WiFi.mode(WIFI_OFF);
    drawBootScreen("Waiting for activation", "Hold BOOT for 5 seconds", "AP is off");
}

void updateActivationButton()
{
    const uint32_t nowMs = millis();
    const int pressedLevel = MobaDisplay::Board::kBootButtonActiveLow ? LOW : HIGH;
    const bool pressed = digitalRead(MobaDisplay::Board::kBootButtonPin) == pressedLevel;
    if (!pressed)
    {
        ProvisioningRuntime& runtime = provisioningRuntime();
        runtime.buttonPressedAtMs = 0;
        runtime.buttonLongActionHandled = false;
        return;
    }

    ProvisioningRuntime& runtime = provisioningRuntime();
    if (runtime.buttonPressedAtMs == 0)
        runtime.buttonPressedAtMs = nowMs;

    const uint32_t heldMs = nowMs - runtime.buttonPressedAtMs;
    if (!runtime.buttonLongActionHandled && heldMs >= kFactoryResetHoldMs)
    {
        runtime.buttonLongActionHandled = true;
        drawBootScreen("Owner approval required", "Factory reset is protected", "Release BOOT");
        return;
    }

    if (!runtime.buttonLongActionHandled && heldMs >= kActivationHoldMs)
    {
        runtime.buttonLongActionHandled = true;
        startProvisioningWindow();
    }
}

esp_err_t handleProvisioningRequest(uint32_t, const uint8_t* input, ssize_t inputLength,
    uint8_t** output, ssize_t* outputLength, void*)
{
    if (input == nullptr || inputLength < 1 || output == nullptr || outputLength == nullptr)
        return ESP_ERR_INVALID_ARG;

    // Protocomm invokes this handler only after Security 2 has authenticated the
    // client. Mark that boundary explicitly; the read-only state query must not
    // be able to grant authentication as a side effect.
    if (!provisioningRuntime().state.AuthenticateSession())
        return ESP_ERR_INVALID_STATE;

    // RF-02 endpoint requests are deliberately bounded binary messages. The only
    // implemented response is a redacted state snapshot; mutating operations fail
    // closed until the owner-signature verifier is wired to the reviewed client contract.
    if (input[0] != 0)
        return ESP_ERR_NOT_SUPPORTED;

    auto* response = static_cast<uint8_t*>(heap_caps_malloc(4, MALLOC_CAP_8BIT));
    if (response == nullptr)
        return ESP_ERR_NO_MEM;

    const auto& state = provisioningRuntime().state;
    response[0] = StateValue(state.GetState());
    response[1] = state.HasOwner() ? 1 : 0;
    response[2] = state.HasActiveCredentials() ? 1 : 0;
    response[3] = state.AuthenticationFailures();
    *output = response;
    *outputLength = 4;
    return ESP_OK;
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
    pinMode(MobaDisplay::Board::kBootButtonPin,
        MobaDisplay::Board::kBootButtonActiveLow ? INPUT_PULLUP : INPUT_PULLDOWN);

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

    loadOwnerBinding();
    connectWifiOrWaitForActivation();

    if (!udp.begin(kUdpPort))
    {
        drawBootScreen("UDP bind failed", "Port already used?", "");
        ESP.restart();
        return;
    }

    gUdpReady = true;
    Serial.printf("UDP frame port: %u\r\n", static_cast<unsigned>(kUdpPort));
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

void handleUdpPacket(const MobaDisplay::Udp::PacketView& packet)
{
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
        if (linesReceivedCurrentFrame < kTftHeight && gFb)
        {
            uint16_t* row = gFb + (static_cast<uint32_t>(linesReceivedCurrentFrame) * kTftWidth);
            unpackLineBigEndianRgb565IntoRow(pktBuf, row);
            ++linesReceivedCurrentFrame;
        }
        return;
    }

    if (packet.kind != MobaDisplay::Udp::PacketKind::IndexedLine || !gFb)
        return;

    const uint16_t rowIndex = packet.rowIndex;
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

void processUdpPacket()
{
    const int pktSize = udp.parsePacket();
    if (pktSize <= 0)
        return;

    const int cap = pktSize > static_cast<int>(sizeof(pktBuf)) ? static_cast<int>(sizeof(pktBuf)) : pktSize;
    const int rd = udp.read(reinterpret_cast<unsigned char*>(pktBuf), cap);
    if (rd <= 0)
        return;

    while (udp.available() > 0)
        udp.read();

    const MobaDisplay::Udp::PacketView packet = MobaDisplay::Udp::ClassifyPacket(
        pktBuf,
        static_cast<size_t>(rd),
        static_cast<size_t>(pktSize));
    handleUdpPacket(packet);
}

void loop()
{
    updateActivationButton();
    ProvisioningRuntime& runtime = provisioningRuntime();
    runtime.state.Tick(millis());
    const auto state = runtime.state.GetState();
    if (runtime.transport.IsRunning() && !IsProvisioningWindowState(state))
    {
        closeProvisioningWindow(runtime.state.HasActiveCredentials());
    }

    if (!gUdpReady)
    {
        delay(2);
        return;
    }

    processUdpPacket();
}
