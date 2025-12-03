# SimplePropertyGrid Troubleshooting Guide

## ❌ Error: 0xc000027b (STATUS_DLL_INIT_FAILED)

### Root Cause
ResourceDictionary konnte nicht geladen werden

### Fixes Applied
1. ✅ Entfernt `<Page Remove="Controls\SimplePropertyGrid.xaml" />`
2. ✅ SDK auto-discovery aktiviert (keine expliziten Page-Einträge)
3. ✅ ResourceDictionary-Pfad korrigiert: `/Controls/SimplePropertyGrid.xaml`
4. ✅ `[TemplatePart]` Attribut hinzugefügt

### Verification Steps

1. **Check if XAML is compiled:**
```powershell
Test-Path "WinUI\obj\Debug\*.g.cs" -Include "*SimplePropertyGrid*"
```

2. **Check ResourceDictionary in App.xaml:**
```xml
<ResourceDictionary Source="/Controls/SimplePropertyGrid.xaml" />
```

3. **Clean and Rebuild:**
```powershell
dotnet clean
dotnet build
```

### Alternative: Inline Styles (If ResourceDictionary Issues Persist)

If loading from separate XAML fails, you can inline the style in App.xaml:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
        </ResourceDictionary.MergedDictionaries>
        
        <!-- Inline SimplePropertyGrid Style -->
        <Style TargetType="local:SimplePropertyGrid" xmlns:local="using:Moba.WinUI.Controls">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="local:SimplePropertyGrid">
                        <ScrollViewer VerticalScrollBarVisibility="Auto">
                            <StackPanel x:Name="PART_PropertiesPanel" Spacing="0" Padding="16" />
                        </ScrollViewer>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

### Debug Output

Enable detailed XAML diagnostics in App.xaml.cs:

```csharp
public App()
{
    this.InitializeComponent();
    
    #if DEBUG
    this.DebugSettings.EnableFrameRateCounter = false;
    this.DebugSettings.IsBindingTracingEnabled = true;
    this.DebugSettings.IsOverdrawHeatMapEnabled = false;
    #endif
}
```

### Common Issues

| Issue | Symptom | Fix |
|-------|---------|-----|
| **ResourceDictionary not found** | `0xc000027b` on startup | Check path uses `/` not `\` |
| **PART_PropertiesPanel null** | Empty PropertyGrid | Verify ControlTemplate name matches |
| **Properties not updating** | Stale values | Call `Bindings.Update()` in Page |
| **No properties shown** | Empty panel | Check `SelectedObject != null` |

### Testing

1. Set breakpoint in `SimplePropertyGrid.OnApplyTemplate()`
2. Verify `_propertiesPanel` is not null
3. Step through `RefreshProperties()` to see property enumeration
4. Check `CreateEditor()` returns valid controls

