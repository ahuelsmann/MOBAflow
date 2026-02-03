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
| Terminal für Datei-Ops | `create_file`, `edit_file` |
| **Edit ohne vollen Kontext** | **Bei kritischem Code: VOLLEN Kontext lesen (100+ Zeilen)** |
| **Backend/Z21.cs blind editieren** | **Siehe z21-backend.instructions.md - Paket-Parsing ist kritisch!** |

---

## 🔧 Spezielle Regeln für kritische Dateien

### **Backend/Z21.cs:**
- ✅ **IMMER** `OnUdpReceived` komplett lesen vor Edit
- ✅ Jeder Paket-Typ = eigener `if`-Block mit `return`
- ✅ Nach Edit: **Sofort Connection testen** (nicht nur Build!)
- ✅ Siehe: `.github/instructions/z21-backend.instructions.md`

### **ViewModels mit berechneten Properties:**
- ✅ **Alle Abhängigkeiten** mit `[NotifyPropertyChangedFor]` deklarieren
- ✅ Beispiel: `SpeedKmh` hängt von `Speed`, `MaxSpeedStep`, `SelectedVmax` ab
- ✅ JEDE Abhängigkeit braucht Notification!

---

**Letzte Aktualisierung:** 2026-01-23
