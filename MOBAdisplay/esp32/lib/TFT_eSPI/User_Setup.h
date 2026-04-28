// ============================================================
//  User Setup for ESP32-S3 + Waveshare ST7789V2 (240x280)
// ============================================================

// ---- ESP32-S3 aktivieren ----
#define ESP32
#define ESP32_S3

// Silence optional-feature warnings (touch, etc.).
#define DISABLE_ALL_LIBRARY_WARNINGS

// ---- Display-Treiber ----
#define ST7789_DRIVER

// ---- Display-Auflösung ----
#define TFT_WIDTH  240
#define TFT_HEIGHT 280

// ---- Farbmodus ----
#define TFT_RGB_ORDER TFT_BGR
#define TFT_INVERSION_ON

// ---- Pinbelegung für dein Board ----
#define TFT_MOSI 11
#define TFT_SCLK 12
#define TFT_CS   10
#define TFT_DC    9
#define TFT_RST  13

// ---- Backlight (falls vorhanden) ----
// Waveshare ST7789V2 hat meist festen Backlight-Pin → kein BL-Pin nötig
// #define TFT_BL 38
// #define TFT_BACKLIGHT_ON HIGH

// ---- SPI Geschwindigkeit ----
#define SPI_FREQUENCY  40000000
#define SPI_READ_FREQUENCY 20000000
#define SPI_TOUCH_FREQUENCY 2500000

// ---- Built-in fonts only (no LOAD_GFXFF: smaller flash, no Fonts/GFXFF) ----
#define LOAD_GLCD
#define LOAD_FONT2
#define LOAD_FONT4
#define LOAD_FONT6
#define LOAD_FONT7
#define LOAD_FONT8
