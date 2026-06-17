// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

public partial class AppBottomTabBar
{
    public const int CounterTabIndex = 0;
    public const int SignalBoxTabIndex = 1;
    public const int ControlTabIndex = 2;

    public static readonly BindableProperty SelectedTabProperty = BindableProperty.Create(
        nameof(SelectedTab),
        typeof(int),
        typeof(AppBottomTabBar),
        CounterTabIndex,
        propertyChanged: OnSelectedTabChanged);

    public event EventHandler<int>? TabSelected;

    public int SelectedTab
    {
        get => (int)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public AppBottomTabBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void RefreshVisualState() => UpdateTabVisualState(SelectedTab);

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        UpdateTabVisualState(SelectedTab);

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        ApplyBottomSafeAreaPadding();
        UpdateTabVisualState(SelectedTab);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }
    }

    private static void OnSelectedTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppBottomTabBar tabBar && newValue is int selectedTab)
        {
            tabBar.UpdateTabVisualState(selectedTab);
        }
    }

    private void OnCounterTapped(object? sender, TappedEventArgs e) => SelectTab(CounterTabIndex);

    private void OnSignalBoxTapped(object? sender, TappedEventArgs e) => SelectTab(SignalBoxTabIndex);

    private void OnControlTapped(object? sender, TappedEventArgs e) => SelectTab(ControlTabIndex);

    private void SelectTab(int tabIndex)
    {
        if (SelectedTab == tabIndex)
        {
            return;
        }

        SelectedTab = tabIndex;
        TabSelected?.Invoke(this, tabIndex);
    }

    private void UpdateTabVisualState(int selectedTab)
    {
        UpdateTabItem(CounterIndicator, CounterLabel, selectedTab == CounterTabIndex);
        UpdateTabItem(SignalBoxIndicator, SignalBoxLabel, selectedTab == SignalBoxTabIndex);
        UpdateTabItem(ControlIndicator, ControlLabel, selectedTab == ControlTabIndex);
    }

    private static void UpdateTabItem(BoxView indicator, Label label, bool isSelected)
    {
        indicator.IsVisible = isSelected;

        if (Application.Current?.Resources.TryGetValue("TabBarSelectedForeground", out var selectedColor) == true
            && selectedColor is Color selected)
        {
            label.TextColor = isSelected
                ? selected
                : GetUnselectedColor();
        }
        else
        {
            label.TextColor = isSelected
                ? (Color)Application.Current!.Resources["Primary"]
                : (Color)Application.Current!.Resources["TextSecondary"];
        }

        label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
    }

    private static Color GetUnselectedColor()
    {
        if (Application.Current?.Resources.TryGetValue("TabBarUnselectedForeground", out var unselectedColor) == true
            && unselectedColor is Color unselected)
        {
            return unselected;
        }

        return (Color)Application.Current!.Resources["TextSecondary"];
    }

    private void ApplyBottomSafeAreaPadding()
    {
        var bottomInset = GetBottomSafeAreaInset();
        TabBarBorder.Padding = new Thickness(0, 0, 0, bottomInset);
    }

    private static double GetBottomSafeAreaInset()
    {
#if ANDROID
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is Android.Views.View platformView)
        {
            var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(platformView);
            if (insets is not null)
            {
                var systemBars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
                if (systemBars is null)
                {
                    return 0;
                }

                var density = platformView.Resources?.DisplayMetrics?.Density ?? 1.0f;
                return systemBars.Bottom / density;
            }
        }
#endif
        return 0;
    }
}
