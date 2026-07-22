// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend;
using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;

internal sealed class RecordingSessionServiceTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Session_Should_RecordLifecycleAnnotationsAndPauseInterval()
    {
        var clock = new MutableTimeProvider(StartTime);
        await using var service = new RecordingSessionService(clock);

        var started = service.Start(new RecordingSessionStartRequest("Morning run", "1.2.3"));
        clock.UtcNow = StartTime.AddSeconds(2);
        var paused = service.Pause();
        var ignored = service.TryRecord(CreateProjection(1));
        var marker = service.AddMarker("Reached yard");
        clock.UtcNow = StartTime.AddSeconds(7);
        var resumed = service.Resume();
        var note = service.AddNote("Proceed slowly");
        var accepted = service.TryRecord(CreateProjection(2));
        clock.UtcNow = StartTime.AddSeconds(10);

        var stopped = await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(started.Succeeded, Is.True);
            Assert.That(paused.Succeeded, Is.True);
            Assert.That(ignored, Is.EqualTo(RecordingSubmissionResult.IgnoredWhilePaused));
            Assert.That(marker.Succeeded, Is.True);
            Assert.That(resumed.Succeeded, Is.True);
            Assert.That(note.Succeeded, Is.True);
            Assert.That(accepted, Is.EqualTo(RecordingSubmissionResult.Accepted));
            Assert.That(stopped.Operation.Succeeded, Is.True);
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingSessionState.Completed));
            Assert.That(stopped.Artifact, Is.Not.Null);
        });

        var artifact = stopped.Artifact!;
        Assert.That(
            artifact.Entries.Select(entry => entry.TypeKey),
            Is.EqualTo(new[]
            {
                "recorder.started",
                "recorder.paused",
                "recorder.marker",
                "recorder.resumed",
                "recorder.note",
                "test.event",
                "recorder.completed"
            }));
        Assert.That(artifact.Entries.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
        Assert.That(artifact.Entries.Single(entry => entry.TypeKey == "recorder.resumed")
            .Payload.GetProperty("durationTicks").GetInt64(), Is.EqualTo(TimeSpan.FromSeconds(5).Ticks));
        Assert.Multiple(() =>
        {
            Assert.That(artifact.Summary.MarkerCount, Is.EqualTo(1));
            Assert.That(artifact.Summary.NoteCount, Is.EqualTo(1));
            Assert.That(artifact.Metadata.CompletedUtc, Is.EqualTo(StartTime.AddSeconds(10)));
        });
    }

    [Test]
    public async Task Controls_Should_ReturnStructuredFailuresAndIdempotentTargetStates()
    {
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime));

        var pauseWithoutSession = service.Pause();
        var invalidStart = service.Start(new RecordingSessionStartRequest(" ", "1.0"));
        var started = service.Start(new RecordingSessionStartRequest("Session", "1.0"));
        var duplicateStart = service.Start(new RecordingSessionStartRequest("Other", "1.0"));
        var resumeWhileRecording = service.Resume();
        var firstPause = service.Pause();
        var secondPause = service.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(pauseWithoutSession.FailureCode, Is.EqualTo(RecordingFailureCode.InvalidState));
            Assert.That(invalidStart.FailureCode, Is.EqualTo(RecordingFailureCode.InvalidRequest));
            Assert.That(started.Succeeded, Is.True);
            Assert.That(duplicateStart.FailureCode, Is.EqualTo(RecordingFailureCode.InvalidState));
            Assert.That(resumeWhileRecording.IsIdempotent, Is.True);
            Assert.That(firstPause.Succeeded, Is.True);
            Assert.That(secondPause.IsIdempotent, Is.True);
        });
    }

    [Test]
    public async Task ConcurrentProducers_Should_AssignOneOrderedSequenceAtIdenticalTimestamps()
    {
        const int producerCount = 1_000;
        var options = new RecorderOptions(pendingCapacity: 2_000, entryLimit: 5_000);
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime), options);
        service.Start(new RecordingSessionStartRequest("Concurrent", "1.0"));
        var outcomes = new ConcurrentBag<RecordingSubmissionResult>();

        Parallel.For(0, producerCount, index => outcomes.Add(service.TryRecord(CreateProjection(index))));
        var stopped = await service.StopAsync();

        Assert.That(outcomes, Is.All.EqualTo(RecordingSubmissionResult.Accepted));
        var recorded = stopped.Artifact!.Entries.Where(entry => entry.TypeKey == "test.event").ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(recorded, Has.Length.EqualTo(producerCount));
            Assert.That(recorded.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(recorded.Select(entry => entry.TimestampUtc).Distinct().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Saturation_Should_NotBlockProducersAndShouldEmitGapRecord()
    {
        const int attemptCount = 20_000;
        var options = new RecorderOptions(pendingCapacity: 1, entryLimit: 100_000);
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime), options);
        service.Start(new RecordingSessionStartRequest("Saturation", "1.0"));
        var outcomes = new RecordingSubmissionResult[attemptCount];

        Parallel.For(0, attemptCount, index => outcomes[index] = service.TryRecord(CreateProjection(index)));
        var stopped = await service.StopAsync();

        Assert.That(outcomes, Does.Contain(RecordingSubmissionResult.DroppedCapacity));
        Assert.That(stopped.Artifact!.Entries, Has.Some.Matches<RecordingEntry>(entry => entry.TypeKey == "recorder.gap"));
        Assert.That(stopped.Artifact.Entries.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
        Assert.That(service.CurrentStatus.DroppedEntryCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task EntryLimit_Should_StopNormalAcceptanceAndPreserveTerminalRecords()
    {
        var options = new RecorderOptions(pendingCapacity: 8, entryLimit: 6);
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime), options);
        service.Start(new RecordingSessionStartRequest("Limited", "1.0"));

        var first = service.TryRecord(CreateProjection(1));
        var second = service.TryRecord(CreateProjection(2));
        var rejected = service.TryRecord(CreateProjection(3));
        var rejectedAgain = service.TryRecord(CreateProjection(4));
        var stopped = await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(RecordingSubmissionResult.Accepted));
            Assert.That(second, Is.EqualTo(RecordingSubmissionResult.Accepted));
            Assert.That(rejected, Is.EqualTo(RecordingSubmissionResult.RejectedLimit));
            Assert.That(rejectedAgain, Is.EqualTo(RecordingSubmissionResult.RejectedLimit));
            Assert.That(stopped.Artifact!.Entries, Has.Some.Matches<RecordingEntry>(entry => entry.TypeKey == "recorder.limit"));
            Assert.That(stopped.Artifact.Entries[^1].TypeKey, Is.EqualTo("recorder.completed"));
            Assert.That(stopped.Artifact.Entries.Length, Is.LessThanOrEqualTo(options.EntryLimit));
        });
    }

    [Test]
    public async Task CancelledStop_Should_ProduceFaultedArtifactWithoutThrowing()
    {
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime));
        service.Start(new RecordingSessionStartRequest("Cancelled", "1.0"));
        service.TryRecord(CreateProjection(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var stopped = await service.StopAsync(cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(stopped.Operation.FailureCode, Is.EqualTo(RecordingFailureCode.Cancelled));
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingSessionState.Faulted));
            Assert.That(stopped.Artifact, Is.Not.Null);
            Assert.That(stopped.Artifact!.Entries, Has.Some.Matches<RecordingEntry>(entry => entry.TypeKey == "recorder.fault"));
            Assert.That(stopped.Artifact.Entries[^1].TypeKey, Is.EqualTo("recorder.completed"));
        });
    }

    [Test]
    public async Task ZeroDrainTimeout_Should_ProduceTerminalTimeoutFault()
    {
        var options = new RecorderOptions(stopDrainTimeout: TimeSpan.Zero);
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime), options);
        service.Start(new RecordingSessionStartRequest("Timeout", "1.0"));
        service.TryRecord(CreateProjection(1));

        var stopped = await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.Operation.FailureCode, Is.EqualTo(RecordingFailureCode.DrainTimeout));
            Assert.That(service.CurrentStatus.State, Is.EqualTo(RecordingSessionState.Faulted));
            Assert.That(stopped.Artifact!.Entries.Single(entry => entry.TypeKey == "recorder.fault")
                .Payload.GetProperty("failureCode").GetString(), Is.EqualTo(nameof(RecordingFailureCode.DrainTimeout)));
        });
    }

    [Test]
    public async Task Stop_Should_BeSingleShotAndReturnTheSameArtifact()
    {
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime));
        service.Start(new RecordingSessionStartRequest("Single stop", "1.0"));
        service.TryRecord(CreateProjection(1));

        var firstStop = service.StopAsync();
        var secondStop = service.StopAsync();
        var firstResult = await firstStop;
        var secondResult = await secondStop;
        var completedResult = await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(secondStop, Is.SameAs(firstStop));
            Assert.That(secondResult.Artifact, Is.SameAs(firstResult.Artifact));
            Assert.That(completedResult.Artifact, Is.SameAs(firstResult.Artifact));
            Assert.That(completedResult.Operation.IsIdempotent, Is.True);
        });
    }

    [Test]
    public async Task PayloadLimit_Should_EmitLimitBeforeOversizedSessionEntry()
    {
        var projection = CreateProjection(12345);
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { value = 12345 }).Length;
        var options = new RecorderOptions(estimatedPayloadByteLimit: payloadBytes - 1);
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime), options);
        service.Start(new RecordingSessionStartRequest("Payload limit", "1.0"));

        var submission = service.TryRecord(projection);
        var stopped = await service.StopAsync();

        Assert.Multiple(() =>
        {
            Assert.That(submission, Is.EqualTo(RecordingSubmissionResult.RejectedLimit));
            Assert.That(stopped.Artifact!.Entries, Has.None.Matches<RecordingEntry>(entry => entry.TypeKey == "test.event"));
            Assert.That(stopped.Artifact.Entries, Has.Some.Matches<RecordingEntry>(entry => entry.TypeKey == "recorder.limit"));
        });
    }

    [Test]
    public async Task Import_Should_CreateReadOnlyCompletedSessionAndAllowLaterReplacement()
    {
        await using var source = new RecordingSessionService(new MutableTimeProvider(StartTime));
        source.Start(new RecordingSessionStartRequest("Source", "1.0"));
        var artifact = (await source.StopAsync()).Artifact!;
        await using var target = new RecordingSessionService(new MutableTimeProvider(StartTime));

        var imported = target.Import(artifact);
        var annotation = target.AddNote("Not allowed");
        var restarted = target.Start(new RecordingSessionStartRequest("Replacement", "1.1"));

        Assert.Multiple(() =>
        {
            Assert.That(imported.Succeeded, Is.True);
            Assert.That(annotation.FailureCode, Is.EqualTo(RecordingFailureCode.InvalidState));
            Assert.That(restarted.Succeeded, Is.True);
            Assert.That(target.CurrentStatus.SessionName, Is.EqualTo("Replacement"));
        });
    }

    [Test]
    public async Task ReadEntries_Should_ReturnBoundedOrderedJournalPages()
    {
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime));
        service.Start(new RecordingSessionStartRequest("Paged", "1.0"));
        service.AddMarker("First");
        service.AddNote("Second");

        var firstPage = service.ReadEntries(0, 2);
        var secondPage = service.ReadEntries(firstPage[^1].Sequence, 2);

        Assert.Multiple(() =>
        {
            Assert.That(firstPage, Has.Count.EqualTo(2));
            Assert.That(firstPage.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(secondPage, Has.Count.EqualTo(1));
            Assert.That(secondPage[0].TypeKey, Is.EqualTo("recorder.note"));
            Assert.That(secondPage[0].Sequence, Is.GreaterThan(firstPage[^1].Sequence));
        });
    }

    [Test]
    public async Task ReadEntries_Should_RejectInvalidBounds()
    {
        await using var service = new RecordingSessionService(new MutableTimeProvider(StartTime));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ReadEntries(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ReadEntries(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ReadEntries(0, 10_001));
        });
    }

    [Test]
    public async Task BackendRegistration_Should_ResolveOneServiceAsSessionAndStatusContracts()
    {
        var services = new ServiceCollection();
        services.AddMobaBackendServices();
        await using var provider = services.BuildServiceProvider();

        var sessionService = provider.GetRequiredService<IRecordingSessionService>();
        var statusSource = provider.GetRequiredService<IRecordingStatusSource>();

        Assert.That(statusSource, Is.SameAs(sessionService));
    }

    private static RecordingEntryProjection CreateProjection(int value) =>
        new(
            "test",
            "unit-test",
            "test.event",
            "information",
            null,
            null,
            JsonSerializer.SerializeToElement(new { value }),
            $"Event {value}",
            RecordingReplayApplicability.ReplayApplicable);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}