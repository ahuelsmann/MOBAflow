# DI Pattern Consistency & Existing Patterns Checklist

> **Session:** Dec 18, 2025 | FeedbackPointsPage Refactoring  
> **Key Lesson:** Check existing patterns first - never create new approaches!

---

## ✅ CRITICAL RULE: Existing Patterns Checklist (BEFORE ANY IMPLEMENTATION)

**Before implementing ANY new feature (Page, ViewModel, Service), execute this checklist:**

### 1️⃣ **Does this feature type already exist?**

Search for similar implementations in the codebase.

**Example:**
- ❓ "Need a new page for managing FeedbackPoints"
- ✅ **Check:** Existing Pages (JourneysPage, WorkflowsPage, SettingsPage, TrackPlanEditorPage)
- ✅ **Pattern:** All inject `MainWindowViewModel` in constructor
- ✅ **Decision:** Replicate that pattern

### 2️⃣ **What pattern does it follow?**

Analyze existing implementations:
- **All WinUI Pages use:** Constructor injection of `MainWindowViewModel`
- **All WinUI Pages have:** `public MainWindowViewModel ViewModel { get; }` property
- **All WinUI Pages use in XAML:** `DataContext="{x:Bind ViewModel}"`
- **All Pages are registered as:** `services.AddTransient<View.PageName>()`
- **All Pages are navigated via:** `_serviceProvider.GetRequiredService<PageName>()`

### 3️⃣ **Could I replicate this pattern?**

✅ **DO:**
- Reuse `MainWindowViewModel` for new pages
- Use simple `GetRequiredService<PageName>()` navigation
- Bind UI directly to collections on MainWindowViewModel

❌ **DON'T:**
- Create custom factory methods for page creation
- Create separate PageViewModels for simple pages
- Use workarounds like `ToObservableCollection()` extensions
- Create inconsistent navigation patterns

### 4️⃣ **Check these source files for patterns:**

| Pattern | File(s) | Purpose |
|---------|---------|---------|
| **Page Template** | WinUI/View/JourneysPage.xaml.cs, WorkflowsPage.xaml.cs | Constructor pattern, ViewModel injection |
| **Navigation Logic** | WinUI/Service/NavigationService.cs | How pages are created and navigated to |
| **DI Setup** | WinUI/App.xaml.cs | How services and pages are registered |
| **ViewModel Wrapping** | SharedUI/ViewModel/JourneyViewModel.cs | 1:1 property mapping with Domain models |
| **List Binding** | WinUI/View/JourneysPage.xaml | ItemsSource binding to SelectedProject collections |
| **Entity Templates** | WinUI/Resources/EntityTemplates.xaml | DataTemplate per entity type |

---

## 🔴 Anti-Pattern: Custom Factory Methods (Discovered Dec 18, 2025)

### The Problem

Creating custom page factory methods creates **inconsistency and hidden complexity**:

```csharp
// ❌ WRONG: Custom factory method (created inconsistency)
private FeedbackPointsPage CreateFeedbackPointsPage()
{
    var page = _serviceProvider.GetRequiredService<FeedbackPointsPage>();
    var mainWindowVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
    if (mainWindowVm.SelectedProject?.Model != null)
    {
        var viewModel = new FeedbackPointsPageViewModel(mainWindowVm.SelectedProject.Model);
        page.ViewModel = viewModel;  // ❌ Separate PageViewModel just for one page
    }
    return page;
}

// Navigation: "feedbackpoints" => CreateFeedbackPointsPage(),  // ❌ Special case!
```

### Why This Is Wrong

1. **Inconsistent DI Pattern**
   - All other pages use: `GetRequiredService<PageName>()`
   - This page uses: `CreateFeedbackPointsPage()` method
   - **Result:** Navigation pattern differs from all other pages

2. **Unnecessary ViewModel Wrapper**
   - Creates `FeedbackPointsPageViewModel` just for one page
   - Other pages (JourneysPage, WorkflowsPage) don't have PageViewModels
   - **Result:** Extra complexity, extra file to maintain

3. **Workaround Dependencies**
   - Requires `ToObservableCollection()` extension as workaround
   - Extension exists only to convert between wrapper and Domain types
   - **Result:** Hidden technical debt

4. **Harder to Debug**
   - Different pages use different navigation patterns
   - When debugging, have to check if page is "special"
   - **Result:** Cognitive load, maintainability issues

### The Correct Solution

```csharp
// ✅ CORRECT: Consistent DI in App.xaml.cs
services.AddTransient<View.FeedbackPointsPage>();

// ✅ CORRECT: Simple navigation like all other pages
"feedbackpoints" => _serviceProvider.GetRequiredService<FeedbackPointsPage>(),

// ✅ CORRECT: Code-behind injects MainWindowViewModel (like WorkflowsPage)
public sealed partial class FeedbackPointsPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public FeedbackPointsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
    }
}

// ✅ CORRECT: XAML binds to MainWindowViewModel's collections
<Page DataContext="{x:Bind ViewModel}">
    <ListView ItemsSource="{x:Bind ViewModel.SelectedProject.FeedbackPoints, Mode=OneWay}" />
</Page>
```

### Impact of Correct Solution

- ✅ **-1 file** (FeedbackPointsPageViewModel deleted)
- ✅ **-20 LOC** (CreateFeedbackPointsPage method removed)
- ✅ **-0 workarounds** (No ToObservableCollection() extension needed)
- ✅ **+Consistency** (All pages follow same pattern)
- ✅ **+Maintainability** (One pattern to understand and maintain)

---

## 🎯 Pattern Reference Table

| Scenario | Source to Copy From | Key Pattern |
|----------|---|---|
| **Creating a new Page** | JourneysPage.xaml.cs, WorkflowsPage.xaml.cs | Constructor: `public PageName(MainWindowViewModel viewModel) { ViewModel = viewModel; }` |
| **Page Navigation** | NavigationService.cs (line ~45) | `"tag" => _serviceProvider.GetRequiredService<PageName>()` |
| **DI Registration** | App.xaml.cs (line ~130) | `services.AddTransient<View.PageName>();` |
| **ViewModel 1:1 Mapping** | JourneyViewModel.cs, StationViewModel.cs | Same property names as Domain model |
| **List Binding in XAML** | JourneysPage.xaml | `ItemsSource="{x:Bind ViewModel.SelectedProject.Journeys, Mode=OneWay}"` |
| **Entity Templates** | EntityTemplates.xaml | `<DataTemplate x:DataType="vm:JourneyViewModel">` |
| **Empty State Detection** | JourneysPage.xaml | `Visibility="{x:Bind ViewModel.SelectedJourney, Converter={StaticResource NotNullConverter}}"` |

---

## 🔍 Decision Tree: When to Create New ViewModel vs. Reuse

```
Implementing New Feature?
│
├─ Is it a Page/View?
│  ├─ Simple (readonly or list view)? 
│  │  └─ ✅ Use MainWindowViewModel directly
│  └─ Complex (custom editor/designer)?
│     └─ ✅ Check TrackPlanEditorViewModel pattern (custom ViewModel + service)
│
├─ Is it a Domain model wrapper?
│  └─ ✅ Create XxxViewModel with 1:1 property mapping
│     (e.g., JourneyViewModel, StationViewModel, TrainViewModel)
│
├─ Is it a singleton service/state manager?
│  └─ ✅ Create specialized ViewModel (e.g., TrackPlanEditorViewModel, CounterViewModel)
│
└─ NEVER: Create "PageViewModel" for simple pages ❌
```

---

## 📋 Pre-Implementation Checklist

Before starting ANY new feature, run through this:

- [ ] **Step 1:** Search for similar features in codebase (Pages, ViewModels, Services)
- [ ] **Step 2:** Review the source files (copy exact pattern)
- [ ] **Step 3:** Check NavigationService.cs for navigation pattern
- [ ] **Step 4:** Check App.xaml.cs for DI registration pattern
- [ ] **Step 5:** Verify your implementation matches existing patterns
- [ ] **Step 6:** Build and validate (no custom workarounds)

**If you find yourself creating:**
- Custom factory methods
- Special-case exceptions in navigation
- Custom extensions (e.g., `ToObservableCollection()`)
- Extra workaround code

→ **STOP! You're not following the pattern. Go back to Step 1.**

---

## 📚 Related Files to Review

- `WinUI/Service/NavigationService.cs` - Navigation pattern reference
- `WinUI/App.xaml.cs` - DI registration pattern reference
- `WinUI/View/JourneysPage.xaml.cs` - Page constructor pattern reference
- `WinUI/View/WorkflowsPage.xaml.cs` - Page constructor pattern reference
- `SharedUI/ViewModel/MainWindowViewModel.cs` - Central ViewModel reference
- `SharedUI/ViewModel/TrackPlanEditorViewModel.cs` - Complex ViewModel reference (when main VM isn't enough)

---

## 🎓 Lesson Learned

**Don't optimize prematurely. If existing patterns work, replicate them.**

The cost of consistency > the perceived benefit of special cases.
