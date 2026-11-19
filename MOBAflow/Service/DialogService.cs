// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.Interface;

using System.Runtime.InteropServices;

using View;

/// <summary>
/// WinUI implementation of IDialogService using ContentDialog.
/// XamlRoot is resolved lazily to avoid DI circular dependency during startup.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <summary>E_ASYNC_OPERATION_NOT_STARTED, raised by a second ContentDialog.ShowAsync while one is open.</summary>
    private const int AsyncOperationNotStartedHResult = unchecked((int)0x80000019);

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

    /// <inheritdoc />
    public async Task<DialogChoice> ShowChoiceAsync(
        string title,
        string message,
        string primaryButtonText,
        string secondaryButtonText,
        string cancelButtonText)
    {
        var xamlRoot = GetXamlRoot();
        if (xamlRoot == null || IsContentDialogOpen(xamlRoot))
        {
            // Without a window, or while another dialog is open (WinUI shows one ContentDialog at a time),
            // no dialog can be shown; the caller chooses its safe default.
            return DialogChoice.NotShown;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = cancelButtonText,
            DefaultButton = ContentDialogButton.Primary
        };

        try
        {
            return ToChoice(await dialog.ShowAsync());
        }
        catch (COMException ex) when (IsAnotherDialogOpen(ex.HResult))
        {
            // A dialog opened between the check above and ShowAsync.
            return DialogChoice.NotShown;
        }
    }

    /// <summary>Maps the button the operator pressed to the platform-neutral choice.</summary>
    internal static DialogChoice ToChoice(ContentDialogResult result) => result switch
    {
        ContentDialogResult.Primary => DialogChoice.Primary,
        ContentDialogResult.Secondary => DialogChoice.Secondary,
        _ => DialogChoice.Cancel
    };

    /// <summary>
    /// Whether <see cref="ContentDialog.ShowAsync()"/> failed because another dialog is open: WinUI reports
    /// "Only a single ContentDialog can be open at any time" as E_ASYNC_OPERATION_NOT_STARTED.
    /// </summary>
    internal static bool IsAnotherDialogOpen(int hResult) => hResult == AsyncOperationNotStartedHResult;

    private static bool IsContentDialogOpen(XamlRoot xamlRoot) =>
        VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Any(popup => popup.Child is ContentDialog);
}
