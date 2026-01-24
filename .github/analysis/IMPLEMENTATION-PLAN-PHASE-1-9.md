# 🚀 MOBAflow Implementation Plan - Phase 1-9
> Erstellt: 2025-01-24 | Status: Ready for Session Start  
> Geschätzte Gesamtzeit: 13-15 Minuten (AI-Ausführung) | Bottleneck: Build-Kompilation

---

## 📋 ÜBERBLICK

**Ziel:** Alle 9 Phasen in einer Session ausführen  
**Abhängigkeiten:** Phase 1 → 2 → 3 (sequenziell). Phase 6,7,8,9 können parallel nach Phase 1.  
**Kritische Dateien:** TrackPlanPage.xaml.cs, CanvasRenderer.cs, TrackPlanEditorViewModel.cs

---

## 🎯 PHASE 1: IDesignSystemProvider Foundation (60 min)

### Ziel
Schaffe das Basis-Framework für Design System Switching (Fluent, Metro, Material3, Minimal).

### Abhängigkeiten
- Keine (Foundation Phase)

### Sub-Steps

#### 1.1 Erstelle neue Datei: `SharedUI/DesignSystem/IDesignSystemProvider.cs`
```csharp
namespace MOBAflow.SharedUI.DesignSystem;

public interface IDesignSystemProvider
{
    DesignSystem CurrentSystem { get; }
    DesignTokens GetTokens(ElementTheme theme);
    event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    void SetDesignSystem(DesignSystem system);
}

public enum DesignSystem
{
    FluentDefault,
    FluentHighContrast,
    Metro,
    MetroHighContrast,
    Material3,
    MinimalLight,
    MinimalDark,
    MinimalHighViz
}

public record DesignTokens(
    Color AccentColor,
    Color PrimaryText,
    Color SecondaryText,
    Color PortOpenColor,
    Color PortConnectedColor,
    Color SnapPreviewColor,
    Color GridColor,
    double GridOpacity,
    double GhostOpacity);

public class DesignSystemChangedEventArgs : EventArgs
{
    public DesignSystem PreviousSystem { get; init; }
    public DesignSystem NewSystem { get; init; }
}
```

#### 1.2 Erstelle `SharedUI/DesignSystem/DefaultDesignSystemProvider.cs`
Implementiere FluentDesignSystemProvider (bereits existierend in UpdateTheme() - refaktoriere)

```csharp
public class DefaultDesignSystemProvider : IDesignSystemProvider
{
    public DesignSystem CurrentSystem => DesignSystem.FluentDefault;
    
    public event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    public DesignTokens GetTokens(ElementTheme theme)
    {
        var accentColor = GetColorResource("SystemAccentColor", Colors.Blue);
        var primaryText = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
        var portOpen = GetColorResource("SystemFillColorCaution", Colors.Orange);
        var portConnected = GetColorResource("SystemFillColorPositive", Colors.Green);
        
        return new DesignTokens(
            AccentColor: accentColor,
            PrimaryText: primaryText,
            SecondaryText: theme == ElementTheme.Dark ? Color.FromArgb(255, 200, 200, 200) : Color.FromArgb(255, 100, 100, 100),
            PortOpenColor: portOpen,
            PortConnectedColor: portConnected,
            SnapPreviewColor: accentColor,
            GridColor: Colors.Gray,
            GridOpacity: 0.25,
            GhostOpacity: theme == ElementTheme.Dark ? 0.85 : 0.75
        );
    }
    
    public void SetDesignSystem(DesignSystem system) => throw new NotSupportedException("Use service to change");
    
    private Color GetColorResource(string resourceName, Color fallback)
    {
        // Implementation aus TrackPlanPage.UpdateTheme()
        if (Application.Current?.Resources.TryGetValue(resourceName, out var resource) ?? false)
            return (Color)resource;
        return fallback;
    }
}
```

#### 1.3 Erstelle `SharedUI/DesignSystem/MetroDesignSystemProvider.cs`
Implementiere klassisches Windows Metro Design

```csharp
public class MetroDesignSystemProvider : IDesignSystemProvider
{
    public DesignSystem CurrentSystem => DesignSystem.Metro;
    
    public event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    public DesignTokens GetTokens(ElementTheme theme)
    {
        var primaryText = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
        var portOpen = theme == ElementTheme.Dark ? Color.FromArgb(255, 255, 140, 0) : Color.FromArgb(255, 0, 120, 215);
        var portConnected = theme == ElementTheme.Dark ? Color.FromArgb(255, 76, 175, 80) : Color.FromArgb(255, 51, 153, 102);
        
        return new DesignTokens(
            AccentColor: Color.FromArgb(255, 0, 120, 215),  // Metro Blue
            PrimaryText: primaryText,
            SecondaryText: theme == ElementTheme.Dark ? Color.FromArgb(255, 170, 170, 170) : Color.FromArgb(255, 128, 128, 128),
            PortOpenColor: portOpen,
            PortConnectedColor: portConnected,
            SnapPreviewColor: Color.FromArgb(255, 0, 120, 215),
            GridColor: Color.FromArgb(255, 192, 192, 192),
            GridOpacity: 0.20,  // Metro: cleaner grid
            GhostOpacity: 0.70  // Metro: consistent opacity
        );
    }
    
    public void SetDesignSystem(DesignSystem system) => throw new NotSupportedException("Use service to change");
}
```

#### 1.4 Erstelle `SharedUI/DesignSystem/MinimalDesignSystemProvider.cs`
Implementiere minimalistisches Design (Light/Dark/HighViz)

```csharp
public class MinimalDesignSystemProvider : IDesignSystemProvider
{
    private readonly DesignSystem _variant;
    
    public DesignSystem CurrentSystem => _variant;
    
    public event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    public MinimalDesignSystemProvider(DesignSystem variant) => _variant = variant;
    
    public DesignTokens GetTokens(ElementTheme theme)
    {
        return _variant switch
        {
            DesignSystem.MinimalLight => GetLightTokens(),
            DesignSystem.MinimalDark => GetDarkTokens(),
            DesignSystem.MinimalHighViz => GetHighVizTokens(),
            _ => throw new ArgumentException()
        };
    }
    
    private DesignTokens GetLightTokens() => new(
        AccentColor: Colors.Black,
        PrimaryText: Colors.Black,
        SecondaryText: Colors.Gray,
        PortOpenColor: Color.FromArgb(255, 255, 165, 0),
        PortConnectedColor: Color.FromArgb(255, 0, 128, 0),
        SnapPreviewColor: Colors.Black,
        GridColor: Color.FromArgb(255, 230, 230, 230),
        GridOpacity: 0.15,
        GhostOpacity: 0.60
    );
    
    private DesignTokens GetDarkTokens() => new(
        AccentColor: Colors.White,
        PrimaryText: Colors.White,
        SecondaryText: Color.FromArgb(255, 180, 180, 180),
        PortOpenColor: Color.FromArgb(255, 255, 165, 0),
        PortConnectedColor: Color.FromArgb(255, 144, 238, 144),
        SnapPreviewColor: Colors.White,
        GridColor: Color.FromArgb(255, 80, 80, 80),
        GridOpacity: 0.20,
        GhostOpacity: 0.80
    );
    
    private DesignTokens GetHighVizTokens() => new(
        AccentColor: Colors.Yellow,
        PrimaryText: Colors.Black,
        SecondaryText: Colors.Black,
        PortOpenColor: Colors.Red,
        PortConnectedColor: Colors.Green,
        SnapPreviewColor: Colors.Yellow,
        GridColor: Colors.Black,
        GridOpacity: 0.30,
        GhostOpacity: 0.90
    );
    
    public void SetDesignSystem(DesignSystem system) => throw new NotSupportedException("Use service to change");
}
```

#### 1.5 Refaktoriere `TrackPlanPage.xaml.cs` - UpdateTheme() Method
Ersetze hardcodierte Farben mit IDesignSystemProvider

**Änderungen:**
- Füge `_designSystemProvider` als DI-Parameter hinzu
- In `UpdateTheme()`: Nutze `_designSystemProvider.GetTokens(ActualTheme)` statt `GetColorResource()`
- Aktualisiere alle 6 Farbvariablen

#### 1.6 Registriere in DI Container
In `SharedUI/ServiceCollectionExtensions.cs` oder `App.xaml.cs`:
```csharp
services.AddSingleton<IDesignSystemProvider>(sp => new DefaultDesignSystemProvider());
```

#### 1.7 Führe Build aus
```bash
run_build()
# Erwartet: 0 Errors
```

#### 1.8 Commit
```bash
git add -A && git commit -m "Phase 1: IDesignSystemProvider Foundation with Metro + Minimal variants"
```

---

## 🎨 PHASE 2: Composition Effects + Settings UI (80 min)

### Ziel
Implementiere visuelle Effekte (GaussianBlur, DropShadow, etc.) + Settings-Dialog für Design System Switching.

### Abhängigkeiten
- Phase 1 (IDesignSystemProvider)

### Sub-Steps

#### 2.1 Erstelle `SharedUI/DesignSystem/DesignSystemService.cs`
Service-Klasse für Runtime-Switching

```csharp
public class DesignSystemService
{
    private IDesignSystemProvider _provider;
    
    public event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    public DesignSystemService(IDesignSystemProvider initialProvider)
    {
        _provider = initialProvider;
    }
    
    public void SetDesignSystem(DesignSystem system)
    {
        var newProvider = system switch
        {
            DesignSystem.FluentDefault => new DefaultDesignSystemProvider(),
            DesignSystem.Metro => new MetroDesignSystemProvider(),
            DesignSystem.MinimalLight => new MinimalDesignSystemProvider(DesignSystem.MinimalLight),
            DesignSystem.MinimalDark => new MinimalDesignSystemProvider(DesignSystem.MinimalDark),
            DesignSystem.MinimalHighViz => new MinimalDesignSystemProvider(DesignSystem.MinimalHighViz),
            _ => throw new ArgumentException()
        };
        
        var oldSystem = _provider.CurrentSystem;
        _provider = newProvider;
        SystemChanged?.Invoke(this, new DesignSystemChangedEventArgs { PreviousSystem = oldSystem, NewSystem = system });
    }
    
    public DesignTokens GetCurrentTokens(ElementTheme theme) => _provider.GetTokens(theme);
}
```

#### 2.2 Implementiere Composition Effects in `CanvasRenderer.cs`
Füge 3 Effekte für Ghost, Snap, Selected Track hinzu:

**Ghost Effect (GaussianBlur + Fade):**
```csharp
private void ApplyGhostBlurEffect(Canvas canvas, double opacity)
{
    var visual = ElementCompositionPreview.GetElementVisual(canvas);
    var compositor = visual.Compositor;
    
    var blurEffect = new GaussianBlurEffect
    {
        BlurAmount = 4.0f,
        Source = new CompositionEffectSourceParameter("source")
    };
    
    var effectFactory = compositor.CreateEffectFactory(blurEffect);
    var effectBrush = effectFactory.CreateBrush();
    
    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
    opacityAnimation.InsertKeyFrame(0f, 1.0f);
    opacityAnimation.InsertKeyFrame(1f, opacity);
    opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
    
    visual.StartAnimation("Opacity", opacityAnimation);
}
```

**Snap Highlight (DropShadow + Pulse):**
```csharp
private void ApplySnapHighlight(UIElement element, Color highlightColor)
{
    var visual = ElementCompositionPreview.GetElementVisual(element);
    var compositor = visual.Compositor;
    
    var dropShadow = compositor.CreateDropShadowEffect();
    dropShadow.BlurRadius = 8;
    dropShadow.OffsetX = 0;
    dropShadow.OffsetY = 0;
    dropShadow.Color = highlightColor;
    dropShadow.Opacity = 0.8f;
    
    var scaleAnimation = compositor.CreateScalarKeyFrameAnimation();
    scaleAnimation.InsertKeyFrame(0f, 1.0f);
    scaleAnimation.InsertKeyFrame(0.5f, 1.1f);
    scaleAnimation.InsertKeyFrame(1f, 1.0f);
    scaleAnimation.Duration = TimeSpan.FromMilliseconds(600);
    scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
    
    visual.StartAnimation("Scale.X", scaleAnimation);
    visual.StartAnimation("Scale.Y", scaleAnimation);
}
```

#### 2.3 Erstelle Settings Dialog: `WinUI/View/DesignSystemSettingsDialog.xaml`
```xaml
<ContentDialog x:Class="MOBAflow.WinUI.DesignSystemSettingsDialog"
    Title="Design System Settings"
    PrimaryButtonText="Apply"
    SecondaryButtonText="Cancel">
    <StackPanel Spacing="12">
        <TextBlock Text="Select Design System:" FontWeight="Bold"/>
        <ComboBox x:Name="DesignSystemCombo" SelectedIndex="0">
            <x:String>Fluent Default</x:String>
            <x:String>Fluent High Contrast</x:String>
            <x:String>Metro</x:String>
            <x:String>Minimal Light</x:String>
            <x:String>Minimal Dark</x:String>
            <x:String>Minimal High Viz</x:String>
        </ComboBox>
        <TextBlock Text="Preview:" FontWeight="Bold" Margin="0,12,0,0"/>
        <Grid x:Name="PreviewGrid" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" 
              Padding="12" CornerRadius="8" Height="100"/>
    </StackPanel>
</ContentDialog>
```

#### 2.4 Code-Behind für Settings Dialog
```csharp
public sealed partial class DesignSystemSettingsDialog : ContentDialog
{
    private readonly DesignSystemService _designSystemService;
    
    public DesignSystemSettingsDialog(DesignSystemService service)
    {
        _designSystemService = service;
        this.InitializeComponent();
        DesignSystemCombo.SelectionChanged += (s, e) => UpdatePreview();
    }
    
    private void UpdatePreview()
    {
        // Zeige Live-Preview der Farben
        var system = DesignSystemCombo.SelectedIndex switch { /* ... */ };
        var tokens = _designSystemService.GetCurrentTokens(ActualTheme);
        PreviewGrid.Background = new SolidColorBrush(tokens.SnapPreviewColor);
    }
    
    public DesignSystem SelectedDesignSystem => 
        DesignSystemCombo.SelectedIndex switch { /* ... */ };
}
```

#### 2.5 Integriere Settings Dialog in `TrackPlanPage.xaml.cs`
Füge Settings-Button hinzu + öffne Dialog

#### 2.6 Speichere Preference in LocalSettings
```csharp
ApplicationData.Current.LocalSettings.Values["DesignSystem"] = system.ToString();
```

#### 2.7 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 2: Composition Effects + Design System Settings UI"
```

---

## 🎨 PHASE 3: Material3 + Minimal Variants Complete (90 min)

### Ziel
Integriere Material Design 3 (NuGet), erweitere Minimal-Varianten.

### Abhängigkeiten
- Phase 1-2

### Sub-Steps

#### 3.1 Installiere NuGet: Material.WinUI.3
```bash
# In NuGet Package Manager Console:
Install-Package Material.WinUI.3
```

#### 3.2 Erstelle `SharedUI/DesignSystem/Material3DesignSystemProvider.cs`
Implementiere Material3 mit Dynamic Colors

```csharp
public class Material3DesignSystemProvider : IDesignSystemProvider
{
    public DesignSystem CurrentSystem => DesignSystem.Material3;
    
    public event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    public DesignTokens GetTokens(ElementTheme theme)
    {
        // Material3 Dynamic Colors basierend auf System-Theme
        var accentColor = Color.FromArgb(255, 103, 58, 183);  // Deep Purple
        var primaryText = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
        
        return new DesignTokens(
            AccentColor: accentColor,
            PrimaryText: primaryText,
            SecondaryText: theme == ElementTheme.Dark ? Color.FromArgb(255, 200, 200, 200) : Color.FromArgb(255, 100, 100, 100),
            PortOpenColor: Color.FromArgb(255, 255, 152, 0),   // Material Orange
            PortConnectedColor: Color.FromArgb(255, 76, 175, 80),  // Material Green
            SnapPreviewColor: accentColor,
            GridColor: theme == ElementTheme.Dark ? Color.FromArgb(255, 100, 100, 100) : Color.FromArgb(255, 200, 200, 200),
            GridOpacity: 0.22,
            GhostOpacity: 0.77
        );
    }
    
    public void SetDesignSystem(DesignSystem system) => throw new NotSupportedException();
}
```

#### 3.3 Aktualisiere `DesignSystemService.cs`
Füge Material3 zu Switch-Statement hinzu

#### 3.4 Erweitere `DesignSystemSettingsDialog.xaml`
Füge Material3 Option hinzu (jetzt 7 Optionen statt 6)

#### 3.5 Erstelle Settings Persistence
Erweitere LocalSettings um Theme-Preferences Export/Import

```csharp
public void SavePreferences()
{
    ApplicationData.Current.LocalSettings.Values["DesignSystem"] = CurrentSystem.ToString();
    ApplicationData.Current.LocalSettings.Values["Theme"] = ActualTheme.ToString();
}

public void LoadPreferences()
{
    var savedSystem = ApplicationData.Current.LocalSettings.Values["DesignSystem"]?.ToString();
    if (savedSystem != null && Enum.TryParse<DesignSystem>(savedSystem, out var system))
        _designSystemService.SetDesignSystem(system);
}
```

#### 3.6 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 3: Material3 Integration + Persistent Theme Preferences"
```

---

## 🔗 PHASE 6: Snap-to-Connect Service (70 min)

### Ziel
Optimiere Snap-Logik für Multi-Port Detection + Performance.

### Abhängigkeiten
- Keine (kann parallel nach Phase 1 laufen)

### Sub-Steps

#### 6.1 Analysiere `TrackPlanEditorViewModel.cs`
- Finde `SnapEdgeToPort()` Methode
- Verstehe aktuelle Offset-Berechnung
- Identifiziere Performance-Bottlenecks

#### 6.2 Erstelle neue Datei: `TrackPlan.Editor/Service/SnapToConnectService.cs`

```csharp
public class SnapToConnectService
{
    private const double SNAP_THRESHOLD = 10.0; // pixels
    
    public record SnapCandidate(Guid TrackId, PortLocation Port, double Distance);
    
    public SnapCandidate? FindBestSnap(Point dragPosition, IEnumerable<TrackTemplate> catalog)
    {
        var candidates = new List<SnapCandidate>();
        
        foreach (var track in catalog)
        {
            foreach (var port in track.Ports)
            {
                var distance = CalculateDistance(dragPosition, port.GlobalPosition);
                if (distance < SNAP_THRESHOLD)
                {
                    candidates.Add(new SnapCandidate(track.Id, port, distance));
                }
            }
        }
        
        return candidates.OrderBy(c => c.Distance).FirstOrDefault();
    }
    
    public List<SnapCandidate> FindAllSnapsInRange(Point dragPosition, IEnumerable<TrackTemplate> catalog)
    {
        var candidates = new List<SnapCandidate>();
        
        // Multi-Port Detection für komplexe Layouts
        foreach (var track in catalog)
        {
            foreach (var port in track.Ports)
            {
                var distance = CalculateDistance(dragPosition, port.GlobalPosition);
                if (distance < SNAP_THRESHOLD)
                    candidates.Add(new SnapCandidate(track.Id, port, distance));
            }
        }
        
        return candidates.OrderBy(c => c.Distance).ToList();
    }
    
    private double CalculateDistance(Point p1, Point p2)
        => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
}
```

#### 6.3 Integriere in `TrackPlanEditorViewModel.cs`
- Ersetze inline Snap-Logik mit Service
- Nutze `FindBestSnap()` für Single-Snap
- Nutze `FindAllSnapsInRange()` für Preview

#### 6.4 Performance-Optimierung
- Implementiere Spatial Indexing (nur Ports in Nähe prüfen)
- Cache Port-Positionen zwischen Drag-Moves
- Test mit 100+ Elements auf Canvas

#### 6.5 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 6: Snap-to-Connect Service with Multi-Port Detection + Performance Optimization"
```

---

## 🚂 PHASE 7: Piko A Track Catalog Expansion (50 min)

### Ziel
Erweitere Track-Katalog mit R9-Oval, BWL/BWR, W3, Switch States.

### Abhängigkeiten
- Keine (unabhängig)

### Sub-Steps

#### 7.1 Analysiere aktuelle Track-Templates
In `TrackPlan.Domain/Catalog/PikoACatalog.cs`:
- WL (Weiche Links)
- WR (Weiche Rechts)
- G (Gerade)
- K (Kurve)

#### 7.2 Füge neue Templates hinzu

**R9-Oval:**
```csharp
public static TrackTemplate R9Oval => new(
    Id: Guid.Parse("...R9-Oval..."),
    Name: "R9 Oval",
    Geometry: new OvalGeometry(radiusX: 90, radiusY: 45),
    Ports: new[] { 
        new PortLocation(0, 45, Rotation.Degrees0),
        new PortLocation(180, 45, Rotation.Degrees180)
    }
);
```

**BWL (Bogen Weiche Links):**
```csharp
public static TrackTemplate BWL => new(
    Id: Guid.Parse("...BWL..."),
    Name: "Bogen Weiche Links",
    Geometry: new CurvedSwitchGeometry(isLeft: true, radius: 45),
    Ports: new[] {
        new PortLocation(0, 0, Rotation.Degrees0),
        new PortLocation(45, 45, Rotation.Degrees90),
        new PortLocation(45, -45, Rotation.Degrees270)
    }
);
```

**W3 (3-Wege Weiche):**
```csharp
public static TrackTemplate W3 => new(
    Id: Guid.Parse("...W3..."),
    Name: "3-Way Switch",
    Geometry: new ThreeWaySwitchGeometry(),
    Ports: new[] {
        new PortLocation(0, 0, Rotation.Degrees0),
        new PortLocation(45, 45, Rotation.Degrees45),
        new PortLocation(-45, 45, Rotation.Degrees315)
    }
);
```

#### 7.3 Implementiere Switch Position States
```csharp
public enum SwitchPosition { Straight, Diverging1, Diverging2 }

public void VisualizeSwitchState(Canvas canvas, Switch track)
{
    var stateColor = track.Position switch
    {
        SwitchPosition.Straight => Colors.Green,
        SwitchPosition.Diverging1 => Colors.Orange,
        SwitchPosition.Diverging2 => Colors.Red,
    };
    
    // Zeichne State-Indicator (kleines Farbsymbol)
    var indicator = new Ellipse 
    { 
        Fill = new SolidColorBrush(stateColor),
        Width = 8,
        Height = 8
    };
    canvas.Children.Add(indicator);
}
```

#### 7.4 Test mit Snap-Service (Phase 6)
Stelle sicher, dass alle neuen Templates mit Snap-Logik funktionieren

#### 7.5 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 7: Piko A Track Catalog Expansion (R9-Oval, BWL, W3, Switch States)"
```

---

## ✨ PHASE 8: TrackPlanPage Animation & Effects (100 min)

### Ziel
Implementiere 6 WinUI 3 Animations/Effects für Interaktivität.

### Abhängigkeiten
- Phase 2 (Effect Patterns)

### Sub-Steps

#### 8.1 Ghost Track Blur Animation
In `CanvasRenderer.RenderGhostTrack()`:
- Fade-In: Opacity 0→1 (200ms)
- Blur: GaussianBlur 0→4 (200ms)
- Fade-Out on Release: Opacity 1→0 (150ms)

#### 8.2 Snap Highlight Pulse
In `CanvasRenderer.RenderSnapHighlight()`:
- DropShadow 4→12 (600ms, loop)
- Scale 1.0→1.1→1.0 (600ms, loop)
- Color Transition: Orange→Yellow→Orange

#### 8.3 Selected Track Glow
In `CanvasRenderer.RenderSelectedTrack()`:
- ColorAnimation: Normal Color→Accent (300ms)
- Opacity stays 1.0
- Continuous loop with easing

#### 8.4 Drag Start Fade
In `TrackPlanPage.PointerMoved()`:
- When drag starts: Ghost opacity 0→0.75/0.85 (200ms, ease-out)

#### 8.5 Connection Success Pulse
On successful snap:
- Green flash (50ms)
- Expansion ring: Scale 1.0→2.0 (500ms, ease-out)
- Fade-out simultaneously

#### 8.6 Grid Parallax (Optional)
Subtle depth effect on grid:
- Slight Y-offset based on scroll position
- Opacity varies with theme

#### 8.7 Performance Profiling
- Measure GPU load with 10+ animations simultaneously
- Optimize if necessary (reduce effect complexity)
- Profile on low-end hardware

#### 8.8 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 8: TrackPlanPage Animations & WinUI 3 Composition Effects"
```

---

## 🧠 PHASE 9.1: Attention Control (Neuro-UI) (40 min)

### Ziel
Reduziere kognitive Belastung durch Dimmen nicht-relevanter Tracks während Drag.

### Abhängigkeiten
- Keine (unabhängig)

### Sub-Steps

#### 9.1.1 Erstelle `CanvasRenderer.DimIrrelevantTracks()` Methode
```csharp
public void DimIrrelevantTracks(Canvas canvas, IEnumerable<Guid> selectedTrackIds, double opacity = 0.3)
{
    foreach (var shape in canvas.Children.OfType<UIElement>())
    {
        var trackId = (Guid?)shape.Tag;
        if (trackId.HasValue && !selectedTrackIds.Contains(trackId.Value))
        {
            var visual = ElementCompositionPreview.GetElementVisual(shape);
            visual.Opacity = (float)opacity;
        }
    }
}
```

#### 9.1.2 Integriere in `TrackPlanPage.BeginMultiGhostPlacement()`
- Rufe `DimIrrelevantTracks()` auf wenn Drag startet
- Restore full opacity wenn Drag endet (in PointerReleased)

#### 9.1.3 Füge Settings-Toggle hinzu
```csharp
private bool _enableAttentionControl = true;
// In DesignSystemSettingsDialog: Checkbox für "Reduce Cognitive Load"
```

#### 9.1.4 Unit Tests
- Test: Dimming works correctly
- Test: Restore on drag end
- Test: Works with all design systems

#### 9.1.5 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 9.1: Attention Control - Dim Irrelevant Tracks During Drag"
```

---

## 🎯 PHASE 9.2: Type Indicators (Neuro-UI) (30 min)

### Ziel
Zeige Switch-Typen durch Unicode-Symbole + Farben für schnelle Mustererkennung.

### Abhängigkeiten
- Keine (unabhängig)

### Sub-Steps

#### 9.2.1 Erstelle `CanvasRenderer.RenderSwitchTypeIndicator()` Methode
```csharp
private void RenderSwitchTypeIndicator(Canvas canvas, Switch track, SwitchGeometry geometry)
{
    var (symbol, color) = geometry.SwitchType switch
    {
        SwitchType.WL => ("◀", Colors.Blue),
        SwitchType.WR => ("▶", Colors.Red),
        SwitchType.W3 => ("▼", Colors.Green),
        SwitchType.BWL => ("⟲", Colors.Orange),
        SwitchType.BWR => ("⟳", Colors.Orange),
        _ => ("?", Colors.Gray)
    };
    
    var textBlock = new TextBlock
    {
        Text = symbol,
        FontSize = 8,
        Foreground = new SolidColorBrush(color),
        Opacity = 0.5
    };
    
    Canvas.SetLeft(textBlock, track.X + 2);
    Canvas.SetTop(textBlock, track.Y + 2);
    canvas.Children.Add(textBlock);
}
```

#### 9.2.2 Integriere in `CanvasRenderer.RenderTrack()`
- Rufe für jeden Switch auf
- Position: Top-Left corner

#### 9.2.3 Konfigurierbar machen
```csharp
public bool ShowTypeIndicators { get; set; } = true;
// In Settings hinzufügen
```

#### 9.2.4 Unit Tests
- Test: Correct symbol per switch type
- Test: Correct color mapping
- Test: Readable in all themes

#### 9.2.5 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 9.2: Type Indicators - Unicode Symbols for Switch Types"
```

---

## 🎨 PHASE 9.3: Hover Affordances (Neuro-UI) (20 min)

### Ziel
Zeige Interaktivität durch visuelle Feedback auf Hover (Ports, Tracks).

### Abhängigkeiten
- Keine (unabhängig)

### Sub-Steps

#### 9.3.1 Implementiere Port Hover State
In `TrackPlanPage.xaml.cs`:
```csharp
private void Port_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    var port = sender as UIElement;
    var visual = ElementCompositionPreview.GetElementVisual(port);
    visual.Opacity = 1.0f;
    // TODO: Add StrokeThickness animation
}

private void Port_PointerExited(object sender, PointerRoutedEventArgs e)
{
    var port = sender as UIElement;
    var visual = ElementCompositionPreview.GetElementVisual(port);
    visual.Opacity = 0.6f;
}
```

#### 9.3.2 Implementiere Track Hover State
```csharp
private void Track_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    var track = sender as UIElement;
    if (_canDragTrack(track))
    {
        var brush = new SolidColorBrush(Colors.Yellow);
        brush.Opacity = 0.2;
        track.Opacity = 0.8;  // Highlight
    }
}

private void Track_PointerExited(object sender, PointerRoutedEventArgs e)
{
    track.Opacity = 1.0;
}
```

#### 9.3.3 Optional: Sound Effect
Bei Port-Hover (auditory feedback):
```csharp
private async void PlaySnapReadySound()
{
    var uri = new Uri("ms-appx:///Assets/Sounds/snap-ready.wav");
    var element = new MediaElement { Source = uri };
    // Play
}
```

#### 9.3.4 Unit Tests
- Test: Hover states apply correctly
- Test: Cursor changes on hover
- Test: No performance impact

#### 9.3.5 Build + Commit
```bash
run_build()
git add -A && git commit -m "Phase 9.3: Hover Affordances - Visual Feedback for Interactivity"
```

---

## ✅ FINALIZATION (30 min)

### Sub-Steps

#### F.1 Regression Testing
- Teste alle 9 Phasen zusammen
- Überprüfe: Multi-Drag, Snap, Effects, Design Systems
- Teste alle 7 Design System Kombinationen

#### F.2 Build Verification
```bash
run_build()  # Debug
run_build()  # Release
# Erwartet: 0 Errors in beiden
```

#### F.3 Performance Profiling
- Öffne Größtes Projekt in TrackPlanPage
- Aktiviere alle Effects + Neuro-UI Features
- Überprüfe: Smooth 60 FPS

#### F.4 Final Commit + Push
```bash
git add -A && git commit -m "Complete Implementation: Phases 1-9 (Design Systems, Effects, Neuro-UI)"
git push origin main
```

#### F.5 Update todos.instructions.md
- Markiere alle Phasen als ✅ Complete
- Dokumentiere Performance-Metriken
- Setze neue TODO-Items für Phase 10+

---

## 📊 ZEITÜBERSICHT

| Phase | Beschreibung | Zeit | Dependency |
|-------|-------------|------|-----------|
| 1 | Design System Foundation | 60 min | None |
| 2 | Effects + Settings UI | 80 min | Phase 1 |
| 3 | Material3 + Minimal | 90 min | Phase 1-2 |
| 6 | Snap Service | 70 min | None |
| 7 | Track Catalog | 50 min | None |
| 8 | Animations | 100 min | Phase 2 |
| 9.1 | Attention Control | 40 min | None |
| 9.2 | Type Indicators | 30 min | None |
| 9.3 | Hover Affordances | 20 min | None |
| Fin | Testing + Push | 30 min | All |
| **TOTAL** | **All Phases** | **~570 min** | **Sequential** |

**Parallel möglich (2-3 gleichzeitig):**
- Phase 1 + Phase 6/7 (60+50 = 110 min)
- Phase 2 (nach 1) + Phase 6/7/9.x (80+70 = 150 min)
- Phase 3 (nach 2) + Phase 8/9.x (90+100 = 190 min)

**Mit Parallelisierung: ~350-400 Minuten statt 570**

---

## 🎯 START CHECKLIST FOR TOMORROW

**Before Session Starts:**
- [ ] Öffne Visual Studio
- [ ] Öffne MOBAflow Solution
- [ ] Checkout main Branch
- [ ] Öffne diese Datei im Editor

**During Session:**
- [ ] Start Phase 1 Step 1.1
- [ ] Führe jeden Step sequenziell aus
- [ ] Nach jedem Phase: `run_build()` + `git commit`
- [ ] Nutze Plan als Checkpoints

**Files to Have Open:**
- TrackPlanPage.xaml.cs
- CanvasRenderer.cs
- TrackPlanEditorViewModel.cs
- .github/analysis/DESIGN-SYSTEMS-AND-EFFECTS.md (Reference)

---

**Plan erstellt:** 2025-01-24  
**Status:** Ready for Session Start ✅  
**Nächster Schritt:** Phase 1 Step 1.1
