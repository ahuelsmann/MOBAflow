// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel für DockingManager mit Layout-State Management.
/// Verwaltet Visibility, Größen und State aller Dock-Panels.
/// </summary>
public sealed partial class DockingPanelViewModel : ObservableObject
{
    #region Observable Properties

    [ObservableProperty]
    private bool isLeftPanelVisible = true;

    [ObservableProperty]
    private bool isRightPanelVisible = true;

    [ObservableProperty]
    private bool isTopPanelVisible = false;

    [ObservableProperty]
    private bool isBottomPanelVisible = true;

    [ObservableProperty]
    private double leftPanelWidth = 240;

    [ObservableProperty]
    private double rightPanelWidth = 240;

    [ObservableProperty]
    private double topPanelHeight = 100;

    [ObservableProperty]
    private double bottomPanelHeight = 100;

    [ObservableProperty]
    private bool isLeftPanelPinned = true;

    [ObservableProperty]
    private bool isRightPanelPinned = true;

    [ObservableProperty]
    private bool isTopPanelPinned = false;

    [ObservableProperty]
    private bool isBottomPanelPinned = true;

    [ObservableProperty]
    private bool isLeftPanelMaximized = false;

    [ObservableProperty]
    private bool isRightPanelMaximized = false;

    [ObservableProperty]
    private bool isTopPanelMaximized = false;

    [ObservableProperty]
    private bool isBottomPanelMaximized = false;

    #endregion

    #region Fields

    private Dictionary<string, double> _widthBackups = new();
    private Dictionary<string, double> _heightBackups = new();
    private Dictionary<string, Visibility> _visibilityState = new();

    #endregion

    public DockingPanelViewModel()
    {
        InitializeDefaults();
    }

    #region Commands

    [RelayCommand]
    public void ToggleLeftPanel()
    {
        IsLeftPanelVisible = !IsLeftPanelVisible;
        SaveState();
    }

    [RelayCommand]
    public void ToggleRightPanel()
    {
        IsRightPanelVisible = !IsRightPanelVisible;
        SaveState();
    }

    [RelayCommand]
    public void ToggleTopPanel()
    {
        IsTopPanelVisible = !IsTopPanelVisible;
        SaveState();
    }

    [RelayCommand]
    public void ToggleBottomPanel()
    {
        IsBottomPanelVisible = !IsBottomPanelVisible;
        SaveState();
    }

    [RelayCommand]
    public void PinLeftPanel()
    {
        IsLeftPanelPinned = !IsLeftPanelPinned;
        SaveState();
    }

    [RelayCommand]
    public void PinRightPanel()
    {
        IsRightPanelPinned = !IsRightPanelPinned;
        SaveState();
    }

    [RelayCommand]
    public void PinTopPanel()
    {
        IsTopPanelPinned = !IsTopPanelPinned;
        SaveState();
    }

    [RelayCommand]
    public void PinBottomPanel()
    {
        IsBottomPanelPinned = !IsBottomPanelPinned;
        SaveState();
    }

    [RelayCommand]
    public void MaximizeLeftPanel()
    {
        if (!IsLeftPanelMaximized)
        {
            BackupPanelSizes();
            IsLeftPanelMaximized = true;
            IsRightPanelVisible = false;
            IsTopPanelVisible = false;
            IsBottomPanelVisible = false;
        }
        else
        {
            RestorePanelSizes();
            IsLeftPanelMaximized = false;
        }
        SaveState();
    }

    [RelayCommand]
    public void MaximizeRightPanel()
    {
        if (!IsRightPanelMaximized)
        {
            BackupPanelSizes();
            IsRightPanelMaximized = true;
            IsLeftPanelVisible = false;
            IsTopPanelVisible = false;
            IsBottomPanelVisible = false;
        }
        else
        {
            RestorePanelSizes();
            IsRightPanelMaximized = false;
        }
        SaveState();
    }

    [RelayCommand]
    public void MaximizeTopPanel()
    {
        if (!IsTopPanelMaximized)
        {
            BackupPanelSizes();
            IsTopPanelMaximized = true;
            IsLeftPanelVisible = false;
            IsRightPanelVisible = false;
            IsBottomPanelVisible = false;
        }
        else
        {
            RestorePanelSizes();
            IsTopPanelMaximized = false;
        }
        SaveState();
    }

    [RelayCommand]
    public void MaximizeBottomPanel()
    {
        if (!IsBottomPanelMaximized)
        {
            BackupPanelSizes();
            IsBottomPanelMaximized = true;
            IsLeftPanelVisible = false;
            IsRightPanelVisible = false;
            IsTopPanelVisible = false;
        }
        else
        {
            RestorePanelSizes();
            IsBottomPanelMaximized = false;
        }
        SaveState();
    }

    [RelayCommand]
    public void ResetLayout()
    {
        InitializeDefaults();
        SaveState();
    }

    #endregion

    #region Helper Methods

    private void InitializeDefaults()
    {
        IsLeftPanelVisible = true;
        IsRightPanelVisible = true;
        IsTopPanelVisible = false;
        IsBottomPanelVisible = true;

        LeftPanelWidth = 240;
        RightPanelWidth = 240;
        TopPanelHeight = 100;
        BottomPanelHeight = 100;

        IsLeftPanelPinned = true;
        IsRightPanelPinned = true;
        IsTopPanelPinned = false;
        IsBottomPanelPinned = true;

        IsLeftPanelMaximized = false;
        IsRightPanelMaximized = false;
        IsTopPanelMaximized = false;
        IsBottomPanelMaximized = false;
    }

    private void BackupPanelSizes()
    {
        _widthBackups["left"] = LeftPanelWidth;
        _widthBackups["right"] = RightPanelWidth;
        _heightBackups["top"] = TopPanelHeight;
        _heightBackups["bottom"] = BottomPanelHeight;

        _visibilityState["left"] = IsLeftPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        _visibilityState["right"] = IsRightPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        _visibilityState["top"] = IsTopPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        _visibilityState["bottom"] = IsBottomPanelVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RestorePanelSizes()
    {
        if (_widthBackups.TryGetValue("left", out var leftWidth))
            LeftPanelWidth = leftWidth;
        if (_widthBackups.TryGetValue("right", out var rightWidth))
            RightPanelWidth = rightWidth;
        if (_heightBackups.TryGetValue("top", out var topHeight))
            TopPanelHeight = topHeight;
        if (_heightBackups.TryGetValue("bottom", out var bottomHeight))
            BottomPanelHeight = bottomHeight;

        if (_visibilityState.TryGetValue("left", out var leftVis))
            IsLeftPanelVisible = leftVis == Visibility.Visible;
        if (_visibilityState.TryGetValue("right", out var rightVis))
            IsRightPanelVisible = rightVis == Visibility.Visible;
        if (_visibilityState.TryGetValue("top", out var topVis))
            IsTopPanelVisible = topVis == Visibility.Visible;
        if (_visibilityState.TryGetValue("bottom", out var bottomVis))
            IsBottomPanelVisible = bottomVis == Visibility.Visible;
    }

    /// <summary>
    /// Speichert den aktuellen Layout-State.
    /// </summary>
    public void SaveState()
    {
        // Wird von DockingLayoutService aufgerufen
    }

    #endregion
}
