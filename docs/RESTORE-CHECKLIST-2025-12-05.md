# Wiederherstellungs-Checkliste für Session 2025-12-05

**Basis**: `docs/SESSION-SUMMARY-2025-12-04.md` enthält alle Details  
**Geschätzte Zeit**: 25 Minuten

---

## ✅ Phase 1: Announcement-Bug fixen (5 Min)

### Datei: `Backend/Services/ActionExecutor.cs`

**Suche Zeile**: `private async Task ExecuteAnnouncementAsync`

**Ersetze**:
```csharp
if (action.Parameters == null)
    throw new ArgumentException("Announcement action requires Parameters");

if (context.SpeakerEngine == null)
{
    Debug.WriteLine($"    ⚠ Announcement skipped: No SpeakerEngine configured");
    return;
}

var message = action.Parameters["Message"].ToString()!;
```

**Durch**:
```csharp
if (action.Parameters == null || !action.Parameters.ContainsKey("Message"))
{
    Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: Missing Message parameter");
    return;
}

if (context.SpeakerEngine == null)
{
    Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: No SpeakerEngine");
    return;
}

var message = action.Parameters["Message"].ToString()!;
```

**Build & Test**: ✅

---

## ✅ Phase 2: UI-Verbesserungen (15 Min)

### 1. Grüne Farbe (1 Min)
**Datei**: `SharedUI/ViewModel/StationViewModel.cs`  
**Zeile**: ~200

```csharp
public string BackgroundColor => IsCurrentStation ? "#60A060" : "Transparent";
```

### 2. Reset-Button Größe (1 Min)
**Datei**: `WinUI/View/EditorPage.xaml`  
**Zeile**: ~220 (Journey Buttons)

```xml
<Button
    Command="{x:Bind ViewModel.ResetJourneyCommand}"
    Width="32"
    Height="32"
    Padding="0">
```

### 3. Margins/Paddings (5 Min)

#### Header Margin
**Zeile**: ~15
```xml
Margin="16,2,16,2"  <!-- war: 24,8,24,8 -->
```

#### TabView Margin
**Zeile**: ~25
```xml
Margin="16,0,16,0"  <!-- war: 24,0,24,8 -->
VerticalAlignment="Stretch"
```

#### Grid Paddings (mehrere Stellen)
```xml
<Grid Padding="4" VerticalAlignment="Stretch">  <!-- war: 8 oder 16 -->
```

#### Properties Header
**Zeile**: ~500
```xml
Padding="16,6,24,4"  <!-- war: 16,12,24,8 -->
```

#### List Item Padding (mehrere Stellen)
```xml
<TextBlock Padding="6" />  <!-- war: 8 -->
```

#### Button Margins (mehrere Stellen)
```xml
Margin="0,4,0,0"  <!-- war: 0,8,0,0 -->
```

### 4. Tab-Optionen (2 Min)

#### TabViewItems (alle 6)
```xml
<TabViewItem Header="Solution" IsClosable="False">
<TabViewItem Header="Journeys" IsClosable="False">
<TabViewItem Header="Workflows" IsClosable="False">
<TabViewItem Header="Trains" IsClosable="False">
<TabViewItem Header="Locomotives" IsClosable="False">
<TabViewItem Header="Wagons" IsClosable="False">
```

#### TabView
```xml
<TabView
    IsAddTabButtonVisible="False"
    CanDragTabs="False"
    CanReorderTabs="False">
```

### 5. Solution-Name entfernen (1 Min)
**Zeile**: ~18
```xml
<!-- Diese Zeile LÖSCHEN -->
<TextBlock Text="{x:Bind ViewModel.Solution.Name, Mode=OneWay}" />
```

### 6. Drop-Zone kompakter (3 Min)
**Datei**: `WinUI/Controls/SimplePropertyGrid.cs`  
**Zeile**: ~350

```csharp
Height = 44,        // war: 60
FontSize = 16,      // war: 20
Spacing = 2         // war: 4
```

### 7. Debug-Logs (2 Min)
**Datei**: `SharedUI/ViewModel/MainWindowViewModel.Z21.cs`

**Zeile 1**: Using hinzufügen
```csharp
using System.Diagnostics;
```

**Zeile ~115**: In `SimulateFeedback()`
```csharp
if (_journeyManager == null && Solution?.Projects.Count > 0)
{
    var project = Solution.Projects[0];
    
    Debug.WriteLine($"🔍 [DEBUG] Creating ExecutionContext:");
    Debug.WriteLine($"   - SelectedSpeakerEngine: {SelectedSpeakerEngine?.Name ?? "NULL"}");
    
    var executionContext = new ActionExecutionContext
    {
        Z21 = _z21,
        SpeakerEngine = SelectedSpeakerEngine
    };
    
    Debug.WriteLine($"   - ExecutionContext.SpeakerEngine: {executionContext.SpeakerEngine?.Name ?? "NULL"}");
    
    _journeyManager = _journeyManagerFactory.Create(/*...*/);
    
    Debug.WriteLine($"✅ [DEBUG] JourneyManager created with SpeakerEngine: {executionContext.SpeakerEngine?.Name ?? "NULL"}");
}
```

---

## ✅ Phase 3: Testen (5 Min)

1. **Build**: F6 → Sollte erfolgreich sein
2. **Run**: F5 → App starten
3. **Workflow reparieren**:
   - Workflows-Tab öffnen
   - "Arrival Main Station" auswählen
   - Action bearbeiten: Message = `"Nächster Halt: {StationName}"`
4. **Simulate**: InPort 1 → Durchsage sollte kommen! 🎤

---

## 📝 Nach Fertigstellung

- [ ] Commit: `git commit -am "feat: UI optimizations and announcement bug fix"`
- [ ] Review: `docs/SESSION-SUMMARY-2025-12-04.md`
- [ ] Nächste Session: Actions-Refactoring nach Backend verschieben

---

**Geschätzte Gesamt-Zeit**: 25 Minuten  
**Priorität**: Phase 1 (Bug-Fix) > Phase 2 (UI) > Phase 3 (Test)
