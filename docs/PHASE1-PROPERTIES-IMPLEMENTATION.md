# Phase 1 Properties - Station mit Platform-Features

**Datum**: 2025-12-01  
**Status**: ✅ Temporäre Properties hinzugefügt für Phase 1 MVP

---

## 🎯 Problem & Lösung

### Problem
Phase 1 soll nur **Stations** anzeigen (ohne Platform-Details), aber die Properties wie `Track`, `Arrival`, `Departure`, `IsExitOnLeft` gehören konzeptionell zur **Platform**-Ebene.

### Lösung
**Temporäre "flache" Properties** in Station für Phase 1:
- Station bekommt die Properties vorübergehend
- Dokumentiert mit TODO-Kommentaren
- Werden in Phase 2 zu Platform verschoben

---

## 📊 Implementierte Änderungen

### 1. Domain\Station.cs
```csharp
// --- Phase 1 Properties (Simplified Platform representation) ---
// TODO: Move to Platform when Phase 2 is implemented

public string? Track { get; set; }         // Gleis-Nummer
public string? Arrival { get; set; }       // Ankunft
public string? Departure { get; set; }     // Abfahrt
public bool IsExitOnLeft { get; set; }     // Ausstieg links
```

### 2. SharedUI\ViewModel\StationViewModel.cs
```csharp
// --- Phase 1 Properties (Simplified Platform representation) ---
// TODO: Remove when Phase 2 (Platform support) is implemented

public string? Track { get; set; }
public string? Arrival { get; set; }
public string? Departure { get; set; }
public bool IsExitOnLeft { get; set; }
```

### 3. WinUI\View\ProjectConfigurationPage.xaml
```xaml
<!-- Stations ListView Headers -->
<TextBlock Text="Name" />
<TextBlock Text="Track" />          ⭐ NEU
<TextBlock Text="Laps to Stop" />
<TextBlock Text="Arrival" />        ⭐ NEU
<TextBlock Text="Departure" />      ⭐ NEU
<TextBlock Text="Workflow" />
<TextBlock Text="Exit Left" />      ⭐ NEU

<!-- Stations ListView Items -->
<TextBlock Text="{x:Bind Name}" />
<TextBlock Text="{x:Bind Track}" />              ⭐ NEU
<TextBlock Text="{x:Bind NumberOfLapsToStop}" />
<TextBlock Text="{x:Bind Arrival}" />            ⭐ NEU
<TextBlock Text="{x:Bind Departure}" />          ⭐ NEU
<TextBlock Text="{x:Bind Flow.Name}" />
<CheckBox IsChecked="{x:Bind IsExitOnLeft}" />   ⭐ NEU
```

---

## 🔄 Migrations-Plan für Phase 2

### Schritt 1: Properties von Station zu Platform verschieben

**Domain\Station.cs - ENTFERNEN:**
```csharp
// Diese Properties löschen:
public string? Track { get; set; }
public string? Arrival { get; set; }
public string? Departure { get; set; }
public bool IsExitOnLeft { get; set; }
```

**Domain\Platform.cs - HINZUFÜGEN:**
```csharp
// Platform hat bereits: Track (als string)
// Neu hinzufügen:
public TimeSpan? ArrivalTime { get; set; }
public TimeSpan? DepartureTime { get; set; }
public bool IsExitOnLeft { get; set; }
```

### Schritt 2: StationViewModel anpassen

**SharedUI\ViewModel\StationViewModel.cs:**
```csharp
// Phase 1 Properties ENTFERNEN
// Stattdessen: Platform-Collection verwalten

public ObservableCollection<PlatformViewModel> PlatformViewModels { get; }

public StationViewModel(Station model)
{
    Model = model;
    PlatformViewModels = new ObservableCollection<PlatformViewModel>(
        Model.Platforms.Select(p => new PlatformViewModel(p))
    );
}
```

### Schritt 3: Neue PlatformViewModel erstellen

**SharedUI\ViewModel\PlatformViewModel.cs (NEU):**
```csharp
public partial class PlatformViewModel : ObservableObject
{
    [ObservableProperty]
    private Platform model;
    
    public string? Track => Model.Track;
    public TimeSpan? Arrival => Model.ArrivalTime;
    public TimeSpan? Departure => Model.DepartureTime;
    public bool IsExitOnLeft => Model.IsExitOnLeft;
}
```

### Schritt 4: UI umbauen

**Option A: Erweiterte Station-Ansicht mit Platforms**
```xaml
<ListView ItemsSource="{x:Bind ViewModel.Stations}">
    <ItemTemplate>
        <StackPanel>
            <!-- Station Header -->
            <TextBlock Text="{x:Bind Name}" FontWeight="Bold" />
            
            <!-- Platforms darunter -->
            <ListView ItemsSource="{x:Bind PlatformViewModels}">
                <ItemTemplate>
                    <Grid>
                        <TextBlock Text="{x:Bind Track}" />
                        <TextBlock Text="{x:Bind Arrival}" />
                        <TextBlock Text="{x:Bind Departure}" />
                    </Grid>
                </ItemTemplate>
            </ListView>
        </StackPanel>
    </ItemTemplate>
</ListView>
```

**Option B: Separate Platform-Detail-Seite**
```
Stations-Liste → Station auswählen → Platform-Details-Seite
```

---

## 📝 Wichtige Hinweise

### Für Phase 1 (Aktuell)
- ✅ Station hat alle Properties direkt
- ✅ UI kann Gleis-Details anzeigen
- ✅ Vereinfachtes Datenmodell
- ⚠️ Properties sind an falscher Stelle (Station statt Platform)

### Für Phase 2 (Migration)
- 🔜 Properties in korrektes Entity verschieben
- 🔜 Hierarchie: Station → Platforms → Platform-Details
- 🔜 UI: Entweder verschachtelte ListView oder separate Detailseite
- 🔜 Datenmigration: Bestehende Station.Track → Platform.Track

### Datenmigration
Wenn gespeicherte Daten existieren (JSON), muss bei Phase 2 Migration erfolgen:

```csharp
// Beispiel-Migration
foreach (var station in solution.Stations)
{
    if (!string.IsNullOrEmpty(station.Track))
    {
        // Alte Phase 1 Daten: Eine Station hatte ein Gleis
        var platform = new Platform
        {
            Name = $"Gleis {station.Track}",
            Track = station.Track,
            ArrivalTime = ParseTime(station.Arrival),
            DepartureTime = ParseTime(station.Departure),
            IsExitOnLeft = station.IsExitOnLeft
        };
        station.Platforms.Add(platform);
        
        // Alte Properties löschen
        station.Track = null;
        station.Arrival = null;
        station.Departure = null;
    }
}
```

---

## ✅ Status

- ✅ Phase 1 Properties implementiert
- ✅ Domain Model erweitert
- ✅ ViewModel erweitert
- ✅ XAML aktualisiert
- ✅ Build erfolgreich
- ⏸️ Phase 2 Migration dokumentiert (wartet auf Implementierung)

---

## 📖 Referenz

- **Domain**: `Domain\Station.cs`, `Domain\Platform.cs`
- **ViewModel**: `SharedUI\ViewModel\StationViewModel.cs`
- **XAML**: `WinUI\View\ProjectConfigurationPage.xaml`
- **Analyse**: `docs\STATION-PROPERTIES-ANALYSIS.md`

**Nächster Schritt**: In Phase 2 Properties zu Platform migrieren gemäß Plan oben.
