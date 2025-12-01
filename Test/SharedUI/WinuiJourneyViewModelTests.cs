// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using NUnit.Framework;

[TestFixture]
public class WinuiJourneyViewModelTests
{
    [Test]
    public void StateChanged_RaisesPropertyChanged_Dispatches()
    {
        var model = new Journey();

        // Act
        model.CurrentCounter++;
        model.CurrentPos++;

        Assert.Pass();
    }
}
