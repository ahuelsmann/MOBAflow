---
description: 'Minimale TODO-Liste fuer MOBAflow'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2026-01-24

---

## 🚨 NAECHSTE AUFGABEN

### 1. Skin-System Refactoring (Naming Convention)

> **Status:** OFFEN
> **Prioritaet:** MITTEL

**Aktuelle Situation:**
- Interface heisst `IThemeProvider` (verwirrend mit WinUI ElementTheme)
- Skin-Namen verwenden Markennamen (EsuCabControl, RocoZ21, MaerklinCS)

**Gewuenschte Aenderungen:**

| Aktuell | Neu |
|---------|-----|
| `IThemeProvider` | `ISkinProvider` |
| `ThemeChanged` Event | `SkinChanged` Event |
| `CurrentTheme` Property | `CurrentSkin` Property |
| `SetTheme()` Method | `SetSkin()` Method |
| `ApplicationTheme` Enum | `AppSkin` Enum |
| `ThemeColors` Class | `SkinColors` Class |

**Skin-Namen (Farben statt Marken):**

| Aktuell | Neu (Farbe) | Akzentfarbe |
|---------|-------------|-------------|
| `Original` | `System` | Windows Akzentfarbe |
| `Modern` | `Blue` | Blau |
| `Classic` | `Green` | Gruen |
| `Dark` | `Violet` | Violett |
| `EsuCabControl` | `Orange` | Orange |
| `RocoZ21` | `DarkOrange` | Dunkelorange |
| `MaerklinCS` | `Red` | Rot |

**Betroffene Dateien:**
- `WinUI/Service/IThemeProvider.cs` -> `ISkinProvider.cs`
- `WinUI/Service/ThemeProvider.cs` -> `SkinProvider.cs`
- `WinUI/Service/ThemeColors.cs` -> `SkinColors.cs`
- `WinUI/Service/ApplicationTheme.cs` -> `AppSkin.cs`
- `WinUI/View/TrainControlPage.xaml.cs`
- `WinUI/View/SignalBoxPage.xaml.cs`
- `WinUI/View/TrainControlPage.xaml` (Flyout Labels)
- `WinUI/View/SignalBoxPage.xaml` (Flyout Labels)
- `WinUI/App.xaml.cs` (DI Registration)
- `Common/Configuration/AppSettings.cs` (falls Theme dort gespeichert)

---

### 2. ReactApp SPA 404-Fehler

> **Status:** OFFEN - Middleware fehlt
> **Prioritaet:** NIEDRIG (WinUI hat Prioritaet)

**Problem:**
- ReactApp zeigt HTTP 404 fuer alle Routen
- `Microsoft.AspNetCore.SpaProxy` Paket installiert
- Aber: Kein SPA-Middleware im Pipeline!

**Loesung:**
```csharp
// In ReactApp/Program.cs vor app.RunAsync():
app.MapFallbackToFile("index.html");
// ODER
app.UseSpa(spa =>
{
    spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});
```

**Hinweis:** Vite Dev-Server muss auf Port 5173 laufen (`npm run dev` im reactclient Ordner)

---

## ✅ ERLEDIGT (2026-01-24)

### SignalBoxPage MVVM Refactoring
- SignalBoxPage.Mapping.cs geloescht
- Lokale Typen (TrackElement, TrackElementType) entfernt
- Domain-Enums (SignalBoxSymbol, SignalAspect, SwitchPosition) verwendet
- SignalBoxPlanViewModel mit Editor-Methoden erweitert

### SignalBoxPage Skin-System Integration
- IThemeProvider injiziert (wie TrainControlPage)
- ApplyThemeColors() implementiert
- Skin-Flyout mit allen 7 Skins
- Event-Handler fuer ThemeChanged/DarkModeChanged
- DI-Registration in App.xaml.cs aktualisiert

### SignalBoxPage Stellwerk-Funktionalitaet
- Gleisdarstellung (vereinfachte Linien, Bezier-Kurven)
- Weichen mit aktiver Stellung in Gruen
- Dreiwege-Weiche hinzugefuegt
- Signale mit Blink-Animation (Ks1Blink, Zs1)
- Doppelklick zum Umschalten

---

## 📊 OFFENE QUALITY TASKS

- [ ] Domain-Enums Dokumentation (11 Typen)
- [ ] Test Coverage verbessern
- [ ] VSM Audit: JourneysPage, TrainsPage, SettingsPage

---

## 🏗️ ARCHITEKTUR-ENTSCHEIDUNGEN

### Skin-System Pattern
```
ISkinProvider (Interface)
    |
    +-- CurrentSkin: AppSkin
    +-- IsDarkMode: bool
    +-- SetSkin(AppSkin skin)
    +-- SkinChanged Event
    +-- DarkModeChanged Event
    |
SkinColors.GetPalette(skin, isDark) -> SkinPalette
    |
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
    _skinProvider.SkinChanged += (s, e) => DispatcherQueue.TryEnqueue(ApplyThemeColors);

    // In Loaded:
    ApplyThemeColors();

    // In Unloaded:
    _skinProvider.SkinChanged -= ...;
}
```

---

## 📋 REGELN

1. XAML zuerst schreiben, dann Code-Behind
2. Fluent Design System konsistent anwenden
3. Skin-Support auf allen neuen Pages (ISkinProvider Pattern)
4. Responsive Layout mit VisualStateManager (3 States)
5. Keine hardcodierten Farben (Theme-Resources oder SkinColors)
6. Markennamen vermeiden (Farbnamen verwenden)

