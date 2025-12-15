# MOBAflow - Continue Session: Track Visualization

**Erstellt:** 2025-01-XX (Büro)  
**Für:** Zuhause-Session mit AnyRail SVG Export

---

## 📋 Status: Grundgerüst fertig ✅

### Neue Pages erstellt:

| Page | Datei | Zweck |
|------|-------|-------|
| **Track Plan** | `WinUI/View/TrackPlanPage.xaml` | Physischer Gleisplan (Option A) |
| **Journey Map** | `WinUI/View/JourneyMapPage.xaml` | Virtuelle Streckenansicht (Option B) |

### ViewModels:

| ViewModel | Datei |
|-----------|-------|
| `TrackPlanViewModel` | `SharedUI/ViewModel/TrackPlanViewModel.cs` |
| `JourneyMapViewModel` | `SharedUI/ViewModel/JourneyMapViewModel.cs` |

### Navigation hinzugefügt:
- `MainWindow.xaml` - Menüeinträge für beide Pages
- `MainWindow.xaml.cs` - Navigation Handler
- `App.xaml.cs` - DI Registrierung

---

## 🏠 Was zuhause zu tun ist:

### 1. AnyRail SVG Export bereitstellen

Exportiere den Gleisplan aus AnyRail als SVG:
- **Datei → Exportieren → SVG**
- Speichere die Datei im Projekt oder teile den Inhalt

### 2. SVG Struktur analysieren

Copilot soll die SVG-Datei analysieren:
```
Zeig mir die ersten 100 Zeilen der AnyRail SVG-Datei
```

Wichtige Fragen:
- Wie sind Gleise strukturiert? (`<path>`, `<line>`, `<polyline>`?)
- Gibt es Gruppen/Layer für Weichen?
- Sind Beschriftungen/IDs vorhanden?

### 3. Sensor-Positionen definieren

Deine 3 Rückmelder (InPort 1, 2, 3):
- Wo sind sie physisch auf der Anlage?
- Wie sollen sie im SVG markiert werden?

**Option A:** Manuell X/Y Koordinaten eingeben
**Option B:** Interaktiv per Klick auf den Gleisplan setzen

### 4. SVG Rendering implementieren

Mögliche Ansätze:
- **WebView2** - SVG direkt anzeigen
- **Win2D** - SVG zu Canvas rendern
- **Svg.Skia** - NuGet Package für SVG Parsing

### 5. Live-Updates verbinden

`JourneyManager.StationChanged` Event → UI Update:
- Aktive Station highlighten
- Train-Icon bewegen (falls Position bekannt)
- Sensor-Marker aktivieren bei Feedback

---

## 📁 Benötigte Dateien von dir:

1. **AnyRail SVG Export** - `gleisplan.svg`
2. **Optional:** DXF Export falls SVG nicht ausreicht
3. **Info:** Welcher Sensor ist wo auf dem Gleisplan?

---

## 🔧 Bereits implementierte Features:

### TrackPlanPage:
- [ ] SVG Container (Placeholder)
- [x] Sensor Markers Overlay (ItemsControl mit Canvas)
- [x] Train Position Indicator (Placeholder)
- [x] Status Bar (Station, Lap, Journey)
- [x] Import Button (Command vorbereitet)
- [x] SensorMarker Model (InPort, X, Y, IsActive)

### JourneyMapPage:
- [x] Journey Selector (ComboBox)
- [x] Horizontale Stations-Route
- [x] Current Station Indicator
- [x] Progress/Counter Status Bar
- [ ] Converters für Styling (BoolToFontWeight, etc.)

---

## ⚠️ Bekannte TODOs:

### Converters fehlen in XAML:
Die folgenden Converter werden in den Templates referenziert, müssen aber noch erstellt werden:
- `BoolToColorConverter`
- `BoolToVisibilityConverterInverted`
- `BoolToFontWeightConverter`
- `BoolToAccentBrushConverter`
- `TrackNumberConverter`
- `ExitSideConverter`

### TrackPlanViewModel:
```csharp
// TODO in ImportTrackPlanAsync:
// - File Picker öffnen
// - SVG laden und parsen
// - Gleise rendern
```

---

## 💡 Prompt für Copilot zuhause:

```
Ich habe die AnyRail SVG Datei. Hier ist der Inhalt:
[SVG-Inhalt einfügen oder Datei angeben]

Analysiere die Struktur und implementiere:
1. SVG Rendering in TrackPlanPage
2. Sensor-Marker Positionierung
3. Die fehlenden Converter
```

---

## 🎯 Ziel der Visualisierung:

```
┌─────────────────────────────────────────────────────────────────┐
│                      Track Plan (AnyRail SVG)                   │
│                                                                 │
│         ╔═══════════════════════════════════════╗               │
│         ║                                       ║               │
│         ║    ┌─────────────────────┐            ║               │
│         ║    │   BAHNHOF           │ ← [1]      ║  ← InPort 1   │
│    ═════╬════│   🚂 RE 78          │════════════╬═══            │
│         ║    └─────────────────────┘   [2]      ║  ← InPort 2   │
│         ║                                       ║               │
│         ╚═══════════════[3]═════════════════════╝  ← InPort 3   │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Station: Herford | Lap: 2/6 | Journey: RE 78 (Porta-Express)    │
└─────────────────────────────────────────────────────────────────┘
```

Die Sensor-Marker [1], [2], [3] werden beim Feedback-Event aktiviert (Farbe wechselt).
