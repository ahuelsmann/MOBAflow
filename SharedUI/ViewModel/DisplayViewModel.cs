// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Common.Configuration;
using Moba.Display.Protocol;
using Moba.Display.Transport;

public enum DisplayConnectionState
{
    NotConfigured,
    InvalidConfiguration,
    Disconnected,
    Connecting,
    Connected,
    Offline
}

public enum DisplayNegotiationState
{
    NotStarted,
    Negotiating,
    Succeeded,
    Failed,
    Stale
}

public enum DisplayCapabilityFreshness
{
    Unavailable,
    Live,
    Stale
}

/// <summary>
/// Projects one configured ESP32 display endpoint and its live negotiated state.
/// </summary>
public sealed partial class DisplayViewModel : ObservableObject
{
    private const string NotNegotiatedText = "Not negotiated";
    private const DisplayFrameCapabilityFlags RequiredFrameCapabilities =
        DisplayFrameCapabilityFlags.FullFrameStaging
        | DisplayFrameCapabilityFlags.RegionTransfer
        | DisplayFrameCapabilityFlags.AtomicPresentation;
    private readonly AppSettings _settings;
    private readonly IDisplayDeviceClient _deviceClient;
    private DisplayEndpoint? _configuredEndpoint;
    private DisplayEndpoint? _connectedEndpoint;
    private CapabilitiesResponsePayload? _capabilities;

    public DisplayViewModel(AppSettings settings, IDisplayDeviceClient deviceClient)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(deviceClient);
        _settings = settings;
        _deviceClient = deviceClient;
        SynchronizeConfiguration();
    }

    [ObservableProperty]
    public partial bool IsEndpointValid { get; private set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string ConfiguredEndpointText { get; private set; } = "Not configured";

    [ObservableProperty]
    public partial string EndpointStatusText { get; private set; } = "Configure a display IP address in Settings.";

    [ObservableProperty]
    public partial DisplayConnectionState ConnectionState { get; private set; } = DisplayConnectionState.NotConfigured;

    [ObservableProperty]
    public partial DisplayNegotiationState NegotiationState { get; private set; } = DisplayNegotiationState.NotStarted;

    [ObservableProperty]
    public partial DisplayCapabilityFreshness CapabilityFreshness { get; private set; } = DisplayCapabilityFreshness.Unavailable;

    [ObservableProperty]
    public partial string HealthText { get; private set; } = "Not queried";

    [ObservableProperty]
    public partial string LastResultText { get; private set; } = "No display operation has run.";

    [ObservableProperty]
    public partial double BrightnessPercentage { get; set; } = 100;

    public bool CanConnect => IsEndpointValid && !IsBusy;

    public bool CanRefreshHealth => HasLiveCapabilities && !IsBusy;

    public bool CanSendStandardTestPattern =>
        HasLiveCapabilities && IsStandardPatternCompatible && !IsBusy;

    public bool CanRenderBuiltInTestPattern =>
        HasLiveCapabilities
        && _capabilities?.OptionalCommands.HasFlag(DisplayOptionalCommandFlags.RenderTestPattern) == true
        && !IsBusy;

    public bool CanSetBrightness =>
        HasLiveCapabilities
        && _capabilities?.OptionalCommands.HasFlag(DisplayOptionalCommandFlags.SetBrightness) == true
        && !IsBusy;

    public bool HasLiveCapabilities =>
        ConnectionState == DisplayConnectionState.Connected
        && NegotiationState == DisplayNegotiationState.Succeeded
        && CapabilityFreshness == DisplayCapabilityFreshness.Live
        && _capabilities is not null
        && Equals(_configuredEndpoint, _connectedEndpoint);

    public string ConnectionStatusText => ConnectionState switch
    {
        DisplayConnectionState.NotConfigured => "Not configured",
        DisplayConnectionState.InvalidConfiguration => "Invalid endpoint",
        DisplayConnectionState.Disconnected => "Disconnected",
        DisplayConnectionState.Connecting => "Connecting",
        DisplayConnectionState.Connected => "Connected",
        _ => "Offline"
    };

    public string NegotiationStatusText => NegotiationState switch
    {
        DisplayNegotiationState.NotStarted => "Not started",
        DisplayNegotiationState.Negotiating => "Negotiating protocol and capabilities",
        DisplayNegotiationState.Succeeded => "Protocol and capabilities negotiated",
        DisplayNegotiationState.Stale => "Previous capabilities are stale; reconnect required",
        _ => "Negotiation failed"
    };

    public string CapabilityStatusText => CapabilityFreshness switch
    {
        DisplayCapabilityFreshness.Live => "Capabilities are live for the configured endpoint.",
        DisplayCapabilityFreshness.Stale => "Previous capabilities are stale and cannot authorize commands.",
        _ => "Capabilities have not been negotiated."
    };

    public string ResolutionText => _capabilities is null
        ? NotNegotiatedText
        : $"{_capabilities.Width} x {_capabilities.Height}";

    public string ProtocolVersionText => _capabilities?.SelectedVersion.ToString() ?? NotNegotiatedText;

    public string FirmwareVersionText => _capabilities?.FirmwareVersion ?? NotNegotiatedText;

    public string DeviceIdentityText => _capabilities?.DeviceIdentity ?? NotNegotiatedText;

    public string AdapterIdentityText => _capabilities?.AdapterIdentity ?? NotNegotiatedText;

    public string PixelFormatsText => _capabilities?.PixelFormats.ToString() ?? NotNegotiatedText;

    public string RotationsText => _capabilities?.Rotations.ToString() ?? NotNegotiatedText;

    public string RegionLimitText => _capabilities is null
        ? NotNegotiatedText
        : $"{_capabilities.MaximumRegionPayloadLength} bytes";

    public string StandardPatternAvailabilityText => _capabilities switch
    {
        null => "Negotiate capabilities before sending the standard host pattern.",
        { PixelFormats: var formats }
            when !formats.HasFlag(DisplayPixelFormatFlags.Rgb565BigEndian) =>
            "The device does not support the RGB565 big-endian format required by the standard pattern.",
        { Rotations: var rotations }
            when !rotations.HasFlag(DisplayRotationFlags.Degrees0) =>
            "The device does not support the zero-degree rotation required by the standard pattern.",
        { FrameCapabilities: var frameCapabilities }
            when (frameCapabilities & RequiredFrameCapabilities) != RequiredFrameCapabilities =>
            "The device does not support the complete atomic frame transfer required by the standard pattern.",
        _ when !HasLiveCapabilities =>
            "Reconnect and negotiate live capabilities before sending the standard pattern.",
        _ => "The standard host-rendered pattern is supported by the live device session."
    };

    public string BuiltInPatternAvailabilityText => _capabilities switch
    {
        null => "Negotiate capabilities to check built-in pattern support.",
        { OptionalCommands: var commands }
            when commands.HasFlag(DisplayOptionalCommandFlags.RenderTestPattern) =>
            "The device supports its built-in conformance pattern.",
        _ => "The device does not support a built-in test pattern. Use the standard host pattern instead."
    };

    public string BrightnessAvailabilityText => _capabilities switch
    {
        null => "Negotiate capabilities to check brightness support.",
        { OptionalCommands: var commands }
            when commands.HasFlag(DisplayOptionalCommandFlags.SetBrightness) =>
            "The device supports brightness control.",
        _ => "The device does not support brightness control."
    };

    /// <summary>
    /// Re-reads endpoint settings and invalidates live capabilities when the endpoint changed.
    /// </summary>
    public void SynchronizeConfiguration()
    {
        var (valid, endpoint, validationError) = ReadConfiguredEndpoint();
        ApplyEndpointProjection(valid, endpoint, validationError);

        if (_connectedEndpoint is not null && !Equals(_connectedEndpoint, endpoint))
        {
            InvalidateChangedEndpoint(valid, validationError);
        }
        else if (_connectedEndpoint is null)
        {
            ProjectDisconnectedEndpoint(valid, validationError);
        }

        RaiseDerivedProperties();
    }

    private (bool IsValid, DisplayEndpoint? Endpoint, DisplayEndpointValidationError Error)
        ReadConfiguredEndpoint()
    {
        var valid = DisplayEndpoint.TryCreate(
            _settings.Display.Esp32IpAddress,
            _settings.Display.Port,
            out var endpoint,
            out var validationError);
        return (valid, endpoint, validationError);
    }

    private void ApplyEndpointProjection(
        bool valid,
        DisplayEndpoint? endpoint,
        DisplayEndpointValidationError validationError)
    {
        _configuredEndpoint = endpoint;
        IsEndpointValid = valid;
        ConfiguredEndpointText = endpoint?.ToString() ?? "Not configured";
        EndpointStatusText = GetEndpointStatusText(validationError);
    }

    private void InvalidateChangedEndpoint(
        bool valid,
        DisplayEndpointValidationError validationError)
    {
        _deviceClient.Disconnect();
        _connectedEndpoint = null;
        MarkCapabilitiesStale();
        ConnectionState = valid
            ? DisplayConnectionState.Disconnected
            : GetUnconfiguredConnectionState(validationError);
    }

    private void ProjectDisconnectedEndpoint(
        bool valid,
        DisplayEndpointValidationError validationError)
    {
        ConnectionState = valid
            ? DisplayConnectionState.Disconnected
            : GetUnconfiguredConnectionState(validationError);
        if (_capabilities is null)
        {
            NegotiationState = DisplayNegotiationState.NotStarted;
            CapabilityFreshness = DisplayCapabilityFreshness.Unavailable;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        SynchronizeConfiguration();
        if (_configuredEndpoint is null || !CanConnect)
        {
            return;
        }

        await ConnectCoreAsync(_configuredEndpoint, cancellationToken).ConfigureAwait(true);
    }

    private async Task ConnectCoreAsync(
        DisplayEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        BeginNegotiation();
        try
        {
            var result = await _deviceClient.ConnectAsync(
                endpoint,
                cancellationToken).ConfigureAwait(true);
            await ProjectNegotiationResultAsync(result, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            SetBusy(false);
            RaiseDerivedProperties();
        }
    }

    private async Task ProjectNegotiationResultAsync(
        DisplayDeviceNegotiationResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsSuccessful || result.Capabilities is null)
        {
            ProjectNegotiationFailure(result);
            return;
        }

        await ProjectNegotiationSuccessAsync(result.Capabilities, cancellationToken).ConfigureAwait(true);
    }

    private void BeginNegotiation()
    {
        SetBusy(true);
        ConnectionState = DisplayConnectionState.Connecting;
        NegotiationState = DisplayNegotiationState.Negotiating;
        LastResultText = "Connecting and negotiating display protocol v1.0...";
        RaiseDerivedProperties();
    }

    private void ProjectNegotiationFailure(DisplayDeviceNegotiationResult result)
    {
        _connectedEndpoint = null;
        ConnectionState = DisplayConnectionState.Offline;
        NegotiationState = DisplayNegotiationState.Failed;
        CapabilityFreshness = _capabilities is null
            ? DisplayCapabilityFreshness.Unavailable
            : DisplayCapabilityFreshness.Stale;
        LastResultText = FormatFailure(
            "Display negotiation",
            result.RequestFailure,
            result.ResultCode,
            result.Diagnostic);
    }

    private async Task ProjectNegotiationSuccessAsync(
        CapabilitiesResponsePayload capabilities,
        CancellationToken cancellationToken)
    {
        _capabilities = capabilities;
        _connectedEndpoint = _configuredEndpoint;
        ConnectionState = DisplayConnectionState.Connected;
        NegotiationState = DisplayNegotiationState.Succeeded;
        CapabilityFreshness = DisplayCapabilityFreshness.Live;
        LastResultText = "Display capabilities negotiated successfully.";
        await RefreshHealthCoreAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRefreshHealth))]
    private async Task RefreshHealthAsync(CancellationToken cancellationToken)
    {
        SetBusy(true);
        try
        {
            await RefreshHealthCoreAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            SetBusy(false);
            RaiseDerivedProperties();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendStandardTestPattern))]
    private async Task SendStandardTestPatternAsync(CancellationToken cancellationToken)
    {
        await RunOperationAsync(
            () => _deviceClient.SendStandardTestPatternAsync(cancellationToken),
            "Standard test pattern presented successfully.").ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRenderBuiltInTestPattern))]
    private async Task RenderBuiltInTestPatternAsync(CancellationToken cancellationToken)
    {
        await RunOperationAsync(
            () => _deviceClient.RenderBuiltInTestPatternAsync(cancellationToken),
            "Built-in test pattern presented successfully.").ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanSetBrightness))]
    private async Task ApplyBrightnessAsync(CancellationToken cancellationToken)
    {
        var percentage = (byte)Math.Clamp((int)Math.Round(BrightnessPercentage), 0, 100);
        await RunOperationAsync(
            () => _deviceClient.SetBrightnessAsync(percentage, cancellationToken),
            $"Brightness set to {percentage} percent.").ConfigureAwait(true);
    }

    private async Task RefreshHealthCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _deviceClient.QueryHealthAsync(cancellationToken).ConfigureAwait(true);
        if (result.IsSuccessful && result.Health is { } health)
        {
            HealthText = $"{health.HealthState}; {health.AcceptedFrameCount} accepted, {health.RejectedFrameCount} rejected";
            return;
        }

        HealthText = FormatFailure("Health query", result.RequestFailure, result.ResultCode, result.Diagnostic);
        if (ShouldInvalidateCapabilities(result.RequestFailure, result.ResultCode))
        {
            MarkCapabilitiesStale();
        }
    }

    private async Task RunOperationAsync(
        Func<Task<DisplayDeviceOperationResult>> operation,
        string successMessage)
    {
        SetBusy(true);
        try
        {
            var result = await operation().ConfigureAwait(true);
            LastResultText = result.IsSuccessful
                ? successMessage
                : FormatFailure("Display operation", result.RequestFailure, result.ResultCode, result.Diagnostic);
            if (ShouldInvalidateCapabilities(result.RequestFailure, result.ResultCode))
            {
                MarkCapabilitiesStale();
            }
        }
        finally
        {
            SetBusy(false);
            RaiseDerivedProperties();
        }
    }

    private void MarkCapabilitiesStale()
    {
        _connectedEndpoint = null;
        ConnectionState = DisplayConnectionState.Disconnected;
        NegotiationState = DisplayNegotiationState.Stale;
        CapabilityFreshness = DisplayCapabilityFreshness.Stale;
        HealthText = "Stale; reconnect to refresh device health";
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        RaiseDerivedProperties();
    }

    private void RaiseDerivedProperties()
    {
        RaiseCapabilityProperties();
        RaiseDiagnosticProperties();
        ConnectCommand.NotifyCanExecuteChanged();
        RefreshHealthCommand.NotifyCanExecuteChanged();
        SendStandardTestPatternCommand.NotifyCanExecuteChanged();
        RenderBuiltInTestPatternCommand.NotifyCanExecuteChanged();
        ApplyBrightnessCommand.NotifyCanExecuteChanged();
    }

    private void RaiseCapabilityProperties()
    {
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(CanRefreshHealth));
        OnPropertyChanged(nameof(CanSendStandardTestPattern));
        OnPropertyChanged(nameof(CanRenderBuiltInTestPattern));
        OnPropertyChanged(nameof(CanSetBrightness));
        OnPropertyChanged(nameof(HasLiveCapabilities));
    }

    private void RaiseDiagnosticProperties()
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        OnPropertyChanged(nameof(NegotiationStatusText));
        OnPropertyChanged(nameof(CapabilityStatusText));
        OnPropertyChanged(nameof(ResolutionText));
        OnPropertyChanged(nameof(ProtocolVersionText));
        OnPropertyChanged(nameof(FirmwareVersionText));
        OnPropertyChanged(nameof(DeviceIdentityText));
        OnPropertyChanged(nameof(AdapterIdentityText));
        OnPropertyChanged(nameof(PixelFormatsText));
        OnPropertyChanged(nameof(RotationsText));
        OnPropertyChanged(nameof(RegionLimitText));
        OnPropertyChanged(nameof(StandardPatternAvailabilityText));
        OnPropertyChanged(nameof(BuiltInPatternAvailabilityText));
        OnPropertyChanged(nameof(BrightnessAvailabilityText));
    }

    private bool IsStandardPatternCompatible =>
        _capabilities is
        {
            PixelFormats: var formats,
            Rotations: var rotations,
            FrameCapabilities: var frameCapabilities
        }
        && formats.HasFlag(DisplayPixelFormatFlags.Rgb565BigEndian)
        && rotations.HasFlag(DisplayRotationFlags.Degrees0)
        && (frameCapabilities & RequiredFrameCapabilities) == RequiredFrameCapabilities;

    private static bool ShouldInvalidateCapabilities(
        DisplayRequestFailure requestFailure,
        DisplayResultCode? resultCode) =>
        resultCode == DisplayResultCode.WrongSession
        || requestFailure is DisplayRequestFailure.TimedOut
            or DisplayRequestFailure.TransportFailure
            or DisplayRequestFailure.ClientDisposed;

    private static DisplayConnectionState GetUnconfiguredConnectionState(
        DisplayEndpointValidationError error) =>
        error == DisplayEndpointValidationError.MissingAddress
            ? DisplayConnectionState.NotConfigured
            : DisplayConnectionState.InvalidConfiguration;

    private static string GetEndpointStatusText(DisplayEndpointValidationError error) => error switch
    {
        DisplayEndpointValidationError.None => "The endpoint is valid. Connect to negotiate live capabilities.",
        DisplayEndpointValidationError.MissingAddress => "Configure a display IP address in Settings.",
        DisplayEndpointValidationError.InvalidAddress => "Enter a valid IPv4 or IPv6 address in Settings.",
        DisplayEndpointValidationError.UnspecifiedAddress => "Enter the display device address, not an unspecified local address.",
        _ => "Enter a UDP port from 1 through 65535 in Settings."
    };

    private static string FormatFailure(
        string operation,
        DisplayRequestFailure requestFailure,
        DisplayResultCode? resultCode,
        string? diagnostic)
    {
        var summary = resultCode switch
        {
            DisplayResultCode.Unsupported => $"{operation} is not supported by this device.",
            DisplayResultCode.Busy => $"{operation} could not complete because the device is busy.",
            DisplayResultCode.HardwareFailure => $"{operation} failed in the display hardware.",
            DisplayResultCode.WrongSession => $"{operation} used a stale device session. Reconnect and try again.",
            not null => $"{operation} failed with {resultCode}.",
            null when requestFailure == DisplayRequestFailure.TimedOut => $"{operation} timed out.",
            null when requestFailure == DisplayRequestFailure.Cancelled => $"{operation} was cancelled.",
            _ => $"{operation} failed with {requestFailure}."
        };
        return string.IsNullOrWhiteSpace(diagnostic) ? summary : $"{summary} {diagnostic}";
    }
}
