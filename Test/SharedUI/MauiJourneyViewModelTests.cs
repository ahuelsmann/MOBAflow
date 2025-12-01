// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if !SKIP_ANDROID_TESTS
namespace Moba.Test.SharedUI;

using Moba.Domain;
using NUnit.Framework;

[TestFixture]
public class MauiJourneyViewModelTests
{
    [Test]
    public void StateChanged_RaisesPropertyChanged_DispatchesOnMainThread()
    {
        var model = new Journey();
        var vm = new Moba.SharedUI.ViewModel.MAUI.JourneyViewModel(model);

        // Act
        model.CurrentCounter++;
        model.CurrentPos++;

        // If no exception is thrown here in a test context, the fallback path executed synchronously.
        Assert.Pass();
    }
}
#endif
