# MOBAflow TODOs & Roadmap

> Last Updated: 2026-03-19

---

## ✅ SESSION COMPLETED: UI Layout Pattern Rollout (JourneysPage Style)

**Ziel:** Das erfolgreiche und hochgelobte Layout-Muster der
`JourneysPage` auf alle anderen spaltenorientierten Seiten
(`WorkflowsPage`, `TrainsPage`, `LocomotivesPage` etc.) übertragen,
um ein einheitliches, schlankes und professionelles Fluent-Design in der
gesamten App zu gewährleisten.

**Erledigt:**

- [x] Identifikation aller relevanten Seiten mit Spalten/Grid-Layout.
- [x] Umbau der XAML-Strukturen zur Nutzung von
  `CollapsibleColumnCollection`, `CollapsibleColumnHelper` und
  `CollapsibleColumnProperties` für `WorkflowsPage`, `LocomotivesPage`,
  `PassengerWagonPage` und `GoodsWagonPage`.
- [x] Anpassung der `GridSplitter`: Breite konsequent auf `4` Pixel reduzieren.
- [x] Entfernung äußerer `Margin`-Werte (`Margin="0"`) an den
  Collapsible-Controls für maximale Platznutzung.
- [x] Übernahme der entsprechenden Code-Behind-Logik
  (Layout-Persistierung in `AppSettings`, dynamische Breiten mit
  `GridLength.Auto`).
- [x] Sicherstellen, dass Suchfelder (`AutoSuggestBox`) und
  Kontextmenüs (`ContextFlyout`) in allen Listen verfügbar sind.

---

## ✅ SESSION COMPLETED: UI Layout Pattern & Fluent Design (JourneysPage)

**Ziel:** Implementierung eines professionellen, platzsparenden und
optisch ansprechenden Spaltenlayouts für die JourneysPage auf Basis des
Fluent Design Systems.

**Erledigt:**

- [x] Architektur-Wechsel: Einführung der Controls
  `CollapsibleColumnCollection`, `CollapsibleColumnHelper` und
  `CollapsibleColumnProperties` zur klaren semantischen Trennung.
- [x] Subtile Farbakzente (20% Opacity) am linken Rand zur Orientierung
  (Blau für Listen, Gelb für Helper, Orange für Properties).
- [x] Schlankheitskur: Entfernung überflüssiger Margins (`Margin="0"`),
  schmalere GridSplitter (`Width="4"`), reduziertes inneres Padding
  (`12px` statt `20px`) und reduziertes Padding in EntityTemplates.
- [x] UX-Verbesserungen: Einheitliche Suchfelder (`AutoSuggestBox`)
  für Journeys, Stations, Workflows und Cities hinzugefügt.
- [x] Navigation: Umfangreiche Kontextmenüs (Rechtsklick) in Listen
  (Journeys, Stations, Workflows, Actions) implementiert.
- [x] Code-Behind-Refactoring für dynamisches Resizing
  (`PreviousAndNext`) und Persistierung in den `AppSettings`.

---

## ✅ SESSION COMPLETED: EventBus & UI-Thread-Grenze (Dispatcher-Reduktion)

**Ziel:** Saubere Architektur: Marshalling Hintergrund → UI an einer
Stelle (EventBus-Decorator), ViewModels ohne Dispatcher für Event-Quellen.

**Erledigt:**

- [x] `UiThreadEventBusDecorator` – alle EventBus-Handler laufen auf
  UI-Thread (WinUI: `AddEventBusWithUiDispatch()`)
- [x] Z21 publiziert: `Z21ConnectionEstablishedEvent`,
  `Z21ConnectionLostEvent`, `FeedbackReceivedEvent`
- [x] PostStartupInitializationService publiziert `PostStartupStatusEvent`
- [x] MainWindowViewModel: EventBus-Subscriptions für Z21 und
  PostStartup; keine direkten Z21-Events mehr
- [x] TrainControlViewModel: nur noch EventBus; `IUiDispatcher` entfernt
- [x] `EnqueueOnUi` überall entfernt; Dispatcher-Service auf
  `InvokeOnUi` / `InvokeOnUiAsync` reduziert
- [x] Architektur-Dokumentation: `docs/ARCHITECTURE.md` Abschnitt
  „Threading und UI-Thread-Grenze“ + Umsetzungsstand

**Optional (verbleibende Dispatcher-Nutzung, bei Bedarf auf Events umstellbar):**

- [ ] Asynchrones Solution-Laden: `ApplyLoadedSolution` nach Load –
  ggf. `SolutionLoadedEvent` + Subscriber
- [ ] Health-Status: `MainWindow.xaml.cs` ruft ViewModel mit
  `InvokeOnUi` – ggf. HealthCheckService publiziert Event
- [ ] Settings-/Journey-/Solution-Callbacks
  (Save, StationChanged etc.) – ggf. über EventBus
- [ ] `MonitorPageViewModel` und `MauiViewModel` – weitere
  `InvokeOnUi`-Stellen bei Gelegenheit prüfen

**Referenz:** `docs/ARCHITECTURE.md`
(Threading und UI-Thread-Grenze),
`SharedUI/Service/UiThreadEventBusDecorator.cs`,
`Common/Events/Z21Events.cs`

---

## 📋 OFFEN: TrackPlanPage MVVM-Refaktorierung

**Ziel:** Logik aus dem Code-Behind der TrackPlanPage ins
TrackPlanViewModel verlagern (gemäß Instructions:
„in Views so wenig Code-Behind wie möglich“).

- [ ] `TrackPlanViewModel` um `EditableTrackPlan` erweitern
  (Plan, Commands, Stats, Selection)
- [ ] Commands ins ViewModel: `LoadTestPlan`, `OpenSvgInBrowser`,
  `DisconnectSelectedSegment`, `DeleteSelectedSegment`
- [ ] Beobachtbare Properties: `SegmentCount`, `ConnectionCount`,
  `OpenEndCount`, `SelectionInfo`, `SnapEnabled`, `StatusMessage`
- [ ] Snap-/Place-Logik (`FindBestSnap`, `TrySnapAndPlace`,
  `AdjustTargetSegmentForNewEntryPort`) ins ViewModel oder über
  ViewModel aufrufbar
- [ ] TrackPlanPage: Nur Drag & Drop (Pointer-Events, Ghost,
  Canvas-Draw) und UI-Setup im Code-Behind belassen;
  Rest über ViewModel/Binding
- [ ] DI anpassen: ViewModel erhält `EditableTrackPlan`;
  Page erhält Plan ggf. nur noch über `ViewModel.Plan`

**Hinweis:** SharedUI hat bereits Referenz auf TrackLibrary.PikoA;
TrackPlan.Renderer = Backend, TrackLibrary.PikoA = Domain.

---

## ✅ ERLEDIGT: TrainControlPage – ComboBox für Lok-/Train-Auswahl

**Umsetzung:** Neben den Presets gibt es eine ComboBox mit den
Projekt-Lokomotiven (`ProjectLocomotives` /
`SelectedLocomotiveFromProject`). Bei Auswahl wird die DCC-Adresse
automatisch aus `Locomotive.DigitalAddress` übernommen
(`OnSelectedLocomotiveFromProjectChanged` → `LocoAddress`).

**Referenz:** `MOBAflow/View/TrainControlPage.xaml`
(ComboBox „Lok aus Projekt"),
`SharedUI/ViewModel/TrainControlViewModel.cs`
(`ProjectLocomotives`, `SelectedLocomotiveFromProject`).

---

## 📋 FUTURE: Train Control & Pages

**TrainsPage – Umgestaltung:**

- [ ] Aktuell: Katalog-Verwaltung
  (Locomotives, Passenger Wagons, Goods Wagons in Spalten)
- [ ] Ziel: View dient dazu, aus Loks und Wagen **Züge (Trains)** zu bilden
- [ ] Fokus: Zugzusammenstellung (Lok + Wagen in Reihenfolge), nicht CRUD der Bestände

**Neue Wagen-Pages (analog zu LocomotivesPage):**

- [ ] `PassengerWagonsPage` – Personenwagen-Verwaltung
  (+ ggf. wagon-spezifische Infos)
- [ ] GoodsWagonsPage – Güterwagen-Verwaltung
- [ ] `TrainsPage` konzentriert sich dann auf Zugzusammenstellung;
  Wagenbestände kommen aus diesen Pages

---

## ✅ SESSION 35 COMPLETED: Track Plan Win2D Phase 1

**Win2D GPU-Rendering für Track Plan (Phase 1):**

- [x] Microsoft.Graphics.Win2D 1.3.2 ins WinUI-Projekt integriert
- [x] TargetFramework WinUI auf 22621 angehoben
  (NETSDK1130-Workaround für Win2D)
- [x] PathToCanvasGeometryConverter – Pfad-Befehle → CanvasGeometry
- [x] CanvasControl ersetzt XAML-Canvas für Segment-Zeichnung
- [x] Draw-Handler zeichnet alle Gleissegmente via Win2D
- [x] Overlay-Canvas für Ghost, Rotation-Handle, Port-Indikatoren beibehalten
- [x] Zoom/Pan (ScrollViewer) unverändert funktionsfähig

**Dateien:**

- `MOBAflow/View/PathToCanvasGeometryConverter.cs` (NEU)
- `MOBAflow/View/TrackPlanPage.xaml` / `.xaml.cs` (CanvasControl + Draw)
- `Directory.Packages.props`, `MOBAflow/MOBAflow.csproj` (Win2D, TargetFramework)

**Dokumentation:** `docs/TrackPlan-Win2D-DragDrop-Selection.md`

**Nächste Phase (Phase 2):** Hit-Testing via `StrokeContainsPoint`,
Ghost in Win2D zeichnen, optional Caching

---

## ⏳ SESSION 33 IN PROGRESS: Signal Box Feature Breakthrough

**Signal Control Implementation Progress:**

- [x] Signal images rendering in UI
- [x] Signal state properties linked to domain model
- [x] Signal input/switching implemented on at least 2 controls
- [x] Signal image alignment/composition not fully matching
  (visual refinement needed)
- [x] Not all signal image properties produce visual effects yet
  (conditional rendering to investigate)
- ⏳ Full integration with MOBAesb topology model pending

**What Works:**

- ✅ Signal aspect switching detected and responding on 2+ controls
- ✅ Domain model `SignalBoxPlan` and signal state persistence
- ✅ UI bindings for signal display and manipulation

**Known Issues:**

- Signal images don't align perfectly when composed
- Some property combinations don't visually update
  (e.g., certain aspect + speed signal combos)
- Need to audit property-to-visual mapping for all signal types

**Next Steps:**

- Debug visual composition/alignment issues in SignalBoxPage XAML
- Add conditional rendering for multi-aspect signal combinations
- Test all signal type configurations (main, distant, combined, shunting)
- Implement full signal logic for routes and interlocking

**Files Involved:**

- `SharedUI/ViewModel/SignalBoxViewModel.cs`
- `MOBAflow/View/SignalBoxPage.xaml` / `.xaml.cs`
- `Domain/SignalBoxPlan.cs`
- `appsettings.json` (FeatureToggles: IsSignalBoxPageAvailable)

---

## ✅ SESSION 32+ COMPLETED: Git Hooks Comprehensive System

**Git Hooks Implemented:**

- [x] `pre-commit` - JSON validation (Syntax, Schema, Secrets)
- [x] `commit-msg` - Conventional Commits enforcement (feat:, fix:, docs:, etc.)
- [x] `pre-push` - Unit tests validation before push
- [x] `post-checkout` - Auto NuGet restore on branch change
- [x] Windows-compatible (PowerShell + .cmd wrapper)
- [x] Comprehensive `.git/hooks/README.md` documentation

**Hook Details:**

| Hook | Trigger | Action | Bypass |
| ------ | --------- | -------- | -------- |
| **pre-commit** | `git commit` | Validate JSON files, secrets | `--no-verify` |
| **commit-msg** | `git commit` | Commit format check | (format required) |
| **pre-push** | `git push` | Run unit tests | `--no-verify` |
| **post-checkout** | `git checkout` | Auto `dotnet restore` | (automatic) |

**Conventional Commit Examples:**

```text
feat(signal-box): Add signal aspect switching
fix(z21): Reconnect after network timeout
docs: Update README for signal controls
test(locomotive): Add brake test cases
refactor(validator): Simplify completeness check
perf(json): Improve validation performance
```

**Benefits:**

- ✅ Consistent commit history across team
- ✅ Prevents JSON errors before commit
- ✅ Failed builds never reach remote
- ✅ Automatic NuGet sync prevents build breaks
- ✅ Clear, searchable commit messages for Azure DevOps

**Status:**

- Build: ✅ Successful
- Hooks: ✅ All tested and working
- Documentation: ✅ Comprehensive README in `.git/hooks/`

**Setup for New Developers:**

```powershell
git clone <repo>
git config core.hooksPath .git/hooks  # Activate hooks
# All hooks now active automatically!
```

---

## ✅ SESSION 30 COMPLETED: JSON Configuration Validation Framework

**What was implemented:**

- [x] JSON syntax validation in build pipeline (`ValidateJsonConfiguration.targets`)
- [x] PowerShell validation script with secrets warning (`ValidateJsonConfiguration.ps1`)
- [x] JSON Schema for configuration structure (`appsettings.schema.json`)
- [x] Pre-commit Git hook to prevent invalid config commits (`.git/hooks/pre-commit`)
- [x] Fixed `appsettings.Development.json` missing comma bug

**Issues resolved:**

- [x] `System.IO.InvalidDataException` when parsing malformed JSON at runtime
- [x] Build now fails early with clear error messages (Line number + position)
- [x] Developers warned about missing Speech.Key or Z21 IP during development

**Build Integration:**

- Build runs `ValidateJsonConfiguration` target before compilation
- Pre-commit hook prevents commits with syntax errors
- Both DEBUG and RELEASE configurations validate

**Status:**

- Build: ✅ Successful
- Validation: ✅ All files valid
- Pre-commit: ✅ Installed and functional

---

## ✅ SESSION 31 COMPLETED: Data File Validation & NuGet Recommendations

**JSON Schema Validation Extended:**

- [x] Created schema for `solution.json` (projects, locomotives, journeys)
- [x] Master data consolidated in `data.json`
  (Städte/Bahnhöfe + Lokomotiv-Bibliothek);
  separate germany-* files removed
- [x] Extended `ValidateJsonConfiguration.ps1` with type-specific validation
- [x] Updated `MOBAflow.csproj` to include all schemas in build output

**Type-Specific Validation:**

- Configuration files: Secrets warnings (Speech.Key, Z21 IP)
- Solutions: Project count validation
- Locomotives: Category & epoch validation
- Stations: Station count validation

**Validation Coverage:**

| File | Schema | Type | Validation |
| ------ | -------- | ------ | ----------- |
| `appsettings*.json` | ✅ | Configuration | Secrets check |
| `solution.json` | ✅ | Solution | Projects, Locomotives |
| `data.json` | ✅ | Master Data | Cities, Stations, Locomotives |

---

## ✅ SESSION 32 COMPLETED: JSON Schema Validation on Project Load

### Completeness Checks

**Runtime Validation Implemented:**

- [x] Enhanced `solution.schema.json` with ALL 8 Project domain properties
- [x] Created `ProjectValidator` service for completeness validation
- [x] Integrated `IProjectValidator` into IoService.LoadAsync()
- [x] ProjectValidator logs detailed info, warnings, and errors for
  missing domain types
- [x] Registered validator in DI container via `MobaServiceCollectionExtensions`

**Validation Flow:**

1. User loads .json → IoService.LoadAsync()
2. JSON syntax validation (existing)
3. Schema validation (existing)
4. **NEW**: ProjectValidator.ValidateCompleteness()
   - Checks if Locomotives, Journeys, Stations are present
   - Logs details about Trains, Workflows, Wagons, SignalBoxPlan
   - Per-project validation (supports multi-project solutions)

**Completeness Checks:**

| Property | Required | Warning Trigger |
| ---------- | ---------- | ----------------- |
| Locomotives | ❌ | If Journey defined but no Locomotives |
| Journeys | ❌ | If Project defined but no Journeys |
| Stations | ❌ | If Journeys defined but no Stations |
| Trains | ❌ | Info only (optional) |
| Workflows | ❌ | Info only (optional) |
| PassengerWagons | ❌ | Info only (optional) |
| GoodsWagons | ❌ | Info only (optional) |
| SignalBoxPlan | ❌ | Info only (optional) |

**solution.json Should Contain Examples:**

All domain types should be represented in solution.json to serve as a
complete reference for developers:

```json
{
  "name": "Example Solution",
  "projects": [{
    "name": "Example Project",
    "locomotives": [...],          // Required: must have at least one
    "trains": [...],               // Optional: train compositions
    "passengerWagons": [...],      // Optional: passenger cars
    "goodsWagons": [...],          // Optional: freight cars
    "journeys": [...],             // Required: must have at least one
    "workflows": [...],            // Optional: automation sequences
    "signalBoxPlan": {...}         // Optional: signal box topology
  }]
}
```

**Files Modified:**

- `Backend/Service/ProjectValidator.cs` (NEW)
- `Backend/Extensions/MobaServiceCollectionExtensions.cs` (added validator registration)
- `MOBAflow/Service/IoService.cs` (added validator call + logging)
- `MOBAflow/Build/Schemas/solution.schema.json` (enhanced with all properties)

**Build Status:** ✅ Successful

---

## 📦 Recommended NuGet Packages for MOBAflow

### **Already Installed & Recommended** ✅

**Logging & Diagnostics:**

- ✅ `Serilog.Extensions.Logging` – Currently used
- ✅ `Serilog.Sinks.File` – Currently used
- ✅ `Serilog.Sinks.Debug` – Currently used
- ➕ **NEW**: `Serilog.Sinks.Async` (v2.1.0+) – Async logging without blocking UI
- ➕ **NEW**: `Serilog.Enrichers.Environment` – Add OS/CPU info to logs
- ➕ **NEW**: `Serilog.Enrichers.Process` – Add process ID/name to logs

**Dependency Injection:**

- ✅ `Microsoft.Extensions.DependencyInjection` – Currently used
- ✅ `Microsoft.Extensions.Configuration` – Currently used
- ➕ **NEW**: `Microsoft.Extensions.Options.ConfigurationExtensions`
  – Strong config validation

**Real-time Communication:**

- ✅ `Microsoft.AspNetCore.SignalR.Client` – Currently used
- ✅ `Microsoft.WindowsAppSDK` – Currently used

**Health Checking:**

- ✅ Built-in (via HealthCheckService)
- ➕ **NEW**: `AspNetCore.HealthChecks.Uris` (optional)
  – If implementing HTTP health checks

### **HIGHLY RECOMMENDED** 🎯

| Package | Version | Purpose | Impact |
| --------- | --------- | --------- | -------- |
| **Polly** | 8.2.0+ | Resilience | Optional for later Z21 hardening |
| **Spectre.Console** | 0.50.0+ | Rich console output | Better diagnostics |
| **FluentValidation** | 11.8.0+ | Config validation | Catch bad config early |
| **MediatR** | 12.1.0+ | CQRS pattern | Cleaner architecture |

### **OPTIONAL BUT VALUABLE** 💡

| Package | Version | Purpose | Use Case |
| --------- | --------- | --------- | ---------- |
| **Mapster** | 8.0.0+ | High-perf object mapping | Domain conversions |
| **Dapper** | 2.1.0+ | Lightweight ORM | Solution persistence |
| **MessagePack** | 2.5.0+ | Binary serialization | Z21 data serialization |
| **SharpZipLib** | 1.4.0+ | ZIP compression | Solution/Backup export |
| **CsvHelper** | 30.0.0+ | CSV parsing | Export journeys/locomotives to CSV |
| **SkiaSharp** | 2.88.0+ | Graphics alternative | Win2D is already integrated |
| **OpenTelemetry** | 1.7.0+ | Distributed tracing | Production monitoring |

### **CI/CD & Build Tools** (DevDependencies)

| Package | Version | Purpose |
| --------- | --------- | --------- |
| **dotnet-format** | latest | Code style enforcement in CI/CD |
| **dotnet-coverage** | latest | Code coverage reporting |
| **SonarAnalyzer.CSharp** | 9.8.0+ | Static code analysis (SONAR) |

---

## 🔧 Recommended Installation Commands

```powershell
# Logging & Diagnostics (HIGH PRIORITY)
dotnet add MOBAflow package Serilog.Sinks.Async
dotnet add MOBAflow package Serilog.Enrichers.Environment
dotnet add MOBAflow package Serilog.Enrichers.Process

# Resilience (OPTIONAL - bei Bedarf später für Z21 ergänzen)
# dotnet add Backend package Polly

# Validation (MEDIUM PRIORITY)
dotnet add Common package FluentValidation

# Architecture Improvement (LOW PRIORITY - refactor over time)
dotnet add SharedUI package MediatR

# Build/CI Tools (DevDependencies)
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-coverage
```

---

## 🎯 Implementation Roadmap (by session)

**Session 33:** Polish Signal Box feature: visual alignment,
conditional rendering, test all configurations

**Session 34:** Polly (optional): later addition for Z21 resilience

**Session 35:** ✅ Track Plan Win2D Phase 1 (completed)

**Session 35:** Add FluentValidation for AppSettings validation

**Session 36:** (MediatR/CQRS – aktuell nicht geplant, Architektur passt)

**Session 37:** Mapster optional – siehe „Vorteil Mapster“ unten

**Vorteil Mapster:** Spart Boilerplate bei Domain ↔ ViewModel/DTO.
Statt vieler Zeilen manueller Property-Kopien genügt oft
`source.Adapt<Dest>()`. Weniger Fehler beim Erweitern von Modellen und oft
bessere Performance bei vielen Mappings.

Lohnt sich, wenn ihr viele Konvertierungen
(z. B. Locomotive/Journey ↔ Anzeige- oder API-Modelle) habt;
bei wenigen, einfachen Fällen bleibt es optional.

---

## ⚠️ Packages to AVOID

| Package | Reason |
| --------- | -------- |
| **Newtonsoft.Json (Json.NET)** | System.Text.Json is faster & lighter |
| **Entity Framework Core** | Overkill for MOBAflow; use Dapper instead |
| **Hangfire** | Not needed; use BackgroundService for deferred tasks |
| **NLog** | Serilog is already integrated; don't mix loggers |
