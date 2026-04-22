namespace Moba.WinUI.View;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using SharedUI.ViewModel;

using Controls.SignalBox;

using Service;

using System;
using System.Collections.Specialized;
using System.ComponentModel;

using ViewModel;

sealed partial class SignalBoxPage
{
    private GridLength _toolboxExpandedWidth = new(240);
    private GridLength _propertiesExpandedWidth = new(300);

    public MainWindowViewModel ViewModel { get; }

    private SignalBoxPlanViewModel? _planViewModel;

    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ViessmannSignalService viessmannSignalService,
        ILogger<SignalBoxPropertiesControl>? signalBoxPropertiesLogger = null,
        ILogger<SignalBoxCanvasControl>? signalBoxCanvasLogger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(viessmannSignalService);

        ViewModel = viewModel;

        InitializeComponent();

        PropertiesControl.AttachRuntimeServices(viessmannSignalService, signalBoxPropertiesLogger);
        CanvasControl.AttachLogger(signalBoxCanvasLogger);

        ViewModel.SolutionLoaded += OnSolutionLoaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

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
                if (!ColToolbox.Width.IsAuto) _toolboxExpandedWidth = ColToolbox.Width;
                ColToolbox.Width = GridLength.Auto;
            }
            else
            {
                ColToolbox.Width = _toolboxExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsSignalBoxPropertiesExpanded))
        {
            if (!ViewModel.IsSignalBoxPropertiesExpanded)
            {
                if (!ColProperties.Width.IsAuto) _propertiesExpandedWidth = ColProperties.Width;
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = _propertiesExpandedWidth;
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
        LoadFromModel();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        ViewModel.SolutionLoaded -= OnSolutionLoaded;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        DetachPlanViewModel();
        Loaded -= OnPageLoaded;
        Unloaded -= OnPageUnloaded;
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
