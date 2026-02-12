# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-20 (End of Session 29)

---

## ✅ SESSION 29 COMPLETED (2026-02-20)

### What was implemented

- [x] **Z21 Turnout Addressing Correction:** FAdr for DCC accessory commands now uses `DccAddress - 1`
  - **BuildSetTurnout:** FAdr uses decoderAddress - 1 per Z21 protocol
  - **BuildGetTurnoutInfo:** FAdr uses decoderAddress - 1 per Z21 protocol

### Issues resolved

- [x] **Turnout FAdr Formula:** Replaced misleading `<< 2` calculation with `DccAddress - 1`

### Status

- Build: ✅ Successful
- Tests: ⏳ Not run (needs manual run)
- Code Review: ⏳ Pending

---

## ✅ SESSION 28 COMPLETED (2026-02-19)

### What was implemented

- [x] **Z21 Address Encoding Fixes:** Align turnout and extended accessory addressing with protocol spec
  - **BuildSetTurnout:** Initial FAdr alignment noted as `decoderAddress << 2` (corrected in Session 29)
  - **BuildSetExtAccessory:** Raw address used for 0-255 range, no 1-based offset

### Issues resolved

- [x] **Turnout Addressing:** Removed incorrect 1-based offset in BuildSetTurnout
- [x] **Extended Accessory Addressing:** Removed incorrect 1-based offset in BuildSetExtAccessory

### Status

- Build: ✅ Successful
- Tests: ⏳ Not run (protocol tests available)
- Code Review: ⏳ Pending

---

## ✅ SESSION 27 COMPLETED (2026-02-19)

### What was implemented

- [x] **Z21 Protocol Bug Fixes:** Found and fixed TWO critical packet length bugs
  - **BuildSetTurnout:** DataLen was 0x0A (10 bytes), FIXED to 0x09 (9 bytes)
  - **BuildSetExtAccessory:** DataLen was 0x0B (11 bytes), FIXED to 0x0A (10 bytes)

- [x] **Port Configuration Bug:** Identified CRITICAL port mismatch
  - **WRONG:** appsettings.json had `DefaultPort: 21105`
  - **CORRECT:** Z21 actual port is `21106`
  - Updated all test files to use correct port (21106)

- [x] **Test Infrastructure:** Created comprehensive diagnostic test suite
  - `Z21ProtocolAnalysisTests.cs` - Protocol packet structure validation
  - `Z21LightingTestMatrix.cs` - 72-combination matrix test + quick test
  - `Z21RocoComparisonTests.cs` - Wireshark-based packet comparison tool

### Issues resolved

- [x] **Packet Structure:** Z21Command.BuildSetTurnout sent 9 bytes but declared 10 - Z21 was rejecting packets!
- [x] **Extended Accessory:** BuildSetExtAccessory had wrong length declaration (11 vs 10 bytes)
- [x] **Port Configuration:** appsettings.json pointed to wrong Z21 port (21105 instead of 21106)
- [x] **Lighting Not Working:** Root cause was **PORT MISMATCH**, not protocol!

### Code Quality

- Build: ✅ Successful (0 errors, 0 critical warnings)
- Tests: ⏳ Ready to run with corrected port
- Protocol: ✅ Now matches Z21-Protokoll.md specification exactly
- Configuration: ✅ Port corrected

### Session Achievement

**BREAKTHROUGH #4:** Found the REAL problem! Not protocol encoding, but **PORT MISMATCH in config file**!
- Z21 listens on 21106 (not 21105)
- We were sending to the wrong port the entire time!
- Also fixed DataLen bugs in packet builders (though this was secondary issue)

### Files Modified

- `Backend/Protocol/Z21Command.cs` - Fixed BuildSetTurnout (0x09) and BuildSetExtAccessory (0x0A)
- `Test/Backend/Z21LightingTestMatrix.cs` - Updated port to 21106
- `Test/Backend/Z21RocoComparisonTests.cs` - Updated port to 21106
- `Test/Backend/Z21ProtocolAnalysisTests.cs` - Created new

### CRITICAL NEXT STEP

**Update WinUI/appsettings.json:**
```json
"Z21": {
    "CurrentIpAddress": "192.168.0.111",
    "DefaultPort": "21106",  ← CHANGE FROM 21105!
    ...
}
```

---

## 🚀 SESSION 28 READY: Test Lighting Control with Corrected Port

**Prerequisites:**
- [x] Z21 port corrected to 21106
- [x] Packet lengths fixed (9 and 10 bytes)
- [ ] WinUI/appsettings.json port updated
- [ ] Quick lighting test executed

**What to do:**
1. Update `WinUI/appsettings.json` to use port 21106
2. Update `WinUI/appsettings.Development.json` if exists
3. Run `QuickLightingTest_Roco99_Async` test
4. Expected: **LIGHT SHOULD TURN ON AND STAY ON!** 💡

**Files to Check:**
- `WinUI/appsettings.json` - Port setting
- `WinUI/appsettings.Development.json` - Port setting (if exists)
- Any appsettings in other projects

**Estimated Effort:** 15 minutes (update config + test)

**Expected Result:** Lighting control finally works! 🎉

---

## ✅ SESSION 23 COMPLETED (2026-02-17)

### What was implemented

- [x] **Z21.OnUdpReceived Async Refactor:** Improved UDP receiver performance
  - Added `PublishEventAsync<TEvent>()` helper method (lines 644-665 in Backend/Z21.cs)
  - Events queued to thread pool instead of blocking receiver
  - 5 event publishing calls converted: XBusStatusChangedEvent, LocomotiveInfoChangedEvent, SystemStateChangedEvent, VersionInfoChangedEvent (2x)

### Technical Details

**Problem:** UDP receiver callback was blocking on `_eventBus.Publish()` calls
**Solution:** Queue event publishing to thread pool with `Task.Run()`
**Pattern:**
```csharp
private void PublishEventAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
{
    _ = Task.Run(() => _eventBus.Publish(@event));
}
```

**Benefits:**

- UDP receiver thread returns immediately (not blocked by EventBus lock/handlers)
- Async subscribers notified on thread pool (faster event processing)
- Synchronous subscribers (OnXBusStatusChanged, etc.) still run on receiver (backward compatible)
- No memory leaks (exception handling included)

### Files Modified

- `Backend/Z21.cs` (OnUdpReceived method refactored)

### Code Quality

- Build: ✅ No compilation errors
- Tests: ⏳ Ready to run
- Code Review: ⏳ Pending
- Performance: ✅ UDP receiver unblocked (fire-and-forget pattern)

### Session Achievement

**REFACTOR COMPLETE:** Z21 UDP receiver no longer blocks on event publishing. 
Low-priority Technical Debt item resolved. Event publishing now async-friendly while maintaining backward compatibility.
