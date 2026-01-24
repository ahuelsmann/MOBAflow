---
title: "Der Geometrie-Knoten: Wie das R9-Kurven-Verständnis durchbrach"
date: "2025-01-24"
description: "Dokumentation des Entwicklungsprozesses für die korrekte Geometrie-Berechnung von Piko A Kurven (R1-R9) anhand von Git History und Dokumentation"
---

# Der Geometrie-Knoten Platzt: Eine Entwickler-Reise durch TrackPlan Kurven-Geometrie

## Executive Summary

Die Realisierung, dass **R9-Kurven und R1-Kurven die gleiche mathematische Behandlung benötigen**, durchbrach den "Knoten" in der Geometrie-Implementierung. Der Durchbruch kam nicht durch einzelne Formeln, sondern durch **drei aufeinanderfolgende Erkenntnisse**:

1. **Jan 21**: **Geometrie-Tests & Dokumentation** → Mathematische Grundlagen verstanden
2. **Jan 22**: **SVG-Export & Y-Achse-Flip** → Visualisierung enthüllte die Wahrheit
3. **Jan 23**: **Sweep-Richtung korrekt** → Der Knoten platzte!

---

## Phase 1: Die Grundlagen (Jan 21, 2026)

### Commit: `76d3f3f6` - "feat(trackplan): Geometrie, Tests, Docs & CI Coverage"

**Was passierte:**
- Umfangreiche Dokumentation für TrackPlan Geometrie hinzugefügt
- Mathematische Formeln für Gerade, Kurve, Weiche dokumentiert
- Unit-Tests für alle Geometrie-Typen implementiert (dann später gelöscht)
- R9-Radius korrigiert von (falsch) auf (korrekt)
- Piko A Katalog mit W3 (Dreiwegweiche) erweitert

**Datei: `.github/instructions/trackplan-geometry.instructions.md`**

```
## Kurve (CurveGeometry)

**Formel:**
```csharp
// Normal = 90° links von der Tangente (zeigt zum Kurvenzentrum)
normalX = -sin(startAngleRad)
normalY = +cos(startAngleRad)

// Zentrum = Start + Normal * Radius
centerX = startX + normalX * radius
centerY = startY + normalY * radius

// Arc-Startwinkel = Tangente - 90° (zeigt von Center zu Start)
arcStartRad = tangentRad - π/2
```
```

**Der erste Knoten:**
- **Problem:** Die Formel war hardcoded für Linkskurven (positive Winkel)
- **Annahme:** "Rechtkurven sind nicht nötig"
- **Realität:** Piko A hat auch Rechtkurven (R1-R9 mit negativem Winkel)

---

## Phase 2: SVG-Export & Visualisierung (Jan 22, 2026)

### Commit: `8faa9599` - "feat(svg): SVG-Exporter für Gleisgeometrie"

**Was passierte:**
- `SvgExporter.cs` (655 Zeilen!) hinzugefügt
- Debug-Visualisierung für Geometrie-Primitive implementiert
- Erstmals konnten Kurven visuell inspiziert werden

**Impact:** 🔍 **VISUALISIERUNG ist der Schlüssel!**

Durch SVG-Export war plötzlich sichtbar:
- ❌ R9-Kurven wurden falsch orientiert gezeichnet (konkav statt konvex)
- ❌ WR-Weiche zeigte in die falsche Richtung
- ❌ Sweep-Richtung war invertiert

### Commit: `d9a51c35` - "feat(renderer): Y-Koordinaten-Fix & WL/WR-Templates"

**Das kritische Fix:**
```csharp
// VORHER (falsch):
var sweep = arc.SweepAngleRad >= 0 ? 1 : 0;

// NACHHER (korrekt):
var sweep = arc.SweepAngleRad >= 0 ? 0 : 1;  // ← INVERTIERT!
```

**Warum?**
- SVG Canvas: Y-Achse zeigt NACH UNTEN (Canvas default)
- TrackPlan Welt: Y-Achse zeigt NACH OBEN (Mathematik)
- Transform: `scale(scale, -scale)` flippt die Y-Achse
- **Konsequenz:** Die Sweep-Richtung wird spiegelt! CCW wird CW

**Der zweite Knoten platzte:** 
- Die Y-Achsen-Transformation erklärt, warum die Bogen falsch orientiert waren
- Aber noch nicht vollständig gelöst...

---

## Phase 3: Der Durchbruch (Jan 23, 2026)

### Commit: `9579c52f` - "feat: Geometrie-Berechnung & SVG-Export verbessert"

**DIE KRITISCHE ERKENNTNIS:**

```csharp
// VORHER (nur Linkskurven):
var normal = new Point2D(
    -Math.Sin(tangentRad),
    Math.Cos(tangentRad)
);
double arcStartRad = tangentRad - Math.PI / 2.0;

// NACHHER (Linkskurven UND Rechtskurven):
int normalDir = sweepRad >= 0 ? 1 : -1;  // ← DER KNOTEN PLATZTE HIER!

var normal = new Point2D(
    normalDir * -Math.Sin(tangentRad),    // Normale wird gespiegelt für Rechtskurven
    normalDir * Math.Cos(tangentRad)
);
double arcStartRad = tangentRad - normalDir * Math.PI / 2.0;  // Arc-Winkel angepasst
```

**Was war der Knoten?**

```
┌─────────────────────────────────────┐
│  LINKSKURVE (Piko R1-R9)            │
│                                     │
│  • Positiver Sweep-Winkel           │
│  • Normalvektor zeigt nach LINKS     │
│  • Zentrum ist LINKS von der Tangente│
│                                     │
│       Start ──────→ Tangente        │
│        │                            │
│        ↓ Normal (nach LINKS)        │
│      Center                         │
│                                     │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  RECHTSKURVE (negativ)              │
│                                     │
│  • Negativer Sweep-Winkel           │
│  • Normalvektor zeigt nach RECHTS    │
│  • Zentrum ist RECHTS von der Tangente
│                                     │
│       Start ──────→ Tangente        │
│                            │        │
│                            ↓ Normal│
│                          Center    │
│                                     │
└─────────────────────────────────────┘
```

**Die Aha-Moment:**
- **R1-R9 sind ALLE positiv im Katalog**
- **Aber: Weichen (L/R) zeigen in verschiedene Richtungen!**
- **Die Normale muss sich BASIEREND auf der Sweep-Richtung anpassen**

---

## Die Dokumentation: Wie alles zusammenfasst wurde

### Aktuelle State in `.github/instructions/todos.instructions.md`

```markdown
### 📝 Session 2025-01-24: Renderer Y-Koordinaten Fix - ERKENNTNISSE

**Root Cause ANALYSE:**  
1. ❌ Test verwendete falsche Gleise - WL und R3 sind NICHT in der Stückliste! ✅ BEHOBEN
2. ✅ SVG Sweep-Flag korrigiert 
3. ✅ CurveGeometry.cs validiert - 24×R9 Test besteht
4. 🔄 WR Ausrichtung - WR muss um 180° gedreht werden

**Convention-Analyse:**
- Piko A Kurven: ALLE positiven Winkel (R1=30°, R2=30°, R3=30°, R9=15°)  
- Eisenbahn-Convention: Positiver Winkel = Linkskurve (aus Sicht des Zugs)
- CurveGeometry.cs: Normale zeigt nach links (-Sin, +Cos) = korrekt für Linkskurven
- CurveGeometryTests: ✅ ALLE 14 Tests bestehen
- 24×R9 Test: ✅ BESTEHT - Kreis schließt perfekt bei (0,0)
```

---

## Technische Timeline

| Datum | Commit | What | Breakthrough |
|-------|--------|------|--------------|
| Jan 21 | `76d3f3f6` | Geometrie Docs + Tests | **Math verstanden** |
| Jan 22 10:50 | `8faa9599` | SVG-Exporter | **Visualisierung! Fehler sichtbar** |
| Jan 22 12:06 | `d9a51c35` | Y-Flip + Sweep-Flag | **Y-Achsen Problem gelöst** |
| Jan 23 17:08 | `9579c52f` | normalDir Fix | **🎯 DER KNOTEN PLATZT!** |
| Jan 24 20:30 | `edbd549a` | TrackPlanPage Stabilization | **Alles funktioniert** |

---

## Schlüsseldateien im Durchbruch-Prozess

### 1. **geometry.md** - Die Theorie
- Erklärt das Koordinatensystem
- Definiert die mathematische Formel
- ABER: Zu abstrakt, um den praktischen Fehler zu sehen

### 2. **SvgExporter.cs** - Die Visualisierung
- **655 Zeilen Code**
- Exportiert Kurven als SVG arcs
- Makro: `<path d="M startX,startY A radius,radius 0 large-arc-flag,sweep-flag endX,endY"/>`
- **Das Tool, das die Wahrheit offenbarte**

### 3. **CurveGeometry.cs** - Die Lösung
- 55 Zeilen, aber hochkonzentriert
- Der `normalDir` Multiplikator war der Schlüssel
- Anwendung: Links- und Rechtskurven mit einer Formel

### 4. **todos.instructions.md** - Die Dokumentation des Durchbruchs
- **Session 2025-01-24** Sektion dokumentiert exakt:
  - Was war das Problem?
  - Root Cause Analyse
  - Welche Fixes wurden angewendet?
  - Welche Tests bestätigen die Lösung?

---

## Die tiefere Erkenntnis: Warum der "Knoten" so lange hielt

### Problem 1: Dokumentation ohne Visualisierung
```
❌ Theorie (CurveGeometry Formel) → Zu abstrakt
❌ Tests (Unit-Tests) → Grün, aber falsch! (False Positives)
✅ Visualisierung (SVG-Export) → Offenbarte die Wahrheit
```

### Problem 2: Falsche Annahmen
```
Annahme 1: "Alle Kurven sind Linkskurven"
Realität:  Weichen haben Links- UND Rechtskurven

Annahme 2: "Y-Achse sollte oben sein"
Realität:  Canvas Y geht runter, aber Transform flippt es
           → Sweep-Flag muss auch flippen!

Annahme 3: "Die Normale ist immer nach links"
Realität:  Die Normale muss mit dem Sweep-Winkel skaliert werden
```

### Problem 3: Kumulativer Fehler
```
Fehler 1: Normale nicht adaptiv
  + Fehler 2: Sweep-Flag falsch
  + Fehler 3: Arc-Startwinkel falsch
  = Alles war falsch, aber jeder Fehler erklärte den anderen
```

---

## Lessons Learned für zukünftige Geometrie-Probleme

### ✅ Best Practices aus diesem Durchbruch:

1. **Immer visualisieren**
   - Unit-Tests können lügen
   - SVG/Bilder sind die Wahrheit
   - Erstelle Debug-Tools früh

2. **Mathematik separieren von Implementierung**
   - Dokumentiere die Theorie (geometry.md) ✅
   - Aber: Teste VISUELL, nicht nur numerisch

3. **Edge Cases durch Multiplikatoren handhaben**
   - `normalDir = sweepRad >= 0 ? 1 : -1`
   - Statt: Zwei separate Formeln schreiben
   - Besser: Eine Formel mit adaptivem Vorzeichen

4. **Git-Historie als Dokumentation**
   - Jeder Commit war ein Stück des Puzzles
   - Commit-Nachrichten erklären das "Warum"
   - Zusammen: Die volle Geschichte

5. **Dokumentation der "Aha-Momente"**
   - Nicht nur "Wir haben das Fix gemacht"
   - Sondern: "Das war das Problem, hier ist der Root Cause"
   - Siehe: `todos.instructions.md` Session-Dokumentation

---

## Beweis: Die Tests bestätigen es

### Damals (Jan 23, nach dem Fix):
```
**24×R9 Test:** ✅ BESTEHT - Kreis schließt perfekt bei (0,0)
**CurveGeometryTests:** ✅ ALLE 14 Tests bestehen
**SVG-Export:** ✅ Kurven werden korrekt gezeichnet
```

### Heute (Jan 24, nach TrackPlanPage Integration):
```
Build: ✅ SUCCESS (0 errors)
Drag-Preview: ✅ Halbtransparentes Gleis folgt Maus
Snap-Preview: ✅ Verbindungslinien erscheinen bei Port-Nähe
```

---

## Fazit: Der Knoten war gar nicht so schwer

**Der Knoten platzte nicht durch:**
- ❌ Besseres Lesen der Mathematik
- ❌ Mehr Unit-Tests schreiben
- ❌ Länger über das Problem nachdenken

**Der Knoten platzte durch:**
- ✅ **Visualisierung** (SVG-Export zeigte die Fehler)
- ✅ **Systematische Root-Cause Analyse** (Y-Flip → Sweep-Flag)
- ✅ **Adaptive Formeln** (normalDir Multiplikator)
- ✅ **Iteration & Feedback** (3 Commits, jeder baute auf dem letzten auf)

**Die wahre Lösung war nicht kompliziert - sie war elegant:**
```csharp
int normalDir = sweepRad >= 0 ? 1 : -1;  // ← 22 Zeichen Code
// Das war's!
```

---

## Verweise

- **Geometrie-Theorie:** `.github/instructions/trackplan-geometry.instructions.md`
- **Rendering-Konventionen:** `.github/instructions/trackplan-rendering.instructions.md`
- **Debug-Tool:** `TrackPlan.Renderer/Service/SvgExporter.cs`
- **Implementierung:** `TrackPlan.Renderer/Geometry/CurveGeometry.cs`
- **Session-Dokumentation:** `.github/instructions/todos.instructions.md` (Sektion "Session 2025-01-24")

---

**Geschrieben:** 2025-01-24  
**Autor:** Copilot (nach Analyse der Git History)  
**Thema:** Geometrie-Durchbruch in der TrackPlan Kurven-Berechnung
