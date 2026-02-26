# Viessmann Multiplex Signals (5229) – Aspect Mapping

**Version:** 1.0  
**Scope:** Mapping SignalBoxPage signal aspects to Viessmann 5229 DCC addresses  
**Status:** Production  
**Last Updated:** 2026-02-01

---

This page describes how the **SignalBoxPage** (signal box) maps signal aspects to a real Viessmann multiplex signal (decoder 5229) and how to find the correct configuration.

## Overview

- The signal uses **4 DCC addresses** (e.g. 201, 202, 203, 204).
- Each address is driven as a **turnout command** (Z21 `LAN_X_SET_TURNOUT`): **address**, **output** (0 or 1), **Activate** (true/false).
- The mapping “signal aspect → (DCC address, output, activate)” is defined in code in `Common/Multiplex/MultiplexerHelper.cs` (5229 definition).

## Current Mapping (Base Address 201, Ks Multi-Section Signal 4046)

| Signal aspect (UI) | DCC address | Output | Activate | Note |
|--------------------|-------------|--------|----------|------|
| **Hp0** (red)      | 201         | 0      | No       | Stop |
| **Ks1** (green)    | 201         | 0      | Yes      | Proceed |
| **Ks2** (yellow)   | 203         | 0      | No       | Expect stop |
| **Ks1 blinking**   | 202         | 0      | Yes      | Proceed with speed indication |
| **Ra12**           | 202         | 0      | No       | Shunting movement |

Additional aspects (marker light, dark, Zs1, Zs7) are currently not mapped for the 4046.

**Speed indicators (numeric variants):**  
The multiplex signal can have two additional numeric indicators:
- **Top (white digit):** Maximum speed in km/h that applies **from now on** (e.g. 8 = 80 km/h).
- **Bottom (yellow digit):** Maximum speed in km/h that applies **from the next signal** (e.g. 6 = 60 km/h).

These variants can be added once the exact DCC addresses/outputs for the speed indicators are known from the Viessmann documentation.

## How to Find the Correct Configuration

1. **Check base address**  
   The 5229 decoder has a start address (e.g. 201). In SignalBoxPage, in the “MultipLEX-SIGNAL CONFIGURATION” section of the signal, set the **base DCC address** to the same value (e.g. 201).

2. **Inspect the command being sent**  
   After clicking a signal aspect, the status bar at the bottom shows e.g.:  
   `Signal: Ks1 / DCC address: 201, Output: 0, Activate: Yes`.  
   In the main status line you may see:  
   `Signal '…' set: DCC address 201, Output 0, Activate=True`.  
   This tells you exactly which turnout command is being sent.

3. **Invert polarity per address**  
   If an aspect appears swapped on the model (e.g. green/red swapped on address 201), invert the **polarity** for that address:
   - **Settings → “Signal box / Viessmann signals”** → enable “Invert polarity for address X” for the affected address (4 checkboxes for the 4 consecutive addresses, e.g. 201, 202, 203, 204).
   - Or in `appsettings.json` under `signalBox`:
     ```json
     "signalBox": {
       "invertPolarityOffset0": true,
       "invertPolarityOffset1": false,
       "invertPolarityOffset2": false,
       "invertPolarityOffset3": false
     }
     ```
   - Each of the 4 addresses can be inverted individually (Offset 0 = first address, e.g. 201; Offset 1 = 202; etc.).

4. **Cross-check with the Viessmann manual**  
   The Viessmann 5229 manual documents which DCC address controls which aspect. If your model differs (e.g. different signal model 4040/4042/4046), the address offset or activate logic can be different. Always refer to the current Viessmann documentation for the exact table.

5. **Extend for additional aspects (e.g. with speed indicators)**  
   Once you know the exact addresses/outputs for additional aspects, they can be added to the 5229 definition in `MultiplexerHelper.cs` (and optionally to the UI).

## Technical Notes (for Developers)

- **Turnout command:** Z21 `LAN_X_SET_TURNOUT` (0x53); `FAdr = DCC address − 1`; each address has two outputs (P = 0/1), each with Activate on/off.
- **Calculation:** `DCC address = base address + addressOffset` from the multiplexer definition.
- **Invert:** `SignalBox.InvertPolarityOffset0` … `InvertPolarityOffset3` – for each address (offset 0–3) the Activate bit can be inverted individually.

## See Also

- Signal box page (`SignalBoxPage`) in the MOBAflow app
- `Common/Multiplex/MultiplexerHelper.cs` – 5229 definition and mapping
- `docs/wiki/INDEX.md` – documentation index
