# WinUI 3 Best Practices - Roadmap für Steps 4-12

> Detaillierter Implementierungsplan für die verbleibenden WinUI 3 Best Practices nach VSM

---

## 📋 Übersicht: Steps 4-12 (Priorisiert)

| Step | Aufgabe | Aufwand | Komplexität | Nutzen |
|------|---------|--------|------------|--------|
| **4** | **Refactor Navigation Pattern nach Shell-Standard** | 2-3h | Mittel | Code-Reduktion, bessere Wartbarkeit |
| **5** | **UI Thread Dispatching Audit** | 2-3h | Mittel | Stabilität, keine Cross-Thread-Crashes |
| **6** | **Fluent Design System konsistent** | 3-4h | Hoch | Professionelles Erscheinungsbild |
| **7** | **DI-Registration überprüfen** | 1-2h | Niedrig | Cleancode, richtige Service-Lifetimes |
| **8** | **ItemsControl Pattern dokumentieren** | 1h | Niedrig | Schnelle Referenz |
| **9** | **File I/O Pattern überprüfen** | 1h | Niedrig | Sicherheit, Best Practices |
| **10** | **Keyboard Shortcuts konsistent** | 2h | Mittel | User Experience, Accessibility |
| **11** | **Window Management Audit** | 1h | Niedrig | Professionelle App-Init |
| **12** | **Finale winui.instructions.md Erweiterung** | 1h | Niedrig | Dokumentation komplett |

---

## 🎯 STEP 4: Refactor Navigation Pattern nach Shell-Standard

### Was bedeutet "Shell-Standard"?

MOBAflow hat bereits ein Shell-System implementiert:
- ✅ `INavigationService` in `SharedUI/Shell/`
- ✅ `IPageFactory` für DI-basierte Page-Erstellung
- ✅ `PageDescriptor` für Page-Metadaten
- ⚠️ Verwendung in Pages: **NOCH NICHT VOLLSTÄNDIG**

### Aufgabe:

**Überprüfe alle Pages und stelle sicher, dass sie das Shell-Pattern verwenden:**

```csharp
// ❌ ALTE PATTERN (hardcoded Navigation):
private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    var item = args.InvokedItemContainer.Tag?.ToString();
    switch (item)
    {
        case "Journeys":
            ContentFrame.Navigate(typeof(JourneysPage));  // ❌ Direkt, kein INavigationService
            break;
    }
}

// ✅ NEUE PATTERN (Shell-basiert):
public class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    
    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
    
    private void NavigationViewItem_Invoked(string tag)
    {
        // Nutze INavigationService für konsistente Navigation mit History!
        _navigationService.NavigateTo(tag);
    }
}
```

### Konkrete Aufgaben:

- [ ] Überprüfe `MainWindow.xaml.cs` - nutzt es `INavigationService`?
- [ ] Überprüfe alle `Page` Code-Behind - Direct Frame.Navigate() → `INavigationService`?
- [ ] Überprüfe ViewModels - haben sie `INavigationService` im Constructor?
- [ ] Tests: Gehe durch alle Navigation-Pfade und überprüfe History/Back-Button

### Dateien zu überprüfen:

```
WinUI/View/MainWindow.xaml.cs       ← Shell/Navigation
WinUI/View/*.xaml.cs                ← Alle Pages
SharedUI/ViewModel/MainWindowViewModel.cs  ← Navigation Commands
```

---

## 🎯 STEP 5: UI Thread Dispatching Audit

### Was ist das Problem?

WinUI ist **Single-Threaded** wie alle UI-Frameworks. Background-Tasks (Z21 Events, JSON Loading, Calculations) dürfen **nicht direkt** UI-Properties updaten.

### Aufgabe:

**Finde alle Background→UI Updates und stelle sicher, dass sie `DispatcherQueue` verwenden:**

```csharp
// ❌ FALSCH (Cross-thread Exception):
private void OnZ21Event(object sender, Z21EventArgs e)
{
    ViewModel.Speed = e.Speed;  // ❌ Crash wenn von Background-Thread aufgerufen!
}

// ✅ RICHTIG (DispatcherQueue):
private readonly DispatcherQueue _dispatcher;

private void OnZ21Event(object sender, Z21EventArgs e)
{
    _dispatcher.TryEnqueue(() =>
    {
        ViewModel.Speed = e.Speed;  // ✅ Sicher auf UI-Thread
    });
}
```

### Konkrete Aufgaben:

- [ ] Finde alle Event-Handler die UI updaten:
  - Z21 Events (Connection, Speed, Feedback)
  - Journey Events (Lap-Counter, Distance)
  - Backend Service Events
- [ ] Überprüfe ViewModels auf `PropertyChanged` aus Background-Threads
- [ ] Überprüfe async/await Patterns (richtig `ConfigureAwait(false)`?)
- [ ] Tests: Starte Z21-Verbindung und überprüfe auf Crashes

### Dateien zu überprüfen:

```
SharedUI/ViewModel/TrainControlViewModel.cs        ← Speed Updates
SharedUI/ViewModel/JourneyViewModel.cs             ← Lap Counter
SharedUI/ViewModel/MainWindowViewModel.Z21.cs    ← Z21 Events
Backend/Service/Z21.cs                            ← Events
```

### Expected Result:

Alle Event-Handler, die UI-Properties ändern, nutzen `DispatcherQueue.TryEnqueue()`.

---

## 🎯 STEP 6: Fluent Design System konsistent implementieren

### Was ist Fluent Design?

Microsoft's moderne Design Language für WinUI mit:
- Akryl-Effekte (Hintergrund-Blur)
- Fluent Icons (Segoe MDL2 Assets)
- Theme-basierte Farben (Light/Dark)
- Smooth Transitions & Animations
- Konsistente Spacing & Typography

### Aufgabe:

**Überprüfe alle UI-Elemente und stelle sicher, dass sie Fluent Design verwenden:**

```xaml
<!-- ❌ FALSCH (hardcoded Colors): -->
<Grid Background="LightGray">
    <TextBlock Foreground="Black" FontSize="14" Text="Title" />
    <Button Background="Blue">Click Me</Button>
</Grid>

<!-- ✅ RICHTIG (Theme Resources): -->
<Grid Background="{ThemeResource LayerFillColorDefaultBrush}">
    <TextBlock 
        Foreground="{ThemeResource TextFillColorPrimaryBrush}" 
        Style="{StaticResource SubtitleTextBlockStyle}"
        Text="Title" />
    <Button Content="Click Me" />  <!--  Default Button Style  -->
</Grid>
```

### Konkrete Aufgaben:

- [ ] Überprüfe alle `Background` Attribute:
  - Card-Background → `{ThemeResource CardBackgroundFillColorDefaultBrush}`
  - Layer-Background → `{ThemeResource LayerFillColorDefaultBrush}`
  - Acrylic → `{ThemeResource AcrylicBackgroundFillColorDefaultBrush}`
  
- [ ] Überprüfe alle `Foreground` Attribute:
  - Primary Text → `{ThemeResource TextFillColorPrimaryBrush}`
  - Secondary Text → `{ThemeResource TextFillColorSecondaryBrush}`
  - Tertiary Text → `{ThemeResource TextFillColorTertiaryBrush}`

- [ ] Überprüfe Icons:
  - Nutzen sie `FontIcon` mit Segoe MDL2 Assets?
  - Konsistent? (Alle Train-Icons gleich, alle Save-Icons gleich?)

- [ ] Überprüfe TextBlock Styles:
  - `TitleTextBlockStyle` für Haupttitel
  - `SubtitleTextBlockStyle` für Untertitel
  - `CaptionTextBlockStyle` für kleine Texte

- [ ] Light/Dark Theme:
  - Funktioniert Theme Toggle?
  - Alle Colors angepasst?

### Dateien zu überprüfen:

```
WinUI/View/*.xaml                          ← Alle Pages
WinUI/Resources/Colors.xaml               ← Color Definitions
WinUI/Resources/Styles.xaml               ← Style Definitions
WinUI/App.xaml                            ← Global Resources
```

---

## 🎯 STEP 7: DI-Registration überprüfen

### Aufgabe:

**Überprüfe `App.xaml.cs` und stelle sicher, dass die Service-Lifetimes korrekt sind:**

```csharp
// Singleton: Eine Instanz für die ganze App
services.AddSingleton<IZ21, Z21>();

// Transient: Neue Instanz für jeden Zugriff
services.AddTransient<SomeViewModel>();

// Scoped: Eine Instanz pro Scope (z.B. pro Page Navigation)
services.AddScoped<MyService>();
```

### Konkrete Aufgaben:

- [ ] Überprüfe alle Services:
  - **Singleton:** Backend Services (Z21, Solution), ViewModels (wenn sie lange-lived sind)
  - **Transient:** ViewModels (wenn sie pro Page created werden)
  - **Scoped:** Temporary Services

- [ ] Überprüfe CircularDependencies:
  - Verursacht `ServiceA` → `ServiceB` → `ServiceA` ein Problem?

- [ ] Tests:
  - Kann die App starten?
  - Gibt es DI-Fehler in der Visual Studio Debug-Ausgabe?

### Dateien zu überprüfen:

```
WinUI/App.xaml.cs               ← DI Registration
```

---

## 🎯 STEP 8: ItemsControl Pattern dokumentieren

### Aufgabe:

**Überprüfe bestehende ItemsControl/ListView Nutzungen und dokumentiere Best Practices:**

```xaml
<!-- ✅ ITEMSCONTROL: Einfache Read-Only Listen (z.B. Toolbox) -->
<ItemsControl ItemsSource="{x:Bind ViewModel.Presets}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel />  <!-- Standard: Vertikal stacked -->
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Button Content="{Binding Name}" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- ✅ LISTVIEW: Selektierbare Listen mit Fokus -->
<ListView ItemsSource="{x:Bind ViewModel.Workflows}" SelectionMode="Single">
    <ListView.ItemTemplate>
        <DataTemplate>
            <Grid Padding="12" BorderThickness="0,0,0,1" BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}">
                <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>

<!-- ✅ GRIDVIEW: 2D Grid (z.B. Track Elements) -->
<GridView ItemsSource="{x:Bind ViewModel.TrackElements}">
    <GridView.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Rows="4" Columns="4" />
        </ItemsPanelTemplate>
    </GridView.ItemsPanel>
    <GridView.ItemTemplate>
        <DataTemplate>
            <Button Width="60" Height="60" Content="{Binding Icon}" />
        </DataTemplate>
    </GridView.ItemTemplate>
</GridView>
```

### Konkrete Aufgaben:

- [ ] Finde alle `ItemsControl`, `ListView`, `GridView` usages
- [ ] Überprüfe ItemsPanelTemplate (Virtualization enabled?)
- [ ] Überprüfe ItemTemplate (binding correct?)
- [ ] Dokumentiere in winui.instructions.md:
  - **ItemsControl:** Wann verwenden? (Einfache, nicht-interaktive Listen)
  - **ListView:** Wann verwenden? (Selektierbare Elemente, Focus)
  - **GridView:** Wann verwenden? (2D Grid Layouts)

### Dateien:

```
WinUI/View/*.xaml  ← Suche nach ItemsControl/ListView/GridView
```

---

## 🎯 STEP 9: File I/O Pattern überprüfen

### Aufgabe:

**Überprüfe alle FilePicker Nutzungen und stelle sicher, dass sie sicher sind:**

```csharp
// ✅ RICHTIG: Window Handle initialisiert + Async/Await
public async Task SaveFileAsync()
{
    var savePicker = new FileSavePicker();
    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WindowHandle);
    savePicker.SuggestedFileName = "data.json";
    savePicker.FileTypeChoices.Add("JSON", new[] { ".json" });
    
    var file = await savePicker.PickSaveFileAsync();  // ← Async!
    if (file != null)
    {
        await FileIO.WriteTextAsync(file, jsonContent);
    }
}
```

### Konkrete Aufgaben:

- [ ] Finde alle `FilePicker` usages
- [ ] Überprüfe `InitializeWithWindow` - ist es vorhanden?
- [ ] Überprüfe `async/await` - wird nicht blockiert?
- [ ] Überprüfe Fehlerbehandlung - wird es abgefangen?

### Dateien:

```
WinUI/View/*.xaml.cs   ← Suche nach "FilePicker"
```

---

## 🎯 STEP 10: Keyboard Shortcuts konsistent implementieren

### Aufgabe:

**Überprüfe alle wichtigen Keyboard-Shortcuts und stelle sicher, dass sie konsistent sind:**

```xaml
<!-- ✅ Standard Shortcuts -->
<Button Content="Save">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="S" Modifiers="Control" />  <!-- Ctrl+S -->
    </Button.KeyboardAccelerators>
</Button>

<Button Content="Open">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="O" Modifiers="Control" />  <!-- Ctrl+O -->
    </Button.KeyboardAccelerators>
</Button>

<Button Content="Delete">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="Delete" />  <!-- Delete key -->
    </Button.KeyboardAccelerators>
</Button>
```

### Standard Shortcuts für MOBAflow:

```
Ctrl+N  → New Project
Ctrl+O  → Open Project
Ctrl+S  → Save Project
Ctrl+Z  → Undo
Ctrl+Y  → Redo
F5      → Play/Run (Train Control)
Space   → Emergency Stop (Alternative)
Escape  → Cancel/Close Dialog
F1      → Help
```

### Konkrete Aufgaben:

- [ ] Überprüfe ob diese Shortcuts implementiert sind:
  - Ctrl+S (Save)
  - Ctrl+O (Open)
  - F5 (Play)
  - Escape (Cancel)
  
- [ ] Überprüfe Consistency:
  - Alle Button-ähnlichen Controls haben Shortcuts?
  - Konflikte mit System-Shortcuts?

### Dateien:

```
WinUI/View/*.xaml       ← Button KeyboardAccelerators
WinUI/View/*.xaml.cs   ← KeyDown Event Handler
```

---

## 🎯 STEP 11: Window Management Audit

### Aufgabe:

**Überprüfe `MainWindow.xaml.cs` und stelle sicher, dass die Window-Init richtig ist:**

```csharp
// ✅ RICHTIG:
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set title (wichtig für Alt+Tab identification)
        Title = "MOBAflow - Railway Automation";
        
        // Set initial size (sollte vernünftig sein, nicht 0,0!)
        AppWindow.Resize(new SizeInt32(1200, 800));
        
        // Center window (professioneller Look)
        CenterWindow();
    }
    
    private void CenterWindow()
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var centeredPosition = new PointInt32(
            (workArea.Width - AppWindow.Size.Width) / 2,
            (workArea.Height - AppWindow.Size.Height) / 2
        );
        AppWindow.Move(centeredPosition);
    }
}
```

### Konkrete Aufgaben:

- [ ] Überprüfe MainWindow Title - ist es aussagekräftig?
- [ ] Überprüfe Startup Size - ist sie angemessen (z.B. 1200x800)?
- [ ] Überprüfe Centering - wird Fenster zentriert?
- [ ] Optional: Window State Persistence (speichere Size/Position beim Exit)

### Dateien:

```
WinUI/View/MainWindow.xaml.cs
```

---

## 🎯 STEP 12: Finale winui.instructions.md Erweiterung

### Aufgabe:

**Aktualisiere `winui.instructions.md` mit:**

- [ ] Architecture Decision Log (ADL)
  - Warum VSM statt Custom Controls?
  - Warum INavigationService?
  - Warum DispatcherQueue für all UI updates?

- [ ] Final Checklist (erweitern)
  - Alle Steps 4-12 geprüft?

- [ ] Links & Verweise
  - Zu winui3-vsm-detailed-guide.md
  - Zu Shell/Navigation Interfaces
  - Zu Best Practices

### Dateien:

```
.github/instructions/winui.instructions.md
```

---

## 🚀 Ausführungsreihenfolge empfohlen:

**Erste Woche:**
1. Step 4: Navigation Pattern (2-3h)
2. Step 5: UI Thread Dispatching (2-3h)

**Zweite Woche:**
3. Step 6: Fluent Design (3-4h)
4. Step 7: DI-Registration (1-2h)

**Dritte Woche:**
5. Step 8-11: Quick Wins (1-2h pro Step)
6. Step 12: Dokumentation (1h)

**Gesamtaufwand:** ~16-20 Stunden über 3 Wochen

---

## 💡 Debugging-Tipps für jeden Step

### Step 4 (Navigation):
- Nutze Breakpoints in INavigationService
- Überprüfe History Stack im Debug

### Step 5 (DispatcherQueue):
- Output Window auf "Exceptions" filtern
- Thread ID in Debug prüfen

### Step 6 (Fluent Design):
- Toggle Light/Dark Theme und prüfe Farben
- Windows > Live Property Explorer nutzen

### Step 7 (DI):
- "DI Diagnostics" im Output Window
- ServiceProvider.GetService() Breakpoints

### Step 10 (Keyboard):
- KeyboardAccelerator aktiviert? (richtig geschrieben?)
- Verursacht Konfl ikte mit Menu Shortcuts?

---

## ✅ Success Criteria

Wenn alle 12 Steps fertig sind:

- ✅ Responsive Layouts (VSM) alle wichtigen Pages
- ✅ Konsistente Navigation (INavigationService)
- ✅ Safe UI Updates (DispatcherQueue)
- ✅ Fluent Design System durchgängig
- ✅ Saubere DI-Registration
- ✅ Intuitives Keyboard-Shortcut System
- ✅ Professionelle Window-Init
- ✅ Komplett dokumentiert

