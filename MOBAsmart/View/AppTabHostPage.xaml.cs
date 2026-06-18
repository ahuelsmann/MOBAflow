// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;



using Controls;



using SharedUI.ViewModel;



using System.ComponentModel;



public partial class AppTabHostPage

{

    private readonly IServiceProvider _serviceProvider;

    private readonly MauiViewModel _mauiViewModel;

    private CounterPage? _counterPage;

    private SignalBoxPage? _signalBoxPage;

    private ControlPage? _controlPage;

    private readonly Microsoft.Maui.Controls.View?[] _tabContents = new Microsoft.Maui.Controls.View?[3];

    private int _activeTabIndex = AppBottomTabBar.CounterTabIndex;

    private bool _initialTabScheduled;

    private int _tabActivationVersion;



    public AppTabHostPage(IServiceProvider serviceProvider, MauiViewModel mauiViewModel)

    {

        _serviceProvider = serviceProvider;

        _mauiViewModel = mauiViewModel;



        InitializeComponent();

        BindingContext = _mauiViewModel;

        SessionLockOverlay.BindingContext = _mauiViewModel;

        _mauiViewModel.PropertyChanged += OnMauiViewModelPropertyChanged;

    }



    protected override void OnAppearing()

    {

        base.OnAppearing();

        ScheduleInitialTabIfNeeded();

    }



    private void OnMauiViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)

    {

        if (e.PropertyName is nameof(MauiViewModel.IsSessionOperational)

            or nameof(MauiViewModel.IsSignalBoxAvailable)

            or nameof(MauiViewModel.IsControlAvailable))

        {

            UpdateSessionLockOverlay(_activeTabIndex);

        }

    }



    private void ScheduleInitialTabIfNeeded()

    {

        if (_initialTabScheduled)

        {

            return;

        }



        _initialTabScheduled = true;

        Dispatcher.DispatchAsync(() => ShowTab(AppBottomTabBar.CounterTabIndex));

    }



    private void OnTabSelected(object? sender, int tabIndex) => ShowTab(tabIndex);



    private void ShowTab(int tabIndex)

    {

        if (tabIndex < 0 || tabIndex >= _tabContents.Length)

        {

            return;

        }



        DeactivateTabIfNeeded(_activeTabIndex, tabIndex);



        BottomTabBar.SelectedTab = tabIndex;

        var content = EnsureTabContent(tabIndex);

        var activationVersion = ++_tabActivationVersion;



        PageHost.Content = content;



        Dispatcher.DispatchAsync(() =>

        {

            if (activationVersion != _tabActivationVersion)

            {

                return;

            }



            FinishTabActivation(tabIndex, content);

        });

    }



    private void DeactivateTabIfNeeded(int previousTabIndex, int nextTabIndex)

    {

        if (previousTabIndex == AppBottomTabBar.ControlTabIndex && nextTabIndex != AppBottomTabBar.ControlTabIndex)

        {

            _controlPage?.DeactivateTab();

        }



        if (previousTabIndex == AppBottomTabBar.SignalBoxTabIndex && nextTabIndex != AppBottomTabBar.SignalBoxTabIndex)

        {

            _signalBoxPage?.DeactivateTab();

        }



        if (previousTabIndex == AppBottomTabBar.CounterTabIndex && nextTabIndex != AppBottomTabBar.CounterTabIndex)

        {

            _counterPage?.DeactivateTab();

        }

    }



    private void FinishTabActivation(int tabIndex, Microsoft.Maui.Controls.View content)

    {

        switch (tabIndex)

        {

            case AppBottomTabBar.CounterTabIndex:

                GetCounterPage().ActivateTab();

                break;

            case AppBottomTabBar.SignalBoxTabIndex:

                _signalBoxPage?.ActivateTab();

                break;

            case AppBottomTabBar.ControlTabIndex:

                _controlPage?.ActivateTab();

                break;

        }



        var bindingContext = GetPageBindingContext(tabIndex);

        PageHost.BindingContext = bindingContext;

        content.BindingContext = bindingContext;



        UpdateMauiViewModelTabState(tabIndex);

        _activeTabIndex = tabIndex;

        UpdateSessionLockOverlay(tabIndex);

    }



    private void UpdateSessionLockOverlay(int tabIndex)

    {

        var isLockedTab = tabIndex is AppBottomTabBar.SignalBoxTabIndex or AppBottomTabBar.ControlTabIndex;

        SessionLockOverlay.IsVisible = isLockedTab && !_mauiViewModel.IsSessionOperational;

    }



    private void UpdateMauiViewModelTabState(int tabIndex)

    {

        _mauiViewModel.SetSignalBoxTabActive(tabIndex == AppBottomTabBar.SignalBoxTabIndex);



        if (tabIndex == AppBottomTabBar.ControlTabIndex)

        {

            _mauiViewModel.PauseHeavyUpdates();

        }

        else

        {

            _mauiViewModel.ResumeHeavyUpdates();

        }

    }



    private CounterPage GetCounterPage() =>

        _counterPage ??= _serviceProvider.GetRequiredService<CounterPage>();



    private Microsoft.Maui.Controls.View EnsureTabContent(int tabIndex)

    {

        if (_tabContents[tabIndex] is { } existing)

        {

            return existing;

        }



        var page = tabIndex switch

        {

            AppBottomTabBar.SignalBoxTabIndex => (ContentPage)(_signalBoxPage ??= _serviceProvider.GetRequiredService<SignalBoxPage>()),

            AppBottomTabBar.ControlTabIndex => (ContentPage)(_controlPage ??= _serviceProvider.GetRequiredService<ControlPage>()),

            _ => (ContentPage)GetCounterPage()

        };



        var content = DetachPageContent(page);

        _tabContents[tabIndex] = content;

        return content;

    }



    private object? GetPageBindingContext(int tabIndex) =>

        tabIndex switch

        {

            AppBottomTabBar.CounterTabIndex => _counterPage?.BindingContext,

            AppBottomTabBar.SignalBoxTabIndex => _signalBoxPage?.BindingContext,

            AppBottomTabBar.ControlTabIndex => _controlPage?.BindingContext,

            _ => null

        };



    private static Microsoft.Maui.Controls.View DetachPageContent(ContentPage page)

    {

        var content = page.Content ?? throw new InvalidOperationException($"Page {page.GetType().Name} has no content.");

        page.Content = new Grid();



        if (page.BindingContext is not null)

        {

            content.BindingContext = page.BindingContext;

        }



        if (page.Resources.Count > 0)

        {

            content.Resources = page.Resources;

        }



        return content;

    }

}


