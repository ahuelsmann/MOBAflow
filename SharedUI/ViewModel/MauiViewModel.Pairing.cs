// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.



namespace Moba.SharedUI.ViewModel;



using Common.Configuration;

using Common.Security;



using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;



using Microsoft.Extensions.Logging;



/// <summary>

/// QR pairing flow for MOBAsmart ↔ MOBAflow.

/// </summary>

public sealed partial class MauiViewModel

{

    private const string PairingKeyRequiredMessage = "Enter the pairing key from MOBAflow.";

    private const string PairingDiscoveryFailedMessage =

        "MOBAflow was not found on the network. Scan the QR code or check the IP address.";

    private const string PairingInvalidDataMessage = "Invalid pairing data.";

    private const string PairingConnectionFailedMessage =

        "Connection failed. Scan the QR code again or restart MOBApi in MOBAflow.";

    private const string PairingSuccessfulMessage = "Pairing successful.";

    private const string PairingFailedMessage = "Pairing failed.";



    [ObservableProperty]

    private bool _isPairingBusy;



    [ObservableProperty]

    private bool _hasPairingError;



    [ObservableProperty]

    private string _pairingStatusText = string.Empty;



    public void ClearPairingStatus()

    {

        HasPairingError = false;

        PairingStatusText = string.Empty;

    }



    public async Task<bool> TryApplyPairingPayloadAsync(MobaPairingPayload payload)

    {

        ArgumentNullException.ThrowIfNull(payload);

        return await ApplyPairingAsync(payload.Host, payload.Port, payload.ApiKey).ConfigureAwait(false);

    }



    [RelayCommand]

    private async Task ApplyManualPairingAsync()

    {

        if (string.IsNullOrWhiteSpace(RestApiApiKey))

        {

            PairingStatusText = PairingKeyRequiredMessage;

            HasPairingError = true;

            return;

        }



        IsPairingBusy = true;

        ClearPairingStatus();

        try

        {

            var (ip, port) = await _restDiscoveryService

                .DiscoverServerFastAsync(Z21IpAddress, _applicationLifetimeCts.Token)

                .ConfigureAwait(false);



            if (string.IsNullOrWhiteSpace(ip) || port is null or <= 0)

            {

                PairingStatusText = PairingDiscoveryFailedMessage;

                HasPairingError = true;

                return;

            }



            await ApplyPairingAsync(ip, port.Value, RestApiApiKey).ConfigureAwait(false);

        }

        finally

        {

            IsPairingBusy = false;

        }

    }



    private async Task<bool> ApplyPairingAsync(string host, int port, string apiKey)

    {

        if (string.IsNullOrWhiteSpace(host) || port <= 0 || string.IsNullOrWhiteSpace(apiKey))

        {

            PairingStatusText = PairingInvalidDataMessage;

            HasPairingError = true;

            return false;

        }



        IsPairingBusy = true;

        ClearPairingStatus();

        try

        {

            var trimmedHost = host.Trim();

            var trimmedKey = apiKey.Trim();



            RestApiIpAddress = trimmedHost;

            RestApiPort = port;

            RestApiApiKey = trimmedKey;



            _settings.RestApi.CurrentIpAddress = trimmedHost;

            _settings.RestApi.Port = port;

            _settings.RestApi.ApiKey = trimmedKey;

            RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, trimmedHost);



            if (!_isApplyingLoadedSettings)

            {

                QueueSaveSettings();

            }



            _mobaflowConnectCts?.Cancel();

            _mobaflowConnectCts?.Dispose();

            _mobaflowConnectCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetimeCts.Token);



            _suppressMobaflowAutoConnect = true;

            try

            {

                if (!IsMobaflowConnectionEnabled)

                {

                    IsMobaflowConnectionEnabled = true;

                }

                else

                {

                    _settings.RestApi.IsConnectionEnabled = true;

                }

            }

            finally

            {

                _suppressMobaflowAutoConnect = false;

            }



            var connected = await ConnectAfterPairingAsync(_mobaflowConnectCts.Token).ConfigureAwait(false);

            if (!connected)

            {

                PairingStatusText = PairingConnectionFailedMessage;

                HasPairingError = true;

                return false;

            }



            PairingStatusText = PairingSuccessfulMessage;

            HasPairingError = false;

            return true;

        }

        catch (Exception ex)

        {

            _logger.LogWarning(ex, "MOBAflow pairing failed");

            PairingStatusText = PairingFailedMessage;

            HasPairingError = true;

            return false;

        }

        finally

        {

            IsPairingBusy = false;

        }

    }

}


