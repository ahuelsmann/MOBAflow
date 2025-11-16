using Moba.SharedUI.ViewModel;
using Moba.SharedUI.Service;
using Moq;

namespace Moba.Test.SharedUI;

public class MainWindowViewModelTests
{
    [Test]
    public void CanAddProject_IncrementsProjects()
    {
        var ioMock = new Mock<IIoService>();
        var vm = new MainWindowViewModel(ioMock.Object);

        vm.Solution = new Moba.Backend.Model.Solution();
        vm.AddProjectCommand.Execute(null);

        Assert.That(vm.Solution.Projects.Count, Is.EqualTo(1));
    }
}
