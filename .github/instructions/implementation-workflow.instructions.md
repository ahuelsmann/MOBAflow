---
description: 'Workflow fuer strukturierte Implementierung'
applyTo: '**/*.cs,**/*.xaml'
---

# Implementation Workflow

## 5-Schritte-Workflow

1. **ANALYSE**: Anforderung verstehen, betroffene Dateien identifizieren
2. **PATTERNS**: Bestehende Patterns im Code suchen und wiederverwenden
3. **PLAN**: Bei >2 Dateien `plan()` Tool verwenden
4. **IMPLEMENTIEREN**: Backend → ViewModel → View, nach jeder Datei Build
5. **VALIDIEREN**: `run_build`, Tests

## Checkliste vor Implementierung

- [ ] Gibt es bereits aehnliche Implementierung im Projekt?
- [ ] Welche Instructions sind relevant? (architecture, mvvm, winui, fluent-design)
- [ ] Bei UI: ThemeResource, 8px Grid, Fluent Design

## Bei Unsicherheit

Microsoft-Dokumentation ZUERST via `azure_documentation` Tool konsultieren.
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
