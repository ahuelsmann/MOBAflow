// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.Interface;

using View;

/// <summary>
/// WinUI implementation of IDialogService using ContentDialog.
/// XamlRoot is resolved lazily to avoid DI circular dependency during startup.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private XamlRoot? _cachedXamlRoot;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the XamlRoot lazily from the MainWindow (resolved on first use to avoid startup deadlock).
    /// </summary>
    private XamlRoot? GetXamlRoot()
    {
        if (_cachedXamlRoot != null)
            return _cachedXamlRoot;

        var mainWindow = _serviceProvider.GetService(typeof(MainWindow)) as MainWindow;
        _cachedXamlRoot = mainWindow?.Content?.XamlRoot;
        return _cachedXamlRoot;
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmButtonText = "Yes",
        string cancelButtonText = "No",
        bool isCancelDefault = true)
    {
        var xamlRoot = GetXamlRoot();
        if (xamlRoot == null)
        {
            // Fallback: keine Dialoge möglich ohne XamlRoot (z.B. Headless-Tests)
            return false;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmButtonText,
            SecondaryButtonText = cancelButtonText,
            DefaultButton = isCancelDefault ? ContentDialogButton.Secondary : ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
