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
    }

    /// <summary>
    /// Indicates whether post-startup initialization is running.
    /// </summary>
    [ObservableProperty]
    private bool isPostStartupInitializationRunning;

    /// <summary>
    /// Status text for post-startup initialization.
    /// </summary>
    [ObservableProperty]
    private string postStartupStatusText = string.Empty;

    /// <summary>
    /// Updates the post-startup initialization status displayed in the status bar.
    /// </summary>
    public void UpdatePostStartupInitializationStatus(bool isRunning, string statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            statusText = isRunning ? "Initializing services..." : string.Empty;
        }

        _uiDispatcher.EnqueueOnUi(() =>
        {
            IsPostStartupInitializationRunning = isRunning;
            PostStartupStatusText = statusText;
        });
    }
}
