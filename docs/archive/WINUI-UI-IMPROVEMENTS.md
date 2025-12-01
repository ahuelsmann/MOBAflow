# 🎨 WinUI 3 Desktop App - UI/UX Verbesserungsplan

**Datum**: 2025-11-27  
**App**: MOBAflow (WinUI 3 Desktop)

---

## 🎯 Aktuelle Situation

### Explorer-Seite Analyse

**Layout:**
```
┌────────────────────────────────────────────┐
│ [NavigationView]                           │
├───────────────┬────────────────────────────┤
│               │                            │
│   TreeView    │   Properties Grid          │
│   (Solution)  │   (Selected Item)          │
│               │                            │
│               │   Cities List              │
│               │   (Station Selection)      │
│               │                            │
└───────────────┴────────────────────────────┘
```

**Probleme:**
- ❌ Feste Spaltenbreiten - kein Splitter
- ❌ TreeView und Properties nicht resizable
- ❌ Keine visuelle Trennung
- ❌ Technisches Layout

---

## 💡 Lösungen für WinUI 3

### 1️⃣ **GridSplitter** (Community Toolkit)

**JA, es gibt einen GridSplitter!**

```xml
<!-- NuGet Package -->
CommunityToolkit.WinUI.Controls.Sizers

<!-- Usage -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="250" MinWidth="200" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" MinWidth="300" />
    </Grid.ColumnDefinitions>
    
    <!-- TreeView -->
    <TreeView Grid.Column="0" />
    
    <!-- Splitter -->
    <toolkit:GridSplitter 
        Grid.Column="1" 
        Width="8"
        ResizeDirection="Columns"
        ResizeBehavior="BasedOnAlignment"
        Background="{ThemeResource ControlFillColorDefaultBrush}"
        CursorBehavior="ChangeOnSplitterHover" />
    
    <!-- Properties -->
    <ScrollViewer Grid.Column="2" />
</Grid>
```

### 2️⃣ **Visual Studio-Style Docking**

**NEIN, es gibt keinen vollständigen Dock Manager wie VS, ABER:**

**Alternativen:**
1. **TabView** für Multi-Documents
2. **Split View** für Master/Detail
3. **Custom Layout** mit GridSplitter

---

## 🎨 Konkrete Verbesserungen

### Priority 1: GridSplitter für Explorer

#### Vorher (Aktuell)
```xaml
<Grid Grid.Column="0" Padding="20">
    <TreeView ... />
</Grid>

<Grid Grid.Column="1" Padding="20">
    <ScrollViewer ... />
</Grid>
```

#### Nachher (Mit Splitter)
```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300" MinWidth="200" MaxWidth="600" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="2*" MinWidth="400" />
    </Grid.ColumnDefinitions>
    
    <!-- Left: TreeView -->
    <Grid Grid.Column="0">
        <TreeView ... />
    </Grid>
    
    <!-- Splitter -->
    <toolkit:GridSplitter 
        Grid.Column="1"
        Width="12"
        HorizontalAlignment="Center"
        VerticalAlignment="Stretch"
        Background="{ThemeResource CardStrokeColorDefaultBrush}"
        ResizeDirection="Columns"
        ResizeBehavior="BasedOnAlignment"
        CursorBehavior="ChangeOnSplitterHover">
        <toolkit:GridSplitter.Element>
            <Grid>
                <Rectangle Fill="{ThemeResource SystemControlBackgroundAccentBrush}" 
                           Width="2" />
            </Grid>
        </toolkit:GridSplitter.Element>
    </toolkit:GridSplitter>
    
    <!-- Right: Properties + Cities -->
    <Grid Grid.Column="2">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        
        <!-- Properties -->
        <ScrollViewer Grid.Row="0" />
        
        <!-- Horizontal Splitter -->
        <toolkit:GridSplitter 
            Grid.Row="1" 
            Height="12"
            ResizeDirection="Rows" />
        
        <!-- Cities -->
        <ListView Grid.Row="2" />
    </Grid>
</Grid>
```

---

### Priority 2: Moderne Farbpalette (wie MAUI)

#### WinUI 3 Theme Resources

```xml
<!-- App.xaml oder Resources.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="ms-appx:///Microsoft.UI.Xaml/DensityStyles/Compact.xaml" />
        </ResourceDictionary.MergedDictionaries>
        
        <!-- Railway Theme Colors -->
        <Color x:Key="RailwayPrimary">#1976D2</Color>
        <Color x:Key="RailwaySecondary">#FF6F00</Color>
        <Color x:Key="RailwayAccent">#00C853</Color>
        <Color x:Key="RailwayDanger">#D32F2F</Color>
        
        <!-- Brush Resources -->
        <SolidColorBrush x:Key="RailwayPrimaryBrush" Color="{StaticResource RailwayPrimary}" />
        <SolidColorBrush x:Key="RailwaySecondaryBrush" Color="{StaticResource RailwaySecondary}" />
        <SolidColorBrush x:Key="RailwayAccentBrush" Color="{StaticResource RailwayAccent}" />
        <SolidColorBrush x:Key="RailwayDangerBrush" Color="{StaticResource RailwayDanger}" />
    </ResourceDictionary>
</Application.Resources>
```

---

### Priority 3: Modern Cards (Fluent Design)

#### Card Style

```xaml
<Style x:Key="ModernCard" TargetType="Grid">
    <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
    <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
    <Setter Property="Margin" Value="0,0,0,12" />
    <Setter Property="Shadow">
        <Setter.Value>
            <ThemeShadow />
        </Setter.Value>
    </Setter>
</Style>

<!-- Usage -->
<Grid Style="{StaticResource ModernCard}">
    <StackPanel>
        <TextBlock Text="Card Title" Style="{StaticResource SubtitleTextBlockStyle}" />
        <TextBlock Text="Card content" Style="{StaticResource BodyTextBlockStyle}" />
    </StackPanel>
</Grid>
```

---

### Priority 4: Improved TreeView

#### Modern TreeView with Icons

```xaml
<TreeView x:Name="SolutionTree"
          ItemsSource="{x:Bind ViewModel.TreeNodes, Mode=OneWay}"
          SelectionMode="Single"
          SelectedItem="{x:Bind ViewModel.SelectedNode, Mode=TwoWay}"
          CanDragItems="True"
          CanReorderItems="True"
          AllowDrop="True">
    
    <TreeView.ItemTemplate>
        <DataTemplate x:DataType="vm:TreeNodeViewModel">
            <TreeViewItem ItemsSource="{x:Bind Children}"
                         IsExpanded="{x:Bind IsExpanded, Mode=TwoWay}">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <!-- Icon -->
                    <FontIcon 
                        Glyph="{x:Bind Icon}"
                        FontFamily="{StaticResource SymbolThemeFontFamily}"
                        FontSize="16"
                        Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}" />
                    
                    <!-- Display Name -->
                    <TextBlock Text="{x:Bind DisplayName}" 
                              VerticalAlignment="Center" />
                </StackPanel>
            </TreeViewItem>
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

---

### Priority 5: Master/Detail mit Acrylic

#### Fluent Design mit Acrylic Background

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    
    <!-- Master: TreeView mit Acrylic -->
    <Grid Grid.Column="0"
          Width="300"
          Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}">
        <TreeView ... />
    </Grid>
    
    <!-- Detail: Content -->
    <Grid Grid.Column="1" Padding="20">
        <!-- Content -->
    </Grid>
</Grid>
```

---

## 🔍 Referenz-Projekte

### 1. **WinUI 3 Gallery** (Official)
- **GitHub**: https://github.com/microsoft/WinUI-Gallery
- **Features**:
  - TreeView with Icons
  - GridSplitter Examples
  - NavigationView Patterns
  - Master/Detail Layouts

**Relevante Seiten:**
- `TreeViewPage.xaml` - TreeView Beispiele
- `GridSplitterPage.xaml` - Splitter Beispiele
- `MasterDetailsViewPage.xaml` - Master/Detail Pattern

### 2. **Windows File Manager Sample**
- **Pattern**: TreeView für Ordnerstruktur + Details
- **Features**: Drag & Drop, Context Menu

### 3. **DevToys** (Open Source)
- **GitHub**: https://github.com/veler/DevToys
- **Features**: Modern WinUI 3 UI, NavigationView, GridSplitter

### 4. **Windows Terminal** (Partially WinUI)
- **Pattern**: Tabs, Panes, Settings UI
- **Inspiration**: Modern, Fast, Fluent Design

---

## 📦 Benötigte NuGet Packages

```xml
<ItemGroup>
    <!-- GridSplitter -->
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Sizers" Version="8.2.*" />
    
    <!-- Additional Controls -->
    <PackageReference Include="CommunityToolkit.WinUI.Controls" Version="8.2.*" />
    
    <!-- Icons -->
    <PackageReference Include="Microsoft.UI.Xaml.Controls.Icons" Version="2.8.*" />
</ItemGroup>
```

---

## 🎨 Implementierungs-Plan

### Phase 1: GridSplitter (2-3 Std)
1. NuGet Package installieren
2. Explorer-Seite mit GridSplitter
3. Horizontal Splitter für Properties/Cities
4. Persistente Spaltenbreiten (Settings)

### Phase 2: Moderne Farben (1-2 Std)
1. Railway Theme Colors
2. Acrylic Backgrounds
3. Fluent Design Shadows

### Phase 3: TreeView Verbesserungen (2-3 Std)
1. Icons für Nodes
2. Context Menu
3. Drag & Drop

### Phase 4: Card-Based Layout (2-3 Std)
1. Card Styles
2. Properties in Cards
3. Better Spacing

### Phase 5: Status Bar & Info Panel (1-2 Std)
1. Status Bar am unteren Rand
2. Live-Status Indikatoren
3. Connection Status

---

## 📊 Vorher → Nachher

### Vorher
```
┌────────────────────────────────────┐
│ [Nav]                              │
├───────────┬────────────────────────┤
│           │                        │
│ TreeView  │ Properties (fix)       │
│ (fix)     │                        │
│           │ Cities (fix)           │
└───────────┴────────────────────────┘
```

### Nachher
```
┌────────────────────────────────────┐
│ [Nav mit Acrylic]                  │
├───────────┬─┬──────────────────────┤
│           │▓│                      │
│ TreeView  │▓│ Properties           │
│ + Icons   │▓├─────────────────────┤
│           │▓│                      │
│ (resize!) │▓│ Cities (resize!)    │
└───────────┴─┴──────────────────────┘
           ↑ GridSplitter
```

**Verbesserungen:**
- ✅ Resizable Panes
- ✅ Visual Splitter
- ✅ Icons in TreeView
- ✅ Modern Acrylic
- ✅ Fluent Design

---

## 💡 Alternative: TwoPane Layout

### Microsoft.UI.Xaml.Controls.TwoPaneView

```xaml
<TwoPaneView MinWideModeWidth="720"
             MinTallModeHeight="900"
             Pane1Length="1*"
             Pane2Length="2*">
    
    <TwoPaneView.Pane1>
        <!-- TreeView -->
    </TwoPaneView.Pane1>
    
    <TwoPaneView.Pane2>
        <!-- Properties + Cities -->
    </TwoPaneView.Pane2>
    
</TwoPaneView>
```

**Vorteil**: Responsive (Mobile-ready)  
**Nachteil**: Kein manueller Splitter

---

## 🎯 Empfehlung

**Start mit Phase 1: GridSplitter**

**Warum?**
1. ✅ Sofort spürbarer Unterschied
2. ✅ Einfach zu implementieren
3. ✅ Standard in Desktop-Apps
4. ✅ Professional Look

**Nächste Schritte:**
1. CommunityToolkit.WinUI.Controls.Sizers installieren
2. Explorer-Layout mit GridSplitter
3. Horizontal Splitter hinzufügen
4. Dann Farben & Icons

---

## 📚 Zusätzliche Resources

### WinUI 3 Dokumentation
- **GridSplitter**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/windows/sizers/gridsplitter
- **TreeView**: https://learn.microsoft.com/en-us/windows/apps/design/controls/tree-view
- **Master/Details**: https://learn.microsoft.com/en-us/windows/apps/design/controls/master-details
- **Fluent Design**: https://fluent2.microsoft.design/

### GitHub Samples
- **WinUI Gallery**: https://github.com/microsoft/WinUI-Gallery
- **DevToys**: https://github.com/veler/DevToys
- **Windows Community Toolkit**: https://github.com/CommunityToolkit/Windows

---

## ✅ Zusammenfassung

**Ihre Fragen beantwortet:**

1. **Splitter zwischen Baum und Properties?**
   → ✅ JA! `GridSplitter` aus CommunityToolkit

2. **Gibt es einen DockManager?**
   → ❌ NEIN, nicht wie Visual Studio
   → ✅ ABER: GridSplitter + TwoPaneView + Custom Layouts

3. **WinUI 3 Gallery Projekte?**
   → ✅ Ja! WinUI Gallery, DevToys, und mehr

**Nächster Schritt:**
Soll ich **Phase 1 (GridSplitter)** implementieren?
