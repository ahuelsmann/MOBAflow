// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;

using Common.Configuration;
using Common.Runtime;

using Domain;

using Microsoft.Extensions.Logging;

/// <summary>
/// Polls MOBApi for queued runtime commands when SignalR host forwarding is unavailable.
/// </summary>
public sealed class RestApiRuntimeCommandConsumerService : IDisposable
{
    private readonly IMobaRuntime _mobaRuntime;
    private readonly AppSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiRuntimeCommandConsumerService> _logger;
    private readonly HostControlPlaneSession? _hostSession;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public RestApiRuntimeCommandConsumerService(
        IMobaRuntime mobaRuntime,
        AppSettings appSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<RestApiRuntimeCommandConsumerService> logger,
        HostControlPlaneSession? hostSession = null)
    {
        _mobaRuntime = mobaRuntime;
        _appSettings = appSettings;
        _httpClient = httpClientFactory.CreateClient(nameof(RestApiRuntimeCommandConsumerService));
        _logger = logger;
        _hostSession = hostSession;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        _ = ConsumeLoopAsync(_cts.Token);
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await ProcessNextCommandAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Runtime command consumer tick failed");
            }
        }
    }

    private async Task ProcessNextCommandAsync(CancellationToken cancellationToken)
    {
        if (_hostSession?.IsEnrolled != true)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/runtime/commands/pending");
        using var response = await _hostSession.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var command = await System.Text.Json.JsonSerializer.DeserializeAsync<RuntimeCommandEnvelope>(
            stream,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (command == null)
        {
            return;
        }

        await ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteAsync(RuntimeCommandEnvelope command, CancellationToken cancellationToken)
    {
        switch (command.Type)
        {
            case RuntimeCommandType.SetSignalAspect
                when command.SignalId.HasValue && command.SignalAspect.HasValue:
                await _mobaRuntime
                    .SetSignalAspectAsync(command.SignalId.Value, command.SignalAspect.Value, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.SetLocomotiveDrive
                when command.LocomotiveAddress.HasValue && command.Speed.HasValue && command.Forward.HasValue:
                await _mobaRuntime
                    .SetLocomotiveDriveAsync(
                        command.LocomotiveAddress.Value,
                        command.Speed.Value,
                        command.Forward.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.SetLocomotiveFunction
                when command.LocomotiveAddress.HasValue && command.FunctionIndex.HasValue && command.FunctionIsOn.HasValue:
                await _mobaRuntime
                    .SetLocomotiveFunctionAsync(
                        command.LocomotiveAddress.Value,
                        command.FunctionIndex.Value,
                        command.FunctionIsOn.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.ResetJourney when command.JourneyId.HasValue:
                await _mobaRuntime.ResetJourneyAsync(command.JourneyId.Value, cancellationToken).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Skipping unsupported runtime command {Type}", command.Type);
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
    }
}