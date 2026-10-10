// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.ProjectRuntimes;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;

using Moq;

/// <summary>
/// A real <see cref="ProjectRuntimeHost"/> over fake Z21s: every Z21 connection the host opens gets its own mock,
/// in the order the connections are created.
/// </summary>
internal sealed class TestProjectRuntimeHost : IAsyncDisposable
{
    public TestProjectRuntimeHost()
    {
        Registry = new Z21ConnectionRegistry(
            () => new ForwardingEventBus(new EventBus(NullLogger<EventBus>.Instance), ApplicationBus),
            _ =>
            {
                var z21 = new Mock<IZ21>();
                Z21s.Add(z21);
                return z21.Object;
            });
        Host = new ProjectRuntimeHost(
            new ProjectRuntimeFactory(Registry, CreateServices()),
            NullLogger<ProjectRuntimeHost>.Instance);
    }

    /// <summary>The application bus that receives the events of the selected project.</summary>
    public EventBus ApplicationBus { get; } = new(NullLogger<EventBus>.Instance);

    /// <summary>One mock per Z21 connection the host created.</summary>
    public List<Mock<IZ21>> Z21s { get; } = [];

    public Z21ConnectionRegistry Registry { get; }

    public ProjectRuntimeHost Host { get; }

    /// <summary>A project with the given Z21 address; an empty address means no Z21 is assigned.</summary>
    public static Project Project(string name, string z21Address) =>
        new() { Name = name, Z21 = { IpAddress = z21Address } };

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync().ConfigureAwait(false);
        await Registry.DisposeAsync().ConfigureAwait(false);
    }

    private static ProjectRuntimeServices CreateServices() => new()
    {
        Settings = new AppSettings { Counter = { CountOfFeedbackPoints = 2, UseTimerFilter = false } },
        ActionExecutor = new Mock<IActionExecutor>().Object,
        WorkflowDependencies = new WorkflowServiceDependencies
        {
            Validator = new Mock<IWorkflowValidator>().Object,
            EffectPlanner = new Mock<IWorkflowEffectPlanner>().Object,
            TraceStore = new Mock<IWorkflowTraceStore>().Object,
            TimeProvider = TimeProvider.System
        },
        SharedExecutionContext = new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
        StopTransitionService = new JourneyStopTransitionService(),
        RuntimeStateStore = new Mock<IJourneyRuntimeStateStore>().Object,
        TimeProvider = TimeProvider.System,
        LoggerFactory = NullLoggerFactory.Instance
    };
}
