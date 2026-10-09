# MOBAdisplay hardware notes

The current PlatformIO target is an ESP32-S3 with an ST7789 display configured
for 240x280 pixels. The authoritative pin and driver settings are in
`MOBAdisplay/esp32/lib/TFT_eSPI/User_Setup.h`.

Current SPI mapping:

| Signal | ESP32-S3 GPIO |
| --- | --- |
| MOSI / DIN | 11 |
| SCLK / CLK | 12 |
| CS | 10 |
| DC | 9 |
| RST | 13 |

The current target assumes a display with a fixed backlight connection; no
`TFT_BL` pin is configured. Do not reuse the older GPIO 23/18/5/2/4 wiring notes
with this firmware without also changing `User_Setup.h`.

## Module and flash layout

The minimum supported module is the ESP32-S3 N16R8 (16 MB flash, 8 MB octal
PSRAM). `platformio.ini` and `sdkconfig.defaults` configure 16 MB flash and the
custom partition table `MOBAdisplay/esp32/partitions.csv`:

| Partition | Offset | Size |
| --- | --- | --- |
| `nvs` | `0x9000` | 24 KB |
| `phy_init` | `0xf000` | 4 KB |
| `factory` (app) | `0x10000` | 4 MB |

`nvs` and `phy_init` keep the ESP-IDF default offsets, so provisioned Wi-Fi
credentials survive reflashing. The flash after the app partition stays
unallocated for later use.

After changing the flash size or partition table, delete the generated
`MOBAdisplay/esp32/sdkconfig.esp32s3` and `MOBAdisplay/esp32/.pio` before the
next build, because PlatformIO keeps the old values otherwise. Flash the
complete bundle, not only the app: `bootloader.bin` at `0x0`, `partitions.bin`
at `0x8000` and `firmware.bin` at `0x10000`. The CI artifact contains these
three files with `flash-offsets.txt` and `flash-bundle.sha256`.

## Current-hardware acceptance record

Use the ESP32-S3 N16R8/ST7789 reference device for the final Issue #36
acceptance. Do not copy results from a simulator or an older firmware image.
Flash the complete bundle of the commit under review, as described above.

Approved thresholds for the sustained-refresh run:

- 2 hours at the normal MOBAflow refresh rate;
- at most 1 percent of the expected frames dropped or rejected;
- zero partially presented frames;
- the device stays negotiated and does not reboot.

The sustained-refresh harness is an explicit hardware test, so CI and normal
test runs never contact the display. Run it from the repository root with the
display on the network:

```text
MOBADISPLAY_IP=<address> dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~SustainedRefreshHardwareTests" -l "console;verbosity=detailed"
```

On Windows PowerShell, set the variable first with `$env:MOBADISPLAY_IP = "<address>"`.
Optional variables are `MOBADISPLAY_PORT` (default `4210`),
`MOBADISPLAY_SOAK_MINUTES` (default `120`) and `MOBADISPLAY_REFRESH_HZ`
(default: the MOBAflow refresh rate). The test prints the report used in item 8
and fails when a threshold is missed; the detailed console logger in the command
keeps that report visible when the test passes. Start it at least one minute after the
display booted. Only a run of at least 2 hours at the normal rate that started
that way ends with `Result: PASSED`; any other run ends with
`PASSED (not an acceptance run)` and is no acceptance evidence. Lost frames are
the expected frames the device did not confirm as presented, each counted once:
skipped timer ticks, host failures and frames the device rejected for good. The
device's rejected-frame counter is shown for information, because it also counts
incomplete transfers that the host repaired. Every protocol negotiation after the first one counts as a session
loss, whichever request showed that the device rejected the session, and the
run fails on any session loss even without a reboot. Watch
the display during the run:
the protocol presents only complete frames, so any torn or partial image is a
failure.

The Display page checks are run once by the maintainer, who starts MOBAflow for
them. They block closing Issue #36 like the hardware items.

Copy this template into an Issue #36 comment and fill in every field. Mark an
item `Fail` with its evidence rather than leaving it out.

```markdown
## Issue #36 acceptance record

### Identity

- Tested Git commit:
- SHA-256 values from `flash-bundle.sha256`:
- Board module marking, flash/PSRAM configuration, display controller:
- Display resolution and pin configuration (`User_Setup.h`):
- Negotiated protocol version, firmware version, device identity, adapter identity:
- Build, flash, monitor, host-test and native-test commands used:
- Harness command and every `MOBADISPLAY_*` value used (the address may be masked):

### Hardware results

| # | Check | Result (Pass/Fail) | Evidence |
| --- | --- | --- | --- |
| 1 | Negotiate from a fresh host session and query health | | |
| 2 | Present the standard host-rendered test pattern | | |
| 3 | Run every advertised optional command; unsupported commands stay disabled | | |
| 4 | Malformed, duplicate, reordered and conflicting packets cause no partial display update | | |
| 5 | An interrupted incomplete transfer keeps the previous frame visible | | |
| 6 | Reconnect after an endpoint/session reset | | |
| 7 | After a device reboot the stale host session is rejected until a new negotiation succeeds | | |
| 8 | Sustained refresh meets the approved thresholds (paste the harness report below) | | |

Harness report:

    (paste the test output from "Duration:" to "Result:")

Partially presented frames observed during the run (expected: none):

### Display page checks (MOBAflow)

| Check | Result (Pass/Fail) | Notes |
| --- | --- | --- |
| Light theme | | |
| Dark theme | | |
| High Contrast theme | | |
| Keyboard-only operation | | |
| Visible focus on every interactive element | | |
| Narrator reads names, states and messages | | |
```

Issue #36 remains open until this record contains evidence from the current
hardware and the exact firmware image under review.
