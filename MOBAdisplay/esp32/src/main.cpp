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
const int FRAME_SIZE = WIDTH * HEIGHT * 2;

uint8_t framebuffer[FRAME_SIZE];
int framePos = 0;

void displayFrame()
{
    tft.startWrite();
    tft.setAddrWindow(0, 0, WIDTH, HEIGHT);
    tft.pushPixels((uint16_t*)framebuffer, WIDTH * HEIGHT);
    tft.endWrite();
}

void setup()
{
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

    udp.begin(udpPort);
}

void loop()
{
    int packetSize = udp.parsePacket();
    if (packetSize > 0) {
        char header[16];
        int headerLen = udp.read(header, sizeof(header));

        if (strncmp(header, "FRAME_DONE", 10) == 0) {
            displayFrame();
            framePos = 0;
            return;
        }

        udp.read(framebuffer + framePos, packetSize);
        framePos += packetSize;

        if (framePos > FRAME_SIZE) {
            framePos = 0;
        }
    }
}
