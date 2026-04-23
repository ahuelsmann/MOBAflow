#include <WiFi.h>
#include <WiFiUdp.h>
#include <TFT_eSPI.h>

TFT_eSPI tft = TFT_eSPI();

const char* ssid     = "vWLAN";
const char* password = "Kuhiksqx#1177";

WiFiUDP udp;
const int udpPort = 4210;

const int WIDTH = 240;
const int HEIGHT = 280;

// Buffer für EINE Zeile (240 Pixel × 2 Byte)
uint8_t lineBuffer[WIDTH * 2];

int currentLine = 0;

void setup() {
    Serial.begin(115200);
    delay(500);

    pinMode(22, OUTPUT);
    digitalWrite(22, HIGH);

    tft.init();
    tft.setRotation(0);
    tft.fillScreen(TFT_BLACK);

    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, password);

    while (WiFi.status() != WL_CONNECTED) {
        delay(300);
        Serial.print(".");
    }

    Serial.println("\nWLAN verbunden");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());

    udp.begin(udpPort);
}

void loop() {
    int packetSize = udp.parsePacket();
    if (packetSize <= 0) return;

    // Header lesen
    char header[16];
    int headerLen = udp.read(header, sizeof(header));

    // FRAME_START → neue Frame beginnt
    if (strncmp(header, "FRAME_START", 11) == 0) {
        currentLine = 0;
        return;
    }

    // FRAME_DONE → nichts tun
    if (strncmp(header, "FRAME_DONE", 10) == 0) {
        return;
    }

    // Normale Zeilendaten
    if (packetSize == WIDTH * 2) {
        udp.read(lineBuffer, WIDTH * 2);

        tft.startWrite();
        tft.setAddrWindow(0, currentLine, WIDTH, 1);
        tft.pushPixels((uint16_t*)lineBuffer, WIDTH);
        tft.endWrite();

        currentLine++;
        if (currentLine >= HEIGHT) currentLine = 0;
    }
}
