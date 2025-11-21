// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Model;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages auto-save and undo/redo functionality using file-based version history.
/// </summary>
public class UndoRedoManager
{
    private readonly string _historyDirectory;
    private readonly List<string> _history = [];
    private int _currentIndex = -1;
    private readonly int _maxHistorySize = 50;
    private CancellationTokenSource? _autoSaveCts;
    private Solution? _pendingSolution;
    private readonly object _lock = new();

    public UndoRedoManager(string historyDirectory)
    {
        _historyDirectory = historyDirectory;
        
        // Create history directory if it doesn't exist
        if (!Directory.Exists(_historyDirectory))
        {
            Directory.CreateDirectory(_historyDirectory);
        }
    }

    /// <summary>
    /// Saves current state to history (throttled - waits 1 second after last change).
    /// </summary>
    public void SaveStateThrottled(Solution solution)
    {
        lock (_lock)
        {
            _pendingSolution = solution;
            
            // Cancel previous auto-save timer
            _autoSaveCts?.Cancel();
            _autoSaveCts = new CancellationTokenSource();
            
            var token = _autoSaveCts.Token;
            
            // Wait 1 second, then save
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000, token);
                    
                    if (!token.IsCancellationRequested && _pendingSolution != null)
                    {
                        await SaveStateImmediateAsync(_pendingSolution);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when user makes another change quickly
                }
            }, token);
        }
    }

    /// <summary>
    /// Saves current state immediately (use for explicit user actions like Add/Delete).
    /// </summary>
    public async Task SaveStateImmediateAsync(Solution solution)
    {
        string fileToSave;
        
        lock (_lock)
        {
            // Remove any "future" history after current position
            if (_currentIndex < _history.Count - 1)
            {
                for (int i = _history.Count - 1; i > _currentIndex; i--)
                {
                    DeleteHistoryFile(_history[i]);
                    _history.RemoveAt(i);
                }
            }

            // Create new history file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            fileToSave = Path.Combine(_historyDirectory, $"history_{timestamp}.json");
            
            // Remove oldest history if limit reached
            if (_history.Count >= _maxHistorySize)
            {
                DeleteHistoryFile(_history[0]);
                _history.RemoveAt(0);
            }

            _history.Add(fileToSave);
            _currentIndex = _history.Count - 1;
        }

        // Save outside of lock (async I/O)
        await Solution.SaveAsync(fileToSave, solution);
    }

    /// <summary>
    /// Checks if undo is possible.
    /// </summary>
    public bool CanUndo
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex > 0 && _history.Count > 0;
            }
        }
    }

    /// <summary>
    /// Checks if redo is possible.
    /// </summary>
    public bool CanRedo
    {
        get
        {
            lock (_lock)
            {
                return _currentIndex >= 0 && _currentIndex < _history.Count - 1;
            }
        }
    }

    /// <summary>
    /// Loads previous state from history.
    /// </summary>
    public async Task<Solution?> UndoAsync()
    {
        string? filename;
        
        lock (_lock)
        {
            if (!CanUndo) return null;
            _currentIndex--;
            
            if (_currentIndex < 0 || _currentIndex >= _history.Count)
            {
                _currentIndex++; // Revert
                return null;
            }
            
            filename = _history[_currentIndex];
        }

        try
        {
            var solution = new Solution();
            return await solution.LoadAsync(filename);
        }
        catch
        {
            // If file load fails, revert index
            lock (_lock)
            {
                _currentIndex++;
            }
            return null;
        }
    }

    /// <summary>
    /// Loads next state from history.
    /// </summary>
    public async Task<Solution?> RedoAsync()
    {
        string? filename;
        
        lock (_lock)
        {
            if (!CanRedo) return null;
            _currentIndex++;
            
            if (_currentIndex < 0 || _currentIndex >= _history.Count)
            {
                _currentIndex--; // Revert
                return null;
            }
            
            filename = _history[_currentIndex];
        }

        try
        {
            var solution = new Solution();
            return await solution.LoadAsync(filename);
        }
        catch
        {
            // If file load fails, revert index
            lock (_lock)
            {
                _currentIndex--;
            }
            return null;
        }
    }

    /// <summary>
    /// Clears all history files and disposes auto-save timer.
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            // Cancel and dispose auto-save timer
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
            
            foreach (var filename in _history)
            {
                DeleteHistoryFile(filename);
            }
            _history.Clear();
            _currentIndex = -1;
        }
    }

    private static void DeleteHistoryFile(string filename)
    {
        try
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
        catch
        {
            // Ignore errors when deleting history files
        }
    }

    /// <summary>
    /// Gets history statistics for debugging.
    /// </summary>
    public (int totalStates, int currentIndex, bool canUndo, bool canRedo) GetHistoryInfo()
    {
        lock (_lock)
        {
            return (_history.Count, _currentIndex, CanUndo, CanRedo);
        }
    }
}
