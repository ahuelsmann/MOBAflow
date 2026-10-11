// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend;
using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using global::Moba.Domain;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Service;

using Microsoft.Extensions.DependencyInjection;

internal sealed class RecordingRuntimeCommandGatewayTests
{
    [Test]
    public async Task Commands_Should_RecordCorrelatedRequestsAndResultsWithStableTypeKeys()
    {
        await using var session = new RecordingSessionService(TimeProvider.System);
        session.Start(new RecordingSessionStartRequest("Commands", "1.0"));
        var inner = new StubRuntimeCommandGateway();
        var gateway = new RecordingRuntimeCommandGateway(inner, session);
        var journeyId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await gateway.SetTrackPowerAsync(true);
        await gateway.AcknowledgeFailSafeAsync().ConfigureAwait(false);
        await gateway.SimulateFeedbackAsync(12);
        await gateway.ResetJourneyAsync(journeyId);
        await gateway.ResetInPortCountersAsync();
        await gateway.SetInPortCounterAsync(2, ulong.MaxValue);
        await gateway.ResetInPortCounterAsync(2);
        await gateway.SetSignalAspectAsync(signalId, Enum.GetValues<SignalAspect>()[0]);
        await gateway.SetLocomotiveDriveAsync(3, 42, true);
        await gateway.SetLocomotiveFunctionAsync(3, 5, true);
        await gateway.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);
        await gateway.SendTurnoutCommandAsync(17, 2, true, queue: true);
        var artifact = (await session.StopAsync()).Artifact!;
        var recordedCommands = artifact.Entries
            .Where(entry => entry.Source == "runtime-command-gateway")
            .ToArray();

        var expectedCommandKeys = new[]
        {
            "command.track-power",
            "command.fail-safe-acknowledge",
            "command.simulate-feedback",
            "command.journey-reset",
            "command.inport-counters-reset",
            "command.inport-counter-set",
            "command.inport-counter-reset",
            "command.signal-aspect",
            "command.locomotive-drive",
            "command.locomotive-function",
            "command.locomotive-functions-off",
            "command.turnout"
        };
        Assert.Multiple(() =>
        {
            Assert.That(inner.ExecutionCount, Is.EqualTo(expectedCommandKeys.Length));
            Assert.That(recordedCommands, Has.Length.EqualTo(expectedCommandKeys.Length * 2));
            Assert.That(recordedCommands.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(
                recordedCommands.Where(entry => entry.TypeKey.EndsWith(".request", StringComparison.Ordinal)),
                Has.All.Property(nameof(RecordingEntry.ReplayApplicability))
                    .EqualTo(RecordingReplayApplicability.ReplayApplicable));
        });

        foreach (var commandKey in expectedCommandKeys)
        {
            var commandEntries = recordedCommands
                .Where(entry => entry.TypeKey.StartsWith(commandKey + ".", StringComparison.Ordinal))
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(commandEntries.Select(entry => entry.TypeKey),
                    Is.EqualTo(new[] { $"{commandKey}.request", $"{commandKey}.result" }));
                Assert.That(commandEntries[0].CorrelationId, Is.Not.Null);
                Assert.That(commandEntries[0].CorrelationId, Is.EqualTo(commandEntries[1].CorrelationId));
                Assert.That(commandEntries[1].ReplayApplicability, Is.EqualTo(RecordingReplayApplicability.DisplayOnly));
            });
        }

        var services = new ServiceCollection();
        services.AddMobaBackendServices();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<RecordingArtifactSerializer>();
        var imported = serializer.Import(serializer.SerializeToUtf8(artifact));
        Assert.That(imported.IsValid, Is.True, () => string.Join("; ", imported.Errors.Select(error => error.Message)));
        var importedRequests = imported.Artifact!.Entries
            .Where(entry => entry.Source == "runtime-command-gateway" && entry.TypeKey.EndsWith(".request", StringComparison.Ordinal))
            .ToArray();
        Assert.That(importedRequests, Has.Length.EqualTo(expectedCommandKeys.Length));
        var isolatedRuntime = new IsolatedReplayRuntime();
        // Replay persisted entries: a request type without a payload validator is imported as display-only.
        foreach (var request in importedRequests)
        {
            Assert.That(isolatedRuntime.Apply(request).Succeeded, Is.True, request.TypeKey);
        }
    }

    [Test]
    public async Task LocomotiveInfoQuery_RecordsCorrelatedDisplayOnlyEntriesThatSurviveImport()
    {
        var session = new RecordingSessionService(TimeProvider.System);
        await using var sessionLifetime = session.ConfigureAwait(false);
        session.Start(new RecordingSessionStartRequest("Query", "1.0"));
        var inner = new StubRuntimeCommandGateway();
        var gateway = new RecordingRuntimeCommandGateway(inner, session);

        await gateway.RequestLocomotiveInfoAsync(7).ConfigureAwait(false);
        var artifact = (await session.StopAsync().ConfigureAwait(false)).Artifact!;
        var services = new ServiceCollection();
        services.AddMobaBackendServices();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<RecordingArtifactSerializer>();
        var imported = serializer.Import(serializer.SerializeToUtf8(artifact));

        Assert.That(imported.IsValid, Is.True);
        var entries = imported.Artifact!.Entries.Where(entry => entry.Source == "runtime-command-gateway").ToArray();
        Assert.That(entries, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inner.ExecutionCount, Is.EqualTo(1));
            Assert.That(entries[0].TypeKey, Is.EqualTo("command.locomotive-info.request"));
            Assert.That(entries[0].Payload.GetProperty("address").GetInt32(), Is.EqualTo(7));
            Assert.That(entries[1].TypeKey, Is.EqualTo("command.locomotive-info.result"));
            Assert.That(entries[0].CorrelationId, Is.EqualTo(entries[1].CorrelationId));
            Assert.That(entries, Has.All.Property(nameof(RecordingEntry.ReplayApplicability))
                .EqualTo(RecordingReplayApplicability.DisplayOnly));
            Assert.That(new IsolatedReplayRuntime().Apply(entries[0]).Succeeded, Is.False);
        }
    }

    [TestCase("command.fail-safe-acknowledge", false)]
    [TestCase("command.locomotive-functions-off", false)]
    [TestCase("command.locomotive-info", false)]
    [TestCase("command.fail-safe-acknowledge", true)]
    [TestCase("command.locomotive-functions-off", true)]
    [TestCase("command.locomotive-info", true)]
    public async Task PreviouslyMissingCommand_RecordsSanitizedFailureOrCancellation(string commandKey, bool cancelled)
    {
        const string privateDetail = "private command failure detail";
        using var cancellation = new CancellationTokenSource();
        if (cancelled)
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }

        var session = new RecordingSessionService(TimeProvider.System);
        await using var sessionLifetime = session.ConfigureAwait(false);
        session.Start(new RecordingSessionStartRequest("Outcome", "1.0"));
        var inner = new StubRuntimeCommandGateway
        {
            Failure = cancelled ? new OperationCanceledException(cancellation.Token) : new InvalidOperationException(privateDetail)
        };
        var gateway = new RecordingRuntimeCommandGateway(inner, session);
        var exception = await Assert.CatchAsync<Exception>(() => commandKey switch
        {
            "command.fail-safe-acknowledge" => gateway.AcknowledgeFailSafeAsync(cancellation.Token),
            "command.locomotive-functions-off" => gateway.SetAllLocomotiveFunctionsOffAsync(7, cancellation.Token),
            _ => gateway.RequestLocomotiveInfoAsync(7, cancellation.Token)
        }).ConfigureAwait(false);
        var entries = (await session.StopAsync().ConfigureAwait(false)).Artifact!.Entries
            .Where(entry => entry.Source == "runtime-command-gateway").ToArray();
        Assert.That(entries, Has.Length.EqualTo(2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.SameAs(inner.Failure));
            Assert.That(inner.LastCancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(entries[0].TypeKey, Is.EqualTo(commandKey + ".request"));
            Assert.That(entries[1].TypeKey, Is.EqualTo(commandKey + ".failure"));
            Assert.That(entries[0].CorrelationId, Is.EqualTo(entries[1].CorrelationId));
            Assert.That(entries[1].Payload.GetProperty("outcome").GetString(), Is.EqualTo(cancelled ? "cancelled" : "failed"));
            Assert.That(entries[1].Payload.GetRawText(), Does.Not.Contain(privateDetail));
            Assert.That(entries[1].ReplayApplicability, Is.EqualTo(RecordingReplayApplicability.DisplayOnly));
        }
    }

    [Test]
    public async Task Failure_Should_RecordOnlySanitizedOutcomeAndRethrowOriginalException()
    {
        const string secret = "token=super-secret";
        const string path = "C:\\private\\credential.json";
        const string endpoint = "https://internal.example.test:9443";
        await using var session = new RecordingSessionService(TimeProvider.System);
        session.Start(new RecordingSessionStartRequest("Failure", "1.0"));
        var inner = new StubRuntimeCommandGateway
        {
            Failure = new InvalidOperationException($"{secret} {path} {endpoint}")
        };
        var gateway = new RecordingRuntimeCommandGateway(inner, session);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await gateway.SetLocomotiveDriveAsync(3, 20, true)).ConfigureAwait(false);
        var artifact = (await session.StopAsync()).Artifact!;
        var commandEntries = artifact.Entries
            .Where(entry => entry.Source == "runtime-command-gateway")
            .ToArray();
        var persistedText = string.Join(
            "|",
            artifact.Entries.Select(entry => $"{entry.TypeKey}|{entry.DisplayText}|{entry.Payload.GetRawText()}"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(secret));
            Assert.That(commandEntries.Select(entry => entry.TypeKey), Is.EqualTo(new[]
            {
                "command.locomotive-drive.request",
                "command.locomotive-drive.failure"
            }));
            Assert.That(commandEntries[0].CorrelationId, Is.EqualTo(commandEntries[1].CorrelationId));
            Assert.That(commandEntries[1].Payload.GetProperty("outcome").GetString(), Is.EqualTo("failed"));
            Assert.That(persistedText, Does.Not.Contain(secret));
            Assert.That(persistedText, Does.Not.Contain(path));
            Assert.That(persistedText, Does.Not.Contain(endpoint));
            Assert.That(persistedText, Does.Not.Contain(nameof(InvalidOperationException)));
        });
    }

    [Test]
    public async Task CommandWithoutActiveRecording_Should_ExecuteWithoutCreatingJournalEntries()
    {
        await using var session = new RecordingSessionService(TimeProvider.System);
        var inner = new StubRuntimeCommandGateway();
        var gateway = new RecordingRuntimeCommandGateway(inner, session);

        await gateway.SetTrackPowerAsync(false);
        await gateway.AcknowledgeFailSafeAsync().ConfigureAwait(false);
        await gateway.SetAllLocomotiveFunctionsOffAsync(7).ConfigureAwait(false);
        await gateway.RequestLocomotiveInfoAsync(7).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(inner.ExecutionCount, Is.EqualTo(4));
            Assert.That(session.ReadEntries(0, 10), Is.Empty);
        });
    }

    private sealed class StubRuntimeCommandGateway : IRuntimeCommandGateway
    {
        public int ExecutionCount { get; private set; }

        public Exception? Failure { get; init; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default) => ExecuteWithCancellationAsync(cancellationToken);

        public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task ResetInPortCountersAsync(CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SetInPortCounterAsync(uint inPort, ulong value, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task ResetInPortCounterAsync(uint inPort, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SetSignalAspectAsync(
            Guid signalId,
            SignalAspect aspect,
            CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SetLocomotiveDriveAsync(
            int address,
            int speed,
            bool forward,
            CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SetLocomotiveFunctionAsync(
            int address,
            int functionIndex,
            bool isOn,
            CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SetAllLocomotiveFunctionsOffAsync(int address, CancellationToken cancellationToken = default) =>
            ExecuteWithCancellationAsync(cancellationToken);

        public Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default) => ExecuteWithCancellationAsync(cancellationToken);

        public Task SendTurnoutCommandAsync(
            int decoderAddress,
            int output,
            bool activate,
            bool queue = false,
            CancellationToken cancellationToken = default) => ExecuteAsync();

        private Task ExecuteWithCancellationAsync(CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return ExecuteAsync();
        }

        private Task ExecuteAsync()
        {
            ExecutionCount++;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
