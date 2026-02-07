# DockingManager Control für WinUI3

Ein professioneller **DockingManager** für WinUI3 mit Visual Studio 2026-ähnlichem Layout und Fluent Design System Integration.

## 🎯 Features

### DockingManager
- **5 Dock-Bereiche**: Left, Right, Top, Bottom, Document (Center)
- **Resizable Panels**: Drag Splitter zum Anpassen der Größen
- **Fluent Design System**: Theme-aware Colors, Icons, Animations
- **Layout Persistierung**: Speichern/Laden des letzten Layouts
- **Pin/Unpin**: Panel-Minimierung ohne zu schließen
- **Maximize**: Maximiere einzelnes Panel, alle anderen verstecken
- **Drag & Drop**: Tab/Panel-Repositionierung zwischen Bereichen

### LayoutDocument (Basis)
- Tab-basierte Document Area
- ObservableCollection Binding

### LayoutDocumentEx (Erweitert) ⭐ **NEU**
- **Tab-Grouping**: Automatische Gruppierung (Modified, Pinned, Regular)
- **ItemsSource Binding**: Vollständige MVVM-Unterstützung
- **Custom Templates**: `TabTemplate` & `ContentTemplate`
- **Floating Windows**: Tabs in separaten Fenstern öffnen
- **Rich Events**: DocumentSelected, DocumentClosing, TabMovedToFloatingWindow
- **Tab-Management**: MarkAsModified, PinDocument, etc.

## 📁 Komponenten

### 1. **DockingManager** (Main Control)
5 Dock-Bereiche mit vollständiger Layout-Engine

### 2. **DockPanel** (Dockable Container)
Header mit Icon, Titel und Aktionsbuttons (Pin/Maximize/Close)

### 3. **LayoutDocument** (Basic Tabs)
Einfaches Tab-System mit ObservableCollection

### 4. **LayoutDocumentEx** (Advanced Tabs) ⭐ **NEU**
- Tab-Grouping (Modified, Pinned, Open)
- Template Support
- Floating Windows
- Rich Binding Support

### 5. **DockingPanelViewModel** (MVVM)
Observable Properties + RelayCommands für Layout-Management

### 6. **DockingLayoutService** (Persistierung)
Speichern/Laden von Layouts als JSON

## 🚀 Quick Start

### Basic DockingManager

```xaml
<controls:DockingManager
    LeftPanelContent="{Binding LeftPanel}"
    RightPanelContent="{Binding RightPanel}"
    TopPanelContent="{Binding TopPanel}"
    BottomPanelContent="{Binding BottomPanel}"
    DocumentAreaContent="{Binding DocumentArea}"
    LeftPanelWidth="240"
    IsLeftPanelVisible="True" />
```

### LayoutDocumentEx mit Tab-Grouping

```xaml
<controls:LayoutDocumentEx
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding ActiveDocument, Mode=TwoWay}"
    EnableTabGrouping="True"
    AllowFloatingTabs="True" />
```

```csharp
var docArea = new LayoutDocumentEx();
docArea.AddDocument(new DocumentTab 
{ 
    Title = "File.txt",
    IconGlyph = "\uE745",
    Content = new TextBlock { Text = "Content" }
});
```

---

## 📖 Dokumentation

- **[DOCKING-MANAGER-GUIDE.md](DOCKING-MANAGER-GUIDE.md)** - Hauptkontrol & Panels
- **[LAYOUTDOCUMENTEX-GUIDE.md](LAYOUTDOCUMENTEX-GUIDE.md)** - Erweiterte Tab-Features mit Grouping & Floating Windows

---

## ✨ Highlights

| Feature | Basis | Extended |
|---------|-------|----------|
| Dock-Areas | ✅ | ✅ |
| Tab Support | ✅ | ✅ |
| ObservableCollection Binding | ✅ | ✅ |
| Custom Templates | ❌ | ✅ |
| Tab-Grouping | ❌ | ✅ |
| Modified/Pinned Indicators | ❌ | ✅ |
| Floating Windows | ❌ | ✅ |
| Rich Events | ❌ | ✅ |

---

## 📄 Lizenz

Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
