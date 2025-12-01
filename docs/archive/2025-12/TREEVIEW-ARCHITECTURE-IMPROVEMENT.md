# TreeView Architecture Improvement Plan

**Date**: 2025-12-01  
**Status**: 📋 **RECOMMENDATION** - Not Implemented Yet  
**Priority**: Medium - Quality of Life Improvement  

---

## 🎯 Problem Statement

### Current Architecture Issues

#### 1. **TreeViewBuilder Creates New Instances**
```csharp
private void BuildTreeView()
{
    // ❌ Problem: Creates NEW TreeNodeViewModels every time
    TreeNodes = _treeViewBuilder.BuildTreeView(SolutionViewModel);
    
    // Result:
    // - Selection lost
    // - Expansion state lost
    // - Must manually save/restore state (100+ lines of code)
}
```

**Calls to `BuildTreeView()`**:
- `OnSolutionChanged()` - Solution loaded/created
- `LoadSolutionAsync()` - After load
- `AddProject()` - New project added
- `RefreshTreeView()` - Manual refresh

**Impact**: Every modification triggers full tree rebuild → poor UX

---

#### 2. **Complex State Management**

**Current Code** (~150 lines):
- `SaveExpansionStates()` - Save which nodes are expanded
- `RestoreExpansionStates()` - Restore after rebuild
- `GetNodePath()` - Find node by path
- `FindNodeByPath()` - Restore selection
- `ExpandParentNodes()` - Make selected node visible
- `FindParentNodeInTree()` - Find parent of node

**All this code exists ONLY because we rebuild the tree!**

---

#### 3. **Performance Impact**

**Tree Rebuild Complexity**: O(n × m) where:
- n = number of nodes in tree (~100-500 typical)
- m = average depth (4-5 levels)

**Operations**:
1. Save expansion states: Traverse entire tree
2. Create new ViewModels: Allocate memory
3. Restore expansion states: Traverse again
4. Find selected node: Traverse again
5. Expand parent nodes: Traverse again

**Total**: 5 tree traversals on every modification!

---

## ✅ Recommended Solution: Hierarchical Binding

### Architecture Change

#### **Current** (TreeNodeViewModel Wrapper):
```
Solution → TreeViewBuilder → TreeNodeViewModel[] → WinUI TreeView
```

#### **Proposed** (Direct Binding):
```
Solution → SolutionViewModel (hierarchical) → WinUI TreeView
```

---

### Implementation Options

#### **Option A: Pure ViewModel Binding (Recommended - Phase 1)**

**Concept**: `SolutionViewModel` already has hierarchical structure!

**Current Structure**:
```csharp
public class SolutionViewModel
{
    public ObservableCollection<ProjectViewModel> Projects { get; }
}

public class ProjectViewModel
{
    public ObservableCollection<JourneyViewModel> Journeys { get; }
    public ObservableCollection<WorkflowViewModel> Workflows { get; }
    public ObservableCollection<TrainViewModel> Trains { get; }
}

public class JourneyViewModel
{
    public ObservableCollection<StationViewModel> Stations { get; }
}
```

**Change Needed**: Minimal!
```csharp
// MainWindowViewModel - SIMPLIFIED
public ObservableCollection<ProjectViewModel> TreeRoot => SolutionViewModel?.Projects ?? [];

// NO BuildTreeView() needed!
// NO TreeViewBuilder needed!
// NO TreeNodeViewModel needed!
```

**XAML Binding**:
```xaml
<TreeView ItemsSource="{x:Bind ViewModel.TreeRoot, Mode=OneWay}">
    <TreeView.ItemTemplate>
        <DataTemplate>
            <TreeViewItem Header="{Binding Name}">
                <!-- Use DataTemplateSelector for different types -->
            </TreeViewItem>
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

**Advantages**:
- ✅ No tree rebuilds - ViewModels ARE the tree!
- ✅ Selection preserved automatically (WinUI handles it)
- ✅ Expansion state preserved automatically
- ✅ Remove ~200 lines of state management code
- ✅ Better performance (no rebuilds)
- ✅ Direct model → UI binding

**Challenges**:
- Need `DataTemplateSelector` for different node types
- WinUI `TreeView` still uses `TreeViewNode` internally
- May need custom `TreeView` control

---

#### **Option B: Smart TreeViewNode Reuse (Recommended - Phase 2)**

**Reality Check**: Community Toolkit hat **KEIN** hierarchisches TreeView für WinUI 3!

**What exists**:
- `CommunityToolkit.WinUI.Controls.HeaderedTreeView` - Only adds Header property
- Still uses `TreeViewNode` wrapper internally
- No `HierarchicalDataTemplate` support

**Alternative Approach**: Smart Node Caching

**Concept**:
```csharp
public class MainWindowViewModel
{
    // Cache TreeViewNodes for reuse
    private Dictionary<object, TreeViewNode> _nodeCache = new();
    
    public ObservableCollection<TreeViewNode> TreeRoot
    {
        get
        {
            if (SolutionViewModel == null) return [];
            
            var nodes = new ObservableCollection<TreeViewNode>();
            foreach (var project in SolutionViewModel.Projects)
            {
                nodes.Add(GetOrCreateNode(project));
            }
            return nodes;
        }
    }
    
    private TreeViewNode GetOrCreateNode(object dataContext)
    {
        // ✅ Reuse existing node if available
        if (_nodeCache.TryGetValue(dataContext, out var node))
            return node;
        
        // Create new node only when needed
        node = new TreeViewNode { Content = dataContext };
        _nodeCache[dataContext] = node;
        
        // Subscribe to collection changes for dynamic children
        if (dataContext is ProjectViewModel projectVM)
        {
            projectVM.Journeys.CollectionChanged += (s, e) => 
                UpdateNodeChildren(node, projectVM.Journeys);
        }
        
        return node;
    }
}
```

**Advantages**:
- ✅ ~80% code reduction (TreeViewBuilder eliminated)
- ✅ Selection preserved (node instances stay same)
- ✅ Expansion state preserved automatically
- ✅ Better performance (node reuse)
- ✅ No external dependencies
- ✅ Works with native WinUI TreeView

**Challenges**:
- Must implement cache invalidation logic
- Still need `TreeViewNode` wrapper (WinUI API requirement)
- Need to sync node children with ViewModel collections

---

#### **Option C: Custom TreeView Control (Advanced - Phase 3)**

Create custom control that:
- Extends `ItemsControl` (not `TreeView`)
- Supports true hierarchical binding
- Full control over rendering

**Advantages**:
- ✅ No dependencies
- ✅ Full customization
- ✅ Optimal performance

**Challenges**:
- Significant development effort (~2-3 days)
- Need to implement keyboard navigation
- Need to implement selection logic
- Need to implement expand/collapse logic

---

## 📊 Impact Analysis

### Code Reduction

| Component | Current LOC | After Option A | After Option B | Reduction |
|-----------|-------------|----------------|----------------|-----------|
| TreeViewBuilder | ~200 | 0 | 0 | -200 |
| State Management | ~150 | 0 | 0 | -150 |
| TreeNodeViewModel | ~100 | 0 | ~50 (Cache logic) | -100/-50 |
| MainWindowViewModel | ~200 | ~50 | ~80 | -150/-120 |
| **Total** | **~650** | **~50** | **~130** | **-600/-520** |

**Net Reduction**: **~80-92% less code!**

---

### Performance Improvement

| Operation | Current | After Change | Improvement |
|-----------|---------|--------------|-------------|
| Add Station | 5 tree traversals | 0 | ✅ 100% |
| Add Journey | 5 tree traversals | 0 | ✅ 100% |
| Load Solution | 5 tree traversals | 0 | ✅ 100% |
| Selection Change | O(n) search | O(1) | ✅ ~99% |
| Memory Usage | 2x (Model + Wrapper) | 1x | ✅ 50% |

---

### User Experience

| Aspect | Current | After Change |
|--------|---------|--------------|
| Selection Preserved | ⚠️ Sometimes (complex logic) | ✅ Always |
| Expansion Preserved | ⚠️ Sometimes (buggy) | ✅ Always |
| Scroll Position | ❌ Lost on rebuild | ✅ Preserved |
| Responsiveness | ⚠️ Stutters on large trees | ✅ Smooth |

---

## 🚀 Implementation Plan

### Phase 1: Pure ViewModel Binding (1-2 days)

**Steps**:
1. Create `DataTemplateSelector` for node types
   - `ProjectTemplate`
   - `JourneyTemplate`
   - `StationTemplate`
   - `WorkflowTemplate`
   - `TrainTemplate`

2. Update XAML to bind directly to `SolutionViewModel.Projects`

3. Remove `TreeViewBuilder` class

4. Remove state management code from `MainWindowViewModel`

5. Test selection, expansion, drag & drop

**Risk**: Low - Gradual migration, can rollback

**Effort**: ~8-16 hours

---

### Phase 2: Smart Node Caching (1 day)

**Steps**:
1. Implement `_nodeCache` Dictionary in `MainWindowViewModel`

2. Create `GetOrCreateNode()` method with cache logic

3. Subscribe to ViewModel collection changes

4. Implement cache invalidation (when Solution changes)

5. Test selection/expansion preservation

**Risk**: Medium - Need careful cache management

**Effort**: ~6-8 hours

---

### Phase 3: Polish & Optimize (0.5 days)

**Steps**:
1. Add icons for different node types
2. Implement context menus
3. Add drag & drop reordering
4. Performance profiling

**Risk**: Low

**Effort**: ~4 hours

---

## 📝 Migration Guide

### Before (Current Code):

```csharp
// ❌ MainWindowViewModel - 650 lines
private void BuildTreeView()
{
    var expansionStates = new Dictionary<string, bool>();
    SaveExpansionStates(TreeNodes, expansionStates, "");
    
    TreeNodes = _treeViewBuilder.BuildTreeView(SolutionViewModel);
    
    RestoreExpansionStates(TreeNodes, expansionStates, "");
    // ... 100 more lines
}

public void OnNodeSelected(TreeNodeViewModel? node)
{
    // ... 50 lines of property grid logic
}
```

### After (Option A):

```csharp
// ✅ MainWindowViewModel - ~50 lines
public ObservableCollection<ProjectViewModel> TreeRoot => 
    SolutionViewModel?.Projects ?? [];

public void OnNodeSelected(object? dataContext)
{
    // Direct access to ViewModel - no wrapper!
    Properties.Clear();
    
    if (dataContext == null) return;
    
    var props = dataContext.GetType().GetProperties()
        .Where(IsSimpleType);
    
    foreach (var prop in props)
    {
        Properties.Add(new PropertyViewModel(prop, dataContext));
    }
}
```

### After (Option B - Smart Caching):

```csharp
// ✅ MainWindowViewModel - Smart Node Reuse
private Dictionary<object, TreeViewNode> _nodeCache = new();

public ObservableCollection<TreeViewNode> TreeRoot
{
    get
    {
        if (SolutionViewModel == null) return [];
        
        var nodes = new ObservableCollection<TreeViewNode>();
        foreach (var project in SolutionViewModel.Projects)
        {
            nodes.Add(GetOrCreateNode(project));
        }
        return nodes;
    }
}

private TreeViewNode GetOrCreateNode(object dataContext)
{
    if (_nodeCache.TryGetValue(dataContext, out var node))
    {
        // ✅ Reuse existing node - selection/expansion preserved!
        return node;
    }
    
    node = new TreeViewNode { Content = dataContext };
    _nodeCache[dataContext] = node;
    
    // Auto-update children when ViewModel collections change
    SubscribeToCollectionChanges(node, dataContext);
    
    return node;
}
```

---

## 🎓 Lessons from Current Implementation

### What Works Well
- ✅ `SolutionViewModel` hierarchical structure
- ✅ ViewModel pattern (separates UI from Domain)
- ✅ ObservableCollections for automatic updates

### What Doesn't Work
- ❌ `TreeViewBuilder` creates new instances
- ❌ Manual state management (complex, buggy)
- ❌ Performance impact of rebuilds
- ❌ Poor user experience (lost selection)

### Root Cause
**TreeNodeViewModel wrapper layer is unnecessary!**

The hierarchical structure already exists in:
- `Solution → Projects`
- `Project → Journeys/Workflows/Trains`
- `Journey → Stations`
- `Station → Platforms`

**We should bind directly to this structure!**

---

## 🔗 Related Files

**To Remove**:
- `SharedUI/Service/TreeViewBuilder.cs` (~200 LOC)

**To Simplify**:
- `SharedUI/ViewModel/MainWindowViewModel.cs` (-600 LOC)
- `SharedUI/ViewModel/TreeNodeViewModel.cs` (keep but simplify)

**To Create**:
- `WinUI/Selectors/TreeDataTemplateSelector.cs` (~100 LOC)
- `WinUI/Resources/TreeTemplates.xaml` (~200 LOC XAML)

---

## 🎯 Decision Matrix

| Criteria | Option A | Option B | Option C |
|----------|----------|----------|----------|
| **Effort** | Medium (1-2d) | Medium (1d) | High (2-3d) |
| **Risk** | Low | Medium | High |
| **Code Reduction** | -600 LOC | -520 LOC | -620 LOC |
| **Performance** | ✅ Great | ✅ Excellent | ✅ Excellent |
| **Maintainability** | ✅ Good | ✅ Very Good | ⚠️ Complex |
| **Flexibility** | ⚠️ Limited | ✅ Good | ✅ Full Control |
| **Dependencies** | None | None | None |
| **Selection Preserved** | ⚠️ Manual | ✅ Automatic | ✅ Automatic |
| **Expansion Preserved** | ⚠️ Manual | ✅ Automatic | ✅ Automatic |

**Recommendation**: **Start with Option A, then Option B for optimization**

---

## 📋 Next Steps

1. **Review this document** with team
2. **Decide** on approach (A or B recommended)
3. **Create branch** `feature/treeview-refactor`
4. **Implement** Phase 1 (1-2 days)
5. **Test** thoroughly
6. **Merge** and monitor
7. **Consider** Phase 2 (Community Toolkit) after stabilization

---

## 🚨 Rollback Plan

If issues arise:
1. Revert to `main` branch
2. Current `TreeViewBuilder` code remains functional
3. No data loss (Domain models unchanged)

**Risk Level**: Low - UI-only change, no Domain impact

---

**Status**: ✅ **Ready for Implementation**  
**Recommended**: Start with Option A (Pure ViewModel Binding)  
**Next Review**: After Phase 1 completion

