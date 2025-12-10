# XAML Behaviors Pattern - Event-to-Command (WinUI 3)

**Date:** 2025-12-09  
**Status:** ✅ Implemented & Verified  
**Package:** `Microsoft.Xaml.Behaviors.WinUI.Managed` v3.0.0

---

## Problem: Event Handlers in Code-Behind (Anti-Pattern)

### ❌ Old Pattern (Code-Behind)

```csharp
// EditorPage.xaml.cs
private void ListView_ItemClick(object sender, ItemClickEventArgs e)
{
    ViewModel.CurrentSelectedObject = e.ClickedItem;
}
```

**Problems:**
- ❌ Not MVVM-conform (logic in View)
- ❌ Hard to test
- ❌ Not reusable across views
- ❌ Mixed concerns (UI + Business Logic)

---

## Solution: XAML Behaviors (Event-to-Command)

### ✅ New Pattern (XAML + ViewModel)

**1. XAML (View Layer)**

```xml
<Page
    xmlns:i="using:Microsoft.Xaml.Interactivity"
    ...>
    
    <ListView x:Name="ProjectsListView"
              IsItemClickEnabled="True"
              ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
              SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}">
        
        <i:Interaction.Behaviors>
            <i:EventTriggerBehavior EventName="ItemClick">
                <i:InvokeCommandAction 
                    Command="{x:Bind ViewModel.ItemClickedCommand}"
                    CommandParameter="{Binding SelectedItem, ElementName=ProjectsListView}" />
            </i:EventTriggerBehavior>
        </i:Interaction.Behaviors>
        
        <!-- ItemTemplate ... -->
    </ListView>
</Page>
```

**2. ViewModel (Business Logic)**

```csharp
// SharedUI/ViewModel/MainWindowViewModel.Commands.cs
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Moba.SharedUI.ViewModel
{
    public partial class MainWindowViewModel
    {
        private ICommand? _itemClickedCommand;

        /// <summary>
        /// Command for handling ListView item clicks (all entity types).
        /// </summary>
        public ICommand ItemClickedCommand => _itemClickedCommand ??= new RelayCommand<object?>(item =>
        {
            // Set the selected item to display in properties panel
            CurrentSelectedObject = item;
        });
    }
}
```

---

## NuGet Package Configuration

### Version 3.0 (Current - Recommended)

**Package:** `Microsoft.Xaml.Behaviors.WinUI.Managed`  
**Version:** **3.0.0**

**Features:**
- ✅ Unified namespace: `Microsoft.Xaml.Interactivity`
- ✅ NativeAOT support
- ✅ .NET 8+ support
- ✅ Modern codebase (file-scoped namespaces)

**Directory.Packages.props:**
```xml
<PackageVersion Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="3.0.0" />
```

---

## Migration 2.x → 3.0

### Breaking Changes

**Namespace Unification:**

```xml
<!-- Version 2.x (OLD) -->
xmlns:i="using:Microsoft.Xaml.Interactivity"
xmlns:ic="using:Microsoft.Xaml.Interactions.Core"

<i:Interaction.Behaviors>
    <ic:EventTriggerBehavior EventName="ItemClick">
        <ic:InvokeCommandAction Command="..." />
    </ic:EventTriggerBehavior>
</i:Interaction.Behaviors>
```

```xml
<!-- Version 3.0 (NEW) -->
xmlns:i="using:Microsoft.Xaml.Interactivity"

<i:Interaction.Behaviors>
    <i:EventTriggerBehavior EventName="ItemClick">
        <i:InvokeCommandAction Command="..." />
    </i:EventTriggerBehavior>
</i:Interaction.Behaviors>
```

**Migration Steps:**
1. Update package version to 3.0.0
2. Remove `xmlns:ic="..."` namespace declaration
3. Replace all `ic:` → `i:` in XAML
4. Clean + Rebuild

---

## Common Use Cases

### 1. ListView ItemClick (Repeated Clicks)

**Problem:** `SelectedItem` doesn't fire PropertyChanged when clicking same item twice.

**Solution:** Use `ItemClick` event with Behavior.

```xml
<ListView IsItemClickEnabled="True" ...>
    <i:Interaction.Behaviors>
        <i:EventTriggerBehavior EventName="ItemClick">
            <i:InvokeCommandAction 
                Command="{x:Bind ViewModel.ItemClickedCommand}"
                CommandParameter="{Binding SelectedItem, ElementName=MyListView}" />
        </i:EventTriggerBehavior>
    </i:Interaction.Behaviors>
</ListView>
```

### 2. Button Click with Dynamic Parameter

```xml
<Button Content="Delete">
    <i:Interaction.Behaviors>
        <i:EventTriggerBehavior EventName="Click">
            <i:InvokeCommandAction 
                Command="{x:Bind ViewModel.DeleteCommand}"
                CommandParameter="{x:Bind CurrentItem}" />
        </i:EventTriggerBehavior>
    </i:Interaction.Behaviors>
</Button>
```

### 3. Multiple ListViews → One Command

```csharp
// Reusable command for all entity types
public ICommand ItemClickedCommand => _itemClickedCommand ??= new RelayCommand<object?>(item =>
{
    CurrentSelectedObject = item; // Works for Project, Journey, Station, etc.
});
```

---

## Benefits

| Aspect | Code-Behind ❌ | XAML Behaviors ✅ |
|--------|---------------|------------------|
| **MVVM-Conform** | No | Yes |
| **Testable** | Hard | Easy (test command) |
| **Reusable** | No | Yes (same command) |
| **LOC** | 40+ per handler | 5 XAML lines |
| **Repeated Clicks** | Manual workaround | Works out-of-box |

---

## Troubleshooting

### Build Error: "Unknown type 'EventTriggerBehavior'"

**Cause:** Wrong package or missing namespace.

**Fix:**
1. Check package: `Microsoft.Xaml.Behaviors.WinUI.Managed` (not `.Uwp.Managed`!)
2. Check namespace: `xmlns:i="using:Microsoft.Xaml.Interactivity"`
3. Clean + Restore + Rebuild

### Command Not Firing

**Cause:** Wrong CommandParameter binding.

**Fix:** Use `ElementName` to reference ListView:
```xml
CommandParameter="{Binding SelectedItem, ElementName=MyListView}"
```

### Namespace Mismatch Error

**Cause:** Partial class files have different namespaces.

**Fix:** Ensure all partial files use same namespace:
```csharp
namespace Moba.SharedUI.ViewModel // ✅ Same in all files!
```

---

## Related Documentation

- `docs/LEssONS-LEARNED-PROPERTYGRID-REFACTORING.md` - ContentControl pattern
- `.github/copilot-instructions.md` - Past Mistakes section
- `WinUI\View\EditorPage.xaml` - Reference implementation

---

**Last Updated:** 2025-12-09  
**Pattern:** Event-to-Command (XAML Behaviors)  
**Version:** 3.0.0
