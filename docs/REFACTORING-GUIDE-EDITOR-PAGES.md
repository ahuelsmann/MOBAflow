# Refactoring Guide: Editor Pages Umbenennung

**Datum**: 2025-01-21  
**Ziel**: EditorPage → EditorPage1, ProjectConfigurationPage → EditorPage2, ViewModels entfernen

---

## 🎯 Schritt-für-Schritt Anleitung

### Schritt 1: EditorPage → EditorPage1

**In Visual Studio (Solution Explorer)**:

1. **Rechtsklick auf `WinUI\View\EditorPage.xaml`**
2. **Rename** → `EditorPage1.xaml`
3. Visual Studio fragt: "Rename all references?" → **JA**

**Manuelle Anpassungen** (falls VS nicht alle erwischt):

**Datei: `WinUI\View\EditorPage1.xaml`**
```xml
<!-- VORHER -->
<Page x:Class="Moba.WinUI.View.EditorPage">

<!-- NACHHER -->
<Page x:Class="Moba.WinUI.View.EditorPage1">
```

**Datei: `WinUI\View\EditorPage1.xaml.cs`**
```csharp
// VORHER
public sealed partial class EditorPage : Page
{
    public EditorPageViewModel ViewModel { get; }
    
    public EditorPage(EditorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += EditorPage_Loaded;
    }
    
    private void EditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        // ...
    }
}

// NACHHER
public sealed partial class EditorPage1 : Page
{
    public MainWindowViewModel ViewModel { get; }
    
    public EditorPage1(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += EditorPage1_Loaded;
    }
    
    private void EditorPage1_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("EditorPage1 loaded");
        Bindings.Update();
    }
    
    // ... rest bleibt gleich, aber:
    // ViewModel.MainWindowViewModel → ViewModel
}
```

**Datei: `WinUI\View\EditorPage1.xaml`**
```xml
<!-- Alle Bindings ändern von: -->
{Binding ViewModel.MainWindowViewModel.XXX}

<!-- Nach: -->
{Binding ViewModel.XXX}
```

**Beispiel**:
```xml
<!-- VORHER -->
<ListView ItemsSource="{Binding ViewModel.MainWindowViewModel.CurrentProjectViewModel.Journeys, Mode=OneWay}">

<!-- NACHHER -->
<ListView ItemsSource="{Binding ViewModel.CurrentProjectViewModel.Journeys, Mode=OneWay}">
```

---

### Schritt 2: ProjectConfigurationPage → EditorPage2

**In Visual Studio (Solution Explorer)**:

1. **Rechtsklick auf `WinUI\View\ProjectConfigurationPage.xaml`**
2. **Rename** → `EditorPage2.xaml`
3. Visual Studio fragt: "Rename all references?" → **JA**

**Manuelle Anpassungen**:

**Datei: `WinUI\View\EditorPage2.xaml`**
```xml
<!-- VORHER -->
<Page x:Class="Moba.WinUI.View.ProjectConfigurationPage"
      x:Name="Page">

<!-- NACHHER -->
<Page x:Class="Moba.WinUI.View.EditorPage2"
      x:Name="Page">
```

**Datei: `WinUI\View\EditorPage2.xaml.cs`**
```csharp
// VORHER
public sealed partial class ProjectConfigurationPage : Page
{
    public ProjectConfigurationPageViewModel? ViewModel { get; private set; }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is ProjectConfigurationPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
    }
}

// NACHHER
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
            System.Diagnostics.Debug.WriteLine($"   Journeys: {ViewModel.CurrentProjectViewModel?.Journeys.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ EditorPage2: No MainWindowViewModel in navigation parameter!");
        }
    }
}
```

**Datei: `WinUI\View\EditorPage2.xaml`**
```xml
<!-- Alle Bindings ändern von: -->
{Binding ViewModel.MainWindowViewModel.XXX}

<!-- Nach: -->
{Binding ViewModel.XXX}
```

---

### Schritt 3: MainWindow Navigation anpassen

**Datei: `WinUI\View\MainWindow.xaml.cs`**

```csharp
// VORHER (Editor)
case "editor":
    var editorViewModel = new SharedUI.ViewModel.EditorPageViewModel(ViewModel);
    ContentFrame.Navigate(typeof(EditorPage), editorViewModel);
    break;

// NACHHER (Editor)
case "editor":
    ContentFrame.Navigate(typeof(EditorPage1), ViewModel); // Direkt MainWindowViewModel!
    break;

// VORHER (Configuration)
case "configuration":
    var configViewModel = new SharedUI.ViewModel.ProjectConfigurationPageViewModel(ViewModel);
    ContentFrame.Navigate(typeof(ProjectConfigurationPage), configViewModel);
    break;

// NACHHER (Configuration)
case "configuration":
    ContentFrame.Navigate(typeof(EditorPage2), ViewModel); // Direkt MainWindowViewModel!
    break;
```

---

### Schritt 4: Wrapper-ViewModels löschen

**In Visual Studio (Solution Explorer)**:

1. **Lösche** `SharedUI\ViewModel\EditorPageViewModel.cs`
2. **Lösche** `SharedUI\ViewModel\ProjectConfigurationPageViewModel.cs`

---

### Schritt 5: Build & Test

```powershell
# Build
dotnet build

# Erwartung: Success, 0 Errors
```

**Runtime-Test**:
1. Starte WinUI App
2. Navigiere zu **"Editor"** → Sollte EditorPage1 laden
3. Navigiere zu **"Configuration"** → Sollte EditorPage2 laden
4. Teste Bearbeitung in beiden Pages

---

## 🔧 Binding-Änderungen (Übersicht)

### EditorPage1.xaml

**Suchen & Ersetzen** (Regex in Visual Studio):
```
Suchen:    {Binding ViewModel\.MainWindowViewModel\.
Ersetzen:  {Binding ViewModel.
```

**Beispiele**:
```xml
<!-- VORHER -->
<Button Command="{Binding ViewModel.MainWindowViewModel.AddJourneyCommand}" />
<ListView ItemsSource="{Binding ViewModel.MainWindowViewModel.CurrentProjectViewModel.Journeys}" />

<!-- NACHHER -->
<Button Command="{Binding ViewModel.AddJourneyCommand}" />
<ListView ItemsSource="{Binding ViewModel.CurrentProjectViewModel.Journeys}" />
```

### EditorPage2.xaml

**Gleiche Änderung**:
```
Suchen:    {Binding ViewModel\.MainWindowViewModel\.
Ersetzen:  {Binding ViewModel.
```

---

## ✅ Verifizierung

### Checklist

- [ ] EditorPage → EditorPage1 umbenannt
- [ ] ProjectConfigurationPage → EditorPage2 umbenannt
- [ ] EditorPageViewModel.cs gelöscht
- [ ] ProjectConfigurationPageViewModel.cs gelöscht
- [ ] MainWindow.xaml.cs Navigation angepasst
- [ ] Alle Bindings in EditorPage1.xaml angepasst
- [ ] Alle Bindings in EditorPage2.xaml angepasst
- [ ] Build erfolgreich
- [ ] Runtime-Test erfolgreich

---

## 📊 Vorher/Nachher Vergleich

### Vorher
```
Navigation:
MainWindow → EditorPage → EditorPageViewModel → MainWindowViewModel
MainWindow → ProjectConfigurationPage → ProjectConfigurationPageViewModel → MainWindowViewModel

ViewModels: 2 Wrapper-ViewModels
Binding-Pfade: Verschachtelt (.ViewModel.MainWindowViewModel.XXX)
```

### Nachher
```
Navigation:
MainWindow → EditorPage1 → MainWindowViewModel (direkt!)
MainWindow → EditorPage2 → MainWindowViewModel (direkt!)

ViewModels: 0 Wrapper-ViewModels ✅
Binding-Pfade: Direkt (.ViewModel.XXX)
```

---

## 🎯 Benefits

1. ✅ **Weniger Code** (-200 Zeilen, 2 ViewModels entfernt)
2. ✅ **Einfachere Bindings** (kein .MainWindowViewModel mehr)
3. ✅ **Konsistenter** (alle Pages verwenden gleiche ViewModel-Instanz)
4. ✅ **Wartbarer** (keine Wrapper-Logik mehr)
5. ✅ **Klarer** (Namen zeigen Kongruenz: EditorPage1 ≈ EditorPage2)

---

**Status**: ⏳ Warte auf manuelle Ausführung in Visual Studio  
**Aufwand**: ~30-45 Minuten  
**Risiko**: Low (nur Rename + ViewModel-Entfernung)
