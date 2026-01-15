---
description: 'Architecture Decision Records (ADRs) for MOBAflow'
applyTo: '**'
---

# 🏗️ Architecture Decisions

> **Letztes Update:** 2026-01-18

---

## ADR-001: Auto-Save Pattern (2026-01-17)

**Status:** ✅ IMPLEMENTIERT

**Kontext:** Inkonsistentes Event-Handling für Auto-Save zwischen ViewModels.

**Entscheidung:** PropertyChanged-basiertes Auto-Save für ALLE ViewModels.

**Pattern:**
```csharp
// ViewModel Property Setter
public string Name
{
    get => _model.Name;
    set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
}

// MainWindowViewModel Handler
private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    var ignoredProperties = new[] { "IsSelected", "IsExpanded", "IsHighlighted" };
    if (!ignoredProperties.Contains(e.PropertyName))
    {
        _ = SaveSolutionInternalAsync();
    }
}
```

**Vorteile:**
- Konsistenz über alle ViewModels
- CommunityToolkit.Mvvm Standard
- Ein Event-Handler für alles
- Nested ViewModels propagieren als `PropertyChanged("Collection")`

---

## ADR-002: App Shell Architektur (2026-01-15)

**Status:** ✅ IMPLEMENTIERT

**Kontext:** Zentrales Navigation-Pattern mit DI-Support benötigt.

**Interfaces (SharedUI/Shell/):**
- `INavigationService` - Zentrale Navigation mit History
- `IPageFactory` - DI-basierte Page-Erstellung
- `ILifecycleHost` - App-weite Lifecycle-Events
- `IShellService` - Shell-Regions (Main, Overlay, Sidebar, Footer)

**WinUI Implementierungen:**
- `NavigationService`, `PageFactory`, `LifecycleHost`, `ShellService`

---

## ADR-003: Responsive Layout mit VSM (2026-01-15)

**Status:** ✅ IMPLEMENTIERT

**Kontext:** AdaptivePanel (Custom Control) hatte WinUI 3 Inkompatibilitäten.

**Entscheidung:** VisualStateManager mit AdaptiveTrigger.

**Begründung:**
- Microsoft Best Practice für WinUI 3
- Deklarativ und wartbar
- Kein Code-Behind erforderlich
- Smooth Transitions

**Breakpoints:**
- Compact: 0-640px
- Medium: 641-1199px
- Wide: 1200px+

---

## ADR-004: Plugin-System mit PluginLoadContext (2025-02-05)

**Status:** ✅ IMPLEMENTIERT

**Kontext:** Erweiterbare Architektur für Third-Party Plugins.

**Pattern:**
- `IPlugin` Interface für Plugin-Contract
- `PluginBase` als Abstract Base Class
- `PluginLoadContext` für Assembly-Isolation
- `PluginDiscoveryService` für Auto-Discovery

**Plugin-Lifecycle:**
1. Discovery (Startup)
2. Validation
3. ConfigureServices
4. OnInitializedAsync
5. Runtime
6. OnUnloadingAsync

---

## ADR-005: Logging mit Serilog (2025-01-XX)

**Status:** ✅ IMPLEMENTIERT

**Kontext:** Strukturiertes Logging mit Multi-Sink Support.

**Sinks:**
- File Logs: `logs/mobaflow-YYYYMMDD.log` (7-day rolling)
- In-Memory Sink: Real-time streaming to MonitorPage UI

**Pattern:**
```csharp
_logger.LogInformation("Feedback received: InPort={InPort}", inPort);
```

---

## Best Practices eingehalten

1. **Clean Architecture:** Domain → Backend → SharedUI → Platform
2. **MVVM-Pattern:** CommunityToolkit.Mvvm
3. **DI-Container:** Microsoft.Extensions.DependencyInjection
4. **Interface-Segregation:** IZ21, IActionExecutor, ISpeakerEngine
5. **NullObject-Pattern:** NullSoundPlayer, NullSpeakerEngine
6. **Async-Pattern:** Async/await durchgängig

---

*Teil von: [.copilot-todos.md](../.copilot-todos.md)*
