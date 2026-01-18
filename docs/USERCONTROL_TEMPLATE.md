# WinUI UserControl Template Guide

## Overview

`UserControl1` is/was a placeholder template for creating custom reusable WinUI 3 controls. This guide explains how to create a new UserControl following MOBAflow conventions.

## Creating a New UserControl

### Step 1: Create XAML File
**Path:** `WinUI/Controls/MyCustomControl.xaml`

```xml
<UserControl
    x:Class="Moba.WinUI.Controls.MyCustomControl"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <StackPanel Padding="16" Spacing="8">
        <TextBlock Text="My Custom Control" Style="{ThemeResource SubtitleTextBlockStyle}" />
        <!-- Add your UI here -->
    </StackPanel>

</UserControl>
```

### Step 2: Create Code-Behind
**Path:** `WinUI/Controls/MyCustomControl.xaml.cs`

```csharp
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Moba.WinUI.Controls;

/// <summary>
/// MyCustomControl - Custom reusable UserControl.
/// 
/// Usage:
/// <local:MyCustomControl />
/// </summary>
public sealed partial class MyCustomControl : UserControl
{
    public MyCustomControl()
    {
        InitializeComponent();
    }

    // Add Dependency Properties for data binding
    // Add Custom Events for user interactions
    // Add Methods for programmatic control
}
```

### Step 3: Register in DI (if needed)
**File:** `WinUI/App.xaml.cs`

```csharp
services.AddTransient<MyCustomControl>();
services.AddTransient<MyCustomControlPage>();
```

### Step 4: Use in XAML Pages
```xaml
<Page
    xmlns:local="using:Moba.WinUI.Controls">
    
    <StackPanel>
        <local:MyCustomControl />
    </StackPanel>
</Page>
```

## Conventions

- **Naming:** Use descriptive names (e.g., `ThemeSelectorControl`, `SpeedometerControl`)
- **Namespace:** Always use `Moba.WinUI.Controls`
- **Copyright:** Include MIT license header
- **Documentation:** Add XML comments on public properties/methods
- **Styling:** Use theme resources (`{StaticResource ThemeAccentBrush}`)
- **Testing:** Create tests in `Test/` project if control has complex logic

## Examples in MOBAflow

- **ThemeSelectorControl** - Theme switcher with ComboBox
- **SpeedometerControl** - Custom gauge/speedometer display
- **[Add more examples]**

## See Also

- `WinUI/Controls/` - Control implementations
- `SKIN_SYSTEM_QUICKSTART.md` - Theme integration guide
- `.github/instructions/architecture.instructions.md` - Architecture overview
