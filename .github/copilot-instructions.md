# MOBAflow Copilot Instructions

**For AI coding agents working on MOBAflow — event-driven model railroad automation on .NET 10.**

> Essential patterns, boundaries, and critical knowledge. Read `.github/instructions/` for deep dives. **This file is ALWAYS loaded.**

---

## 🎯 EventBus Threading Boundary (MOST CRITICAL)

**Single thread-marshalling boundary.** Z21 publishes on background thread → decorator marshals to UI → ViewModels update safely.

```
Z21 (background thread) → Publish(FeedbackReceivedEvent)
UiThreadEventBusDecorator → dispatcher.InvokeOnUi(handler)
MainWindowViewModel.OnFeedbackReceived() → IsConnected = true (UI thread safe)
```

**Key files:**
- `Backend/Z21.cs` — Publishes events
- `SharedUI/Service/UiThreadEventBusDecorator.cs` — Marshals to UI thread
- `SharedUI/ViewModel/MainWindowViewModel.cs` — Subscribes (no dispatcher needed!)
- `WinUI/App.xaml.cs:295` — `AddEventBusWithUiDispatch()` registration

---

## 🚫 Absolute Rules

1. **No code without analysis** — `code_search`, `file_search`, `find_symbol` first
2. **No hardcoded colors** — `ThemeResource` only (`CardBackgroundFillColorDefaultBrush`, etc.)
3. **No `.Result` / `.Wait()`** — Use `await`
4. **No `InvokeOnUi` in EventBus handlers** — Decorator already marshals to UI thread
5. **No separate README.md in subdirs** — Root only
6. **No `<Page Remove="..."/>` in .csproj** — Breaks XAML compiler
7. **No TODO comments in code** — Azure DevOps Work Item instead
8. **Backend/Common platform-independent** — Zero WinUI/MAUI references
9. **Never guess file names, APIs** — Use tools first
10. **No session details here** — Session progress → Azure DevOps or `.github/todos.instructions.md`

---

## ✅ 6-Step Workflow

### 1. ANALYSE
- Understand requirements
- Identify affected files
- Find existing patterns
- Identify existing tests

### 2. RESEARCH
- Existing implementations
- Documentation
- .NET 10 / WinUI 3 specs

### 3. PLAN
- **ALWAYS use `plan()` tool**
- Affected files, risks, test strategy

### 4. IMPLEMENTATION
- Backend → ViewModel → View
- Use `get_errors()` after each file
- XAML: `ThemeResource` only
- MVVM: `[ObservableProperty]`, `[RelayCommand]`
- Async: `await` always

### 5. VALIDATION
- `run_build()` at end
- `run_tests()` for relevant projects
- `.editorconfig` compliance

### 6. DOCUMENTATION
- `README.md` updated (if user-facing)
- `.github/todos.instructions.md` updated
- Inline comments: Why, not What
- Public APIs: XML docs

---

## 🏗️ Layer Responsibilities

| Layer | Location | Role |
|-------|----------|------|
| Domain | `Domain/*.cs` | POCOs, business logic |
| Backend | `Backend/Service/*.cs` | Z21, WorkflowService, events |
| EventBus | `Common/Events/`, `SharedUI/Service/` | Pub/sub, UI marshalling |
| ViewModels | `SharedUI/ViewModel/*.cs` | Observable state |
| Platform | `WinUI/`, `MAUI/`, `WebApp/` | Pages, navigation |

---

## 🔀 MVVM & EventBus Pattern

```csharp
// ✅ CORRECT
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IEventBus _eventBus;
    
    public MainWindowViewModel(..., IEventBus eventBus, ...)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived);
    }
    
    private void OnFeedbackReceived(FeedbackReceivedEvent e)
    {
        IsConnected = true;  // Safe: UI thread (decorator guaranteed)
    }
}

// ❌ WRONG
_dispatcher.InvokeOnUi(() => IsConnected = true);
```

---

## 💉 DI Registration (WinUI/App.xaml.cs:290+)

```csharp
var services = new ServiceCollection();
services.AddSingleton<IZ21, Z21>();
services.AddSingleton<IEventBus, EventBus>();
services.AddEventBusWithUiDispatch();  // Decorator wrapping
services.AddSingleton<MainWindowViewModel>();
services.AddTransient<View.JourneyPage>();
```

---

## 📁 Key File Locations

**Domain:** `Domain/Project.cs`, `Domain/Journey.cs`, `Domain/Locomotive.cs`

**Backend:** `Backend/Z21.cs`, `Backend/Service/WorkflowService.cs`, `Common/Validation/ProjectValidator.cs`

**ViewModels:** `SharedUI/ViewModel/MainWindowViewModel.cs`, `TrainControlViewModel.cs`, `SignalBoxViewModel.cs`

**WinUI Pages:** `WinUI/View/MainWindow.xaml`, `TrackPlanPage.xaml`, `SignalBoxPage.xaml`

---

## 🛠️ Build & Test

```bash
dotnet build          # Full build
dotnet test           # All tests (262 passing)
dotnet run --project WinUI    # Windows app
dotnet run --project WebApp   # Blazor web app
```

---

## 🧭 Design Principles

- **SOLID:** Single Responsibility | Open/Closed | Liskov | Interface Segregation | Dependency Inversion
- **DRY:** Don't Repeat Yourself (max 2x duplication)
- **KISS:** Keep It Simple (<20 lines/method)
- **Meaningful Names:** Not "x", "temp", "data"
- **Separation of Concerns:** Domain ↔ ViewModel ↔ View strictly separated

---

## 📚 Instruction Files

**Architecture:**
- `di-pattern-consistency.md` — DI rules, singletons
- `architecture.md` — Layers, data flow, threading
- `backend.md` — Platform independence
- `mvvm-best-practices.md` — MVVM patterns
- `test.md` — AAA, Fakes, NUnit

**WinUI:**
- `winui.md` — DispatcherQueue, DataTemplates, x:Bind
- `fluent-design.md` — ThemeResource, 8px grid, icons
- `xaml-page-registration.md` — XAML compiler issues

**Code Quality:**
- `naming-conventions.md` — PascalCase
- `self-explanatory-code-commenting.md` — Why not What
- `no-special-chars.md` — ASCII only

**Workflow:**
- **`todos.instructions.md`** — Session progress (OPTIONAL)
- **Azure DevOps (Projekt MOBAflow)** — Open work (AUTHORITATIVE)

---

## ✅ Pre-Commit Checklist

- [ ] `.editorconfig` compliance
- [ ] No TODO comments
- [ ] No `.Result` / `.Wait()`
- [ ] Constructor injection only
- [ ] `[ObservableProperty]` for MVVM
- [ ] XML-docs for public APIs
- [ ] Tests pass: `dotnet test`
- [ ] Build: `dotnet build`
- [ ] README updated (if user-facing)

---

## 🌍 When in Doubt

1. **Azure DevOps** — Authoritative for open work
2. `.github/instructions/` — Technical deep dives
3. Microsoft Learn — .NET 10 / WinUI 3 specs
4. Existing code — Follow surrounding style
