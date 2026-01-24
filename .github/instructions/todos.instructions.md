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
| 9 | Neuro-UI Design Improvements | 📋 |

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

**Erkenntnisse:**
- Fluent Design System Token-basiert (SystemFillColorCaution/Positive, SystemAccentColor)
- Curve-Aware Snapping bereits im Kern implementiert (keine 180°-Rigidität nötig)
- WinUI 3 ProtectedCursor für Cursor-Kontrolle während Drag
- Theme-Aware Design requires dynamic opacity (visibility in Dark Mode kritisch)
- Design System Switching ist möglich über IDesignSystemProvider Interface (siehe DESIGN-SYSTEMS-AND-EFFECTS.md)

---

### 📋 Nächste Phase (Phase 6-9 - BACKLOG)

**Phase 6: Snap-to-Connect Service (QUEUED)**
- Track-zu-Port Snap-Logik optimieren für weitere Szenarien
- Multi-Port Snap Detection
- Snap-Preview Performance für große Layouts

**Phase 7: Piko A Track Catalog (QUEUED)**
- R9-Oval Topologie finalisieren
- Weitere Weichen-Typen hinzufügen
- Switch Position States (Straight/Diverging) visualisieren

**Phase 8: TrackPlanPage Animation & Effects (QUEUED)**

**WinUI 3 Grafikeffekte - Verfügbar & Empfohlen:**
| Feature | Effekt | Beschreibung |
|---------|--------|-------------|
| Ghost Track | GaussianBlurEffect + Fade Animation | Blur am Canvas während Drag |
| Snap Highlight | DropShadow + Pulse Animation | Glow um Snap-Point mit Scale-out |
| Selected Track | ColorAnimation Glow | Yellow Blink bei Selection |
| Drag Start | DoubleAnimation Opacity | Smooth fade-in des Ghosts |
| Connection Success | Expansion Pulse + Green Flash | Grün pulsierender Ring |
| Grid | ExpressionAnimation Parallax | Subtiler Depth-Effekt |

**Implementierungs-Roadmap:**
- Siehe `.github/analysis/DESIGN-SYSTEMS-AND-EFFECTS.md` für Code-Beispiele
- Phase 8a: Composition Effects für Ghost (GaussianBlur + Opacity Animation)
- Phase 8b: Snap Highlight (DropShadow + ScaleAnimation Pulse)
- Phase 8c: Selected Track (ColorAnimation Glow)

**Phase 9: Neuro-UI Design Improvements (QUEUED - Neuroscience-Based UX)**

**Design Token System Hybrid-Ansatz:**
- Erweitern des existierenden ISkinProvider-Systems mit IDesignSystemProvider Interface
- Fluent Design als Base (Windows-native), Custom Token Layer darüber
- Runtime-Switching für Design Systems (Fluent, Material3, Minimal, etc.)
- Siehe `.github/analysis/DESIGN-SYSTEMS-AND-EFFECTS.md` Kapitel 2 für Details

**Phase 9.1-9.3: Konkrete Neuro-UI Implementierungen**

- **9.1: Attention Control** - Dimme nicht-relevante Tracks während Drag (Kognitive Belastung reduzieren)
  - Nur ausgewählte Tracks: Opacity 1.0
  - Andere Tracks: Opacity 0.3 (Gehirn ignoriert schwache Signale)
  - Methode: DimIrrelevantTracks(selectedTrackIds)
  - Neuro-Effekt: Chunking - fokussierte Aufmerksamkeit
  - Dauer: 40 min

- **9.2: Type Indicators für Switch-Varianten** - Visuelles Pattern Recognition
  - WL/WR/W3/BWL/BWR durch kleine Unicode-Symbole markieren (◀/▶/▼)
  - Farbkodierung: WL=Blau, WR=Rot, W3=Grün, Curved=Orange
  - Größe: 8pt, Opacity 0.5 (subtil aber erkennbar)
  - Position: Top-Left von Switch (Leseverhalten)
  - Neuro-Effekt: Gestalt Law (Ähnlichkeit) - schnelle Mustererkennung
  - Dauer: 30 min

- **9.3: Hover Affordances** - Zeige Interaktivität BEVOR User snappt
  - Ports: Opacity 0.6 (base) → 1.0 + StrokeThickness 2 (hover)
  - Tracks: Hover-State mit Yellow Highlight wenn draggable
  - Ports: Optional Sound Effect (auditory feedback)
  - Gleise: Hervorheben wenn draggbar
  - Neuro-Effekt: Affordances - Gehirn lernt "ich kann hier interagieren"
  - Dauer: 20 min

**Dokumentation:** `.github/analysis/DESIGN-SYSTEMS-AND-EFFECTS.md` + `.github/analysis/NEURO-UI-DESIGN.md`

**Neuro-UI Checkliste für Phase 9 & beyond:**
- [ ] **Attention Control:** Dimme nicht-relevante Elemente während Drag
- [ ] **Visual Hierarchy:** Grid-Größe vs. Track-Größe (was ist wichtiger?)
- [ ] **Type Indicators:** Kleine Symbole für Switch-Typen (WL/WR/W3)
- [ ] **Affordances:** Hover-States auf allen interaktiven Elementen
- [ ] **Predictability:** Ghost-Bewegung muss smooth & linear sein (keine Beschleunigung)
- [ ] **Color Progression:** States durch Farbübergänge zeigen (SnapState: Grau → Orange → Gelb → Grün)
- [ ] **Temporal Feedback:** Alle Animationen < 100ms (Gehirn erwartet instant feedback)
- [ ] **Contrast Ratios:** WCAG AA minimum (auch neurodivergente Benutzer)
- [ ] **Reduce Motion:** Option für Users mit vestibular disorders

---

### 📋 Design System Switching Implementation (Phase 1-3)

**Phase 1 (Nächste Session): IDesignSystemProvider Foundation**
- [ ] Erstelle IDesignSystemProvider Interface
- [ ] Erstelle DesignTokens Record mit Track-spezifischen Farben
- [ ] Implementiere DefaultDesignSystemProvider (Fluent Design Base)
- [ ] Integriere in TrackPlanPage.UpdateTheme()
- [ ] Dokumentation: Pattern für Page-Integration
- ETA: 90 min

**Phase 2 (Session danach): Composition Effects + Settings UI**
- [ ] Composition Effects für Ghost: GaussianBlur + Opacity Animation
- [ ] Snap Highlight: DropShadow + ScaleAnimation (Pulse)
- [ ] Selected Track: ColorAnimation Glow
- [ ] Settings UI für Design System Selector (ComboBox)
- [ ] Runtime Design System Switching testen
- ETA: 120 min

**Phase 3 (Optional): Material Design 3 + Alternative Systems**
- [ ] NuGet: Material.WinUI.3 Integration
- [ ] Erstelle Material3DesignSystemProvider Klasse
- [ ] Erstelle MinimalDesignSystemProvider (Light/Dark/HighViz)
- [ ] Theme-Preview im Settings Dialog
- [ ] A/B Testing für Benutzer-Feedback
- ETA: 150 min (optional)

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


