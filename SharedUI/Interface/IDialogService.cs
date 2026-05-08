// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Service for showing confirmation dialogs in a platform-agnostic way.
/// Implemented by WinUI/Maui with native dialog controls.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog with Yes/No options.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message content.</param>
    /// <param name="confirmButtonText">Text for the confirm/yes button.</param>
    /// <param name="cancelButtonText">Text for the cancel/no button.</param>
    /// <param name="isCancelDefault">True to make cancel the default button.</param>
    /// <returns>True if confirmed, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmButtonText = "Yes",
        string cancelButtonText = "No",
        bool isCancelDefault = true);
}
