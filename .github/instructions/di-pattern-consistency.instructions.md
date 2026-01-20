---
description: 'DI pattern consistency for WinUI pages'
applyTo: 'WinUI/**/*.cs'
---

# DI Pattern Consistency

## Page Pattern (ALL pages follow this)

```csharp
// Constructor injection
public sealed partial class MyPage : Page
{
    public MainWindowViewModel ViewModel { get; }
    public MyPage(MainWindowViewModel viewModel) => ViewModel = viewModel;
}
```

## Checklist BEFORE creating new Page

1. Register in `App.xaml.cs`: `services.AddTransient<View.MyPage>()`
2. Add to `NavigationService.cs`: `"mytag" => _serviceProvider.GetRequiredService<MyPage>()`
3. XAML: `DataContext="{x:Bind ViewModel}"`

## Anti-Patterns (NEVER do)

- Custom factory methods for pages
- Separate PageViewModels for simple pages
- Custom extensions like `ToObservableCollection()`

## When to create ViewModel

| Scenario | Decision |
|----------|----------|
| Simple page (list/readonly) | Use MainWindowViewModel directly |
| Domain model wrapper | Create XxxViewModel (1:1 mapping) |
| Complex editor | Check TrackPlanEditorViewModel pattern |
- `WinUI/View/WorkflowsPage.xaml.cs` - Page constructor pattern reference
- `SharedUI/ViewModel/MainWindowViewModel.cs` - Central ViewModel reference
- `SharedUI/ViewModel/TrackPlanEditorViewModel.cs` - Complex ViewModel reference (when main VM isn't enough)

---

## 🎓 Lesson Learned

**Don't optimize prematurely. If existing patterns work, replicate them.**

The cost of consistency > the perceived benefit of special cases.
