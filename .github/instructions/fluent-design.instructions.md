---
description: 'Fluent Design System guidelines for WinUI 3 - materials, spacing, typography, icons, and theming.'
applyTo: 'WinUI/**/*.xaml'
---

# Fluent Design System (WinUI 3)

> MOBAflow follows Microsoft's **Fluent Design System** for a modern, consistent UI.

---

## Materials (Background Effects)

### Acrylic (Translucent blur)

```xaml
<!-- ✅ In-app Acrylic for panels and sidebars -->
<Grid Background="{ThemeResource AcrylicInAppFillColorDefaultBrush}">
    <!-- Content -->
</Grid>

<!-- ✅ Background Acrylic for app window -->
<Grid Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}">
    <!-- Content -->
</Grid>

<!-- ✅ Custom Acrylic with specific tint -->
<Grid>
    <Grid.Background>
        <AcrylicBrush TintColor="{ThemeResource SystemAccentColor}"
                      TintOpacity="0.8"
                      FallbackColor="{ThemeResource SystemAccentColor}" />
    </Grid.Background>
</Grid>
```

### Mica (Subtle background, Windows 11)

```csharp
// In MainWindow.xaml.cs - Enable Mica backdrop
public MainWindow()
{
    InitializeComponent();
    
    // Apply Mica backdrop (Windows 11)
    if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
    {
        SystemBackdrop = new MicaBackdrop();
    }
}
```

### Card Background (Elevated surfaces)

```xaml
<!-- ✅ Use CardBackgroundFillColorDefaultBrush for cards -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="8"
        Padding="16">
    <StackPanel>
        <TextBlock Text="Card Title" Style="{StaticResource SubtitleTextBlockStyle}" />
        <TextBlock Text="Card content goes here" />
    </StackPanel>
</Border>
```

---

## Spacing System (8px Grid)

### Standard Spacing Values

| Name | Value | Usage |
|------|-------|-------|
| **XXSmall** | 2px | Icon padding, tight groups |
| **XSmall** | 4px | Related items, inline spacing |
| **Small** | 8px | Default element spacing |
| **Medium** | 12px | Section spacing |
| **Large** | 16px | Card padding, major sections |
| **XLarge** | 24px | Page margins |
| **XXLarge** | 32px | Section dividers |

### XAML Examples

```xaml
<!-- ✅ Page with proper margins -->
<Page>
    <Grid Padding="24">  <!-- XLarge page margin -->
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="16" />  <!-- Large spacing -->
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        
        <TextBlock Text="Page Title" Style="{StaticResource TitleTextBlockStyle}" />
        
        <StackPanel Grid.Row="2" Spacing="8">  <!-- Small spacing between items -->
            <Button Content="Action 1" />
            <Button Content="Action 2" />
        </StackPanel>
    </Grid>
</Page>

<!-- ✅ Card with standard padding -->
<Border CornerRadius="8" Padding="16" Margin="0,0,0,12">
    <!-- Card content -->
</Border>

<!-- ✅ Grid with consistent spacing -->
<Grid ColumnSpacing="12" RowSpacing="8">
    <!-- Grid content -->
</Grid>
```

---

## Typography

### Text Styles (Use StaticResource)

```xaml
<!-- ✅ Headers -->
<TextBlock Text="Page Title" Style="{StaticResource TitleTextBlockStyle}" />
<TextBlock Text="Section Title" Style="{StaticResource SubtitleTextBlockStyle}" />
<TextBlock Text="Card Header" Style="{StaticResource BodyStrongTextBlockStyle}" />

<!-- ✅ Body text -->
<TextBlock Text="Regular text" Style="{StaticResource BodyTextBlockStyle}" />
<TextBlock Text="Secondary info" Style="{StaticResource CaptionTextBlockStyle}" />

<!-- ✅ Special styles -->
<TextBlock Text="Display Text" Style="{StaticResource DisplayTextBlockStyle}" />
<TextBlock Text="Header" Style="{StaticResource HeaderTextBlockStyle}" />
```

### Typography Hierarchy

| Style | Size | Weight | Usage |
|-------|------|--------|-------|
| Display | 68px | SemiBold | Hero text |
| Title | 28px | SemiBold | Page titles |
| Subtitle | 20px | SemiBold | Section headers |
| BodyStrong | 14px | SemiBold | Emphasis |
| Body | 14px | Regular | Default text |
| Caption | 12px | Regular | Secondary info |

---

## Icons (Segoe Fluent Icons)

### Common Icons

```xaml
<!-- ✅ Use FontIcon with Segoe Fluent Icons -->
<FontIcon Glyph="&#xE710;" />  <!-- Add -->
<FontIcon Glyph="&#xE711;" />  <!-- Cancel -->
<FontIcon Glyph="&#xE74E;" />  <!-- Save -->
<FontIcon Glyph="&#xE74D;" />  <!-- Delete -->
<FontIcon Glyph="&#xE768;" />  <!-- Play -->
<FontIcon Glyph="&#xE769;" />  <!-- Pause -->
<FontIcon Glyph="&#xE71E;" />  <!-- Search -->
<FontIcon Glyph="&#xE712;" />  <!-- More (Ellipsis) -->
<FontIcon Glyph="&#xE713;" />  <!-- Setting -->
<FontIcon Glyph="&#xE72B;" />  <!-- Sync -->

<!-- MOBAflow-specific icons -->
<FontIcon Glyph="&#xE7C8;" />  <!-- Train (Vehicle) -->
<FontIcon Glyph="&#xE774;" />  <!-- Map (Route) -->
<FontIcon Glyph="&#xE7B7;" />  <!-- Lightbulb (F0 Light) -->
<FontIcon Glyph="&#xE995;" />  <!-- Speaker (Announcement) -->
```

### Icon Sizes

```xaml
<!-- ✅ Standard icon sizes -->
<FontIcon Glyph="&#xE710;" FontSize="16" />  <!-- Small (buttons) -->
<FontIcon Glyph="&#xE710;" FontSize="20" />  <!-- Medium (default) -->
<FontIcon Glyph="&#xE710;" FontSize="24" />  <!-- Large (headers) -->
<FontIcon Glyph="&#xE710;" FontSize="32" />  <!-- XLarge (feature) -->

<!-- ✅ In NavigationView -->
<NavigationViewItem Content="Dashboard">
    <NavigationViewItem.Icon>
        <FontIcon Glyph="&#xE80F;" />  <!-- Home -->
    </NavigationViewItem.Icon>
</NavigationViewItem>
```

### SymbolIcon (Alternative)

```xaml
<!-- ✅ SymbolIcon for common actions -->
<SymbolIcon Symbol="Add" />
<SymbolIcon Symbol="Delete" />
<SymbolIcon Symbol="Save" />
<SymbolIcon Symbol="Edit" />
<SymbolIcon Symbol="Play" />
<SymbolIcon Symbol="Pause" />
<SymbolIcon Symbol="Setting" />
```

---

## Light/Dark Theme Support

### Theme Resources

```xaml
<!-- ✅ Always use ThemeResource for colors -->
<TextBlock Foreground="{ThemeResource TextFillColorPrimaryBrush}" />
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
<Button Background="{ThemeResource AccentFillColorDefaultBrush}" />

<!-- ❌ NEVER hardcode colors -->
<TextBlock Foreground="Black" />  <!-- Breaks in dark theme! -->
<Border Background="#FFFFFF" />    <!-- Breaks in dark theme! -->
```

### Common Theme Resources

| Resource | Light | Dark | Usage |
|----------|-------|------|-------|
| `TextFillColorPrimaryBrush` | Black | White | Primary text |
| `TextFillColorSecondaryBrush` | Gray | LightGray | Secondary text |
| `CardBackgroundFillColorDefaultBrush` | White | #2D2D2D | Card backgrounds |
| `AcrylicBackgroundFillColorDefaultBrush` | Translucent | Translucent | Panels |
| `AccentFillColorDefaultBrush` | Accent | Accent | Primary actions |
| `SystemFillColorCriticalBrush` | Red | Red | Errors |
| `SystemFillColorSuccessBrush` | Green | Green | Success |

### Theme Switching

```csharp
// In App.xaml.cs or SettingsPage
public void SetTheme(ElementTheme theme)
{
    if (App.MainWindow?.Content is FrameworkElement rootElement)
    {
        rootElement.RequestedTheme = theme;
    }
}

// Usage
SetTheme(ElementTheme.Light);
SetTheme(ElementTheme.Dark);
SetTheme(ElementTheme.Default);  // Follow system
```

---

## Control Styling

### Button Styles

```xaml
<!-- ✅ Primary action (filled) -->
<Button Content="Connect" Style="{StaticResource AccentButtonStyle}" />

<!-- ✅ Secondary action (outline) -->
<Button Content="Cancel" />

<!-- ✅ Danger action -->
<Button Content="Delete" 
        Background="{ThemeResource SystemFillColorCriticalBrush}"
        Foreground="White" />

<!-- ✅ Icon button -->
<Button Style="{StaticResource TransparentButtonStyle}">
    <FontIcon Glyph="&#xE712;" />
</Button>
```

### Input Styling

```xaml
<!-- ✅ TextBox with header -->
<TextBox Header="IP Address" 
         PlaceholderText="192.168.0.111"
         Text="{x:Bind ViewModel.IpAddress, Mode=TwoWay}" />

<!-- ✅ ComboBox with header -->
<ComboBox Header="Voice" 
          ItemsSource="{x:Bind ViewModel.AvailableVoices}"
          SelectedItem="{x:Bind ViewModel.SelectedVoice, Mode=TwoWay}" />

<!-- ✅ NumberBox (WinUI 3) -->
<NumberBox Header="Speed"
           Value="{x:Bind ViewModel.Speed, Mode=TwoWay}"
           Minimum="0"
           Maximum="126"
           SpinButtonPlacementMode="Inline" />
```

---

## Layout Patterns

### Standard Page Layout

```xaml
<Page>
    <Grid Padding="24" RowSpacing="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- Header -->
            <RowDefinition Height="*" />      <!-- Content -->
            <RowDefinition Height="Auto" />  <!-- Footer (optional) -->
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <StackPanel Orientation="Horizontal" Spacing="12">
            <TextBlock Text="Page Title" Style="{StaticResource TitleTextBlockStyle}" />
            <Button Content="Add" Style="{StaticResource AccentButtonStyle}" />
        </StackPanel>
        
        <!-- Content -->
        <ScrollViewer Grid.Row="1">
            <!-- Main content -->
        </ScrollViewer>
        
        <!-- Footer -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
            <Button Content="Cancel" />
            <Button Content="Save" Style="{StaticResource AccentButtonStyle}" />
        </StackPanel>
    </Grid>
</Page>
```

### Master-Detail Layout

```xaml
<Grid ColumnSpacing="16">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300" />  <!-- Master list -->
        <ColumnDefinition Width="*" />     <!-- Detail view -->
    </Grid.ColumnDefinitions>
    
    <!-- Master -->
    <Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
            CornerRadius="8">
        <ListView ItemsSource="{x:Bind ViewModel.Items}"
                  SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}" />
    </Border>
    
    <!-- Detail -->
    <Border Grid.Column="1"
            Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
            CornerRadius="8"
            Padding="16">
        <!-- Detail content -->
    </Border>
</Grid>
```

---

## Animation & Motion

### Built-in Transitions

```xaml
<!-- ✅ Page transitions -->
<Frame x:Name="ContentFrame">
    <Frame.ContentTransitions>
        <TransitionCollection>
            <NavigationThemeTransition />
        </TransitionCollection>
    </Frame.ContentTransitions>
</Frame>

<!-- ✅ List item transitions -->
<ListView>
    <ListView.ItemContainerTransitions>
        <TransitionCollection>
            <AddDeleteThemeTransition />
            <ReorderThemeTransition />
        </TransitionCollection>
    </ListView.ItemContainerTransitions>
</ListView>

<!-- ✅ Content transitions -->
<ContentControl>
    <ContentControl.ContentTransitions>
        <TransitionCollection>
            <ContentThemeTransition />
        </TransitionCollection>
    </ContentControl.ContentTransitions>
</ContentControl>
```

### Storyboard Animations

```xaml
<!-- ✅ Fade-in animation -->
<Storyboard x:Name="FadeInStoryboard">
    <DoubleAnimation Storyboard.TargetName="MyElement"
                     Storyboard.TargetProperty="Opacity"
                     From="0" To="1"
                     Duration="0:0:0.3">
        <DoubleAnimation.EasingFunction>
            <CubicEase EasingMode="EaseOut" />
        </DoubleAnimation.EasingFunction>
    </DoubleAnimation>
</Storyboard>
```

---

## MOBAflow-Specific Styles

### ControlStyles.xaml

Located at `WinUI/Resources/ControlStyles.xaml`:

- `BacklightToggleButtonStyle` - Function buttons with glow effect
- `DangerButtonStyle` - Red delete/stop buttons
- `CardStyle` - Standard card with shadow
- `SectionHeaderStyle` - Section title styling

### Usage

```xaml
<!-- ✅ Reference in Page -->
<Page.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Resources/ControlStyles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Page.Resources>

<!-- ✅ Use custom style -->
<ToggleButton Style="{StaticResource BacklightToggleButtonStyle}"
              Background="{x:Bind IsF0On, Converter={StaticResource BacklightConverter}}"
              Content="F0" />
```
