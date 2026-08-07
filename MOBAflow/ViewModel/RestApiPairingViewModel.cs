// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using Moba.Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media;

using Service;

/// <summary>
/// Owns the local administrator pairing workflow displayed under Settings / REST API.
/// </summary>
public sealed partial class RestApiPairingViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly IRestApiPairingHost _pairingHost;
    private readonly IRestApiQrCodeImageFactory _qrCodeImageFactory;
    private readonly ILogger<RestApiPairingViewModel> _logger;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _pollingCancellation;
    private DateTimeOffset _expiresAt;
    private string? _pendingRequestId;
    private bool _disposed;

    [ObservableProperty]
    private string confirmationCode = string.Empty;

    [ObservableProperty]
    private string expirationText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isInvitationVisible;

    [ObservableProperty]
    private bool isPendingRequestVisible;

    [ObservableProperty]
    private string pendingDeviceName = string.Empty;

    [ObservableProperty]
    private ImageSource? qrCodeImage;

    [ObservableProperty]
    private string statusMessage = "Start the REST API to create a MOBAsmart pairing QR code.";

    public RestApiPairingViewModel(
        IRestApiPairingHost pairingHost,
        IRestApiQrCodeImageFactory qrCodeImageFactory,
        ILogger<RestApiPairingViewModel> logger,
        TimeProvider? timeProvider = null)
    {
        _pairingHost = pairingHost ?? throw new ArgumentNullException(nameof(pairingHost));
        _qrCodeImageFactory = qrCodeImageFactory ?? throw new ArgumentNullException(nameof(qrCodeImageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    [RelayCommand]
    private async Task OpenPairingAsync(CancellationToken cancellationToken)
    {
        CancelPolling();
        ResetVisibleInvitation();
        IsBusy = true;
        try
        {
            var invitation = await _pairingHost
                .OpenAdminPairingAsync(cancellationToken)
                .ConfigureAwait(true);
            QrCodeImage = await _qrCodeImageFactory
                .CreateAsync(invitation.EncodedQrPayload, cancellationToken)
                .ConfigureAwait(true);
            _expiresAt = invitation.Invitation.ExpiresAt;
            IsInvitationVisible = true;
            StatusMessage = "Scan this QR code with MOBAsmart. Administrator access is granted only after local approval.";
            UpdateExpirationText();
            StartPolling();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Pairing was cancelled.";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not open the MOBAsmart pairing window");
            StatusMessage = "Pairing is unavailable. Start the REST API and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApproveAsync(CancellationToken cancellationToken)
    {
        var requestId = _pendingRequestId;
        if (requestId is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _pairingHost.ApproveAsync(requestId, cancellationToken).ConfigureAwait(true);
            CancelPolling();
            ResetVisibleInvitation();
            StatusMessage = "MOBAsmart was approved and will connect automatically.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not approve the MOBAsmart pairing request");
            StatusMessage = "The pairing request could not be approved. Try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RejectAsync(CancellationToken cancellationToken)
    {
        var requestId = _pendingRequestId;
        if (requestId is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _pairingHost.RejectAsync(requestId, cancellationToken).ConfigureAwait(true);
            CancelPolling();
            ResetVisibleInvitation();
            StatusMessage = "The MOBAsmart pairing request was rejected.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not reject the MOBAsmart pairing request");
            StatusMessage = "The pairing request could not be rejected. Try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelPairingAsync(CancellationToken cancellationToken)
    {
        CancelPolling();
        IsBusy = true;
        try
        {
            await _pairingHost.CancelAsync(cancellationToken).ConfigureAwait(true);
            ResetVisibleInvitation();
            StatusMessage = "Pairing was cancelled.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not cancel the MOBAsmart pairing window");
            StatusMessage = "The pairing window could not be cancelled. Try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelPolling();
    }

    private void StartPolling()
    {
        _pollingCancellation = new CancellationTokenSource();
        PollPendingRequestsAsync(_pollingCancellation.Token).Observe(
            exception => _logger.LogWarning(exception, "REST API pairing request polling failed."));
    }

    private async Task PollPendingRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_timeProvider.GetUtcNow() >= _expiresAt)
                {
                    ResetVisibleInvitation();
                    StatusMessage = "The pairing QR code expired. Create a new code to try again.";
                    return;
                }

                UpdateExpirationText();
                var requests = await _pairingHost
                    .GetPendingRequestsAsync(cancellationToken)
                    .ConfigureAwait(true);
                var request = requests.FirstOrDefault();
                if (request is not null)
                {
                    _pendingRequestId = request.RequestId;
                    PendingDeviceName = request.DisplayName;
                    ConfirmationCode = request.ConfirmationCode;
                    IsPendingRequestVisible = true;
                    StatusMessage = "Confirm the code on both devices, then approve this administrator.";
                }

                await Task.Delay(PollInterval, _timeProvider, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the owner approves, rejects, cancels, or replaces the pairing window.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not refresh pending MOBAsmart pairing requests");
            StatusMessage = "Pending pairing requests could not be refreshed.";
        }
    }

    private void UpdateExpirationText()
    {
        var remaining = _expiresAt - _timeProvider.GetUtcNow();
        var seconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        ExpirationText = $"QR code expires in {seconds} seconds";
    }

    private void CancelPolling()
    {
        _pollingCancellation?.Cancel();
        _pollingCancellation?.Dispose();
        _pollingCancellation = null;
    }

    private void ResetVisibleInvitation()
    {
        QrCodeImage = null;
        IsInvitationVisible = false;
        IsPendingRequestVisible = false;
        ExpirationText = string.Empty;
        PendingDeviceName = string.Empty;
        ConfirmationCode = string.Empty;
        _pendingRequestId = null;
    }
}