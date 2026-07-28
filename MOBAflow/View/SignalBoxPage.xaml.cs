// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Controls.SignalBox;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

sealed partial class SignalBoxPage
{
    private static readonly Action<ILogger, Exception?> LogPropertyPersistenceFailure =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(LogPropertyPersistenceFailure)),
            "Persist signal-box property change failed");

    private static readonly Action<ILogger, Exception?> LogSignalAspectFailure =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(LogSignalAspectFailure)),
            "Set signal aspect failed");

    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<SignalBoxPage>? _logger;
    private readonly SignalBoxPropertiesViewModel _propertiesViewModel;

    private double _toolboxExpandedWidth = 240;
    private double _canvasExpandedStarValue = 3;
    private double _propertiesExpandedStarValue = 1;

    public MainWindowViewModel ViewModel { get; }
    public InterlockingControlViewModel InterlockingViewModel { get; }

    private SignalBoxPlanViewModel? _planViewModel;

    public SignalBoxPage(
        MainWindowViewModel viewModel,
        InterlockingControlViewModel interlockingViewModel,
        SignalBoxPropertiesViewModel propertiesViewModel,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<SignalBoxPage>? logger = null,
        ILogger<SignalBoxCanvasControl>? signalBoxCanvasLogger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(interlockingViewModel);
        ArgumentNullException.ThrowIfNull(propertiesViewModel);

        ViewModel = viewModel;
        InterlockingViewModel = interlockingViewModel;
        _propertiesViewModel = propertiesViewModel;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;

        InitializeComponent();

        PropertiesControl.EditorViewModel = _propertiesViewModel;
        CanvasControl.AttachLogger(signalBoxCanvasLogger);

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnSignalBoxRuntimeStateChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshSignalBoxRuntimeVisuals);
    }

    private void RefreshSignalBoxRuntimeVisuals()
    {
        PropertiesControl?.RefreshAspectDisplay();

        if (_planViewModel == null)
        {
            return;
        }

        foreach (var signal in _planViewModel.Elements.OfType<SbSignal>().ToList())
        {
            _planViewModel.RefreshElementVisual(signal);
        }
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
        InterlockingViewModel.StartObserving();
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.SolutionLoaded -= OnSolutionLoaded;
        ViewModel.SolutionLoaded += OnSolutionLoaded;
        ViewModel.SignalBoxRuntimeStateChanged -= OnSignalBoxRuntimeStateChanged;
        ViewModel.SignalBoxRuntimeStateChanged += OnSignalBoxRuntimeStateChanged;
        _propertiesViewModel.ElementChanged -= OnPropertyEditorElementChanged;
        _propertiesViewModel.ElementChanged += OnPropertyEditorElementChanged;
        _propertiesViewModel.DeletionRequested -= OnPropertyEditorDeletionRequested;
        _propertiesViewModel.DeletionRequested += OnPropertyEditorDeletionRequested;
        RestoreLayout();
        LoadFromModel();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
        ViewModel.SolutionLoaded -= OnSolutionLoaded;
        ViewModel.SignalBoxRuntimeStateChanged -= OnSignalBoxRuntimeStateChanged;
        _propertiesViewModel.ElementChanged -= OnPropertyEditorElementChanged;
        _propertiesViewModel.DeletionRequested -= OnPropertyEditorDeletionRequested;
        InterlockingViewModel.StopObserving();
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
            Name = "Signal box",
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
            _propertiesViewModel.SelectedElement = _planViewModel.SelectedElement;
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

        if (_planViewModel != null)
        {
            _propertiesViewModel.SelectedElement = _planViewModel.SelectedElement;
            InterlockingViewModel.SelectSignalBoxRepresentation(_planViewModel.SelectedElement?.Id);
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
        _propertiesViewModel.SelectedElement = null;
    }

    private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        DeleteSelectedElement();
    }

    private void OnContextButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        this.DispatcherQueue.TryEnqueue(() => this.SelectedObjectWorkbench.FocusWorkbench());
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
        if (StatusText != null)
        {
            StatusText.Text = "Element deleted.";
        }
        return true;
    }

    private void OnPropertyEditorElementChanged(
        object? sender,
        SignalBoxPropertyChangeEventArgs args)
    {
        _ = sender;

        if (args.RequiresVisualRefresh)
        {
            _planViewModel?.RefreshElementVisual(args.Element);
        }

        if (args.RequiresPersistence)
        {
            ViewModel.SaveSolutionInternalAsync().Observe(LogPropertyPersistenceException);
        }

        if (args.RequiresSignalCommand && args.Element is SbSignal signal)
        {
            ViewModel.SetSignalAspectAsync(signal).Observe(LogSignalAspectException);
        }
    }

    private void LogPropertyPersistenceException(Exception exception)
    {
        if (_logger is not null)
        {
            LogPropertyPersistenceFailure(_logger, exception);
        }
    }

    private void LogSignalAspectException(Exception exception)
    {
        if (_logger is not null)
        {
            LogSignalAspectFailure(_logger, exception);
        }
    }

    private void OnPropertyEditorDeletionRequested(
        object? sender,
        SignalBoxElementEventArgs args)
    {
        _ = sender;
        _planViewModel?.RemoveElement(args.Element);
    }
}
