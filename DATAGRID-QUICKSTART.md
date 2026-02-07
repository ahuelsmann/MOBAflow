# 🚀 Quick Setup: Community Toolkit DataGrid für MOBAflow

## 📋 3-Minuten-Setup

### Schritt 1: NuGet Package hinzufügen

**Option A: Package Manager (Visual Studio)**
```
Tools → NuGet Package Manager → Package Manager Console

Install-Package CommunityToolkit.WinUI.Controls.DataGrid
```

**Option B: CLI**
```bash
cd WinUI
dotnet add package CommunityToolkit.WinUI.Controls.DataGrid
```

**Option C: .csproj manuell bearbeiten**
```xml
<ItemGroup>
    <PackageReference Include="CommunityToolkit.WinUI.Controls.DataGrid" Version="8.0.240808" />
</ItemGroup>
```

### Schritt 2: Rebuild

```bash
dotnet clean
dotnet build
```

### Schritt 3: Testen

1. Starte die App
2. Navigiere zu **"Docking Test"** Plugin
3. Klick auf **"DataGrid Demo"** Tab
4. Das DataGrid wird mit 20 Sample-Items angezeigt!

---

## 🎯 Was zeigt das DataGrid Demo?

```
DataGridDemoPage
├── Header
│   └── "Community Toolkit DataGrid Demo"
├── Sample Data (20 Items)
│   ├── ID (1-20)
│   ├── Name (Item #1, Item #2, ...)
│   ├── Category (Software, Hardware, Network, Database, Application)
│   ├── Value (100-1000)
│   ├── IsActive (Random True/False)
│   └── CreatedDate (Random past dates)
└── Features
    ├── Auto-generated columns
    ├── Sortable columns
    ├── Selectable rows
    ├── Fluent Design styling
    └── Full MVVM binding
```

---

## 📝 So funktioniert's nach der Installation

### ViewModel (DockingTestPluginViewModel.cs)
```csharp
// Sample data wird automatisch generiert
[ObservableProperty]
private ObservableCollection<SampleDataItem> sampleDataItems = new();

// 20 Items in InitializeSampleData()
private void InitializeSampleData()
{
    for (int i = 1; i <= 20; i++)
    {
        SampleDataItems.Add(new SampleDataItem { ... });
    }
}
```

### XAML (DockingTestPluginContentProvider.xaml)
```xaml
<toolkit:DataGrid
    ItemsSource="{Binding SampleDataItems}"
    AutoGenerateColumns="True"
    CanUserAddRows="False" />
```

---

## ✅ Checkliste

- [ ] NuGet Package `CommunityToolkit.WinUI.Controls.DataGrid` installiert
- [ ] `dotnet build` erfolgreich
- [ ] App läuft ohne Fehler
- [ ] "Docking Test" Plugin sichtbar
- [ ] "DataGrid Demo" Tab vorhanden
- [ ] DataGrid mit 20 Items angezeigt
- [ ] Spalten sortierbar
- [ ] Rows selektierbar

---

## 🎨 Customization nach Installation

### Option 1: AutoGenerate (Automatisch)
```xaml
<toolkit:DataGrid
    ItemsSource="{Binding SampleDataItems}"
    AutoGenerateColumns="True" />
```

### Option 2: Manuelle Spalten
```xaml
<toolkit:DataGrid ItemsSource="{Binding SampleDataItems}">
    <toolkit:DataGrid.Columns>
        <toolkit:DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="50" />
        <toolkit:DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="200" />
        <toolkit:DataGridCheckBoxColumn Header="Active" Binding="{Binding IsActive}" Width="80" />
    </toolkit:DataGrid.Columns>
</toolkit:DataGrid>
```

### Option 3: Mit Fluent Design
```xaml
<toolkit:DataGrid
    ItemsSource="{Binding SampleDataItems}"
    AutoGenerateColumns="True"
    Background="{ThemeResource SurfaceBackgroundFillColorDefaultBrush}"
    Foreground="{ThemeResource TextFillColorPrimaryBrush}"
    BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}" />
```

---

## 🔧 Fehlerbehandlung

**Wenn DataGrid nicht angezeigt wird:**

1. ✓ Package installiert? `dotnet list package`
2. ✓ Build erfolgreich? `dotnet build -v`
3. ✓ Clean Rebuild? `dotnet clean && dotnet build`
4. ✓ Namespace? `xmlns:toolkit="using:CommunityToolkit.WinUI.Controls"`
5. ✓ Daten vorhanden? `Debug.WriteLine(SampleDataItems.Count)`

---

## 📚 Weitere Ressourcen

- **Vollständiger Guide:** `docs/DATAGRID-INTEGRATION-GUIDE.md`
- **Plugin Dokumentation:** `Plugins/DockingTestPlugin/README.md`
- **Microsoft Docs:** [DataGrid auf Learn](https://learn.microsoft.com/en-us/windows/communitytoolkit/controls/datagrid)

---

## 🎉 Fertig!

Das DataGrid ist jetzt ready to use im DockingTestPlugin! 📊

**Viel Spaß beim Testen!** 🚀
