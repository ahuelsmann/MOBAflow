# DockingManager - Häufig Gestellte Fragen (FAQ)

## ❓ Frage: Unterstützt du auch Dokumentregisterkarten und Registerkartengruppen?

### ✅ Ja! Es gibt zwei Varianten:

#### 1. **LayoutDocument** (Basis)
- ✅ Basis-Dokumentregisterkarten
- ✅ ObservableCollection Binding
- ❌ Keine automatischen Gruppen

#### 2. **LayoutDocumentEx** (Erweitert) ⭐ **Empfohlen**
- ✅ **Automatische Registerkartengruppen**:
  - **Modified** - Dokumente mit ungespeicherten Änderungen
  - **Pinned** - Wichtige/angeheftete Dokumente
  - **Open** - Normale offene Dokumente
- ✅ Visueller Indicator für Modified (●) und Pinned (📌)
- ✅ Intelligente Gruppierung mit `GetGroupedTabs()`

### 📝 Beispiel:

```csharp
// Modified-Gruppen automatisch generiert
var groups = docArea.GetGroupedTabs();

// Ausgabe:
// Group: Modified
//   - unsaved-file.txt
//   - changes.md
// Group: Pinned
//   - important-doc.txt
// Group: Open
//   - readme.md
//   - config.xml
```

---

## ❓ Frage: Unterstützt das Control auch Binding? Gibt es ItemsSource?

### ✅ Ja! Vollständiges MVVM-Binding:

```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}">
</controls:LayoutDocumentEx>
```

### Bindbare Properties:

| Property | Binding | Beschreibung |
|----------|---------|-------------|
| `Documents` | ✅ TwoWay | ObservableCollection<DocumentTab> |
| `ActiveDocument` | ✅ TwoWay | Aktuell ausgewählter Tab |
| `TabTemplate` | ✅ OneWay | Custom Template für Tabs |
| `ContentTemplate` | ✅ OneWay | Custom Template für Content |
| `EnableTabGrouping` | ✅ OneWay | Grouping aktivieren/deaktivieren |
| `AllowFloatingTabs` | ✅ OneWay | Floating Windows aktivieren |

### Vollständiges MVVM-Beispiel:

```csharp
public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DocumentTab> openDocuments = new();
    
    [ObservableProperty]
    private DocumentTab? activeDocument;
    
    [RelayCommand]
    public void OpenDocument(string filePath)
    {
        var doc = new DocumentTab
        {
            Title = Path.GetFileName(filePath),
            IconGlyph = GetIconForFile(filePath),
            Content = CreateEditorContent(filePath),
            Tag = filePath
        };
        
        OpenDocuments.Add(doc);
        ActiveDocument = doc;
    }
    
    [RelayCommand]
    public void MarkAsModified()
    {
        if (ActiveDocument != null)
            ActiveDocument.IsModified = true;
    }
}
```

---

## ❓ Frage: Gibt es ItemsSource für die Registerkarten?

### ✅ Ja! Es gibt mehrere Bindungs-Optionen:

#### 1. **Direkt über Documents Property** (Empfohlen)
```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding CurrentDocument, Mode=TwoWay}" />
```

#### 2. **Mit Custom Tab-ItemTemplate**
```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    EnableTabGrouping="True">
    
    <!-- Custom Template für Tab-Renderung -->
    <controls:LayoutDocumentEx.TabTemplate>
        <DataTemplate x:DataType="controls:DocumentTab">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <FontIcon Glyph="{Binding IconGlyph}" FontSize="14" />
                <TextBlock Text="{Binding Title}" FontWeight="Bold" />
                <TextBlock 
                    Text="●" 
                    Foreground="Red"
                    Visibility="{Binding IsModified, 
                        Converter={StaticResource BoolToVisibilityConverter}}" />
            </StackPanel>
        </DataTemplate>
    </controls:LayoutDocumentEx.TabTemplate>
</controls:LayoutDocumentEx>
```

#### 3. **Mit Gruppierter View**
```xaml
<!-- Zeige Tabs gruppiert an -->
<ItemsControl ItemsSource="{Binding GroupedTabs}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="controls:TabGroup">
            <StackPanel>
                <TextBlock Text="{Binding Name}" FontWeight="Bold" />
                <ItemsControl ItemsSource="{Binding Tabs}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="controls:DocumentTab">
                            <Button Content="{Binding Title}" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## ❓ Frage: Window bzw Container Support?

### ✅ Ja! Es gibt mehrere Container-Optionen:

#### 1. **In DockingManager integriert**
```xaml
<controls:DockingManager
    DocumentAreaContent="{Binding DocumentArea}">
    <controls:DockingManager.DocumentAreaContent>
        <controls:LayoutDocumentEx
            Documents="{Binding OpenDocuments}"
            ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}" />
    </controls:DockingManager.DocumentAreaContent>
</controls:DockingManager>
```

#### 2. **Standalone Container**
```xaml
<Grid>
    <controls:LayoutDocumentEx
        Documents="{Binding OpenDocuments}"
        ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}" />
</Grid>
```

#### 3. **Floating Windows** ⭐
```xaml
<controls:LayoutDocumentEx
    AllowFloatingTabs="True"
    TabMovedToFloatingWindow="OnTabMovedToFloating" />
```

```csharp
public void OnTabMovedToFloating(object sender, DocumentTabMovedEventArgs e)
{
    var window = e.Window;
    window.Title = $"[Floating] {e.Document.Title}";
    window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });
}
```

---

## 🎯 Schnell-Übersicht: Welche Features wo?

### DockingManager
- **5 Dock-Bereiche** (Left, Right, Top, Bottom, Center)
- **Resizable Panels**
- **Pin/Maximize/Close**
- **Layout-Persistierung**

### LayoutDocument (Basis)
- **Einfache Tabs**
- **ObservableCollection Binding**
- **Tab-Selection**

### LayoutDocumentEx (Erweitert) ⭐
- **Alles aus LayoutDocument +**
- **Tab-Grouping** (Modified, Pinned, Open)
- **Custom Templates** (Tabs & Content)
- **Floating Windows**
- **Rich Events** (DocumentSelected, DocumentClosing, TabMovedToFloatingWindow)
- **Modified/Pinned Indicators**

---

## 💡 Best Practices

### 1. Für Standard-Anwendung (z.B. Text Editor)
```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    EnableTabGrouping="True" />
```

### 2. Für Multi-Monitor-Setup
```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    AllowFloatingTabs="True" />
```

### 3. Für Editor mit Status-Anzeige
```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    EnableTabGrouping="True">
    
    <controls:LayoutDocumentEx.TabTemplate>
        <DataTemplate x:DataType="controls:DocumentTab">
            <!-- Show modified indicator and pin status -->
        </DataTemplate>
    </controls:LayoutDocumentEx.TabTemplate>
</controls:LayoutDocumentEx>
```

---

## 📚 Weiterführende Dokumentation

- **[LAYOUTDOCUMENT-COMPARISON.md](LAYOUTDOCUMENT-COMPARISON.md)** - LayoutDocument vs LayoutDocumentEx
- **[LAYOUTDOCUMENTEX-GUIDE.md](LAYOUTDOCUMENTEX-GUIDE.md)** - Umfassende LayoutDocumentEx Dokumentation
- **[DOCKING-MANAGER-GUIDE.md](DOCKING-MANAGER-GUIDE.md)** - DockingManager Hauptdokumentation

---

## 📞 Zusammenfassung

| Frage | Antwort |
|-------|---------|
| Registerkarten & Gruppen? | ✅ Ja - LayoutDocumentEx mit automatischen Gruppen (Modified/Pinned/Open) |
| Binding & ItemsSource? | ✅ Ja - ObservableCollection mit vollständiger MVVM-Unterstützung |
| Window/Container? | ✅ Ja - Floating Windows, DockingManager Integration, Standalone möglich |
| Custom Templates? | ✅ Ja - TabTemplate & ContentTemplate für Customization |
| Rich Events? | ✅ Ja - DocumentSelected, DocumentClosing, TabMovedToFloatingWindow |
| Production-Ready? | ✅ Ja - Fluent Design System, Persistierung, Error-Handling |

---

## 📄 Lizenz

Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT.
