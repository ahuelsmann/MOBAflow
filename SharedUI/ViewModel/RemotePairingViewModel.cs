// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Discovery;
using Common.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;

public enum RemotePairingUiState
{
    Unpaired,
    Discovering,
    CandidateFound,
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
/// Owns the explicit MOBAsmart pairing flow without exposing credentials to the UI layer.
/// </summary>
public sealed partial class RemotePairingViewModel : ObservableObject
{
    private readonly IAuthenticatedRestDiscoveryService _discoveryService;
    private readonly RemotePairingPollingOptions _pollingOptions;
    private readonly RemoteControlSessionService _sessionService;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _operationCancellation;
    private MobApiDiscoveryEndpoint? _selectedEndpoint;
    private bool _isInitialized;

    [ObservableProperty]
    private string confirmationCode = string.Empty;

    [ObservableProperty]
    private string displayName = "MOBAsmart";

    [ObservableProperty]
    private string fingerprint = string.Empty;

    [ObservableProperty]
    private bool hasDiscoveredServer;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isFingerprintConfirmed;

    [ObservableProperty]
    private bool isPaired;

    [ObservableProperty]
    private bool isReadOnlySelected;

    [ObservableProperty]
    private bool isRemoteControlSelected = true;

    [ObservableProperty]
    private bool isWaitingForApproval;

    [ObservableProperty]
    private string pairingSecret = string.Empty;

    [ObservableProperty]
    private string pairedRole = string.Empty;

    [ObservableProperty]
    private string serverAddress = string.Empty;

    [ObservableProperty]
    private string serverInstanceId = string.Empty;

    [ObservableProperty]
    private bool showPairingForm;

    [ObservableProperty]
    private RemotePairingUiState state = RemotePairingUiState.Unpaired;

    [ObservableProperty]
    private string statusMessage = "Not paired. Discover MOBAflow to begin.";

    public RemotePairingViewModel(
        IAuthenticatedRestDiscoveryService discoveryService,
        RemoteControlSessionService sessionService,
        TimeProvider? timeProvider = null,
        RemotePairingPollingOptions? pollingOptions = null)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
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
            var session = await _sessionService.RestoreAsync(operation.Token);
            if (session is null)
            {
                State = RemotePairingUiState.Unpaired;
                StatusMessage = "Not paired. Discover MOBAflow to begin.";
                return;
            }

            PairedRole = session.Role == RemoteControlRole.ReadOnly
                ? "Read only"
                : "Remote control";
            ConfirmationCode = string.Empty;
            State = RemotePairingUiState.Paired;
            StatusMessage = "Protected credentials restored.";
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

    [RelayCommand(CanExecute = nameof(CanDiscover))]
    private async Task DiscoverAsync()
    {
        var operation = BeginOperation(CancellationToken.None);
        State = RemotePairingUiState.Discovering;
        StatusMessage = "Searching for an authenticated MOBAflow endpoint...";
        ResetCandidate();

        try
        {
            var endpoint = await _discoveryService.DiscoverAuthenticatedServerAsync(operation.Token);
            if (endpoint is null)
            {
                State = RemotePairingUiState.Error;
                StatusMessage = "No authenticated MOBAflow endpoint was found.";
                return;
            }

            _selectedEndpoint = endpoint;
            ServerAddress = $"{endpoint.IpAddress}:{endpoint.HttpsPort}";
            ServerInstanceId = endpoint.ServerInstanceId ?? string.Empty;
            Fingerprint = FormatFingerprint(endpoint.ServerPublicKeyFingerprint);
            HasDiscoveredServer = true;
            IsFingerprintConfirmed = false;
            State = RemotePairingUiState.CandidateFound;
            StatusMessage = "Compare this fingerprint with the value shown by MOBAflow.";
        }
        catch (OperationCanceledException)
        {
            State = RemotePairingUiState.Unpaired;
            StatusMessage = "Discovery cancelled.";
        }
        catch (Exception)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Authenticated discovery failed. Check the network and try again.";
        }
        finally
        {
            CompleteOperation(operation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartPairing))]
    private async Task StartPairingAsync()
    {
        if (_selectedEndpoint is null)
        {
            return;
        }

        var operation = BeginOperation(CancellationToken.None);
        var secret = PairingSecret.Trim();
        PairingSecret = string.Empty;
        ConfirmationCode = string.Empty;

        try
        {
            var role = IsReadOnlySelected
                ? RemoteControlRole.ReadOnly
                : RemoteControlRole.RemoteControl;
            var attempt = await _sessionService.BeginPairingAsync(
                _selectedEndpoint,
                secret,
                DisplayName,
                role,
                operation.Token);

            ConfirmationCode = attempt.ConfirmationCode;
            State = RemotePairingUiState.WaitingForApproval;
            StatusMessage = "Confirm this code in MOBAflow. Waiting for approval...";

            await WaitForApprovalAsync(attempt, operation.Token);
        }
        catch (OperationCanceledException)
        {
            State = RemotePairingUiState.CandidateFound;
            StatusMessage = "Pairing cancelled. The discovered server remains selected.";
        }
        catch (ArgumentException)
        {
            State = RemotePairingUiState.CandidateFound;
            StatusMessage = "Enter a valid 43-character pairing secret and display name.";
        }
        catch (InvalidOperationException)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Pairing was rejected or is no longer valid. Start a new request.";
        }
        catch (Exception)
        {
            State = RemotePairingUiState.Error;
            StatusMessage = "Pairing failed. Check the network and try again.";
        }
        finally
        {
            CompleteOperation(operation);
        }
    }

    [RelayCommand(CanExecute = nameof(CanForget))]
    private async Task ForgetAsync()
    {
        var operation = BeginOperation(CancellationToken.None);
        try
        {
            await _sessionService.ClearAsync(operation.Token);
            PairedRole = string.Empty;
            ConfirmationCode = string.Empty;
            State = HasDiscoveredServer
                ? RemotePairingUiState.CandidateFound
                : RemotePairingUiState.Unpaired;
            StatusMessage = "Protected credentials were removed from this device.";
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

    [RelayCommand]
    private void Cancel()
    {
        _operationCancellation?.Cancel();
    }

    private bool CanDiscover() => _operationCancellation is null && !IsPaired;

    private bool CanStartPairing() =>
        !IsBusy &&
        _selectedEndpoint is not null &&
        IsFingerprintConfirmed &&
        PairingSecret.Trim().Length == 43 &&
        !string.IsNullOrWhiteSpace(DisplayName) &&
        DisplayName.Trim().Length <= 100 &&
        (IsRemoteControlSelected || IsReadOnlySelected);

    private bool CanForget() => _operationCancellation is null && IsPaired;

    partial void OnDisplayNameChanged(string value) => StartPairingCommand.NotifyCanExecuteChanged();

    partial void OnHasDiscoveredServerChanged(bool value) =>
        ShowPairingForm = value && State != RemotePairingUiState.Paired;

    partial void OnIsBusyChanged(bool value)
    {
        DiscoverCommand.NotifyCanExecuteChanged();
        StartPairingCommand.NotifyCanExecuteChanged();
        ForgetCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsFingerprintConfirmedChanged(bool value) => StartPairingCommand.NotifyCanExecuteChanged();

    partial void OnIsReadOnlySelectedChanged(bool value)
    {
        if (value)
        {
            IsRemoteControlSelected = false;
        }
        else if (!IsRemoteControlSelected)
        {
            IsRemoteControlSelected = true;
        }

        StartPairingCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsRemoteControlSelectedChanged(bool value)
    {
        if (value)
        {
            IsReadOnlySelected = false;
        }
        else if (!IsReadOnlySelected)
        {
            IsReadOnlySelected = true;
        }

        StartPairingCommand.NotifyCanExecuteChanged();
    }

    partial void OnPairingSecretChanged(string value) => StartPairingCommand.NotifyCanExecuteChanged();

    partial void OnStateChanged(RemotePairingUiState value)
    {
        IsPaired = value == RemotePairingUiState.Paired;
        IsWaitingForApproval = value == RemotePairingUiState.WaitingForApproval;
        ShowPairingForm = HasDiscoveredServer && !IsPaired;
        DiscoverCommand.NotifyCanExecuteChanged();
        StartPairingCommand.NotifyCanExecuteChanged();
        ForgetCommand.NotifyCanExecuteChanged();
    }

    private async Task WaitForApprovalAsync(
        RemotePairingAttempt attempt,
        CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow() + _pollingOptions.ApprovalTimeout;
        while (_timeProvider.GetUtcNow() < deadline)
        {
            var session = await _sessionService.ClaimAsync(attempt, cancellationToken);
            if (session is not null)
            {
                PairedRole = session.Role == RemoteControlRole.ReadOnly
                    ? "Read only"
                    : "Remote control";
                ConfirmationCode = string.Empty;
                State = RemotePairingUiState.Paired;
                StatusMessage = "Pairing approved. This device is ready.";
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
            await Task.Delay(delay, _timeProvider, cancellationToken);
        }

        State = RemotePairingUiState.Error;
        StatusMessage = "Pairing approval timed out. Start a new pairing request.";
    }

    private CancellationTokenSource BeginOperation(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
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
        operation.Dispose();
        IsBusy = false;
    }

    private void ResetCandidate()
    {
        _selectedEndpoint = null;
        HasDiscoveredServer = false;
        ServerAddress = string.Empty;
        ServerInstanceId = string.Empty;
        Fingerprint = string.Empty;
        IsFingerprintConfirmed = false;
        ConfirmationCode = string.Empty;
    }

    private static string FormatFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return string.Empty;
        }

        var normalized = fingerprint.ToUpperInvariant();
        return string.Join(
            ':',
            Enumerable.Range(0, normalized.Length / 2)
                .Select(index => normalized.Substring(index * 2, 2)));
    }
}