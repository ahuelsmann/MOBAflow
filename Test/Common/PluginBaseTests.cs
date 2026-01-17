// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;
using Moba.Common.Plugins;

namespace Moba.Test.Common;

/// <summary>
/// Unit tests for <see cref="PluginBase"/> default implementations.
/// </summary>
[TestFixture]
public class PluginBaseTests
{
    /// <summary>
    /// Test plugin implementation for testing purposes.
    /// </summary>
    private class TestPlugin : PluginBase
    {
        public override string Name => "Test Plugin";
    }

    /// <summary>
    /// Test plugin with pages.
    /// </summary>
    private class TestPluginWithPages : PluginBase
    {
        public override string Name => "Test Plugin With Pages";

        public override IEnumerable<PluginPageDescriptor> GetPages()
        {
            yield return new PluginPageDescriptor("testpage", "Test Page", "\uE8F1", typeof(object));
        }
    }

    /// <summary>
    /// Test that PluginBase can be instantiated with just Name property.
    /// </summary>
    [Test]
    public void Constructor_WithMinimalImplementation_Succeeds()
    {
        var plugin = new TestPlugin();

        Assert.That(plugin, Is.Not.Null);
        Assert.That(plugin.Name, Is.EqualTo("Test Plugin"));
    }

    /// <summary>
    /// Test that Metadata returns default implementation with only Name.
    /// </summary>
    [Test]
    public void Metadata_Default_ReturnsNameOnly()
    {
        var plugin = new TestPlugin();

        Assert.That(plugin.Metadata, Is.Not.Null);
        Assert.That(plugin.Metadata.Name, Is.EqualTo("Test Plugin"));
        Assert.That(plugin.Metadata.Version, Is.Null);
        Assert.That(plugin.Metadata.Author, Is.Null);
        Assert.That(plugin.Metadata.Description, Is.Null);
    }

    /// <summary>
    /// Test that GetPages returns empty collection by default.
    /// </summary>
    [Test]
    public void GetPages_Default_ReturnsEmptyCollection()
    {
        var plugin = new TestPlugin();
        var pages = plugin.GetPages().ToList();

        Assert.That(pages, Is.Empty);
    }

    /// <summary>
    /// Test that GetPages can return pages when overridden.
    /// </summary>
    [Test]
    public void GetPages_Overridden_ReturnPages()
    {
        var plugin = new TestPluginWithPages();
        var pages = plugin.GetPages().ToList();

        Assert.That(pages, Is.Not.Empty);
        Assert.That(pages.Count, Is.EqualTo(1));
        Assert.That(pages[0].Tag, Is.EqualTo("testpage"));
        Assert.That(pages[0].Title, Is.EqualTo("Test Page"));
    }

    /// <summary>
    /// Test that ConfigureServices does nothing by default.
    /// </summary>
    [Test]
    public void ConfigureServices_Default_DoesNotThrow()
    {
        var plugin = new TestPlugin();
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() => plugin.ConfigureServices(services));
        Assert.That(services.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Test that OnInitializedAsync returns completed task by default.
    /// </summary>
    [Test]
    public async Task OnInitializedAsync_Default_ReturnsCompletedTask()
    {
        var plugin = new TestPlugin();

        var task = plugin.OnInitializedAsync();
        Assert.That(task.IsCompleted, Is.True);

        await task;
        // No exception means success
        Assert.Pass();
    }

    /// <summary>
    /// Test that OnUnloadingAsync returns completed task by default.
    /// </summary>
    [Test]
    public async Task OnUnloadingAsync_Default_ReturnsCompletedTask()
    {
        var plugin = new TestPlugin();

        var task = plugin.OnUnloadingAsync();
        Assert.That(task.IsCompleted, Is.True);

        await task;
        // No exception means success
        Assert.Pass();
    }

    /// <summary>
    /// Test that Name property is required and enforced.
    /// </summary>
    [Test]
    public void Name_IsAbstract_CannotBeInstantiatedWithoutOverride()
    {
        // This test verifies the compile-time contract
        // PluginBase itself cannot be instantiated
        var type = typeof(PluginBase);
        Assert.That(type.IsAbstract, Is.True);
    }
}
