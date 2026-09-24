// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Events;
using Moba.Backend.Interface;
using Moba.Backend.Service.Interlocking;
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
        var projected = fixture.InitialState with
        {
            Revision = 1,
            Turnouts = new Dictionary<Guid, TurnoutRuntimeState>
            {
                [fixture.Turnout.Id] = new(fixture.Turnout.Id, TurnoutLifecycle.Pending, TurnoutPosition.DivergingLeft, null)
            }
        };

        // Act
        fixture.EventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            projected,
            true,
            correlationId,
            "turnout.command.pending"));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trackPage.Revision, Is.EqualTo(projected.Revision));
            Assert.That(signalBoxPage.Revision, Is.EqualTo(projected.Revision));
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
            Assert.That(viewModel.CanOperateTurnout, Is.False);
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
    public static async Task SetTurnoutStraightCommand_TurnoutWithoutCompleteObservations_UsesSemanticRuntimeBoundary()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Runtime.SetupGet(runtime => runtime.IsSynchronized).Returns(false);
        var viewModel = fixture.CreateViewModel();
        viewModel.SelectedTurnout = viewModel.Turnouts.Single();
        Assert.That(viewModel.CanOperateTurnout, Is.True);
        fixture.Runtime
            .Setup(runtime => runtime.SetTurnoutAsync(
                fixture.Turnout.Id,
                TurnoutPosition.Straight,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnoutCoordinatorResult(
                TurnoutCoordinatorStatus.Pending,
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

    [TestCase(SelectedOperationalContext.Block)]
    [TestCase(SelectedOperationalContext.Signal)]
    public static void ReadOnlyOperationalContext_ShowsNoAuthorizedLiveAction(
        SelectedOperationalContext context)
    {
        // Arrange
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();

        // Act
        if (context == SelectedOperationalContext.Block)
            viewModel.SelectedBlock = viewModel.Blocks.Single();
        else
            viewModel.SelectedSignal = viewModel.Signals.Single();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.HasOperationalSelection, Is.True);
            Assert.That(viewModel.HasLiveActionControls, Is.False);
            Assert.That(viewModel.ShowNoAuthorizedLiveActionMessage, Is.True);
        }
    }

    [Test]
    public static void RuntimeSnapshot_DiagnosticsIncludeCorrelationTimestampAndStructuredState()
    {
        // Arrange
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();
        viewModel.StartObserving();
        viewModel.SelectedTurnout = viewModel.Turnouts.Single();
        var correlationId = Guid.NewGuid();
        var runtimeEvent = new InterlockingRuntimeSnapshotChangedEvent(
            fixture.InitialState,
            true,
            correlationId,
            "turnout.observed");

        // Act
        fixture.EventBus.Publish(runtimeEvent);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticsText, Does.Contain(correlationId.ToString("D")));
            Assert.That(viewModel.DiagnosticsText, Does.Contain(runtimeEvent.CreatedUtc.ToString("O")));
            Assert.That(viewModel.DiagnosticsText, Does.Contain("Turnout West turnout"));
            Assert.That(viewModel.DiagnosticsText, Does.Contain("Unknown"));
            Assert.That(viewModel.DiagnosticsText, Does.Contain("synchronized True"));
        }
    }

    [Test]
    public static void StopObserving_RemovesPageSubscriptionWithoutStoppingSharedRuntime()
    {
        var fixture = CreateFixture();
        var firstPage = fixture.CreateViewModel();
        var secondPage = fixture.CreateViewModel();
        firstPage.StartObserving();
        secondPage.StartObserving();
        firstPage.StopObserving();
        var updated = fixture.InitialState with { Revision = 3 };

        fixture.EventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            updated, false, Guid.NewGuid(), "block.observed"));

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(firstPage.Revision, Is.Zero);
        Assert.That(secondPage.Revision, Is.EqualTo(3));
        fixture.Runtime.Verify(runtime => runtime.DisposeAsync(), Times.Never);
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
        var initialState = new InterlockingRuntimeState
        {
            Revision = 0,
            Turnouts = new Dictionary<Guid, TurnoutRuntimeState> { [turnout.Id] = new(turnout.Id, TurnoutLifecycle.Unknown, null, null) },
            Blocks = new Dictionary<Guid, BlockRuntimeState> { [block.Id] = new(block.Id, BlockOccupancy.Unknown) },
            Signals = new Dictionary<Guid, SignalRuntimeState> { [signal.Id] = new(signal.Id, null) },
            ProcessedCorrelationIds = new HashSet<Guid>()
        };
        var runtime = new Mock<IInterlockingRuntime>();
        runtime.SetupGet(item => item.Current).Returns(initialState);
        runtime.SetupGet(item => item.IsSynchronized).Returns(true);
        runtime
            .Setup(item => item.ActivateAsync(
                It.IsAny<InterlockingDefinition>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new Fixture(
            turnout,
            trackSegmentId,
            signalBoxElementId,
            project,
            initialState,
            runtime);
    }

    private sealed record Fixture(
        TurnoutDefinition Turnout,
        Guid TrackSegmentId,
        Guid SignalBoxElementId,
        Project Project,
        InterlockingRuntimeState InitialState,
        Mock<IInterlockingRuntime> Runtime)
    {
        public EventBus EventBus { get; } = new(NullLogger<EventBus>.Instance);

        public InterlockingControlViewModel CreateViewModel(
            IProjectContext? projectContext = null,
            IUiDispatcher? uiDispatcher = null) =>
            new(
                Runtime.Object,
                EventBus,
                projectContext ?? new TestProjectContext(Project),
                uiDispatcher ?? ImmediateUiDispatcher.Instance);
    }

    private sealed partial class TestProjectContext(
        Project project,
        Func<Task>? save = null,
        SolutionSaveState saveState = SolutionSaveState.Saved,
        string saveStatusText = "Saved") : IProjectContext
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

        public SolutionSaveState SolutionSaveState => saveState;

        public string SolutionSaveStatusText => saveStatusText;

        public Task SaveSolutionInternalAsync() => save?.Invoke() ?? Task.CompletedTask;
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public static ImmediateUiDispatcher Instance { get; } = new();

        public void InvokeOnUi(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc) => asyncFunc();

        public void InvokeOnUiHighPriority(Action action) => action();

        public void InvokeOnUiLowPriority(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
        {
            _ = priority;
            return asyncAction();
        }
    }
}
