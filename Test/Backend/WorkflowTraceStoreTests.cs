// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Common.Events;

[TestFixture]
internal sealed class WorkflowTraceStoreTests
{
    [Test]
    public void Append_ExecutionLimit_EvictsOldestCompletedExecutionBeforeActiveExecution()
    {
        var store = new WorkflowTraceStore(maximumExecutions: 2, maximumEntries: 100);
        var active = Guid.NewGuid();
        var completed = Guid.NewGuid();
        var newest = Guid.NewGuid();

        store.Append(CreateEvent(active, WorkflowLifecycleKind.WorkflowStarted, 1));
        store.Append(CreateEvent(completed, WorkflowLifecycleKind.WorkflowStarted, 2));
        store.Append(CreateEvent(completed, WorkflowLifecycleKind.WorkflowCompleted, 3));
        store.Append(CreateEvent(newest, WorkflowLifecycleKind.WorkflowStarted, 4));

        Assert.Multiple(() =>
        {
            Assert.That(store.GetEntries().Any(entry => entry.ExecutionId == active), Is.True);
            Assert.That(store.GetEntries().Any(entry => entry.ExecutionId == completed), Is.False);
            Assert.That(store.GetEntries().Any(entry => entry.ExecutionId == newest), Is.True);
        });
    }

    [Test]
    public void Append_EntryLimit_KeepsNewestEntriesInAppendOrder()
    {
        var store = new WorkflowTraceStore(maximumExecutions: 100, maximumEntries: 2);
        var executionId = Guid.NewGuid();

        store.Append(CreateEvent(executionId, WorkflowLifecycleKind.WorkflowStarted, 1));
        store.Append(CreateEvent(executionId, WorkflowLifecycleKind.StepStarted, 2));
        store.Append(CreateEvent(executionId, WorkflowLifecycleKind.StepCompleted, 3));

        Assert.That(store.GetEntries().Select(entry => entry.Sequence), Is.EqualTo(new long[] { 2, 3 }));
    }

    private static WorkflowLifecycleEvent CreateEvent(
        Guid executionId,
        WorkflowLifecycleKind kind,
        long sequence) =>
        new()
        {
            Kind = kind,
            SourceCorrelationId = Guid.NewGuid(),
            ExecutionId = executionId,
            WorkflowId = Guid.NewGuid(),
            Sequence = sequence,
            Mode = WorkflowLifecycleMode.Live,
            TimestampUtc = DateTimeOffset.UtcNow
        };
}
