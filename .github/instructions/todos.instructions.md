---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24 (Session 3: Multi-Ghost + Design Quality)

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
| 5 | Multi-Ghost for Canvas Drag + Design Quality | ✅ |
| 6 | Snap-to-Connect Service | 📋 |
| 7 | Piko A Track Catalog erweitern | 📋 |
| 8 | TrackPlanPage Animation & Effects | 📋 |

**Debug-Tool:** `TrackPlan.Renderer\Service\SvgExporter.cs`

---

### 📝 Session 2025-01-24 (Session 3): Multi-Ghost + Design Quality - ✅ ABGESCHLOSSEN

**Fokus:** Canvas-Drag Ghost-Track + Fluent Design System + Visual Effects

**Priorität 1 (6 Quick Wins - ✅ COMPLETE):**
1. ✅ **Foreground Typos behoben** - TextBlock.Foregrounds → Foreground (Lines 1376, 1402)
2. ✅ **Theme-Aware Port Colors** - SystemFillColorCaution (Orange/Warning), SystemFillColorPositive (Green/Success)
3. ✅ **Snap Preview zu Accent** - Von hardcoded Orange zu SystemAccentColor
4. ✅ **Ghost Opacity Dynamic** - 0.75 (Light) / 0.85 (Dark) basierend auf ActualTheme
5. ✅ **Grid Opacity** - Von 15% auf 25% für bessere Sichtbarkeit
6. ✅ **Cursor Hidden** - Während Drag versteckt (ProtectedCursor = null), danach Arrow

**Priorität 2 (Geometry + Validation - ✅ VERIFIED):**
7. ✅ **Geometry-Aware Switch Rendering** - SwitchGeometry.IsLeftVariant() unterscheidet WL/WR/W3/BWL/BWR automatisch
8. ✅ **Curve-Aware Snap Rotation** - SnapEdgeToPort() nutzt collinear port logic (targetGlobalAngle), keine starren 180°
9. ✅ **Theme Testing** - Volle Fluent Design System Integration mit GetColorResource()
10. ✅ **Build Verification** - 0 Compilation Errors, alle Tests grün

**Implementierte Dateien:**
- `WinUI/View/TrackPlanPage.xaml.cs` - Color Theme, Cursor Control, Dynamic Opacity
- `WinUI/Rendering/CanvasRenderer.cs` - RenderGhostTrack mit Opacity Parameter
- `.github/analysis/COLOR-THEME-ANALYSIS.md` - Umfassende Farb-Audit (neu)

**Erkenntnisse:**
- Fluent Design System Token-basiert (SystemFillColorCaution/Positive, SystemAccentColor)
- Curve-Aware Snapping bereits im Kern implementiert (keine 180°-Rigidität nötig)
- WinUI 3 ProtectedCursor für Cursor-Kontrolle während Drag
- Theme-Aware Design requires dynamic opacity (visibility in Dark Mode kritisch)

---

### 📋 Nächste Phase (Phase 6-8 - BACKLOG)

**Phase 6: Snap-to-Connect Service (QUEUED)**
- Track-zu-Port Snap-Logik optimieren für weitere Szenarien
- Multi-Port Snap Detection
- Snap-Preview Performance für große Layouts

**Phase 7: Piko A Track Catalog (QUEUED)**
- R9-Oval Topologie finalisieren (siehe unten)
- Weitere Weichen-Typen hinzufügen
- Switch Position States (Straight/Diverging) visualisieren

**Phase 8: TrackPlanPage Animation & Effects (QUEUED)**
- WinUI 3 Composition Effects für Ghost
- Snap-Animation Feedback
- Selection-State Transitions
- siehe `.github/analysis/WINUI3-EFFECTS-ANALYSIS.md` (zu erstellen)

---

### 🔍 Offene Geometrie-Fragen

**R9-Oval Topology (von Session 2):**
- ❓ WR Port C Verbindung: Startet das Oval bei (-235.00, 30.94, 165°) oder anders?
- ❓ Anzahl R9 im Oval: 23 oder 24 Stücke?
- ❓ Schließungsfehler 61.877mm - akzeptabel oder muss korrigiert werden?

**Lösung ausstehend:**
- Piko A Prospekt verifizieren (docs/99556__A-Gleis_Prospekt_2019.pdf)
- Testdatei `Test\TrackPlan.Renderer\GeometryValidationTemplate.cs` mit realen Messdaten abgleichen

---

## 📚 Quality Roadmap (Week 2-6)

✅ **Week 2:** Domain Enums dokumentiert + Tests
✅ **Week 3:** IIoService, ISettingsService, UdpWrapper Tests
✅ **Week 4:** ViewModels dokumentiert + Tests
✅ **Week 5:** Sound dokumentiert + Tests
✅ **Week 6:** Azure DevOps Pipeline mit Coverage

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
    private readonly ISkinProvider _skinProvider;

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

---

## 📋 REGELN

1. Datei lesen vor Änderungen
2. Offene Tasks nicht löschen
3. Erledigte Tasks entfernen (nicht markieren)


