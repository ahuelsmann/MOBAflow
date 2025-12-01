# Station/Platform Hybrid Model - Implementation Guide

**Datum**: 2025-12-01  
**Status**: 🎯 Design finalisiert - Implementierung erforderlich

---

## 🎯 Konzept: Adaptive Properties

### Logik
```csharp
if (station.Platforms == null || station.Platforms.Count == 0)
{
    // Phase 1: Verwende Station-Properties
    string track = station.Track;
    string arrival = station.Arrival;
    // ...
}
else
{
    // Phase 2: Verwende Platform-Properties
    foreach (var platform in station.Platforms)
    {
        string track = platform.Track;
        var arrival = platform.ArrivalTime;
        // ...
    }
}
```

### Vorteile
- ✅ Rückwärtskompatibilität (Phase 1 Daten bleiben gültig)
- ✅ Sanfter Übergang zu Platform-Modell
- ✅ Keine Breaking Changes bei Migration
- ✅ Flexibilität für verschiedene Anwendungsfälle

---

## 📊 Betroffene Manager-Klassen

### 1. StationManager
Muss prüfen welche Properties verwendet werden sollen:

```csharp
// Backend\Manager\StationManager.cs
public class StationManager
{
    private readonly IZ21 _z21;
    private readonly List<Station> _stations;
    private readonly WorkflowService _workflowService;

    public async Task ExecuteStationWorkflowAsync(Station station)
    {
        // Bestimme welches Workflow ausgeführt werden soll
        Workflow? workflow;
        int? feedbackPort;
        
        if (station.Platforms == null || station.Platforms.Count == 0)
        {
            // Phase 1: Station-Level Workflow
            workflow = station.Flow;
            feedbackPort = station.FeedbackInPort;
        }
        else
        {
            // Phase 2: Platform-Level Workflow
            // TODO: Bestimme welche Platform basierend auf Feedback
            var targetPlatform = DeterminePlatform(station, currentFeedback);
            workflow = targetPlatform?.Flow;
            feedbackPort = targetPlatform?.FeedbackInPort;
        }
        
        if (workflow != null)
        {
            await _workflowService.ExecuteAsync(workflow);
        }
    }
    
    private Platform? DeterminePlatform(Station station, int feedbackPort)
    {
        // Finde Platform basierend auf FeedbackInPort
        return station.Platforms.FirstOrDefault(p => p.FeedbackInPort == feedbackPort);
    }
}
```

### 2. JourneyManager
Muss beim Erreichen einer Station die richtige Logik verwenden:

```csharp
// Backend\Manager\JourneyManager.cs
public class JourneyManager
{
    private async Task HandleStationReachedAsync(Journey journey, Station station)
    {
        // Prüfe welche Properties verwendet werden sollen
        if (station.Platforms == null || station.Platforms.Count == 0)
        {
            // Phase 1: Einfache Station
            await HandleSimpleStationAsync(journey, station);
        }
        else
        {
            // Phase 2: Station mit Platforms
            await HandlePlatformStationAsync(journey, station);
        }
    }
    
    private async Task HandleSimpleStationAsync(Journey journey, Station station)
    {
        // Verwende Station.Flow, Station.Track, etc.
        if (station.Flow != null)
        {
            await _workflowService.ExecuteAsync(station.Flow);
        }
        
        // Log: Zug erreicht Station {Name} auf Gleis {Track}
        _logger.LogInformation(
            "Journey {Journey} reached station {Station} on track {Track}",
            journey.Name, station.Name, station.Track ?? "unknown"
        );
    }
    
    private async Task HandlePlatformStationAsync(Journey journey, Station station)
    {
        // Bestimme welche Platform basierend auf Context
        var platform = DetermineTargetPlatform(station, journey);
        
        if (platform?.Flow != null)
        {
            await _workflowService.ExecuteAsync(platform.Flow);
        }
        
        // Log: Zug erreicht Station {Name} auf Platform {Platform}
        _logger.LogInformation(
            "Journey {Journey} reached station {Station} on platform {Platform} (track {Track})",
            journey.Name, station.Name, platform?.Name, platform?.Track
        );
    }
}
```

### 3. PlatformManager (falls existiert)
Muss beide Modi unterstützen:

```csharp
// Backend\Manager\PlatformManager.cs
public class PlatformManager
{
    public async Task OnFeedbackReceivedAsync(int inPort)
    {
        // Finde Station für diesen InPort
        var station = _stations.FirstOrDefault(s => 
            s.FeedbackInPort == inPort || 
            s.Platforms.Any(p => p.FeedbackInPort == inPort)
        );
        
        if (station == null) return;
        
        if (station.Platforms == null || station.Platforms.Count == 0)
        {
            // Phase 1: Station-Level Feedback
            if (station.FeedbackInPort == inPort && station.Flow != null)
            {
                await _workflowService.ExecuteAsync(station.Flow);
            }
        }
        else
        {
            // Phase 2: Platform-Level Feedback
            var platform = station.Platforms.FirstOrDefault(p => p.FeedbackInPort == inPort);
            if (platform?.Flow != null)
            {
                await _workflowService.ExecuteAsync(platform.Flow);
            }
        }
    }
}
```

---

## 🎨 UI-Anpassungen

### 1. StationViewModel - Adaptive Property Wrappers

```csharp
// SharedUI\ViewModel\StationViewModel.cs
public partial class StationViewModel : ObservableObject
{
    // ... bestehende Properties ...
    
    // Adaptive Properties: Zeige Station- ODER erste Platform-Properties
    
    /// <summary>
    /// Gets the effective track number.
    /// Returns Station.Track if no platforms, otherwise first Platform.Track.
    /// </summary>
    public string? EffectiveTrack
    {
        get
        {
            if (Model.Platforms == null || Model.Platforms.Count == 0)
                return Model.Track;
            
            return Model.Platforms[0].Track;
        }
    }
    
    /// <summary>
    /// Gets the effective arrival designation.
    /// Returns Station.Arrival if no platforms, otherwise first Platform arrival info.
    /// </summary>
    public string? EffectiveArrival
    {
        get
        {
            if (Model.Platforms == null || Model.Platforms.Count == 0)
                return Model.Arrival;
            
            // TODO: Format TimeSpan from Platform.ArrivalTime
            return Model.Platforms[0].ArrivalTime?.ToString(@"hh\:mm") ?? "-";
        }
    }
    
    /// <summary>
    /// Gets the effective departure designation.
    /// </summary>
    public string? EffectiveDeparture
    {
        get
        {
            if (Model.Platforms == null || Model.Platforms.Count == 0)
                return Model.Departure;
            
            return Model.Platforms[0].DepartureTime?.ToString(@"hh\:mm") ?? "-";
        }
    }
    
    /// <summary>
    /// Gets the effective exit orientation.
    /// </summary>
    public bool EffectiveIsExitOnLeft
    {
        get
        {
            if (Model.Platforms == null || Model.Platforms.Count == 0)
                return Model.IsExitOnLeft;
            
            return Model.Platforms[0].IsExitOnLeft;
        }
    }
    
    /// <summary>
    /// Indicates if station uses platform-based configuration.
    /// </summary>
    public bool UsesPlatforms => Model.Platforms != null && Model.Platforms.Count > 0;
    
    /// <summary>
    /// Gets display text indicating configuration mode.
    /// </summary>
    public string ConfigurationMode => UsesPlatforms 
        ? $"Platform Mode ({Model.Platforms.Count} platforms)" 
        : "Simple Mode";
}
```

### 2. XAML - Adaptive Bindings

**Option A: Verwende EffectiveXxx Properties (Empfohlen)**
```xaml
<!-- WinUI\View\ProjectConfigurationPage.xaml -->
<ListView ItemsSource="{x:Bind ViewModel.Stations}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="vm:StationViewModel">
            <Grid>
                <TextBlock Text="{x:Bind Name, Mode=OneWay}" />
                <TextBlock Text="{x:Bind EffectiveTrack, Mode=OneWay}" />        ⭐
                <TextBlock Text="{x:Bind NumberOfLapsToStop, Mode=OneWay}" />
                <TextBlock Text="{x:Bind EffectiveArrival, Mode=OneWay}" />      ⭐
                <TextBlock Text="{x:Bind EffectiveDeparture, Mode=OneWay}" />    ⭐
                <TextBlock Text="{x:Bind Flow.Name, Mode=OneWay}" />
                <CheckBox IsChecked="{x:Bind EffectiveIsExitOnLeft, Mode=OneWay}" />  ⭐
                
                <!-- Badge: Zeige Modus -->
                <TextBlock Text="{x:Bind ConfigurationMode, Mode=OneWay}" 
                           Style="{ThemeResource CaptionTextBlockStyle}"
                           Opacity="0.6" />
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Option B: Conditional Visibility**
```xaml
<!-- Zeige verschiedene UI je nach Modus -->
<StackPanel>
    <!-- Phase 1: Einfache Ansicht -->
    <Grid Visibility="{x:Bind UsesPlatforms, Mode=OneWay, Converter={StaticResource InvertedBoolToVisibilityConverter}}">
        <TextBlock Text="{x:Bind Track, Mode=OneWay}" />
        <TextBlock Text="{x:Bind Arrival, Mode=OneWay}" />
    </Grid>
    
    <!-- Phase 2: Platform-Liste -->
    <ListView ItemsSource="{x:Bind Model.Platforms, Mode=OneWay}"
              Visibility="{x:Bind UsesPlatforms, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
        <ItemTemplate>
            <DataTemplate x:DataType="domain:Platform">
                <TextBlock Text="{x:Bind Track}" />
            </DataTemplate>
        </ItemTemplate>
    </ListView>
</StackPanel>
```

### 3. Detail-Ansicht mit Umschaltung

```xaml
<!-- WinUI\View\StationDetailPage.xaml (NEU) -->
<Page>
    <StackPanel>
        <TextBlock Text="{x:Bind ViewModel.SelectedStation.Name}" 
                   Style="{ThemeResource TitleTextBlockStyle}" />
        
        <!-- Toggle: Simple / Platform Mode -->
        <ToggleSwitch Header="Use Platform Configuration"
                      IsOn="{x:Bind ViewModel.SelectedStation.UsesPlatforms, Mode=TwoWay}"
                      OnContent="Platform Mode"
                      OffContent="Simple Mode" />
        
        <!-- Simple Mode Properties -->
        <StackPanel Visibility="{x:Bind ViewModel.SelectedStation.UsesPlatforms, 
                                       Mode=OneWay, 
                                       Converter={StaticResource InvertedBoolToVisibilityConverter}}">
            <TextBox Header="Track" Text="{x:Bind ViewModel.SelectedStation.Track, Mode=TwoWay}" />
            <TextBox Header="Arrival" Text="{x:Bind ViewModel.SelectedStation.Arrival, Mode=TwoWay}" />
            <TextBox Header="Departure" Text="{x:Bind ViewModel.SelectedStation.Departure, Mode=TwoWay}" />
            <CheckBox Content="Exit on Left" IsChecked="{x:Bind ViewModel.SelectedStation.IsExitOnLeft, Mode=TwoWay}" />
        </StackPanel>
        
        <!-- Platform Mode - Liste bearbeiten -->
        <StackPanel Visibility="{x:Bind ViewModel.SelectedStation.UsesPlatforms, 
                                       Mode=OneWay, 
                                       Converter={StaticResource BoolToVisibilityConverter}}">
            <Button Content="Add Platform" Command="{x:Bind ViewModel.AddPlatformCommand}" />
            
            <ListView ItemsSource="{x:Bind ViewModel.SelectedStation.Model.Platforms}">
                <!-- Platform Editor -->
            </ListView>
        </StackPanel>
    </StackPanel>
</Page>
```

---

## 🔄 Workflow Integration

### Action-Klassen müssen Station/Platform unterscheiden

```csharp
// Backend\Action\StationAnnouncement.cs
public class StationAnnouncement : Base
{
    public Station Station { get; set; }
    
    public override async Task ExecuteAsync(IZ21 z21, ActionExecutionContext context)
    {
        string announcement;
        
        if (Station.Platforms == null || Station.Platforms.Count == 0)
        {
            // Phase 1: Einfache Ansage
            announcement = $"Arriving at {Station.Name} on track {Station.Track ?? "unknown"}";
        }
        else
        {
            // Phase 2: Platform-spezifische Ansage
            var platform = context.CurrentPlatform ?? Station.Platforms[0];
            announcement = $"Arriving at {Station.Name}, platform {platform.Name}, track {platform.Track}";
        }
        
        await _ttsService.SpeakAsync(announcement);
    }
}
```

### WorkflowService Extension

```csharp
// Backend\Services\WorkflowService.cs
public class WorkflowService
{
    public async Task ExecuteStationWorkflowAsync(Station station, ActionExecutionContext? context = null)
    {
        context ??= new ActionExecutionContext();
        context.CurrentStation = station;
        
        Workflow? workflow = null;
        
        if (station.Platforms == null || station.Platforms.Count == 0)
        {
            // Phase 1: Station-Workflow
            workflow = station.Flow;
        }
        else
        {
            // Phase 2: Bestimme Platform basierend auf Context
            var platform = DetermineTargetPlatform(station, context);
            context.CurrentPlatform = platform;
            workflow = platform?.Flow ?? station.Flow; // Fallback to station workflow
        }
        
        if (workflow != null)
        {
            await ExecuteAsync(workflow, context);
        }
    }
    
    private Platform? DetermineTargetPlatform(Station station, ActionExecutionContext context)
    {
        // Logik: Finde Platform basierend auf:
        // 1. Feedback InPort
        // 2. Journey-Kontext
        // 3. Default: Erste Platform
        
        if (context.FeedbackPort.HasValue)
        {
            return station.Platforms.FirstOrDefault(
                p => p.FeedbackInPort == context.FeedbackPort
            );
        }
        
        return station.Platforms.FirstOrDefault();
    }
}
```

---

## 📝 ActionExecutionContext erweitern

```csharp
// Backend\Services\ActionExecutionContext.cs
public class ActionExecutionContext
{
    public Station? CurrentStation { get; set; }
    public Platform? CurrentPlatform { get; set; }  // NEU
    public Journey? CurrentJourney { get; set; }
    public int? FeedbackPort { get; set; }         // NEU
    public DateTime ExecutionTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets the effective track - from Platform if available, otherwise from Station.
    /// </summary>
    public string? EffectiveTrack =>
        CurrentPlatform?.Track ?? CurrentStation?.Track;
    
    /// <summary>
    /// Gets the effective workflow - from Platform if available, otherwise from Station.
    /// </summary>
    public Workflow? EffectiveWorkflow =>
        CurrentPlatform?.Flow ?? CurrentStation?.Flow;
}
```

---

## ✅ Implementierungs-Checkliste

### Phase 1: Manager-Klassen anpassen
- [ ] StationManager: Adaptive Workflow-Logik
- [ ] JourneyManager: Adaptive Station-Handling
- [ ] PlatformManager: Beide Modi unterstützen
- [ ] WorkflowService: Platform-Context hinzufügen
- [ ] ActionExecutionContext: CurrentPlatform + FeedbackPort

### Phase 2: ViewModel anpassen
- [ ] StationViewModel: EffectiveXxx Properties
- [ ] StationViewModel: UsesPlatforms Property
- [ ] StationViewModel: ConfigurationMode Property

### Phase 3: XAML anpassen
- [ ] ProjectConfigurationPage: EffectiveXxx Bindings
- [ ] (Optional) StationDetailPage: Toggle Simple/Platform Mode

### Phase 4: Action-Klassen anpassen
- [ ] StationAnnouncement: Adaptive Ansagen
- [ ] Alle Actions mit Station-Referenz prüfen

### Phase 5: Testing
- [ ] Unit Tests: Station ohne Platforms
- [ ] Unit Tests: Station mit Platforms
- [ ] Integration Tests: Workflow-Ausführung
- [ ] UI Tests: Beide Modi

---

## 📖 Referenz

- **Domain**: `Domain\Station.cs`, `Domain\Platform.cs`
- **Manager**: `Backend\Manager\StationManager.cs`, `JourneyManager.cs`
- **Services**: `Backend\Services\WorkflowService.cs`, `ActionExecutionContext.cs`
- **ViewModel**: `SharedUI\ViewModel\StationViewModel.cs`
- **UI**: `WinUI\View\ProjectConfigurationPage.xaml`

**Status**: 🎯 Design finalisiert - Bereit für Implementierung
