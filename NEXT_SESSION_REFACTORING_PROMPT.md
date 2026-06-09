# Prompt Für Die Nächste Refactoring-Sitzung

Du arbeitest im Repository `C:/Repo/ahuelsmann/MOBAflow`.

Bitte antworte auf Deutsch und beachte `AGENTS.md` sowie `.github/copilot-instructions.md`. Arbeite konservativ, lies den relevanten Code vor Änderungen und mache keine unrelated Refactors. Der Arbeitsbaum kann bereits Änderungen enthalten; vorhandene Änderungen nicht zurücksetzen.

## Ausgangslage

In der vorherigen Sitzung wurde eine Refactoring-Roadmap begonnen und teilweise umgesetzt:

- `MultiplexerCommandResolver` wurde eingeführt und in `ActionExecutor` sowie `MobaRuntimeService.SetSignalAspectAsync` genutzt.
- Backend-DI wurde mit `AddMobaBackendServices()` zentralisiert und in WinUI/MAUI eingebunden.
- `RuntimeSnapshotChangedEvent` wurde eingeführt; UI-ViewModels nutzen zunehmend den EventBus-Snapshot-Kanal.
- `RuntimeSnapshotProjector` wurde eingeführt und in `MainWindowViewModel`, `MauiViewModel` und `TrainControlViewModel` verwendet.
- `LocomotivesPage` wurde als Layout-Pilot auf `GridColumnResizeBehavior` umgestellt; `GridSplitter` wurde entfernt und die Properties-Spalte bleibt wieder Star-basiert.
- `MainWindowViewModel` speichert EventBus-Subscription-IDs und meldet sie beim Shutdown ab.

Validierung aus der vorherigen Sitzung:

- `dotnet test Test/Test.csproj` war grün: `473/473`.
- Gezielte Tests nach den letzten Review-Fixes waren grün: `20/20`.
- WinUI-Builds für den Layout-Pilot wurden erfolgreich abgeschlossen.

Hinweis: Vor der Refactoring-Sitzung waren bereits Änderungen an `MOBAflow/Controls/FunctionSymbolPickerDialog.xaml`, `MOBAflow/Controls/FunctionSymbolPickerDialog.xaml.cs` und `MOBAflow/solution.json` im Arbeitsbaum. Diese nicht zurücksetzen, sofern sie nicht ausdrücklich Teil der Aufgabe werden.

## Ziel Der Nächsten Sitzung

Die verbliebenen Architektur-Risiken aus der Review abbauen und die Refactorings stabilisieren.

## Priorisierte Aufgaben

### 1. `ActionExecutionContext` entkoppeln

Problem:

- `ActionExecutionContext` wird aktuell als Singleton registriert.
- Der Typ enthält mutable Laufzeitdaten wie aktuelle Station, Platform oder Journey.
- Parallele Feedbacks/Workflows können diesen Kontext überschreiben.

Ziel:

- Shared Dependencies wie `IZ21`, `ISpeakerEngine`, `ISoundPlayer` weiter zentral bereitstellen.
- Pro Workflow-Ausführung einen frischen Kontext oder eine Factory/Clone-Methode verwenden.
- Mutable per-run Daten dürfen nicht in einem Singleton geteilt werden.

Validierung:

- Bestehende Workflow-/ActionExecutor-Tests müssen grün bleiben.
- Einen Concurrency- oder Isolationstest ergänzen, der zwei Workflow-Kontexte parallel simuliert.

### 2. `MobaRuntimeService` Lifecycle bereinigen

Problem:

- Der Konstruktor ruft `PublishSnapshot()` und `TryAutoConnectToZ21Async().Observe(...)` auf.
- DI-Auflösung startet dadurch Timer/Netzwerk-/AutoConnect-Logik.

Ziel:

- Konstruktor nur für Dependency Assignment und Event-Wiring verwenden.
- AutoConnect und initiale Runtime-Initialisierung in eine explizite `StartAsync`/`InitializeAsync`-Methode verschieben.
- Hosts müssen diese Methode bewusst starten.

Betroffene Stellen prüfen:

- `Backend/Service/MobaRuntimeService.cs`
- `MOBAflow/App.xaml.cs`
- `MOBAsmart/MauiProgram.cs`
- ViewModels, die `IMobaRuntime.Current` direkt nach Konstruktion erwarten

Validierung:

- TrackPower-/Runtime-Tests anpassen oder ergänzen.
- Sicherstellen, dass Start/Shutdown idempotent bleibt.

### 3. Subscription-Cleanup vervollständigen

Problem:

- `MainWindowViewModel` räumt EventBus-Subscriptions jetzt auf.
- `MauiViewModel` und `TrainControlViewModel` haben noch keinen vollständigen deterministischen Cleanup.
- `TrainControlViewModel` hängt zusätzlich an `MainWindowViewModel.PropertyChanged` und wechselnden `JourneyViewModel.PropertyChanged`-Events.

Ziel:

- `MauiViewModel` mit explizitem `StopAsync`, `Dispose` oder vergleichbarem Lifecycle versehen.
- EventBus-Subscription-IDs speichern und abmelden.
- Legacy Runtime-Events abmelden, wenn der Fallback-Pfad genutzt wird.
- Network/Profile-Notifier und CancellationTokenSources sauber beenden.
- `TrainControlViewModel` soll beobachtete Journey explizit tracken und beim Wechsel korrekt abmelden.

Validierung:

- Tests analog `MainWindowViewModelShutdownTests` ergänzen.
- Test für Journey-Wechsel in `TrainControlViewModel`: alte Journey darf danach keine UI-Änderung mehr auslösen.

### 4. Runtime-Kanal finalisieren

Problem:

- `IMobaRuntime` bietet weiterhin direkte Events (`SnapshotChanged`, `FeedbackReceived`, `TrafficPacketLogged`).
- UI soll den EventBus verwenden, weil `UiThreadEventBusDecorator` zentral auf den UI-Thread marshalt.

Ziel:

- UI-seitig konsequent `RuntimeSnapshotChangedEvent` über EventBus verwenden.
- Legacy-Events entweder klar dokumentieren oder schrittweise aus UI-Konsum entfernen.
- Keine manuellen Dispatcher-Aufrufe in EventBus-Handlern.

Validierung:

- Tests sicherstellen, dass ViewModels bei EventBus-Injektion nicht zusätzlich Legacy-Events verarbeiten.

### 5. Layout-Persistenz weiter zentralisieren

Problem:

- `LocomotivesPage` ist ein verbesserter Pilot, aber Layoutlogik liegt noch teilweise im Code-Behind.
- `GridColumnResizeBehavior` ist pixelzentriert und nicht für Star-Spalten geeignet.

Ziel:

- Entscheiden: Behavior nur für explizite Pixel-Spalten nutzen oder Persistenzmodell um `GridUnitType`/Star-Werte erweitern.
- Code-Behind weiter reduzieren.
- Danach weitere einfache Pages migrieren, erst wenn der Pilot stabil ist.

Validierung:

- WinUI-Build: `dotnet build MOBAflow/MOBAflow.csproj --no-restore --no-dependencies`
- Wenn möglich: manuelle UI-Prüfung von `LocomotivesPage` in Light/Dark Theme.

### 6. Testabdeckung erweitern

Ergänzen:

- Contract-Tests für `MultiplexerCommandResolver` gegen alle von `MultiplexerHelper.GetSupportedAspects(...)` gelieferten Aspekte.
- Tests für `signalArticleNumber = null`, alle Offsets 0..3, unbekannte Multiplexer, Grenzadressen und alle InvertPolarity-Flags.
- Vollständigere Mapping-Tests für `RuntimeSnapshotProjector`.
- Cleanup-Tests für `MauiViewModel` und `TrainControlViewModel`.
- Layout-Persistenztests, soweit platform-neutral extrahierbar.

## Empfohlene Reihenfolge

1. `ActionExecutionContext` entkoppeln.
2. `MobaRuntimeService` Lifecycle bereinigen.
3. Cleanup für `MauiViewModel` und `TrainControlViewModel`.
4. Runtime-Kanal dokumentieren/finalisieren.
5. Layout-Pilot weiter abstrahieren.
6. Contract-/Regressionstests ergänzen.

## Abschlusskriterien

- `dotnet test Test/Test.csproj` ist grün.
- Für WinUI-relevante Änderungen zusätzlich projektbezogen bauen:

```powershell
dotnet build "MOBAflow/MOBAflow.csproj" --no-restore --no-dependencies
```

- Keine bestehenden, fremden Änderungen zurücksetzen.
- In der Abschlussantwort klar nennen:
  - was geändert wurde,
  - welche Tests/Builds liefen,
  - welche Risiken oder Folgearbeiten bleiben.
