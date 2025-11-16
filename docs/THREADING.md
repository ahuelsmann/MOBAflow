# 🧵 MAUI Threading Guidelines (CRITICAL!)

⚠️ **IMPORTANT**: MAUI has strict threading requirements on mobile platforms (Android, iOS)!

---

## **The Golden Rule:**
**Only the Main Thread (UI Thread) can modify UI properties!**

| Thread Type | Can Modify UI? | Example |
|-------------|----------------|---------|
| **Main Thread (UI Thread)** | ✅ YES | `stat.Count++` (if `Count` is `ObservableProperty`) |
| **Background Thread** | ❌ NO → CRASH | Network callbacks, UDP events, timers |

---

## **Common Crash Scenarios:**

### ❌ **BAD: Background thread modifying ObservableProperty**
```csharp
// Called from UDP callback (background thread)
private void OnDataReceived(Data data)
{
    StatusText = "Received!";  // ❌ CRASH: Android Looper error!
    Items.Add(data);            // ❌ CRASH: ObservableCollection not thread-safe!
}
```

### ✅ **GOOD: Dispatch to Main Thread**
```csharp
private void OnDataReceived(Data data)
{
    // ✅ Dispatch all UI updates to Main Thread
#if ANDROID || IOS || MACCATALYST || WINDOWS
    MainThread.BeginInvokeOnMainThread(() =>
    {
        StatusText = "Received!";  // ✅ Safe on UI thread
        Items.Add(data);            // ✅ Safe on UI thread
    });
#else
    // Fallback for unit tests
    StatusText = "Received!";
    Items.Add(data);
#endif
}
```

---

## **When to Use MainThread Dispatching:**

| Scenario | Requires Dispatch? |
|----------|-------------------|
| **Network callbacks** (UDP, TCP, HTTP) | ✅ YES |
| **Timer callbacks** (`System.Timers.Timer`) | ✅ YES |
| **Background tasks** (`Task.Run`) | ✅ YES |
| **Event handlers** (button clicks) | ❌ NO (already on UI thread) |
| **ViewModel constructors** | ❌ NO (already on UI thread) |

---

## **Error Signatures (Android):**

```
❌ RuntimeException: Can't create handler inside thread that has not called Looper.prepare()
❌ IllegalStateException: This collection cannot be modified from a background thread
❌ CalledFromWrongThreadException: Only the original thread that created a view hierarchy can touch its views
```

**Solution:** Wrap UI updates in `MainThread.BeginInvokeOnMainThread(() => { ... })`

---

## **Best Practices:**

### 1. **Identify Background Threads:**
- Network callbacks (UDP, TCP, SignalR)
- Timer events (`System.Timers.Timer`)
- `Task.Run()` continuations

### 2. **Dispatch UI Updates:**
```csharp
#if ANDROID || IOS || MACCATALYST || WINDOWS
    MainThread.BeginInvokeOnMainThread(() =>
    {
        // UI updates here
    });
#else
    // Fallback for unit tests
#endif
```

### 3. **Keep Non-UI Work Off Main Thread:**
```csharp
private void OnDataReceived(Data data)
{
    // ✅ Heavy processing on background thread
    var processed = ProcessData(data);
    
    // ✅ UI update on main thread
    MainThread.BeginInvokeOnMainThread(() =>
    {
        StatusText = $"Processed: {processed}";
    });
}
```

### 4. **Don't Block UI Thread:**
```csharp
// ❌ BAD: Blocks UI thread
MainThread.InvokeOnMainThreadAsync(async () =>
{
    await Task.Delay(5000);  // UI frozen for 5 seconds!
}).Wait();

// ✅ GOOD: Async dispatch
MainThread.BeginInvokeOnMainThread(() =>
{
    // Quick UI update only
});
```

---

## **Platform Differences:**

| Platform | Threading Model | Strictness |
|----------|----------------|------------|
| **Android** | **Looper-based** | ⚠️ **VERY STRICT** (crashes immediately) |
| **iOS** | **RunLoop-based** | ⚠️ **VERY STRICT** (crashes immediately) |
| **Windows** | **Dispatcher-based** | ⚠️ **Moderate** (may allow, but unreliable) |
| **Unit Tests** | **None** | ✅ **Lenient** (no UI thread enforcement) |

---

## **Real-World Example: Z21 UDP Callback**

```csharp
// Backend.Z21 raises event on UDP thread
_z21.Received += OnFeedbackReceived;

// ❌ WRONG: Direct UI update
private void OnFeedbackReceived(Backend.FeedbackResult result)
{
    stat.Count++;  // ❌ CRASH on Android!
}

// ✅ CORRECT: Dispatched UI update
private void OnFeedbackReceived(Backend.FeedbackResult result)
{
#if ANDROID || IOS || MACCATALYST || WINDOWS
    MainThread.BeginInvokeOnMainThread(() =>
    {
        var stat = Statistics.FirstOrDefault(s => s.InPort == result.InPort);
        if (stat != null)
        {
            stat.Count++;  // ✅ Safe on UI thread
        }
    });
#else
    // Unit test fallback
    var stat = Statistics.FirstOrDefault(s => s.InPort == result.InPort);
    if (stat != null) stat.Count++;
#endif
}
```

---

## **Debugging Tips:**

### 1. **Enable Thread Names:**
```csharp
System.Diagnostics.Debug.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId}");
```

### 2. **Check If On Main Thread:**
```csharp
if (!MainThread.IsMainThread)
{
    throw new InvalidOperationException("Must be called on UI thread!");
}
```

### 3. **Android LogCat Filter:**
```bash
adb logcat | grep -E "Looper|CalledFromWrongThread|RuntimeException"
```

---

## **Related Guidelines:**
- See [ASYNC-PATTERNS.md](ASYNC-PATTERNS.md) for async patterns
- See [ARCHITECTURE.md](../ARCHITECTURE.md) for platform-specific ViewModels
- See `.copilot-instructions.md` → **MVVM Pattern**
