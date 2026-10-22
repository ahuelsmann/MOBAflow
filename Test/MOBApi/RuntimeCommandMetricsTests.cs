// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.Extensions.Logging;

using Moba.Contracts.Runtime;
using Moba.MOBApi.Service;

using Moq;

using System.Diagnostics.Metrics;

/// <summary>
/// Rejected remote commands are visible: every rejection is counted with its reason and command, and an overloaded
/// queue is logged as a warning (RF-18 telemetry).
/// </summary>
[TestFixture]
internal sealed partial class RuntimeCommandMetricsTests
{
    private static readonly Guid Project = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Test]
    public void FullQueue_IsCountedAsQueueFullAndLoggedAsWarning()
    {
        using var recorder = new RejectionRecorder();
        var logger = new Mock<ILogger<RuntimeCommandAdmission>>();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var admission = TestCommandAdmission.Create(new RuntimeCommandQueue(capacity: 1), KnownProject(), logger.Object);

        admission.Enqueue(Drive(3));
        admission.Enqueue(Drive(4));

        Assert.That(recorder.Rejections, Is.EqualTo(new[] { ("queue_full", "SetLocomotiveDrive") }));
        logger.Verify(
            value => value.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => $"{state}".Contains("queue is full", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void InvalidCommand_IsCountedOnceAsInvalid_OnBothAdmissionPaths()
    {
        using var recorder = new RejectionRecorder();
        var admission = TestCommandAdmission.Create(new RuntimeCommandQueue(capacity: 4), KnownProject());

        admission.Validate(Drive(0));
        admission.Enqueue(Drive(0));

        Assert.That(recorder.Rejections, Is.EqualTo(new[]
        {
            ("invalid", "SetLocomotiveDrive"),
            ("invalid", "SetLocomotiveDrive")
        }));
    }

    [Test]
    public void AcceptedCommands_AreNotCounted()
    {
        using var recorder = new RejectionRecorder();
        var admission = TestCommandAdmission.Create(new RuntimeCommandQueue(capacity: 4), KnownProject());

        admission.Validate(Drive(3));
        admission.Enqueue(Drive(3));

        Assert.That(recorder.Rejections, Is.Empty);
    }

    private static RuntimeCommandEnvelope Drive(int address) => new()
    {
        ProjectId = Project,
        Type = RuntimeCommandType.SetLocomotiveDrive,
        LocomotiveAddress = address,
        Speed = 10,
        Forward = true
    };

    private static ISolutionCache KnownProject()
    {
        var solutionCache = new Mock<ISolutionCache>();
        solutionCache.Setup(cache => cache.ContainsProject(Project)).Returns(true);
        return solutionCache.Object;
    }

    /// <summary>Listens to the MOBApi rejection counter; the test assembly runs its tests one at a time.</summary>
    private sealed partial class RejectionRecorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<(string Reason, string Command)> _rejections = [];

        public RejectionRecorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == RuntimeCommandAdmission.MeterName && instrument.Name == RuntimeCommandAdmission.RejectedCommandsName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                var reason = string.Empty;
                var command = string.Empty;
                foreach (var tag in tags)
                {
                    if (tag.Key == "reason") reason = (string)tag.Value!;
                    if (tag.Key == "command") command = (string)tag.Value!;
                }

                for (var count = 0; count < value; count++)
                {
                    _rejections.Add((reason, command));
                }
            });
            _listener.Start();
        }

        public IReadOnlyList<(string Reason, string Command)> Rejections => _rejections;

        public void Dispose() => _listener.Dispose();
    }
}
