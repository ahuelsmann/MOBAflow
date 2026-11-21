// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using SharedUI.Interface;

using System.Runtime.InteropServices;

/// <summary>
/// Asks before a matrix image is deleted. A dialog that cannot be shown counts as Cancel, and the answer only counts
/// while the same project is still selected and the matrix is still listed.
/// </summary>
internal static class MatrixDeleteConfirmation
{
    internal const string PlaceholderName = "New Matrix";

    internal static async Task<bool> ConfirmAsync(
        IDialogService dialogService,
        ISolutionSession solutionSession,
        string? name,
        Func<bool> isStillListed)
    {
        var project = solutionSession.SelectedProject;
        var displayName = string.IsNullOrWhiteSpace(name) ? PlaceholderName : name;

        bool confirmed;
        try
        {
            confirmed = await dialogService.ShowConfirmationAsync(
                "Delete Matrix",
                $"Delete matrix image '{displayName}'?",
                "Delete",
                "Cancel",
                isCancelDefault: true).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException)
        {
            // WinUI shows one ContentDialog at a time; a dialog that cannot be shown keeps the matrix.
            return false;
        }

        return confirmed
            && ReferenceEquals(solutionSession.SelectedProject, project)
            && isStillListed();
    }
}
