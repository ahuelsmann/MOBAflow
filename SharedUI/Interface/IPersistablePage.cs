// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Interface for pages that need to persist their state to/from the Solution's Domain model.
/// Pages implementing this interface will automatically participate in Solution save/load cycles.
/// 
/// Usage:
/// 1. Implement this interface on any page that manages data (SignalBoxPage, TrackPlanPage, etc.)
/// 2. Register the page with the MainWindowViewModel's SolutionSaving/SolutionLoaded events
/// 3. The page's data will automatically sync during save/load operations
/// 
/// This ensures new pages don't "forget" to implement persistence - the pattern is explicit.
/// </summary>
public interface IPersistablePage
{
    /// <summary>
    /// Sync the page's UI state to the Domain model.
    /// Called before the Solution is saved to disk.
    /// 
    /// Implementation should write all unsaved changes from UI to Project/Solution.
    /// </summary>
    void SyncToModel();

    /// <summary>
    /// Load the page's state from the Domain model.
    /// Called after a Solution is loaded from disk.
    /// 
    /// Implementation should read from Project/Solution and update UI accordingly.
    /// </summary>
    void LoadFromModel();

    /// <summary>
    /// Indicates whether this page has unsaved changes.
    /// Can be used for dirty-tracking and save prompts.
    /// </summary>
    bool HasUnsavedChanges { get; }
}
