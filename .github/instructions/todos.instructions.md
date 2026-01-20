---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2026-01-24

---

## 🔴 KRITISCH

| Aufgabe | Beschreibung |
|---------|--------------|
| Azure Speech Key | `WinUI/appsettings.json` Zeile 24 - Key in Env-Variable oder User Secrets verschieben |
| Git History | BFG Repo-Cleaner vor GitHub-Release (wegen Speech Key in History) |

---

## 📚 Quality Roadmap (Week 2-6)

| Week | Fokus | Tasks |
|------|-------|-------|
| 2 | Domain | Enums dokumentieren, Tests (Journey, Station, Workflow, Train, Project) |
| 3 | Backend | IoService, UdpClientWrapper, SettingsService - Docs + Tests |
| 4 | SharedUI | ViewModels dokumentieren + Tests |
| 5 | Sound | AudioFilePlayer Docs, SpeakerEngineFactory Tests |
| 6 | CI/CD | Doxygen, Coverage-Report, Azure DevOps Pipeline |

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

