# 🏗️ MOBAflow Architecture Guidelines

## 🎯 Core Principle: Keep Backend Platform-Independent

**MANDATORY RULE**: The `Backend` project must remain **100% platform-independent** (no UI thread dependencies)!

---

## 📐 Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    UI Layer (Platform-Specific)              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   WinUI      │  │     MAUI     │  │   WebApp     │      │
│  │  (Windows)   │  │  (Android)   │  │  (Blazor)    │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┴──────────────────┘              │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│               Platform-Specific ViewModels                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ WinUI.       │  │ (none needed │  │ (none needed │      │
│  │ ViewModel    │  │  for MAUI)   │  │  for WebApp) │      │
│  │   Journey    │  │              │  │              │      │
│  │   MainWindow │  │              │  │              │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┴──────────────────┘              │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                  Shared UI Layer                             │
│  ┌───────────────────────────────────────────────────┐      │
│  │              SharedUI Project                      │      │
│  │  - Base ViewModels (JourneyViewModel, etc.)       │      │
│  │  - MVVM patterns (ObservableObject, etc.)         │      │
│  │  - NO platform-specific dispatching!              │      │
│  └────────────────────────┬──────────────────────────┘      │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│              Backend Layer (PLATFORM-INDEPENDENT!)           │
│  ┌───────────────────────────────────────────────────┐      │
│  │              Backend Project                       │      │
│  │  - Z21 (UDP communication)                         │      │
│  │  - JourneyManager / WorkflowManager                │      │
│  │  - Model classes (Journey, Station, etc.)         │      │
│  │  - NO MainThread / DispatcherQueue / UI logic!    │      │
│  └────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚫 What NOT to Put in Backend

❌ **NEVER** add these to `Backend` project:

```csharp
// ❌ WRONG: Platform-specific UI thread dispatching
#if WINDOWS
    DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => { ... });
#endif

// ❌ WRONG: MAUI-specific threading
#if ANDROID || IOS
    MainThread.BeginInvokeOnMainThread(() => { ... });
#endif

// ❌ WRONG: Any UI framework dependencies
using Microsoft.UI.Dispatching;           // WinUI
using Microsoft.Maui.ApplicationModel;   // MAUI
using Microsoft.AspNetCore.Components;   // Blazor
```

**Why?**
- Backend should be **testable** without UI frameworks
- Backend should be **reusable** across all platforms (WinUI, MAUI, WebApp, Console, Unit Tests)
- Backend should focus on **business logic only**

---

## ✅ How to Handle Background Thread Events

### ❌ **WRONG APPROACH: Dispatching in Backend**

```csharp
// Backend/Manager/JourneyManager.cs (WRONG!)
protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
{
#if WINDOWS
    await DispatchToUIThreadAsync(() => HandleFeedbackAsync(journey));  // ❌ BAD!
#endif
}
```

### ✅ **CORRECT APPROACH: Platform-Specific ViewModels**

**Backend (platform-independent):**
```csharp
// Backend/Manager/JourneyManager.cs (CORRECT!)
protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
{
    // ✅ NO UI thread dispatching - just raise events
    journey.CurrentCounter++;  // Raises StateChanged event
}
```

**WinUI (platform-specific ViewModel):**
```csharp
// WinUI/ViewModels/Journey/JourneyViewModel.cs
public class JourneyViewModel : SharedUI.ViewModel.JourneyViewModel
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public JourneyViewModel(Journey model) : base(model)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Subscribe to model events and dispatch to UI thread
        Model.StateChanged += (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(CurrentCounter));
            });
        };
    }
}
```

**MAUI (uses MainThread in SharedUI CounterViewModel):**
```csharp
// SharedUI/ViewModel/CounterViewModel.cs
private void OnFeedbackReceived(Backend.FeedbackResult result)
{
#if ANDROID || IOS || MACCATALYST || WINDOWS
    MainThread.BeginInvokeOnMainThread(() =>
    {
        stat.Count++;  // ✅ Safe on UI thread
    });
#else
    stat.Count++;  // Fallback for unit tests
#endif
}
```

---

## 📁 File Organization for Platform-Specific ViewModels

### **WinUI Project Structure:**

```
WinUI/
├── ViewModels/
│   ├── Journey/
│   │   └── JourneyViewModel.cs       // Inherits from SharedUI.JourneyViewModel
│   └── MainWindowViewModel.cs        // Inherits from SharedUI.MainWindowViewModel
├── Services/
│   └── TreeViewBuilder.cs            // Creates WinUI-specific ViewModels
└── Views/
    └── MainWindow.xaml
```

**Namespace Pattern:**
```csharp
namespace Moba.WinUI.ViewModels.Journey;  // ✅ Use sub-namespaces
```

### **SharedUI Project Structure:**

```
SharedUI/
├── ViewModel/
│   ├── JourneyViewModel.cs           // Base (no dispatching)
│   ├── MainWindowViewModel.cs        // Base (no dispatching)
│   └── CounterViewModel.cs           // MAUI/WebApp (with MainThread)
└── Service/
    └── TreeViewBuilder.cs            // Creates base ViewModels
```

---

## 🔀 When to Create Platform-Specific ViewModels

| Scenario | Needs Platform-Specific ViewModel? |
|----------|-----------------------------------|
| **Background thread events** (Z21 UDP) | ✅ YES (WinUI needs DispatcherQueue) |
| **Simple data binding** (no threading) | ❌ NO (use SharedUI base) |
| **Platform-specific UI logic** (Windows Storage API) | ✅ YES |
| **MAUI-only features** (Geolocation, Camera) | ✅ YES (but in MAUI project, not SharedUI) |

---

## 📊 Decision Flow Chart

```
┌─────────────────────────────────────────┐
│  Need to handle background thread events │
│  from Backend (e.g., Z21 UDP callbacks)? │
└───────────────┬─────────────────────────┘
                │
        ┌───────┴────────┐
        │      YES       │
        └───────┬────────┘
                │
     ┌──────────▼───────────┐
     │  Is this WinUI app?  │
     └──────┬────────┬──────┘
            │        │
       YES  │        │  NO (MAUI/WebApp)
            │        │
    ┌───────▼─────┐  │
    │  Create:    │  │
    │  WinUI.     │  │
    │  ViewModels │  │
    │  .Journey   │  │
    │  .Journey   │  │
    │  ViewModel  │  │
    │             │  │
    │  (with      │  │
    │  Dispatcher │  │
    │  Queue)     │  │
    └─────────────┘  │
                     │
              ┌──────▼──────┐
              │  Use:       │
              │  SharedUI.  │
              │  Counter    │
              │  ViewModel  │
              │             │
              │  (already   │
              │  has        │
              │  MainThread │
              │  dispatch)  │
              └─────────────┘
```

---

## ✅ Implementation Checklist

**When creating a new feature that involves Backend events:**

- [ ] **Backend** raises plain C# events (no UI dependencies)
- [ ] **SharedUI** has base ViewModel (no platform-specific code)
- [ ] **WinUI** has platform-specific ViewModel (if needed) with `DispatcherQueue`
- [ ] **MAUI** uses `MainThread.BeginInvokeOnMainThread` in ViewModel
- [ ] **WebApp** uses `InvokeAsync(() => StateHasChanged())` in Razor component
- [ ] **Unit Tests** can mock Backend without UI framework

---

## 📝 Example: Journey Counter Feature

### **Backend (platform-independent):**
```csharp
// Backend/Model/Journey.cs
public class Journey
{
    public event EventHandler? StateChanged;
    
    private uint _currentCounter;
    public uint CurrentCounter
    {
        get => _currentCounter;
        set
        {
            if (_currentCounter != value)
            {
                _currentCounter = value;
                StateChanged?.Invoke(this, EventArgs.Empty);  // ✅ Plain C# event
            }
        }
    }
}

// Backend/Manager/JourneyManager.cs
protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
{
    journey.CurrentCounter++;  // ✅ NO UI thread dispatching
}
```

### **SharedUI (base ViewModel):**
```csharp
// SharedUI/ViewModel/JourneyViewModel.cs
public partial class JourneyViewModel : ObservableObject
{
    protected Journey Model { get; }
    
    public JourneyViewModel(Journey model)
    {
        Model = model;
        Model.StateChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CurrentCounter));  // ⚠️ NO dispatching here!
        };
    }
    
    public uint CurrentCounter => Model.CurrentCounter;
}
```

### **WinUI (platform-specific ViewModel):**
```csharp
// WinUI/ViewModels/Journey/JourneyViewModel.cs
namespace Moba.WinUI.ViewModels.Journey;

public class JourneyViewModel : SharedUI.ViewModel.JourneyViewModel
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public JourneyViewModel(Backend.Model.Journey model) : base(model)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Unsubscribe from base event
        Model.StateChanged -= OnModelStateChanged;
        
        // Re-subscribe with UI thread dispatching
        Model.StateChanged += (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(CurrentCounter));  // ✅ On UI thread
            });
        };
    }
}
```

### **MAUI (uses CounterViewModel with MainThread):**
```csharp
// SharedUI/ViewModel/CounterViewModel.cs (already handles threading)
private void OnFeedbackReceived(Backend.FeedbackResult result)
{
#if ANDROID || IOS || MACCATALYST || WINDOWS
    MainThread.BeginInvokeOnMainThread(() =>
    {
        var stat = Statistics.FirstOrDefault(s => s.InPort == result.InPort);
        if (stat != null) stat.Count++;  // ✅ On UI thread
    });
#endif
}
```

---

## 🎯 Key Takeaways

1. ✅ **Backend = Platform-Independent** (no `DispatcherQueue`, `MainThread`, UI frameworks)
2. ✅ **SharedUI = Base ViewModels** (common logic, minimal platform code)
3. ✅ **WinUI = Platform-Specific ViewModels** (when needed for `DispatcherQueue`)
4. ✅ **MAUI = Uses `MainThread`** (in SharedUI or MAUI-specific ViewModels)
5. ✅ **WebApp = Uses `InvokeAsync`** (in Razor components)

---

## 📚 Related Guidelines

- See `.copilot-instructions.md` → **Separation of Concerns**
- See `.copilot-instructions.md` → **MAUI Threading (CRITICAL!)**
- See `.copilot-instructions.md` → **MVVM Pattern**
