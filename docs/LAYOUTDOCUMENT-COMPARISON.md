# LayoutDocument vs LayoutDocumentEx - Vergleich

## 📊 Feature-Matrix

| Feature | LayoutDocument | LayoutDocumentEx |
|---------|---|---|
| **Basis-Funktionalität** | | |
| ObservableCollection Binding | ✅ | ✅ |
| Tab-Verwaltung (Add/Remove) | ✅ | ✅ |
| Document-Selection | ✅ | ✅ |
| **Erweiterte Features** | | |
| Tab-Grouping (Modified/Pinned) | ❌ | ✅ |
| Custom Tab Template | ❌ | ✅ |
| Custom Content Template | ❌ | ✅ |
| IsModified Property | ❌ | ✅ |
| IsPinned Property | ❌ | ✅ |
| Floating Windows | ❌ | ✅ |
| Tab-zu-Window-Event | ❌ | ✅ |
| GetGroupedTabs() Method | ❌ | ✅ |
| MarkAsModified() Method | ❌ | ✅ |
| PinDocument() Method | ❌ | ✅ |

---

## 🎯 Wann welches Control verwenden?

### ✅ Nutze **LayoutDocument** wenn:
- Sie ein **einfaches Tab-System** benötigen
- Keine besonderen visuellen Indikatoren (Modified, Pinned) nötig sind
- Keine Custom Templates erforderlich sind
- Größtmögliche **Einfachheit & Performance** wichtig ist
- Sie nur die **Basis-Tab-Funktionalität** brauchen

### ✅ Nutze **LayoutDocumentEx** wenn:
- Sie **VS-ähnliches Interface** mit Grouping wollen
- **Modified-Indicator** (Punkt bei ungespeicherten Dateien) benötigt
- **Pinned-Tabs** für häufig verwendete Dateien wollen
- **Custom Templates** für Tabs/Content brauchen
- **Floating Windows** für Multi-Monitor-Setup wollen
- **Rich Binding** und **Events** benötigen

---

## 🔄 Migration von LayoutDocument zu LayoutDocumentEx

### Schritt 1: Namespace ändern
```csharp
// Alt
var docArea = new LayoutDocument();

// Neu
var docArea = new LayoutDocumentEx();
```

### Schritt 2: XAML aktualisieren
```xaml
<!-- Alt -->
<controls:LayoutDocument
    Documents="{Binding Documents}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}" />

<!-- Neu -->
<controls:LayoutDocumentEx
    Documents="{Binding Documents}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    EnableTabGrouping="True"
    AllowFloatingTabs="True" />
```

### Schritt 3: Events nutzen (optional)
```csharp
// Neue Events hinzufügen
docArea.TabMovedToFloatingWindow += (s, e) =>
{
    Debug.WriteLine($"Tab '{e.Document.Title}' ist jetzt floating");
};
```

---

## 📝 Code-Beispiele

### LayoutDocument - Einfach

```xaml
<controls:LayoutDocument
    x:Name="SimpleDocArea"
    Documents="{Binding OpenFiles}"
    ActiveDocument="{Binding CurrentFile, Mode=TwoWay}" />
```

```csharp
public class SimpleViewModel
{
    public ObservableCollection<DocumentTab> OpenFiles { get; } = new();
    public DocumentTab? CurrentFile { get; set; }
    
    public void OpenFile()
    {
        var tab = new DocumentTab 
        { 
            Title = "file.txt",
            Content = new TextBlock { Text = "Content" }
        };
        OpenFiles.Add(tab);
        CurrentFile = tab;
    }
}
```

---

### LayoutDocumentEx - Mit Grouping

```xaml
<controls:LayoutDocumentEx
    x:Name="AdvDocArea"
    Documents="{Binding OpenFiles}"
    ActiveDocument="{Binding CurrentFile, Mode=TwoWay}"
    EnableTabGrouping="True">
</controls:LayoutDocumentEx>
```

```csharp
public partial class AdvancedViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DocumentTab> openFiles = new();
    
    [ObservableProperty]
    private DocumentTab? currentFile;
    
    [RelayCommand]
    public void OpenFile()
    {
        var tab = new DocumentTab { Title = "file.txt" };
        OpenFiles.Add(tab);
        CurrentFile = tab;
    }
    
    [RelayCommand]
    public void MarkFileAsModified()
    {
        if (CurrentFile != null)
            CurrentFile.IsModified = true;
    }
    
    [RelayCommand]
    public void PinFile()
    {
        if (CurrentFile != null)
            CurrentFile.IsPinned = !CurrentFile.IsPinned;
    }
}
```

---

### LayoutDocumentEx - Mit Custom Templates

```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenFiles}"
    ActiveDocument="{Binding CurrentFile, Mode=TwoWay}"
    EnableTabGrouping="True">
    
    <!-- Custom Tab Template -->
    <controls:LayoutDocumentEx.TabTemplate>
        <DataTemplate x:DataType="controls:DocumentTab">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <FontIcon Glyph="{Binding IconGlyph}" />
                <TextBlock Text="{Binding Title}" />
                <!-- Modified Dot -->
                <Ellipse 
                    Width="6" Height="6"
                    Fill="Red"
                    Visibility="{Binding IsModified, 
                        Converter={StaticResource BoolToVisibilityConverter}}" />
            </StackPanel>
        </DataTemplate>
    </controls:LayoutDocumentEx.TabTemplate>
</controls:LayoutDocumentEx>
```

---

### LayoutDocumentEx - Mit Floating Windows

```xaml
<controls:LayoutDocumentEx
    x:Name="FloatingDocArea"
    Documents="{Binding OpenFiles}"
    ActiveDocument="{Binding CurrentFile, Mode=TwoWay}"
    AllowFloatingTabs="True"
    TabMovedToFloatingWindow="OnTabMovedToFloating" />
```

```csharp
public sealed partial class MainWindow : Window
{
    private LayoutDocumentEx _docArea;
    
    public MainWindow()
    {
        InitializeComponent();
        _docArea = FloatingDocArea;
    }
    
    private void OnTabMovedToFloating(
        object sender, 
        DocumentTabMovedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Tab '{e.Document.Title}' opened in floating window");
    }
    
    public void MoveCurrentTabToWindow()
    {
        if (_docArea.ActiveDocument != null)
            _docArea.MoveTabToFloatingWindow(_docArea.ActiveDocument);
    }
}
```

---

## 🎓 Best Practices

### 1. Dokumentgruppen anzeigen
```csharp
// Gibt automatisch gruppierte Tabs zurück
var groups = docArea.GetGroupedTabs();
foreach (var group in groups)
{
    Console.WriteLine($"Group: {group.Name}");
    foreach (var tab in group.Tabs)
    {
        Console.WriteLine($"  - {tab.Title}");
    }
}
```

### 2. Modified-State richtig nutzen
```csharp
// In TextEditor ViewModel
[RelayCommand]
public void OnTextChanged()
{
    CurrentDocument.IsModified = true;
    // Optional: UI aktualisieren
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
}

[RelayCommand]
public async Task SaveDocument()
{
    await _fileService.SaveAsync(CurrentDocument.Tag as string);
    CurrentDocument.IsModified = false;
}
```

### 3. Floating-Windows-Workflow
```csharp
// Benutzer zieht Tab raus => Window wird erstellt
docArea.MoveTabToFloatingWindow(tab);

// Benutzer schließt Floating Window => Tab kommt zurück
// (Automatisch durch Event-Handler)
```

### 4. Custom Tab-Template für Professionelles UI
```xaml
<controls:LayoutDocumentEx.TabTemplate>
    <DataTemplate x:DataType="controls:DocumentTab">
        <Grid Padding="4,0" ColumnSpacing="4">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            
            <!-- Icon -->
            <FontIcon 
                Grid.Column="0"
                FontSize="14"
                Glyph="{Binding IconGlyph}" />
            
            <!-- Title -->
            <TextBlock 
                Grid.Column="1"
                Text="{Binding Title}"
                MaxWidth="200"
                TextTrimming="CharacterEllipsis" />
            
            <!-- Modified Indicator -->
            <TextBlock
                Grid.Column="2"
                Text="●"
                Foreground="OrangeRed"
                FontSize="8"
                Visibility="{Binding IsModified, 
                    Converter={StaticResource BoolToVisibilityConverter}}" />
            
            <!-- Pin Indicator -->
            <FontIcon
                Grid.Column="3"
                FontSize="12"
                Glyph="&#xE840;"
                Visibility="{Binding IsPinned, 
                    Converter={StaticResource BoolToVisibilityConverter}}" />
        </Grid>
    </DataTemplate>
</controls:LayoutDocumentEx.TabTemplate>
```

---

## 📄 Lizenz

Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT.
