namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Display.Rendering;
using Display.Runtime;

using Common.Configuration;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using SharedUI.Interface;

public sealed partial class DisplayPageViewModel : ObservableObject, IDisposable
{
    private readonly FrameLoopScheduler _frameLoopScheduler;
    private readonly AppSettings _appSettings;
    private readonly ISettingsService _settingsService;
    private readonly byte[] _previewBgraBuffer = new byte[FrameDimensions.Width * FrameDimensions.Height * 4];
    private readonly object _previewLock = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private bool _disposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveWifiConfigCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadWifiStatusCommand))]
    private string _espIpAddress = "192.168.0.82";

    [ObservableProperty]
    private int _udpPort = 4210;

    [ObservableProperty]
    private int _refreshHz = 10;

    [ObservableProperty]
    private int _trackNumber = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveWifiConfigCommand))]
    private string _wifiSsid = string.Empty;

    [ObservableProperty]
    private string _wifiPassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveWifiConfigCommand))]
    private bool _isSavingWifiConfig;

    [ObservableProperty]
    private string _wifiConfigStatusText = "WLAN-Konfiguration: —";

    [ObservableProperty]
    private string _wifiDeviceStatusText = "ESP-WLAN-Status: —";

    private int _previewFrameCount;
    private int _successfulDeviceTransfers;

    [ObservableProperty]
    private DateTime _lastFrameTimestamp;

    [ObservableProperty]
    private WriteableBitmap _previewImage = new(FrameDimensions.Width, FrameDimensions.Height);

    [ObservableProperty]
    private string _activityStatusText = "Aktiv: Nein";

    [ObservableProperty]
    private string _framesSentStatusText = "Vorschau (lokal): 0 | Übertragungen zum Gerät OK: 0";

    [ObservableProperty]
    private string _lastTransportOutcomeText = "Letzter Transport: —";

    [ObservableProperty]
    private string _lastFrameTimeText = "Letztes Frame: -";

    public DisplayPageViewModel(FrameLoopScheduler frameLoopScheduler, AppSettings appSettings, ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(frameLoopScheduler);
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(settingsService);
        _frameLoopScheduler = frameLoopScheduler;
        _appSettings = appSettings;
        _settingsService = settingsService;
        if (!string.IsNullOrWhiteSpace(_appSettings.Display.Esp32IpAddress))
        {
            _espIpAddress = _appSettings.Display.Esp32IpAddress;
        }
        _frameLoopScheduler.FrameReady += OnFrameReady;
        _frameLoopScheduler.FrameTransmissionCompleted += OnFrameTransmissionCompleted;
        _frameLoopScheduler.TrackNumber = _trackNumber;
    }

    partial void OnEspIpAddressChanged(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (!string.Equals(_appSettings.Display.Esp32IpAddress, trimmed, StringComparison.Ordinal))
        {
            _appSettings.Display.Esp32IpAddress = trimmed;
            _ = _settingsService.SaveSettingsAsync(_appSettings);
        }
    }

    public bool TransportReadyForStart => !string.IsNullOrWhiteSpace(EspIpAddress);

    partial void OnTrackNumberChanged(int value)
    {
        _frameLoopScheduler.TrackNumber = Math.Clamp(value, 0, 99);
    }

    private bool CanStart() => !IsRunning && TransportReadyForStart;

    private bool CanStop() => IsRunning;

    private bool CanSaveWifiConfig()
        => !IsSavingWifiConfig
           && !string.IsNullOrWhiteSpace(EspIpAddress)
           && !string.IsNullOrWhiteSpace(WifiSsid);

    private bool CanReadWifiStatus()
        => !IsSavingWifiConfig
           && !string.IsNullOrWhiteSpace(EspIpAddress);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartAsync()
    {
        var options = new FrameLoopOptions
        {
            IpAddress = EspIpAddress,
            Port = UdpPort,
            RefreshHz = RefreshHz,
        };

        _previewFrameCount = 0;
        _successfulDeviceTransfers = 0;
        LastTransportOutcomeText = "Letzter Transport: —";
        IsRunning = true;
        RefreshStatusLineTexts();
        return _frameLoopScheduler.StartAsync(options);
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        await _frameLoopScheduler.StopAsync();
        await RunOnUiThreadAsync(() =>
        {
            IsRunning = false;
            RefreshStatusLineTexts();
        });
    }

    [RelayCommand(CanExecute = nameof(CanSaveWifiConfig))]
    private async Task SaveWifiConfigAsync()
    {
        var host = EspIpAddress.Trim();
        var ssid = WifiSsid.Trim();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(ssid))
        {
            WifiConfigStatusText = "WLAN-Konfiguration: Bitte IP und SSID ausfüllen.";
            return;
        }

        IsSavingWifiConfig = true;
        WifiConfigStatusText = "WLAN-Konfiguration: Sende Daten zum ESP...";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("ssid", ssid),
                new KeyValuePair<string, string>("password", WifiPassword ?? string.Empty),
            ]);

            var response = await client.PostAsync($"http://{host}/api/wifi/config", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                WifiConfigStatusText = "WLAN-Konfiguration: Gespeichert. ESP startet neu (2-5 Sekunden).";
            }
            else
            {
                var compact = CompactResponse(responseText);
                WifiConfigStatusText = $"WLAN-Konfiguration: Fehler {((int)response.StatusCode)} - {compact}";
            }
        }
        catch (Exception ex)
        {
            WifiConfigStatusText = $"WLAN-Konfiguration: Senden fehlgeschlagen - {ex.Message}";
        }
        finally
        {
            IsSavingWifiConfig = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReadWifiStatus))]
    private async Task ReadWifiStatusAsync()
    {
        var host = EspIpAddress.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            WifiDeviceStatusText = "ESP-WLAN-Status: Bitte zuerst ESP-IP eintragen.";
            return;
        }

        WifiDeviceStatusText = "ESP-WLAN-Status: Lade...";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            var json = await client.GetStringAsync($"http://{host}/api/wifi/status");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var mode = root.TryGetProperty("mode", out var modeEl) ? modeEl.GetString() : "?";
            var ssid = root.TryGetProperty("ssid", out var ssidEl) ? ssidEl.GetString() : "?";
            var ip = root.TryGetProperty("ip", out var ipEl) ? ipEl.GetString() : "?";
            var stationIp = root.TryGetProperty("stationIp", out var stationIpEl) ? stationIpEl.GetString() : "?";
            var configApSsid = root.TryGetProperty("configApSsid", out var configApSsidEl) ? configApSsidEl.GetString() : "?";
            var configApIp = root.TryGetProperty("configApIp", out var configApIpEl) ? configApIpEl.GetString() : "?";
            var lastWifiStatus = root.TryGetProperty("lastWifiStatus", out var lastWifiStatusEl) ? lastWifiStatusEl.GetString() : "?";
            var udpPort = root.TryGetProperty("udpPort", out var udpEl) ? udpEl.GetRawText() : "?";
            var configPort = root.TryGetProperty("configPort", out var cfgEl) ? cfgEl.GetRawText() : "?";
            WifiDeviceStatusText =
                $"ESP-WLAN-Status: Mode={mode}, SSID={ssid}, IP={ip}, STA-IP={stationIp}, Config-AP={configApSsid} ({configApIp}), LastWiFi={lastWifiStatus}, UDP={udpPort}, ConfigAPI={configPort}";

            if (string.Equals(configApIp, EspIpAddress.Trim(), StringComparison.Ordinal))
            {
                WifiDeviceStatusText += " | Streaming-Ziel: AP-IP (192.168.4.1).";
            }
            else if (string.Equals(stationIp, EspIpAddress.Trim(), StringComparison.Ordinal))
            {
                WifiDeviceStatusText += " | Streaming-Ziel: WLAN-IP.";
            }
            else
            {
                WifiDeviceStatusText += " | Hinweis: Nutze 192.168.4.1 wenn dein PC im Setup-AP ist, sonst STA-IP.";
            }
        }
        catch (Exception ex)
        {
            WifiDeviceStatusText = $"ESP-WLAN-Status: Fehler beim Lesen - {ex.Message}";
        }
    }

    public async Task ShutdownAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _frameLoopScheduler.StopAsync();

        // Page is unloading: avoid firing PropertyChanged into torn-down XAML objects.
        _frameLoopScheduler.FrameReady -= OnFrameReady;
        _frameLoopScheduler.FrameTransmissionCompleted -= OnFrameTransmissionCompleted;
        _isRunning = false;
        _disposed = true;
    }

    private static string CompactResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return "keine Antwort";
        }

        var compact = responseText.Replace("\r", " ").Replace("\n", " ").Trim();
        if (compact.Length > 96)
        {
            compact = $"{compact[..93]}...";
        }

        return compact;
    }

    private void OnFrameReady(object? sender, FrameReadyEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _ = sender;
        var timestamp = e.Timestamp;
        var frameCopy = e.Frame.ToArray();

        if (_dispatcherQueue is null)
        {
            ApplyFrameOnUiThread(timestamp, frameCopy);
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_disposed)
            {
                return;
            }

            ApplyFrameOnUiThread(timestamp, frameCopy);
        });
    }

    private void ApplyFrameOnUiThread(DateTime timestamp, byte[] frame)
    {
        if (_disposed)
        {
            return;
        }

        LastFrameTimestamp = timestamp;
        _previewFrameCount++;
        UpdatePreview(frame);
        RefreshStatusLineTexts();
    }

    private void OnFrameTransmissionCompleted(object? sender, FrameTransmissionCompletedEventArgs e)
    {
        _ = sender;
        if (_disposed)
        {
            return;
        }

        void Apply()
        {
            if (_disposed)
            {
                return;
            }

            if (e.Success)
            {
                _successfulDeviceTransfers++;
                LastTransportOutcomeText = "Letzter Transport: OK";
            }
            else
            {
                var detail = string.IsNullOrWhiteSpace(e.FailureMessage)
                    ? "(keine Meldung)"
                    : e.FailureMessage!;
                if (detail.Length > 120)
                {
                    detail = $"{detail[..117]}...";
                }

                LastTransportOutcomeText = $"Letzter Transport: Fehler — {detail}";
            }

            RefreshStatusLineTexts();
        }

        if (_dispatcherQueue is null)
        {
            Apply();
            return;
        }

        _dispatcherQueue.TryEnqueue(Apply);
    }

    private void RefreshStatusLineTexts()
    {
        ActivityStatusText = IsRunning ? "Aktiv: Ja" : "Aktiv: Nein";
        FramesSentStatusText =
            $"Vorschau (lokal): {_previewFrameCount} | Übertragungen zum Gerät OK: {_successfulDeviceTransfers}";
        LastFrameTimeText = _previewFrameCount == 0
            ? "Letztes Frame: -"
            : $"Letztes Frame: {LastFrameTimestamp:HH:mm:ss}";
    }

    private void UpdatePreview(byte[] rgb565Frame)
    {
        if (_disposed)
        {
            return;
        }

        lock (_previewLock)
        {
            if (_disposed)
            {
                return;
            }

            Rgb565Converter.DecodeToBgra8888(rgb565Frame, _previewBgraBuffer);
            using var stream = PreviewImage.PixelBuffer.AsStream();
            stream.Position = 0;
            stream.Write(_previewBgraBuffer, 0, _previewBgraBuffer.Length);
            PreviewImage.Invalidate();
        }
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        if (!_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            tcs.SetException(new InvalidOperationException("Failed to enqueue work on UI dispatcher."));
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _frameLoopScheduler.FrameReady -= OnFrameReady;
        _frameLoopScheduler.FrameTransmissionCompleted -= OnFrameTransmissionCompleted;
        IsRunning = false;
        _disposed = true;
    }
}
