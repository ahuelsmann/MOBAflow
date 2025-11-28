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
```
