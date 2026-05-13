---

description: 'WinUI 3 patterns and layout guidance'
applyTo: 'MOBAflow/**/*.xaml,MOBAflow/**/*.cs'
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
<!-- MOBAflow/Resources/EntityTemplates.xaml -->
<DataTemplate x:Key="JourneyTemplate" x:DataType="vm:JourneyViewModel">
    <StackPanel Padding="16" Spacing="16">
        <TextBox Header="Name" Text="{x:Bind Name, Mode=TwoWay}" />
    </StackPanel>
</DataTemplate>
```

### EntityTemplateSelector

```csharp
protected override DataTemplate? SelectTemplateCore(
    object item,
    DependencyObject container)
    => item switch
    {
        JourneyViewModel => JourneyTemplate,
        TrainViewModel => TrainTemplate,
        _ => DefaultTemplate
    };
```

## Grid und Spaltenlayout

Ausführlich:
[fluent-design.instructions.md](./fluent-design.instructions.md)
→ Abschnitt **Grid System (Spalten & Inhalte)**.

- **Spalten:** `Width="Auto"` nur für schmale Inhalte (Splitter,
  Buttons); für Inhalte `*` mit **MinWidth**
  (ggf. **MaxWidth**). Keine Lücken in **Grid.Column**
  (0, 1, 2, …).
- **Inhalt in \*-Spalten:** Immer **ScrollViewer** oder **ListView**,
  wenn Inhalt länger werden kann;
  **TextTrimming="CharacterEllipsis"** für Text in engen Zellen.
  Kein festes **Width** am Kind in \*-Spalten.
- **Zeilen:** Scrollbaren Bereich in Zeile mit **Height="*"** legen;
  darin genau einen **ScrollViewer** mit dem restlichen Inhalt.

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
    <!-- Always visible -->
    <AppBarButton CommandBar.DynamicOverflowOrder="0" Label="Connect" />
    <!-- Overflow first -->
    <AppBarButton CommandBar.DynamicOverflowOrder="2" Label="Settings" />
</CommandBar>
```

## File Picker (requires window handle)

```csharp
var picker = new FileOpenPicker();
InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
var file = await picker.PickSingleFileAsync();
```

## Windows App SDK tooling

- Use `winapp` on Windows when a task needs packaged WinUI execution, packaging/signing, or UI automation.
- `winapp --version` is known to be available on the Windows development machine and returned `0.3.1`.
- Prefer project-scoped commands such as `MOBAflow/MOBAflow.csproj`; avoid repository-wide `.csproj` discovery.
- Treat Microsoft `win-dev-skills` as optional guidance for WinUI 3 workflows, Fluent Design, accessibility, `x:Bind`, packaging, and UI automation. MOBAflow rules remain authoritative.

## Workspace/LSP hygiene

- Keep `.nuget`, `bin`, and `obj` excluded from IDE search, file watchers, and language-server project discovery.
- The repository-local `.nuget/packages` folder can contain package source projects whose `PackageReference` versions conflict with central package management.
- If the C# language server reports duplicate generated files from `.nuget`, `bin`, or `obj`, reload the IDE after verifying the workspace excludes.

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
- **Grid:** ColumnDefinition **Width="Auto"** mit langem oder breitem
  Inhalt → Spalte explodiert; **Width="*"** ohne MinWidth → Spalte kann
  zu schmal werden; Inhalt in \*-Zelle ohne
  ScrollViewer/TextTrimming → Überlauf oder falsche Spaltenbreiten.
  Siehe Fluent Design „Grid System“.
