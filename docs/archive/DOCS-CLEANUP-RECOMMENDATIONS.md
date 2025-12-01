# Documentation Cleanup - Empfehlungen

**Datum**: 2025-01-01  
**Aktuell**: 57 Markdown-Dateien in `docs/`

---

## ✅ Behalten (Kern-Dokumentation)

### Architecture & Guidelines (9 Dateien)
1. **ARCHITECTURE.md** - Haupt-Architektur-Dokumentation
2. **CLEAN-ARCHITECTURE-FINAL-STATUS.md** ⭐ - Finale Clean Architecture Übersicht
3. **DI-INSTRUCTIONS.md** - Dependency Injection Guidelines
4. **BESTPRACTICES.md** - C# Coding Standards
5. **UX-GUIDELINES.md** - Detaillierte UX-Patterns
6. **MAUI-GUIDELINES.md** - MAUI-spezifische Guidelines
7. **UI-GUIDELINES.md** - Allgemeine UI Guidelines
8. **THREADING.md** - UI Thread Dispatching
9. **Z21-PROTOCOL.md** - Z21 Kommunikation

### Build & Development (4 Dateien)
10. **BUILD-ERRORS-STATUS.md** - Aktueller Build-Status
11. **BUILD-PERFORMANCE.md** - Build Optimierungen
12. **CI.md** - CI/CD Pipeline
13. **COPYRIGHT-HEADERS.md** - Copyright Header Automation

### Templates & Security (4 Dateien)
14. **README.md** - Projekt-Übersicht
15. **ISSUE_TEMPLATE.md** - Issue Template
16. **PR_TEMPLATE.md** - Pull Request Template
17. **SECURITY-SECRETS.md** - Security Guidelines

### Testing (2 Dateien)
18. **DI-REGISTRATION.md** - DI Registration Testing
19. **TESTING-SIMULATION.md** - Testing mit Fakes

---

## 📦 Archivieren (Session-Reports & Completed Tasks)

### Empfehlung: Nach `docs/archive/` verschieben (38 Dateien)

**Session-Berichte** (2 Dateien):
- SESSION-SUMMARY-2025-01-20-AFTERNOON.md
- SESSION-SUMMARY-2025-01-20.md

**Alte Clean Architecture Dokumentation** (3 Dateien):
- CLEAN-ARCHITECTURE-QUICKSTART.md
- CLEAN-ARCHITECTURE-STATUS-2025-12-01.md
- CLEAN-ARCHITECTURE-STATUS.md

**Abgeschlossene Migrations/Fix-Dokumentation** (20 Dateien):
- ASYNC-PATTERNS.md
- COPILOT-TROUBLESHOOTING.md
- DI-MVVM-CLEANUP.md
- EXPLORERPAGE-LAYOUT-FIX.md
- FACTORY-CLEANUP.md
- MANUAL-FIX-TRACK-POWER-BUTTON.md
- NEW-SOLUTION-FEATURE.md
- NEW-SOLUTION-IMPROVEMENTS.md
- SELECTION-SYNCHRONIZATION.md
- TEST-MIGRATION-GUIDE.md
- TRACK-POWER-BUTTON-FIX.md
- TREEVIEW-MIGRATION.md
- TREEVIEWBUILDER-DEPENDENCIES.md
- UNDO-REDO-HASUNSAVEDCHANGES-COMPLETE.md
- UNDO-REDO-INTEGRATION-ANALYSIS.md
- UNDO-REDO-REMOVAL.md
- WINUI-CONFIG-SPLITTER-COMPLETE.md
- WINUI-EXPLORER-UI-POLISH.md
- WINUI-FINAL-FIXES.md
- WINUI-FONT-SPLITTER-FINAL.md

**WinUI/MAUI UI-Verbesserungen** (4 Dateien):
- WINUI-NAVIGATION-CONNECTION-FIXES.md
- WINUI-TREEVIEW-FIX.md
- WINUI-UI-IMPROVEMENTS.md
- MAUI-UI-IMPROVEMENTS.md

**Performance & Build** (3 Dateien):
- MAUI-BUILD-PERFORMANCE.md
- MAUI-RESOURCE-LOADING-FIX.md
- VISUAL-STUDIO-PERFORMANCE.md

**Analysen & Deep-Dives** (5 Dateien):
- PROJECT-EVALUATION-2024.md
- SOLUTION-ANALYSIS-2025-11-27.md
- SOLUTION-ANALYSIS-REPORT.md
- SOLUTION-INSTANCE-ANALYSIS.md
- SOLUTION-INSTANCE-FLOW-VISUAL.md
- TECHNICAL-DEEP-DIVE.md

**Sonstige** (2 Dateien):
- PHASE2-CANCELLED.md
- Z21-FREEZE-INVESTIGATION.md

---

## 🎯 Resultat nach Cleanup

**Vorher**: 57 Dateien  
**Nachher**: 19 Dateien (Kern-Dokumentation)  
**Archiviert**: 38 Dateien

---

## 🚀 Manuelle Aktion

```powershell
# Archiv-Ordner erstellen
New-Item -Path "C:\Repos\ahuelsmann\MOBAflow\docs\archive" -ItemType Directory -Force

# Dateien archivieren (Beispiel)
$archiveFiles = @(
    "SESSION-SUMMARY-2025-01-20.md",
    "CLEAN-ARCHITECTURE-STATUS.md",
    # ... (siehe Liste oben)
)

foreach ($file in $archiveFiles) {
    $src = "C:\Repos\ahuelsmann\MOBAflow\docs\$file"
    if (Test-Path $src) {
        Move-Item $src "C:\Repos\ahuelsmann\MOBAflow\docs\archive\" -Force
    }
}
```

---

## 📋 Finale docs/ Struktur

```
docs/
├── archive/                    # Alte Session-Reports & abgeschlossene Tasks
├── ARCHITECTURE.md             # ⭐ Haupt-Architektur
├── CLEAN-ARCHITECTURE-FINAL-STATUS.md  # ⭐ Clean Architecture Status
├── DI-INSTRUCTIONS.md          # DI Guidelines
├── UX-GUIDELINES.md            # UX Patterns
├── BUILD-ERRORS-STATUS.md      # Aktueller Build-Status
├── README.md
└── ... (weitere 13 Kern-Dateien)
```

---

**Empfehlung**: Archivieren Sie die 38 Dateien manuell, um die Dokumentation übersichtlich zu halten.
