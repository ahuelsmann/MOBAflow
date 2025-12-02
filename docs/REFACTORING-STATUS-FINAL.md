# Refactoring Status - Fast fertig!

**Datum**: 2025-01-21 22:00  
**Status**: 90% Complete

---

## ✅ Was bereits erledigt ist:

1. ✅ **EditorPage → EditorPage1**:
   - Datei umbenannt
   - EditorPage1.xaml.cs neu erstellt
   - Bindings angepasst (`{Binding ViewModel.XXX}`)
   - Verwendet jetzt `MainWindowViewModel` direkt

2. ✅ **ProjectConfigurationPage → EditorPage2**:
   - XAML-Datei umbenannt
   - Bindings angepasst
   - x:Class korrekt

3. ✅ **ViewModels gelöscht**:
   - EditorPageViewModel.cs gelöscht
   - ProjectConfigurationPageViewModel.cs gelöscht

4. ✅ **Build-Ordner bereinigt**:
   - `WinUI\obj` gelöscht
   - `WinUI\bin` gelöscht

---

## ⚠️ Was noch fehlt:

### 1. EditorPage2.xaml.cs manuell anpassen

**Datei**: `WinUI\View\EditorPage2.xaml.cs` (ist in VS offen!)

**Ersetze**:
```csharp
public sealed partial class ProjectConfigurationPage : Page
{
    public ProjectConfigurationPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is ProjectConfigurationPageViewModel viewModel)
        {
            ViewModel = viewModel;
            // ...
        }
    }
}
```

**Durch**:
```csharp
public sealed partial class EditorPage2 : Page
{
    public MainWindowViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            System.Diagnostics.Debug.WriteLine("✅ EditorPage2 loaded");
            System.Diagnostics.Debug.WriteLine($"   CurrentProject: {ViewModel.CurrentProjectViewModel?.Name}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ EditorPage2: No MainWindowViewModel!");
        }
    }
}
```

---

### 2. MainWindow.xaml.cs Navigation anpassen

**Datei**: `WinUI\View\MainWindow.xaml.cs`

**Suche nach** (2x):
```csharp
case "editor":
    // ... EditorPageViewModel ...
    ContentFrame.Navigate(typeof(EditorPage), editorViewModel);
```

**Ersetze durch**:
```csharp
case "editor":
    ContentFrame.Navigate(typeof(EditorPage1), ViewModel);
    break;
```

**Und suche nach**:
```csharp
case "configuration":
    // ... ProjectConfigurationPageViewModel ...
    ContentFrame.Navigate(typeof(ProjectConfigurationPage), configViewModel);
```

**Ersetze durch**:
```csharp
case "configuration":
    ContentFrame.Navigate(typeof(EditorPage2), ViewModel);
    break;
```

---

### 3. MainWindow.xaml Navigation Labels anpassen (optional)

**Datei**: `WinUI\View\MainWindow.xaml`

**Suche nach**:
```xml
<NavigationViewItem Content="Editor" ... />
<NavigationViewItem Content="Configuration" ... />
```

**Ersetze durch**:
```xml
<NavigationViewItem Content="Editor 1" ... />
<NavigationViewItem Content="Editor 2" ... />
```

---

## 🔧 Manuelle Schritte (in Visual Studio):

1. **Öffne** `EditorPage2.xaml.cs`
2. **Ändere** class name von `ProjectConfigurationPage` zu `EditorPage2`
3. **Ändere** `ProjectConfigurationPageViewModel` zu `MainWindowViewModel`
4. **Öffne** `MainWindow.xaml.cs`
5. **Suche** nach `typeof(EditorPage)` → ersetze mit `typeof(EditorPage1)`
6. **Suche** nach `typeof(ProjectConfigurationPage)` → ersetze mit `typeof(EditorPage2)`
7. **Lösche** die Zeilen mit `new EditorPageViewModel(...)` und `new ProjectConfigurationPageViewModel(...)`
8. **Ersetze** Navigation-Parameter `editorViewModel` und `configViewModel` mit `ViewModel`
9. **Speichere** alle Dateien
10. **Build Solution** (Ctrl+Shift+B)

---

## 🎯 Erwartetes Ergebnis:

Nach den manuellen Änderungen sollte:
- ✅ Build erfolgreich sein
- ✅ EditorPage1 mit MainWindowViewModel öffnen
- ✅ EditorPage2 mit MainWindowViewModel öffnen
- ✅ Alle Bindings funktionieren
- ✅ Keine Wrapper-ViewModels mehr

---

## 📝 Hinweise:

- **EditorPage1** = Tabs-basierte Ansicht (Journeys, Workflows, Trains, etc.)
- **EditorPage2** = Tabellen-basierte Ansicht (Configuration)
- Beide verwenden jetzt **direkt** `MainWindowViewModel` ohne Wrapper
- Bindings sind jetzt `{Binding ViewModel.XXX}` statt `{Binding ViewModel.MainWindowViewModel.XXX}`

---

**Status**: Fast fertig! Nur noch 2-3 manuelle Änderungen in VS nötig.
