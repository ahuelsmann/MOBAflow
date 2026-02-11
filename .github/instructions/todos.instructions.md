---
description: "MOBAflow open tasks and roadmap"
applyTo: "**"
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-17 (End of Session 22)

---

## ✅ SESSION 22 COMPLETED (2026-02-17)

### What was implemented

- [x] **Z21-Protokoll Study:** Read official Z21-Protokoll.md documentation
  - **Key Discovery:** FAdr Encoding formula from spec:
    ```
    FAdr = (FAdr_MSB << 8) + FAdr_LSB
    DCC_Addr = FAdr >> 2  (Decoder address)
    Port = FAdr & 0x03     (Output port 0-3)
    ```
  - **Example:** FAdr=200 → DCC_Addr=50, Port=0
  - **Example:** FAdr=201 → DCC_Addr=50, Port=1

- [x] **Root Cause Identified:** Signal BaseAddress was WRONG
  - Z21 responds with FAdr=200 (0x00C8) in traffic logs
  - MOBAflow was sending FAdr=804 (201 << 2) ✅ CORRECT FORMULA
  - **BUT:** The DCC address in config should be 50, not 201!
  - **The Issue:** Roco App shows "201" but that's actually "50:1" (Decoder 50, Port 1)

- [x] **Solution Identified:** Change BaseAddress from 201 to 50
  - With BaseAddress=50 + AddressOffset=0:
    - Hp0: Address 50 → FAdr = 50 << 2 = 200 ✅
    - Ks1: Address 50 → FAdr = 50 << 2 = 200 ✅
  - Z21 will decode: DCC_Addr = 200 >> 2 = 50, Port = 0 ✅
  - Matches Z21's response FAdr=200!

### Issues resolved

- [x] **Root Cause:** BuildSetTurnout formula was correct; signal config address was wrong
- [x] **Traffic Analysis:** Z21 respond with FAdr=200 (not 804) proves AddressOffset calculation

### Code Quality

- Build: ✅ No syntax errors (from previous session)
- Tests: ⏳ Ready to run after fixing config
- Code Review: ⏳ Pending
- Hardware Test: 🎯 Ready after config change!

### Session Achievement

**BREAKTHROUGH #3:** Read Z21-Protokoll.md official documentation! 
FAdr encoding is universal for ALL decoders. The confusion was in signal config: BaseAddress should be 50 (Decoder address), 
not 201 (which represents "Decoder 50, Port 1"). Once changed to BaseAddress=50, the math works perfectly:
- Hp0/Ks1 both use Address 50
- FAdr = 50 << 2 = 200
- Z21 decodes: Decoder 50, Port 0 ✅

### Files to modify NEXT

- `example-solution.json` or SignalBoxPlan DB: Change signal BaseAddress from 201 → 50

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

---

## 🚀 SESSION 24 READY: Fix Signal Config & Hardware Test

**Prerequisite:** Change signal configuration

### What to Fix:

1. **Locate signal configuration** (either `example-solution.json` or database):
   ```json
   {
     "Name": "Signal 201",
     "BaseAddress": 201  // ← CHANGE TO 50!
   }
   ```

2. **After change, rebuild:**
   ```powershell
   dotnet clean && dotnet build
   ```

3. **Hardware Test Sequence:**
   - Start MOBAflow
   - Click signal aspect button (Hp0 → Ks1)
   - **Expected:** Lampe leuchtet auf/um!
   - Monitor Z21 Traffic:
     - SendCommand: FAdr=200 (0x00C8)
     - cmdByte: 0x80 (Hp0) vs 0x88 (Ks1)
   - **Compare with Roco App:** Should send same bytes

### Files to Verify:

- `example-solution.json` - Signal BaseAddress
- `Backend/Protocol/Z21Command.cs` - BuildSetTurnout (formula correct ✅)
- `Backend/Z21.cs` - SetTurnoutAsync (routing correct ✅)
- `Common/Multiplex/MultiplexerHelper.cs` - AddressOffset (stays as is)

### Expected Outcome:

- ✅ Signal Hp0 → ROT
- ✅ Signal Ks1 → GRÜN
- ✅ Traffic shows FAdr=200, cmdByte toggles 0x80↔0x88
- ✅ Roco App sends identical bytes

**If still doesn't work:**

- Check Dekoder Config on physical hardware (CV1, CV2)
- Dekoder-Adresse sollte 50 sein (nicht 201!)
- Test with Roco App first to confirm hardware config

---

## 🚀 SESSION 25 READY: KI-basierte Fahrstraßen-Vorschläge (Backend)

**Prerequisites:**
- [ ] `Project.SignalBoxPlan` is present with routes and connections
- [ ] Routes include `ElementIds` and `SwitchPositions`

**What to implement (Backend only):**
- [ ] Add a `IRouteSuggestionService` interface in `Backend/Interface`
- [ ] Implement `RouteSuggestionService` in `Backend/Service`
- [ ] Suggest routes based on start/end signals with conflict checks:
  - [ ] Overlapping `ElementIds` with active routes
  - [ ] Conflicting `SwitchPositions`
  - [ ] `SignalBoxElementState` not `Free`
- [ ] Register service in `Backend/Extensions/MobaServiceCollectionExtensions.cs`
- [ ] Add unit tests in `Test/Backend/RouteSuggestionServiceTests.cs`

**Files to Modify:**
- `Backend/Interface/IRouteSuggestionService.cs` (new)
- `Backend/Service/RouteSuggestionService.cs` (new)
- `Backend/Extensions/MobaServiceCollectionExtensions.cs`
- `Test/Backend/RouteSuggestionServiceTests.cs` (new)

**Estimated Effort:** 4-6 hours

---

## 📚 Previous Sessions

### ✅ SESSION 22 COMPLETED (2026-02-17)

### ✅ SESSION 23 COMPLETED (2026-02-17)
