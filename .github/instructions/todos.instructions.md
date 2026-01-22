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

**Root Cause ERKANNT:** 
1. ❌ **Test verwendet falsche Gleise** - WL und R3 sind NICHT in der Stückliste!
2. ✅ **SVG Sweep-Flag Fix** - `sweep = sweepAngleRad >= 0 ? 0 : 1` (invertiert für Y-down)

**Stückliste aus Vorlage:**
- 1x WR (55221)
- 1x W3 (55225)
- 1x R1 (55211), 1x R2 (55212)
- 23x R9 (55219)
- **KEIN R3, KEINE WL!**

**Bisherige Fixes:**
1. ✅ **WL/WR Templates hinzugefügt** - Katalog vervollständigt
2. ✅ **isLeftSwitch Detection** - `EndsWith('L')` statt `Contains('L')`
3. ✅ **SVG Sweep-Flag invertiert** - Für scale(scale, -scale) Y-Flip
4. 🔄 **Test muss neu geschrieben werden** - Topologie muss aus Foto abgeleitet werden

**Nächster Schritt:** 
- Test mit korrekter Gleisliste schreiben: WR → W3 → (R1+R2 auf einem Port) → (23x R9 auf anderem Port)
- Topologie aus Foto validieren

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


