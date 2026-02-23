// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Service;

[TestFixture]
internal class WinuiJourneyViewModelTests
{
    [Test]
    public void SessionState_CanBeModified()
    {
        var journey = new Journey { Id = Guid.NewGuid() };
        var state = new JourneySessionState { JourneyId = journey.Id };

        // Act
        state.Counter++;
        state.CurrentPos++;

        // Assert
        Assert.That(state.Counter, Is.EqualTo(1));
        Assert.That(state.CurrentPos, Is.EqualTo(1));
    }
}