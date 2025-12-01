// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

/// <summary>
/// Abstraction for platform-specific file picker.
/// Implementations live in platform projects (WinUI, MAUI).
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Opens a file picker for JSON files and returns the selected file path or null if cancelled.
    /// </summary>
    Task<string?> PickJsonFileAsync();
}
