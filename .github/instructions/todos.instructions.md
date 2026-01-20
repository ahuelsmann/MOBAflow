---
description: 'MOBAflow offene Aufgaben - nur bei Bedarf laden'
applyTo: '**/todos.md,**/TODO.md'
---

# MOBAflow TODOs

> Bei Fragen zu offenen Aufgaben dieses Dokument konsultieren.

## Offene Architektur-Entscheidungen

1. **IPersistablePage**: Sollten ViewModels statt Pages persistieren?
2. **Fehlende Persistenz**: TrackPlanPage, JourneysPage, TrainsPage, WorkflowsPage

## Skin-System (aktuell)

- Interface: `ISkinProvider`
- Enum: `AppSkin` (System, Blue, Green, Violet, Orange, DarkOrange, Red)
- Colors: `SkinColors.GetPalette(skin, isDark)`
    +-- HeaderBackground/Foreground
    +-- PanelBackground/Border
    +-- Accent Color
```

### Page-Pattern fuer Skin-Support
```csharp
public sealed partial class MyPage : Page
{
    private readonly ISkinProvider _skinProvider;  // Injected

    // In constructor:
    _skinProvider.SkinChanged += (s, e) => DispatcherQueue.TryEnqueue(ApplySkinColors);

    // In Loaded:
    ApplySkinColors();

    // In Unloaded:
    _skinProvider.SkinChanged -= ...;
}
```

---

## 📋 REGELN

1. XAML zuerst schreiben, dann Code-Behind
2. Fluent Design System konsistent anwenden
3. Skin-Support auf allen neuen Pages (ISkinProvider Pattern)
4. Keine hardcodierten Farben (Theme-Resources oder SkinColors)
5. Markennamen vermeiden (Farbnamen verwenden)

