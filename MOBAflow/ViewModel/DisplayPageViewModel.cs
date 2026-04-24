namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

using Moba.Display.Rendering;
using Moba.Display.Runtime;

using System.Runtime.InteropServices.WindowsRuntime;

public sealed partial class DisplayPageViewModel : ObservableObject, IDisposable
{
    private readonly FrameLoopScheduler _frameLoopScheduler;
    private readonly byte[] _previewBgraBuffer = new byte[FrameDimensions.Width * FrameDimensions.Height * 4];
    private readonly object _previewLock = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private bool _disposed;

    [ObservableProperty]
    private string _espIpAddress = "192.168.0.82";

    [ObservableProperty]
    private int _udpPort = 4210;

    [ObservableProperty]
    private int _refreshHz = 1;

    [ObservableProperty]
    private int _trackNumber = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private int _framesSent;

    [ObservableProperty]
    private DateTime _lastFrameTimestamp;

    [ObservableProperty]
    private WriteableBitmap _previewImage = new(FrameDimensions.Width, FrameDimensions.Height);

    [ObservableProperty]
    private string _activityStatusText = "Aktiv: Nein";

    [ObservableProperty]
    private string _framesSentStatusText = "Frames gesendet: 0";

    [ObservableProperty]
    private string _lastFrameTimeText = "Letztes Frame: -";

    public DisplayPageViewModel(FrameLoopScheduler frameLoopScheduler)
    {
        ArgumentNullException.ThrowIfNull(frameLoopScheduler);
        _frameLoopScheduler = frameLoopScheduler;
        _frameLoopScheduler.FrameReady += OnFrameReady;
        _frameLoopScheduler.TrackNumber = _trackNumber;
    }

    partial void OnTrackNumberChanged(int value)
    {
        _frameLoopScheduler.TrackNumber = Math.Clamp(value, 0, 99);
    }

    private bool CanStart() => !IsRunning;
    private bool CanStop() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartAsync()
    {
        var options = new FrameLoopOptions
        {
            IpAddress = EspIpAddress,
            Port = UdpPort,
            RefreshHz = RefreshHz
        };

        IsRunning = true;
        RefreshStatusLineTexts();
        return _frameLoopScheduler.StartAsync(options);
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        await _frameLoopScheduler.StopAsync();
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
        IsRunning = false;
        _frameLoopScheduler.FrameReady -= OnFrameReady;
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
        FramesSent++;
        UpdatePreview(frame);
        RefreshStatusLineTexts();
    }

    private void RefreshStatusLineTexts()
    {
        ActivityStatusText = IsRunning ? "Aktiv: Ja" : "Aktiv: Nein";
        FramesSentStatusText = $"Frames gesendet: {FramesSent}";
        LastFrameTimeText = FramesSent == 0
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
        IsRunning = false;
        _disposed = true;
    }
}
