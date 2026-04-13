namespace Moba.WinUI.View;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using Moba.WinUI.Controls.SignalBox;

using Service;

using System;
using System.ComponentModel;

using ViewModel;

sealed partial class SignalBoxPage
{
    private GridLength _toolboxExpandedWidth = new(240);
    private GridLength _propertiesExpandedWidth = new(300);

    public MainWindowViewModel ViewModel { get; }
    public SkinSelectorViewModel SkinViewModel { get; }
    
    private SignalBoxPlanViewModel? _planViewModel;
    private readonly ISkinProvider _skinProvider;

    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ISkinProvider skinProvider,
        SkinSelectorViewModel skinViewModel,
        ViessmannSignalService viessmannSignalService,
        ILogger<SignalBoxPropertiesControl>? signalBoxPropertiesLogger = null,
        ILogger<SignalBoxCanvasControl>? signalBoxCanvasLogger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(skinProvider);
        ArgumentNullException.ThrowIfNull(skinViewModel);
        ArgumentNullException.ThrowIfNull(viessmannSignalService);

        ViewModel = viewModel;
        _skinProvider = skinProvider;
        SkinViewModel = skinViewModel;

        InitializeComponent();

        PropertiesControl.AttachRuntimeServices(viessmannSignalService, signalBoxPropertiesLogger);
        CanvasControl.AttachLogger(signalBoxCanvasLogger);

        _skinProvider.SkinChanged += OnSkinProviderChanged;
        _skinProvider.DarkModeChanged += OnDarkModeChanged;

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

    private void OnSkinProviderChanged(object? sender, SkinChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void OnDarkModeChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        LoadFromModel();
        ApplySkinColors();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        _skinProvider.SkinChanged -= OnSkinProviderChanged;
        _skinProvider.DarkModeChanged -= OnDarkModeChanged;
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
    }

    private void OnGridToggled(object sender, RoutedEventArgs e)
    {
        // Handled directly or by passing IsGridVisible property to CanvasControl
        // Optional implementation:
        // if (CanvasControl != null) CanvasControl.IsGridVisible = GridToggleButton.IsChecked ?? false;
    }

    private void ApplySkinColors()
    {
        var palette = SkinColors.GetPalette(_skinProvider.CurrentSkin, _skinProvider.IsDarkMode);
        var isSystemSkin = _skinProvider.CurrentSkin == AppSkin.System;
        
        RequestedTheme = palette.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        if (CanvasControl != null)
        {
            if (!isSystemSkin)
            {
                CanvasControl.Background = palette.PanelBackgroundBrush;
            }
            else
            {
                CanvasControl.Background = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"];
            }
        }
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
