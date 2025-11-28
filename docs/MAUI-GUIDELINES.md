# MAUI Development Guidelines

## 🎨 Resource Loading Pattern

**⚠️ CRITICAL:** MAUI + .NET 10 + UraniumUI have resource loading timing issues.

### Rule 1: Use DynamicResource for Colors and Styles

```xaml
<!-- ✅ CORRECT: Runtime resolution -->
<ContentPage BackgroundColor="{DynamicResource SurfaceBackground}">
<Label Style="{DynamicResource HeadlineMedium}" />

<!-- ❌ WRONG: Compile-time resolution (may fail with late-loaded resources) -->
<ContentPage BackgroundColor="{StaticResource SurfaceBackground}">
```

**When to use which:**
- ✅ `DynamicResource`: Colors, Styles, Theme-dependent values
- ✅ `StaticResource`: Constant values (strings, numbers), converters

### Rule 2: Define Critical Resources Inline in App.xaml

```xaml
<Application.Resources>
  <ResourceDictionary>
    <!-- ⚠️ Define essential colors INLINE first (fallback) -->
    <Color x:Key="SurfaceBackground">#121212</Color>
    <Color x:Key="RailwayPrimary">#1976D2</Color>
    
    <!-- Then load external dictionaries (can override inline values) -->
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
      <ResourceDictionary Source="Resources/Styles/DarkTheme.xaml" />
      <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### Rule 3: Load Resources Programmatically in App.xaml.cs

```csharp
public App(IServiceProvider services)
{
    _services = services;
    
    // ⚠️ CRITICAL: Load BEFORE InitializeComponent!
    LoadEssentialResources();
    
    InitializeComponent();
}

private void LoadEssentialResources()
{
    var resources = Resources ?? new ResourceDictionary();
    
    // Surface colors
    resources["SurfaceBackground"] = Color.FromArgb("#121212");
    resources["SurfaceCard"] = Color.FromArgb("#2D2D30");
    
    // Railway colors
    resources["RailwayPrimary"] = Color.FromArgb("#1976D2");
    resources["RailwayAccent"] = Color.FromArgb("#00C853");
    
    Resources = resources;
}

protected override Window CreateWindow(IActivationState? activationState)
{
    // ✅ Create MainPage AFTER resources are loaded (lazy)
    var mainPage = _services.GetRequiredService<MainPage>();
    return new Window(mainPage);
}
```

**Why this works:**
1. Resources loaded **before** XAML initialization
2. MainPage created **after** App constructor completes
3. DynamicResource resolves at runtime (not compile-time)

---

## 📱 Platform-Specific Best Practices

### Threading

```csharp
// ✅ CORRECT: Dispatch to Main Thread
private async void OnZ21Event(object sender, EventArgs e)
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        SomeProperty = newValue;
        OnPropertyChanged(nameof(SomeProperty));
    });
}

// ❌ WRONG: Direct UI update from background thread
private void OnZ21Event(object sender, EventArgs e)
{
    SomeProperty = newValue; // Crashes!
}
```

### Dependency Injection

```csharp
// ✅ CORRECT: Register in MauiProgram.cs
builder.Services.AddSingleton<IUiDispatcher, MAUI.Service.UiDispatcher>();
builder.Services.AddTransient<MainPage>();

// ✅ CORRECT: Lazy MainPage injection in App
public App(IServiceProvider services)
{
    _services = services;
    LoadEssentialResources();
    InitializeComponent();
}

protected override Window CreateWindow(IActivationState? activationState)
{
    var mainPage = _services.GetRequiredService<MainPage>();
    return new Window(mainPage);
}
```

---

## 🎯 Common Pitfalls

### ❌ MainPage Constructor Injection

```csharp
// ❌ WRONG: MainPage created before App resources are ready
public App(MainPage mainPage)
{
    InitializeComponent(); // Too late! MainPage already exists
    _mainPage = mainPage;
}
```

### ❌ StaticResource for Theme Colors

```xaml
<!-- ❌ WRONG: May fail if resources not loaded yet -->
<ContentPage BackgroundColor="{StaticResource SurfaceBackground}">
```

### ❌ No Inline Fallback

```xaml
<!-- ❌ WRONG: No fallback if external dictionaries fail to load -->
<Application.Resources>
  <ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
  </ResourceDictionary.MergedDictionaries>
</Application.Resources>
```

---

## 📚 See Also

- `docs/MAUI-RESOURCE-LOADING-FIX.md` - Complete troubleshooting guide
- `docs/DI-INSTRUCTIONS.md` - Dependency Injection patterns
- `docs/THREADING.md` - Thread dispatching across platforms
- `.github/copilot-instructions.md` - General project guidelines
