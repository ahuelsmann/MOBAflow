using Moba.SharedUI.ViewModel;
using Moba.Backend.Model;
using Moba.Test.TestBase;

namespace Moba.Test.SharedUI;

public class MainWindowViewModelTests : ViewModelTestBase
{
    [Test]
    public void CanAddProject_IncrementsProjects()
    {
        // Arrange - all mocks from base class
        var solution = new Solution(); // Create empty solution for DI injection
        var vm = new MainWindowViewModel(
            IoServiceMock.Object, 
            Z21Mock.Object, 
            JourneyManagerFactoryMock.Object, 
            TreeViewBuilder,
            solution);

        // Act
        vm.AddProjectCommand.Execute(null);

        // Assert
        Assert.That(vm.Solution.Projects.Count, Is.EqualTo(1));
    }
}
