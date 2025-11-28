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

## 🚦 Target Frameworks

Backend MUST target:
- `net10.0` only (no platform-specific TFMs)

**Never add:**
- `net10.0-windows`
- `net10.0-android`
- `net10.0-ios`
