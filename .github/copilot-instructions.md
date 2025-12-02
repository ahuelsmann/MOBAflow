# MOBAflow - Consolidated Copilot Instructions

> **Multi-platform railway automation control system (.NET 10)**
> - MOBAflow (WinUI) - Desktop control center
> - MOBAsmart (MAUI) - Android mobile app
> - MOBAdash (Blazor) - Web dashboard

---

## 🏗️ Core Architecture

### Clean Architecture Layers
```
Domain (Pure POCOs)
  ↑
Backend (Platform-independent logic)
  ↑
SharedUI (Base ViewModels)
  ↑
WinUI / MAUI / Blazor (Platform-specific)
```

**Critical Rules:**
- ✅ **Domain:** Pure POCOs - NO attributes, NO INotifyPropertyChanged, NO logic
- ✅ **Backend:** Platform-independent - NO DispatcherQueue, NO MainThread
- ✅ **SharedUI:** ViewModels with CommunityToolkit.Mvvm
- ✅ **Platform:** UI-specific code only

---

## 🎯 MVVM Best Practices

### Rule 1: Minimize Code-Behind
```csharp
// ❌ WRONG: Logic in code-behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    ViewModel.Property = value;
    ViewModel.DoSomething();
}

// ✅ CORRECT: Command binding in XAML
<Button Command="{x:Bind ViewModel.DoSomethingCommand}" />
```

### Rule 2: Use Property Changed Notifications
```csharp
// ✅ CORRECT: CommunityToolkit.Mvvm
[ObservableProperty]
private string name;

partial void OnNameChanged(string value)
{
    // Side effects here (NOT in code-behind!)
    UpdateRelatedProperty();
}
```

### Rule 3: Acceptable Code-Behind
```csharp
// ✅ ACCEPTABLE:
- Constructor with DI injection
- Window lifecycle events (delegating to ViewModel)
- Platform-specific UI code (Window.SetTitleBar, etc.)
- Simple event handlers for drag-and-drop (XAML limitation)

// ❌ NEVER:
- Business logic
- Command execution
- Data manipulation
- State management
```

---

## 💉 Dependency Injection

### Service Registration Pattern
```csharp
// WinUI/App.xaml.cs
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
services.AddSingleton<IUiDispatcher, WinUIDispatcher>();
services.AddSingleton<Solution>();
services.AddSingleton<MainWindowViewModel>();

// Pages receive MainWindowViewModel via constructor
services.AddTransient<EditorPage1>();
```

**Lifetime Rules:**
- **Singleton:** Application state, hardware abstraction (IZ21, Solution)
- **Transient:** Pages, disposable services
- **Scoped:** Blazor only (per-request state)

---

## 📁 File Organization

### Namespace Rules
```
Backend/Manager/JourneyManager.cs    → namespace Moba.Backend.Manager;
WinUI/View/EditorPage1.xaml.cs      → namespace Moba.WinUI.View;
SharedUI/ViewModel/JourneyViewModel  → namespace Moba.SharedUI.ViewModel;
```

**One Class Per File:** `JourneyManager.cs` contains ONLY `class JourneyManager`

---

## 🔄 JSON Serialization

### StationConverter Pattern
```csharp
// Domain/Station.cs - PURE POCO
public class Station
{
    public string Name { get; set; }
    public Workflow? Flow { get; set; }        // ← Navigation property
    public Guid? WorkflowId { get; set; }      // ← Foreign key
}

// Backend/Converter/StationConverter.cs
public override void WriteJson(...)
{
    // Serialize WorkflowId instead of Flow (prevent circular refs)
    writer.WritePropertyName("WorkflowId");
    serializer.Serialize(writer, value.Flow.Id);
}
```

**Key Principle:** Domain stays pure, converters handle serialization logic.

---

## 🧪 Testing

### Fake Objects for Backend Tests
```csharp
// Test/Fakes/FakeUdpClientWrapper.cs
public class FakeUdpClientWrapper : IUdpClientWrapper
{
    public void SimulateFeedback(int inPort)
    {
        Received?.Invoke(CreateFeedbackPacket(inPort));
    }
}
```

**Never:** Mock hardware in production code. Use abstractions (IZ21, IUdpClientWrapper).

---

## 📊 Current Status (Dec 2025)

| Metric | Value |
|--------|-------|
| Projects | 9 |
| Build Success | 100% |
| Tests Passing | 104/104 (100%) |
| Architecture Violations | 0 |

---

## 🚨 Common Pitfalls

### 1. **Domain Pollution**
```csharp
// ❌ NEVER in Domain:
[JsonConverter(typeof(CustomConverter))]
[Required, StringLength(100)]
public string Name { get; set; }

// ✅ ALWAYS: Pure POCOs
public string Name { get; set; }
```

### 2. **Platform-Specific Code in Backend**
```csharp
// ❌ NEVER in Backend:
#if WINDOWS
    await DispatcherQueue.EnqueueAsync(...);
#endif

// ✅ ALWAYS: Use IUiDispatcher abstraction
await _uiDispatcher.InvokeOnUiAsync(...);
```

### 3. **Code-Behind Logic**
```csharp
// ❌ NEVER:
private void Button_Click(...)
{
    ViewModel.Property = newValue;
}

// ✅ ALWAYS: Command + Property binding
<Button Command="{x:Bind ViewModel.UpdateCommand}"
        CommandParameter="{x:Bind NewValue}" />
```

---

## 🔧 Quick Reference

### File Locations
- **City Library:** `WinUI/bin/Debug/germany-stations.json` (master data)
- **User Solutions:** `*.mobaflow` files (user projects)
- **Settings:** `appsettings.json` (Z21 IP, Speech config)

### Key Classes
- **MainWindowViewModel:** Central ViewModel (shared by all Pages)
- **Solution:** Root domain object (Projects → Journeys/Workflows/Trains)
- **IZ21:** Hardware abstraction (UDP → Z21 protocol)
- **StationConverter:** JSON serialization (Workflow references)

---

## 📚 Related Documentation

- **Build Status:** `docs/BUILD-ERRORS-STATUS.md`
- **Z21 Protocol:** `docs/Z21-PROTOCOL.md`
- **MVVM Analysis:** `docs/MVVM-ANALYSIS-MAINWINDOW-2025-12-02.md`
- **Session Reports:** `docs/SESSION-SUMMARY-*.md` (archive after 1 month)

---

**Last Updated:** 2025-12-02  
**Version:** 2.0 (Consolidated)
