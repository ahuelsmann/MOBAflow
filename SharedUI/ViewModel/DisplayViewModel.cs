// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Common.Configuration;
using Moba.Display.Protocol;
using Moba.Display.Transport;

/// <summary>Identifies the current connection state for the configured display endpoint.</summary>
public enum DisplayConnectionState
{
    /// <summary>No display endpoint is configured.</summary>
    NotConfigured,
    /// <summary>The configured display endpoint is invalid.</summary>
    InvalidConfiguration,
    /// <summary>A valid endpoint is configured but not connected.</summary>
    Disconnected,
    /// <summary>The client is connecting and negotiating capabilities.</summary>
    Connecting,
    /// <summary>The configured endpoint has a live negotiated session.</summary>
    Connected,
    /// <summary>The configured endpoint could not be reached or negotiated.</summary>
    Offline
}

/// <summary>Identifies the lifecycle state of display capability negotiation.</summary>
public enum DisplayNegotiationState
{
    /// <summary>No negotiation has started for the configured endpoint.</summary>
    NotStarted,
    /// <summary>A negotiation request is currently active.</summary>
    Negotiating,
    /// <summary>The current endpoint negotiated compatible live capabilities.</summary>
    Succeeded,
    /// <summary>The latest negotiation attempt failed.</summary>
    Failed,
    /// <summary>Previously negotiated capabilities no longer authorize commands.</summary>
    Stale
}

/// <summary>Identifies whether displayed capabilities can authorize device commands.</summary>
public enum DisplayCapabilityFreshness
{
    /// <summary>No capabilities have been negotiated.</summary>
    Unavailable,
    /// <summary>Capabilities belong to the current live endpoint session.</summary>
    Live,
    /// <summary>Capabilities are diagnostic history only and require renegotiation.</summary>
    Stale
}

/// <summary>
/// Projects one configured ESP32 display endpoint and its live negotiated state.
/// </summary>
public sealed partial class DisplayViewModel : ObservableObject
{
    private const string NotNegotiatedText = "Not negotiated";
    private readonly AppSettings _settings;
    private readonly IDisplayDeviceClient _deviceClient;
    private DisplayEndpoint? _configuredEndpoint;
    private DisplayEndpoint? _connectedEndpoint;
    private CapabilitiesResponsePayload? _capabilities;

    /// <summary>Initializes display diagnostics and commands for the persisted endpoint settings.</summary>
    /// <param name="settings">Application settings containing the explicit display endpoint.</param>
    /// <param name="deviceClient">Capability-aware display transport client.</param>
    public DisplayViewModel(AppSettings settings, IDisplayDeviceClient deviceClient)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(deviceClient);
        _settings = settings;
        _deviceClient = deviceClient;
        SynchronizeConfiguration();
    }

    /// <summary>Gets whether the persisted endpoint is valid for connection attempts.</summary>
    [ObservableProperty]
    public partial bool IsEndpointValid { get; private set; }

    /// <summary>Gets whether a display command is currently running.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    /// <summary>Gets the normalized configured endpoint or an unconfigured marker.</summary>
    [ObservableProperty]
    public partial string ConfiguredEndpointText { get; private set; } = "Not configured";

    /// <summary>Gets actionable validation guidance for the configured endpoint.</summary>
    [ObservableProperty]
    public partial string EndpointStatusText { get; private set; } = "Configure a display IP address in Settings.";

    /// <summary>Gets the current endpoint connection state.</summary>
    [ObservableProperty]
    public partial DisplayConnectionState ConnectionState { get; private set; } = DisplayConnectionState.NotConfigured;

    /// <summary>Gets the current capability-negotiation state.</summary>
    [ObservableProperty]
    public partial DisplayNegotiationState NegotiationState { get; private set; } = DisplayNegotiationState.NotStarted;

    /// <summary>Gets whether projected capabilities are unavailable, live, or stale.</summary>
    [ObservableProperty]
    public partial DisplayCapabilityFreshness CapabilityFreshness { get; private set; } = DisplayCapabilityFreshness.Unavailable;

    /// <summary>Gets the latest safe device-health summary.</summary>
    [ObservableProperty]
    public partial string HealthText { get; private set; } = "Not queried";

    /// <summary>Gets the safe result text for the latest connection or device operation.</summary>
    [ObservableProperty]
    public partial string LastResultText { get; private set; } = "No display operation has run.";

    /// <summary>Gets or sets the requested brightness percentage from zero through 100.</summary>
    [ObservableProperty]
    public partial double BrightnessPercentage { get; set; } = 100;

    /// <summary>Gets whether a new connection attempt is currently allowed.</summary>
    public bool CanConnect => IsEndpointValid && !IsBusy;

    /// <summary>Gets whether health can be queried through a live negotiated session.</summary>
    public bool CanRefreshHealth => HasLiveCapabilities && !IsBusy;

    /// <summary>Gets whether the host-rendered standard pattern can be sent safely.</summary>
    public bool CanSendStandardTestPattern =>
        HasLiveCapabilities && IsStandardPatternCompatible && !IsBusy;

    /// <summary>Gets whether the live device advertises its optional built-in pattern.</summary>
    public bool CanRenderBuiltInTestPattern =>
        HasLiveCapabilities
        && _capabilities?.OptionalCommands.HasFlag(DisplayOptionalCommandFlags.RenderTestPattern) == true
        && !IsBusy;

    /// <summary>Gets whether the live device advertises brightness control.</summary>
    public bool CanSetBrightness =>
        HasLiveCapabilities
        && _capabilities?.OptionalCommands.HasFlag(DisplayOptionalCommandFlags.SetBrightness) == true
        && !IsBusy;

    /// <summary>Gets whether capabilities belong to the currently configured live endpoint.</summary>
    public bool HasLiveCapabilities =>
        ConnectionState == DisplayConnectionState.Connected
        && NegotiationState == DisplayNegotiationState.Succeeded
        && CapabilityFreshness == DisplayCapabilityFreshness.Live
        && _capabilities is not null
        && Equals(_configuredEndpoint, _connectedEndpoint);

    /// <summary>Gets the user-facing connection-state label.</summary>
    public string ConnectionStatusText => ConnectionState switch
    {
        DisplayConnectionState.NotConfigured => "Not configured",
        DisplayConnectionState.InvalidConfiguration => "Invalid endpoint",
        DisplayConnectionState.Disconnected => "Disconnected",
        DisplayConnectionState.Connecting => "Connecting",
        DisplayConnectionState.Connected => "Connected",
        _ => "Offline"
    };

    /// <summary>Gets the user-facing negotiation-state label.</summary>
    public string NegotiationStatusText => NegotiationState switch
    {
        DisplayNegotiationState.NotStarted => "Not started",
        DisplayNegotiationState.Negotiating => "Negotiating protocol and capabilities",
        DisplayNegotiationState.Succeeded => "Protocol and capabilities negotiated",
        DisplayNegotiationState.Stale => "Previous capabilities are stale; reconnect required",
        _ => "Negotiation failed"
    };

    /// <summary>Gets the user-facing capability-freshness explanation.</summary>
    public string CapabilityStatusText => CapabilityFreshness switch
    {
        DisplayCapabilityFreshness.Live => "Capabilities are live for the configured endpoint.",
        DisplayCapabilityFreshness.Stale => "Previous capabilities are stale and cannot authorize commands.",
        _ => "Capabilities have not been negotiated."
    };

    /// <summary>Gets the negotiated native display dimensions.</summary>
    public string ResolutionText => _capabilities is null
        ? NotNegotiatedText
        : $"{_capabilities.Width} x {_capabilities.Height}";

    /// <summary>Gets the negotiated protocol version.</summary>
    public string ProtocolVersionText => _capabilities?.SelectedVersion.ToString() ?? NotNegotiatedText;

    /// <summary>Gets the negotiated firmware version text.</summary>
    public string FirmwareVersionText => _capabilities?.FirmwareVersion ?? NotNegotiatedText;

    /// <summary>Gets the negotiated device identity.</summary>
    public string DeviceIdentityText => _capabilities?.DeviceIdentity ?? NotNegotiatedText;

    /// <summary>Gets the negotiated display-adapter identity.</summary>
    public string AdapterIdentityText => _capabilities?.AdapterIdentity ?? NotNegotiatedText;

    /// <summary>Gets the negotiated pixel-format flags.</summary>
    public string PixelFormatsText => _capabilities?.PixelFormats.ToString() ?? NotNegotiatedText;

    /// <summary>Gets the negotiated rotation flags.</summary>
    public string RotationsText => _capabilities?.Rotations.ToString() ?? NotNegotiatedText;

    /// <summary>Gets the negotiated maximum region payload length.</summary>
    public string RegionLimitText => _capabilities is null
        ? NotNegotiatedText
        : $"{_capabilities.MaximumRegionPayloadLength} bytes";

    /// <summary>Gets why the standard host pattern is available or blocked.</summary>
    public string StandardPatternAvailabilityText
    {
        get
        {
            if (_capabilities is null)
            {
                return "Negotiate capabilities before sending the standard host pattern.";
            }

            var incompatibility =
                DisplayStandardPatternRequirements.EvaluateStandardPattern(_capabilities);
            if (incompatibility != DisplayStandardPatternIncompatibility.None)
            {
                return GetStandardPatternIncompatibilityText(incompatibility);
            }

            return HasLiveCapabilities
                ? "The standard host-rendered pattern is supported by the live device session."
                : "Reconnect and negotiate live capabilities before sending the standard pattern.";
        }
    }

    /// <summary>Gets why the optional device-rendered pattern is available or blocked.</summary>
    public string BuiltInPatternAvailabilityText => _capabilities switch
    {
        null => "Negotiate capabilities to check built-in pattern support.",
        { OptionalCommands: var commands }
            when commands.HasFlag(DisplayOptionalCommandFlags.RenderTestPattern) =>
            "The device supports its built-in conformance pattern.",
        _ => "The device does not support a built-in test pattern. Use the standard host pattern instead."
    };

    /// <summary>Gets why brightness control is available or blocked.</summary>
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
            await ProjectNegotiationResultAsync(result, endpoint, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            SetBusy(false);
            RaiseDerivedProperties();
        }
    }

    private async Task ProjectNegotiationResultAsync(
        DisplayDeviceNegotiationResult result,
        DisplayEndpoint attemptedEndpoint,
        CancellationToken cancellationToken)
    {
        if (!Equals(attemptedEndpoint, _configuredEndpoint))
        {
            DiscardSupersededNegotiation();
            return;
        }

        if (!result.IsSuccessful || result.Capabilities is null)
        {
            ProjectNegotiationFailure(result);
            return;
        }

        await ProjectNegotiationSuccessAsync(
            result.Capabilities,
            attemptedEndpoint,
            cancellationToken).ConfigureAwait(true);
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
        DisplayEndpoint connectedEndpoint,
        CancellationToken cancellationToken)
    {
        _capabilities = capabilities;
        _connectedEndpoint = connectedEndpoint;
        ConnectionState = DisplayConnectionState.Connected;
        NegotiationState = DisplayNegotiationState.Succeeded;
        CapabilityFreshness = DisplayCapabilityFreshness.Live;
        LastResultText = "Display capabilities negotiated successfully.";
        await RefreshHealthCoreAsync(connectedEndpoint, cancellationToken).ConfigureAwait(true);
    }

    private void DiscardSupersededNegotiation()
    {
        _deviceClient.Disconnect();
        _connectedEndpoint = null;
        if (_capabilities is null)
        {
            NegotiationState = DisplayNegotiationState.NotStarted;
            CapabilityFreshness = DisplayCapabilityFreshness.Unavailable;
        }
        else
        {
            MarkCapabilitiesStale();
        }

        LastResultText = "The configured display endpoint changed during negotiation. Reconnect to the current endpoint.";
    }

    [RelayCommand(CanExecute = nameof(CanRefreshHealth))]
    private async Task RefreshHealthAsync(CancellationToken cancellationToken)
    {
        var operationEndpoint = _connectedEndpoint;
        SetBusy(true);
        try
        {
            await RefreshHealthCoreAsync(operationEndpoint, cancellationToken).ConfigureAwait(true);
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

    private async Task RefreshHealthCoreAsync(
        DisplayEndpoint? operationEndpoint,
        CancellationToken cancellationToken)
    {
        var result = await _deviceClient.QueryHealthAsync(cancellationToken).ConfigureAwait(true);
        if (!IsOperationEndpointCurrent(operationEndpoint))
        {
            return;
        }

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
        var operationEndpoint = _connectedEndpoint;
        SetBusy(true);
        try
        {
            var result = await operation().ConfigureAwait(true);
            if (!IsOperationEndpointCurrent(operationEndpoint))
            {
                return;
            }

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
        _capabilities is not null
        && DisplayStandardPatternRequirements.EvaluateStandardPattern(_capabilities)
            == DisplayStandardPatternIncompatibility.None;

    private static string GetStandardPatternIncompatibilityText(
        DisplayStandardPatternIncompatibility incompatibility) => incompatibility switch
        {
            DisplayStandardPatternIncompatibility.MissingRgb565BigEndian =>
                "The device does not support the RGB565 big-endian format required by the standard pattern.",
            DisplayStandardPatternIncompatibility.MissingZeroDegreeRotation =>
                "The device does not support the zero-degree rotation required by the standard pattern.",
            DisplayStandardPatternIncompatibility.MissingAtomicFrameTransfer =>
                "The device does not support the complete atomic frame transfer required by the standard pattern.",
            DisplayStandardPatternIncompatibility.FrameExceedsHostSafetyLimit =>
                $"The native frame exceeds the {DisplayStandardPatternRequirements.MaximumHostFrameByteCount}-byte host safety limit for the standard pattern.",
            _ => "The standard host-rendered pattern is compatible."
        };

    private bool IsOperationEndpointCurrent(DisplayEndpoint? operationEndpoint) =>
        operationEndpoint is not null
        && Equals(operationEndpoint, _connectedEndpoint)
        && Equals(operationEndpoint, _configuredEndpoint);

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
