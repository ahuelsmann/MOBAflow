---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24

---

## 🔴 KRITISCH

_Keine kritischen Aufgaben offen._

---

## 🚂 TrackPlan Roadmap

**Instructions:** Siehe `.github/instructions/trackplan-*.instructions.md`

| Phase | Fokus | Status |
|-------|-------|--------|
| 1 | Geometry Tests (Straight, Curve, Switch) | ✅ |
| 2 | SVG Debug Exporter | ✅ |
| 3 | Instructions (geometry, rendering, snapping, topology) | ✅ |
| 4 | Renderer Y-Koordinaten Fix + WL/WR Templates | ✅ |
| 5 | Snap-to-Connect Service | 📋 |
| 6 | Piko A Track Catalog erweitern | 📋 |
| 7 | TrackPlanPage UI verbessern | 📋 |

**Test-Dateien:**
- `Test\TrackPlan.Renderer\StraightGeometryTests.cs` (14 Tests)
- `Test\TrackPlan.Renderer\CurveGeometryTests.cs` (12 Tests)
- `Test\TrackPlan.Renderer\SwitchGeometryTests.cs` (13 Tests)
- `Test\TrackPlan.Renderer\ArcPrimitiveTests.cs` (14 Tests)
- `Test\TrackPlan.Renderer\GeometryValidationTemplate.cs` (inkl. R9 Oval Test)

**Debug-Tool:** `TrackPlan.Renderer\Service\SvgExporter.cs`

### 📝 Session 2025-01-24: Renderer Y-Koordinaten Fix - ERKENNTNISSE

**Problem:** R9 Kurven zeigen "nach innen" statt "nach außen" (konkav statt konvex).

**Root Cause ANALYSE:**  
1. ❌ **Test verwendet falsche Gleise** - WL und R3 sind NICHT in der Stückliste! ✅ BEHOBEN
2. ✅ **SVG Sweep-Flag korrigiert** - `sweep = sweepAngleRad >= 0 ? 1 : 0` (korrekt für Y-flip)
3. ✅ **CurveGeometry.cs validiert** - 24×R9 Test besteht, Geometrie ist korrekt
4. 🔄 **WR Ausrichtung** - WR muss um 180° gedreht werden (Bogen nach oben)

**Stückliste aus Vorlage:**
- 1x WR (55221)
- 1x W3 (55225)
- 1x R1 (55211), 1x R2 (55212)
- 23x R9 (55219)

**Bisherige Fixes:**
1. ✅ **WL/WR Templates hinzugefügt**
2. ✅ **isLeftSwitch Detection** - `EndsWith('L')` statt `Contains('L')`
3. ✅ **Port Labels hinzugefügt** - StartPortLabel/EndPortLabel in LabeledTrack
4. ✅ **Export Überladung** - Export(LabeledTrack[]) für showLabels Parameter
5. ✅ **Test neu geschrieben** - PikoA_R9_Oval_With_WR_W3_R1_R2_CORRECTED
6. ✅ **SVG Sweep-Flag KORRIGIERT** - Von invertiert zu korrekt (1:0)
7. ✅ **WR Rotation auf 180°** - Bogen zeigt jetzt nach oben

**Convention-Analyse:**
- **Piko A Kurven:** ALLE positiven Winkel (R1=30°, R2=30°, R3=30°, R9=15°)  
- **Eisenbahn-Convention:** Positiver Winkel = Linkskurve (aus Sicht des Zugs)
- **CurveGeometry.cs:** Normale zeigt nach links (-Sin, +Cos) = korrekt für Linkskurven
- **CurveGeometryTests:** ✅ ALLE 14 Tests bestehen
- **24×R9 Test:** ✅ BESTEHT - Kreis schließt perfekt bei (0,0)

**User Feedback (visuell aus SVG):**
- ✅ R9-20 bis R9-21 (#24, #25) - KORREKT verbunden und gezeichnet
- ❌ R9-1 bis R9-5 - NICHT Teil des Ovals (falsch platziert)
- ❌ WR Bogen - NICHT korrekt mit R9-Oval verbunden
- 🔄 **Lösung:** WR um 180° drehen, Bogen muss oben sein und mit R9 verbinden

**Test-Ergebnis (nach 180° Rotation):**
```
WR Port C (-235.00, 30.94, 165°) → 23×R9 → Ende (-16.01, -59.77, 510°)
Schließungsfehler: 61.877mm (unverändert)
Winkelfehler: 150.0° (SCHLECHTER - vorher 30°)
```

**Nächster Schritt:**  
- Topologie nochmal überdenken - wo genau startet/endet das Oval?
- Anzahl R9 verifizieren - 23 oder 24 im Oval?
- WR Port C Verbindung korrigieren

**Referenz:** Piko A Gleis Prospekt `docs/99556__A-Gleis_Prospekt_2019.pdf`

---

## 📚 Quality Roadmap (Week 2-6)

✅ **Week 2 abgeschlossen:** Domain Enums dokumentiert + Tests (Journey, Station, Workflow, Train, Project)

✅ **Week 3 abgeschlossen:** IIoService, ISettingsService, UdpWrapper dokumentiert + Tests (NullIoService, SettingsService, UdpClientWrapper)

✅ **Week 4 abgeschlossen:** ViewModels dokumentiert + Tests (WorkflowViewModel, TrainViewModel, StationViewModel)

✅ **Week 5 abgeschlossen:** Sound dokumentiert + Tests (ISpeakerEngine, CognitiveSpeechEngine, NullSpeakerEngine, NullSoundPlayer)

✅ **Week 6 abgeschlossen:** Azure DevOps Pipeline mit Coverage-Report (`pr-validation-with-coverage.yml`)

---

## 📖 Referenz: Skin-System

**Nur für:** `TrainControlPage`, `SignalBoxPage`

```
Interface: ISkinProvider
Enum: AppSkin (System, Blue, Green, Violet, Orange, DarkOrange, Red)
Colors: SkinColors.GetPalette(skin, isDark)
```

### Page-Pattern für Skin-Support
```csharp
public sealed partial class MyPage : Page
{
    private readonly ISkinProvider _skinProvider;  // Injected

    // Constructor: _skinProvider.SkinChanged += (s, e) => DispatcherQueue.TryEnqueue(ApplySkinColors);
    // Loaded: ApplySkinColors();
    // Unloaded: _skinProvider.SkinChanged -= ...;
}
```

---

## 📖 Referenz: SignalBox Element-Typen

**Domain Records** (`Domain/SignalBoxPlan.cs`):
```
SbElement (abstract)
├── SbTrackStraight   → X, Y, Rotation, Name
├── SbTrackCurve      → X, Y, Rotation, Name (90° zentriert)
├── SbSwitch          → + Address, SwitchPosition
├── SbSignal          → + Address, SignalSystem, SignalAspect
└── SbDetector        → + FeedbackAddress
```

**XAML Toolbox Tags:**
- `TrackStraight`, `TrackCurve`, `Switch`, `Signal`, `Detector`

**JSON Serialisierung:** `$type` Discriminator für Polymorphie

---

## 📋 REGELN

1. Datei lesen vor Änderungen
2. Offene Tasks nicht löschen
3. Erledigte Tasks entfernen (nicht markieren)


