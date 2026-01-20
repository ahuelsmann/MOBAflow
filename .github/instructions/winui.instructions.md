---
description: WinUI 3 patterns - DispatcherQueue, navigation, responsive layout
applyTo: "WinUI/**/*.xaml,WinUI/**/*.cs"
---

# WinUI 3 Guidelines

## UI Thread Dispatching

```csharp
// Background thread -> UI update
_dispatcher.TryEnqueue(() => OnPropertyChanged(e.PropertyName));

// Get dispatcher
var dispatcher = this.DispatcherQueue;  // In Page/Window
```

## Navigation

```csharp
// NavigationView item invoked
var tag = args.InvokedItemContainer.Tag?.ToString();
ContentFrame.Navigate(typeof(JourneysPage));
```

## XAML Patterns

### ThemeResources (ALWAYS use, never hardcode colors)

```xaml
Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
Foreground="{ThemeResource TextFillColorPrimaryBrush}"
BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
```

### DataTemplates in EntityTemplates.xaml

```xaml
<!-- WinUI/Resources/EntityTemplates.xaml -->
<DataTemplate x:Key="JourneyTemplate" x:DataType="vm:JourneyViewModel">
    <StackPanel Padding="16" Spacing="16">
        <TextBox Header="Name" Text="{x:Bind Name, Mode=TwoWay}" />
    </StackPanel>
</DataTemplate>
```

### EntityTemplateSelector

```csharp
protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    => item switch
    {
        JourneyViewModel => JourneyTemplate,
        TrainViewModel => TrainTemplate,
        _ => DefaultTemplate
    };
```

## Responsive Layout (VSM)

```xaml
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="WideState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="1200" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="SidePanel.Visibility" Value="Visible" />
            </VisualState.Setters>
        </VisualState>
        <VisualState x:Name="CompactState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0" />
            </VisualState.StateTriggers>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

## CommandBar DynamicOverflow

```xaml
<CommandBar OverflowButtonVisibility="Auto">
    <AppBarButton CommandBar.DynamicOverflowOrder="0" Label="Connect" />  <!-- Always visible -->
    <AppBarButton CommandBar.DynamicOverflowOrder="2" Label="Settings" /> <!-- Overflow first -->
</CommandBar>
```

## File Picker (requires window handle)

```csharp
var picker = new FileOpenPicker();
InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
var file = await picker.PickSingleFileAsync();
```

## Selection Management

```csharp
// Preserve selection across source changes
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(SelectedJourney))]
private ProjectViewModel? _selectedProject;

partial void OnSelectedProjectChanged(ProjectViewModel? value)
{
    SelectedJourney = value?.Journeys.FirstOrDefault();
}
```

## Anti-Patterns

- UserControls for property panels → Use DataTemplates in EntityTemplates.xaml
- Hardcoded colors → Use ThemeResource
- Direct UI updates from background → Use DispatcherQueue
- FileOpenPicker without InitializeWithWindow → Will fail
