# 🎨 UX & Usability Guidelines for MOBAflow

**Core Philosophy**: **"If users struggle, we failed - not them."**

This document outlines the UX and usability principles for all MOBAflow platforms (WinUI, MAUI, Blazor).

---

## 📋 Table of Contents

1. [Visual Hierarchy & Layout](#1-visual-hierarchy--layout)
2. [User Feedback & Affordances](#2-user-feedback--affordances)
3. [Accessibility (A11y)](#3-accessibility-a11y)
4. [Discoverability & Learnability](#4-discoverability--learnability)
5. [Consistency & Predictability](#5-consistency--predictability)
6. [Performance & Responsiveness](#6-performance--responsiveness)
7. [Error Prevention & Recovery](#7-error-prevention--recovery)
8. [Mobile/Touch Considerations (MAUI)](#8-mobiletouch-considerations-maui)
9. [WinUI 3 Specific Best Practices](#9-winui-3-specific-best-practices)
10. [Testing UX](#10-testing-ux)
11. [UX Anti-Patterns to Avoid](#ux-anti-patterns-to-avoid)
12. [UX Resources](#ux-resources)

---

## 1. Visual Hierarchy & Layout

### ✅ **Responsive Design**

```xaml
<!-- ❌ WRONG: Fixed width causes gaps on large screens -->
<StackPanel Width="1200" HorizontalAlignment="Center">
    <!-- Content gets centered, leaving gaps -->
</StackPanel>

<!-- ✅ CORRECT: Responsive with MaxWidth for readability -->
<Grid Padding="24">
    <StackPanel MaxWidth="1200" HorizontalAlignment="Left">
        <!-- Content fills available space, respects max width -->
    </StackPanel>
</Grid>
```

**Why MaxWidth?**
- Prevents text lines from becoming too wide (improves readability)
- Standard: 600-1200px for content areas
- Left-aligned prevents gaps when window is maximized

---

### ✅ **Spacing & Density**

Use the **4px grid system** for consistent spacing:

| Element | Spacing | Example |
|---------|---------|---------|
| **Padding** | Multiples of 4 | 4, 8, 12, 16, 24, 32 |
| **Spacing** | Between elements | 8-16px (StackPanel/Grid) |
| **Margins** | Between sections | 16-24px |

```xaml
<!-- ✅ GOOD: Consistent spacing -->
<StackPanel Spacing="16" Padding="24">
    <TextBlock Style="{StaticResource TitleTextBlockStyle}" />
    <TextBlock Style="{StaticResource BodyTextBlockStyle}" Margin="0,8,0,0" />
</StackPanel>
```

---

### ✅ **Information Density**

| View Type | Density | Purpose |
|-----------|---------|---------|
| **Dashboard/Overview** | Lower | Focus on key metrics, scannable |
| **Editor/Details** | Higher | Grouped logically, collapsible sections |
| **Lists/Grids** | Medium | Scannable rows, visual separators |

**Example: Use Expanders for grouped sections**
```xaml
<Expander Header="Advanced Settings" IsExpanded="False">
    <!-- Detailed settings here -->
</Expander>
```

---

## 2. User Feedback & Affordances

### ✅ **Immediate Visual Feedback**

Users must **always know** that their action was received:

```csharp
// ✅ GOOD: User sees immediate feedback
[RelayCommand]
private async Task ConnectAsync()
{
    StatusText = "Connecting...";  // ✅ Immediate feedback
    IsConnecting = true;            // ✅ Disable button (prevent double-click)
    
    try
    {
        await _z21.ConnectAsync();
        StatusText = "Connected ✅";
    }
    catch (Exception ex)
    {
        StatusText = $"Failed: {ex.Message}";
    }
    finally
    {
        IsConnecting = false;  // ✅ Re-enable button
    }
}
```

**Key Points:**
- Update `StatusText` **before** async operation starts
- Disable button to prevent multiple clicks
- Show success/failure after operation completes

---

### ✅ **Loading States**

```xaml
<!-- ✅ GOOD: Show loading indicator -->
<ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />
<TextBlock Text="{x:Bind ViewModel.StatusText, Mode=OneWay}" />
```

**For long operations:**
- Use `ProgressBar` with percentage (0-100%)
- Show estimated time remaining if possible
- Allow cancellation for operations >3 seconds

---

### ✅ **Error Handling**

```csharp
// ✅ GOOD: User-friendly error messages
try
{
    await SaveAsync();
}
catch (IOException ex)
{
    // User-friendly message (what went wrong + what to do)
    ErrorMessage = "Could not save file. Please check disk space and permissions.";
    
    // Log technical details for debugging
    _logger.LogError(ex, "Save failed: {Path}", _filePath);
}
```

**Error Message Guidelines:**
1. **What happened**: "Could not save file"
2. **Why**: "Disk is full"
3. **What to do**: "Free up space and try again"
4. ❌ Avoid: "IOException: 0x80070070"

---

## 3. Accessibility (A11y)

### ✅ **Keyboard Navigation**

**All functionality must be accessible via keyboard!**

```xaml
<!-- ✅ GOOD: TabIndex and keyboard shortcuts -->
<Button 
    Content="Save" 
    Command="{x:Bind SaveCommand}"
    TabIndex="1"
    ToolTipService.ToolTip="Save (Ctrl+S)" />
```

```csharp
// ✅ GOOD: Handle keyboard shortcuts in code-behind
private void OnKeyDown(object sender, KeyRoutedEventArgs e)
{
    var ctrlPressed = IsCtrlPressed();
    
    if (ctrlPressed && e.Key == VirtualKey.S)
    {
        ViewModel.SaveCommand.Execute(null);
        e.Handled = true;
    }
}
```

**Standard Shortcuts:**
- `Ctrl+S`: Save
- `Ctrl+Z`: Undo
- `Ctrl+Y`: Redo
- `Ctrl+N`: New
- `Ctrl+O`: Open
- `F2`: Rename
- `Delete`: Delete selected item

---

### ✅ **Screen Reader Support**

```xaml
<!-- ✅ GOOD: AutomationProperties for screen readers -->
<Button 
    Content="Delete"
    AutomationProperties.Name="Delete Journey"
    AutomationProperties.HelpText="Permanently delete the selected journey" />
```

**Guidelines:**
- **Name**: Brief description (1-3 words)
- **HelpText**: Detailed explanation (1 sentence)
- **LiveRegion**: For dynamic content (status updates)

---

### ✅ **Focus Management**

- **Tab Order**: Logical flow (top-to-bottom, left-to-right)
- **Focus Indicators**: Visible for keyboard users (WinUI default is good)
- **Initial Focus**: Set on primary action or first input field

```csharp
// Set initial focus on load
private void OnPageLoaded(object sender, RoutedEventArgs e)
{
    NameTextBox.Focus(FocusState.Programmatic);
}
```

---

## 4. Discoverability & Learnability

### ✅ **Tooltips & Help Text**

**Every interactive element should have a tooltip!**

```xaml
<!-- ✅ GOOD: Descriptive tooltips -->
<Button Content="Simulate Feedback">
    <ToolTipService.ToolTip>
        <ToolTip>
            <StackPanel>
                <TextBlock Text="Simulate Z21 Feedback" FontWeight="Bold" />
                <TextBlock Text="Triggers a test feedback for the specified InPort" />
            </StackPanel>
        </ToolTip>
    </ToolTipService.ToolTip>
</Button>
```

**Tooltip Guidelines:**
- **Title**: Bold, action-oriented
- **Description**: Explains what happens
- **Keyboard Shortcut**: Show if available (e.g., "Ctrl+S")

---

### ✅ **Contextual Guidance**

```xaml
<!-- ✅ GOOD: InfoBar with hints -->
<InfoBar 
    IsOpen="True"
    Severity="Informational"
    Title="Getting Started"
    Message="Load a solution file to begin editing your journeys." />
```

**Use InfoBar for:**
- Informational messages (blue)
- Success confirmations (green)
- Warnings (yellow)
- Errors (red)

---

### ✅ **Empty States**

**Guide users when there's no data!**

```xaml
<!-- ✅ GOOD: Guide users when no data exists -->
<StackPanel 
    HorizontalAlignment="Center" 
    VerticalAlignment="Center"
    Visibility="{x:Bind ViewModel.HasNoJourneys, Mode=OneWay}">
    <FontIcon Glyph="&#xE8F1;" FontSize="48" Opacity="0.4" />
    <TextBlock Text="No Journeys Yet" FontWeight="Bold" Margin="0,16,0,8" />
    <TextBlock Text="Click 'Add Journey' to create your first journey" />
    <Button Content="Add Journey" Command="{x:Bind ViewModel.AddJourneyCommand}" Margin="0,16,0,0" />
</StackPanel>
```

**Empty State Components:**
1. **Icon**: Large, semi-transparent (Opacity="0.4")
2. **Title**: Bold, explains what's missing
3. **Description**: Guide to next action
4. **Button**: Primary action to populate data

---

## 5. Consistency & Predictability

### ✅ **Standard Patterns**

| Action | Pattern | Example |
|--------|---------|---------|
| **Primary Action** | AccentButtonStyle (blue) | Save, Connect, OK |
| **Destructive Action** | Red icon, confirmation | Delete, Clear All |
| **Cancel/Secondary** | Default style | Cancel, Close |
| **Navigation** | NavigationView | Explorer, Editor, Overview |

```xaml
<!-- Primary action -->
<Button Content="Save" Style="{StaticResource AccentButtonStyle}" />

<!-- Destructive action -->
<Button Content="Delete">
    <Button.Icon>
        <FontIcon Glyph="&#xE74D;" Foreground="Red" />
    </Button.Icon>
</Button>
```

---

### ✅ **Icon Consistency**

**Use Segoe MDL2 Assets consistently:**

```csharp
// ✅ GOOD: Centralized icon constants
public static class Icons
{
    public const string Add = "\uE710";
    public const string Delete = "\uE74D";
    public const string Edit = "\uE70F";
    public const string Save = "\uE74E";
    public const string Folder = "\uE8B7";
    public const string Settings = "\uE713";
    public const string Connect = "\uE8EB";
    public const string Disconnect = "\uE8F1";
}
```

**Icon Resources:**
- [Segoe MDL2 Assets](https://learn.microsoft.com/windows/apps/design/style/segoe-ui-symbol-font)

---

### ✅ **Naming Conventions**

| Element | Convention | Examples |
|---------|-----------|----------|
| **Buttons** | Action verbs | "Save", "Connect", "Add Journey" |
| **Labels** | Noun phrases | "Journey Name", "Z21 IP Address" |
| **Status** | State descriptions | "Connected", "Loading...", "3/10 journeys" |

---

## 6. Performance & Responsiveness

### ✅ **Perceived Performance**

**Show progress for operations >1 second:**

```csharp
// ✅ GOOD: Show progress for long operations
[RelayCommand]
private async Task LoadLargeDataAsync()
{
    IsLoading = true;
    StatusText = "Loading journeys...";
    Progress = 0;
    
    var journeys = await _repository.GetJourneysAsync(
        progress: new Progress<double>(p => Progress = p));
    
    IsLoading = false;
    StatusText = $"Loaded {journeys.Count} journeys";
}
```

**Guidelines:**
- **<100ms**: Instant (no indicator)
- **100ms-1s**: Brief status text
- **1s-10s**: ProgressRing + status
- **>10s**: ProgressBar + percentage + cancel button

---

### ✅ **Virtualization for Large Lists**

```xaml
<!-- ✅ GOOD: ItemsRepeater for large collections -->
<ScrollViewer>
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Journeys}">
        <!-- Only visible items are rendered -->
    </ItemsRepeater>
</ScrollViewer>
```

**Use virtualization when:**
- List has >100 items
- Items are complex (images, nested layouts)

---

### ✅ **Debouncing/Throttling**

```csharp
// ✅ GOOD: Throttle search input
private DispatcherTimer _searchDebounceTimer;

partial void OnSearchTextChanged(string value)
{
    _searchDebounceTimer?.Stop();
    _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
    _searchDebounceTimer.Tick += (s, e) =>
    {
        _searchDebounceTimer.Stop();
        PerformSearch(value);
    };
    _searchDebounceTimer.Start();
}
```

**When to debounce:**
- Search input (300ms)
- Auto-save (2000ms)
- Resize/scroll events (100ms)

---

## 7. Error Prevention & Recovery

### ✅ **Validation Before Destructive Actions**

```csharp
// ✅ GOOD: Validate before delete
[RelayCommand(CanExecute = nameof(CanDeleteJourney))]
private async Task DeleteJourneyAsync()
{
    // Show confirmation dialog
    var result = await ShowConfirmationDialogAsync(
        "Delete Journey?",
        $"Are you sure you want to delete '{SelectedJourney.Name}'? This cannot be undone.");
    
    if (result == ContentDialogResult.Primary)
    {
        // Check for references
        var validation = _validationService.CanDeleteJourney(SelectedJourney);
        if (!validation.IsValid)
        {
            await ShowErrorDialogAsync("Cannot Delete", validation.ErrorMessage);
            return;
        }
        
        // Perform deletion
        _project.Journeys.Remove(SelectedJourney);
    }
}
```

**Confirmation Dialog Guidelines:**
- **Title**: Question format ("Delete Journey?")
- **Message**: Explain consequence ("This cannot be undone")
- **Primary Button**: Destructive action ("Delete")
- **Secondary Button**: Cancel (default)

---

### ✅ **Undo/Redo Support**

```csharp
// ✅ GOOD: Support undo for destructive actions
private readonly UndoRedoManager _undoRedoManager;

[RelayCommand(CanExecute = nameof(CanUndo))]
private async Task UndoAsync()
{
    var previousState = await _undoRedoManager.UndoAsync();
    Solution = previousState;  // Restore previous state
}
```

**Undo Guidelines:**
- Ctrl+Z / Ctrl+Y shortcuts
- Show undo/redo in toolbar
- Limit history to 50 actions

---

### ✅ **Auto-Save**

```csharp
// ✅ GOOD: Throttled auto-save prevents data loss
private void OnPropertyValueChanged(object? sender, EventArgs e)
{
    // Throttled save (e.g., 2 seconds after last change)
    _undoRedoManager.SaveStateThrottled(Solution);
}
```

**Auto-Save Strategy:**
- Save state to temp directory
- Throttle: 2 seconds after last edit
- Recover on crash/restart

---

## 8. Mobile/Touch Considerations (MAUI)

### ✅ **Touch Targets**

**Minimum touch target size:**
- **Apple HIG**: 44x44 dp
- **Material Design**: 48x48 dp
- **Spacing**: 8dp between targets

```xaml
<!-- ✅ GOOD: Large enough touch targets -->
<Button 
    Content="Connect" 
    MinWidth="120" 
    MinHeight="44"
    Padding="16,8" />
```

---

### ✅ **Gestures**

| Gesture | Action | Example |
|---------|--------|---------|
| **Swipe** | Navigation (back/forward) | Swipe right to go back |
| **Pull-to-Refresh** | Update lists | Pull down to refresh |
| **Long-Press** | Context menu | Long-press for options |

---

## 9. WinUI 3 Specific Best Practices

### ✅ **Use Modern Controls**

| Control | Purpose | Example |
|---------|---------|---------|
| **NavigationView** | App-level navigation | Sidebar menu |
| **InfoBar** | Contextual notifications | Success/error messages |
| **Expander** | Collapsible sections | Settings groups |
| **NumberBox** | Numeric input | Lap count, address |

---

### ✅ **Mica/Acrylic Materials**

```xaml
<!-- ✅ GOOD: Use system materials -->
<Window.SystemBackdrop>
    <MicaBackdrop />
</Window.SystemBackdrop>
```

**When to use:**
- **Mica**: Main window background (subtle)
- **Acrylic**: Flyouts, popups (more transparent)

---

### ✅ **Fluent Design Principles**

| Principle | Implementation |
|-----------|---------------|
| **Depth** | Layered UI (elevation, shadows) |
| **Motion** | Meaningful animations (entrance, exit) |
| **Material** | Mica, Acrylic for depth |
| **Scale** | Responsive across devices |

---

## 10. Testing UX

### ✅ **User Testing Checklist**

- [ ] Can a new user complete core tasks without documentation?
- [ ] Are error messages clear and actionable?
- [ ] Is keyboard navigation intuitive?
- [ ] Are loading states and progress visible?
- [ ] Does the app respond within 100ms to user input?
- [ ] Are destructive actions protected by confirmation?

---

### ✅ **Accessibility Audit**

- [ ] All interactive elements have keyboard access
- [ ] Screen reader announces all UI changes
- [ ] Color contrast ratio ≥ 4.5:1 (WCAG AA)
- [ ] No information conveyed by color alone

**Tools:**
- [Accessibility Insights](https://accessibilityinsights.io/)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

---

## UX Anti-Patterns to Avoid

| Anti-Pattern | Why Bad | Better Approach |
|--------------|---------|-----------------|
| **"Save" button only** | User forgets to save, loses work | Auto-save + manual save option |
| **Modal dialogs for everything** | Interrupts flow, annoying | Use InfoBar for non-critical messages |
| **Long forms without sections** | Overwhelming, hard to scan | Use Expanders or TabView to group |
| **No feedback on actions** | User unsure if action worked | Show status, progress, or confirmation |
| **Cryptic error messages** | User doesn't know what to do | Explain problem + suggest solution |
| **Hidden features** | Discoverability issue | Use tooltips, empty states, tutorials |
| **Inconsistent icons** | Confusing, unprofessional | Use Segoe MDL2 Assets consistently |
| **Tiny touch targets** | Hard to tap on mobile | Minimum 44x44 dp |
| **No loading indicators** | App feels frozen | Show ProgressRing for >1s operations |

---

## UX Resources

### **Official Guidelines**
- **Microsoft Fluent Design**: [Fluent 2 Design System](https://fluent2.microsoft.design/)
- **WinUI 3 Guidelines**: [Windows App Design](https://learn.microsoft.com/windows/apps/design/)
- **MAUI Guidelines**: [.NET MAUI Design Guidance](https://learn.microsoft.com/dotnet/maui/user-interface/design/)
- **Accessibility**: [WCAG 2.2 Guidelines](https://www.w3.org/WAI/WCAG22/quickref/)

### **UX Research**
- **Nielsen Norman Group**: [10 Usability Heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/)
- **Don Norman**: *The Design of Everyday Things*

### **Tools**
- **Accessibility Insights**: [Download](https://accessibilityinsights.io/)
- **Segoe MDL2 Icons**: [Reference](https://learn.microsoft.com/windows/apps/design/style/segoe-ui-symbol-font)
- **Color Contrast Checker**: [WebAIM](https://webaim.org/resources/contrastchecker/)

---

## Summary

**Good UX is about:**
1. **Clarity**: User knows what to do
2. **Feedback**: User knows what happened
3. **Consistency**: Patterns are predictable
4. **Accessibility**: Everyone can use it
5. **Performance**: Feels fast and responsive

**Remember:** If users struggle, we failed - not them. Design with empathy! 💙
