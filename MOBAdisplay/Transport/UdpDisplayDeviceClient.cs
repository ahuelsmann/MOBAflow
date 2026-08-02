// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

/// <summary>
/// Maintains one live display protocol v1.0 session for diagnostics and user-requested commands.
/// </summary>
public sealed class UdpDisplayDeviceClient : IDisplayDeviceClient
{
    private readonly Func<DisplayEndpoint, IDisplayDatagramTransport> _transportFactory;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private IDisplayDatagramTransport? _transport;
    private DisplayProtocolClient? _protocolClient;
    private DisplayProtocolFrameSession? _frameSession;
    private CapabilitiesResponsePayload? _capabilities;
    private bool _disposed;

    /// <summary>Initializes a UDP client that creates transport for the configured endpoint.</summary>
    public UdpDisplayDeviceClient()
        : this(endpoint => new UdpDisplayDatagramTransport(endpoint.Address, endpoint.Port))
    {
    }

    internal UdpDisplayDeviceClient(Func<DisplayEndpoint, IDisplayDatagramTransport> transportFactory)
    {
        ArgumentNullException.ThrowIfNull(transportFactory);
        _transportFactory = transportFactory;
    }

    /// <inheritdoc />
    public async Task<DisplayDeviceNegotiationResult> ConnectAsync(
        DisplayEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            DisposeConnection();
            _transport = _transportFactory(endpoint);
            _protocolClient = new DisplayProtocolClient(_transport);
            var minimumVersion = DisplayProtocol.CurrentVersion;
            var maximumVersion = DisplayProtocol.CurrentVersion;
            var outcome = await _protocolClient.SendRequestAsync(
                new HelloRequestPayload(
                    minimumVersion,
                    maximumVersion,
                    DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccessful && outcome.Response is CapabilitiesResponsePayload capabilities)
            {
                var validationFailure = ValidateNegotiatedCapabilities(
                    capabilities,
                    minimumVersion,
                    maximumVersion);
                if (validationFailure is not null)
                {
                    DisposeConnection();
                    return DisplayDeviceNegotiationResult.Failed(
                        DisplayRequestFailure.InvalidPayload,
                        validationFailure);
                }

                _capabilities = capabilities;
                _frameSession = new DisplayProtocolFrameSession(_protocolClient, negotiatedCapabilities: capabilities);
                return DisplayDeviceNegotiationResult.Succeeded(capabilities);
            }

            var resultCode = (outcome.Response as ResultPayload?)?.ResultCode;
            var failed = DisplayDeviceNegotiationResult.Failed(
                outcome.Failure,
                outcome.Diagnostic,
                resultCode);
            DisposeConnection();
            return failed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DisposeConnection();
            return DisplayDeviceNegotiationResult.Failed(
                DisplayRequestFailure.Cancelled,
                "Display negotiation was cancelled.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Net.Sockets.SocketException)
        {
            DisposeConnection();
            return DisplayDeviceNegotiationResult.Failed(
                DisplayRequestFailure.TransportFailure,
                $"Display connection failed with {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<DisplayDeviceHealthResult> QueryHealthAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_protocolClient is null || _capabilities is null)
            {
                return DisplayDeviceHealthResult.Failed(
                    DisplayRequestFailure.TransportFailure,
                    "No negotiated display session is active.");
            }

            var outcome = await _protocolClient.SendRequestAsync(
                new HealthRequestPayload(),
                sessionId: _capabilities.SessionId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccessful && outcome.Response is HealthResponsePayload health)
            {
                return DisplayDeviceHealthResult.Succeeded(health);
            }

            var resultCode = (outcome.Response as ResultPayload?)?.ResultCode;
            if (resultCode == DisplayResultCode.WrongSession)
            {
                DisposeConnection();
            }

            return DisplayDeviceHealthResult.Failed(outcome.Failure, outcome.Diagnostic, resultCode);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<DisplayDeviceOperationResult> SendStandardTestPatternAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_frameSession is null || _capabilities is null)
            {
                return NotConnected();
            }

            var incompatibility =
                DisplayStandardPatternRequirements.EvaluateStandardPattern(_capabilities);
            if (incompatibility != DisplayStandardPatternIncompatibility.None)
            {
                return DisplayDeviceOperationResult.Unsupported(
                    GetStandardPatternIncompatibilityDiagnostic(incompatibility));
            }

            var frame = DisplayConformancePattern.CreateRgb565(
                _capabilities.Width,
                _capabilities.Height);
            await _frameSession.SendFrameAsync(
                frame,
                _capabilities.Width,
                _capabilities.Height,
                cancellationToken).ConfigureAwait(false);
            return DisplayDeviceOperationResult.Succeeded();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return DisplayDeviceOperationResult.Failed(
                DisplayRequestFailure.Cancelled,
                "The standard test pattern transfer was cancelled.");
        }
        catch (DisplayProtocolOperationException ex)
        {
            if (ex.ResultCode == DisplayResultCode.WrongSession)
            {
                DisposeConnection();
            }

            return DisplayDeviceOperationResult.Failed(
                ex.RequestFailure,
                ex.Message,
                ex.ResultCode);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Net.Sockets.SocketException)
        {
            return DisplayDeviceOperationResult.Failed(
                DisplayRequestFailure.TransportFailure,
                ex.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    /// <inheritdoc />
    public Task<DisplayDeviceOperationResult> RenderBuiltInTestPatternAsync(
        CancellationToken cancellationToken = default) =>
        SendOptionalCommandAsync(
            DisplayOptionalCommandFlags.RenderTestPattern,
            new RenderTestPatternPayload(DisplayTestPattern.Conformance),
            "The display does not support its built-in test pattern.",
            cancellationToken);

    /// <inheritdoc />
    public Task<DisplayDeviceOperationResult> SetBrightnessAsync(
        byte percentage,
        CancellationToken cancellationToken = default)
    {
        if (percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage));
        }

        return SendOptionalCommandAsync(
            DisplayOptionalCommandFlags.SetBrightness,
            new SetBrightnessPayload(percentage),
            "The display does not support brightness control.",
            cancellationToken);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        DisposeConnection();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeConnection();
        _operationGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<DisplayDeviceOperationResult> SendOptionalCommandAsync(
        DisplayOptionalCommandFlags requiredCommand,
        IDisplayProtocolPayload request,
        string unsupportedDiagnostic,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_protocolClient is null || _capabilities is null)
            {
                return NotConnected();
            }

            if (!_capabilities.OptionalCommands.HasFlag(requiredCommand))
            {
                return DisplayDeviceOperationResult.Unsupported(unsupportedDiagnostic);
            }

            var outcome = await _protocolClient.SendRequestAsync(
                request,
                sessionId: _capabilities.SessionId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!outcome.IsSuccessful || outcome.Response is not ResultPayload result)
            {
                return DisplayDeviceOperationResult.Failed(outcome.Failure, outcome.Diagnostic);
            }

            if (result.ResultCode == DisplayResultCode.WrongSession)
            {
                DisposeConnection();
            }

            return result.ResultCode == DisplayResultCode.Ok
                ? DisplayDeviceOperationResult.Succeeded()
                : DisplayDeviceOperationResult.Failed(
                    DisplayRequestFailure.None,
                    $"The display returned {result.ResultCode}.",
                    result.ResultCode);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private void DisposeConnection()
    {
        _frameSession = null;
        _capabilities = null;
        _protocolClient?.Dispose();
        _protocolClient = null;
        if (_transport is IDisposable disposableTransport)
        {
            disposableTransport.Dispose();
        }

        _transport = null;
    }

    private static string? ValidateNegotiatedCapabilities(
        CapabilitiesResponsePayload capabilities,
        DisplayProtocolVersion minimumVersion,
        DisplayProtocolVersion maximumVersion)
    {
        if (capabilities.SelectedVersion.CompareTo(minimumVersion) < 0
            || capabilities.SelectedVersion.CompareTo(maximumVersion) > 0)
        {
            return $"The selected protocol version {capabilities.SelectedVersion} is outside "
                + $"the offered range {minimumVersion} through {maximumVersion}.";
        }

        var frameByteCount = (long)capabilities.Width * capabilities.Height * 2;
        if (frameByteCount == 0)
        {
            return "The negotiated frame dimensions must produce at least one RGB565 pixel.";
        }

        return null;
    }

    private static string GetStandardPatternIncompatibilityDiagnostic(
        DisplayStandardPatternIncompatibility incompatibility) => incompatibility switch
        {
            DisplayStandardPatternIncompatibility.FrameExceedsHostSafetyLimit =>
                $"The standard pattern exceeds the {DisplayStandardPatternRequirements.MaximumHostFrameByteCount}-byte host safety limit.",
            _ => $"The negotiated capabilities do not support the standard pattern: {incompatibility}."
        };

    private static DisplayDeviceOperationResult NotConnected() =>
        DisplayDeviceOperationResult.Failed(
            DisplayRequestFailure.TransportFailure,
            "No negotiated display session is active.");
}
