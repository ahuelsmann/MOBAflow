# MOBAflow Copilot Instructions

> Diese Datei wird IMMER geladen - hier stehen die wichtigsten Regeln.

## 🚫 VERBOTEN

1. **Terminal für Dateioperationen** - NIEMALS `run_command_in_terminal` für:
   - Erstellen von Dateien (→ `create_file` verwenden)
   - Ändern von Dateien (→ `replace_string_in_file` verwenden)
   - XAML/C#/JSON Dateien schreiben

2. **Sofort coden** - NIEMALS ohne vorherige Analyse beginnen

3. **Hardcodierte Farben** - IMMER `ThemeResource` in XAML

4. **Session-Details in dieser Datei** - NIEMALS "Completed This Session", Changelogs oder temporäre Notizen hier erfassen. Diese Datei enthält nur permanente Regeln.

5. **Separate README.md Dateien** - NIEMALS separate README.md in Unterordnern erstellen!
   - ✅ Nur EINE zentrale `README.md` im Root
   - ✅ Neue Inhalte als Kapitel in die zentrale README.md aufnehmen
   - ❌ KEINE `scripts/README.md`, `docs/README.md`, etc.
   - **Beispiel:** Für Script-Dokumentation → Kapitel in `README.md` (z.B. "## 🔧 Setup Scripts")

## ✅ PFLICHT: 5-Schritte-Workflow

**Bei JEDER Implementierung:**

1. **ANALYSE** - Anforderung verstehen, betroffene Dateien identifizieren
2. **PATTERNS** - Bestehende Patterns im Code suchen (`code_search`, `find_symbol`)
3. **PLAN** - Bei >2 Dateien das `plan()` Tool verwenden
4. **IMPLEMENTIEREN** - Backend → ViewModel → View, nach jeder Datei `run_build`
5. **VALIDIEREN** - Build prüfen, Fehler beheben

## ✅ PFLICHT: Patterns

### MVVM (CommunityToolkit.Mvvm)
- `[ObservableProperty]` für bindbare Properties
- `[RelayCommand]` für Commands
- Domain Models mit ViewModel wrappen

### DI (Constructor Injection)
- Pages: `public MyPage(MainWindowViewModel vm) => ViewModel = vm;`
- Services: Constructor Injection, kein Service Locator
- Registration: `services.AddTransient<View.MyPage>()`

### WinUI 3
- DispatcherQueue für UI-Updates vom Background-Thread
- DataTemplates in `EntityTemplates.xaml`, keine separaten UserControls
- ThemeResource für alle Farben

## 📁 Projekt-Struktur

| Projekt | Zweck |
|---------|-------|
| `Domain/` | POCOs (Solution, Journey, Train, Workflow) |
| `Backend/` | Services (IZ21, WorkflowService) |
| `SharedUI/` | ViewModels |
| `WinUI/` | Windows Desktop App |

## 📖 Benutzer-Dokumentation

**Wiki-Pfad:** `docs/wiki/`

Bei Fragen zu Features oder Setup:
- `docs/wiki/INDEX.md` - Haupt-Index für alle Plattformen
- `docs/wiki/MOBAFLOW-USER-GUIDE.md` - WinUI Benutzerhandbuch
- `docs/wiki/AZURE-SPEECH-SETUP.md` - Azure Speech Service einrichten
- `docs/wiki/PLUGIN-DEVELOPMENT.md` - Plugin-Entwicklung

**Regel:** Verweise Benutzer ohne Entwickler-Hintergrund auf das Wiki (nicht auf README.md oder Code).

## ⚠️ Bei Unsicherheit

Microsoft-Dokumentation via `azure_documentation` Tool konsultieren BEVOR Code geschrieben wird.

## 📚 Weitere Instructions

Details in `.github/instructions/`:

**Workflow & Patterns:**
- `todos.instructions.md` - **Offene Aufgaben & Quality Roadmap**
- `implementation-workflow.instructions.md` - Detaillierter 5-Schritte-Workflow
- `di-pattern-consistency.instructions.md` - Dependency Injection Patterns
- `hasunsavedchanges-patterns.instructions.md` - Ungespeicherte Änderungen Pattern
- `xaml-page-registration.instructions.md` - XAML Page Registration Pattern

**Architektur & Frameworks:**
- `architecture.instructions.md` - Layer-Architektur
- `backend.instructions.md` - Backend Layer Details
- `dotnet-framework.instructions.md` - .NET Framework Best Practices
- `collections.instructions.md` - Collection Patterns

**UI Frameworks:**
- `winui.instructions.md` - WinUI 3 Spezifika
- `winui3-best-practices-steps-4-12.md` - WinUI 3 Best Practices (Steps 4-12)
- `maui.instructions.md` - .NET MAUI Spezifika
- `blazor.instructions.md` - Blazor Spezifika
- `mvvm-best-practices.instructions.md` - MVVM Details
- `fluent-design.instructions.md` - Fluent Design System

**Track Plan Editor:**
- `geometry.md` - Geometrie-Berechnungen
- `rendering.md` - Rendering Pipeline
- `snapping.md` - Snap-to-Grid & Snap-to-Connect
- `topology.md` - Track Topology

**Code Quality:**
- `self-explanatory-code-commenting.instructions.md` - Code-Kommentar-Regeln
- `no-special-chars.instructions.md` - Keine Sonderzeichen in Identifiern
- `test.instructions.md` - Testing Best Practices

**Tooling:**
- `terminal.instructions.md` - PowerShell-Regeln
- `powershell.instructions.md` - PowerShell Best Practices
- `github-actions-ci-cd-best-practices.instructions.md` - GitHub Actions CI/CD
- `editor-behavior.md` - Editor-Verhalten
- `prompt.instructions.md` - Prompt Engineering
- `instructions.instructions.md` - Meta-Instructions (Instruction-Dateien schreiben)
