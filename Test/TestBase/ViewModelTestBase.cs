using Moq;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

namespace Moba.Test.TestBase;

/// <summary>
/// Base class for ViewModel tests that provides common mock dependencies.
/// Reduces duplication across test classes by centralizing mock setup.
/// </summary>
public abstract class ViewModelTestBase
{
    /// <summary>
    /// Mock for IZ21 interface (Z21 command station)
    /// </summary>
    protected Mock<IZ21> Z21Mock { get; private set; } = null!;

    /// <summary>
    /// Mock for IJourneyManagerFactory interface
    /// </summary>
    protected Mock<IJourneyManagerFactory> JourneyManagerFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for IIoService interface (file operations)
    /// </summary>
    protected Mock<IIoService> IoServiceMock { get; private set; } = null!;

    /// <summary>
    /// Mock for IJourneyViewModelFactory interface
    /// </summary>
    protected Mock<IJourneyViewModelFactory> JourneyViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// TreeViewBuilder instance configured with mocked factory.
    /// Ready to use in tests that need tree structure building.
    /// </summary>
    protected TreeViewBuilder TreeViewBuilder { get; private set; } = null!;

    /// <summary>
    /// Initializes all common mocks and services before each test.
    /// Called automatically by NUnit before each test method.
    /// </summary>
    [SetUp]
    public virtual void BaseSetUp()
    {
        Z21Mock = new Mock<IZ21>();
        JourneyManagerFactoryMock = new Mock<IJourneyManagerFactory>();
        IoServiceMock = new Mock<IIoService>();
        JourneyViewModelFactoryMock = new Mock<IJourneyViewModelFactory>();
        
        // Configure default return for IJourneyViewModelFactory
        JourneyViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Journey>()))
            .Returns((Journey model) => new JourneyViewModel(model));
        
        // TreeViewBuilder with mocked factory
        TreeViewBuilder = new TreeViewBuilder(JourneyViewModelFactoryMock.Object);
        
        // Configure default IoService behavior (returns empty solution)
        IoServiceMock
            .Setup(s => s.LoadAsync())
            .ReturnsAsync((new Solution(), null as string, null as string));
        
        IoServiceMock
            .Setup(s => s.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .ReturnsAsync((true, "test.json", null as string));
    }

    /// <summary>
    /// Cleanup method called after each test.
    /// Override in derived classes if additional cleanup is needed.
    /// </summary>
    [TearDown]
    public virtual void BaseTearDown()
    {
        // Reset mocks to clean state
        Z21Mock?.Reset();
        JourneyManagerFactoryMock?.Reset();
        IoServiceMock?.Reset();
        JourneyViewModelFactoryMock?.Reset();
    }
}
