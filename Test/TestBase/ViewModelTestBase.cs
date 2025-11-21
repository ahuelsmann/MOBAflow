// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moq;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface; // ✅ Factory interfaces
using Moba.SharedUI.ViewModel;
using Moba.Backend.Interface; // ✅ IUiDispatcher

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
    /// Mock for IStationViewModelFactory interface
    /// </summary>
    protected Mock<IStationViewModelFactory> StationViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for IWorkflowViewModelFactory interface
    /// </summary>
    protected Mock<IWorkflowViewModelFactory> WorkflowViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for ILocomotiveViewModelFactory interface
    /// </summary>
    protected Mock<ILocomotiveViewModelFactory> LocomotiveViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for ITrainViewModelFactory interface
    /// </summary>
    protected Mock<ITrainViewModelFactory> TrainViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for IWagonViewModelFactory interface
    /// </summary>
    protected Mock<IWagonViewModelFactory> WagonViewModelFactoryMock { get; private set; } = null!;

    /// <summary>
    /// Mock for IUiDispatcher interface (UI thread dispatching)
    /// </summary>
    protected Mock<IUiDispatcher> UiDispatcherMock { get; private set; } = null!;

    /// <summary>
    /// TreeViewBuilder instance configured with mocked factories.
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
        UiDispatcherMock = new Mock<IUiDispatcher>();
        
        // Configure UiDispatcher to execute actions immediately (synchronous for tests)
        UiDispatcherMock
            .Setup(d => d.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        
        // Initialize all ViewModel factory mocks
        JourneyViewModelFactoryMock = new Mock<IJourneyViewModelFactory>();
        StationViewModelFactoryMock = new Mock<IStationViewModelFactory>();
        WorkflowViewModelFactoryMock = new Mock<IWorkflowViewModelFactory>();
        LocomotiveViewModelFactoryMock = new Mock<ILocomotiveViewModelFactory>();
        TrainViewModelFactoryMock = new Mock<ITrainViewModelFactory>();
        WagonViewModelFactoryMock = new Mock<IWagonViewModelFactory>();
        
        // Configure default returns for all ViewModel factories
        JourneyViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Journey>()))
            .Returns((Journey model) => new JourneyViewModel(model));

        StationViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Station>()))
            .Returns((Station model) => new StationViewModel(model));

        WorkflowViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Workflow>()))
            .Returns((Workflow model) => new WorkflowViewModel(model));

        LocomotiveViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Locomotive>()))
            .Returns((Locomotive model) => new LocomotiveViewModel(model));

        TrainViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Train>()))
            .Returns((Train model) => new TrainViewModel(model));

        WagonViewModelFactoryMock
            .Setup(f => f.Create(It.IsAny<Wagon>()))
            .Returns((Wagon model) => new WagonViewModel(model));
        
        // TreeViewBuilder with all mocked factories
        TreeViewBuilder = new TreeViewBuilder(
            JourneyViewModelFactoryMock.Object,
            StationViewModelFactoryMock.Object,
            WorkflowViewModelFactoryMock.Object,
            LocomotiveViewModelFactoryMock.Object,
            TrainViewModelFactoryMock.Object,
            WagonViewModelFactoryMock.Object);
        
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
        // Reset all mocks to clean state
        Z21Mock?.Reset();
        JourneyManagerFactoryMock?.Reset();
        IoServiceMock?.Reset();
        UiDispatcherMock?.Reset();
        JourneyViewModelFactoryMock?.Reset();
        StationViewModelFactoryMock?.Reset();
        WorkflowViewModelFactoryMock?.Reset();
        LocomotiveViewModelFactoryMock?.Reset();
        TrainViewModelFactoryMock?.Reset();
        WagonViewModelFactoryMock?.Reset();
    }
}
