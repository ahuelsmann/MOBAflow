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
   - `ripgrep`
   - `filesystem`
   - `openapi`
   - `documents`
   - `markitdown`

---

## ✅ Pflicht: 6‑Schritte‑Workflow
Bei JEDER Implementierung:

1. **ANALYSE**  
   - Anforderungen verstehen  
   - Betroffene Dateien identifizieren (→ `ripgrep`)  
   - Muster/Duplikate finden  

2. **RECHERCHE**  
   - Bestehende Implementierungen (→ `ripgrep.search`)  
   - Dokumentation (→ `markitdown` oder `documents`)  
   - API (→ `openapi`)  
   - WinUI / .NET Docs (→ `microsoft-learn`)  

3. **PLAN**  
   - Immer das `plan()` Tool verwenden  
   - Plan muss enthalten:  
     - Betroffene Dateien  
     - Neue Klassen / Methoden  
     - Risiken  
     - Tests  

4. **IMPLEMENTIERUNG**  
   - Backend → ViewModel → View  
   - Nach jeder Datei: Build ausführen (VS Build Pipeline)
   - XAML: ThemeResource, keine Farben  
   - MVVM Toolkit: `[ObservableProperty]`, `[RelayCommand]`  

5. **VALIDIERUNG**  
   - Build  
   - Tests  
   - Linting / ReSharper  

6. **DOKUMENTATION**  
   - README.md aktualisieren  
   - Wiki falls Nutzer-Themen

---

## ✅ PFLICHT: Programmierprinzipien beachten: 
- SOLID; Single Responsibility Principle (SRP)
- DRY
- KISS (Keep It Simple, Stupid)
- Meaningful Names
- Kleine, fokussierte Methoden
- Konsistente Formatierung
- Separation of Concerns
- Klare Namespaces und Projektstruktur
- Sinnvolle Enums, Records, Interfaces
- Pattern-basierte APIs

---

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

---

## 📁 Projekt-Struktur

| Projekt | Zweck |
|---------|-------|
| `Domain/` | POCOs (Solution, Journey, Train, Workflow) |
| `Backend/` | Services (IZ21, WorkflowService) |
| `SharedUI/` | ViewModels |
| `WinUI/` | Windows Desktop App |

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
- `todos.instructions.md` – Offene Aufgaben / Roadmap  
- `naming-conventions.instructions.md` – C# Naming Standards (Protocol Constants)  
- `di-pattern-consistency.instructions.md` – DI-Regeln  

### Architektur & Frameworks
- `architecture.instructions.md` – Layer-Architektur  
- `backend.instructions.md` – Backend Details  
- `z21-backend.instructions.md` – Z21 Connection & Traffic Rules (CRITICAL)

### UI Frameworks
- `winui.instructions.md` – WinUI 3 Spezifika

### Code Quality
- `test.instructions.md` – Testing Best Practices