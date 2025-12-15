# MOBAflow - Track Visualization

**Aktualisiert:** 2025-01-15  
**Gleissystem:** Piko A-Gleis (H0)  
**Layout:** Hundeknochen Mittelstadt (266cm x 110cm)

---

## 📋 Status: In Arbeit 🔧

### ✅ Erledigt

- [x] **TrackPlanPage** mit kombinierter Ansicht (Track Plan + Journey Map)
- [x] **Segment Details Panel** - immer sichtbar (wie Properties Panel)
- [x] **InPort-Zuweisung** - NumberBox + Assign/Clear Buttons
- [x] **Journey Map** unten integriert (Station-Route-Visualisierung)
- [x] **Exakte Gleis-Sequenzen** aus AnyRail-Screenshot übernommen
- [x] **Piko A-Gleis Spezifikationen** dokumentiert

### ❌ Offen (TODO für morgen)

- [ ] **Kurven werden nicht angezeigt** - SVG Arc Geometrie prüfen!
- [ ] **JourneyMapPage entfernen** (obsolet, in TrackPlanPage integriert)
- [ ] **Live-Sensor-Highlighting** bei Z21 Feedback Events

---

## 🐛 Aktuelles Problem: Kurven fehlen!

Im Screenshot sind nur die **geraden Gleise** sichtbar, die **Kurven (R1, R2, R3)** fehlen.

### Vermutete Ursache:
Die SVG Arc Path-Berechnung (`AddSemicircle`) erzeugt möglicherweise ungültige Koordinaten oder die Bögen sind außerhalb des sichtbaren Canvas-Bereichs.

### Debug-Schritte für morgen:
1. Koordinaten in `AddSemicircle()` loggen
2. Prüfen ob Kurven innerhalb Canvas-Grenzen liegen (0-1000, 0-420)
3. SVG Arc Syntax validieren: `M x1,y1 A rx,ry 0 0 sweep x2,y2`

---

## 🎯 Piko A-Gleis Spezifikationen (aus PDF)

### Kurven (30° pro Stück, 6 = 180°)

| Artikel | Bezeichnung | Radius | Abstand |
|---------|-------------|--------|---------|
| 55211 | R1 | 360.0 mm | - |
| 55212 | R2 | 421.9 mm | 61.9 mm |
| 55213 | R3 | 483.8 mm | 61.9 mm |
| 55214 | R4 | 545.6 mm | 61.9 mm |

### Geraden

| Artikel | Bezeichnung | Länge |
|---------|-------------|-------|
| 55200 | G239 | 239 mm |
| 55201 | G231 | 231 mm |
| 55202 | G62 | 62 mm |
| 55203 | G119 | 119 mm |

### Weichen

| Artikel | Bezeichnung | Länge | Winkel |
|---------|-------------|-------|--------|
| 55220 | WL (Links) | 239 mm | 15° |
| 55221 | WR (Rechts) | 239 mm | 15° |
| 55224 | W3 (3-Wege) | 239 mm | 15° |
| 55226 | DKW | 239 mm | 15° |

---

## 🗺️ Exakte Gleis-Sequenzen (aus AnyRail-Screenshot)

### Obere Station (4 Gleise)

| Gleis | Links | Gerade Strecke | Rechts |
|-------|-------|----------------|--------|
| **1** | R3 | WR - G231 - G231 - G231 - G239 - G231 - WL | R3 |
| **2** | R2 | G231 - W3 - WR - G231 - G231 - W3 - G231 | R2 |
| **3** | R1 | WL - G231 - G231 - DKW - G231 - G231 - WR | R1 |
| **4** | - | G62 - G231 - G231 - G62 - G119 - G239 - WR - G119 - G62 - G231 - G62 | - |

### Untere Station (3 Gleise)

| Gleis | Links | Gerade Strecke | Rechts |
|-------|-------|----------------|--------|
| **5** | R1 | G231 - G239 - G231 - G239 - G231 - G239 - G231 | R1 |
| **6** | R2 | G231 - G239 - G231 - G239 - G231 - G239 - G231 | R2 |
| **7** | R3 | G231 - G239 - G231 - G239 - G231 - G239 - G231 | R3 |

### G62 Extensions
- Links und rechts am äußersten Ring (Gleis 1 oben, Gleis 7 unten)

---

## 🏗️ Architektur

### Dateien

| Datei | Status | Zweck |
|-------|--------|-------|
| `Domain/TrackPlan/TrackLayout.cs` | ✅ | Factory für Hundeknochen-Layout |
| `Domain/TrackPlan/TrackSegment.cs` | ✅ | Einzelnes Gleissegment |
| `Domain/TrackPlan/TrackSegmentType.cs` | ✅ | Enum: Straight, Curve, Switch, etc. |
| `SharedUI/ViewModel/TrackPlanViewModel.cs` | ✅ | ViewModel mit Journey-Integration |
| `SharedUI/ViewModel/TrackSegmentViewModel.cs` | ✅ | ViewModel für einzelnes Segment |
| `WinUI/View/TrackPlanPage.xaml` | ✅ | UI mit Canvas + Journey Map |
| `WinUI/View/JourneyMapPage.xaml` | ❌ TODO: Entfernen | Obsolet |

### UI-Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  🚂 Track Plan                                    [Reload]      │
│  Piko A-Gleis (H0)                                              │
├─────────────────────────────────────────────────────────────────┤
│                                                 │ Segment       │
│  ╭─────────────────────────────────────────╮    │ Details       │
│  │         TRACK PLAN (Canvas)             │    ├───────────────┤
│  │  [Kurven fehlen noch!]                  │    │ Name: ...     │
│  │  ════════════════════════════════════   │    │ Code: G231    │
│  │  ════════════════════════════════════   │    │ Layer: ...    │
│  │  ════════════════════════════════════   │    ├───────────────┤
│  │  ════════════════════════════════════   │    │ InPort: [___] │
│  │                                         │    │ [Assign][Clear│
│  │  ════════════════════════════════════   │    │               │
│  │  ════════════════════════════════════   │    │               │
│  │  ════════════════════════════════════   │    │               │
│  ╰─────────────────────────────────────────╯    │               │
├─────────────────────────────────────────────────────────────────┤
│  🚂 Journey: [Dropdown]                                         │
│  ●────────●────────●────────●────────●                          │
│  Station1  Station2  Station3  Station4                         │
├─────────────────────────────────────────────────────────────────┤
│  Station: ... | Lap: 2/6 | Journey: RE78 | 0 sensors assigned  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 Nächste Schritte (Morgen)

1. **Kurven-Bug fixen**
   - `AddSemicircle()` debuggen
   - Koordinaten validieren
   - SVG Arc Syntax prüfen

2. **JourneyMapPage.xaml entfernen**
   - Aus Navigation entfernen
   - Datei löschen

3. **Live-Integration testen**
   - Z21 Feedback → Segment-Highlighting
   - Journey Station → Journey Map aktualisieren

---

## 💡 Hinweise

- **Canvas-Größe:** 1000 x 420 px
- **Kurven-Zentren:** Links X=140, Rechts X=860
- **Center Y:** 210 (Mitte des Canvas)
- **Kurvenradien:** R1=65px, R2=88px, R3=110px (skaliert)

Gute Nacht! 🌙
│         ╚═══════════════[3]═════════════════════╝  ← InPort 3   │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Station: Herford | Lap: 2/6 | Journey: RE 78 (Porta-Express)    │
└─────────────────────────────────────────────────────────────────┘
```

Die Sensor-Marker [1], [2], [3] werden beim Feedback-Event aktiviert (Farbe wechselt).
