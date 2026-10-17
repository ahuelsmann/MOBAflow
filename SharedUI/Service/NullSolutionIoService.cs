// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Domain;

using Interface;

/// <summary>
/// Solution file access for hosts without files (MAUI remote, tests). It never loads or saves.
/// </summary>
public sealed class NullSolutionIoService : ISolutionIoService
{
    private const string NotSupported = "File operations not supported on this platform";

    /// <inheritdoc />
    public bool CanPersist => false;

    /// <inheritdoc />
    public Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges) =>
        Task.FromResult<(bool, bool, string?)>((false, false, NotSupported));

    /// <inheritdoc />
    public Task<(Solution? solution, string? path, string? error)> LoadAsync() =>
        Task.FromResult<(Solution?, string?, string?)>((null, null, NotSupported));

    /// <inheritdoc />
    public Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath) =>
        Task.FromResult<(Solution?, string?, string?)>((null, null, NotSupported));

    /// <inheritdoc />
    public Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string currentPath) =>
        Task.FromResult<(bool, string?, string?)>((false, null, NotSupported));

    /// <inheritdoc />
    public Task<(bool success, string? path, string? error)> SaveAsAsync(Solution solution) =>
        Task.FromResult<(bool, string?, string?)>((false, null, NotSupported));
}
