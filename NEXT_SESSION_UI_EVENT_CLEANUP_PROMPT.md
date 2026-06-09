# Prompt Fuer Die Naechste UI-/Event-Cleanup-Sitzung

Du arbeitest im Repository `C:/Repo/ahuelsmann/MOBAflow`.

Bitte antworte auf Deutsch und beachte `AGENTS.md` sowie `.github/copilot-instructions.md`. Arbeite konservativ, lies relevanten Code vor Aenderungen und mache keine unrelated Refactors. Der Arbeitsbaum kann bereits Aenderungen enthalten; vorhandene Aenderungen nicht zuruecksetzen.

## Ausgangslage

In der vorherigen Refactoring-Sitzung wurden die Punkte 1-6 aus `NEXT_SESSION_REFACTORING_PROMPT.md` umgesetzt:

- `ActionExecutionContext` wird pro Workflow-Ausfuehrung isoliert ueber eine DI-faehige Factory erzeugt.
- `MobaRuntimeService` startet AutoConnect/Initial-Snapshot nicht mehr im Konstruktor, sondern explizit ueber `StartAsync`.
- `MauiViewModel` und `TrainControlViewModel` haben deterministischen Cleanup fuer EventBus-, Legacy- und Journey-Subscriptions.
- UI-ViewModels verarbeiten bei EventBus-Injektion keine zusaetzlichen Legacy-Runtime-Snapshot-Events.
- `LocomotivesPage` nutzt eine zentrale Clear-Regel fuer nicht pixelpersistente Star-Spalten.
- Regressionstests wurden fuer Context-Isolation, Lifecycle, Cleanup, EventBus-Kanal, Layout, Multiplexer und RuntimeSnapshotProjector ergaenzt.

Validierung aus der vorherigen Sitzung:

- `dotnet test "Test/Test.csproj"` war gruen: `492/492`.
- `dotnet build "MOBAflow/MOBAflow.csproj" --no-restore --no-dependencies` war erfolgreich.
- ReadLints meldete keine Diagnostics in den geaenderten Bereichen.

Hinweis: Vor und waehrend der vorherigen Sitzung gab es bereits weitere Aenderungen im Arbeitsbaum, u. a. `Directory.Packages.props`, `MOBAflow/solution.json` und ggf. IDE-Artefakte. Diese nicht zuruecksetzen, sondern vor einem Commit bewusst einordnen.

## Ziel Der Naechsten Sitzung

Die UI-/Runtime-Altpfade weiter abbauen und den Arbeitsbaum commitfaehig strukturieren:

1. `GridSplitter` auf geeigneten einfachen Seiten durch das neue Resize-Behavior ersetzen.
2. Legacy Runtime Events aus `IMobaRuntime` entfernen und UI konsequent ueber EventBus/Projection betreiben.
3. Riskante Fire-and-forget-/`ContinueWith`-Pfade vereinheitlichen.
4. Tests aktualisieren und Validierung ausfuehren.
5. Arbeitsbaum fuer einen sauberen Commit vorbereiten, ohne fremde Aenderungen zu verlieren.

## Was Statt `GridSplitter` Verwendet Werden Soll

Verwende das Muster aus `MOBAflow/View/LocomotivesPage.xaml`:

- Am Root-`Grid`: `GridColumnResizeBehavior` mit `PersistenceKey`.
- Als sichtbarer Splitter: schmaler `Border` mit `GridColumnResizeBehavior.SplitterMode`.
- Pixel-Spalten: Bindung an `LayoutColumnWidthsViewModel` mit `DoubleToGridLengthConverter`.
- Star-Spalten: weiter ueber strukturierte Settings wie `PropertiesColumnStarValue`; keine Star-Spalten als Pixelwerte in `Layout.ColumnWidths` persistieren.
- Fuer Star-Spalten, die nicht im Pixel-Dictionary liegen duerfen, `LayoutColumnWidthsViewModel.ClearColumnWidth(...)` verwenden.

### Geeignete Erste Migrationen

Priorisiert:

1. `MOBAflow/View/GoodsWagonPage.xaml` und `.xaml.cs`
2. `MOBAflow/View/PassengerWagonPage.xaml` und `.xaml.cs`
3. `MOBAflow/View/WorkflowsPage.xaml` und `.xaml.cs`
4. `MOBAflow/View/StationsPage.xaml` und `.xaml.cs`

Vorerst nicht migrieren oder nur nach separater Analyse:

- `JourneysPage` wegen nested Grids und mehreren Visibility-gebundenen Splittern.
- `TrackPlanPage` wegen Canvas-/Pointer-Interaktion und gemischter Persistenz.
- `SignalBoxPage` wegen interaktiver Canvas-Mittelspalte.
- `TrainsPage`, `SolutionPage`, `MonitorPage` wegen Star-zu-Star-Semantik.

Validierung fuer jede migrierte Seite:

- WinUI-Build.
- Resize jeder Trennlinie.
- Collapse/Expand vor und nach Resize.
- Neustart-/Reload-Persistenz.
- Light/Dark Theme des `Border`-Splitters.

## Legacy Runtime Events Entfernen

Problem:

- `IMobaRuntime` enthaelt noch direkte UI-nahe Events: `SnapshotChanged`, `FeedbackReceived`, `TrafficPacketLogged`.
- Snapshots laufen bereits ueber `RuntimeSnapshotChangedEvent`.
- Feedbacks laufen bereits ueber `FeedbackReceivedEvent`.
- Traffic-Packets haben noch keinen EventBus-Ersatz.

Ziel:

- `IMobaRuntime.SnapshotChanged` entfernen.
- `IMobaRuntime.FeedbackReceived` entfernen.
- `MauiViewModel` und `TrainControlViewModel` sollen `IEventBus` verpflichtend bekommen; Legacy-Fallback-Handler entfernen.
- `MobaRuntimeService.PublishSnapshot()` soll nur noch `RuntimeSnapshotChangedEvent` publizieren.
- Neues EventBus-Event fuer Traffic einfuehren, z. B. `Z21TrafficPacketLoggedEvent`.
- `MainWindowViewModel.Z21.cs` von `_mobaRuntime.TrafficPacketLogged` auf EventBus-Subscription umstellen.
- `GetTrafficPackets()` und `ClearTrafficMonitor()` als Runtime Query/Command behalten.

Betroffene Stellen pruefen:

- `Backend/Interface/IMobaRuntime.cs`
- `Backend/Service/MobaRuntimeService.cs`
- `Backend/Service/MobaRuntimeService.Z21Handlers.cs`
- `SharedUI/ViewModel/MainWindowViewModel.Z21.cs`
- `SharedUI/ViewModel/MauiViewModel.cs`
- `SharedUI/ViewModel/TrainControlViewModel.cs`
- Tests mit `Moq.Raise(... SnapshotChanged ...)`

## EventBus/Projection Und Dispatcher

Pruefe verbleibende manuelle `_uiDispatcher.InvokeOnUi(...)`-Nutzung:

- EventBus-Handler duerfen keine manuelle UI-Dispatcher-Marshal-Logik brauchen, weil `UiThreadEventBusDecorator` zentral marshalt.
- Direkte async-/UI-Callbacks duerfen Dispatcher weiter nutzen, wenn sie nicht ueber EventBus laufen.
- Ziel ist Klassifizierung und gezielte Entfernung, nicht blinde Entfernung aller Dispatcher-Aufrufe.

## Async-Fire-and-forget Vereinheitlichen

Priorisierte Fundstellen:

- `SharedUI/ViewModel/MainWindowViewModel.Z21.cs`: unobserved `_ = _mobaRuntime.RequestSystemStateAsync();`.
- `Backend/Manager/StationManager.cs` und `Backend/Manager/PlatformManager.cs`: manuelles `ContinueWith(... OnlyOnFaulted)` durch `Observe(...)` ersetzen.
- `SharedUI/ViewModel/MauiViewModel.cs` und `SharedUI/ViewModel/TrainControlViewModel.cs`: lokale `QueueBackgroundTask`-Helper auf `task?.Observe(...)` vereinheitlichen.
- `MOBAflow/Converter/AssetNameToSvgImageSourceConverter.cs`: `async void`/unobserved SVG-Load-Pfade pruefen und, wenn sinnvoll, auf synchronen Adapter + `Observe(...)` umstellen.
- `MOBAflow/View/TrackPlanPage.EditorFeatures.cs`: async Click-Lambda mit Fehlerpfad versehen.
- `MOBAsmart/Service/SettingsService.cs`: `ContinueWith` ohne Fehlerbeobachtung pruefen.

Vorgabe:

- Keine `.Result`/`.Wait()`.
- Keine swallowed Exceptions ohne Logging.
- Wenn Aufrufer awaiten kann, `async Task`/`AsyncRelayCommand` bevorzugen.
- Wenn Fire-and-forget erforderlich ist, `Observe(...)` oder `SafeFireAndForget(...)` mit Logging verwenden.

## Teststrategie

Ergaenzen/anpassen:

- EventBus-only Tests fuer `MauiViewModel` und `TrainControlViewModel`.
- Test fuer neues `Z21TrafficPacketLoggedEvent` und Shutdown-Unsubscribe in `MainWindowViewModel`.
- Tests entfernen oder anpassen, die Legacy-`IMobaRuntime.SnapshotChanged`/`FeedbackReceived` raisen.
- Layout-Persistenztests fuer weitere migrierte einfache Seiten.
- Optional: Fehlerpfad-Test fuer `RefreshZ21StatusCommand` oder vergleichbare Fire-and-forget-Refactorings.

## Arbeitsbaum Commitfaehig Machen

Am Ende:

1. `git status --short` pruefen.
2. `git diff --stat` und relevante Diffs pruefen.
3. Fremde/unrelated Aenderungen nicht zuruecksetzen.
4. `.vs`-Artefakte nicht aufnehmen.
5. Falls ein Commit gewuenscht ist: erst nach expliziter Aufforderung committen.
6. Commit-Message auf Englisch im Conventional-Commits-Format.

## Abschlusskriterien

- `dotnet test "Test/Test.csproj"` ist gruen.
- WinUI-Build ist gruen:

```powershell
dotnet build "MOBAflow/MOBAflow.csproj" --no-restore --no-dependencies
```

- ReadLints fuer geaenderte Dateien ohne neue Diagnostics.
- Keine Legacy UI-Events mehr in `IMobaRuntime`, sofern technisch vollstaendig ersetzt.
- In der Abschlussantwort klar nennen:
  - was geaendert wurde,
  - welche Tests/Builds liefen,
  - welche Risiken oder Folgearbeiten bleiben,
  - welche Dateien ggf. bewusst nicht migriert wurden.
