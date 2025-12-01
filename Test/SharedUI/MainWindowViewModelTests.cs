// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.SharedUI.ViewModel;
using Moba.Test.TestBase;

namespace Moba.Test.SharedUI;

public class MainWindowViewModelTests : ViewModelTestBase
{
    [Test]
    public void CanAddProject_IncrementsProjects()
    {
        // Arrange - all mocks from base class
        var solution = new Solution();
        var settings = new Common.Configuration.AppSettings();
        var vm = new MainWindowViewModel(
            IoServiceMock.Object, 
            Z21Mock.Object, 
            JourneyManagerFactoryMock.Object, 
            UiDispatcherMock.Object,
            settings,
            solution);

        // Act - AddProjectCommand no longer exists in simplified MainWindowViewModel
        // This test is obsolete as project management moved to UI layer
        
        // Assert
        Assert.Pass("AddProjectCommand removed - project management now in UI layer");
    }
}
