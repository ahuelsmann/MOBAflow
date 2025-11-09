using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;

namespace MOBAsmart.Services;

/// <summary>
/// SignalR client service that connects to the FeedbackApi and receives real-time feedback updates.
/// </summary>
public class FeedbackService
{
    private HubConnection? _connection;
    private readonly string _serverUrl;

    /// <summary>
    /// Observable collection of feedback statistics for UI binding.
    /// </summary>
    public ObservableCollection<FeedbackStatistic> Statistics { get; } = new();

    /// <summary>
    /// Indicates whether the service is connected to the server.
    /// </summary>
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    public event EventHandler<bool>? ConnectionStateChanged;

    public FeedbackService(string serverUrl)
    {
        _serverUrl = serverUrl;
    }

    /// <summary>
    /// Connects to the SignalR hub and registers event handlers.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            // Dispose old connection if exists
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

            _connection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/feedbackHub")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Register reconnection handlers
            _connection.Reconnecting += error =>
            {
                System.Diagnostics.Debug.WriteLine($"Connection lost. Reconnecting... Error: {error?.Message}");
                ConnectionStateChanged?.Invoke(this, false);
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                System.Diagnostics.Debug.WriteLine($"Reconnected with connection ID: {connectionId}");
                ConnectionStateChanged?.Invoke(this, true);
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                System.Diagnostics.Debug.WriteLine($"Connection closed. Error: {error?.Message}");
                ConnectionStateChanged?.Invoke(this, false);
                return Task.CompletedTask;
            };

            // Handle initial statistics
            _connection.On<List<FeedbackStatistic>>("InitialStatistics", (stats) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"📥 InitialStatistics received: {stats.Count} items");
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Statistics.Clear();
                        foreach (var stat in stats.OrderBy(s => s.InPort))
                        {
                            Statistics.Add(stat);
                            System.Diagnostics.Debug.WriteLine($"   - InPort {stat.InPort}: Count={stat.TotalCount}");
                        }
                        System.Diagnostics.Debug.WriteLine($"✅ Statistics collection now has {Statistics.Count} items");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error handling InitialStatistics: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                }
            });

            // Handle real-time updates
            _connection.On<FeedbackStatistic>("FeedbackUpdate", (stat) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"📊 FeedbackUpdate received: InPort={stat.InPort}, Count={stat.TotalCount}, Time={stat.LastTrigger}");
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var existing = Statistics.FirstOrDefault(s => s.InPort == stat.InPort);
                        if (existing != null)
                        {
                            // Update existing
                            var index = Statistics.IndexOf(existing);
                            Statistics[index] = stat;
                            System.Diagnostics.Debug.WriteLine($"✅ Updated existing InPort {stat.InPort}");
                        }
                        else
                        {
                            // Add new (maintain sorted order)
                            var insertIndex = Statistics.TakeWhile(s => s.InPort < stat.InPort).Count();
                            Statistics.Insert(insertIndex, stat);
                            System.Diagnostics.Debug.WriteLine($"➕ Added new InPort {stat.InPort}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error handling FeedbackUpdate: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                }
            });

            // Handle reset
            _connection.On("StatisticsReset", () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Statistics reset received");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Statistics.Clear();
                        System.Diagnostics.Debug.WriteLine("✅ Statistics collection cleared");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error handling StatisticsReset: {ex.Message}");
                }
            });

            await _connection.StartAsync();
            ConnectionStateChanged?.Invoke(this, true);
            System.Diagnostics.Debug.WriteLine($"Connected to {_serverUrl}/feedbackHub");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
            ConnectionStateChanged?.Invoke(this, false);
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the SignalR hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                _connection = null;
                ConnectionStateChanged?.Invoke(this, false);
            }
        }
    }
}

/// <summary>
/// Feedback statistics data model for UI binding.
/// </summary>
public class FeedbackStatistic
{
    public uint InPort { get; set; }
    public long TotalCount { get; set; }
    public DateTime LastTrigger { get; set; }
    public string? EntityName { get; set; }
    public string? EntityType { get; set; }
}
