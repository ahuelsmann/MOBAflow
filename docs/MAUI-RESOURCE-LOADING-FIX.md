# MAUI Resource Loading Fix

## Problem

Die MAUI Android-App stürzte beim Start mit folgendem Fehler ab:

```
Position 8:5. StaticResource not found for key SurfaceBackground
```

## Root Cause

**.NET 10 + MAUI + UraniumUI** hatten ein Timing-Problem bei der XAML-Ressourcen-Kompilierung:

1. `StaticResource` wird zur **Compile-Zeit** aufgelöst
2. MAUI's XAML-Compiler konnte die Resource-Reihenfolge nicht korrekt verarbeiten
3. `MainPage.xaml` wurde initialisiert, **bevor** die Ressourcen aus `App.xaml` verfügbar waren
4. UraniumUI-Integration verschärfte das Problem

## Lösung

Die Lösung besteht aus **3 Komponenten**:

### ✅ 1. StaticResource → DynamicResource in MainPage.xaml

**Geändert:** `MAUI/MainPage.xaml`

```xaml
<!-- ❌ VORHER: Compile-Zeit-Auflösung -->
<ContentPage BackgroundColor="{StaticResource SurfaceBackground}">

<!-- ✅ NACHHER: Runtime-Auflösung -->
<ContentPage BackgroundColor="{DynamicResource SurfaceBackground}">
```

**Warum das funktioniert:**
- `DynamicResource` löst Ressourcen zur **Laufzeit** auf
- Umgeht XAML-Kompilierungs-Probleme
- Funktioniert auch wenn Ressourcen verzögert geladen werden

### ✅ 2. Inline-Farben in App.xaml

**Geändert:** `MAUI/App.xaml`

```xaml
<Application.Resources>
  <ResourceDictionary>
    <!-- ⚠️ CRITICAL: Define essential colors INLINE -->
    <Color x:Key="SurfaceBackground">#121212</Color>
    <Color x:Key="RailwayPrimary">#1976D2</Color>
    <!-- ... weitere kritische Farben ... -->
    
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
      <ResourceDictionary Source="Resources/Styles/DarkTheme.xaml" />
      <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

**Warum das hilft:**
- Inline-Definitionen werden **garantiert** geladen
- Fallback falls externe ResourceDictionaries nicht rechtzeitig laden
- Externe Dictionaries können diese Werte überschreiben

### ✅ 3. Programmatisches Resource-Loading in App.xaml.cs

**Geändert:** `MAUI/App.xaml.cs`

```csharp
public App(IServiceProvider services)
{
    _services = services;
    
    // ⚠️ CRITICAL: Load resources BEFORE InitializeComponent
    LoadEssentialResources();
    
    InitializeComponent();
}

private void LoadEssentialResources()
{
    var resources = Resources ?? new ResourceDictionary();
    
    resources["SurfaceBackground"] = Color.FromArgb("#121212");
    resources["RailwayPrimary"] = Color.FromArgb("#1976D2");
    // ... weitere Farben ...
    
    Resources = resources;
}

protected override Window CreateWindow(IActivationState? activationState)
{
    // ✅ Create MainPage AFTER App is initialized
    var mainPage = _services.GetRequiredService<MainPage>();
    return new Window(mainPage);
}
```

**Warum das wichtig ist:**
- Ressourcen werden **VOR** `InitializeComponent()` geladen
- `MainPage` wird **LAZY** erstellt (erst in `CreateWindow()`)
- Garantiert dass Ressourcen verfügbar sind wenn `MainPage.xaml` geladen wird

---

## Performance-Hinweise

### DynamicResource vs. StaticResource

**StaticResource** (Compile-Zeit):
- ✅ Schneller zur Laufzeit
- ✅ Type-safe
- ❌ Muss zur Compile-Zeit verfügbar sein
- ❌ Funktioniert nicht mit spätem Resource-Loading

**DynamicResource** (Laufzeit):
- ✅ Flexibel, funktioniert mit spätem Loading
- ✅ Unterstützt Theme-Wechsel zur Laufzeit
- ❌ Minimal langsamer (Lookup bei jedem Zugriff)
- ❌ Keine Compile-Zeit-Validierung

**Empfehlung:** 
- Verwenden Sie `DynamicResource` für **Farben und Styles**
- Verwenden Sie `StaticResource` für **konstante Werte** (z.B. Strings, Zahlen)

---

## Anwendung auf andere XAML-Dateien

Falls Sie weitere XAML-Dateien mit ähnlichen Problemen haben:

1. **Prüfen Sie, ob die Ressource in `App.xaml` inline definiert ist**
2. **Verwenden Sie `DynamicResource` statt `StaticResource`**
3. **Falls das nicht hilft:** Laden Sie die Ressource programmatisch in `LoadEssentialResources()`

---

## Bekannte Einschränkungen

- **UraniumUI Compatibility:** UraniumUI kann eigene Ressourcen überschreiben. Die Inline-Definitionen in `App.xaml` haben Vorrang.
- **Hot Reload:** DynamicResource funktioniert besser mit XAML Hot Reload als StaticResource
- **Performance:** Minimal langsamer als StaticResource (normalerweise vernachlässigbar)

---

## Siehe auch

- [MAUI Resource Dictionaries Dokumentation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/resource-dictionaries)
- [StaticResource vs DynamicResource](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/resource-dictionaries#stand-alone-resource-dictionaries)
- `docs/DI-INSTRUCTIONS.md` - Dependency Injection Guidelines
- `.github/copilot-instructions.md` - MOBAflow Coding Standards
