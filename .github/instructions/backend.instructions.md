---
description: Backend must remain 100% platform-independent - no UI framework references
applyTo: "Backend/**/*.cs"
---

# Backend Platform Independence Rules

## 🚨 CRITICAL: Backend MUST Stay Platform-Independent

**This applies to ALL files in `Backend/`**

### ❌ FORBIDDEN in Backend

```csharp
// ❌ NEVER: Platform-specific threading
#if WINDOWS
await DispatchToUIThreadAsync(...);
#endif

// ❌ NEVER: UI framework references
using Microsoft.UI.Dispatching;
using Microsoft.Maui.Controls;
MainThread.BeginInvokeOnMainThread(...);

// ❌ NEVER: Platform-specific APIs
DispatcherQueue.TryEnqueue(...);
Application.Current.Dispatcher.Invoke(...);
```

### ✅ ALLOWED in Backend

```csharp
// ✅ Standard .NET APIs
Task.Run(async () => { ... });
await Task.Delay(...);

// ✅ Events for UI notification
public event EventHandler<DataChangedEventArgs> DataChanged;

// ✅ Interfaces for I/O abstraction
private readonly IUdpClientWrapper _udpClient;
private readonly IZ21 _z21;
```

## 🎯 Pattern: Events Instead of Dispatching

```csharp
// ✅ CORRECT: Raise event, let UI handle threading
public class JourneyManager
{
    public event EventHandler<JourneyChangedEventArgs> JourneyChanged;
    
    private void OnDataReceived()
    {
        // Backend just raises event
        JourneyChanged?.Invoke(this, new JourneyChangedEventArgs(...));
    }
}

// Platform-specific ViewModel handles dispatching
public class WinUIJourneyViewModel
{
    private readonly DispatcherQueue _dispatcher;
    
    private void OnJourneyChanged(object sender, JourneyChangedEventArgs e)
    {
        // WinUI handles UI thread dispatching
        _dispatcher.TryEnqueue(() => UpdateUI());
    }
}
```

## 🔒 I/O Abstraction

All external I/O MUST use interfaces:

```csharp
// ✅ CORRECT: Interface-based I/O
public interface IUdpClientWrapper
{
    Task SendAsync(byte[] data);
    event EventHandler<DataReceivedEventArgs> DataReceived;
}

public class Z21
{
    private readonly IUdpClientWrapper _udpClient;
    
    public Z21(IUdpClientWrapper udpClient)
    {
        _udpClient = udpClient; // ✅ DI-injected
    }
}
```

## 📋 Checklist

When modifying Backend code:

- [ ] No `#if WINDOWS`, `#if ANDROID`, `#if IOS`
- [ ] No `using Microsoft.UI.*` or `using Microsoft.Maui.*`
- [ ] No `DispatcherQueue`, `MainThread`, `Dispatcher`
- [ ] All I/O uses interfaces (`IUdpClientWrapper`, `IZ21`)
- [ ] Events instead of callbacks for notifications
- [ ] Async/await for all I/O operations
- [ ] No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`

## 🎯 Event-Driven State Management Pattern

### Anti-Pattern: Manual State Override in Commands

```csharp
// ❌ WRONG: Manual state override (race condition!)
[RelayCommand]
private async Task SetTrackPowerAsync(bool turnOn)
{
    await _z21.SetTrackPowerOffAsync();
    IsTrackPowerOn = false;
    
    // ❌ Manual reset creates timing issues:
    MainCurrent = 0;      // What if Z21 sends update after this?
    Temperature = 0;      // Race condition!
    SupplyVoltage = 0;    // Values may be overwritten by late events
}
```

**Problems:**
- **Race Condition:** Z21 could send `SystemState` update after manual reset
- **Timing Issues:** Order of execution depends on network latency
- **Violation of Single Source of Truth:** ViewModel "guesses" values instead of reading from Z21
- **Hard to Test:** Non-deterministic behavior

### Correct Pattern: Filter Events Based on State

```csharp
// ✅ CORRECT: Event-driven filtering
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
        // ✅ No manual state reset - values come from Z21 events
    }
}

// ✅ Single source of truth: Z21 events set all values
private void UpdateSystemState(Backend.SystemState systemState)
{
    // Update track power status first
    IsTrackPowerOn = systemState.IsTrackPowerOn;

    // Filter values based on power state
    if (systemState.IsTrackPowerOn)
    {
        // Power ON → Show real values
        MainCurrent = systemState.MainCurrent;
        Temperature = systemState.Temperature;
        SupplyVoltage = systemState.SupplyVoltage;
        VccVoltage = systemState.VccVoltage;
        CentralState = $"0x{systemState.CentralState:X2}";
        CentralStateEx = $"0x{systemState.CentralStateEx:X2}";
    }
    else
    {
        // Power OFF → Reset to zero (no stale values)
        MainCurrent = 0;
        Temperature = 0;
        SupplyVoltage = 0;
        VccVoltage = 0;
        CentralState = "0x00";
        CentralStateEx = "0x00";
    }
}
```

### Why This is Better

| Aspect | Manual Override ❌ | Event Filter ✅ |
|--------|-------------------|-----------------|
| **Responsibility** | ViewModel guesses values | Only Z21 sets values |
| **Race Condition** | Possible | Impossible |
| **Consistency** | Values can be overwritten | Always consistent |
| **Timing** | Depends on command order | Event-based (deterministic) |
| **Testability** | Hard to test timing | Easy to test (predictable) |

### Execution Flow (Event-Driven)

```
1. User clicks "Track Power OFF"
   ↓
2. SetTrackPowerAsync(false) executes
   ↓
3. Z21 receives command → Turns power OFF
   ↓
4. Z21 sends SystemState update (IsTrackPowerOn = false)
   ↓
5. OnSystemStateChanged event fires
   ↓
6. UpdateSystemState() filters values based on IsTrackPowerOn
   ↓
7. UI updates → Displays 0 values ✅
```

### Key Principles

1. **Single Source of Truth:** External system (Z21) owns the state
2. **Events Only:** ViewModel reacts to events, never overrides state
3. **Filter, Don't Override:** Apply business logic in event handlers
4. **Commands Trigger Actions:** Commands send requests, don't set state directly
5. **Deterministic Testing:** Event-driven flow is predictable and testable

---

## 🚦 Target Frameworks

Backend MUST target:
- `net10.0` only (no platform-specific TFMs)

**Never add:**
- `net10.0-windows`
- `net10.0-android`
- `net10.0-ios`
