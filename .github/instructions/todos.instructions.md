---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24 (Session 4: Phase 9 Testing + Features Backlog)

---

## 🔴 KRITISCH

_Keine kritischen Aufgaben offen._

---

## 📋 Session 2025-01-24 (Session 4): Phase 9 Testing + Feature Backlog - ✅ ABGESCHLOSSEN

**Fokus:** Phase 9.2/9.3 Test Suite + User-Facing Features Roadmap

**Phase 9 Testing (✅ COMPLETE):**
1. ✅ **Phase9RenderingTests.cs erstellt** - 27 NUnit Tests für Type Indicators & Hover Affordances
   - ✅ PositionStateRenderer Tests (13 Tests)
   - ✅ Catalog API Tests (6 Tests)
   - ✅ TrackPlanEditorViewModel Integration (8 Tests)
   - ✅ Added TrackPlan.Editor project reference to Test.csproj
   - ✅ Build: 0 Errors

**Settings Feature (✅ PARTIAL):**
2. ✅ **Locomotive Library Icon** - Geändert zu Library-Symbol (&#xE82D;) in SettingsPage.xaml
3. ✅ **Speech Setup Guide** - Button navigiert bereits zu HelpPage mit "Azure Speech Setup" Topic
4. 🚫 **Speech Test Error Handling** - BLOCKED: TestSpeechCommand fehlt im MainWindowViewModel

**Help & Info Features (📋 QUEUED):**
- 📋 **Help Wiki Integration** - "More coming…" durch Wiki-Inhalte ersetzen (Markdown-Parsing, File I/O)
- 📋 **Info README Display** - Root README.md in InfoPage anzeigen
- ⚠️ **Sonderzeichen in Wiki** - Potenzial Encoding-Issue (UTF-8 BOM check erforderlich)

**UI Enhancement Features (📋 QUEUED):**
5. 📋 **Skin Persistence** - TrainControlPage & SignalBoxPage: Last selected Skin wird nicht gespeichert
   - Erfordert: PreferencesService + ISkinProvider.SkinChanged Event Subscription
   - Pattern: Siehe Skin-System Reference unten

6. 📋 **TrainControl Enhancements** - Mehrere Features:
   - [ ] **SteppingMode Enum** - Optionen: 14, 28, 128 Fahrstufen (für Locomotive Model)
   - [ ] **Speed Gauge Update** - Vmax-Eingabe aktualisiert Speedometer-Werte
   - [ ] **Station Display** - Letzte/Aktuelle/Nächste Haltestelle als vertikale Liste
      - Binding: Wie Journey Map, aber vertikal, von Selected Journey
   - [ ] **Speed/Stepping Display** - Aktuelle Geschwindigkeit + Fahrstufen anzeigen

**Tech Debt & Fixes (📋 QUEUED):**
7. 📋 **ReactApp DI Container** - System.InvalidOperationException: Unable to resolve ISwaggerProvider
   - Betroffen: https://localhost:49913/
   - Ursache: Swagger Middleware Registration oder Middleware-Reihenfolge
   - Fix: Program.cs Startup.cs überprüfen

8. 📋 **Dead Code Cleanup** - Alle Member mit 0 Verweisen (ausser Views/Pages) überprüfen & löschen
   - Empfehlung: Separate dedizierte Session (17 Projekte = großer Scope)
   - Priorität: Niedrig (Code Quality, nicht User-Facing)

---

## 📊 Session 4 Outcome Summary

| Task | Status | Owner | LOC | Blockers |
|------|--------|-------|-----|----------|
| Phase 9 Tests | ✅ | Copilot | 27 Tests | None |
| Library Icon | ✅ | Copilot | 1 | None |
| Speech Guide | ✅ | Existing | 0 | None |
| Speech Error | 🚫 | BLOCKED | 60 | TestSpeechCommand missing |
| Skin Persist | 📋 | TODO | 100 | Design review needed |
| Help Wiki | 📋 | TODO | 150 | Markdown parser |
| TrainControl | 📋 | TODO | 200 | LocomotiveViewModel |
| ReactApp DI | 📋 | TODO | 50 | Swagger config |
| README Display | 📋 | TODO | 80 | File I/O |
| Dead Code | 📋 | TODO | TBD | 17 projects |

**Recommended Next Session Priority:**
1. **Speech Error Handling** - Implement TestSpeechCommand (unblocks error messages)
2. **Skin Persistence** - Fix PreferencesService integration (quick win)
3. **ReactApp DI** - Fix Swagger registration (critical for dev experience)

---

### 🔧 Implementation Notes for Next Session

**Step 5 - Speech Test Error Handling (60 LOC):**
```csharp
// In MainWindowViewModel.Settings.cs add:
[RelayCommand]
private async Task TestSpeech()
{
    // Validate key exists
    if (string.IsNullOrEmpty(SpeechKey))
    {
        ErrorMessage = "Azure Speech Key is not configured. Please enter your key in Settings.";
        ShowErrorMessage = true;
        return;
    }
    
    try
    {
        // Get speech service, test synthesis
        await _speechService.SynthesizeAsync("This is a test message", cancellationToken: default);
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Speech test failed: {ex.Message}";
        ShowErrorMessage = true;
    }
}
```

**Step 6 - Skin Persistence (100 LOC):**
```csharp
// In TrainControlPage.xaml.cs:
public sealed partial class TrainControlPage : Page
{
    private readonly ISkinProvider _skinProvider;
    private readonly IPreferencesService _preferencesService; // New
    
    public TrainControlPage(TrainControlViewModel vm, ISkinProvider skinProvider, IPreferencesService prefs)
    {
        _skinProvider = skinProvider;
        _preferencesService = prefs;
        ViewModel = vm;
        InitializeComponent();
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Load last skin
        var lastSkin = _preferencesService.GetLastSelectedSkin();
        if (lastSkin.HasValue)
            _skinProvider.SelectSkin(lastSkin.Value);
        
        // Subscribe to changes
        _skinProvider.SkinChanged += OnSkinChanged;
    }
    
    private void OnSkinChanged(AppSkin skin) =>
        _preferencesService.SaveLastSelectedSkin(skin);
    
    private void Page_Unloaded(object sender, RoutedEventArgs e) =>
        _skinProvider.SkinChanged -= OnSkinChanged;
}
```

**Step 7 - TrainControl Enhancements (200 LOC):**
```csharp
// Add to Domain/Model/Locomotive.cs:
public enum SteppingMode
{
    Steps14 = 14,
    Steps28 = 28,
    Steps128 = 128
}

// In Locomotive class:
public SteppingMode SteppingMode { get; set; } = SteppingMode.Steps28;

// In TrainControlPage: Display current stepping + speed
// Binding: vm.SelectedLocomotive.SteppingMode
// Binding: vm.CurrentSpeed (0-126) mapped to steps
```

**Step 11 - ReactApp DI Fix (50 LOC):**
```csharp
// In Program.cs - check Swagger registration order:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // BEFORE app.UseSwagger()

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();           // Must be AFTER Build()
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
```

---

## 🚂 TrackPlan Roadmap

| Phase | Fokus | Status |
|-------|-------|--------|
| 1 | Geometry Tests | ✅ |
| 2 | SVG Debug Exporter | ✅ |
| 3 | Instructions | ✅ |
| 4 | Renderer Y-Fix + Templates | ✅ |
| 5 | Multi-Ghost + Design Quality | ✅ |
| 6 | Snap-to-Connect Service | ✅ |
| 7 | Piko A Catalog | ✅ |
| 8 | Animation & Effects | ✅ |
| 9 | Neuro-UI Design | ✅ Testing Complete |

**Nächste:** Phase 9.1-9.3 Implementierung (Attention Control, Type Indicators Rendering, Hover Affordances)

---

## 📚 Quality Roadmap

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
    private readonly IPreferencesService _preferencesService; // NEW

    // Constructor: Save preferences service
    // Loaded: Load last skin + Subscribe to SkinChanged
    // OnSkinChanged: Save to preferences
    // Unloaded: Unsubscribe from SkinChanged
}
```

---

## 📋 REGELN

1. Datei lesen vor Änderungen
2. Offene Tasks nicht löschen
3. Erledigte Tasks entfernen (nicht markieren)



