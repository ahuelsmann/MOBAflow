// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel.Dialogs;

/// <summary>
/// Review dialog shown after Azure AI Vision has extracted PIKO A codes from a screenshot.
/// The user confirms which matches to import and optionally resolves OCR artifacts via a
/// per-token dropdown. Result is read through <see cref="ViewModel"/>.<c>BuildImportList()</c>.
/// </summary>
public sealed partial class VisionImportDialog : ContentDialog
{
    public VisionImportDialogViewModel ViewModel { get; }

    public VisionImportDialog(VisionImportDialogViewModel viewModel, XamlRoot xamlRoot)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(xamlRoot);

        ViewModel = viewModel;
        XamlRoot = xamlRoot;

        InitializeComponent();
    }
}
