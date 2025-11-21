// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Data;
using Backend.Model;

using System.Threading.Tasks;

public interface IIoService
{
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath);
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath);
    Task<(DataManager? dataManager, string? path, string? error)> LoadDataManagerAsync();
}