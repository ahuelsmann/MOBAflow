// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using Backend.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Configuration;
using SharedUI.Interface;
using SharedUI.ViewModel;

/// <summary>
/// Mobile-optimized ViewModel for MAUI - focused on Z21 monitoring and feedback statistics.
/// </summary>
public partial class MauiViewModel : ObservableObject
{
    private readonly IZ21 _z21;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly AppSettings _settings;

    public MauiViewModel(IZ21 z21, IUiDispatcher uiDispatcher, AppSettings settings)
    {
        _z21 = z21;
        _uiDispatcher = uiDispatcher;
        _settings = settings;

        // Subscribe to Z21 events
        _z21.Received += OnFeedbackReceived;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;

        // Initialize with saved settings
        Z21IpAddress = _settings.Z21.CurrentIpAddress;
        CountOfFeedbackPoints = _settings.Counter.CountOfFeedbackPoints;
        
        InitializeStatistics();
    }

    #region Z21 Connection

    [ObservableProperty]
    private string z21IpAddress = "192.168.0.111";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isTrackPowerOn;

    [ObservableProperty]
    private int mainCurrent;

    [ObservableProperty]
    private int temperature;

    [ObservableProperty]
    private int supplyVoltage;

    [ObservableProperty]
    private int vccVoltage;

    partial void OnZ21IpAddressChanged(string value)
    {
        _settings.Z21.CurrentIpAddress = value;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(Z21IpAddress)) return;

        try
        {
            var address = System.Net.IPAddress.Parse(Z21IpAddress);
            int port = int.Parse(_settings.Z21.DefaultPort);
            await _z21.ConnectAsync(address, port);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _z21.DisconnectAsync();
    }

    [RelayCommand]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        if (turnOn)
            await _z21.SetTrackPowerOnAsync();
        else
            await _z21.SetTrackPowerOffAsync();
    }

    #endregion

    #region Feedback Statistics

    [ObservableProperty]
    private ObservableCollection<InPortStatistic> statistics = [];

    [ObservableProperty]
    private int countOfFeedbackPoints = 3;

    partial void OnCountOfFeedbackPointsChanged(int value)
    {
        _settings.Counter.CountOfFeedbackPoints = value;
        InitializeStatistics();
    }

    private void InitializeStatistics()
    {
        Statistics.Clear();
        for (int i = 1; i <= CountOfFeedbackPoints; i++)
        {
            Statistics.Add(new InPortStatistic
            {
                InPort = i,
                Name = $"Track {i}",
                Count = 0
            });
        }
    }

    [RelayCommand]
    private void ResetCounters()
    {
        foreach (var stat in Statistics)
        {
            stat.Count = 0;
            stat.LastFeedbackTime = null;
            stat.LastLapTime = TimeSpan.Zero;
        }
    }

    #endregion

    #region Z21 Event Handlers

    private void OnZ21ConnectedChanged(bool connected)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = connected;
        });
    }

    private void OnZ21SystemStateChanged(Backend.SystemState state)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsTrackPowerOn = state.IsTrackPowerOn;
            MainCurrent = state.MainCurrent;
            Temperature = state.Temperature;
            SupplyVoltage = state.SupplyVoltage;
            VccVoltage = state.VccVoltage;
        });
    }

    private void OnFeedbackReceived(Backend.FeedbackResult feedback)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            var stat = Statistics.FirstOrDefault(s => s.InPort == feedback.InPort);
            if (stat != null)
            {
                stat.Count++;
                stat.LastFeedbackTime = DateTime.Now;
                
                // Calculate lap time if we have a previous feedback
                if (stat.LastLapTime != TimeSpan.Zero)
                {
                    var lapTime = DateTime.Now - (stat.LastFeedbackTime.Value - stat.LastLapTime);
                    stat.LastLapTime = lapTime;
                }
            }
        });
    }

    #endregion
}
