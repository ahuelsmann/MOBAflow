// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Events;
using Moba.Backend.Interface;
using Moba.Backend.Service.Interlocking;
using Moba.Backend.Service.Validation;
using Moba.Common.Events;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

[TestFixture]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names use the repository's Method_State_Result convention.")]
internal static class InterlockingControlViewModelTests
{
    [Test]
    public static void RuntimeSnapshotChanged_TwoPageViewModels_ProjectSameRevisionAndTurnoutState()
    {
        // Arrange
        var fixture = CreateFixture();
        var trackPage = fixture.CreateViewModel();
        var signalBoxPage = fixture.CreateViewModel();
        trackPage.StartObserving();
        signalBoxPage.StartObserving();
        var correlationId = Guid.NewGuid();
        var projected = InterlockingSafetyEngine.ProjectTurnoutCommand(
            fixture.InitialState,
            fixture.Turnout.Id,
            TurnoutLifecycle.Pending,
            TurnoutPosition.DivergingLeft,
            correlationId,
            fixture.InitialState.Revision);

        // Act
        fixture.EventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            projected.State,
            true,
            correlationId,
            projected.Code));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trackPage.Revision, Is.EqualTo(projected.State.Revision));
            Assert.That(signalBoxPage.Revision, Is.EqualTo(projected.State.Revision));
            Assert.That(trackPage.Turnouts.Single().State, Is.EqualTo("Pending"));
            Assert.That(signalBoxPage.Turnouts.Single().State, Is.EqualTo("Pending"));
            Assert.That(trackPage.Turnouts.Single().Detail, Does.Contain("DivergingLeft"));
            Assert.That(signalBoxPage.Turnouts.Single().Detail, Does.Contain("DivergingLeft"));
        }
    }

    [Test]
    public static void RepresentationSelection_PhysicalAndLogicalBindingsResolveSameOperationalTurnout()
    {
        // Arrange
        var fixture = CreateFixture();
        var trackPage = fixture.CreateViewModel();
        var signalBoxPage = fixture.CreateViewModel();

        // Act
        trackPage.SelectTrackRepresentation(fixture.TrackSegmentId);
        signalBoxPage.SelectSignalBoxRepresentation(fixture.SignalBoxElementId);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trackPage.SelectedTurnout?.Id, Is.EqualTo(fixture.Turnout.Id));
            Assert.That(signalBoxPage.SelectedTurnout?.Id, Is.EqualTo(fixture.Turnout.Id));
            Assert.That(trackPage.GetTrackVisualState(fixture.TrackSegmentId)?.AccessibleState, Does.Contain(fixture.Turnout.Name));
            Assert.That(signalBoxPage.GetSignalBoxVisualState(fixture.SignalBoxElementId)?.AccessibleState, Does.Contain(fixture.Turnout.Name));
        }
    }

    [Test]
    public static async Task SetTurnoutStraightCommand_SynchronizedUnlockedTurnout_UsesSemanticRuntimeBoundary()
    {
        // Arrange
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();
        viewModel.SelectedTurnout = viewModel.Turnouts.Single();
        fixture.Runtime
            .Setup(runtime => runtime.SetTurnoutAsync(
                fixture.Turnout.Id,
                TurnoutPosition.Straight,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnoutCoordinatorResult(
                RouteCoordinatorStatus.Pending,
                "turnout.command.pending",
                "Turnout command is awaiting confirmation.",
                Guid.NewGuid(),
                fixture.InitialState));

        // Act
        await viewModel.SetTurnoutStraightCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            fixture.Runtime.Verify(runtime => runtime.SetTurnoutAsync(
                fixture.Turnout.Id,
                TurnoutPosition.Straight,
                It.Is<Guid>(id => id != Guid.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(viewModel.StatusCode, Is.EqualTo("turnout.command.pending"));
            Assert.That(viewModel.StatusText, Is.EqualTo("Turnout command is awaiting confirmation."));
        }
    }

    [Test]
    public static void LockedTurnout_CannotOperateAndIncludesNonColorLockDescription()
    {
        // Arrange
        var fixture = CreateFixture();
        var observed = fixture.Engine.ObserveBlock(
            fixture.InitialState,
            fixture.Project.Interlocking.Blocks.Single().Id,
            BlockOccupancy.Free,
            Guid.NewGuid(),
            fixture.InitialState.Revision);
        var selected = fixture.Engine.ReserveRoute(
            observed.State,
            fixture.Route.Id,
            Guid.NewGuid(),
            observed.State.Revision);
        var setting = fixture.Engine.BeginSetting(
            selected.State,
            fixture.Route.Id,
            Guid.NewGuid(),
            selected.State.Revision);
        var viewModel = fixture.CreateViewModel();
        viewModel.StartObserving();

        // Act
        fixture.EventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            setting.State,
            true,
            Guid.NewGuid(),
            setting.Code));
        viewModel.SelectedTurnout = viewModel.Turnouts.Single();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CanOperateTurnout, Is.False);
            Assert.That(viewModel.SelectedTurnout.IsLocked, Is.True);
            Assert.That(viewModel.SelectedTurnout.AccessibleState, Does.Contain("route locked"));
        }
    }

    [Test]
    public static async Task SelectRouteCommand_SynchronizedRoute_UsesSharedRuntimeBoundary()
    {
        // Arrange
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();
        viewModel.SelectedRoute = viewModel.Routes.Single();
        fixture.Runtime
            .Setup(runtime => runtime.SelectRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.reserved",
                "Route resources reserved atomically.",
                Guid.NewGuid(),
                fixture.InitialState));

        // Act
        await viewModel.SelectRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        fixture.Runtime.Verify(runtime => runtime.SelectRouteAsync(
            fixture.Route.Id,
            It.Is<Guid>(id => id != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(viewModel.StatusCode, Is.EqualTo("route.reserved"));
    }

    [Test]
    public static async Task SaveRouteDraftCommand_PersistenceFailure_RollsBackModelBeforeRuntimeActivation()
    {
        // Arrange
        var fixture = CreateFixture();
        var projectContext = new TestProjectContext(
            fixture.Project,
            () => Task.FromException(new IOException("Injected save failure.")));
        var validator = new Mock<IInterlockingDefinitionValidator>();
        validator
            .Setup(candidate => candidate.Validate(fixture.Project))
            .Returns(new InterlockingValidationReport([]));
        var viewModel = fixture.CreateViewModel(projectContext, validator.Object);
        var originalRouteCount = fixture.Project.Interlocking.Routes.Count;
        viewModel.BeginRouteDraftCommand.Execute(null);
        viewModel.DraftName = "East arrival";
        viewModel.SelectedOperationalElement = viewModel.OperationalElements.Single(item => item.Kind == "Signal");
        viewModel.SetDraftEntryCommand.Execute(null);
        viewModel.SelectedOperationalElement = viewModel.OperationalElements.Single(item => item.Kind == "Block");
        viewModel.SetDraftExitCommand.Execute(null);

        // Act
        await viewModel.SaveRouteDraftCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Project.Interlocking.Routes, Has.Count.EqualTo(originalRouteCount));
            Assert.That(viewModel.StatusCode, Is.EqualTo("route.draft.save-failed"));
            fixture.Runtime.Verify(runtime => runtime.ActivateAsync(
                It.IsAny<InterlockingDefinition>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    private static Fixture CreateFixture()
    {
        var turnout = new TurnoutDefinition
        {
            Name = "West turnout",
            DecoderAddress = 10
        };
        var block = new BlockDefinition
        {
            Name = "West block"
        };
        var signal = new SignalDefinition
        {
            Name = "Entry signal",
            BaseAddress = 20
        };
        var route = new RouteDefinition
        {
            Name = "West arrival",
            EntryElementId = signal.Id,
            ExitElementId = block.Id,
            TurnoutRequirements =
            [
                new RouteTurnoutRequirement
                {
                    TurnoutId = turnout.Id,
                    Position = TurnoutPosition.Straight
                }
            ],
            ProtectedBlockIds = [block.Id]
        };
        var trackSegmentId = Guid.NewGuid();
        var signalBoxElementId = Guid.NewGuid();
        var project = new Project
        {
            Name = "Test layout",
            Interlocking = new InterlockingDefinition
            {
                Turnouts = [turnout],
                Blocks = [block],
                Signals = [signal],
                Routes = [route],
                Bindings =
                [
                    new OperationalBinding
                    {
                        OperationalId = turnout.Id,
                        TrackSegmentIds = [trackSegmentId],
                        SignalBoxElementIds = [signalBoxElementId]
                    }
                ]
            }
        };
        var engine = new InterlockingSafetyEngine(project.Interlocking);
        var runtime = new Mock<IInterlockingRuntime>();
        runtime.SetupGet(item => item.Current).Returns(engine.InitialState);
        runtime.SetupGet(item => item.IsSynchronized).Returns(true);
        return new Fixture(
            turnout,
            route,
            trackSegmentId,
            signalBoxElementId,
            project,
            engine,
            runtime);
    }

    private sealed record Fixture(
        TurnoutDefinition Turnout,
        RouteDefinition Route,
        Guid TrackSegmentId,
        Guid SignalBoxElementId,
        Project Project,
        InterlockingSafetyEngine Engine,
        Mock<IInterlockingRuntime> Runtime)
    {
        public InterlockingRuntimeState InitialState => Engine.InitialState;

        public EventBus EventBus { get; } = new(NullLogger<EventBus>.Instance);

        public InterlockingControlViewModel CreateViewModel(
            IProjectContext? projectContext = null,
            IInterlockingDefinitionValidator? validator = null) =>
            new(
                Runtime.Object,
                EventBus,
                projectContext ?? new TestProjectContext(Project),
                validator ?? new InterlockingDefinitionValidator(),
                NullLogger<InterlockingControlViewModel>.Instance);
    }

    private sealed class TestProjectContext(Project project, Func<Task>? save = null) : IProjectContext
    {
        private ProjectViewModel? _selectedProject = new(project);
        private JourneyViewModel? _selectedJourney;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ProjectViewModel? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (ReferenceEquals(_selectedProject, value))
                    return;

                _selectedProject = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedProject)));
            }
        }

        public JourneyViewModel? SelectedJourney
        {
            get => _selectedJourney;
            set
            {
                if (ReferenceEquals(_selectedJourney, value))
                    return;

                _selectedJourney = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedJourney)));
            }
        }

        public SolutionViewModel? SolutionViewModel => null;

        public Task SaveSolutionInternalAsync() => save?.Invoke() ?? Task.CompletedTask;
    }
}