// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Backend.Data;
using Domain;

using System.Threading.Tasks;

public interface IIoService
{
    /// <summary>
    /// Creates a new empty solution and updates the DI singleton.
    /// Prompts user for confirmation if unsaved changes exist.
    /// </summary>
    /// <param name="hasUnsavedChanges">Whether current solution has unsaved changes</param>
    Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges);
    
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath);
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath);
    
    /// <summary>
    /// Loads city master data using legacy Backend.Data.DataManager format.
    /// This method is obsolete - use ICityService.LoadCitiesAsync() instead.
    /// </summary>
    [Obsolete("Use ICityService.LoadCitiesAsync() instead. This method loads deprecated Backend.Data.City format.")]
    Task<(DataManager? dataManager, string? path, string? error)> LoadDataManagerAsync();
}
