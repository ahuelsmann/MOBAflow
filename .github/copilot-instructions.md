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

## ⚠️ Bei Unsicherheit

Microsoft-Dokumentation via `azure_documentation` Tool konsultieren BEVOR Code geschrieben wird.

## 📚 Weitere Instructions

Details in `.github/instructions/`:
- `implementation-workflow.instructions.md` - Detaillierter 5-Schritte-Workflow
- `architecture.instructions.md` - Layer-Architektur
- `mvvm-best-practices.instructions.md` - MVVM Details
- `fluent-design.instructions.md` - Fluent Design System
- `terminal.instructions.md` - PowerShell-Regeln
