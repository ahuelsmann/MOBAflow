// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using Controls;

/// <summary>
/// Hosts bottom-tab content. Secondary tabs are created on first visit so ControlPage XAML is not parsed at startup.
/// </summary>
public partial class AppTabHostPage
{
    private readonly CounterPage _counterPage;
    private readonly IServiceProvider _serviceProvider;
    private SignalBoxPage? _signalBoxPage;
    private ControlPage? _controlPage;
    private readonly Microsoft.Maui.Controls.View?[] _tabContents = new Microsoft.Maui.Controls.View?[3];

    public AppTabHostPage(
        CounterPage counterPage,
        IServiceProvider serviceProvider)
    {
        _counterPage = counterPage;
        _serviceProvider = serviceProvider;
        _tabContents[AppBottomTabBar.CounterTabIndex] = DetachPageContent(_counterPage);

        InitializeComponent();
        ShowTab(AppBottomTabBar.CounterTabIndex);
    }

    private void OnTabSelected(object? sender, int tabIndex) => ShowTab(tabIndex);

    private void ShowTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= _tabContents.Length)
        {
            return;
        }

        BottomTabBar.SelectedTab = tabIndex;
        var content = EnsureTabContent(tabIndex);
        PageHost.BindingContext = GetPageBindingContext(tabIndex);
        PageHost.Content = content;

        switch (tabIndex)
        {
            case AppBottomTabBar.CounterTabIndex:
                _counterPage.ActivateTab();
                break;
            case AppBottomTabBar.SignalBoxTabIndex:
                _signalBoxPage?.ActivateTab();
                break;
            case AppBottomTabBar.ControlTabIndex:
                _controlPage?.ActivateTab();
                break;
        }
    }

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
            _ => _counterPage
        };

        var content = DetachPageContent(page);
        _tabContents[tabIndex] = content;
        return content;
    }

    private object? GetPageBindingContext(int tabIndex) =>
        tabIndex switch
        {
            AppBottomTabBar.CounterTabIndex => _counterPage.BindingContext,
            AppBottomTabBar.SignalBoxTabIndex => _signalBoxPage?.BindingContext,
            AppBottomTabBar.ControlTabIndex => _controlPage?.BindingContext,
            _ => null
        };

    private static Microsoft.Maui.Controls.View DetachPageContent(ContentPage page)
    {
        var content = page.Content ?? throw new InvalidOperationException($"Page {page.GetType().Name} has no content.");
        page.Content = new Grid();

        // Detached content leaves the page visual tree; preserve bindings and page-scoped resources.
        content.BindingContext = page.BindingContext;
        if (page.Resources.Count > 0)
        {
            content.Resources = page.Resources;
        }

        return content;
    }
}
