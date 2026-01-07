---
description: MAUI-specific patterns for threading, navigation, and platform features
applyTo: "MAUI/**/*.cs, MAUI/**/*.xaml"
---

# MAUI Development Guidelines

## Critical Rules (NEVER violate!)

- NEVER use ListView (deprecated). Use CollectionView.
- NEVER use TableView (deprecated). Prefer CollectionView or Grid/VerticalStackLayout.
- NEVER use Frame (deprecated). Use Border instead.
- NEVER use `*AndExpand` layout options (deprecated). Use Grid and explicit sizing.
- NEVER place ScrollView or CollectionView inside StackLayout (breaks scrolling). Use Grid as parent.
- NEVER reference images as `.svg` at runtime. Use PNG/JPG resources.
- NEVER mix Shell navigation with NavigationPage/TabbedPage/FlyoutPage.
- NEVER use renderers. Use handlers.
- NEVER set `BackgroundColor`; use `Background` (supports gradients/brushes).

## Layout Selection

- Prefer `VerticalStackLayout`/`HorizontalStackLayout` over `StackLayout Orientation="..."` (more performant).
- Use `BindableLayout` for small, non-scrollable lists (20 items or less). Use `CollectionView` for larger lists.
- Prefer `Grid` for complex layouts.
- Prefer `Border` over `Frame` for containers with borders/backgrounds.

## MAUI-Specific Patterns

### UI Thread Dispatching

```csharp
// CORRECT: MainThread for UI updates
private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        OnPropertyChanged(nameof(DisplayName));
    });
}

// WRONG: No await or background thread issues
private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(nameof(DisplayName)); // May crash if called from background thread
}
```

### Resource Paths

```csharp
// CORRECT: MAUI resource paths
public const string STYLES_PATH = "MAUI/Resources/Styles/Styles.xaml";
public const string COLORS_PATH = "MAUI/Resources/Styles/Colors.xaml";
public const string IMAGES_PATH = "MAUI/Resources/Images/";
public const string FONTS_PATH = "MAUI/Resources/Fonts/";
```

### Platform-Specific Code

```csharp
// CORRECT: Conditional compilation for Android
#if ANDROID
using Android.App;
using AndroidX.Core.App;

public void ShowNotification()
{
    var builder = new NotificationCompat.Builder(Platform.AppContext, CHANNEL_ID);
}
#endif
```

### Navigation Patterns

```csharp
// CORRECT: Shell navigation
await Shell.Current.GoToAsync("//MainPage");
await Shell.Current.GoToAsync($"details?id={journeyId}");

// Query parameters in target page
[QueryProperty(nameof(JourneyId), "id")]
public partial class DetailsPage : ContentPage
{
    public string JourneyId { get; set; }
}
```

## XAML Styling

### Dynamic Resources

```xaml
<!-- Use DynamicResource for theme support -->
<Label TextColor="{DynamicResource White}" 
       BackgroundColor="{DynamicResource Gray600}" />

<!-- Avoid static colors -->
<Label TextColor="White" BackgroundColor="#404040" />
```

### Control Styling

```xaml
<!-- Remove default borders with VisualStateManager -->
<Style TargetType="Entry">
    <Setter Property="BackgroundColor" Value="{DynamicResource Gray600}" />
    <Setter Property="VisualStateManager.VisualStateGroups">
        <VisualStateGroupList>
            <VisualStateGroup x:Name="CommonStates">
                <VisualState x:Name="Normal">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{DynamicResource Gray600}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Focused">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{DynamicResource Gray600}" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateGroupList>
    </Setter>
</Style>
```

### Compact Control Spacing

**CheckBox + Label Pattern (Negative Margin Technique)**

When using `Scale` to reduce control size, use negative margin to maintain visual compactness:

```xaml
<!-- CORRECT: Compact CheckBox with tight label spacing -->
<HorizontalStackLayout Spacing="6">
    <CheckBox
        IsChecked="{Binding UseTimerFilter}"
        Scale="0.90"
        VerticalOptions="Center" />
    <Label
        FontSize="12"
        Text="Timer in s" 
        Margin="-8,0,0,0"
        TextColor="{DynamicResource TextSecondary}"
        VerticalOptions="Center" />
</HorizontalStackLayout>
```

**Why this works:**
- `Scale="0.90"` shrinks visual size but preserves hit box (good for touch)
- `Spacing="6"` would be too large due to invisible CheckBox padding
- `Margin="-8,0,0,0"` pulls label closer for visual balance
- Result: Compact appearance + functional touch target

**Scale Guidelines for Mobile:**
- `Scale="1.0"` - Default (often too large for compact UI)
- `Scale="0.90"` - CheckBox (recommended)
- `Scale="0.75"` - Switch controls (recommended)
- Never go below `0.7` (accessibility issues)

### Stepper Control Pattern

Standard pattern for increment/decrement controls:

```xaml
<HorizontalStackLayout Spacing="6">
    <!-- Decrement Button -->
    <Button
        Padding="8,4"
        BackgroundColor="{DynamicResource SurfaceDark}"
        Command="{Binding DecrementCommand}"
        CornerRadius="4"
        FontSize="16"
        Text="-"
        TextColor="{DynamicResource TextPrimary}"
        WidthRequest="36" />
    
    <!-- Value Display Border -->
    <Border
        Padding="10,4"
        BackgroundColor="{DynamicResource SurfaceDark}"
        StrokeShape="RoundRectangle 4"
        StrokeThickness="0"
        WidthRequest="44">
        <Label
            FontAttributes="Bold"
            FontSize="15"
            HorizontalOptions="Center"
            Text="{Binding Count}"
            TextColor="{DynamicResource TextPrimary}"
            VerticalOptions="Center" />
    </Border>
    
    <!-- Increment Button -->
    <Button
        Padding="8,4"
        BackgroundColor="{DynamicResource SurfaceDark}"
        Command="{Binding IncrementCommand}"
        CornerRadius="4"
        FontSize="16"
        Text="+"
        TextColor="{DynamicResource TextPrimary}"
        WidthRequest="36" />
</HorizontalStackLayout>
```

**Width Guidelines:**
- Buttons: `WidthRequest="36"` (square touch target)
- Value display:
  - `WidthRequest="44"` - Single/double digit integers (e.g., "3", "10")
  - `WidthRequest="70"` - Decimal values (e.g., "2.5")

## Touch and Mobile UX

### Touch Targets

```xaml
<!-- Minimum 44x44 dp for touch targets -->
<Button WidthRequest="44" HeightRequest="44" />

<!-- Add padding for comfortable tapping -->
<Button Padding="16,12" Text="Save" />
```

### Responsive Layout

```xaml
<!-- Use Grid with flexible sizing -->
<Grid ColumnDefinitions="*,Auto,*" RowDefinitions="Auto,*,Auto">
    <!-- Content adapts to screen size -->
</Grid>

<!-- Avoid fixed widths - breaks on small screens -->
