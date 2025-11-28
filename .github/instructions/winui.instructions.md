---
description: WinUI 3 specific patterns for desktop development with DispatcherQueue and navigation
applyTo: "WinUI/**/*.cs"
---

# WinUI 3 Development Guidelines

## 🎯 WinUI-Specific Patterns

### UI Thread Dispatching

```csharp
// ✅ CORRECT: DispatcherQueue for UI updates
public class WinUIJourneyViewModel : JourneyViewModel
{
    private readonly DispatcherQueue _dispatcher;
    
    public WinUIJourneyViewModel(Journey model, DispatcherQueue dispatcher)
        : base(model)
    {
        _dispatcher = dispatcher;
        model.PropertyChanged += OnModelPropertyChanged;
    }
    
    private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            OnPropertyChanged(e.PropertyName);
        });
    }
}

// ❌ WRONG: Direct property updates from background thread
private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    OnPropertyChanged(e.PropertyName); // May crash!
}
```

### Getting DispatcherQueue

```csharp
// ✅ In MainWindow or Page code-behind
var dispatcher = this.DispatcherQueue;

// ✅ From any UI element
var dispatcher = button.DispatcherQueue;

// ✅ Via DI (register in App.xaml.cs)
services.AddSingleton(sp => 
{
    var mainWindow = sp.GetRequiredService<MainWindow>();
    return mainWindow.DispatcherQueue;
});
```

### Navigation Patterns

```csharp
// ✅ Frame navigation
Frame.Navigate(typeof(DetailsPage), journey);

// ✅ NavigationView
private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    var item = args.InvokedItemContainer.Tag?.ToString();
    switch (item)
    {
        case "Journeys":
            ContentFrame.Navigate(typeof(JourneysPage));
            break;
        case "Trains":
            ContentFrame.Navigate(typeof(TrainsPage));
            break;
    }
}
```

## 🎨 WinUI 3 XAML Patterns

### Resource Dictionaries

```xaml
<!-- ✅ Merge global styles -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            <ResourceDictionary Source="Styles/Colors.xaml" />
            <ResourceDictionary Source="Styles/Styles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Fluent Design System

```xaml
<!-- ✅ Use Acrylic for modern look -->
<Grid Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}">

<!-- ✅ NavigationView with Fluent styling -->
<NavigationView PaneDisplayMode="Left" IsBackButtonVisible="Collapsed">
    <NavigationView.MenuItems>
        <NavigationViewItem Icon="Home" Content="Dashboard" Tag="Dashboard" />
        <NavigationViewItem Icon="List" Content="Journeys" Tag="Journeys" />
    </NavigationView.MenuItems>
</NavigationView>

<!-- ✅ Use Segoe MDL2 Assets icons -->
<FontIcon Glyph="&#xE8B7;" /> <!-- Train icon -->
<FontIcon Glyph="&#xE74E;" /> <!-- Save icon -->
```

### Responsive Layout

```xaml
<!-- ✅ Use VisualStateManager for adaptive layout -->
<Grid>
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup>
            <VisualState x:Name="WideState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="720" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <Setter Target="ContentGrid.ColumnDefinitions" Value="300,*" />
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="NarrowState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="0" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <Setter Target="ContentGrid.ColumnDefinitions" Value="*" />
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
</Grid>
```

## 🪟 Window Management

```csharp
// ✅ Proper window initialization
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set title
        Title = "MOBAflow - Railway Automation";
        
        // Set window size
        this.AppWindow.Resize(new SizeInt32(1200, 800));
        
        // Center window
        CenterWindow();
    }
    
    private void CenterWindow()
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var centeredPosition = new PointInt32(
            (workArea.Width - AppWindow.Size.Width) / 2,
            (workArea.Height - AppWindow.Size.Height) / 2
        );
        AppWindow.Move(centeredPosition);
    }
}
```

## 🔧 DI Registration

```csharp
// App.xaml.cs
public App()
{
    InitializeComponent();
    
    var services = new ServiceCollection();
    
    // Platform services
    services.AddSingleton<MainWindow>();
    services.AddSingleton<DispatcherQueue>(sp =>
    {
        var window = sp.GetRequiredService<MainWindow>();
        return window.DispatcherQueue;
    });
    
    // Backend services
    services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
    services.AddSingleton<IZ21, Z21>();
    services.AddSingleton<Backend.Model.Solution>();
    
    // ViewModels
    services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();
    services.AddTransient<MainWindowViewModel>();
    
    ServiceProvider = services.BuildServiceProvider();
}
```

## 🎯 File I/O with Pickers

```csharp
// ✅ File save picker
var savePicker = new FileSavePicker();
WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);
savePicker.SuggestedFileName = "journey";
savePicker.FileTypeChoices.Add("JSON Files", new[] { ".json" });

var file = await savePicker.PickSaveFileAsync();
if (file != null)
{
    await FileIO.WriteTextAsync(file, jsonContent);
}

// ✅ File open picker
var openPicker = new FileOpenPicker();
WinRT.Interop.InitializeWithWindow.Initialize(openPicker, windowHandle);
openPicker.FileTypeFilter.Add(".json");

var file = await openPicker.PickSingleFileAsync();
if (file != null)
{
    var content = await FileIO.ReadTextAsync(file);
}
```

## 📋 Checklist

When modifying WinUI code:

- [ ] UI updates use `DispatcherQueue.TryEnqueue()`
- [ ] DispatcherQueue obtained from UI element or DI
- [ ] Navigation via Frame or NavigationView
- [ ] Use Fluent Design System (Acrylic, Icons)
- [ ] Responsive layout with VisualStateManager
- [ ] File pickers initialized with window handle
- [ ] Window properly configured (size, title)
- [ ] Factory pattern for ViewModels (DI)
- [ ] Keyboard shortcuts implemented (Ctrl+S, etc.)

## 🗂️ File Organization

```
WinUI/
├── Views/                      ← Pages and UserControls
│   ├── MainWindow.xaml
│   ├── JourneysPage.xaml
│   └── TrainsPage.xaml
├── ViewModels/                 ← Platform-specific ViewModels
│   ├── MainWindowViewModel.cs
│   └── Journey/
│       └── JourneyViewModel.cs
├── Factory/                    ← ViewModel factories
│   ├── WinUIJourneyViewModelFactory.cs
│   └── ...
├── Service/                    ← WinUI-specific services
│   ├── IoService.cs
│   └── UiDispatcher.cs
├── Styles/                     ← XAML resources
│   ├── Colors.xaml
│   └── Styles.xaml
└── App.xaml.cs                 ← DI registration
```

## 🎨 Keyboard Shortcuts

```xaml
<!-- ✅ Add keyboard accelerators -->
<Button Content="Save">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="S" Modifiers="Control" />
    </Button.KeyboardAccelerators>
</Button>

<!-- ✅ Access keys (Alt+S) -->
<Button Content="_Save" AccessKey="S" />
```

## 💡 Performance Tips

```csharp
// ✅ Virtualize large lists
<ListView ItemsSource="{x:Bind Journeys}">
    <ListView.ItemsPanel>
        <ItemsPanelTemplate>
            <ItemsStackPanel /> <!-- Virtualized by default -->
        </ItemsPanelTemplate>
    </ListView.ItemsPanel>
</ListView>

// ✅ Use x:Bind instead of Binding (compiled, faster)
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />

// ❌ Avoid
<TextBlock Text="{Binding Title}" /> <!-- Runtime, slower -->
```
