---
description: MAUI-specific patterns for threading, navigation, and platform features
applyTo: "MAUI/**/*.cs"
---

# MAUI Development Guidelines

## 🎯 MAUI-Specific Patterns

### UI Thread Dispatching

```csharp
// ✅ CORRECT: MainThread for UI updates
private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        // Update UI properties here
        OnPropertyChanged(nameof(DisplayName));
    });
}

// ❌ WRONG: No await or background thread issues
private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(nameof(DisplayName)); // May crash if called from background thread
}
```

### Resource Paths

```csharp
// ✅ CORRECT: MAUI resource paths
public const string STYLES_PATH = "MAUI/Resources/Styles/Styles.xaml";
public const string COLORS_PATH = "MAUI/Resources/Styles/Colors.xaml";
public const string IMAGES_PATH = "MAUI/Resources/Images/";
public const string FONTS_PATH = "MAUI/Resources/Fonts/";
```

### Platform-Specific Code

```csharp
// ✅ CORRECT: Conditional compilation for Android
#if ANDROID
using Android.App;
using AndroidX.Core.App;

public void ShowNotification()
{
    var builder = new NotificationCompat.Builder(Platform.AppContext, CHANNEL_ID);
    // Android-specific notification code
}
#endif
```

### Navigation Patterns

```csharp
// ✅ CORRECT: Shell navigation
await Shell.Current.GoToAsync("//MainPage");
await Shell.Current.GoToAsync($"details?id={journeyId}");

// Query parameters in target page
[QueryProperty(nameof(JourneyId), "id")]
public partial class DetailsPage : ContentPage
{
    public string JourneyId { get; set; }
}
```

## 🎨 XAML Styling

### Dynamic Resources

```xaml
<!-- ✅ Use DynamicResource for theme support -->
<Label TextColor="{DynamicResource White}" 
       BackgroundColor="{DynamicResource Gray600}" />

<!-- ❌ Avoid static colors -->
<Label TextColor="White" BackgroundColor="#404040" />
```

### Control Styling

```xaml
<!-- ✅ Remove default borders with VisualStateManager -->
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
<!-- ✅ CORRECT: Compact CheckBox with tight label spacing -->
<HorizontalStackLayout Spacing="6">
    <CheckBox
        IsChecked="{Binding UseTimerFilter}"
        Scale="0.90"
        VerticalOptions="Center" />
    <Label
        FontSize="12"
        Text="Timer in s" 
        Margin="-8,0,0,0"    <!-- Negative margin compensates for invisible padding -->
        TextColor="{DynamicResource TextSecondary}"
        VerticalOptions="Center" />
</HorizontalStackLayout>
```

**Why this works:**
- `Scale="0.90"` shrinks visual size but preserves hit box (good for touch)
- `Spacing="6"` would be too large due to invisible CheckBox padding
- `Margin="-8,0,0,0"` pulls label closer for visual balance
- Result: Compact appearance + functional touch target

**Alternatives to avoid:**
```xaml
<!-- ❌ Less precise: Removing all spacing -->
<HorizontalStackLayout Spacing="0">
    <CheckBox Scale="0.90" />
    <Label Margin="2,0,0,0" />  <!-- Less control than negative margin -->
</HorizontalStackLayout>

<!-- ❌ Overkill: Using Grid for 2 elements -->
<Grid ColumnDefinitions="Auto,Auto">
    <CheckBox Grid.Column="0" />
    <Label Grid.Column="1" Margin="-8,0,0,0" />
</Grid>
```
**Scale Guidelines for Mobile:**
- `Scale="1.0"` - Default (often too large for compact UI)
- `Scale="0.90"` - CheckBox (recommended)
- `Scale="0.75"` - Switch controls (recommended)
- Never go below `0.7` (accessibility issues)

### Stepper Control Pattern (±Buttons)

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
        Text="−"
        TextColor="{DynamicResource TextPrimary}"
        WidthRequest="36" />
    
    <!-- Value Display Border -->
    <Border
        Padding="10,4"
        BackgroundColor="{DynamicResource SurfaceDark}"
        StrokeShape="RoundRectangle 4"
        StrokeThickness="0"
        WidthRequest="44">  <!-- Adjust width based on content -->
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
  
**Example from MOBAsmart:**
```xaml
<!-- Timer interval with decimal display -->
<Border WidthRequest="70">
    <Label Text="{Binding TimerIntervalSeconds, StringFormat='{0:F1}'}" />
</Border>
```

## 📱 Touch & Mobile UX

### Touch Targets

```xaml
<!-- ✅ Minimum 44x44 dp for touch targets -->
<Button WidthRequest="44" HeightRequest="44" />

<!-- ✅ Add padding for comfortable tapping -->
<Button Padding="16,12" Text="Save" />
```

### Responsive Layout

```xaml
<!-- ✅ Use Grid with flexible sizing -->
<Grid ColumnDefinitions="*,Auto,*" RowDefinitions="Auto,*,Auto">
    <!-- Content adapts to screen size -->
</Grid>

<!-- ❌ Avoid fixed widths -->
<StackPanel Width="400"> <!-- Breaks on small screens -->
```

## 🔧 DI Registration

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IUiDispatcher, MauiDispatcher>();
builder.Services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
builder.Services.AddSingleton<IZ21, Z21>();
builder.Services.AddSingleton<Backend.Model.Solution>();
builder.Services.AddSingleton<IJourneyViewModelFactory, MauiJourneyViewModelFactory>();
builder.Services.AddTransient<MainPage>();
```

## 📋 Checklist

When modifying MAUI code:

- [ ] UI updates use `MainThread.InvokeOnMainThreadAsync()`
- [ ] Touch targets ≥44x44 dp
- [ ] Use `DynamicResource` for colors/styles
- [ ] Navigation via Shell
- [ ] Platform-specific code in `#if ANDROID` blocks
- [ ] Resources in `MAUI/Resources/` subdirectories
- [ ] Factory pattern for ViewModels (DI)
- [ ] Test on different screen sizes

## 🗂️ File Organization

```
MAUI/
├── Resources/
│   ├── Styles/
│   │   ├── Styles.xaml        ← Global styles
│   │   ├── Colors.xaml        ← Color definitions
│   │   ├── DarkTheme.xaml
│   │   └── LightTheme.xaml
│   ├── Images/
│   └── Fonts/
├── Factory/                    ← ViewModel factories
├── Service/                    ← Platform-specific services
│   ├── UiDispatcher.cs
│   └── NotificationService.cs
├── Platforms/
│   └── Android/                ← Android-specific code
├── MauiProgram.cs              ← DI registration
└── App.xaml                    ← Application entry point
