// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using View;

/// <summary>
/// Navigation service for MOBAflow WinUI application.
/// Handles page navigation via DI-resolved pages.
/// Extracted from MainWindow.xaml.cs to reduce code-behind complexity.
/// </summary>
public class NavigationService
{
    #region Fields
    private readonly IServiceProvider _serviceProvider;
    private Frame? _contentFrame;
    #endregion

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initialize the navigation service with the content frame.
    /// Must be called before any navigation operations.
    /// </summary>
    public void Initialize(Frame contentFrame)
    {
        _contentFrame = contentFrame ?? throw new ArgumentNullException(nameof(contentFrame));
    }

    /// <summary>
    /// Navigate to a page by tag identifier.
    /// </summary>
    public void NavigateToPage(string tag)
    {
            if (_contentFrame == null)
                throw new InvalidOperationException("NavigationService not initialized. Call Initialize(Frame) first.");

            try
            {
                object page = tag switch
                {
                    "overview" => _serviceProvider.GetRequiredService<OverviewPage>(),
                    "solution" => _serviceProvider.GetRequiredService<SolutionPage>(),
                    "journeys" => _serviceProvider.GetRequiredService<JourneysPage>(),
                    "workflows" => _serviceProvider.GetRequiredService<WorkflowsPage>(),
                    "feedbackpoints" => _serviceProvider.GetRequiredService<FeedbackPointsPage>(),
                    "trackplaneditor" => _serviceProvider.GetRequiredService<TrackPlanEditorPage>(),
                    "journeymap" => _serviceProvider.GetRequiredService<JourneyMapPage>(),
                    "settings" => _serviceProvider.GetRequiredService<SettingsPage>(),
                    "monitor" => _serviceProvider.GetRequiredService<MonitorPage>(),
                    _ => throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag))
                };

                _contentFrame.Content = page;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Navigation to '{tag}' failed: {ex.Message}");
            }
        }

    /// <summary>
    /// Navigate to Overview page (default startup page).
    /// </summary>
    public void NavigateToOverview()
    {
        NavigateToPage("overview");
    }
}
