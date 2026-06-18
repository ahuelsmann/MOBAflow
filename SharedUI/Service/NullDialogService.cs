// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// Null implementation of IDialogService - always returns false (not confirmed).
/// Used when no dialog service is available (e.g., MAUI, headless tests).
/// </summary>
public sealed class NullDialogService : IDialogService
{
    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmButtonText = "Yes",
        string cancelButtonText = "No",
        bool isCancelDefault = true)
    {
        // Always returns false (not confirmed) - no dialogs in headless/testing mode
        return Task.FromResult(false);
    }
}