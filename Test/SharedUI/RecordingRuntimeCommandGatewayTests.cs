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
        await gateway.SimulateFeedbackAsync(12);
        await gateway.ResetJourneyAsync(journeyId);
        await gateway.SetSignalAspectAsync(signalId, Enum.GetValues<SignalAspect>()[0]);
        await gateway.SetLocomotiveDriveAsync(3, 42, true);
        await gateway.SetLocomotiveFunctionAsync(3, 5, true);
        await gateway.SendTurnoutCommandAsync(17, 2, true, queue: true);
        var artifact = (await session.StopAsync()).Artifact!;
        var recordedCommands = artifact.Entries
            .Where(entry => entry.Source == "runtime-command-gateway")
            .ToArray();

        var expectedCommandKeys = new[]
        {
            "command.track-power",
            "command.simulate-feedback",
            "command.journey-reset",
            "command.signal-aspect",
            "command.locomotive-drive",
            "command.locomotive-function",
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
                .Where(entry => entry.TypeKey.StartsWith(commandKey, StringComparison.Ordinal))
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

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await gateway.SetLocomotiveDriveAsync(3, 20, true));
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

        Assert.Multiple(() =>
        {
            Assert.That(inner.ExecutionCount, Is.EqualTo(1));
            Assert.That(session.ReadEntries(0, 10), Is.Empty);
        });
    }

    private sealed class StubRuntimeCommandGateway : IRuntimeCommandGateway
    {
        public int ExecutionCount { get; private set; }

        public Exception? Failure { get; init; }

        public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) => ExecuteAsync();

        public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) => ExecuteAsync();

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

        public Task SendTurnoutCommandAsync(
            int decoderAddress,
            int output,
            bool activate,
            bool queue = false,
            CancellationToken cancellationToken = default) => ExecuteAsync();

        private Task ExecuteAsync()
        {
            ExecutionCount++;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}