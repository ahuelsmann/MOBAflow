// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Common.Extension;
using SharedUI.ViewModel;

using SharedUI.Interface;

using Windows.ApplicationModel.DataTransfer;

internal sealed partial class StationsPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<StationsPage>? _logger;

    public MainWindowViewModel ViewModel { get; }

    private double _stationsExpandedWidth = 280;
    private double _platformsExpandedWidth = 280;
    private double _workflowLibExpandedWidth = 250;
    private double _propertiesExpandedWidth = 350;

    public StationsPage(
        MainWindowViewModel viewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<StationsPage>? logger = null)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsStationsListExpanded))
        {
            if (!ViewModel.IsStationsListExpanded)
            {
                if (ColStations.Width.IsAbsolute)
                {
                    _stationsExpandedWidth = ColStations.Width.Value;
                }
                ColStations.Width = GridLength.Auto;
            }
            else
            {
                ColStations.Width = new GridLength(_stationsExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsPlatformsListExpanded))
        {
            if (!ViewModel.IsPlatformsListExpanded)
            {
                if (ColPlatforms.Width.IsAbsolute)
                {
                    _platformsExpandedWidth = ColPlatforms.Width.Value;
                }
                ColPlatforms.Width = GridLength.Auto;
            }
            else
            {
                ColPlatforms.Width = new GridLength(_platformsExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWorkflowLibraryVisible))
        {
            if (!ViewModel.IsWorkflowLibraryVisible)
            {
                if (ColWorkflowLib.Width.IsAbsolute)
                {
                    _workflowLibExpandedWidth = ColWorkflowLib.Width.Value;
                }
                ColWorkflowLib.Width = GridLength.Auto;
            }
            else
            {
                ColWorkflowLib.Width = new GridLength(_workflowLibExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsStationsPropertiesExpanded))
        {
            if (!ViewModel.IsStationsPropertiesExpanded)
            {
                if (ColProperties.Width.IsAbsolute)
                {
                    _propertiesExpandedWidth = ColProperties.Width.Value;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = new GridLength(_propertiesExpandedWidth);
            }
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.StationsPage;

        // Restore Column Widths First
        if (layout.StationsColumnWidth > 0)
        {
            _stationsExpandedWidth = layout.StationsColumnWidth;
        }

        if (layout.IsStationsListExpanded)
        {
            ColStations.Width = new GridLength(_stationsExpandedWidth);
        }
        else
        {
            ColStations.Width = GridLength.Auto;
        }

        if (layout.PlatformsColumnWidth > 0)
        {
            _platformsExpandedWidth = layout.PlatformsColumnWidth;
        }

        if (layout.IsPlatformsListExpanded)
        {
            ColPlatforms.Width = new GridLength(_platformsExpandedWidth);
        }
        else
        {
            ColPlatforms.Width = GridLength.Auto;
        }

        if (layout.WorkflowLibraryColumnWidth > 0)
        {
            _workflowLibExpandedWidth = layout.WorkflowLibraryColumnWidth;
        }

        if (layout.IsWorkflowLibraryExpanded)
        {
            ColWorkflowLib.Width = new GridLength(_workflowLibExpandedWidth);
        }
        else
        {
            ColWorkflowLib.Width = GridLength.Auto;
        }

        if (layout.PropertiesColumnWidth > 0)
        {
            _propertiesExpandedWidth = layout.PropertiesColumnWidth;
        }

        if (layout.IsPropertiesExpanded)
        {
            ColProperties.Width = new GridLength(_propertiesExpandedWidth);
        }
        else
        {
            ColProperties.Width = GridLength.Auto;
        }

        // Restore CollapsibleColumn states
        if (ViewModel.IsStationsListExpanded != layout.IsStationsListExpanded)
        {
            ViewModel.IsStationsListExpanded = layout.IsStationsListExpanded;
        }
        if (ViewModel.IsPlatformsListExpanded != layout.IsPlatformsListExpanded)
        {
            ViewModel.IsPlatformsListExpanded = layout.IsPlatformsListExpanded;
        }
        if (ViewModel.IsWorkflowLibraryVisible != layout.IsWorkflowLibraryExpanded)
        {
            ViewModel.IsWorkflowLibraryVisible = layout.IsWorkflowLibraryExpanded;
        }
        if (ViewModel.IsStationsPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsStationsPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.StationsPage;

        // Save CollapsibleColumn states
        layout.IsStationsListExpanded = ViewModel.IsStationsListExpanded;
        layout.IsPlatformsListExpanded = ViewModel.IsPlatformsListExpanded;
        layout.IsWorkflowLibraryExpanded = ViewModel.IsWorkflowLibraryVisible;
        layout.IsPropertiesExpanded = ViewModel.IsStationsPropertiesExpanded;

        // Save Column Widths
        if (ColStations.Width.IsAbsolute)
        {
            layout.StationsColumnWidth = ColStations.Width.Value;
        }
        else if (!ViewModel.IsStationsListExpanded)
        {
            layout.StationsColumnWidth = _stationsExpandedWidth;
        }

        if (ColPlatforms.Width.IsAbsolute)
        {
            layout.PlatformsColumnWidth = ColPlatforms.Width.Value;
        }
        else if (!ViewModel.IsPlatformsListExpanded)
        {
            layout.PlatformsColumnWidth = _platformsExpandedWidth;
        }

        if (ColWorkflowLib.Width.IsAbsolute)
        {
            layout.WorkflowLibraryColumnWidth = ColWorkflowLib.Width.Value;
        }
        else if (!ViewModel.IsWorkflowLibraryVisible)
        {
            layout.WorkflowLibraryColumnWidth = _workflowLibExpandedWidth;
        }

        if (ColProperties.Width.IsAbsolute)
        {
            layout.PropertiesColumnWidth = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsStationsPropertiesExpanded)
        {
            layout.PropertiesColumnWidth = _propertiesExpandedWidth;
        }
    }

    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is WorkflowViewModel workflow)
        {
            e.Data.Properties.Add("Workflow", workflow);
            e.Data.RequestedOperation = DataPackageOperation.Link;
            e.Data.SetText(workflow.Name);
        }
    }
}
