// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Discovery;
using Common.Events;
using Common.Security;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

public enum RemotePairingUiState
{
    Unpaired,
    Scanning,
    WaitingForApproval,
    Paired,
    Error
}

public sealed record RemotePairingPollingOptions(
    TimeSpan PollInterval,
    TimeSpan ApprovalTimeout)
{
    public static RemotePairingPollingOptions Default { get; } = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromMinutes(2));
}

/// <summary>
/// Owns the QR-only MOBAsmart administrator pairing flow without exposing credentials to the UI.
/// </summary>
public sealed partial class RemotePairingViewModel : ObservableObject
{
    private readonly IEventBus? _eventBus;
    private readonly IPairingCameraAccess? _pairingCameraAccess;
    private readonly RemotePairingPollingOptions _pollingOptions;
    private readonly RemoteControlSessionService _sessionService;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _operationCancellation;
    private bool _isInitialized;

    [ObservableProperty]
    public partial string ConfirmationCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "MOBAsmart";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsPaired { get; set; }

    [ObservableProperty]
    public partial bool IsScannerVisible { get; set; }

    [ObservableProperty]
    public partial bool IsWaitingForApproval { get; set; }

    [ObservableProperty]
    public partial RemotePairingUiState State { get; set; } = RemotePairingUiState.Unpaired;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } =
        "Not paired. Scan the QR code shown under MOBAflow Settings / REST API.";

    public RemotePairingViewModel(
        RemoteControlSessionService sessionService,
        IPairingCameraAccess? pairingCameraAccess = null,
        IEventBus? eventBus = null,
        TimeProvider? timeProvider = null,
        RemotePairingPollingOptions? pollingOptions = null)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _pairingCameraAccess = pairingCameraAccess;
        _eventBus = eventBus;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _pollingOptions = pollingOptions ?? RemotePairingPollingOptions.Default;

        if (_pollingOptions.PollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingOptions), "The polling interval must be positive.");
        }

        if (_pollingOptions.ApprovalTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingOptions), "The approval timeout must be positive.");
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        var operation = BeginOperation(cancellationToken);
        try
        {
            var session = await _sessionService.RestoreAsync(operation.Token).ConfigureAwait(true);
            if (session is null)
            {
                SetUnpaired("Not paired. Scan the QR code shown under MOBAflow Settings / REST API.");
                return;
            }

            if (session.Role != RemoteControlRole.RemoteControl)
            {
                await _sessionService.ClearAsync(operation.Token).ConfigureAwait(true);
                SetUnpaired("The previous access level is no longer supported. Scan a new administrator QR code.");
                return;
            }

            State = RemotePairingUiState.Paired;
            StatusMessage = "Administrator credentials restored. Connecting to MOBAflow...";
        }
        catch (OperationCanceledException)
        {
            _isInitialized = false;
            StatusMessage = "Credential restore cancelled.";
        }
        catch (Exception)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Protected credentials could not be restored. Pair this device again.";
        }
        finally
        {
            CompleteOperation(operation);
        }
    }

    [RelayCommand]
    private async Task OpenScannerAsync(CancellationToken cancellationToken)
    {
        if (_pairingCameraAccess is not null &&
            !await _pairingCameraAccess.RequestAsync(cancellationToken).ConfigureAwait(true))
        {
            StatusMessage = "Camera access is required to scan the MOBAflow pairing QR code.";
            return;
        }

        State = RemotePairingUiState.Scanning;
        StatusMessage = "Point the camera at the QR code shown by MOBAflow.";
    }

    [RelayCommand]
    private async Task ScanQrCodeAsync(string? payload)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        IsScannerVisible = false;
        var decoded = RemotePairingQrCode.Decode(payload, _timeProvider);
        if (!decoded.IsSuccess)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = decoded.Failure == RemotePairingQrFailure.Expired
                ? "This pairing QR code expired. Create a new code in MOBAflow."
                : "This is not a valid MOBAflow pairing QR code.";
            return;
        }

        var invitation = decoded.Invitation
            ?? throw new InvalidOperationException("A successful QR decode must contain an invitation.");
        var endpoint = new MobApiDiscoveryEndpoint(
            invitation.IpAddress,
            invitation.HttpPort,
            invitation.HttpsPort,
            invitation.ServerInstanceId,
            invitation.ServerPublicKeyFingerprint,
            DiscoveryResponseParser.CurrentProtocolVersion);
        var operation = BeginOperation(CancellationToken.None);
        ConfirmationCode = string.Empty;
        try
        {
            var attempt = await _sessionService.BeginPairingAsync(
                endpoint,
                invitation.PairingSecret,
                DisplayName,
                RemoteControlRole.RemoteControl,
                operation.Token).ConfigureAwait(true);
            ConfirmationCode = attempt.ConfirmationCode;
            State = RemotePairingUiState.WaitingForApproval;
            StatusMessage = "Confirm this code in MOBAflow. Waiting for approval...";
            await WaitForApprovalAsync(attempt, operation.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            SetUnpaired("Pairing was cancelled.");
        }
        catch (Exception)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Pairing failed. Create a new QR code in MOBAflow and try again.";
        }
        finally
        {
            CompleteOperation(operation);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _operationCancellation?.Cancel();
        SetUnpaired("Pairing was cancelled.");
    }

    [RelayCommand(CanExecute = nameof(CanForget))]
    private async Task ForgetAsync()
    {
        var operation = BeginOperation(CancellationToken.None);
        try
        {
            await _sessionService.ClearAsync(operation.Token).ConfigureAwait(true);
            SetUnpaired("Protected credentials were removed from this device.");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Credential removal cancelled.";
        }
        catch (Exception)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Protected credentials could not be removed.";
        }
        finally
        {
            CompleteOperation(operation);
        }
    }

    private bool CanForget() =>
        this.State == RemotePairingUiState.Paired && !this.IsBusy;

    partial void OnStateChanged(RemotePairingUiState value)
    {
        IsPaired = value == RemotePairingUiState.Paired;
        IsScannerVisible = value == RemotePairingUiState.Scanning;
        IsWaitingForApproval = value == RemotePairingUiState.WaitingForApproval;
        ForgetCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value) => ForgetCommand.NotifyCanExecuteChanged();

    private async Task WaitForApprovalAsync(
        RemotePairingAttempt attempt,
        CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow() + _pollingOptions.ApprovalTimeout;
        while (_timeProvider.GetUtcNow() < deadline)
        {
            var session = await _sessionService
                .ClaimAsync(attempt, cancellationToken)
                .ConfigureAwait(true);
            if (session is not null)
            {
                if (session.Role != RemoteControlRole.RemoteControl)
                {
                    await _sessionService.ClearAsync(cancellationToken).ConfigureAwait(true);
                    State = RemotePairingUiState.Error;
                    StatusMessage = "MOBAflow did not grant administrator access. Create a new QR code.";
                    return;
                }

                ConfirmationCode = string.Empty;
                State = RemotePairingUiState.Paired;
                StatusMessage = "Pairing approved. Connecting to MOBAflow...";
                _eventBus?.Publish(new RemotePairingCompletedEvent(
                    attempt.Endpoint.IpAddress,
                    attempt.Endpoint.HttpPort));
                return;
            }

            var remaining = deadline - _timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            var delay = remaining < _pollingOptions.PollInterval
                ? remaining
                : _pollingOptions.PollInterval;
            await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(true);
        }

        State = RemotePairingUiState.Error;
        StatusMessage = "Pairing approval timed out. Create a new QR code in MOBAflow.";
    }

    private void SetUnpaired(string message)
    {
        this.ConfirmationCode = string.Empty;
        this.State = RemotePairingUiState.Unpaired;
        this.StatusMessage = message;
    }

    private CancellationTokenSource BeginOperation(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : new CancellationTokenSource();
        IsBusy = true;
        return _operationCancellation;
    }

    private void CompleteOperation(CancellationTokenSource operation)
    {
        if (!ReferenceEquals(_operationCancellation, operation))
        {
            operation.Dispose();
            return;
        }

        _operationCancellation = null;
        IsBusy = false;
        operation.Dispose();
    }
}
