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


## 🎨 DataTemplate Binding Rules (x:Bind vs Binding)

### **Decision Matrix:**

| Context | Binding Type | Performance | Reason |
|---------|--------------|-------------|--------|
| **Page/UserControl** | `{x:Bind}` ✅ | 10x faster | Compiled, Type-Safe |
| **Inline DataTemplate (in Page)** | `{x:Bind}` ✅ | 10x faster | With x:DataType attribute |
| **ResourceDictionary DataTemplate** | `{Binding}` ⚠️ | Slower | No Code-Behind available! |
| **Control Template** | `{TemplateBinding}` ✅ | Fast | Template-Specific |

---

### **Rule 1: Page/UserControl → x:Bind**

```xaml
<!-- ✅ CORRECT: Page with x:Bind -->
<Page x:Class="Moba.WinUI.View.EditorPage"
      xmlns:vm="using:Moba.SharedUI.ViewModel"
      DataContext="{x:Bind ViewModel}">
    
    <TextBox Header="Name" Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
    <CheckBox Content="Active" IsChecked="{x:Bind ViewModel.IsActive, Mode=TwoWay}" />
</Page>
```

**Why x:Bind?**
- ✅ Compile-time type checking (errors during build)
- ✅ 10x faster than `Binding` (no Reflection)
- ✅ `Mode=OneTime` is default (Performance!)
- ✅ IntelliSense support

---

### **Rule 2: Inline DataTemplate → x:Bind with x:DataType**

```xaml
<!-- ✅ CORRECT: Inline DataTemplate with x:DataType -->
<ListView ItemsSource="{x:Bind ViewModel.Stations}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="vm:StationViewModel">
            <StackPanel>
                <TextBlock Text="{x:Bind Name}" FontWeight="Bold" />
                <TextBlock Text="{x:Bind InPort}" Foreground="Gray" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Important:** `x:DataType` is **mandatory** for x:Bind in DataTemplate!

---

### **Rule 3: ResourceDictionary DataTemplate → Binding**

```xaml
<!-- ❌ WRONG: ResourceDictionary with x:Bind -->
<ResourceDictionary>
    <DataTemplate x:Key="StationTemplate">
        <TextBox Text="{x:Bind Name, Mode=TwoWay}" />  
        <!-- ❌ Compiler Error WMC1119: No code-behind! -->
    </DataTemplate>
</ResourceDictionary>

<!-- ✅ CORRECT: ResourceDictionary with Binding -->
<ResourceDictionary>
    <DataTemplate x:Key="StationTemplate">
        <StackPanel Padding="16" Spacing="16">
            <TextBox Header="Name" Text="{Binding Name, Mode=TwoWay}" />
            <NumberBox Header="InPort" Value="{Binding InPort, Mode=TwoWay}" />
        </StackPanel>
    </DataTemplate>
</ResourceDictionary>
```

**Why Binding?**
- ⚠️ ResourceDictionary files have **no Code-Behind**
- ⚠️ `x:Bind` requires Code-Behind at compile-time
- ✅ `Binding` is runtime binding (DataContext-based)

---

### **Compiler Error: WMC1119**

```
WMC1119: This Xaml file must have a code-behind class to use {x:Bind}
```

**Solution:**
1. **Option A:** Use Binding instead of x:Bind
2. **Option B:** Move DataTemplate from ResourceDictionary into Page/UserControl

---

### **Real-World Example: MOBAflow EntityTemplates.xaml**

```xaml
<!-- WinUI/Resources/EntityTemplates.xaml -->
<ResourceDictionary>
    <!-- ✅ CORRECT: Use Binding in ResourceDictionary -->
    <DataTemplate x:Key="JourneyTemplate">
        <ScrollViewer>
            <StackPanel Padding="16" Spacing="16">
                <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}" Text="Journey Properties" />
                <TextBox Header="Name" Text="{Binding Name, Mode=TwoWay}" />
                <NumberBox Header="InPort" Value="{Binding InPort, Mode=TwoWay}" SpinButtonPlacementMode="Inline" />
            </StackPanel>
        </ScrollViewer>
    </DataTemplate>
</ResourceDictionary>
```

**Usage in Page:**

```xaml
<!-- EditorPage.xaml -->
<Page x:Class="Moba.WinUI.View.EditorPage">
    <Page.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Resources/EntityTemplates.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Page.Resources>
    
    <!-- ContentControl applies template based on object type -->
    <ContentControl Content="{x:Bind ViewModel.CurrentSelectedObject, Mode=OneWay}"
                    ContentTemplateSelector="{StaticResource EntityTemplateSelector}" />
</Page>
```

---

### **Performance Comparison:**

| Binding Type | Resolution | Type Safety | Performance |
|--------------|------------|-------------|-------------|
| `{x:Bind}` | Compile-time | ✅ Yes | 🚀 10x |
| `{Binding}` | Runtime | ❌ No | ⚠️ 1x |
| `{TemplateBinding}` | Compile-time | ✅ Yes | 🚀 10x |

**Recommendation:**
- Use `x:Bind` **always** when possible (Page, UserControl, Inline Templates)
- Use `Binding` **only** in ResourceDictionary DataTemplates

---

**Last Updated:** 2025-12-09  
**Related Error:** WMC1119



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

// ✅ CORRECT: Explicit priority
<CommandBar OverflowButtonVisibility="Auto">
    <AppBarButton Label="Connect" CommandBar.DynamicOverflowOrder="0" />
</CommandBar>
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
