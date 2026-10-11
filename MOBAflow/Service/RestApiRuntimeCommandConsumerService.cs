// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Runtime;

using Microsoft.Extensions.Logging;

/// <summary>
/// Polls MOBApi for queued runtime commands when SignalR host forwarding is unavailable.
/// </summary>
public sealed class RestApiRuntimeCommandConsumerService : IDisposable
{
    private static readonly Action<ILogger, RuntimeCommandType, Guid, Exception?> LogUnknownProject =
        LoggerMessage.Define<RuntimeCommandType, Guid>(
            LogLevel.Debug,
            new EventId(1, nameof(LogUnknownProject)),
            "Skipping runtime command {Type} for unknown project {ProjectId}");

    private readonly ProjectRuntimeCommandRouter _router;
    private readonly ILogger<RestApiRuntimeCommandConsumerService> _logger;
    private readonly LocalMobApiClient _mobApiClient;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public RestApiRuntimeCommandConsumerService(
        ProjectRuntimeCommandRouter router,
        ILogger<RestApiRuntimeCommandConsumerService> logger,
        LocalMobApiClient mobApiClient)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
        _mobApiClient = mobApiClient ?? throw new ArgumentNullException(nameof(mobApiClient));
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
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/runtime/commands/pending");
        using var response = await _mobApiClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

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
        // Every project has its own runtime; a command for a project that is no longer loaded is dropped.
        var gateway = _router.ForProject(command.ProjectId);
        if (gateway is null)
        {
            LogUnknownProject(_logger, command.Type, command.ProjectId, null);
            return;
        }

        switch (command.Type)
        {
            case RuntimeCommandType.SetSignalAspect
                when command.SignalId.HasValue && command.SignalAspect.HasValue:
                await gateway
                    .SetSignalAspectAsync(command.SignalId.Value, command.SignalAspect.Value, cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.SetLocomotiveDrive
                when command.LocomotiveAddress.HasValue && command.Speed.HasValue && command.Forward.HasValue:
                await gateway
                    .SetLocomotiveDriveAsync(
                        command.LocomotiveAddress.Value,
                        command.Speed.Value,
                        command.Forward.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.SetLocomotiveFunction
                when command.LocomotiveAddress.HasValue && command.FunctionIndex.HasValue && command.FunctionIsOn.HasValue:
                await gateway
                    .SetLocomotiveFunctionAsync(
                        command.LocomotiveAddress.Value,
                        command.FunctionIndex.Value,
                        command.FunctionIsOn.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case RuntimeCommandType.ResetJourney when command.JourneyId.HasValue:
                await gateway.ResetJourneyAsync(command.JourneyId.Value, cancellationToken).ConfigureAwait(false);
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
