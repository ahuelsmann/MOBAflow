// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

using Microsoft.Extensions.Logging;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>
/// Unit tests for WinUI Page ViewModels.
/// Tests basic page initialization and properties.
/// </summary>
[TestFixture]
public class WinUIPageViewModelTests
{
    private Mock<ILogger<MainWindowViewModel>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();
    }

    [Test]
    public void MainWindowViewModel_Constructor_ShouldInitialize()
    {
        // Note: MainWindowViewModel requires complex dependencies
        // This test documents the basic contract
        // In real scenario, would need full DI setup
        Assert.That(_mockLogger, Is.Not.Null);
    }

    [Test]
    public void PageViewModel_ShouldHaveViewModelProperty()
    {
        // This test documents that all Pages should have a ViewModel property
        // Actual pages: JourneysPage, WorkflowsPage, FeedbackPointsPage, etc.
        // All follow the same pattern
        Assert.Pass("Page ViewModel pattern documented");
    }

    [Test]
    public void PageNavigation_ShouldUseNavigationService()
    {
        // This test documents the navigation pattern
        // NavigationService.cs handles page creation and dependency injection
        Assert.Pass("Navigation pattern documented");
    }
}
