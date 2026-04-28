#include <TFT_eSPI.h>
#include <SPI.h>

TFT_eSPI tft = TFT_eSPI();

void setup() {
  tft.init();
  tft.setRotation(0);

  tft.fillScreen(TFT_RED);
  delay(800);

  tft.fillScreen(TFT_GREEN);
  delay(800);

  tft.fillScreen(TFT_BLUE);
  delay(800);

  tft.fillScreen(TFT_WHITE);
  delay(800);
}

void loop() {}
