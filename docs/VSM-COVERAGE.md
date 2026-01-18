# VisualStateManager (VSM) Coverage Report

> **Last Updated:** 2026-01-22  
> **Responsible:** Copilot  
> **Status:** Week 2 Quality Improvement

---

## 📊 Overview

MOBAflow uses **VisualStateManager (VSM)** with **AdaptiveTriggers** for responsive layouts across desktop/tablet/mobile form factors.

### Responsive Breakpoints

| State | Width Range | Device | Layout |
|-------|-------------|--------|--------|
| **Compact** | 0-640px | Mobile, small tablet | Single-column, hidden panels |
| **Medium** | 641-1199px | Tablet landscape | 2-column where possible |
| **Wide** | 1200px+ | Desktop, large monitor | Full multi-column layout |

---

## ✅ VSM-Enabled Pages (Complete)

| Page | File | VSM States | Last Updated | Notes |
|------|------|-----------|--------------|-------|
| **TrainControlPage** | `WinUI/View/TrainControlPage.xaml` | Compact/Medium/Wide | 2026-01-17 | 3 responsive layouts |
| **TrackPlanEditorPage** | `WinUI/View/TrackPlanEditorPage.xaml` | Compact/Medium/Wide | 2026-01-18 | Grid + Toolbox responsive |
| **WorkflowsPage** | `WinUI/View/WorkflowsPage.xaml` | Compact/Medium/Wide | 2026-01-22 | ✅ DONE - List (25%) + Editor (75%) responsive |

### Example: TrainControlPage VSM Structure

```xaml
<VisualStateManager.VisualStateGroups>
  <VisualStateGroup x:Name="TrainControlStates">
    
    <!-- WIDE: All 4 columns visible -->
    <VisualState x:Name="WideState">
      <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="1200" />
      </VisualState.StateTriggers>
      <VisualState.Setters>
        <Setter Target="ControlGrid.ColumnDefinitions" Value="260,*,100,200" />
        <Setter Target="SettingsPanel.Visibility" Value="Visible" />
        <Setter Target="SpeedPanel.Visibility" Value="Visible" />
      </VisualState.Setters>
    </VisualState>
    
    <!-- MEDIUM: Settings + Tachometer, Functions hidden -->
    <VisualState x:Name="MediumState">
      <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="641" />
      </VisualState.StateTriggers>
      <VisualState.Setters>
        <Setter Target="ControlGrid.ColumnDefinitions" Value="250,*" />
        <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
        <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
      </VisualState.Setters>
    </VisualState>
    
    <!-- COMPACT: Full-width stacked -->
    <VisualState x:Name="CompactState">
      <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />
      </VisualState.StateTriggers>
      <VisualState.Setters>
        <Setter Target="ControlGrid.ColumnDefinitions" Value="*" />
        <Setter Target="SettingsPanel.Visibility" Value="Collapsed" />
        <Setter Target="FunctionsPanel.Visibility" Value="Collapsed" />
        <Setter Target="SpeedPanel.Visibility" Value="Collapsed" />
      </VisualState.Setters>
    </VisualState>
    
  </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

---

## ⚠️ Non-VSM Pages (Skin-Based, Pure C#)

These pages use **programmatic UI rendering** in C# (not XAML), so VSM is **not applicable**:

| Page | File | Type | Status | Notes |
|------|------|------|--------|-------|
| **TrainControlPage2** | `WinUI/View/TrainControlPage2.cs` | Skin-based | Active | Pure C#, renders UI programmatically |
| **SignalBoxPage2** | `WinUI/View/SignalBoxPage2.cs` | Skin-based | Active | Pure C#, renders elements dynamically |

### Why Not VSM for Page2?

- ✅ **Programmatic rendering** = No XAML VisualStateManager syntax
- ✅ **Dynamic theme + layout selection** = Runtime control flow (easier than VSM)
- ✅ **Existing UI state management** = Already handles layout via `Visibility` bindings

**Alternative approach:** Responsive logic handled in code-behind:
```csharp
private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
{
    if (e.Size.Width < 640)
        ApplyCompactLayout();
    else if (e.Size.Width < 1200)
        ApplyMediumLayout();
    else
        ApplyWideLayout();
}
```

---

## 📌 TODO: Pages Needing VSM (Week 2-3)

| Page | File | Priority | Breakpoints Needed | Estimated Effort |
|------|------|----------|-------------------|------------------|
| **JourneysPage** | `WinUI/View/JourneysPage.xaml` | 🟡 MEDIUM | Compact/Medium/Wide | 1-2 hours |
| **TrainsPage** | `WinUI/View/TrainsPage.xaml` | 🟡 MEDIUM | Compact/Medium/Wide | 1-2 hours |
| **SettingsPage** | `WinUI/View/SettingsPage.xaml` | 🟡 MEDIUM | Compact/Medium/Wide | 1-2 hours |
| **ProjectsPage** | (if exists) | 🟡 MEDIUM | Compact/Medium/Wide | 1-2 hours |

### WorkflowsPage Specifics (NEXT):

**Current:** List + Editor side-by-side (works on desktop, breaks on tablet)

**Needed:**
- **Wide (1200px+):** List left (25%), Editor right (75%)
- **Medium (641-1199px):** List full-width, Editor as modal
- **Compact (0-640px):** List full-width, swipe/button to edit

---

## 🔍 Quick Audit: Other Pages

Run this checklist for each page:

```
Page: [PageName]
- [ ] Has XAML root? (if yes, can use VSM)
- [ ] Has responsive layout needs?
- [ ] Tested at 640px width? (mobile)
- [ ] Tested at 1024px width? (tablet)
- [ ] Tested at 1920px width? (desktop)
- [ ] Content overflow on small screens?
- [ ] Panels/sidebars hide on small screens?
```

---

## 🎯 Week 2 Plan

1. **Refactor WorkflowsPage** with VSM (Compact/Medium/Wide)
2. **Audit other pages** for responsive needs
3. **Document** in this file as pages are completed
4. **Test** all breakpoints (resize window 640px → 1920px)

---

## 🛠️ How to Add VSM to a Page

### Template (Copy-Paste):

```xaml
<Page x:Class="Moba.WinUI.View.MyPage">
    <Grid x:Name="RootGrid">
        <!-- Step 1: Define VisualStateManager -->
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="AdaptiveStates">
                
                <!-- Wide State -->
                <VisualState x:Name="WideState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1200" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <!-- Wide-specific changes -->
                        <Setter Target="MainGrid.ColumnDefinitions" Value="300,*,150" />
                        <Setter Target="SidebarPanel.Visibility" Value="Visible" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- Medium State -->
                <VisualState x:Name="MediumState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="641" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="MainGrid.ColumnDefinitions" Value="*" />
                        <Setter Target="SidebarPanel.Visibility" Value="Collapsed" />
                    </VisualState.Setters>
                </VisualState>
                
                <!-- Compact State -->
                <VisualState x:Name="CompactState">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="0" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="MainGrid.ColumnDefinitions" Value="*" />
                        <Setter Target="SidebarPanel.Visibility" Value="Collapsed" />
                        <Setter Target="DetailsPanel.Visibility" Value="Collapsed" />
                    </VisualState.Setters>
                </VisualState>
                
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        
        <!-- Step 2: Layout (same for all states) -->
        <Grid x:Name="MainGrid" ColumnSpacing="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="150" />
            </Grid.ColumnDefinitions>
            
            <StackPanel x:Name="SidebarPanel" Grid.Column="0">
                <!-- Sidebar content (hidden in Medium/Compact) -->
            </StackPanel>
            
            <Grid Grid.Column="1">
                <!-- Main content -->
            </Grid>
            
            <StackPanel x:Name="DetailsPanel" Grid.Column="2">
                <!-- Details panel (hidden in Compact) -->
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

---

## 📚 Related Documents

- [`ARCHITECTURE.md`](ARCHITECTURE.md) - Overall architecture
- [`.github/instructions/winui.instructions.md`](.github/instructions/winui.instructions.md) - WinUI best practices including VSM
- [`.copilot-todos.md`](.github/instructions/.copilot-todos.md) - Quality roadmap

---

## Testing Checklist

Before considering VSM implementation complete:

- [ ] Window at 300px width → Compact state active
- [ ] Window at 800px width → Medium state active
- [ ] Window at 1400px width → Wide state active
- [ ] No layout overflow or clipping
- [ ] Smooth transition between states (no jarring jumps)
- [ ] All interactive elements accessible in all states
- [ ] Tested on actual tablet/mobile device if possible

---

**Status:** 🟡 In Progress (Week 2)  
**Next Action:** Refactor WorkflowsPage with VSM
