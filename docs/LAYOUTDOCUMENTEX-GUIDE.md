# LayoutDocumentEx - Erweiterte Dokumentation

## 🎯 Features

### ✅ Tab-Grouping
Automatische Gruppierung von Tabs in Kategorien:
- **Modified** - Dokumente mit ungespeicherten Änderungen
- **Pinned** - Wichtige Dokumente
- **Open** - Reguläre offene Tabs

### ✅ ItemsSource & Binding
Vollständige MVVM-Unterstützung mit `ObservableCollection<DocumentTab>`

### ✅ Template Support
- `TabTemplate` - Custom Template für Tabs
- `ContentTemplate` - Custom Template für Tab-Content

### ✅ Floating Windows
Tabs können in separaten Fenstern geöffnet werden

### ✅ Rich Events
- `DocumentSelected` - Tab gewechselt
- `DocumentClosing` - Tab wird geschlossen (mit Cancel-Support)
- `NewTabRequested` - Neuer Tab angefordert
- `TabMovedToFloatingWindow` - Tab zu Floating Window verschoben

---

## 📚 Anwendungsbeispiele

### 1. Basic Usage - Simple Tab Collection

```xaml
<controls:LayoutDocumentEx
    x:Name="DocumentArea"
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}" />
```

```csharp
public class MainViewModel
{
    public ObservableCollection<DocumentTab> OpenDocuments { get; } = new();
    public DocumentTab? ActiveDocument { get; set; }
    
    public MainViewModel()
    {
        OpenDocuments.Add(new DocumentTab 
        { 
            Title = "Overview.md",
            IconGlyph = "\uE745",
            Content = new TextBlock { Text = "Overview Content" }
        });
    }
}
```

---

### 2. Mit Tab-Grouping

```xaml
<controls:LayoutDocumentEx
    x:Name="DocumentArea"
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    EnableTabGrouping="True" />
```

Bei dieser Konfiguration werden Tabs automatisch gruppiert nach:
- Modified (rote Punkt-Indicator)
- Pinned (Pin-Icon)
- Open (normale Tabs)

---

### 3. Custom Tab Template

```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}">
    
    <!-- Custom Tab Template -->
    <controls:LayoutDocumentEx.TabTemplate>
        <DataTemplate x:DataType="controls:DocumentTab">
            <StackPanel Orientation="Horizontal" Spacing="8" Padding="4">
                <!-- Icon -->
                <FontIcon
                    FontSize="14"
                    FontFamily="{StaticResource SymbolThemeFontFamily}"
                    Glyph="{Binding IconGlyph}" />
                
                <!-- Title -->
                <TextBlock 
                    Text="{Binding Title}"
                    VerticalAlignment="Center" />
                
                <!-- Modified Indicator -->
                <TextBlock
                    Text="●"
                    Foreground="Red"
                    FontSize="8"
                    Visibility="{Binding IsModified, Converter={StaticResource BoolToVisibilityConverter}}" />
                
                <!-- Pin Indicator -->
                <FontIcon
                    FontSize="12"
                    Glyph="&#xE840;"
                    Visibility="{Binding IsPinned, Converter={StaticResource BoolToVisibilityConverter}}" />
            </StackPanel>
        </DataTemplate>
    </controls:LayoutDocumentEx.TabTemplate>
    
    <!-- Custom Content Template -->
    <controls:LayoutDocumentEx.ContentTemplate>
        <DataTemplate x:DataType="controls:DocumentTab">
            <Border
                Background="{ThemeResource SurfaceBackgroundFillColorDefaultBrush}"
                Padding="16">
                <ScrollViewer>
                    <ContentPresenter Content="{Binding Content}" />
                </ScrollViewer>
            </Border>
        </DataTemplate>
    </controls:LayoutDocumentEx.ContentTemplate>
</controls:LayoutDocumentEx>
```

---

### 4. Mit Floating Windows

```xaml
<controls:LayoutDocumentEx
    x:Name="DocumentArea"
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    AllowFloatingTabs="True"
    TabMovedToFloatingWindow="DocumentArea_TabMovedToFloatingWindow" />
```

```csharp
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void DocumentArea_TabMovedToFloatingWindow(
        object sender, 
        DocumentTabMovedEventArgs e)
    {
        // Ein Tab wurde zu einem Floating Window verschoben
        System.Diagnostics.Debug.WriteLine($"Tab '{e.Document.Title}' is now floating");
    }
}
```

---

### 5. Tab-Verwaltung im ViewModel

```csharp
public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DocumentTab> openDocuments = new();
    
    [ObservableProperty]
    private DocumentTab? activeDocument;
    
    private readonly LayoutDocumentEx _documentArea;
    
    [RelayCommand]
    public void OpenNewDocument()
    {
        var doc = new DocumentTab
        {
            Title = $"Document {OpenDocuments.Count + 1}",
            IconGlyph = "\uE745",
            Content = CreateDocumentContent()
        };
        
        OpenDocuments.Add(doc);
        ActiveDocument = doc;
    }
    
    [RelayCommand]
    public void CloseCurrentDocument()
    {
        if (ActiveDocument != null)
        {
            _documentArea.RemoveDocument(ActiveDocument);
        }
    }
    
    [RelayCommand]
    public void MarkAsModified()
    {
        if (ActiveDocument != null)
        {
            _documentArea.MarkAsModified(ActiveDocument, true);
        }
    }
    
    [RelayCommand]
    public void PinCurrentDocument()
    {
        if (ActiveDocument != null)
        {
            _documentArea.PinDocument(
                ActiveDocument, 
                !ActiveDocument.IsPinned);
        }
    }
    
    [RelayCommand]
    public void MoveToFloatingWindow()
    {
        if (ActiveDocument != null)
        {
            _documentArea.MoveTabToFloatingWindow(ActiveDocument);
        }
    }
    
    private UIElement CreateDocumentContent()
    {
        return new Grid
        {
            Background = new SolidColorBrush(Colors.White),
            Children = 
            {
                new TextBlock 
                { 
                    Text = "Document Content",
                    Margin = new Thickness(16)
                }
            }
        };
    }
}
```

---

### 6. Mit Tab-Grouping-View

```xaml
<ItemsControl
    ItemsSource="{Binding GroupedTabs}"
    Background="{ThemeResource SurfaceBackgroundFillColorDefaultBrush}">
    
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="controls:TabGroup">
            <StackPanel Margin="0,8,0,0">
                <!-- Group Header -->
                <TextBlock
                    Text="{Binding Name}"
                    FontSize="12"
                    FontWeight="Bold"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                    Margin="16,4" />
                
                <!-- Group Items -->
                <ItemsControl ItemsSource="{Binding Tabs}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="controls:DocumentTab">
                            <Button
                                Padding="16,8"
                                Background="Transparent"
                                HorizontalAlignment="Stretch"
                                HorizontalContentAlignment="Left">
                                <StackPanel Orientation="Horizontal" Spacing="8">
                                    <FontIcon 
                                        FontSize="14"
                                        Glyph="{Binding IconGlyph}" />
                                    <TextBlock Text="{Binding Title}" />
                                </StackPanel>
                            </Button>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                
                <Border 
                    Height="1" 
                    Background="{ThemeResource DividerStrokeColorDefaultBrush}"
                    Margin="16,4" />
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🔧 API-Übersicht

### Properties

| Property | Type | Beschreibung |
|----------|------|-------------|
| `Documents` | `ObservableCollection<DocumentTab>` | Alle Tabs (Binding) |
| `ActiveDocument` | `DocumentTab` | Aktueller Tab (Binding) |
| `TabTemplate` | `DataTemplate` | Custom Template für Tabs |
| `ContentTemplate` | `DataTemplate` | Custom Template für Content |
| `EnableTabGrouping` | `bool` | Tab-Grouping aktivieren |
| `AllowFloatingTabs` | `bool` | Floating Windows zulassen |

### Methods

| Methode | Beschreibung |
|---------|-------------|
| `AddDocument(DocumentTab)` | Tab hinzufügen |
| `RemoveDocument(DocumentTab)` | Tab entfernen |
| `MarkAsModified(DocumentTab, bool)` | "Modified" Flag setzen |
| `PinDocument(DocumentTab, bool)` | Tab pinnen/unpinnen |
| `GetGroupedTabs()` | Gruppierte Tabs abrufen |
| `MoveTabToFloatingWindow(DocumentTab)` | Tab in Floating Window verschieben |
| `GetFloatingWindows()` | Alle Floating Windows abrufen |

### Events

| Event | Args | Beschreibung |
|-------|------|-------------|
| `DocumentSelected` | `DocumentTabChangedEventArgs` | Tab gewechselt |
| `DocumentClosing` | `DocumentTabClosingEventArgs` | Tab wird geschlossen |
| `NewTabRequested` | `EventArgs` | Neuer Tab angefordert |
| `TabMovedToFloatingWindow` | `DocumentTabMovedEventArgs` | Tab zu Floating Window |

---

## 🎨 Styling & Themes

Alle Farben verwenden `ThemeResource`:
- `SurfaceSecondaryBrush` - Tab Bar Hintergrund
- `SurfaceBackgroundFillColorDefaultBrush` - Content Bereich
- `TextFillColorPrimaryBrush` - Primary Text
- `TextFillColorSecondaryBrush` - Secondary Text
- `DividerStrokeColorDefaultBrush` - Trennlinien

---

## 📝 Lizenz

Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT.
