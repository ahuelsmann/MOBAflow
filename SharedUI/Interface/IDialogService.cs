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

    /// <summary>
    /// Shows a dialog with a primary, a secondary and a cancel button; the primary button is the default.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message content.</param>
    /// <param name="primaryButtonText">Text for the primary button.</param>
    /// <param name="secondaryButtonText">Text for the secondary button.</param>
    /// <param name="cancelButtonText">Text for the cancel button.</param>
    /// <returns>The chosen button, or <see cref="DialogChoice.NotShown"/> when no dialog could be shown.</returns>
    Task<DialogChoice> ShowChoiceAsync(
        string title,
        string message,
        string primaryButtonText,
        string secondaryButtonText,
        string cancelButtonText);
}