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
    public Task InitializeAsync(Frame contentFrame)
    {
        _contentFrame = contentFrame ?? throw new ArgumentNullException(nameof(contentFrame));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to a page by tag identifier.
    /// Page instantiation via DI happens asynchronously to avoid blocking the UI thread.
    /// </summary>
    public async Task NavigateToPageAsync(string tag)
    {
        if (_contentFrame is null)
            throw new InvalidOperationException("NavigationService not initialized. Call InitializeAsync(Frame) first.");

        try
        {
            // Resolve page from DI container (potentially expensive operation)
            object page = tag switch
            {
                "overview" => _serviceProvider.GetRequiredService<OverviewPage>(),
                "solution" => _serviceProvider.GetRequiredService<SolutionPage>(),
                "journeys" => _serviceProvider.GetRequiredService<JourneysPage>(),
                "workflows" => _serviceProvider.GetRequiredService<WorkflowsPage>(),
                "trains" => _serviceProvider.GetRequiredService<TrainsPage>(),
                "trackplaneditor" => _serviceProvider.GetRequiredService<TrackPlanEditorPage>(),
                "journeymap" => _serviceProvider.GetRequiredService<JourneyMapPage>(),
                "settings" => _serviceProvider.GetRequiredService<SettingsPage>(),
                "monitor" => _serviceProvider.GetRequiredService<MonitorPage>(),
                _ => throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag))
            };

            _contentFrame.Content = page;
            
            await Task.CompletedTask; // Explicit async completion
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Navigation to '{tag}' failed: {ex.Message}");
            throw; // Re-throw for caller to handle
        }
    }

    /// <summary>
    /// Navigate to Overview page (default startup page).
    /// </summary>
    public Task NavigateToOverviewAsync()
    {
        return NavigateToPageAsync("overview");
    }
}