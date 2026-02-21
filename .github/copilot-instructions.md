# MOBAflow Copilot Instructions

> Diese Datei wird IMMER geladen - hier stehen die wichtigsten Regeln.

## 🚫 VERBOTEN
1. **Sofort coden** - NIEMALS ohne vorherige Analyse beginnen

2. **Hardcodierte Farben** - IMMER `ThemeResource` in XAML

3. **Session-Details in dieser Datei** - NIEMALS "Completed This Session", Changelogs oder temporäre Notizen hier erfassen. Diese Datei enthält nur permanente Regeln.

4. **Separate README.md Dateien** - NIEMALS separate README.md in Unterordnern erstellen!
   - ✅ Nur EINE zentrale `README.md` im Root
   - ✅ Neue Inhalte als Kapitel in die zentrale README.md aufnehmen
   - ❌ KEINE `scripts/README.md`, `docs/README.md`, etc.
   - **Beispiel:** Für Script-Dokumentation → Kapitel in `README.md` (z.B. "## 🔧 Setup Scripts")

5. ❌ **NIE Dateinamen, Klassen oder APIs raten** — IMMER Tools verwenden:
   - `ripgrep` / `file_search`
   - `filesystem` / `get_file`
   - `openapi` / `get_web_pages`
   - `documents` / `markitdown`

6. ❌ **WinUI-Projektdatei-Pattern verboten**
   - Keine `<ItemGroup><Page Remove="View\DockingPage.xaml" /></ItemGroup>`
   - Keine `<Compile Update="View\DockingPage.xaml.cs"><DependentUpon>DockingPage.xaml</DependentUpon></Compile>`

7. ❌ **Keine TODOs im Code** — in Azure DevOps (Work Item) oder in `todos.instructions.md` dokumentieren
   - Bevorzugt: Task/Feature in Azure DevOps (Projekt MOBAflow) anlegen
   - Exception: Temporäre Marker während aktiven Debug (mit Datum)
   - Regel: Vor Commit entfernen oder in ADO/todos dokumentieren

---

## ✅ Pflicht: 6‑Schritte‑Workflow
Bei JEDER Implementierung:

1. **ANALYSE**  
   - Anforderungen verstehen  
   - Betroffene Dateien identifizieren (→ `file_search`, `get_files_in_project`)  
   - Muster/Duplikate finden (→ `code_search`)  
   - Bestehende Tests identifizieren (→ `get_tests`)

2. **RECHERCHE**  
   - Bestehende Implementierungen (→ `find_symbol`, `code_search`)  
   - Dokumentation (→ `.github/instructions/*.md`)  
   - API (→ `openapi` if available)  
   - .NET/WinUI Docs  

3. **PLAN**  
   - IMMER `plan()` Tool verwenden  
   - Plan muss enthalten:  
     - Betroffene Dateien  
     - Neue Klassen / Methoden  
     - Änderungen an bestehenden Klassen  
     - Risiken & Dependencies  
     - Test-Strategie  

4. **IMPLEMENTIERUNG**  
   - Backend → ViewModel → View  
   - Nach jeder Datei: `get_errors()` für die Datei  
   - XAML: `ThemeResource`, keine Farben  
   - MVVM Toolkit: `[ObservableProperty]`, `[RelayCommand]`  
   - Async/Await: Nie `.Result` oder `.Wait()`  

5. **VALIDIERUNG**  
   - `run_build()` am Ende  
   - `run_tests()` für relevante Test-Projekte  
   - Code-Qualität: `.editorconfig` Compliance  
   - Keine new Warnings in ReSharper  

6. **DOKUMENTATION**  
   - README.md aktualisieren (wenn User-Feature)  
   - `todos.instructions.md` aktualisieren (Session-Status)  
   - Inline-Comments: NUR für komplexe Logik  
   - Public APIs: XML-Dokumentation (`/// <summary>`)

---

## ✅ PFLICHT: Programmierprinzipien beachten: 
- **SOLID**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
- **DRY**: Don't Repeat Yourself (maximal 2x Copy-Paste → Extract Method/Class)
- **KISS**: Keep It Simple, Stupid (< 20 Zeilen pro Methode wenn möglich)
- **Meaningful Names**: Nicht "x", "temp", "data" — Intention klar machen
- **Kleine, fokussierte Methoden**: Max 20-25 Zeilen
- **Konsistente Formatierung**: `.editorconfig` befolgen (auto via VS)
- **Separation of Concerns**: Domain, ViewModel, View streng trennen
- **Klare Namespaces**: `Moba.<Layer>.<Feature>` Struktur
- **Sinnvolle Enums, Records, Interfaces**: Nicht alles in Klassen packen
- **Pattern-basierte APIs**: Fluent Builders, Observer Pattern wo sinnvoll

---

## ✅ PFLICHT: Patterns

### MVVM (CommunityToolkit.Mvvm)
- `[ObservableProperty]` für bindbare Properties  
- `[RelayCommand]` für Commands  
- Domain Models separieren von ViewModels
- **NIEMALS** Async/Await mit `.Result` oder `.Wait()`

### DI (Constructor Injection)
- Pages: `public MyPage(MainWindowViewModel vm) => ViewModel = vm;`
- Services: Constructor Injection, kein Service Locator  
- Registration: `services.AddTransient<View.MyPage>()`  
- Validation: `IProjectValidator` pattern

### WinUI 3
- DispatcherQueue für UI-Updates vom Background-Thread  
- DataTemplates in `EntityTemplates.xaml`, keine separaten UserControls  
- ThemeResource für alle Farben/Brushes  
- x:Bind (compile-time binding) für Performance  

### JSON Validation
- Schema in `WinUI/Build/Schemas/` definieren
- Pre-commit Hook prüft automatisch
- `ProjectValidator` für Completeness Checks
- Siehe: `.github/instructions/di-pattern-consistency.instructions.md`

---

## 📁 Projekt-Struktur

| Projekt | Zweck | Beispiel |
|---------|-------|---------|
| `Domain/` | POCOs (Solution, Journey, Train, Workflow) | `Project.cs`, `Locomotive.cs` |
| `Backend/` | Services (IZ21, WorkflowService, ProjectValidator) | `WorkflowService.cs` |
| `SharedUI/` | ViewModels (Multi-Platform) | `SignalBoxViewModel.cs` |
| `WinUI/` | Windows Desktop App | `SignalBoxPage.xaml` |
| `MAUI/` | Mobile App (Android) | `MauiProgram.cs` |
| `Common/` | Shared Utilities (Validation, Events) | `JsonValidationService.cs` |
| `Test/` | Unit Tests | `ProjectValidatorTests.cs` |

---

## ⚙️ MCP-Tools verwenden

Wenn Aufgaben Dateizugriff, Suche oder Dokumentanalyse betreffen:

1. **Ripgrep** für Code-Suche  
   - Immer zuerst `ripgrep.search()` benutzen.  
   - Ziel: Existierende Patterns finden → Konsistenz sicherstellen.

2. **Filesystem** für Dateizugriff  
   - Nie raten → `filesystem.read_file()` verwenden.  
   - Schreiben nur, wenn explizit vom User gefordert.

3. **MarkItDown** oder **Documents**  
   - PDFs, DOCX, PPTX, HTML → zuerst konvertieren, dann analysieren.

4. **OpenAPI**  
   - Für REST-APIs:  
     → API-Schema lesen, DTOs prüfen, Testaufrufe durchführen.

5. **Azure / Azure DevOps / GitHub**  
   - Nur bei Pipelines, PRs, Issues, Repo-bezogenen Aufgaben.

---

## 📖 Benutzer-Dokumentation

**Wiki-Pfad:** `docs/wiki/`

Bei Fragen zu Features oder Setup:
- `docs/wiki/INDEX.md` – Haupt-Index  
- `docs/wiki/MOBAFLOW-USER-GUIDE.md` – WinUI Benutzerhandbuch  
- `docs/wiki/AZURE-SPEECH-SETUP.md` – Azure Speech Service einrichten  
- `docs/wiki/PLUGIN-DEVELOPMENT.md` – Plugin-Entwicklung  

**Regel:**  
Nutzer ohne Entwickler-Hintergrund → IMMER ins Wiki verweisen.

---

## ⚠️ Bei Unsicherheit
Microsoft-Dokumentation über das MCP‑Tool `microsoft-learn` abrufen.

---

## 📚 Weitere Instructions

Details in `.github/instructions/`:

### Workflow & Patterns
- **Offene Arbeit / Roadmap:** **Azure DevOps** (Projekt MOBAflow) ist die maßgebliche Quelle. Bei Fragen wie „was ist offen?“ oder „Features/Tasks“ zuerst das **Azure-DevOps-MCP** nutzen (Work Items, Features, Backlog). `todos.instructions.md` kann weiterhin für Session-Historie oder technische Notizen genutzt werden.
- `todos.instructions.md` – optional: Session-Status, technische Roadmap-Notizen (wenn nicht in ADO abgebildet)
- `naming-conventions.instructions.md` – C# Naming Standards (Protocol Constants)  
- `di-pattern-consistency.instructions.md` – DI-Regeln  
- `plan-completion.instructions.md` – Plan-Validierung, Build-Checks  

### Tools & Hooks
- `.git/hooks/README.md` – Git Hooks Dokumentation
- `WinUI/Build/ValidateJsonConfiguration.ps1` – JSON Validator
- `WinUI/Build/Schemas/` – JSON Schema Definitionen

---

## 🔍 Qualitäts-Checklist für Copilot-Code

Vor **Commit** IMMER überprüfen:
- [ ] `.editorconfig` befolgt (Formatting)
- [ ] Keine `TODO` Comments (→ Work Item in Azure DevOps oder todos.instructions.md)
- [ ] Keine Magic Numbers (→ Named Constants)
- [ ] Keine `.Result` / `.Wait()` (→ `await`)
- [ ] Constructor Injection statt `new Service()`
- [ ] `[ObservableProperty]` für MVVM Properties
- [ ] XML-Docs für public APIs (`/// <summary>`)
- [ ] Tests geschrieben (Enumerable.Range statt for-Loops)
- [ ] `run_build()` erfolgreich
- [ ] `run_tests()` alle bestanden
- [ ] Keine neuen ReSharper Warnings
- [ ] README/todos.md aktualisiert