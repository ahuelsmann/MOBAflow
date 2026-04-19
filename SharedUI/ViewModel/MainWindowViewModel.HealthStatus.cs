// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// MainWindowViewModel - Health Status UI Logic (partial class).
/// Handles health check status display for Speech Service.
/// Extracted from MainWindow.xaml.cs code-behind.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Gets the speech status text shown in the status bar.
    /// Uses Azure health details for Azure engine and a local-ready text for System Speech.
    /// </summary>
    public string SpeechStatusDisplayText =>
        IsAzureSpeechEngineSelected
            ? SpeechHealthStatus
            : "System Speech: Ready (local)";

    /// <summary>
    /// Update health status UI properties based on status message.
    /// Called by HealthCheckService via event.
    /// </summary>
    public void UpdateHealthStatus(string statusMessage)
    {
        // Prefix with "Azure Speech: " for UI display
        SpeechHealthStatus = $"Azure Speech: {statusMessage.TrimStart('✅', '❌', '⚠', '️', '⏳', ' ')}";

        // Update icon and color based on status
        if (statusMessage.Contains("Ready"))
        {
            SpeechHealthIcon = "\uE930"; // Checkmark circle
            SpeechHealthColor = "Green";
        }
        else if (statusMessage.Contains("Not Configured"))
        {
            SpeechHealthIcon = "\uE7BA"; // Warning
            SpeechHealthColor = "SystemFillColorCautionBrush";
        }
        else if (statusMessage.Contains("Failed"))
        {
            SpeechHealthIcon = "\uE711"; // Error
            SpeechHealthColor = "Red";
        }
        else // Initializing
        {
            SpeechHealthIcon = "\uE946"; // Sync
            SpeechHealthColor = "SystemFillColorCautionBrush";
        }

        OnPropertyChanged(nameof(SpeechStatusDisplayText));
        RecomputeOperatingState();
    }

    /// <summary>
    /// Update Azure AI Vision health status UI properties based on the raw status message
    /// emitted by the <c>HealthCheckService</c>.
    /// </summary>
    public void UpdateVisionHealthStatus(string statusMessage)
    {
        VisionHealthStatus = $"Azure AI Vision: {statusMessage.TrimStart('✅', '❌', '⚠', '️', '⏳', ' ')}";

        if (statusMessage.Contains("Ready"))
        {
            VisionHealthIcon = "\uE930"; // Checkmark circle
            VisionHealthColor = "Green";
        }
        else if (statusMessage.Contains("Not Configured"))
        {
            VisionHealthIcon = "\uE7BA"; // Warning
            VisionHealthColor = "SystemFillColorCautionBrush";
        }
        else if (statusMessage.Contains("Failed"))
        {
            VisionHealthIcon = "\uE711"; // Error
            VisionHealthColor = "Red";
        }
        else // Initializing
        {
            VisionHealthIcon = "\uE946"; // Sync
            VisionHealthColor = "SystemFillColorCautionBrush";
        }
    }

    /// <summary>
    /// Indicates whether post-startup initialization is running.
    /// </summary>
    [ObservableProperty]
    private bool _isPostStartupInitializationRunning;

    /// <summary>
    /// Status text for post-startup initialization.
    /// </summary>
    [ObservableProperty]
    private string _postStartupStatusText = string.Empty;

    /// <summary>
    /// Updates the post-startup initialization status displayed in the status bar.
    /// </summary>
    public void UpdatePostStartupInitializationStatus(bool isRunning, string statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            statusText = isRunning ? "Initializing services..." : string.Empty;
        }

        IsPostStartupInitializationRunning = isRunning;
        PostStartupStatusText = statusText;
        RecomputeOperatingState();
    }
}
