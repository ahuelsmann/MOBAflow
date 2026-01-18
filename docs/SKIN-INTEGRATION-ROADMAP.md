# Skin Integration Architecture

> **Concept:** Embed original TrainControlPage & SignalBoxPage as "Skins" into Page2 variants  
> **Goal:** Consolidate duplicate pages into single, theme+layout-aware UIs  
> **Status:** 🔴 PLANNED (Week 2-3)

---

## 🎨 Vision: Unified UI System

### Current State (Problematic)

```
NavigationView contains:
├── Train Control           (original, light/dark only)
├── Train Control 2         (new, 6 themes + skin-selector)
├── Signal Box              (original, light/dark only)
├── Signal Box 2            (new, 6 themes + skin-selector)
```

**Problems:**
- ❌ Code duplication (UI logic exists in 2 places)
- ❌ Confusing navigation (which should user pick?)
- ❌ Maintenance burden (bug fix = update 2 pages)
- ❌ Limited customization

### Future State (Proposed)

```
NavigationView contains:
├── Train Control           (= Page2, selectable layout + theme)
├── Signal Box              (= Page2, selectable layout + theme)

Layout + Theme Selector (unified):
├── Theme Dropdown:
│   ├── Modern
│   ├── Classic
│   ├── Dark
│   ├── ESU CabControl
│   ├── Roco Z21
│   └── Märklin CS
│
└── Layout Dropdown (context-sensitive):
    ├── Original      (← from old TrainControlPage / SignalBoxPage)
    ├── Modern        (← current Page2 modern skin)
    ├── ESU           (← ESU CabControl variant, if Theme = ESU)
    ├── Märklin       (← Märklin CS variant, if Theme = Märklin)
    └── Z21           (← Z21 variant, if Theme = Z21)
```

**Benefits:**
- ✅ Single source of truth
- ✅ Cleaner navigation
- ✅ Easier maintenance
- ✅ More customization options
- ✅ Theme + Layout independent (mix & match)

---

## 🏗️ Implementation Strategy

### Phase 1: Create "Original" Theme

**Goal:** Make old hard-coded light/dark rendering data-driven

```csharp
// ThemeResourceBuilder.cs - NEW METHOD
public static ResourceDictionary BuildOriginalTheme()
{
    var dict = new ResourceDictionary();
    
    // Extract from SignalBoxPage.xaml + TrainControlPage.xaml
    dict["OriginalHeaderBackground"] = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
    dict["OriginalHeaderForeground"] = new SolidColorBrush(Colors.Black);
    
    dict["OriginalPanelBackground"] = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
    dict["OriginalControlBackground"] = new SolidColorBrush(Colors.White);
    
    // Dark variant
    dict["OriginalHeaderBackgroundDark"] = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
    dict["OriginalHeaderForegroundDark"] = new SolidColorBrush(Colors.White);
    
    dict["OriginalPanelBackgroundDark"] = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
    dict["OriginalControlBackgroundDark"] = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
    
    return dict;
}

// ApplicationTheme.cs - ADD TO ENUM
public enum ApplicationTheme
{
    Modern = 0,
    Classic = 1,
    Dark = 2,
    ESU = 3,
    Z21 = 4,
    Märklin = 5,
    Original = 6  // ← NEW!
}
```

### Phase 2: Add Layout Enum

```csharp
// Enums/TrainControlLayout.cs - NEW FILE
public enum TrainControlLayout
{
    Original,   // From old TrainControlPage.xaml
    Modern,     // Current Page2 default
    ESU,        // ESU CabControl variant (TrainControlPage3 concept)
    Märklin     // Märklin CS variant (TrainControlPage4 concept)
}

// Enums/SignalBoxLayout.cs - NEW FILE
public enum SignalBoxLayout
{
    Original,   // From old SignalBoxPage.cs
    Modern,     // Current Page2 default
    ESU,        // ESU variant
    Z21         // Roco Z21 variant
}
```

### Phase 3: Enhance Page2 Classes

#### **SignalBoxPage2 - Dual Selector**

```csharp
public sealed class SignalBoxPage2 : SignalBoxPageBase
{
    [ObservableProperty]
    private ApplicationTheme _selectedTheme = ApplicationTheme.Modern;
    
    [ObservableProperty]
    private SignalBoxLayout _selectedLayout = SignalBoxLayout.Modern;
    
    private readonly IThemeProvider _themeProvider;
    
    // Called when theme or layout changes
    partial void OnSelectedThemeChanged(ApplicationTheme value)
    {
        _themeProvider.SetTheme(value);
        ApplyThemeColors();
    }
    
    partial void OnSelectedLayoutChanged(SignalBoxLayout value)
    {
        RefreshLayout();
    }
    
    // Render based on layout
    private UIElement CreateElementVisual(SignalBoxElement element)
    {
        return SelectedLayout switch
        {
            SignalBoxLayout.Original => RenderElementAsOriginal(element),
            SignalBoxLayout.Modern => RenderElementAsModern(element),
            SignalBoxLayout.ESU => RenderElementAsESU(element),
            SignalBoxLayout.Z21 => RenderElementAsZ21(element),
            _ => RenderElementAsModern(element)
        };
    }
    
    // ← NEW: Original layout (copy from SignalBoxPage.cs)
    private UIElement RenderElementAsOriginal(SignalBoxElement element)
    {
        // ... copy logic from old SignalBoxPage.cs BuildTrackElement() ...
        // Use "Original" theme colors
    }
    
    // ← EXISTING: Modern layout
    private UIElement RenderElementAsModern(SignalBoxElement element)
    {
        // ... existing logic from Page2 ...
    }
    
    // Selector UI in header
    protected override Border BuildHeader()
    {
        var baseHeader = base.BuildHeader(); // ← from Page2
        
        // Add Layout Selector ComboBox
        var layoutCombo = new ComboBox
        {
            ItemsSource = Enum.GetValues<SignalBoxLayout>(),
            SelectedItem = SelectedLayout
        };
        layoutCombo.SelectionChanged += (s, e) =>
        {
            SelectedLayout = (SignalBoxLayout)layoutCombo.SelectedItem;
        };
        
        // Insert into header toolbar
        // ...
        
        return headerWithLayoutSelector;
    }
}
```

#### **TrainControlPage2 - Similar Pattern**

```csharp
public sealed class TrainControlPage2 : ObservableObject
{
    [ObservableProperty]
    private ApplicationTheme _selectedTheme = ApplicationTheme.Modern;
    
    [ObservableProperty]
    private TrainControlLayout _selectedLayout = TrainControlLayout.Modern;
    
    // ...similar structure as SignalBoxPage2...
    
    private UIElement BuildTrainControlUI()
    {
        return SelectedLayout switch
        {
            TrainControlLayout.Original => BuildOriginalLayout(),
            TrainControlLayout.Modern => BuildModernLayout(),
            TrainControlLayout.ESU => BuildESULayout(),
            TrainControlLayout.Märklin => BuildMärklinLayout(),
            _ => BuildModernLayout()
        };
    }
    
    private UIElement BuildOriginalLayout()
    {
        // Copy UI construction from old TrainControlPage.xaml
        // Rebuild as C# Grid/StackPanel/TextBox hierarchy
        // Use "Original" theme colors
    }
}
```

### Phase 4: Update NavigationRegistry

```csharp
// Common/Registry/NavigationRegistry.cs

public static void RegisterPages(IServiceCollection services)
{
    // REMOVE these entries:
    // pages.Add(new PageDescriptor(Tag: "TrainControl", ...));
    // pages.Add(new PageDescriptor(Tag: "SignalBox", ...));
    
    // KEEP these (now with embedded layouts):
    pages.Add(new PageDescriptor(
        Tag: "TrainControl",
        Title: "Train Control (Theme + Layout)",
        Icon: Symbol.Directions,
        PageType: typeof(TrainControlPage2),  // ← Point to Page2
        Description: "Locomotive control with theme & layout options"));
    
    pages.Add(new PageDescriptor(
        Tag: "SignalBox",
        Title: "Signal Box (Theme + Layout)",
        Icon: Symbol.SlideShow,
        PageType: typeof(SignalBoxPage2),  // ← Point to Page2
        Description: "Electronic signal box with theme & layout options"));
}
```

### Phase 5: Update DI Registrations

```csharp
// App.xaml.cs or MauiProgram.cs

// REMOVE:
services.AddTransient<TrainControlPage>();
services.AddTransient<SignalBoxPage>();

// KEEP (no change needed):
services.AddTransient<TrainControlPage2>();
services.AddTransient<SignalBoxPage2>();

// App.xaml.cs: Navigation setup
private async void OnNavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    var item = args.InvokedItemContainer.Tag?.ToString();
    switch (item)
    {
        case "TrainControl":
            ContentFrame.Navigate(typeof(TrainControlPage2));  // ← was TrainControlPage
            break;
        case "SignalBox":
            ContentFrame.Navigate(typeof(SignalBoxPage2));  // ← was SignalBoxPage
            break;
    }
}
```

### Phase 6: Delete Old Pages

```powershell
# Delete these files:
rm WinUI/View/TrainControlPage.xaml
rm WinUI/View/TrainControlPage.xaml.cs
rm WinUI/View/SignalBoxPage.xaml
rm WinUI/View/SignalBoxPage.cs

# Verify project files are updated
# Edit WinUI/WinUI.csproj: Remove <Compile Include="View/TrainControlPage.xaml.cs" />
```

---

## 📊 Layout Compatibility Matrix

| Layout | TrainControl | SignalBox | Theme-Dependent? |
|--------|---|---|---|
| **Original** | ✅ | ✅ | Dark/Light only |
| **Modern** | ✅ | ✅ | All 6 themes |
| **ESU** | ✅ | ✅ | Optimized for ESU theme |
| **Märklin** | ✅ | ❓ | Optimized for Märklin theme |
| **Z21** | ❓ | ✅ | Optimized for Z21 theme |

**Logic:** Allow all combinations, but visually recommend:
- Theme "Original" → Layout "Original"
- Theme "Modern" → Layout "Modern"
- Theme "ESU" → Layout "ESU" (but allow Modern)
- Etc.

---

## 🧪 Testing Plan

### Unit Tests

```csharp
// WinUI.Tests/TrainControlPage2Tests.cs
[TestClass]
public class TrainControlPage2LayoutTests
{
    [TestMethod]
    public void SettingLayoutToOriginal_ShouldRenderOriginalUI()
    {
        var page = new TrainControlPage2(...);
        page.SelectedLayout = TrainControlLayout.Original;
        
        // Assert: specific elements from original appear
        Assert.IsNotNull(page.FindName("OriginalSlider"));
    }
    
    [TestMethod]
    public void SettingLayoutToModern_ShouldRenderModernUI()
    {
        var page = new TrainControlPage2(...);
        page.SelectedLayout = TrainControlLayout.Modern;
        
        // Assert: modern layout elements appear
        Assert.IsNotNull(page.FindName("ModernTachometer"));
    }
    
    [TestMethod]
    [DataRow(ApplicationTheme.Modern, TrainControlLayout.Original)]
    [DataRow(ApplicationTheme.ESU, TrainControlLayout.ESU)]
    public void ThemeAndLayoutCombinations_ShouldNotCrash(
        ApplicationTheme theme, TrainControlLayout layout)
    {
        var page = new TrainControlPage2(...);
        page.SelectedTheme = theme;
        page.SelectedLayout = layout;
        
        // Assert: page renders without exception
        page.Measure(new Size(1200, 800));
        Assert.AreEqual(1200, page.DesiredSize.Width);
    }
}
```

### Manual Testing

1. **Open TrainControlPage2**
   - [ ] Selector shows "Modern" layout
   - [ ] Select "Original" → UI switches to old style
   - [ ] Change theme → Colors update
   - [ ] Functionality preserved

2. **Open SignalBoxPage2**
   - [ ] Selector shows "Modern" layout
   - [ ] Select "Original" → Grid renders in old style
   - [ ] Elements draggable/rotateable in all layouts
   - [ ] Signals display correctly in all layouts

3. **Responsive Test (Resize Window)**
   - [ ] Original layout: responsive at 640px? (or warn user)
   - [ ] Modern layout: responsive ✅
   - [ ] No crashes on resize

4. **Theme Switching**
   - [ ] All 6 themes work with Original layout
   - [ ] All 6 themes work with Modern layout
   - [ ] Colors apply correctly

---

## 📋 Rollout Checklist

- [ ] "Original" theme created in ThemeResourceBuilder
- [ ] Layout enums created (TrainControlLayout, SignalBoxLayout)
- [ ] SignalBoxPage2 refactored (dual selector + layout rendering)
- [ ] TrainControlPage2 refactored (dual selector + layout rendering)
- [ ] Unit tests written for all layout combinations
- [ ] Manual testing completed (all breakpoints)
- [ ] NavigationRegistry updated (points to Page2)
- [ ] DI registrations updated
- [ ] Old TrainControlPage/SignalBoxPage deleted
- [ ] Project file cleaned (.csproj)
- [ ] Navigation items renamed (remove "2")
- [ ] Release notes documented

---

## 🎯 Success Criteria

✅ **When complete:**
- Single navigation entry for "Train Control" (no "Train Control 2")
- Single navigation entry for "Signal Box" (no "Signal Box 2")
- Layout + Theme selectors visible in both pages
- Original UI still available (as one layout option)
- All responsive breakpoints work
- No code duplication
- Tests pass: 100% coverage

---

## 📚 Related Files

| File | Purpose |
|------|---------|
| `ThemeResourceBuilder.cs` | Theme rendering (add "Original" theme) |
| `SignalBoxPage2.cs` | Add layout selector + rendering logic |
| `TrainControlPage2.cs` | Add layout selector + rendering logic |
| `NavigationRegistry.cs` | Update page references |
| `App.xaml.cs` | Update DI + navigation logic |

---

**Next Step:** Start Phase 1 → Create "Original" theme  
**Estimated Effort:** 8-12 hours total  
**Target Completion:** End of Week 2-3
