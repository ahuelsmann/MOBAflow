# Undo/Redo Functionality Removal

**Date**: 2025-11-29  
**Reason**: Implementation was too complex and caused issues with tree expansion state restoration

## What Was Removed

### Files
- `SharedUI/Service/UndoRedoManager.cs` - Can be kept for reference but is no longer used

### MainWindowViewModel Changes

#### Removed Fields
- `_undoRedoManager` - UndoRedoManager instance

#### Removed Properties
- `CanUndo` - Boolean indicating if undo is available
- `CanRedo` - Boolean indicating if redo is available

#### Removed Methods
- `UndoAsync()` - Undo command
- `RedoAsync()` - Redo command
- `UpdateUndoRedoState()` - Updates CanUndo/CanRedo properties
- `RestoreExpansionStatesWithoutCollapse()` - Undo/redo specific expansion restore

#### Removed Method Calls
All calls to:
- `_undoRedoManager.SaveStateImmediateAsync()`
- `_undoRedoManager.MarkCurrentAsSaved()`
- `_undoRedoManager.ClearHistory()`
- `UpdateUndoRedoState()`

### UI Changes

#### WinUI/View/MainWindow.xaml
- Removed Undo button from toolbar (line ~156-161)
- Commented out for future reference

#### WinUI/View/MainWindow.xaml.cs
- Removed Ctrl+Z (Undo) keyboard shortcut handler
- Removed Ctrl+Y (Redo) keyboard shortcut handler

## Current State

### HasUnsavedChanges
Still tracked but now only set by:
- Direct user property edits
- Adding/removing items (projects, stations, etc.)
- Loading/saving solutions

### Tree Expansion State
- Still preserved during `BuildTreeView()` operations
- Uses `SaveExpansionStates()` and `RestoreExpansionStates()`
- No longer tied to undo/redo functionality

## Future Reimplementation

When reimplementing undo/redo, consider:

1. **Simpler State Management**
   - Use a command pattern instead of full solution snapshots
   - Store only incremental changes, not entire solution
   
2. **Separate Concerns**
   - Tree expansion state should be independent of undo/redo
   - Don't rebuild tree on every undo/redo
   
3. **Performance**
   - File-based history was slow for frequent operations
   - Consider in-memory circular buffer with configurable size
   
4. **User Experience**
   - Save state only on significant actions, not every keystroke
   - Clear visual feedback when undo/redo is performed
   
5. **Test Coverage**
   - Add comprehensive tests before UI integration
   - Test edge cases (empty solution, rapid undo/redo, etc.)

## Files to Review for Reimplementation

- `SharedUI/Service/UndoRedoManager.cs` - Reference implementation
- `Backend/Model/Solution.cs` - Has `UpdateFrom()` method for efficient updates
- `SharedUI/ViewModel/MainWindowViewModel.cs` - Integration point
- This document for what was removed and why

## Related Issues

- Tree expansion state restoration was unreliable with undo/redo
- Type-based index keys didn't match between save and restore
- Performance issues with frequent file I/O
