# MOBAflow GitHub Launch Readiness

> **Version:** 0.1.0 (Pre-Release)  
> **Target Launch:** ~3-4 Wochen  
> **Status:** 🚧 Feature Completion Phase  
> **Last Updated:** 2026-02-05

---

## 📊 Feature-Status-Übersicht

| Feature | Status | Priority | Blocker? |
|---------|--------|----------|----------|
| TrackPlan Save/Load | ✅ Done | 🔥 HIGH | ❌ Nein |
| TrackPlan Editing UI | 🚧 In Progress | 🔥 HIGH | ✅ Ja |
| Journey Execution | 🚧 Testing | 🔥 HIGH | ✅ Ja |
| Multi-Loco Control | 📝 Planned | 🔥 HIGH | ✅ Ja |
| Unit Tests 70%+ | 🚧 In Progress | 🟡 MEDIUM | ✅ Ja |
| Piko A Library (complete) | 📝 Planned | 🟢 LOW | ❌ Nein |
| MAUI Android Test | 📝 Planned | 🟡 MEDIUM | ❌ Nein |
| Undo/Redo TrackPlan | 📝 Planned | 🟢 LOW | ❌ Nein |

---

## 🎯 Kritische Features (Blocker)

### 1. TrackPlan Save/Load ✅ DONE
**Status:** ✅ 100% komplett  

**Implementierung:**
- ✅ SignalBoxPlan ist Teil des Domain-Models (`Project.SignalBoxPlan`)
- ✅ Automatische Serialisierung mit `System.Text.Json`
- ✅ Polymorphie-Support via `$type` discriminator
- ✅ Alle Track-Elemente werden korrekt gespeichert/geladen
- ✅ JSON-Schema-Validierung (schemaVersion: 1)

**JSON-Beispiel:**
```json
{
  "name": "Example Solution",
  "schemaVersion": 1,
  "projects": [
    {
      "name": "myMOBA",
      "signalBoxPlan": {
        "id": "f1d3dd73-dcd5-4faa-b180-e687659598ea",
        "name": "Stellwerk",
        "gridWidth": 20,
        "gridHeight": 12,
        "cellSize": 60,
        "elements": [
          {
            "$type": "TrackCurve",
            "id": "f364d7d8-8fc3-4217-a84f-31e9805a88b2",
            "x": 2,
            "y": 3,
            "rotation": 270
          }
        ]
      }
    }
  ]
}
```

**Nächste Schritte:**
- [ ] UI für Track-Element-Bearbeitung (move, rotate, delete)
- [ ] Feedback-Point-Zuordnung UI
- [ ] Route-Definition UI

---

### 2. Journey & Workflow Execution
**Status:** 🚧 70% komplett  
**Was fehlt:**
- [ ] End-to-End-Test mit echtem Z21
- [ ] Error-Handling bei Z21-Verbindungsverlust
- [ ] Journey-Reset-Funktion
- [ ] Multi-Journey-Support (parallel)

**Test-Kriterien:**
- ✅ Journey startet bei Station 1
- ✅ Z21 sendet Feedback → Workflow wird ausgeführt
- ✅ Ansage wird abgespielt
- ✅ Journey springt zu Station 2
- ✅ Journey kann zurückgesetzt werden

**Zeitaufwand:** ~5-7 Tage

---

### 3. TrainControlPage (Multi-Loco)
**Status:** 🚧 60% komplett  
**Was fehlt:**
- [ ] Lokauswahl (Dropdown/Liste)
- [ ] Schnelle Lok-Umschaltung
- [ ] Emergency-Stop-Button (alle Loks)
- [ ] Consist/Double-Traction-Unterstützung

**Test-Kriterien:**
- ✅ Benutzer wählt Lok 1 aus
- ✅ Benutzer steuert Lok 1 (Speed/Direction/Functions)
- ✅ Benutzer wechselt zu Lok 2
- ✅ Benutzer steuert Lok 2
- ✅ Emergency-Stop stoppt alle Loks

**Zeitaufwand:** ~2-3 Tage

---

### 4. Unit Tests 70%+
**Status:** 🚧 ~50% aktuell  
**Was fehlt:**
- [ ] TrackPlan Renderer Tests (Geometrie)
- [ ] Workflow Execution Tests
- [ ] Journey State Machine Tests
- [ ] DI Container Tests

**Test-Kriterien:**
- ✅ Code Coverage Report zeigt >70%
- ✅ Alle kritischen Pfade getestet
- ✅ CI/CD Pipeline läuft grün

**Zeitaufwand:** ~4-5 Tage

---

## 🟡 Wichtige Features (Nice-to-Have)

### 5. Piko A-Gleis Library (vollständig)
**Status:** 📝 Geplant  
**Was fehlt:**
- [ ] Alle Artikel-Codes aus Katalog implementieren
- [ ] Weichen (BWL/BWR komplette Serie)
- [ ] Kreuzungen
- [ ] Drehscheiben/Schiebebühnen

**Kann nach GitHub-Launch iterativ erweitert werden.**

**Zeitaufwand:** ~2-3 Wochen (Post-Launch)

---

### 6. MAUI Android App Testing
**Status:** 📝 Geplant  
**Was fehlt:**
- [ ] Test auf echtem Android-Gerät
- [ ] Battery Optimization
- [ ] Network Reconnection
- [ ] Permissions Handling

**Kann mit "Beta"-Tag launched werden.**

**Zeitaufwand:** ~2-3 Tage

---

## 🔧 Bugs & Stability

### Kritische Bugs (vor Launch fixen)
- [x] ✅ **COMException beim Erstellen neuer Projekte** (FIXED 2026-02-05)
- [x] ✅ **JSON-Validierung fehlt** (FIXED 2026-02-05)
- [ ] **Z21 Auto-Reconnect bei Verbindungsverlust**
- [ ] **TrackPlan Undo-Stack Memory Leak**

### Minor Bugs (Post-Launch)
- [ ] Theme-Wechsel erfordert Neustart
- [ ] Einige Icons fehlen (Fallback zu Symbolen)
- [ ] Ladezeiten bei großen TrackPlans

---

## 📈 Test-Coverage-Ziele

| Projekt | Aktuell | Ziel |
|---------|---------|------|
| **Domain** | ~80% | 90% |
| **Backend** | ~60% | 80% |
| **SharedUI** | ~40% | 70% |
| **WinUI** | ~20% | 50% |
| **TrackPlan.Renderer** | ~30% | 70% |
| **Gesamt** | ~50% | 70% |

**Kritische Lücken:**
1. Workflow-Execution-Tests (Backend)
2. Journey-State-Machine-Tests (Backend)
3. ViewModel-Binding-Tests (SharedUI)
4. TrackPlan-Geometry-Tests (TrackPlan.Renderer)

---

## 🚀 Launch-Bereitschaft-Kriterien

### Must-Have (Blocker)
- [ ] TrackPlan Save/Load funktioniert
- [ ] Journey Execution End-to-End getestet
- [ ] Multi-Loco Control funktioniert
- [ ] Unit Tests >70% Coverage
- [ ] Keine kritischen Bugs

### Should-Have (Wichtig)
- [ ] MAUI auf Android getestet
- [ ] Z21 Auto-Reconnect funktioniert
- [ ] Emergency-Stop getestet

### Could-Have (Bonus)
- [ ] Piko A Library komplett
- [ ] Undo/Redo in TrackPlan
- [ ] DCC CV Programming

---

## 📅 Zeitplan (Realistisch)

### Woche 1 (5-9 Feb 2026)
- [ ] TrackPlan Save/Load implementieren
- [ ] Journey Execution testen
- [ ] Bug-Fixes

### Woche 2 (10-16 Feb 2026)
- [ ] Multi-Loco Control implementieren
- [ ] Unit Tests schreiben (TrackPlan, Journey)
- [ ] MAUI testen

### Woche 3 (17-23 Feb 2026)
- [ ] Final Testing
- [ ] Dokumentation aktualisieren
- [ ] Bug-Fixes

### Woche 4 (24-28 Feb 2026)
- [ ] GitHub Repo erstellen
- [ ] CI/CD Pipelines einrichten
- [ ] **🚀 LAUNCH!**

---

## 💡 Was KANN nach Launch nachgeliefert werden?

✅ **Post-Launch Features (v0.2.0):**
- Piko A Library vervollständigen
- Undo/Redo in TrackPlan
- DCC CV Programming
- Weitere Track Libraries (RocoLine, Tillig)
- Performance-Optimierungen
- UI Polish

❌ **Muss VOR Launch fertig sein:**
- TrackPlan Save/Load
- Journey Execution
- Multi-Loco Control
- Kritische Bug-Fixes
- JSON-Validierung ✅
- Thread-Safety ✅

---

## 🎯 Entscheidung: Wann ist GitHub-Launch?

**Option A: Konservativ (4 Wochen)**
- Alle Must-Have Features fertig
- Unit Tests >70%
- MAUI getestet
- Keine bekannten kritischen Bugs

**Option B: Aggressiv (2 Wochen)**
- Nur TrackPlan Save/Load + Journey Execution
- Unit Tests >60%
- MAUI "Beta"-Tag
- Bekannte Bugs dokumentiert

**Empfehlung:** **Option A (4 Wochen)**  
→ Bessere First Impression für Community  
→ Weniger Supportaufwand nach Launch  
→ Höhere Qualität = mehr Contributor

---

## 📝 Zusammenfassung

**Aktueller Status:** 🚧 **~70% GitHub-Ready**

**Kritische Blocker:**
1. 🚧 TrackPlan Save/Load (50% → 100%)
2. 🚧 Journey Execution (70% → 100%)
3. 🚧 Multi-Loco Control (60% → 100%)
4. 🚧 Unit Tests (50% → 70%+)

**Geschätzter Zeitaufwand:** 15-20 Arbeitstage

**Realistisches Launch-Datum:** **Ende Februar 2026** (Option A)

---

*Letzte Aktualisierung: 2026-02-05*
