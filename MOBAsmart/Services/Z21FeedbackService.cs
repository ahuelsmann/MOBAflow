using Moba.Backend;
using System.Collections.ObjectModel;
using System.Net;

namespace MOBAsmart.Services;

/// <summary>
/// Direct Z21 connection service for MOBAsmart Android app.
/// Provides real-time feedback statistics via UDP communication with Z21 digital station.
/// Implements Z21 LAN Protocol V1.13 for R-BUS feedback events.
/// </summary>
public sealed class Z21FeedbackService : IDisposable
{
    private Z21? _z21;
    private FeedbackStatisticsManager? _statisticsManager;
    private bool _disposed;

    /// <summary>
    /// Observable collection of feedback statistics for UI binding.
    /// Thread-safe updates via MainThread.BeginInvokeOnMainThread.
    /// </summary>
    public ObservableCollection<FeedbackStatistic> Statistics { get; } = [];

    /// <summary>
    /// Indicates whether the service is connected to Z21.
    /// </summary>
    public bool IsConnected => _z21?.IsConnected ?? false;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    public event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Connects to the Z21 digital station via UDP.
    /// </summary>
    /// <param name="ipAddress">IP address of the Z21 (e.g., "192.168.0.111")</param>
    /// <exception cref="FormatException">If IP address format is invalid</exception>
    /// <exception cref="SocketException">If connection fails</exception>
    public async Task ConnectAsync(string ipAddress)
    {
        if (IsConnected)
        {
            System.Diagnostics.Debug.WriteLine("⚠ Already connected to Z21");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"🔌 Connecting to Z21 at {ipAddress}...");

            _z21 = new Z21();
            _statisticsManager = new FeedbackStatisticsManager();
            _z21.Received += OnZ21FeedbackReceived;

            var address = IPAddress.Parse(ipAddress);
            await _z21.ConnectAsync(address);

            ConnectionStateChanged?.Invoke(this, true);
            System.Diagnostics.Debug.WriteLine($"✅ Connected to Z21 at {ipAddress}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Z21 connection failed: {ex.Message}");
            
            if (_z21 != null)
            {
                _z21.Received -= OnZ21FeedbackReceived;
                _z21.Dispose();
                _z21 = null;
            }

            ConnectionStateChanged?.Invoke(this, false);
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the Z21 digital station.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_z21 == null)
        {
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("🔌 Disconnecting from Z21...");

            _z21.Received -= OnZ21FeedbackReceived;
            await _z21.DisconnectAsync();
            _z21.Dispose();
            _z21 = null;

            ConnectionStateChanged?.Invoke(this, false);
            System.Diagnostics.Debug.WriteLine("✅ Disconnected from Z21");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠ Error during disconnect: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles feedback messages from Z21.
    /// Updates statistics and UI on the main thread.
    /// </summary>
    private void OnZ21FeedbackReceived(FeedbackResult feedbackContent)
    {
        try
        {
            if (_statisticsManager == null)
            {
                return;
            }

            uint inPort = (uint)feedbackContent.InPort;
            System.Diagnostics.Debug.WriteLine($"📥 Feedback received for InPort {inPort}");

            var statistic = _statisticsManager.RecordFeedback(inPort);

            MainThread.BeginInvokeOnMainThread(() => UpdateStatisticsUI(inPort, statistic));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error handling feedback: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the statistics UI (must be called on main thread).
    /// </summary>
    private void UpdateStatisticsUI(uint inPort, FeedbackStatistic statistic)
    {
        var existing = Statistics.FirstOrDefault(s => s.InPort == inPort);
        if (existing != null)
        {
            var index = Statistics.IndexOf(existing);
            Statistics[index] = statistic;
            System.Diagnostics.Debug.WriteLine($"✅ Updated InPort {inPort}: Count={statistic.TotalCount}");
        }
        else
        {
            var insertIndex = Statistics.TakeWhile(s => s.InPort < inPort).Count();
            Statistics.Insert(insertIndex, statistic);
            System.Diagnostics.Debug.WriteLine($"➕ Added InPort {inPort}: Count={statistic.TotalCount}");
        }
    }

    /// <summary>
    /// Resets all statistics counters.
    /// </summary>
    public void ResetStatistics()
    {
        _statisticsManager?.Reset();
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Statistics.Clear();
            System.Diagnostics.Debug.WriteLine("🔄 Statistics reset");
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_z21 != null)
        {
            _z21.Received -= OnZ21FeedbackReceived;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await _z21.DisconnectAsync();
                    _z21.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠ Error during Z21 cleanup: {ex.Message}");
                }
            });

            _z21 = null;
        }

        _disposed = true;
    }
}
