// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.Collections.Generic;

/// <summary>
/// Coordinates persistence for all IPersistablePage instances.
/// Automatically syncs page state during Solution save/load operations.
/// 
/// Usage:
/// 1. Register this service as Singleton in DI
/// 2. Call RegisterPage() for each IPersistablePage during app startup
/// 3. The coordinator handles SolutionSaving/SolutionLoaded events automatically
/// </summary>
public sealed class PagePersistenceCoordinator : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly List<IPersistablePage> _pages = [];
    private bool _disposed;

    public PagePersistenceCoordinator(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.SolutionSaving += OnSolutionSaving;
        _viewModel.SolutionLoaded += OnSolutionLoaded;
    }

    /// <summary>
    /// Register a page for automatic persistence coordination.
    /// </summary>
    public void RegisterPage(IPersistablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        
        if (!_pages.Contains(page))
        {
            _pages.Add(page);
        }
    }

    /// <summary>
    /// Unregister a page from persistence coordination.
    /// </summary>
    public void UnregisterPage(IPersistablePage page)
    {
        _pages.Remove(page);
    }

    /// <summary>
    /// Check if any registered page has unsaved changes.
    /// </summary>
    public bool HasAnyUnsavedChanges => _pages.Any(p => p.HasUnsavedChanges);

    /// <summary>
    /// Get all registered pages.
    /// </summary>
    public IReadOnlyList<IPersistablePage> RegisteredPages => _pages.AsReadOnly();

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        foreach (var page in _pages)
        {
            page.SyncToModel();
        }
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        foreach (var page in _pages)
        {
            page.LoadFromModel();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _viewModel.SolutionSaving -= OnSolutionSaving;
        _viewModel.SolutionLoaded -= OnSolutionLoaded;
        _pages.Clear();
        
        _disposed = true;
    }
}
