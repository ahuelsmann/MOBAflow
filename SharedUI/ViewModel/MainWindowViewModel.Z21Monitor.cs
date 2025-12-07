// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using System.Collections.ObjectModel;

/// <summary>
/// MainWindowViewModel - Z21 Traffic Monitor
/// Handles Z21 UDP traffic monitoring and display.
/// </summary>
public partial class MainWindowViewModel
{
    #region Z21 Traffic Monitor

    [ObservableProperty]
    private ObservableCollection<Z21TrafficPacket> trafficPackets = [];

    private void InitializeTrafficMonitor()
    {
        if (_z21?.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged += OnTrafficPacketLogged;

            // Load existing packets (if any)
            var existingPackets = _z21.TrafficMonitor.GetPackets();
            foreach (var packet in existingPackets)
            {
                TrafficPackets.Add(packet);
            }
        }
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        // Ensure UI updates happen on UI thread
        _uiDispatcher.InvokeOnUi(() =>
        {
            TrafficPackets.Insert(0, packet); // Add to top (newest first)

            // Keep only last 100 packets in UI
            while (TrafficPackets.Count > 100)
            {
                TrafficPackets.RemoveAt(TrafficPackets.Count - 1);
            }
        });
    }

    [RelayCommand]
    private void ClearTrafficMonitor()
    {
        TrafficPackets.Clear();
        _z21?.TrafficMonitor?.Clear();
    }

    #endregion
}
