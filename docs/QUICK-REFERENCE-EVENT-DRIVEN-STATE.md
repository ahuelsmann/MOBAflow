# Event-Driven State Management - Quick Reference

## ❌ Anti-Pattern: Manual State Override

```csharp
// DON'T DO THIS!
[RelayCommand]
private async Task SetTrackPowerAsync(bool turnOn)
{
    await _z21.SetTrackPowerOffAsync();
    IsTrackPowerOn = false;
    
    // ❌ Manual reset creates race conditions!
    MainCurrent = 0;
    Temperature = 0;
    SupplyVoltage = 0;
    // What if Z21 sends update AFTER this?
}
```

**Problems:**
- Race conditions
- Timing issues
- Violates Single Source of Truth
- Hard to test

## ✅ Correct Pattern: Event-Driven Filter

```csharp
// DO THIS INSTEAD!
[RelayCommand]
private async Task SetTrackPowerAsync(bool turnOn)
{
    if (turnOn)
    {
        await _z21.SetTrackPowerOnAsync();
        StatusText = "Track power ON";
    }
    else
    {
        await _z21.SetTrackPowerOffAsync();
        StatusText = "Track power OFF";
        // ✅ No manual state reset!
    }
}

// ✅ Filter events in event handler
private void UpdateSystemState(Backend.SystemState systemState)
{
    IsTrackPowerOn = systemState.IsTrackPowerOn;

    if (systemState.IsTrackPowerOn)
    {
        // Power ON → Show real values
        MainCurrent = systemState.MainCurrent;
        Temperature = systemState.Temperature;
        SupplyVoltage = systemState.SupplyVoltage;
    }
    else
    {
        // Power OFF → Reset to zero
        MainCurrent = 0;
        Temperature = 0;
        SupplyVoltage = 0;
    }
}
```

**Benefits:**
- No race conditions
- Deterministic behavior
- Single Source of Truth
- Easy to test

## 🎯 Key Principles

1. **Single Source of Truth**
   - External system (Z21) owns the state
   - ViewModel reflects state via events

2. **Events Only**
   - ViewModel reacts to events
   - Never manually override state

3. **Filter, Don't Override**
   - Apply business logic in event handlers
   - Don't guess values in commands

4. **Commands Trigger Actions**
   - Commands send requests
   - Don't set state directly

## 📊 Comparison Table

| Aspect | Manual Override | Event Filter |
|--------|----------------|--------------|
| Responsibility | ViewModel guesses | Only Z21 sets values |
| Race Condition | Possible ❌ | Impossible ✅ |
| Consistency | Can be overwritten | Always consistent |
| Timing | Unpredictable | Deterministic |
| Testability | Hard | Easy |

## 🔄 Execution Flow

```
User Action → Command Executes
              ↓
         Z21 Receives Request
              ↓
         Z21 Changes State
              ↓
         Z21 Sends Event
              ↓
       Event Handler Filters
              ↓
         UI Updates ✅
```

## 📖 See Also

- `docs/SESSION-SUMMARY-2025-12-10-UI-IMPROVEMENTS.md`
- `.github/instructions/backend.instructions.md` (Event-Driven State Management)
- `.github/copilot-instructions.md` (Past Mistakes #3)
