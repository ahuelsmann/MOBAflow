// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using SharedUI.ViewModel;

using Controls.SignalBox;

using Service;
using SharedUI.Interface;

using System;
using System.Collections.Specialized;
using System.ComponentModel;

using ViewModel;

sealed partial class SignalBoxPage
{
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<SignalBoxPage>? _logger;

    private double _toolboxExpandedWidth = 240;
    private double _canvasExpandedStarValue = 3;
    private double _propertiesExpandedStarValue = 1;

    public MainWindowViewModel ViewModel { get; }

    private SignalBoxPlanViewModel? _planViewModel;

    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ViessmannSignalService viessmannSignalService,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<SignalBoxPage>? logger = null,
        ILogger<SignalBoxPropertiesControl>? signalBoxPropertiesLogger = null,
        ILogger<SignalBoxCanvasControl>? signalBoxCanvasLogger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(viessmannSignalService);

        ViewModel = viewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;

        InitializeComponent();

        PropertiesControl.AttachRuntimeServices(viessmannSignalService, signalBoxPropertiesLogger);
        CanvasControl.AttachLogger(signalBoxCanvasLogger);

        ViewModel.SolutionLoaded += OnSolutionLoaded;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        DetachPlanViewModel();
        _planViewModel = null;
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadFromModel();
        });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsSignalBoxToolboxExpanded))
        {
            if (!ViewModel.IsSignalBoxToolboxExpanded)
            {
                if (ColToolbox.Width.IsAbsolute)
                {
                    _toolboxExpandedWidth = ColToolbox.Width.Value;
                }
                ColToolbox.Width = GridLength.Auto;
            }
            else
            {
                ColToolbox.Width = new GridLength(_toolboxExpandedWidth);
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsSignalBoxPropertiesExpanded))
        {
            if (!ViewModel.IsSignalBoxPropertiesExpanded)
            {
                if (ColCanvas.Width.IsStar)
                {
                    _canvasExpandedStarValue = ColCanvas.Width.Value;
                }
                if (ColProperties.Width.IsStar)
                {
                    _propertiesExpandedStarValue = ColProperties.Width.Value;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColCanvas.Width = new GridLength(_canvasExpandedStarValue, GridUnitType.Star);
                ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            DetachPlanViewModel();
            _planViewModel = null;
            DispatcherQueue.TryEnqueue(() =>
            {
                LoadFromModel();
            });
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        RestoreLayout();
        LoadFromModel();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
        ViewModel.SolutionLoaded -= OnSolutionLoaded;
        DetachPlanViewModel();
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
        var layout = _settings.Layout.SignalBoxPage;

        if (layout.ToolboxColumnWidth > 0)
        {
            _toolboxExpandedWidth = layout.ToolboxColumnWidth;
        }
        if (layout.PropertiesColumnStarValue > 0)
        {
            _propertiesExpandedStarValue = layout.PropertiesColumnStarValue;
        }
        if (layout.CanvasColumnStarValue > 0)
        {
            _canvasExpandedStarValue = layout.CanvasColumnStarValue;
        }

        ColCanvas.Width = new GridLength(_canvasExpandedStarValue, GridUnitType.Star);
        if (layout.IsToolboxExpanded)
        {
            ColToolbox.Width = new GridLength(_toolboxExpandedWidth);
        }
        else
        {
            ColToolbox.Width = GridLength.Auto;
        }

        if (layout.IsPropertiesExpanded)
        {
            ColProperties.Width = new GridLength(_propertiesExpandedStarValue, GridUnitType.Star);
        }
        else
        {
            ColProperties.Width = GridLength.Auto;
        }

        if (ViewModel.IsSignalBoxToolboxExpanded != layout.IsToolboxExpanded)
        {
            ViewModel.IsSignalBoxToolboxExpanded = layout.IsToolboxExpanded;
        }
        if (ViewModel.IsSignalBoxPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsSignalBoxPropertiesExpanded = layout.IsPropertiesExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.SignalBoxPage;

        layout.IsToolboxExpanded = ViewModel.IsSignalBoxToolboxExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsSignalBoxPropertiesExpanded;

        if (ColToolbox.Width.IsAbsolute)
        {
            layout.ToolboxColumnWidth = ColToolbox.Width.Value;
        }
        else if (!ViewModel.IsSignalBoxToolboxExpanded)
        {
            layout.ToolboxColumnWidth = _toolboxExpandedWidth;
        }

        if (ColProperties.Width.IsStar)
        {
            layout.PropertiesColumnStarValue = ColProperties.Width.Value;
        }
        else if (!ViewModel.IsSignalBoxPropertiesExpanded)
        {
            layout.PropertiesColumnStarValue = _propertiesExpandedStarValue;
        }

        if (ColCanvas.Width.IsStar)
        {
            layout.CanvasColumnStarValue = ColCanvas.Width.Value;
        }
        else
        {
            layout.CanvasColumnStarValue = _canvasExpandedStarValue;
        }
    }

    private void LoadFromModel()
    {
        var project = ViewModel.SelectedProject?.Model;
        if (project == null) return;

        project.SignalBoxPlan ??= new SignalBoxPlan
        {
            Name = "Stellwerk",
            Grid = new(32, 18, 60)
        };

        if (_planViewModel == null)
        {
            _planViewModel = new SignalBoxPlanViewModel(project.SignalBoxPlan);
            _planViewModel.PropertyChanged += OnPlanViewModelPropertyChanged;
            _planViewModel.Elements.CollectionChanged += OnElementsCollectionChanged;
        }

        if (CanvasControl != null)
        {
            CanvasControl.PlanViewModel = _planViewModel;
        }

        if (PropertiesControl != null)
        {
            PropertiesControl.PlanViewModel = _planViewModel;
            PropertiesControl.SelectedElement = _planViewModel.SelectedElement;
            PropertiesControl.UpdateStatistics();
        }

        UpdateElementCount();
    }

    private void UpdateElementCount()
    {
        if (ElementCountText == null)
        {
            return;
        }

        var count = _planViewModel?.Elements.Count ?? 0;
        ElementCountText.Text = $"Elements: {count}";
    }

    private void OnElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        UpdateElementCount();
    }

    private void OnPlanViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SignalBoxPlanViewModel.SelectedElement))
        {
            return;
        }

        if (PropertiesControl != null && _planViewModel != null)
        {
            PropertiesControl.SelectedElement = _planViewModel.SelectedElement;
        }
    }

    private void DetachPlanViewModel()
    {
        if (_planViewModel == null)
        {
            return;
        }

        _planViewModel.PropertyChanged -= OnPlanViewModelPropertyChanged;
        _planViewModel.Elements.CollectionChanged -= OnElementsCollectionChanged;
    }

    private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        DeleteSelectedElement();
    }

    private void OnDeleteAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        args.Handled = DeleteSelectedElement();
    }

    private bool DeleteSelectedElement()
    {
        var selected = _planViewModel?.SelectedElement;
        if (selected == null || _planViewModel == null)
        {
            return false;
        }

        _planViewModel.RemoveElement(selected);
        PropertiesControl?.UpdateStatistics();
        if (StatusText != null)
        {
            StatusText.Text = "Element deleted.";
        }
        return true;
    }

    private void OnPropertyControlVisualRefresh(object sender, SbElement element)
    {
        var index = _planViewModel?.Elements.IndexOf(element) ?? -1;
        if (index != -1 && _planViewModel != null)
        {
            _planViewModel.Elements.RemoveAt(index);
            _planViewModel.Elements.Insert(index, element);
            _planViewModel.SelectedElement = element;
        }
    }

    private void OnPropertyControlElementDeletion(object sender, SbElement element)
    {
        _planViewModel?.RemoveElement(element);
        PropertiesControl?.UpdateStatistics();
    }
}
