---
description: 'Minimale TODO-Liste fuer MOBAflow'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2026-01-24

---

## ✅ ERLEDIGT (2026-01-24)

### Skin-System Refactoring (Naming Convention)
- `IThemeProvider` -> `ISkinProvider`
- `ThemeProvider` -> `SkinProvider`
- `ThemeColors` -> `SkinColors`
- `ThemeResourceBuilder` -> `SkinResourceBuilder`
- `ApplicationTheme` enum -> `AppSkin` enum
- Skin-Namen: System, Blue, Green, Violet, Orange, DarkOrange, Red
- Alle Pages aktualisiert (TrainControlPage, SignalBoxPage, MainWindow, SettingsPage)
- XAML Event-Handler umbenannt

### ReactApp SPA Fix
- **Development:** Vite Dev-Server auf Port 5173 (`cd ClientApp && npm run dev`)
  - React-App direkt unter http://localhost:5173 aufrufen
  - ASP.NET Backend laeuft auf separatem Port fuer API-Calls
- **Production:** `app.MapFallbackToFile("index.html")` served aus wwwroot

### SignalBoxPage MVVM Refactoring
- SignalBoxPage.Mapping.cs geloescht
- Domain-Enums (SignalBoxSymbol, SignalAspect, SwitchPosition) verwendet
- SignalBoxPlanViewModel mit Editor-Methoden erweitert
- ISkinProvider injiziert, ApplySkinColors() implementiert

---

## 🚨 NAECHSTE AUFGABEN

### 1. VisualStateManager (VSM) fuer Responsive Layout

> **Status:** OFFEN
> **Prioritaet:** MITTEL

**Analyse durchgefuehrt:**

| Page | VSM Status | Prioritaet |
|------|------------|------------|
| TrainControlPage.xaml | ✅ HAT VSM | - |
| SignalBoxPage.xaml | ✅ HAT VSM | - |
| TrackPlanPage.xaml | ✅ HAT VSM | - |
| WorkflowsPage.xaml | ✅ HAT VSM | - |
| JourneysPage.xaml | ❌ KEIN VSM | HOCH |
| TrainsPage.xaml | ❌ KEIN VSM | HOCH |
| SettingsPage.xaml | ❌ KEIN VSM | MITTEL |
| SolutionPage.xaml | ❌ KEIN VSM | MITTEL |
| OverviewPage.xaml | ❌ KEIN VSM | NIEDRIG |
| MonitorPage.xaml | ❌ KEIN VSM | NIEDRIG |
| JourneyMapPage.xaml | ❌ KEIN VSM | NIEDRIG |
| HelpPage.xaml | ❌ KEIN VSM | NIEDRIG |
| InfoPage.xaml | ❌ KEIN VSM | NIEDRIG |

**VSM Breakpoints (aus winui.instructions.md):**
- Compact: 0-640px (Mobile/Tablet Portrait)
- Medium: 641-1199px (Tablet Landscape)
- Wide: 1200px+ (Desktop)

---

### 2. IPersistablePage Architektur-Review

> **Status:** OFFEN - Architektur-Entscheidung erforderlich
> **Prioritaet:** MITTEL

**Aktuelle Implementierung:**
- `IPersistablePage` wird von **Pages** implementiert (z.B. SignalBoxPage)
- `PagePersistenceCoordinator` sammelt alle registrierten Pages
- Bei SolutionSaving/SolutionLoaded werden SyncToModel()/LoadFromModel() aufgerufen

**Analyse:**

| Page | IPersistablePage | Hat eigene Daten? |
|------|------------------|-------------------|
| SignalBoxPage | ✅ JA | ✅ SignalBoxPlan |
| TrackPlanPage | ❌ NEIN | ✅ TrackPlan |
| JourneysPage | ❌ NEIN | ✅ Journeys (via ViewModel) |
| TrainsPage | ❌ NEIN | ✅ Trains (via ViewModel) |
| WorkflowsPage | ❌ NEIN | ✅ Workflows (via ViewModel) |
| Andere | ❌ NEIN | ❌ NEIN |

**Frage: Sollten ViewModels statt Pages IPersistablePage implementieren?**

**Pro ViewModel:**
- ViewModels sind bereits fuer Datenverwaltung zustaendig
- Besser testbar (Unit Tests ohne UI)
- Klare Trennung: ViewModel = Daten, Page = UI
- ViewModels existieren unabhaengig von Pages

**Pro Page:**
- Page weiss wann UI-Elemente initialisiert sind
- Direkter Zugriff auf UI-State (z.B. ScrollPosition)
- Einfachere DI-Registration (Page ist bereits Singleton)

**Empfehlung:**
```
HYBRID-ANSATZ:
1. IPersistableViewModel fuer Datenlogik (SyncToModel, LoadFromModel)
2. Page ruft ViewModel.SyncToModel() auf (fuer UI-spezifische Dinge)
3. PagePersistenceCoordinator arbeitet mit ViewModels
```

---

### 3. Fehlende IPersistablePage Implementierungen

> **Status:** OFFEN
> **Prioritaet:** HOCH (Datenverlust moeglich)

Folgende Pages verwalten Daten, implementieren aber NICHT IPersistablePage:
- [ ] TrackPlanPage
- [ ] JourneysPage (via JourneyViewModel)
- [ ] TrainsPage (via TrainViewModel)
- [ ] WorkflowsPage (via WorkflowViewModel)

**Risiko:** Aenderungen koennten bei Solution-Wechsel verloren gehen.

---

## 📊 OFFENE QUALITY TASKS

- [ ] Domain-Enums Dokumentation (11 Typen)
- [ ] Test Coverage verbessern
- [ ] VSM fuer JourneysPage und TrainsPage implementieren
- [ ] IPersistablePage fuer alle datenverwaltenden Pages

---

## 🏗️ ARCHITEKTUR-REFERENZ

### Skin-System Pattern (AKTUELL)
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
4. Responsive Layout mit VisualStateManager (3 States)
5. Keine hardcodierten Farben (Theme-Resources oder SkinColors)
6. Markennamen vermeiden (Farbnamen verwenden)

