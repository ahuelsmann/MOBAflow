// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Controls;
using Microsoft.UI.Xaml;
using SharedUI.Interface;

/// <summary>
/// WinUI implementation of <see cref="IFunctionAppearancePicker"/> backed by <see cref="FunctionSymbolPickerWindow"/>.
/// </summary>
public sealed class WinUiFunctionAppearancePicker : IFunctionAppearancePicker
{
    /// <inheritdoc />
    public async Task<FunctionAppearancePickerResult?> PickAsync(
        FunctionAppearancePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var picker = new FunctionSymbolPickerWindow
        {
            SelectedTheme = ResolveTheme()
        };
        picker.SetInitialColor(request.InitialColorHex);

        var confirmed = await picker.ShowDialogAsync().ConfigureAwait(true);
        cancellationToken.ThrowIfCancellationRequested();

        if (!confirmed || !picker.IsConfirmed)
        {
            return null;
        }

        return new FunctionAppearancePickerResult(
            IsConfirmed: true,
            IsSelectionCleared: picker.IsSelectionCleared,
            Glyph: picker.SelectedGlyph,
            ColorHex: picker.SelectedColorHex);
    }

    private static ElementTheme ResolveTheme()
    {
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            return root.ActualTheme == ElementTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        }

        return ElementTheme.Default;
    }
}
