---
description: 'Workflow für strukturierte Implementierung: Analyse, Best Practices, Fluent Design, Plan, dann Code.'
applyTo: '**'
---

# Implementation Workflow

> **PFLICHT:** Vor jeder Implementierung diesen Workflow befolgen.

---

## 🔄 Der 5-Schritte-Workflow

### 1️⃣ ANALYSE (Verstehen)

**Vor dem Coden:**
- [ ] Anforderung vollständig verstehen
- [ ] Betroffene Dateien identifizieren (get_file, file_search, code_search)
- [ ] Bestehende Patterns im Code erkennen
- [ ] Abhängigkeiten und Auswirkungen analysieren

**Fragen:**
- Was genau soll erreicht werden?
- Welche Komponenten sind betroffen?
- Gibt es bereits ähnliche Implementierungen im Projekt?

---

### 2️⃣ BEST PRACTICES (Regeln prüfen)

**Instruction-Dateien konsultieren:**
- `architecture.instructions.md` - Layer-Zugehörigkeit prüfen
- `mvvm-best-practices.instructions.md` - ViewModel-Patterns
- `winui.instructions.md` - WinUI 3 spezifische Patterns
- `self-explanatory-code-commenting.instructions.md` - Kommentar-Regeln

**Code-Regeln beachten:**
- SOLID-Prinzipien
- DRY (Don't Repeat Yourself)
- Async/Await korrekt verwenden
- Null-Checks mit `ArgumentNullException.ThrowIfNull()`
- Keine hardcodierten Strings/Farben

---

### 3️⃣ FLUENT DESIGN (UI-Guidelines)

**Bei UI-Änderungen IMMER prüfen:**
- `fluent-design.instructions.md` konsultieren
- ThemeResource statt hardcodierte Farben
- 8px Grid-System für Spacing
- Korrekte TextBlock-Styles (TitleTextBlockStyle, BodyTextBlockStyle, etc.)
- VisualStateManager für Responsive Layout (Compact/Medium/Wide)
- Acrylic/Mica für Backgrounds wo passend

**Checkliste:**
- [ ] ThemeResource für alle Farben?
- [ ] Spacing in 8px-Schritten (8, 16, 24, 32)?
- [ ] Responsive Layout mit VisualStateManager?
- [ ] FontIcon/SymbolIcon statt Text für Icons?

---

### 4️⃣ PLAN (Strukturieren)

**Bei komplexen Aufgaben (>2 Dateien, >40 LOC):**

```markdown
# Titel

## Steps
1. [Datei] Aktion beschreiben
2. [Datei] Nächste Aktion
3. Build und Test
```

**Plan-Tool verwenden:**
- `plan()` für Multi-File-Änderungen
- `update_plan_progress()` nach jedem Schritt
- `record_observation()` bei Problemen
- `finish_plan()` am Ende

**Einfache Aufgaben (≤2 Dateien, ≤40 LOC):**
- Kurze Erklärung was geändert wird
- Direkt implementieren

---

### 5️⃣ IMPLEMENTIERUNG (Coden)

**Reihenfolge:**
1. Backend/Domain zuerst (falls betroffen)
2. ViewModel/Service
3. XAML/View
4. Build prüfen
5. Testen

**Während der Implementierung:**
- Kleine, fokussierte Änderungen
- Nach jeder Datei Build prüfen
- Bei Fehlern: `record_observation()` → Fix → Weitermachen

---

## 🖥️ Terminal-Nutzung

**Siehe:** `terminal.instructions.md`

**ERLAUBT:**
- `dotnet build`, `dotnet test`
- Git-Befehle (`git status`, `git diff`)
- `Select-String` für Suchen in Dateien

**VERBOTEN:**
- Dateien erstellen/ändern via Terminal
- XAML-Dateien via Terminal schreiben
- Komplexe Datei-Operationen

---

## ⚠️ Anti-Patterns vermeiden

| ❌ Nicht tun | ✅ Stattdessen |
|-------------|----------------|
| Sofort coden ohne Analyse | Erst verstehen, dann coden |
| Große Änderungen auf einmal | Kleine, inkrementelle Schritte |
| Hardcodierte Farben | ThemeResource verwenden |
| Vergessen zu builden | Nach jeder Datei `run_build` |
| Instructions ignorieren | Immer relevante Instructions prüfen |
| Terminal für Datei-Ops | `create_file`, `replace_string_in_file` |

---

## 📋 Schnell-Checkliste

Vor jeder Implementierung:

- [ ] Anforderung verstanden?
- [ ] Relevante Instructions gelesen?
- [ ] Fluent Design beachtet?
- [ ] Plan erstellt (wenn komplex)?
- [ ] Betroffene Dateien identifiziert?

Nach der Implementierung:

- [ ] Build erfolgreich?
- [ ] Keine Compiler-Warnings?
- [ ] Code folgt Projekt-Patterns?
- [ ] Plan abgeschlossen (finish_plan)?

---

**Letzte Aktualisierung:** 2026-01-23
