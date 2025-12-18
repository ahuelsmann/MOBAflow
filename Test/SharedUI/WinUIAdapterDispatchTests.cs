// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.SharedUI.Interface;
using Moq;

[TestFixture]
public class WinUIAdapterDispatchTests
{
    private class TestUiDispatcher : IUiDispatcher
    {
        private readonly ManualResetEventSlim _dispatchedEvent = new(false);
        
        public bool Dispatched { get; private set; }
        public ManualResetEventSlim DispatchedEvent => _dispatchedEvent;
        
        public void InvokeOnUi(Action action)
        {
            Dispatched = true; // Track that dispatch was called
            action(); // Execute the action
            _dispatchedEvent.Set(); // Signal that dispatch happened
        }
    }

    // Test helper: JourneyManager subclass that allows triggering StationChanged
    private class TestableJourneyManager : JourneyManager
    {
        public TestableJourneyManager(IZ21 z21, Project project, WorkflowService workflowService)
            : base(z21, project, workflowService)
        {
        }

        public void TriggerStationChanged(StationChangedEventArgs e)
        {
            OnStationChanged(e);
        }
    }

    [Test]
    public void StationChanged_UsesDispatch()
    {
        // Arrange
        var journey = new Journey { Id = Guid.NewGuid(), FirstPos = 0 };
        var project = new Project { Journeys = new List<Journey> { journey } };
        var state = new JourneySessionState { JourneyId = journey.Id };
        var dispatcher = new TestUiDispatcher();
        
        // Create a testable JourneyManager
        var z21Mock = new Mock<IZ21>();
        var actionExecutor = new ActionExecutor();
        var workflowService = new WorkflowService(actionExecutor);
        var journeyManager = new TestableJourneyManager(z21Mock.Object, project, workflowService);

        // Act - Trigger StationChanged event
        journeyManager.TriggerStationChanged(new StationChangedEventArgs
        {
            JourneyId = journey.Id,
            Station = new Station { Name = "Test" },
            SessionState = state
        });

        // Wait for the PropertyChanged event to be processed (max 1 second)
        bool signaled = dispatcher.DispatchedEvent.Wait(TimeSpan.FromSeconds(1));
        
        // Assert
        Assert.That(signaled, Is.True, "Dispatch was not called within timeout");
        Assert.That(dispatcher.Dispatched, Is.True);
    }
}
