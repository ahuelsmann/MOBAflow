namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Display.Rendering;
using Display.Runtime;
using Display.Transport;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices.WindowsRuntime;

public sealed partial class DisplayPageViewModel : ObservableObject, IDisposable
{
    private readonly FrameLoopScheduler _frameLoopScheduler;
    private readonly SerialLineFrameSender _serialLease;
    private readonly byte[] _previewBgraBuffer = new byte[FrameDimensions.Width * FrameDimensions.Height * 4];
    private readonly object _previewLock = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private bool _disposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(UdpInputsVisible))]
    [NotifyPropertyChangedFor(nameof(SerialInputsVisible))]
    private int _transportKindIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _espIpAddress = "192.168.0.82";

    [ObservableProperty]
    private int _udpPort = 4210;

    [ObservableProperty]
    private int _serialBaudRate = 921_600;

    [ObservableProperty]
    private int _refreshHz = 1;

    [ObservableProperty]
    private int _trackNumber = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string? _selectedSerialPort;

    public ObservableCollection<string> SerialPortNames { get; } = new();

    public bool UdpInputsVisible => TransportKindIndex == 0;

    public bool SerialInputsVisible => TransportKindIndex != 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

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

    public DisplayPageViewModel(FrameLoopScheduler frameLoopScheduler, SerialLineFrameSender serialLease)
    {
        ArgumentNullException.ThrowIfNull(frameLoopScheduler);
        _frameLoopScheduler = frameLoopScheduler;
        _serialLease = serialLease ?? throw new ArgumentNullException(nameof(serialLease));
        _frameLoopScheduler.FrameReady += OnFrameReady;
        _frameLoopScheduler.FrameTransmissionCompleted += OnFrameTransmissionCompleted;
        _frameLoopScheduler.TrackNumber = _trackNumber;
        ReloadSerialPortList();
    }

    public bool TransportReadyForStart => TransportKindIndex == 0
        ? !string.IsNullOrWhiteSpace(EspIpAddress)
        : !string.IsNullOrWhiteSpace(SelectedSerialPort);

    partial void OnTrackNumberChanged(int value)
    {
        _frameLoopScheduler.TrackNumber = Math.Clamp(value, 0, 99);
    }

    [RelayCommand]
    private void RefreshSerialPorts()
    {
        ReloadSerialPortList();
    }

    private void ReloadSerialPortList()
    {
        SerialPortNames.Clear();
        try
        {
            foreach (var name in SerialPort.GetPortNames().Order(StringComparer.OrdinalIgnoreCase))
            {
                SerialPortNames.Add(name);
            }

            if (SerialPortNames.Count > 0
                && string.IsNullOrWhiteSpace(SelectedSerialPort))
            {
                SelectedSerialPort = SerialPortNames[0];
            }

            foreach (var p in SerialPortNames)
            {
                if (string.Equals(p, SelectedSerialPort, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedSerialPort = p;
                    break;
                }
            }
        }
        catch (IOException)
        {
            // Ports may be inaccessible during driver updates.
        }
    }

    private bool CanStart() => !IsRunning && TransportReadyForStart;

    private bool CanStop() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartAsync()
    {
        var options = new FrameLoopOptions
        {
            Transport = TransportKindIndex == 0 ? DisplayTransportKind.Udp : DisplayTransportKind.Serial,
            IpAddress = EspIpAddress,
            Port = UdpPort,
            RefreshHz = RefreshHz,
            SerialPortName = SelectedSerialPort?.Trim() ?? string.Empty,
            SerialBaudRate = SerialBaudRate,
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
        _serialLease.ReleasePort();
        IsRunning = false;
        RefreshStatusLineTexts();
    }

    public async Task ShutdownAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _frameLoopScheduler.StopAsync();
        _serialLease.ReleasePort();
        IsRunning = false;
        _frameLoopScheduler.FrameReady -= OnFrameReady;
        _frameLoopScheduler.FrameTransmissionCompleted -= OnFrameTransmissionCompleted;
        _disposed = true;
        RefreshStatusLineTexts();
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
                    detail = $"{detail[..117]}…";
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
