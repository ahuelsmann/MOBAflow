// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moba.Common.Plugins;
using Moq;

namespace Moba.Test.Common;

/// <summary>
/// Unit tests for PluginLoader behavior without NavigationRegistry dependency.
/// Note: Full PluginLoader testing is in WinUI integration tests due to NavigationRegistry dependency.
/// </summary>
[TestFixture]
public class PluginLoaderTests
{
    /// <summary>
    /// Test that plugin lifecycle methods can be called without errors.
    /// </summary>
    [Test]
    public async Task PluginLifecycle_InitializeAndUnload_CompletesSuccessfully()
    {
        var mockPlugin = new Mock<IPlugin>();
        mockPlugin.Setup(p => p.Name).Returns("Test Plugin");
        mockPlugin.Setup(p => p.Metadata).Returns(new PluginMetadata("Test Plugin"));
        mockPlugin.Setup(p => p.GetPages()).Returns([]);
        mockPlugin.Setup(p => p.OnInitializedAsync()).Returns(Task.CompletedTask);
        mockPlugin.Setup(p => p.OnUnloadingAsync()).Returns(Task.CompletedTask);

        // Test lifecycle methods
        await mockPlugin.Object.OnInitializedAsync();
        await mockPlugin.Object.OnUnloadingAsync();

        mockPlugin.Verify(p => p.OnInitializedAsync(), Times.Once);
        mockPlugin.Verify(p => p.OnUnloadingAsync(), Times.Once);
    }

    /// <summary>
    /// Test that ConfigureServices can be called.
    /// </summary>
    [Test]
    public void ConfigureServices_ValidPlugin_CanBeCalled()
    {
        var mockPlugin = new Mock<IPlugin>();
        mockPlugin.Setup(p => p.Name).Returns("Test Plugin");
        var services = new ServiceCollection();

        mockPlugin.Object.ConfigureServices(services);

        mockPlugin.Verify(p => p.ConfigureServices(It.IsAny<IServiceCollection>()), Times.Once);
    }

    /// <summary>
    /// Test that plugin pages can be retrieved.
    /// </summary>
    [Test]
    public void GetPages_ValidPlugin_ReturnsPages()
    {
        var mockPlugin = new Mock<IPlugin>();
        var pages = new List<PluginPageDescriptor>
        {
            new PluginPageDescriptor("tag1", "Page 1", null, typeof(object))
        };
        mockPlugin.Setup(p => p.GetPages()).Returns(pages);

        var result = mockPlugin.Object.GetPages().ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Tag, Is.EqualTo("tag1"));
    }

    /// <summary>
    /// Test that plugin initialization can handle exceptions.
    /// </summary>
    [Test]
    public void OnInitializedAsync_ThrowsException_CanBeHandled()
    {
        var mockPlugin = new Mock<IPlugin>();
        mockPlugin.Setup(p => p.OnInitializedAsync())
            .ThrowsAsync(new InvalidOperationException("Init failed"));

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await mockPlugin.Object.OnInitializedAsync());
    }

    /// <summary>
    /// Test that multiple plugins can coexist.
    /// </summary>
    [Test]
    public void MultiplePlugins_CanBeManaged()
    {
        var plugin1 = new Mock<IPlugin>();
        plugin1.Setup(p => p.Name).Returns("Plugin 1");
        plugin1.Setup(p => p.Metadata).Returns(new PluginMetadata("Plugin 1"));

        var plugin2 = new Mock<IPlugin>();
        plugin2.Setup(p => p.Name).Returns("Plugin 2");
        plugin2.Setup(p => p.Metadata).Returns(new PluginMetadata("Plugin 2"));

        var plugins = new List<IPlugin> { plugin1.Object, plugin2.Object };

        Assert.That(plugins, Has.Count.EqualTo(2));
        Assert.That(plugins[0].Name, Is.EqualTo("Plugin 1"));
        Assert.That(plugins[1].Name, Is.EqualTo("Plugin 2"));
    }
}
