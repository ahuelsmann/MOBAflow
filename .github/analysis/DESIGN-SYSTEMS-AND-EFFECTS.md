# Design Systems, Skins & Visual Effects für WinUI 3

> Analyse: Alternativen zu Fluent Design + Runtime Design System Switching + Grafikeffekte

---

## 1. Moderne Alternativen zum Fluent Design System

### A. Fluent Design System (Aktuell in MOBAflow)

**Vorteile:**
- ✅ Native WinUI 3 Integration (SystemFillColor*, SystemAccentColor)
- ✅ Light/Dark Theme automatisch
- ✅ Konsistent mit Windows 11 UI
- ✅ Accessibility WCAG A+ out-of-box
- ✅ Performance optimiert (native GPU rendering)

**Nachteile:**
- ❌ Limited zur Windows Ecosystem
- ❌ Nur 2 Themes built-in (Light/Dark)
- ❌ Farbpalette relativ konservativ

---

### B. Material Design 3 (Google)

**Für WinUI 3:**
- 📦 NuGet: `Material.WinUI.3` (Community-maintained)
- 🎨 Dynamic color generation (Material You)
- 🌈 Umfangreiche Farbpalette (12+ vordefinierte Themes)
- 📱 Responsive grid + Spacing system

**Vorteile:**
- ✅ Cross-platform (Android/Web/WinUI)
- ✅ Material You: Dynamische Farben aus Wallpaper
- ✅ Strikte Design Guidelines mit Type Scale
- ✅ Aktive Community

**Nachteile:**
- ❌ Community-maintained (nicht offiziell von Google)
- ❌ Einige Performance-Overhead
- ❌ Learning curve für Design Rules

---

### C. Metro Design System (Windows 8/10 Classic)

**Für WinUI 3:**
- 🎨 Clean, minimalist aesthetic (Modern flat design)
- 📦 NuGet: `Microsoft.UI.Xaml` built-in support
- ⚡ Ultra-high performance (low overhead)
- 🔒 Conservative, proven design language

**Vorteile:**
- ✅ Windows 8/10 Klassiker - viele kennen das Design
- ✅ Extrem performant (keine extra dependencies)
- ✅ WCAG A+ Accessibility (high contrast)
- ✅ Clean typography, clear hierarchy
- ✅ Nostalgisch für Power-User

**Nachteile:**
- ❌ Wird von Microsoft nicht mehr aktiv entwickelt
- ❌ Weniger "modern" wirken als Fluent
- ❌ Limitierte Animation/Effect Support
- ❌ Farbpalette kleiner

**Verwendung (WinUI 3):**
```xml
<!-- Metro: Tile-basierte Farbpalette -->
<SolidColorBrush x:Key="MetroPrimary" Color="#0078D4" />      <!-- Classic Blue -->
<SolidColorBrush x:Key="MetroAccent" Color="#50E6FF" />       <!-- Cyan -->
<SolidColorBrush x:Key="MetroBackground" Color="#FFFFFF" />   <!-- White -->
<SolidColorBrush x:Key="MetroForeground" Color="#000000" />   <!-- Black -->
```

**Best For:**
- Classic/retro aesthetic preference
- Power-user workflows
- Enterprise applications
- Maximum performance on old hardware

---

### D. Custom Design Token System (Empfohlen für MOBAflow)

**Hybrid-Ansatz:**
- Fluent Design als Base (Windows-native)
- Custom Token Layer darüber
- Runtime-Switching für "Skins" (wie ISkinProvider)
- Composition Effects für Advanced Visuals

**Vorteile:**
- ✅ Maximale Kontrolle
- ✅ Performant (keine zusätzlichen Dependencies)
- ✅ Integriert sich mit bestehendem ISkinProvider-System
- ✅ Future-proof

---

## 2. Runtime Design System Switching für TrackPlanPage

### A. Aktuelles Modell (TrainControlPage / SignalBoxPage)

```csharp
public interface ISkinProvider
{
    AppSkin CurrentSkin { get; }
    event EventHandler<SkinChangedEventArgs>? SkinChanged;
}

public enum AppSkin
{
    System,      // Windows Theme
    Blue,
    Green,
    Violet,
    Orange,
    DarkOrange,
    Red
}
```

**Anwendung in Pages:**
```csharp
public sealed partial class TrainControlPage : Page
{
    private readonly ISkinProvider _skinProvider;

    public TrainControlPage(ISkinProvider skinProvider)
    {
        _skinProvider = skinProvider;
        _skinProvider.SkinChanged += OnSkinChanged;
    }

    private void OnSkinChanged(object? s, SkinChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => ApplySkinColors());
    }

    private void ApplySkinColors()
    {
        var palette = SkinColors.GetPalette(_skinProvider.CurrentSkin, IsDarkMode);
        MyButton.Background = palette.Primary;
        MyText.Foreground = palette.Secondary;
    }
}
```

---

### B. Erweiterte Design Systems für TrackPlanPage

#### Option 1: Design Token System (Lightweight)

```csharp
// Domain/DesignTokens.cs
public record DesignTokens
{
    // Colors
    public required Color Primary { get; init; }
    public required Color Secondary { get; init; }
    public required Color Accent { get; init; }
    public required Color Surface { get; init; }
    public required Color Error { get; init; }
    
    // Track-specific
    public required Color TrackNormal { get; init; }
    public required Color TrackSelected { get; init; }
    public required Color PortOpen { get; init; }
    public required Color PortConnected { get; init; }
    public required Color SnapPreview { get; init; }
    
    // Effects
    public required double GhostOpacity { get; init; }
    public required double GridOpacity { get; init; }
    public required double SelectionStrokeWidth { get; init; }
};

// Services/IDesignSystemProvider.cs
public interface IDesignSystemProvider
{
    DesignSystem CurrentSystem { get; }
    DesignTokens GetTokens(ElementTheme theme);
    event EventHandler<DesignSystemChangedEventArgs>? SystemChanged;
    
    // Runtime switching
    void SetDesignSystem(DesignSystem system);
}

public enum DesignSystem
{
    FluentDefault,    // Current (Fluent Design System)
    FluentHighContrast,
    Material3,        // Google Material Design
    MinimalDark,      // Minimalist (Dark)
    MinimalLight,     // Minimalist (Light)
    HighViz,          // High Visibility (for accessibility)
}
```

**Verwendung:**
```csharp
public sealed partial class TrackPlanPage : Page
{
    private readonly IDesignSystemProvider _designProvider;
    private DesignTokens _tokens = null!;

    public TrackPlanPage(IDesignSystemProvider designProvider)
    {
        _designProvider = designProvider;
        _designProvider.SystemChanged += OnSystemChanged;
        Loaded += (_, _) => ApplyDesignTokens();
    }

    private void OnSystemChanged(object? s, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyDesignTokens);
    }

    private void ApplyDesignTokens()
    {
        _tokens = _designProvider.GetTokens(ActualTheme);
        
        // Apply to brushes
        _trackBrush = new SolidColorBrush(_tokens.TrackNormal);
        _trackSelectedBrush = new SolidColorBrush(_tokens.TrackSelected);
        _portOpenBrush = new SolidColorBrush(_tokens.PortOpen);
        _portConnectedBrush = new SolidColorBrush(_tokens.PortConnected);
        _snapPreviewBrush = new SolidColorBrush(_tokens.SnapPreview);
        
        // Apply opacity values
        _ghostOpacity = _tokens.GhostOpacity;
        _gridOpacity = _tokens.GridOpacity;
        
        RenderGraph();
    }
}
```

---

#### Option 2: Material Design 3 Integration

**Installation:**
```powershell
Install-Package Material.WinUI.3
```

**Verwendung:**
```xml
<!-- App.xaml -->
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <XamlControlsResources />
        <Material3Resources />
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

**Themes:**
```csharp
public enum Material3Theme
{
    Light,
    Dark,
    DynamicLight,  // From wallpaper
    DynamicDark,
    TonalLight,    // Muted colors
    TonalDark,
}

// Runtime switching
public void SetMaterial3Theme(Material3Theme theme)
{
    // Apply Material3 color palette
    // Resources["Primary"] = MaterialColors.GetPrimary(theme);
}
```

---

### C. Implementation für MOBAflow: Empfohlener Weg

**Schritt 1: Existierendes Skin-System erweitern**
```csharp
// Keep ISkinProvider as-is for TrainControl/SignalBox
// Add IDesignSystemProvider for advanced pages

public interface IDesignSystemProvider
{
    DesignTokens GetTokens(AppSkin skin, ElementTheme theme);
    event EventHandler<EventArgs>? DesignChanged;
}
```

**Schritt 2: TrackPlanPage updaten**
```csharp
public sealed partial class TrackPlanPage : Page
{
    private readonly ISkinProvider _skinProvider;
    private readonly IDesignSystemProvider _designProvider;
    
    // Update colors on skin change
    private void OnSkinChanged() => UpdateTheme();
}
```

**Schritt 3: Settings UI für Design System Auswahl**
```xml
<ComboBox Header="Design System">
    <ComboBoxItem>Fluent Design (Default)</ComboBoxItem>
    <ComboBoxItem>Material Design 3</ComboBoxItem>
    <ComboBoxItem>Minimal (Light)</ComboBoxItem>
    <ComboBoxItem>Minimal (Dark)</ComboBoxItem>
    <ComboBoxItem>High Visibility</ComboBoxItem>
</ComboBox>
```

---

## 3. WinUI 3 Grafikeffekte für TrackPlanPage

### A. Verfügbare Effekte in WinUI 3

#### 1. **Composition Effects (Recommended)**

```csharp
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;

// Blur Effect (Ghost Track)
public void ApplyBlurToGhost(UIElement element)
{
    var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
    
    var blurEffect = new GaussianBlurEffect
    {
        BlurAmount = 5f,
        Source = new CompositionEffectSourceParameter("backdrop"),
        Optimization = EffectOptimization.Balanced
    };
    
    var effectFactory = compositor.CreateEffectFactory(blurEffect);
    var effectBrush = effectFactory.CreateBrush();
    
    var backdrop = compositor.CreateBackdropBrush();
    effectBrush.SetSourceParameter("backdrop", backdrop);
    
    var shape = compositor.CreateSpriteShape();
    shape.FillBrush = effectBrush;
    
    // Apply to ghost rendering
}

// Shadow Effect (Depth for snapping)
public void ApplyDropShadowToSnapPreview(UIElement element)
{
    var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
    var dropShadow = compositor.CreateDropShadow();
    
    dropShadow.BlurRadius = 10f;
    dropShadow.Opacity = 0.8f;
    dropShadow.Offset = new System.Numerics.Vector3(2, 2, 5);
    dropShadow.Color = Windows.UI.Colors.Black;
    
    var visual = ElementCompositionPreview.GetElementVisual(element);
    visual.Shadow = dropShadow;
}

// Glow Effect (Snap highlight)
public void ApplyGlowEffect(UIElement element)
{
    var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
    
    var glowEffect = new ColorSourceEffect
    {
        Color = Windows.UI.Colors.Cyan
    };
    
    var factory = compositor.CreateEffectFactory(glowEffect);
    var brush = factory.CreateBrush();
    
    // Layer with transparency
}
```

---

#### 2. **Animationen & Transitions**

```csharp
using Windows.UI.Xaml.Media.Animation;

// Ghost Track Fade-in During Drag
public void AnimateGhostAppearance()
{
    var storyboard = new Storyboard();
    
    var opacityAnimation = new DoubleAnimation
    {
        From = 0,
        To = 0.75,
        Duration = new Duration(TimeSpan.FromMilliseconds(200)),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    
    Storyboard.SetTarget(opacityAnimation, _ghostCanvas);
    Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
    storyboard.Children.Add(opacityAnimation);
    storyboard.Begin();
}

// Snap Completion Pulse
public void AnimateSnapPulse(Point center)
{
    var pulse = new Ellipse
    {
        Width = 10,
        Height = 10,
        Fill = new SolidColorBrush(Windows.UI.Colors.Green),
        Opacity = 0.8
    };
    
    Canvas.SetLeft(pulse, center.X - 5);
    Canvas.SetTop(pulse, center.Y - 5);
    GraphCanvas.Children.Add(pulse);
    
    var scaleAnimation = new DoubleAnimation
    {
        From = 1,
        To = 3,
        Duration = new Duration(TimeSpan.FromMilliseconds(600)),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
    
    var opacityAnimation = new DoubleAnimation
    {
        From = 0.8,
        To = 0,
        Duration = new Duration(TimeSpan.FromMilliseconds(600)),
        EasingFunction = new LinearEase()
    };
    
    var storyboard = new Storyboard();
    
    Storyboard.SetTarget(scaleAnimation, pulse);
    Storyboard.SetTargetProperty(scaleAnimation, "(RenderTransform).(ScaleTransform.ScaleX)");
    
    Storyboard.SetTarget(opacityAnimation, pulse);
    Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
    
    storyboard.Children.Add(scaleAnimation);
    storyboard.Children.Add(opacityAnimation);
    storyboard.Completed += (_, _) => GraphCanvas.Children.Remove(pulse);
    storyboard.Begin();
}

// Selection Glow Animation
public void AnimateSelectionGlow(UIElement element)
{
    var glowAnimation = new ColorAnimation
    {
        From = Colors.Transparent,
        To = Colors.Yellow,
        Duration = new Duration(TimeSpan.FromMilliseconds(400)),
        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
    };
    
    var storyboard = new Storyboard
    {
        RepeatBehavior = new RepeatBehavior(0.5)
    };
    
    Storyboard.SetTarget(glowAnimation, element);
    Storyboard.SetTargetProperty(glowAnimation, "(Foreground).(Color)");
    storyboard.Children.Add(glowAnimation);
    storyboard.Begin();
}
```

---

#### 3. **Visual Layer Effects (Advanced)**

```csharp
using Windows.UI.Composition;

// Parallax Effect for Grid
public void ApplyParallaxToGrid(UIElement grid, UIElement scrollViewer)
{
    var compositor = ElementCompositionPreview.GetElementVisual(grid).Compositor;
    var scrollerVisual = ElementCompositionPreview.GetElementVisual(scrollViewer);
    
    var parallaxExpression = compositor.CreateExpressionAnimation(
        "-scrollViewer.Translation.Y * 0.2");
    parallaxExpression.SetReferenceParameter("scrollViewer", scrollerVisual);
    
    var gridVisual = ElementCompositionPreview.GetElementVisual(grid);
    gridVisual.StartAnimation("Offset.Y", parallaxExpression);
}

// Scroll Snap Animation
public void AnimateSnapToGrid()
{
    var compositor = ElementCompositionPreview.GetElementVisual(GraphCanvas).Compositor;
    
    var keyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
    keyFrameAnimation.InsertKeyFrame(0f, new System.Numerics.Vector3(0));
    keyFrameAnimation.InsertKeyFrame(1f, new System.Numerics.Vector3(10, 0, 0));
    keyFrameAnimation.Duration = TimeSpan.FromMilliseconds(200);
    
    var visual = ElementCompositionPreview.GetElementVisual(GraphCanvas);
    visual.StartAnimation("Offset", keyFrameAnimation);
}

// Depth Effect with Z-index
public void ApplyDepthLayers()
{
    var visual = ElementCompositionPreview.GetElementVisual(GraphCanvas);
    
    // Tracks behind
    visual.Offset = new System.Numerics.Vector3(0, 0, -100);
    
    // Ghost in middle
    // SnapPreview in front
    // Ports highest
}
```

---

### B. Empfohlene Effekte für TrackPlanPage

| Feature | Effekt | Implementierung |
|---------|--------|-----------------|
| **Ghost Track** | Fade-in + Blur | DoubleAnimation + GaussianBlur |
| **Snap Preview** | Glow + Pulse | DropShadow + ScaleAnimation |
| **Selected Track** | Highlight + Border Glow | ColorAnimation + CompositionEffect |
| **Grid** | Subtle Parallax | ExpressionAnimation |
| **Port Connection** | Spring Animation | KeyFrameAnimation + EasingFunction |
| **Drag Start** | Cursor Hide + Fade | null + Opacity Animation |

---

### C. Performance Considerations

| Effekt | GPU Impact | Empfehlung |
|--------|-----------|------------|
| Blur | Hoch | Limit auf Ghost nur |
| DropShadow | Mittel | OK für einzelne Elemente |
| KeyFrame Animations | Niedrig | Unbegrenzt möglich |
| Parallax | Mittel | Nur für statische Elemente |
| Composition Effects | Mittel | Verwende sparingly |

---

## 4. Implementierungs-Roadmap für MOBAflow

### Phase 1: Design Token System (Next Session)
```
1. Erstelle IDesignSystemProvider Interface
2. Implementiere DesignTokens Record mit Track-spezifischen Farben
3. Integriere in TrackPlanPage.UpdateTheme()
4. Settings UI für Design System Auswahl
```

### Phase 2: Composition Effects (Session nach nächster)
```
1. Ghost Track: GaussianBlur + Fade Animation
2. Snap Preview: DropShadow + Pulse Animation
3. Selected Track: Glow Effect mit ColorAnimation
```

### Phase 3: Material Design 3 Alternative (Optional)
```
1. NuGet: Material.WinUI.3
2. Erstelle Material3DesignSystem Klasse
3. Toggle in Settings zwischen Fluent/Material
```

---

## Fazit

**Für MOBAflow empfohlen:**

1. **Kurz-Fristig:** Erweitern des existierenden ISkinProvider-Systems mit IDesignSystemProvider
2. **Mittelfristig:** Composition Effects für Snap-Feedback + Ghost-Animation
3. **Langfristig:** Optional Material Design 3 als Alternative (wenn Multi-Platform nötig)

**Hauptvorteil:** Alle Features können PARALLEL zur aktuellen Fluent Design Integration arbeiten, ohne Breaking Changes!

