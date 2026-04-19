// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using Moba.SharedUI.ViewModel.Dialogs;
using Moba.TrackLibrary.PikoA.Import;
using Moba.Vision;
using Moba.WinUI.View.Dialogs;

/// <summary>
/// Screenshot-to-TrackPlan import flow. Reads an image through <see cref="IVisionService"/>,
/// extracts PIKO A catalog codes with <see cref="PikoACodeExtractor"/>, shows the review
/// dialog, and finally imports the user-approved candidates as loose
/// <see cref="PlacedSegment"/>s through <see cref="VisionTrackPlanImporter"/>.
/// </summary>
public sealed partial class TrackPlanPage
{
    private async Task ImportFromScreenshotAsync()
    {
        if (_visionService is null || !_visionService.IsConfigured)
        {
            await ShowVisionUnavailableAsync();
            return;
        }

        var mainWindow = App.MainWindow;
        if (mainWindow is null || Content.XamlRoot is null)
        {
            _logger?.LogWarning("Import screenshot aborted: window or XamlRoot unavailable");
            return;
        }

        // 1) Pick the image file
        var picker = new FileOpenPicker(mainWindow.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".tiff");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        // 2) Run Azure AI Vision
        VisionReadResult readResult;
        try
        {
            readResult = await _visionService.ReadTextAsync(file.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Azure AI Vision call failed for {Path}", file.Path);
            await ShowErrorAsync("Azure AI Vision call failed", ex.Message);
            return;
        }

        var extraction = PikoACodeExtractor.Extract(readResult);
        _logger?.LogInformation(
            "Screenshot import: {Matches} matches, {Unresolved} unresolved ({W}x{H}px)",
            extraction.Matches.Count, extraction.Unresolved.Count,
            readResult.ImageWidth, readResult.ImageHeight);

        if (extraction.Matches.Count == 0 && extraction.Unresolved.Count == 0)
        {
            await ShowErrorAsync(
                "No PIKO A codes detected",
                "Azure AI Vision did not find any recognizable PIKO A track codes in this image. " +
                "Try a higher-resolution screenshot or make sure the labels are visible.");
            return;
        }

        // 3) Show review dialog
        var vm = new VisionImportDialogViewModel(
            extraction,
            readResult.ImageWidth,
            readResult.ImageHeight);
        var dialog = new VisionImportDialog(vm, Content.XamlRoot);

        var outcome = await dialog.ShowAsync();
        if (outcome != ContentDialogResult.Primary)
        {
            _logger?.LogInformation("Screenshot import cancelled by user");
            return;
        }

        if (!vm.IsValidScale)
        {
            await ShowErrorAsync(
                "Invalid scale",
                "The pixels-per-millimeter scale must be a positive number. Import aborted.");
            return;
        }

        // 4) Import
        var toImport = vm.BuildImportList();
        if (toImport.Count == 0)
        {
            _logger?.LogInformation("Screenshot import: no candidates selected");
            return;
        }

        var added = VisionTrackPlanImporter.Import(_plan, toImport, vm.PixelsPerMillimeter);
        _logger?.LogInformation("Screenshot import: {Added} segments added to the track plan", added);

        // Force a redraw after bulk-add
        GraphCanvasControl.Invalidate();

        await ShowInfoAsync(
            "Import complete",
            $"Added {added} track segments to the plan. " +
            "They are placed at the OCR coordinates with rotation 0° — arrange/snap them in the editor.");
    }

    private async Task ShowVisionUnavailableAsync()
    {
        await ShowErrorAsync(
            "Azure AI Vision not configured",
            "To import from a screenshot, configure Azure AI Vision in Settings → Azure AI Vision (Key + Endpoint).");
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dlg.ShowAsync();
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dlg.ShowAsync();
    }
}
