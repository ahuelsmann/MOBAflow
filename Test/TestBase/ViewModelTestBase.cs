// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moq;
using Moba.Backend.Interface; // ✅ IUiDispatcher
using Moba.SharedUI.Service;

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
        
        // TreeViewBuilder with all mocked factories
        TreeViewBuilder = new TreeViewBuilder();
        
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
    }
}
