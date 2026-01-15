// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Interface;
using SharedUI.ViewModel;

public sealed partial class TrainControlPage
{
    private readonly ILocomotiveService _locomotiveService;
    private List<LocomotiveSeries> _allLocomotives = [];

    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage(TrainControlViewModel viewModel, ILocomotiveService locomotiveService)
    {
        ViewModel = viewModel;
        _locomotiveService = locomotiveService;
        InitializeComponent();
        Loaded += OnLoaded;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.SpeedKmh) or nameof(ViewModel.SelectedVmax) or nameof(ViewModel.HasValidLocoSeries))
        {
            UpdateVmaxDisplay();
        }
    }

    private void UpdateVmaxDisplay()
    {
        VmaxDisplay.Visibility = ViewModel.HasValidLocoSeries ? Visibility.Visible : Visibility.Collapsed;
        VmaxText.Text = ViewModel.SelectedVmax.ToString();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _allLocomotives = await _locomotiveService.GetAllSeriesAsync().ConfigureAwait(false);
    }

    private void LocoSeriesBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var query = sender.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            sender.ItemsSource = null;
            ViewModel.SelectedLocoSeries = string.Empty;
            ViewModel.SelectedVmax = 0;
            return;
        }

        var suggestions = _locomotiveService.FilterSeries(query)
            .Take(10)
            .Select(s => new LocomotiveSeriesDisplay(s.Name, s.Vmax))
            .ToList();

        sender.ItemsSource = suggestions;
    }

    private void LocoSeriesBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is LocomotiveSeriesDisplay series)
        {
            sender.Text = series.Name;
            ViewModel.SelectedLocoSeries = series.Name;
            ViewModel.SelectedVmax = series.Vmax;
        }
    }

    private void LocoSeriesBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is LocomotiveSeriesDisplay series)
        {
            ViewModel.SelectedLocoSeries = series.Name;
            ViewModel.SelectedVmax = series.Vmax;
        }
        else
        {
            // Try to find exact match
            var match = _locomotiveService.FindByName(args.QueryText ?? string.Empty);

            if (match != null)
            {
                ViewModel.SelectedLocoSeries = match.Name;
                ViewModel.SelectedVmax = match.Vmax;
            }
            else
            {
                ViewModel.SelectedLocoSeries = args.QueryText ?? string.Empty;
                ViewModel.SelectedVmax = 0;
            }
        }
    }
}

/// <summary>
/// Display wrapper for AutoSuggestBox items.
/// </summary>
internal record LocomotiveSeriesDisplay(string Name, int Vmax)
{
    public override string ToString() => $"{Name} ({Vmax} km/h)";
}