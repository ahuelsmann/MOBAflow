---
description: WinUI 3 specific patterns for desktop development with DispatcherQueue, navigation, and responsive layout
applyTo: "WinUI/**/*.xaml,WinUI/**/*.cs"
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

---

## 📱 Responsive Layout with VisualStateManager (CRITICAL FOR MOBAflow)

### What is VisualStateManager?

**VisualStateManager (VSM)** is a WinUI feature that **automatically switches between different UI layouts** based on **window/screen size**. This makes your application responsive across desktop, tablet, and touch scenarios.

### Why VSM? (Architecture Decision)

| Issue | Without VSM | With VSM |
|-------|------------|----------|
| **Hardcoded Layout** | UI breaks when user resizes window | Adapts automatically |
| **Tablet/Touch** | Single-column only | Optimize per screen size |
| **Maintainability** | Multiple XAML files needed | One file, multiple states |
| **Performance** | Custom code in code-behind | Native WinUI optimization |

### MOBAflow Responsive Breakpoints

MOBAflow targets three responsive states based on **window width**:

| State | Width | Use Case | Layout Strategy |
|-------|-------|----------|-----------------|
| **Compact** | 0-640px | Mobile/Tablet Portrait | Single column, hide secondary panels |
| **Medium** | 641-1199px | Tablet Landscape | 2-column, vertical stacking where needed |
| **Wide** | 1200px+ | Desktop | Full multi-column layout (3-4 columns) |

### Basic VSM Pattern

```xaml
<!-- ✅ TEMPLATE: Use this pattern for all responsive Pages -->
<Page>
    <Grid x:Name="RootGrid">
        <!-- Define VisualStateGroups FIRST -->
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="AdaptiveStates">
                
                <!-- STATE 1: Wide (Desktop with plenty of space) -->
                <VisualState x:Name="WideState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1200" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Apply Wide-specific layout changes -->
                        <Setter Target="ContentGrid.ColumnDefinitions" Value="300,*,150" />
                        <Setter Target="SidebarPanel.Visibility" Value="Visible" />
                        <Setter Target="ToolsPanel.Width" Value="200" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- STATE 2: Medium (Tablet or resized window) -->
                <VisualState x:Name="MediumState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="641" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Apply Medium-specific layout changes -->
                        <Setter Target="ContentGrid.ColumnDefinitions" Value="*" />
                        <Setter Target="SidebarPanel.Visibility" Value="Collapsed" />
                        <Setter Target="ToolsPanel.Width" Value="*" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- STATE 3: Compact (Mobile or very small window) -->
                <VisualState x:Name="CompactState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="0" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Apply Compact-specific layout changes -->
                        <Setter Target="ContentGrid.ColumnDefinitions" Value="*" />
                        <Setter Target="SidebarPanel.Visibility" Value="Collapsed" />
                        <Setter Target="ToolsPanel.Visibility" Value="Collapsed" />
                        <Setter Target="ToolsPanel.Margin" Value="0,10,0,0" />
                    </VisualState.Setters>
                </VisualState>
                
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        
        <!-- Your actual layout (same for all states) -->
        <Grid x:Name="ContentGrid" ColumnSpacing="10">
            <Grid.ColumnDefinitions>
                <!-- Values will be changed by VisualState.Setters -->
                <ColumnDefinition Width="300" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="150" />
            </Grid.ColumnDefinitions>
            
            <StackPanel x:Name="SidebarPanel" Grid.Column="0">
                <!-- Sidebar content (hidden in Medium/Compact) -->
            </StackPanel>
            
            <Grid Grid.Column="1">
                <!-- Main content -->
            </Grid>
            
            <StackPanel x:Name="ToolsPanel" Grid.Column="2" Width="200">
                <!-- Tools panel (hidden in Medium/Compact) -->
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

### Real-World Example: TrainControlPage with VSM

```xaml
<Page x:Class="Moba.WinUI.View.TrainControlPage">
    <Grid x:Name="TrainControlRoot">
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="TrainControlStates">
                
                <!-- WIDE: All 4 columns visible -->
                <VisualState x:Name="WideState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1200" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="ControlGrid.ColumnDefinitions" 
                                Value="260,*,100,200" />
                        <Setter Target="SettingsPanel.Visibility" Value="Visible" />
                        <Setter Target="SpeedPanel.Visibility" Value="Visible" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- MEDIUM: Settings + Tachometer only, Functions hidden -->
                <VisualState x:Name="MediumState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="641" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="ControlGrid.ColumnDefinitions" 
                                Value="250,*" />
                        <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
                        <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
                        <!-- Functions shown in TabView below Tachometer -->
                    </VisualState.Setters>
                </VisualState>
                
                <!-- COMPACT: Stacked, Settings as Expander -->
                <VisualState x:Name="CompactState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="0" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="ControlGrid.ColumnDefinitions" 
                                Value="*" />
                        <Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
                        <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
                        <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
                        <!-- All in expandable sections -->
                    </VisualState.Setters>
                </VisualState>
                
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        
        <Grid x:Name="ControlGrid" ColumnSpacing="10" Padding="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="260" /> <!-- Settings -->
                <ColumnDefinition Width="*" />   <!-- Tachometer -->
                <ColumnDefinition Width="100" /> <!-- Functions -->
                <ColumnDefinition Width="200" /> <!-- Speed -->
            </Grid.ColumnDefinitions>
            
            <!-- Column 0: Locomotive Settings -->
            <StackPanel x:Name="SettingsPanel" Grid.Column="0" Spacing="10">
                <TextBlock Text="Locomotive Settings" FontWeight="Bold" />
                <ComboBox Header="Locomotive" ItemsSource="{x:Bind ViewModel.Locomotives}" />
                <Slider Header="Address" />
                <!-- ... more settings -->
            </StackPanel>
            
            <!-- Column 1: Tachometer Display -->
            <Canvas x:Name="TachometerCanvas" Grid.Column="1" />
            
            <!-- Column 2: Function Buttons F0-F20 -->
            <StackPanel x:Name="FunctionsPanel" Grid.Column="2" Spacing="5">
                <TextBlock Text="Functions" FontWeight="Bold" />
                <Button Content="F0" />
                <Button Content="F1" />
                <!-- ... F2 through F20 -->
            </StackPanel>
            
            <!-- Column 3: Speed Presets -->
            <StackPanel x:Name="SpeedPanel" Grid.Column="3" Spacing="5">
                <TextBlock Text="Speed Presets" FontWeight="Bold" />
                <Button Content="Stop" />
                <Button Content="Slow" />
                <Button Content="Normal" />
                <Button Content="Fast" />
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

### How VisualStateManager Works (Under the Hood)

1. **Initialization:** When page loads, VSM checks current `ActualWidth`
2. **Trigger Matching:** Finds first matching `AdaptiveTrigger` (highest MinWindowWidth that fits)
3. **Apply Setters:** Sets values from matching `VisualState.Setters`
4. **Listen to Resize:** When window resizes, re-evaluates triggers automatically
5. **Smooth Transition:** Changes apply smoothly (not abruptly)

**No code-behind required!** VSM handles everything.

---

### When to Use Each State in MOBAflow

| Page | Wide (1200px+) | Medium (641-1199px) | Compact (0-640px) |
|------|----------------|-------------------|------------------|
| **TrainControlPage** | 4 columns visible | Settings hidden, Tabs for Functions | Full stack, Expanders |
| **TrackPlanEditorPage** | Canvas + Toolbox | Canvas full-width, Toolbox as CommandBar | Canvas full, Toolbox modal |
| **SignalBoxPage** | Grid + Log side-by-side | Grid full, Log below | Grid full-width |
| **WorkflowsPage** | List + Editor side-by-side | List full, Editor modal | List full-width, swipe to edit |
| **MainWindow** | 3-column (Rail + Content + Details) | 2-column (Rail + Content) | Hamburger menu |

### Common VSM Patterns for MOBAflow

#### Pattern 1: Hide/Show Panels

```xaml
<!-- ✅ Common: Hide sidebar in Compact/Medium -->
<Setter Target="SidebarPanel.Visibility" Value="Collapsed" />
```

#### Pattern 2: Change Column Widths

```xaml
<!-- ✅ Common: Adjust grid columns -->
<Setter Target="MainGrid.ColumnDefinitions" Value="300,*,150" />
<!-- After state change: 300px | auto-fill | 150px -->
```

#### Pattern 3: Stack Vertically (Remove Columns)

```xaml
<!-- ✅ Common: Single column layout -->
<Setter Target="MainGrid.ColumnDefinitions" Value="*" />
<!-- Single column, all controls stack vertically -->
```

#### Pattern 4: Show/Hide with Margin Adjustment

```xaml
<!-- ✅ Hide panel and remove spacing -->
<Setter Target="Panel.Visibility" Value="Collapsed" />
<Setter Target="Panel.Margin" Value="0" />
```

---

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
        
        // Set window size (important for testing responsive layouts!)
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
- [ ] **Responsive layout with VisualStateManager** (NEW - CRITICAL)
  - [ ] Three states defined: Wide (1200px+), Medium (641-1199px), Compact (0-640px)
  - [ ] Tested by resizing window
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
```

## 🧪 Testing Responsive Layouts

```csharp
// Test your VSM implementation:
// 1. Resize window from 1920px down to 320px
// 2. Verify each state activates at correct breakpoints
// 3. Verify all elements visible/hidden as expected
// 4. Check no layout overflow or clipping
// 5. Verify smooth transition (no jarring jumps)

// Window resize keyboard shortcut: Win+Left/Right Arrow (snap to half)
```

## 🚫 Anti-Patterns to Avoid

### **1. Priority Hierarchy in Computed Properties**

**Problem:** Computed properties with if-else chains create hidden dependencies and unexpected behavior.

```csharp
// ❌ WRONG: Hidden Priority Logic
public object? CurrentSelectedObject
{
    get
    {
        if (SelectedChild != null) return SelectedChild;    // Higher priority
        if (SelectedParent != null) return SelectedParent;  // Lower priority
        return null;
    }
}

// Result: Clicking Parent when Child is selected → Still shows Child!
```

**Why it's bad:**
- ❌ Hidden business logic in getter
- ❌ Violates Principle of Least Astonishment
- ❌ Hard to debug (no breakpoint in getter effective)
- ❌ Requires manual state clearing everywhere

**Solution:** Use `[ObservableProperty]` with explicit assignment in `OnChanged` handlers.

```csharp
// ✅ CORRECT: Explicit Assignment
[ObservableProperty]
private object? currentSelectedObject;

partial void OnSelectedParentChanged(ParentViewModel? value)
{
    if (value != null)
        CurrentSelectedObject = value;  // Explicit & Clear!
}
```

---

### **2. Manual State Clearing in Commands**

**Problem:** Requiring manual cleanup in every command creates boilerplate and error-prone code.

```csharp
// ❌ WRONG: Manual Clearing Everywhere
[RelayCommand]
private void SelectParent(ParentViewModel? parent)
{
    SelectedChild = null;      // Manual clearing
    SelectedGrandChild = null;  // Manual clearing
    SelectedParent = parent;
    OnPropertyChanged(nameof(CurrentView));
}

[RelayCommand]
private void SelectChild(ChildViewModel? child)
{
    SelectedGrandChild = null;  // Manual clearing
    SelectedChild = child;
    OnPropertyChanged(nameof(CurrentView));
}
```

**Why it's bad:**
- ❌ Boilerplate code in every command
- ❌ Easy to forget clearing (bugs!)
- ❌ Maintenance nightmare (add new property → update all commands)

**Solution:** Direct assignment automatically "replaces" previous selection.

```csharp
// ✅ CORRECT: No Manual Clearing Needed
[RelayCommand]
private void SelectParent(ParentViewModel? parent)
{
    SelectedParent = parent;  // That's it!
}

partial void OnSelectedParentChanged(ParentViewModel? value)
{
    if (value != null)
        CurrentView = value;  // Automatic replacement!
}
```

---

### **3. Callback-Heavy Selection Managers**

**Problem:** Over-engineering selection logic with callbacks and helper classes.

```csharp
// ❌ WRONG: Too Much Abstraction
_selectionManager.SelectEntity(
    entity,
    type,
    currentSelected,
    setter,
    onReselect: () => OnPropertyChanged(...),
    clearChildren: () => { /* manual clearing */ }
);
```

**Why it's bad:**
- ❌ Hidden logic in helper class
- ❌ Hard to understand control flow
- ❌ Overkill for simple property assignment

**Solution:** Keep it simple - direct property assignment.

```csharp
// ✅ CORRECT: Simple & Direct
[RelayCommand]
private void SelectEntity(EntityViewModel? entity)
{
    SelectedEntity = entity;
}
```

---

### **Pattern Recognition: Code Smells**

If your selection management has:
- ✋ **Manual clearing in every command** → Simplify to direct assignment
- ✋ **Complex computed properties** → Use `[ObservableProperty]` + OnChanged
- ✋ **Callback-heavy helpers** → Remove abstraction, use direct calls
- ✋ **Priority hierarchies** → Rethink: Why do you need this?

**Remember:** Simple is not simplistic. Simple is elegant. 🎯

---

## 🎛️ CommandBar Responsive Design

### Problem: CommandBar Overflow
WinUI 3 CommandBar **does not automatically** move buttons to overflow menu when window is resized. Buttons get cut off at smaller window sizes.

### Solution: Dynamic Overflow Priority

```xaml
<!-- ✅ CORRECT: Explicit overflow configuration -->
<CommandBar OverflowButtonVisibility="Auto" DefaultLabelPosition="Right">
    <!-- Priority 0 = Always visible (critical controls) -->
    <AppBarButton 
        Command="{x:Bind ViewModel.ConnectCommand}"
        CommandBar.DynamicOverflowOrder="0"
        Label="Connect" />
    
    <AppBarButton 
        Command="{x:Bind ViewModel.DisconnectCommand}"
        CommandBar.DynamicOverflowOrder="0"
        Label="Disconnect" />
    
    <!-- Priority 1 = High priority (frequent operations) -->
    <AppBarButton 
        Command="{x:Bind ViewModel.LoadCommand}"
        CommandBar.DynamicOverflowOrder="1"
        Label="Load" />
    
    <!-- Priority 2 = Medium priority -->
    <AppBarElementContainer CommandBar.DynamicOverflowOrder="2">
        <ToggleButton IsChecked="{x:Bind ViewModel.IsDarkMode, Mode=TwoWay}" />
    </AppBarElementContainer>
    
    <!-- Priority 4 = Lowest priority (developer tools) -->
    <AppBarButton 
        Command="{x:Bind ViewModel.SimulateCommand}"
        CommandBar.DynamicOverflowOrder="4"
        Label="Simulate" />
</CommandBar>
```

### Priority Strategy
- **Priority 0:** Critical controls that must always be visible (Connect, Disconnect, Track Power)
- **Priority 1:** Frequent operations (Load, Save)
- **Priority 2:** Settings/preferences (Theme Toggle)
- **Priority 3:** Infrequent operations (Add Project)
- **Priority 4:** Developer/debug tools (Simulate Feedback)

### Key Requirements
1. **Set `OverflowButtonVisibility="Auto"`** on CommandBar
2. **Set `CommandBar.DynamicOverflowOrder`** on **every** button/container
3. **Lower number = Higher priority** (stays visible longer)
4. **AppBarSeparator** does not support DynamicOverflowOrder (automatically hidden)

```csharp
// ❌ WRONG: No overflow configuration
<CommandBar>
    <AppBarButton Label="Connect" />  <!-- Will be cut off! -->
</CommandBar>

<!-- ❌ AUCH FALSCH: Workaround mit Disabled ScrollViewer -->
<ListView ScrollViewer.VerticalScrollBarVisibility="Disabled" />
<!-- Warum einen ScrollViewer deaktivieren? Nutze ItemsControl! -->
```

---

**Last Updated:** 2025-12-10  
**Related:** Selection Management Best Practices, Responsive Design

---

## 📐 DataTemplate Architecture (CRITICAL!)

### ✅ CORRECT: DataTemplates in EntityTemplates.xaml

**All Properties Panels MUST be DataTemplates in `WinUI/Resources/EntityTemplates.xaml`**

```xaml
<!-- EntityTemplates.xaml -->
<ResourceDictionary>
    <DataTemplate x:Key="LocomotiveTemplate">
        <ScrollViewer>
            <StackPanel Padding="16" Spacing="16">
                <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}" 
                           Text="Locomotive Properties" />
                
                <Border Padding="12"
                        Background="{ThemeResource CardBackgroundFillColorSecondaryBrush}"
                        CornerRadius="8">
                    <StackPanel Spacing="8">
                        <TextBox Header="Name" Text="{Binding Name, Mode=TwoWay}" />
                    </StackPanel>
                </Border>
            </StackPanel>
        </ScrollViewer>
    </DataTemplate>
</ResourceDictionary>
```

### ❌ WRONG: Separate UserControl Files

**NEVER create separate .xaml UserControl files for Properties Panels!**

```xaml
<!-- ❌ DO NOT CREATE: LocomotivePropertiesPanel.xaml -->
<UserControl x:Class="Moba.WinUI.View.LocomotivePropertiesPanel">
    <!-- This will NOT compile correctly in WinUI 3! -->
    <!-- InitializeComponent() will be missing! -->
</UserControl>
```

**Why DataTemplates instead of UserControls?**
- ✅ WinUI 3 XAML compiler generates code properly
- ✅ EntityTemplateSelector handles polymorphic ViewModels
- ✅ No `InitializeComponent()` issues
- ✅ Standard WinUI 3 pattern (see Journey/Workflow/Station templates)
- ❌ UserControls require `<Page>` root in WinUI 3 for complex scenarios

---

## 🔀 EntityTemplateSelector Pattern

```csharp
// WinUI/Selector/EntityTemplateSelector.cs
public class EntityTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LocomotiveTemplate { get; set; }
    public DataTemplate? WagonTemplate { get; set; }
    
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            LocomotiveViewModel => LocomotiveTemplate,
            PassengerWagonViewModel => WagonTemplate,
            GoodsWagonViewModel => WagonTemplate,
            _ => DefaultTemplate
        };
    }
}
```

**Usage in Page:**

```xaml
<ContentControl Content="{x:Bind ViewModel.SelectedObject, Mode=OneWay}"
                ContentTemplateSelector="{StaticResource EntityTemplateSelector}" />
```

---

## 📋 CalendarDatePicker + DateTimeOffsetConverter

```csharp
// WinUI/Converter/DateTimeOffsetConverter.cs
public class DateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTime dateTime)
            return new DateTimeOffset(dateTime);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.DateTime;
        return null;
    }
}
```

```xaml
<!-- Register in App.xaml -->
<converter:DateTimeOffsetConverter x:Key="DateTimeOffsetConverter" />

<!-- Use in XAML -->
<CalendarDatePicker Header="Invoice Date (optional)"
                    Date="{Binding InvoiceDate, Mode=TwoWay, Converter={StaticResource DateTimeOffsetConverter}}" />
```

---

## 🔢 NumberBox + NullableUIntConverter

```csharp
// WinUI/Converter/NullableUIntConverter.cs
public class NullableUIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is uint uintValue)
            return (double)uintValue;
        return double.NaN; // NumberBox displays empty when NaN
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is double doubleValue && !double.IsNaN(doubleValue))
            return (uint)doubleValue;
        return null;
    }
}
```

```xaml
<NumberBox Header="Digital Address (optional)"
           Maximum="10239"
           Minimum="1"
           Value="{Binding DigitalAddress, Mode=TwoWay, Converter={StaticResource NullableUIntConverter}}" />
```

---

## 📂 FileOpenPicker Pattern

### ❌ WRONG: Direct XAML Binding

```csharp
// ❌ FileOpenPicker CANNOT be triggered from XAML Command binding directly!
[RelayCommand]
private async Task BrowsePhotoAsync()
{
    var picker = new FileOpenPicker();
    var file = await picker.PickSingleFileAsync(); // FAILS: No window handle!
}
```

### ✅ CORRECT: InitializeWithWindow

```csharp
// ✅ Code-Behind Event Handler
private async void BrowsePhoto_Click(object sender, RoutedEventArgs e)
{
    var picker = new FileOpenPicker
    {
        SuggestedStartLocation = PickerLocationId.PicturesLibrary
    };
    picker.FileTypeFilter.Add(".jpg");
    
    // ✅ CRITICAL: Initialize with window handle
    var window = GetWindowForElement(this);
    InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
    
    var file = await picker.PickSingleFileAsync();
    if (file != null)
    {
        // Process file...
    }
}
```

---

## 📝 Properties Panel Checklist

Before creating ANY Properties Panel:

- [ ] ✅ Create DataTemplate in `EntityTemplates.xaml` (NOT separate .xaml file!)
- [ ] ✅ Register template in `EntityTemplateSelector`
- [ ] ✅ Use `ScrollViewer` as root element
- [ ] ✅ Use `StackPanel` with `Spacing="16"` for content
- [ ] ✅ Group properties in `Border` with `CardBackgroundFillColorSecondaryBrush`
- [ ] ✅ Use `FontWeight="SemiBold"` for section titles
- [ ] ✅ Register all new converters in `App.xaml`
- [ ] ✅ Use `CalendarDatePicker` for `DateTime?` properties
- [ ] ✅ Use `NumberBox` for numeric properties
- [ ] ❌ NEVER create separate UserControl `.xaml` files for Properties Panels

**Violation of these patterns = Immediate refactoring required!**

---

## 🔧 Custom Controls in WinUI 3 (CRITICAL!)

### Unterschiede zu WPF/UWP

WinUI 3 Custom Controls haben wichtige Unterschiede zu WPF:

| Feature | WPF | WinUI 3 |
|---------|-----|---------|
| **Implizite Styles** | Funktioniert | ❌ NICHT in ResourceDictionary! |
| **GridLength Konvertierung** | Automatisch | ❌ Manuelles Parsing erforderlich |
| **Resource-Zugriff** | Direkter Cast | TryGetValue() empfohlen |
| **TypeConverter** | Eingebaut | Nur fuer Framework-Typen |

### ❌ FALSCH: Impliziter Style fuer Custom Control

```xaml
<!-- ❌ FALSCH: Funktioniert NICHT in WinUI 3 -->
<ResourceDictionary xmlns:controls="using:MyApp.Controls">
    <Style TargetType="controls:MyCustomControl">
        <Setter Property="Width" Value="100" />
    </Style>
</ResourceDictionary>
```

**Warum:** Der XAML-Parser versucht den Typ aufzuloesen bevor das Assembly geladen ist.

### ✅ RICHTIG: Expliziter Style mit x:Key

```xaml
<!-- ✅ RICHTIG: Expliziter Key -->
<Style x:Key="MyCustomControlStyle" TargetType="Button">
    <Setter Property="Width" Value="100" />
</Style>

<!-- Oder: Properties direkt in XAML setzen -->
<controls:MyCustomControl Width="100" />
```

---

### ❌ FALSCH: GridLength als DependencyProperty-Typ

```csharp
// ❌ FALSCH: XAML kann GridLength nicht aus String parsen
public static readonly DependencyProperty WidthProperty =
    DependencyProperty.Register(nameof(Width), typeof(GridLength), ...);

public GridLength Width
{
    get => (GridLength)GetValue(WidthProperty);
    set => SetValue(WidthProperty, value);
}
```

### ✅ RICHTIG: String mit Parser-Methode

```csharp
// ✅ RICHTIG: String-Property mit Parser
public static readonly DependencyProperty WidthProperty =
    DependencyProperty.Register(nameof(Width), typeof(string), typeof(MyControl),
        new PropertyMetadata("*"));

public string Width
{
    get => (string)GetValue(WidthProperty);
    set => SetValue(WidthProperty, value);
}

public GridLength GetGridLength()
{
    var width = Width?.Trim() ?? "*";

    if (string.Equals(width, "Auto", StringComparison.OrdinalIgnoreCase))
        return GridLength.Auto;

    if (width == "*")
        return new GridLength(1, GridUnitType.Star);

    if (width.EndsWith('*'))
    {
        if (double.TryParse(width.AsSpan(0, width.Length - 1), out var starValue))
            return new GridLength(starValue, GridUnitType.Star);
        return new GridLength(1, GridUnitType.Star);
    }

    if (double.TryParse(width, out var pixelValue))
        return new GridLength(pixelValue, GridUnitType.Pixel);

    return new GridLength(1, GridUnitType.Star);
}
```

---

### ❌ FALSCH: Direkter Resource-Cast

```csharp
// ❌ FALSCH: Kann Exception werfen wenn App nicht vollstaendig geladen
var brush = (Brush)Application.Current.Resources["MyBrush"];
```

### ✅ RICHTIG: TryGetValue Pattern

```csharp
// ✅ RICHTIG: Sicherer Zugriff
if (Application.Current.Resources.TryGetValue("MyBrush", out var resource))
{
    myElement.Background = resource as Brush;
}
```

---

### Custom Control Checklist

- [ ] ❌ KEINE impliziten Styles in ResourceDictionary
- [ ] ✅ String statt GridLength fuer Width/Height Properties
- [ ] ✅ TryGetValue() fuer Resource-Zugriff
- [ ] ✅ Parser-Methoden fuer komplexe Typen (GetGridLength(), etc.)
- [ ] ✅ Null-Checks in Loaded-Event statt Konstruktor
- [ ] ✅ DI via Constructor Injection (nicht Service Locator)

---

## 📐 Responsive Layouts - VisualStateManager (Empfohlen)

### Warum VisualStateManager statt Custom Controls?

WinUI 3 hat erhebliche Einschraenkungen bei Custom Controls:
- ContentProperty mit ObservableCollection funktioniert nicht zuverlaessig
- Implizite Styles fuer Custom Controls crashen
- DependencyObject-basierte Items werden nicht korrekt geparsed

**Empfehlung:** Verwende den nativen VisualStateManager mit AdaptiveTrigger.

### Beispiel: Responsive Multi-Column Layout

```xaml
<Grid>
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup>
            <VisualState x:Name="WideState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="1024" />
                </VisualState.StateTriggers>
                <!-- Alle Panels sichtbar (Standardzustand) -->
            </VisualState>
            <VisualState x:Name="MediumState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="640" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <Setter Target="LibraryPanel.Visibility" Value="Collapsed" />
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="CompactState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="0" />
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <Setter Target="LibraryPanel.Visibility" Value="Collapsed" />
                    <Setter Target="PropertiesPanel.Visibility" Value="Collapsed" />
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
</Grid>
```

### Vorteile

- ✅ Native WinUI 3-Loesung
- ✅ Keine Custom Control-Probleme
- ✅ Bessere Performance
- ✅ Gut dokumentiert von Microsoft

### Verfuegbares Behavior

`WinUI/Behavior/ResponsiveLayoutBehavior.cs` kann fuer programmatische Layout-Anpassungen verwendet werden.

---

**Last Updated:** 2026-01-15  
**Related:** VisualStateManager, AdaptiveTrigger, ResponsiveLayoutBehavior


