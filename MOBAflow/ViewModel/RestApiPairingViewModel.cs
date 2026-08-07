// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using Moba.Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media;

using Service;
using System.Text.Json;

/// <summary>
/// Owns the local administrator pairing workflow displayed under Settings / REST API.
/// </summary>
internal sealed partial class RestApiPairingViewModel(
    IRestApiPairingHost pairingHost,
    IRestApiQrCodeImageFactory qrCodeImageFactory,
    ILogger<RestApiPairingViewModel> logger,
    TimeProvider? timeProvider = null) : ObservableObject, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly IRestApiPairingHost _pairingHost =
        pairingHost ?? throw new ArgumentNullException(nameof(pairingHost));
    private readonly IRestApiQrCodeImageFactory _qrCodeImageFactory =
        qrCodeImageFactory ?? throw new ArgumentNullException(nameof(qrCodeImageFactory));
    private readonly ILogger<RestApiPairingViewModel> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private CancellationTokenSource? _pollingCancellation;
    private DateTimeOffset _expiresAt;
    private string? _pendingRequestId;
    private bool _disposed;

    [ObservableProperty]
    public partial string ConfirmationCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExpirationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsInvitationVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPendingRequestVisible { get; set; }

    [ObservableProperty]
    public partial string PendingDeviceName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? QrCodeImage { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } =
        "Start the REST API to create a MOBAsmart pairing QR code.";

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
        catch (Exception exception) when (IsExpectedPairingFailure(exception))
        {
            LogOpenPairingFailed(_logger, exception);
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
            LogApprovePairingFailed(_logger, exception);
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
            LogRejectPairingFailed(_logger, exception);
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
            LogCancelPairingFailed(_logger, exception);
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
            exception => LogPairingPollingFailed(_logger, exception));
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
                var request = requests.Count > 0 ? requests[0] : null;
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
        catch (Exception exception) when (IsExpectedPairingFailure(exception))
        {
            LogRefreshPendingRequestsFailed(_logger, exception);
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

    private static bool IsExpectedPairingFailure(Exception exception) =>
        exception is HttpRequestException or InvalidOperationException or InvalidDataException or
        IOException or JsonException or NotSupportedException or ArgumentException;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not open the MOBAsmart pairing window")]
    private static partial void LogOpenPairingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not approve the MOBAsmart pairing request")]
    private static partial void LogApprovePairingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not reject the MOBAsmart pairing request")]
    private static partial void LogRejectPairingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not cancel the MOBAsmart pairing window")]
    private static partial void LogCancelPairingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "REST API pairing request polling failed")]
    private static partial void LogPairingPollingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not refresh pending MOBAsmart pairing requests")]
    private static partial void LogRefreshPendingRequestsFailed(ILogger logger, Exception exception);
}
