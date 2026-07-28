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
internal static partial class InterlockingControlViewModelTests
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
    public static void RepresentationSelection_UnboundElement_ClearsStaleOperationalCommands()
    {
        // Arrange
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();
        viewModel.SelectTrackRepresentation(fixture.TrackSegmentId);

        // Act
        viewModel.SelectTrackRepresentation(Guid.NewGuid());

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SelectedContext, Is.EqualTo(SelectedOperationalContext.Unbound));
            Assert.That(viewModel.SelectedTurnout, Is.Null);
            Assert.That(viewModel.SelectedRoute, Is.Null);
            Assert.That(viewModel.CanOperateTurnout, Is.False);
            Assert.That(viewModel.CanOperateRoute, Is.False);
            Assert.That(viewModel.SelectedObjectDetail, Is.EqualTo("No operational binding"));
        }
    }

    [Test]
    public static void SelectionProjection_Turnout_ExposesContextAndConfiguredPositions()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Turnout.Commands =
        [
            new TurnoutCommandMapping { Position = TurnoutPosition.Straight },
            new TurnoutCommandMapping { Position = TurnoutPosition.DivergingLeft }
        ];
        var viewModel = fixture.CreateViewModel();

        // Act
        viewModel.SelectTrackRepresentation(fixture.TrackSegmentId);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SelectedContext, Is.EqualTo(SelectedOperationalContext.Turnout));
            Assert.That(viewModel.SelectedObjectTitle, Is.EqualTo(fixture.Turnout.Name));
            Assert.That(viewModel.IsStraightActionVisible, Is.True);
            Assert.That(viewModel.IsDivergingLeftActionVisible, Is.True);
            Assert.That(viewModel.IsDivergingRightActionVisible, Is.False);
            Assert.That(viewModel.AvailabilityText, Is.EqualTo("Fault"));
            Assert.That(viewModel.DiagnosticsText, Does.Contain("Revision"));
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
    public static async Task CancelRouteCommand_SettingRoute_ConfirmsConsequenceBeforeRuntimeCommand()
    {
        // Arrange
        var fixture = CreateFixture();
        var free = fixture.Engine.ObserveBlock(
            fixture.InitialState,
            fixture.Project.Interlocking.Blocks.Single().Id,
            BlockOccupancy.Free,
            Guid.NewGuid(),
            fixture.InitialState.Revision);
        var selected = fixture.Engine.ReserveRoute(
            free.State,
            fixture.Route.Id,
            Guid.NewGuid(),
            free.State.Revision);
        var setting = fixture.Engine.BeginSetting(
            selected.State,
            fixture.Route.Id,
            Guid.NewGuid(),
            selected.State.Revision);
        fixture.Runtime.SetupGet(runtime => runtime.Current).Returns(setting.State);
        var dialog = new Mock<IDialogService>();
        dialog
            .Setup(candidate => candidate.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(true);
        fixture.Runtime
            .Setup(runtime => runtime.CancelRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.cancel.reconciliation",
                "Locks retained for reconciliation.",
                Guid.NewGuid(),
                setting.State));
        var viewModel = fixture.CreateViewModel(dialogService: dialog.Object);
        viewModel.SelectedRoute = viewModel.Routes.Single();

        // Act
        await viewModel.CancelRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        dialog.Verify(candidate => candidate.ShowConfirmationAsync(
            "Cancel route setting?",
            It.Is<string>(message =>
                message.Contains(fixture.Route.Name, StringComparison.Ordinal) &&
                message.Contains("remain locked", StringComparison.OrdinalIgnoreCase)),
            "Cancel setting",
            "Keep setting",
            true), Times.Once);
        fixture.Runtime.Verify(runtime => runtime.CancelRouteAsync(
            fixture.Route.Id,
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public static async Task CancelRouteCommand_SelectedRoute_DoesNotRequestConfirmation()
    {
        // Arrange
        var fixture = CreateFixture();
        var free = fixture.Engine.ObserveBlock(
            fixture.InitialState,
            fixture.Project.Interlocking.Blocks.Single().Id,
            BlockOccupancy.Free,
            Guid.NewGuid(),
            fixture.InitialState.Revision);
        var selected = fixture.Engine.ReserveRoute(
            free.State,
            fixture.Route.Id,
            Guid.NewGuid(),
            free.State.Revision);
        fixture.Runtime.SetupGet(runtime => runtime.Current).Returns(selected.State);
        fixture.Runtime
            .Setup(runtime => runtime.CancelRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.cancelled",
                "Route cancelled before hardware dispatch.",
                Guid.NewGuid(),
                selected.State));
        var dialog = new Mock<IDialogService>();
        var viewModel = fixture.CreateViewModel(dialogService: dialog.Object);
        viewModel.SelectedRoute = viewModel.Routes.Single();

        // Act
        await viewModel.CancelRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        dialog.Verify(candidate => candidate.ShowConfirmationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
        fixture.Runtime.Verify(runtime => runtime.CancelRouteAsync(
            fixture.Route.Id,
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public static async Task ReconcileRouteCommand_AlwaysConfirmsBeforeRuntimeCommand()
    {
        // Arrange
        var fixture = CreateFixture();
        var failedState = CreateRouteState(
            fixture,
            RouteLifecycle.Failed,
            BlockOccupancy.Unknown,
            "route.feedback.timeout");
        fixture.Runtime.SetupGet(runtime => runtime.Current).Returns(failedState);
        var dialog = new Mock<IDialogService>();
        dialog
            .Setup(candidate => candidate.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(false);
        var viewModel = fixture.CreateViewModel(dialogService: dialog.Object);
        viewModel.SelectedRoute = viewModel.Routes.Single();

        // Act
        await viewModel.ReconcileRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        dialog.Verify(candidate => candidate.ShowConfirmationAsync(
            "Reconcile route?",
            It.Is<string>(message => message.Contains("release retained locks", StringComparison.OrdinalIgnoreCase)),
            "Reconcile",
            "Keep locked",
            true), Times.Once);
        fixture.Runtime.Verify(runtime => runtime.ReconcileRouteAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public static async Task ReleaseRouteCommand_VerifiedClearRoute_DoesNotRequestConfirmation()
    {
        // Arrange
        var fixture = CreateFixture();
        var establishedState = CreateRouteState(
            fixture,
            RouteLifecycle.Established,
            BlockOccupancy.Free);
        fixture.Runtime.SetupGet(runtime => runtime.Current).Returns(establishedState);
        fixture.Runtime
            .Setup(runtime => runtime.ReleaseRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.available",
                "Route resources released.",
                Guid.NewGuid(),
                establishedState));
        var dialog = new Mock<IDialogService>();
        var viewModel = fixture.CreateViewModel(dialogService: dialog.Object);
        viewModel.SelectedRoute = viewModel.Routes.Single();

        // Act
        await viewModel.ReleaseRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CanReleaseRoute, Is.True);
            Assert.That(viewModel.ReleaseRouteCommand.CanExecute(null), Is.True);
        }
        dialog.Verify(candidate => candidate.ShowConfirmationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
        fixture.Runtime.Verify(runtime => runtime.ReleaseRouteAsync(
            fixture.Route.Id,
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public static async Task SafeStopRouteCommand_UnsafeReleaseState_IsAvailableWithoutConfirmation()
    {
        // Arrange
        var fixture = CreateFixture();
        var occupiedState = CreateRouteState(
            fixture,
            RouteLifecycle.Occupied,
            BlockOccupancy.Occupied);
        fixture.Runtime.SetupGet(runtime => runtime.Current).Returns(occupiedState);
        fixture.Runtime
            .Setup(runtime => runtime.SafeStopRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.safe-stopped",
                "Protected signals are safe and locks remain retained.",
                Guid.NewGuid(),
                occupiedState));
        var dialog = new Mock<IDialogService>();
        var viewModel = fixture.CreateViewModel(dialogService: dialog.Object);
        viewModel.SelectedRoute = viewModel.Routes.Single();

        // Act
        await viewModel.SafeStopRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CanReleaseRoute, Is.False);
            Assert.That(viewModel.ReleaseRouteCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.PrimaryRouteActionLabel, Is.EqualTo("Safe stop"));
        }
        dialog.Verify(candidate => candidate.ShowConfirmationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
        fixture.Runtime.Verify(runtime => runtime.SafeStopRouteAsync(
            fixture.Route.Id,
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public static async Task ValidRouteDefinitionChange_PersistenceFailure_RetainsAuthoritativeDefinitionAsNotSaved()
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
        await viewModel.WhenDefinitionSaveIdleAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Project.Interlocking.Routes, Has.Count.EqualTo(originalRouteCount + 1));
            Assert.That(viewModel.DefinitionSaveState, Is.EqualTo(DefinitionSaveState.NotSaved));
            Assert.That(viewModel.DefinitionSaveStatusText, Does.Contain("Not saved"));
            fixture.Runtime.Verify(runtime => runtime.ActivateAsync(
                It.IsAny<InterlockingDefinition>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public static async Task InvalidRouteFieldChange_RetainsLastValidDefinitionWithoutAnotherSave()
    {
        // Arrange
        var fixture = CreateFixture();
        var saveCount = 0;
        var projectContext = new TestProjectContext(
            fixture.Project,
            () =>
            {
                saveCount++;
                return Task.CompletedTask;
            });
        var validator = new Mock<IInterlockingDefinitionValidator>();
        validator
            .Setup(candidate => candidate.Validate(fixture.Project))
            .Returns(() =>
            {
                var draft = fixture.Project.Interlocking.Routes
                    .FirstOrDefault(route => route.Id != fixture.Route.Id);
                return string.IsNullOrWhiteSpace(draft?.Name)
                    ? new InterlockingValidationReport(
                    [
                        new InterlockingValidationFinding(
                            "route.name.missing",
                            InterlockingValidationSeverity.Error,
                            draft?.Id ?? Guid.Empty,
                            [],
                            "Every route requires a name.")
                    ])
                    : new InterlockingValidationReport([]);
            });
        var viewModel = fixture.CreateViewModel(projectContext, validator.Object);
        viewModel.BeginRouteDraftCommand.Execute(null);
        await viewModel.WhenDefinitionSaveIdleAsync().ConfigureAwait(false);
        var acceptedRoute = fixture.Project.Interlocking.Routes
            .Single(route => route.Id != fixture.Route.Id);
        var savesAfterAcceptedDefinition = saveCount;

        // Act
        viewModel.DraftName = string.Empty;
        await viewModel.WhenDefinitionSaveIdleAsync().ConfigureAwait(false);
        viewModel.SelectTrackRepresentation(fixture.TrackSegmentId);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(acceptedRoute.Name, Is.EqualTo("New route"));
            Assert.That(saveCount, Is.EqualTo(savesAfterAcceptedDefinition));
            Assert.That(viewModel.DefinitionSaveState, Is.EqualTo(DefinitionSaveState.ValidationError));
            Assert.That(viewModel.DefinitionSaveStatusText, Does.StartWith("Not saved"));
            Assert.That(viewModel.SelectedRoute, Is.Null);
        }
    }

    [Test]
    public static async Task LiveRouteCommand_DoesNotStartDefinitionAutosave()
    {
        // Arrange
        var fixture = CreateFixture();
        var saveCount = 0;
        var projectContext = new TestProjectContext(
            fixture.Project,
            () =>
            {
                saveCount++;
                return Task.CompletedTask;
            });
        var viewModel = fixture.CreateViewModel(projectContext);
        viewModel.SelectedRoute = viewModel.Routes.Single();
        fixture.Runtime
            .Setup(runtime => runtime.PreviewRouteAsync(
                fixture.Route.Id,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteCoordinatorResult(
                RouteCoordinatorStatus.Accepted,
                "route.preview.available",
                "Route is available.",
                Guid.NewGuid(),
                fixture.InitialState));

        // Act
        await viewModel.PreviewRouteCommand.ExecuteAsync(null).ConfigureAwait(false);

        // Assert
        Assert.That(saveCount, Is.Zero);
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
        runtime
            .Setup(item => item.ActivateAsync(
                It.IsAny<InterlockingDefinition>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new Fixture(
            turnout,
            route,
            trackSegmentId,
            signalBoxElementId,
            project,
            engine,
            runtime);
    }

    private static InterlockingRuntimeState CreateRouteState(
        Fixture fixture,
        RouteLifecycle lifecycle,
        BlockOccupancy occupancy,
        string? failureCode = null) =>
        new()
        {
            Revision = fixture.InitialState.Revision + 1,
            Turnouts = fixture.InitialState.Turnouts,
            Blocks = fixture.InitialState.Blocks.Values
                .Select(block => block with { Occupancy = occupancy })
                .ToDictionary(block => block.BlockId),
            Signals = fixture.InitialState.Signals,
            Routes = fixture.InitialState.Routes.Values.Select(route =>
                route.RouteId == fixture.Route.Id
                    ? route with { Lifecycle = lifecycle, FailureCode = failureCode }
                    : route)
                .ToDictionary(route => route.RouteId),
            ProcessedCorrelationIds = fixture.InitialState.ProcessedCorrelationIds
        };

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
            IInterlockingDefinitionValidator? validator = null,
            IDialogService? dialogService = null) =>
            new(
                Runtime.Object,
                EventBus,
                projectContext ?? new TestProjectContext(Project),
                validator ?? new InterlockingDefinitionValidator(),
                dialogService ?? new Mock<IDialogService>().Object,
                NullLogger<InterlockingControlViewModel>.Instance);
    }

    private sealed partial class TestProjectContext(Project project, Func<Task>? save = null) : IProjectContext
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
