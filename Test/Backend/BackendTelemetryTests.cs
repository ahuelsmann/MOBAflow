// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Test.Helpers;

using Moq;

using System.Diagnostics.Metrics;

/// <summary>
/// The Z21 pipeline and the runtime fail-safe report their failure paths end to end (RF-18): each path increments its
/// <see cref="BackendTelemetry"/> counter and keeps its structured log entry.
/// </summary>
[TestFixture]
internal sealed partial class BackendTelemetryTests : IDisposable
{
    private readonly TestRuntimes _runtimes = new();

    public void Dispose() => _runtimes.Dispose();

    [Test]
    public async Task RejectedEvent_IsCountedWithItsTypeAndLoggedAsWarning()
    {
        using var recorder = new TelemetryRecorder();
        var logger = new RecordingLogger();
        var pipeline = new Z21EventPipeline(new Mock<IEventBus>().Object, logger, capacity: 1);
        await using var pipelineLifetime = pipeline.ConfigureAwait(false);
        await pipeline.StopAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

        var accepted = pipeline.TryEnqueue(new TelemetryProbeEvent());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accepted, Is.False);
            Assert.That(recorder.Count(BackendTelemetry.RejectedZ21EventsName, nameof(TelemetryProbeEvent)), Is.EqualTo(1));
            Assert.That(logger.Entries, Has.Exactly(1).Matches<(LogLevel Level, string Message)>(
                entry => entry.Level == LogLevel.Warning
                    && entry.Message.Contains($"EventType={nameof(TelemetryProbeEvent)}", StringComparison.Ordinal)));
        }
    }

    [Test]
    public async Task FailedDispatch_IsCountedWithItsTypeAndLoggedAsError()
    {
        using var recorder = new TelemetryRecorder();
        var logger = new RecordingLogger();
        var bus = new Mock<IEventBus>();
        bus.Setup(value => value.Publish(It.IsAny<TelemetryProbeEvent>())).Throws(new InvalidOperationException("Expected test failure"));
        var pipeline = new Z21EventPipeline(bus.Object, logger, capacity: 2);
        await using var pipelineLifetime = pipeline.ConfigureAwait(false);

        Assert.That(pipeline.TryEnqueue(new TelemetryProbeEvent()), Is.True);
        await pipeline.StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recorder.Count(BackendTelemetry.FailedZ21DispatchesName, nameof(TelemetryProbeEvent)), Is.EqualTo(1));
            Assert.That(logger.Entries, Has.Exactly(1).Matches<(LogLevel Level, string Message)>(
                entry => entry.Level == LogLevel.Error && entry.Message.Contains("dispatch failed", StringComparison.Ordinal)));
        }
    }

    [Test]
    public async Task DrainTimeout_IsCountedAndLoggedAsWarning()
    {
        using var recorder = new TelemetryRecorder();
        var logger = new RecordingLogger();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bus = new Mock<IEventBus>();
        bus.Setup(value => value.Publish(It.IsAny<TelemetryProbeEvent>())).Callback(() =>
        {
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(5));
        });
        var pipeline = new Z21EventPipeline(bus.Object, logger, capacity: 2);
        await using var pipelineLifetime = pipeline.ConfigureAwait(false);
        Assert.That(pipeline.TryEnqueue(new TelemetryProbeEvent()), Is.True);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        await pipeline.StopAsync(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
        release.Set();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recorder.Count(BackendTelemetry.PipelineShutdownTimeoutsName), Is.EqualTo(1));
            Assert.That(logger.Entries, Has.Exactly(1).Matches<(LogLevel Level, string Message)>(
                entry => entry.Level == LogLevel.Warning && entry.Message.Contains("drain timed out", StringComparison.Ordinal)));
        }
    }

    [Test]
    public void UnexpectedConnectionLoss_CountsOneFailSafeTrigger()
    {
        var z21 = new Mock<IZ21>();
        using var runtime = _runtimes.Create(z21.Object);
        z21.Raise(value => value.OnConnectedChanged += null, true);
        using var recorder = new TelemetryRecorder();

        z21.Raise(value => value.OnConnectionLost += null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recorder.Count(BackendTelemetry.FailSafeTriggersName), Is.EqualTo(1));
            Assert.That(runtime.Current.IsOperatorAckRequired, Is.True);
        }
    }

    [Test]
    public void Counters_DescribeTheirUnits()
    {
        // The counters exist once the type is initialized; the listener then publishes them on start.
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(BackendTelemetry).TypeHandle);

        using var recorder = new TelemetryRecorder();

        Assert.That(recorder.Units, Is.EquivalentTo(new Dictionary<string, string?>
        {
            [BackendTelemetry.RejectedZ21EventsName] = "{event}",
            [BackendTelemetry.FailedZ21DispatchesName] = "{event}",
            [BackendTelemetry.PipelineShutdownTimeoutsName] = "{drain}",
            [BackendTelemetry.FailSafeTriggersName] = "{trigger}"
        }));
    }

    private sealed record TelemetryProbeEvent : EventBase;

    /// <summary>Records the Backend counters; the test assembly runs its tests one at a time.</summary>
    private sealed partial class TelemetryRecorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<(string Name, string? EventType, long Value)> _measurements = [];

        public TelemetryRecorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != BackendTelemetry.MeterName)
                {
                    return;
                }

                lock (_measurements)
                {
                    Units[instrument.Name] = instrument.Unit;
                }

                listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                string? eventType = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "event_type") eventType = (string?)tag.Value;
                }

                lock (_measurements)
                {
                    _measurements.Add((instrument.Name, eventType, value));
                }
            });
            _listener.Start();
        }

        public Dictionary<string, string?> Units { get; } = [];

        public long Count(string name, string? eventType = null)
        {
            lock (_measurements)
            {
                return _measurements.Where(measurement => measurement.Name == name
                        && (eventType is null || measurement.EventType == eventType))
                    .Sum(measurement => measurement.Value);
            }
        }

        public void Dispose() => _listener.Dispose();
    }

    /// <summary>Records formatted log entries; every level is enabled.</summary>
    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public (LogLevel Level, string Message)[] Entries
        {
            get
            {
                lock (_entries)
                {
                    return [.. _entries];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
