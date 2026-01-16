// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;

/// <summary>
/// TrainControlPage3 - ESU CabControl Style locomotive control page.
/// Features a round throttle control, locomotive tabs, and function button grid.
/// Layout inspired by ESU Mobile Control Pro app.
/// </summary>
public sealed partial class TrainControlPage3 : Page
{
    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage3(TrainControlViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Wire up function button events
        SetupFunctionButtons();
    }

    private void SetupFunctionButtons()
    {
        F0Button.FunctionClicked += OnFunctionClicked;
        F1Button.FunctionClicked += OnFunctionClicked;
        F2Button.FunctionClicked += OnFunctionClicked;
        F3Button.FunctionClicked += OnFunctionClicked;
        F4Button.FunctionClicked += OnFunctionClicked;
        F5Button.FunctionClicked += OnFunctionClicked;
        F6Button.FunctionClicked += OnFunctionClicked;
        F7Button.FunctionClicked += OnFunctionClicked;
    }

    private void OnFunctionClicked(object? sender, FunctionButtonClickedEventArgs e)
    {
        // Execute the function command via ViewModel
        switch (e.FunctionNumber)
        {
            case 0:
                ViewModel.ToggleF0Command.Execute(null);
                break;
            case 1:
                ViewModel.ToggleF1Command.Execute(null);
                break;
            case 2:
                ViewModel.ToggleF2Command.Execute(null);
                break;
            case 3:
                ViewModel.ToggleF3Command.Execute(null);
                break;
            case 4:
                ViewModel.ToggleF4Command.Execute(null);
                break;
            case 5:
                ViewModel.ToggleF5Command.Execute(null);
                break;
            case 6:
                ViewModel.ToggleF6Command.Execute(null);
                break;
            case 7:
                ViewModel.ToggleF7Command.Execute(null);
                break;
        }
    }
}
