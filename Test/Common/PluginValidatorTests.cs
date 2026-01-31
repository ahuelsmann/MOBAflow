// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Microsoft.Extensions.DependencyInjection;
using Moba.Common.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginValidator"/>.
/// </summary>
[TestFixture]
public class PluginValidatorTests
{
    /// <summary>
    /// Mock plugin for testing.
    /// </summary>
    private class MockPlugin : IPlugin
    {
        public string Name { get; set; } = "Test Plugin";
        public PluginMetadata Metadata => new(Name);
        
        private List<PluginPageDescriptor> _pages = [];

        public IEnumerable<PluginPageDescriptor> GetPages() => _pages;
        public void AddPage(PluginPageDescriptor page) => _pages.Add(page);
        public void ClearPages() => _pages.Clear();
        
        public void ConfigureServices(IServiceCollection services) { }
        public Task OnInitializedAsync() => Task.CompletedTask;
        public Task OnUnloadingAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// Test that valid plugin passes validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_ValidPlugin_ReturnsTrue()
    {
        var plugin = new MockPlugin { Name = "Valid Plugin" };
        plugin.AddPage(new PluginPageDescriptor("page1", "Page 1", null, typeof(object)));

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Test that plugin with empty name fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_EmptyName_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = "" };

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that plugin with null name fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_NullName_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = null! };

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that plugin with whitespace-only name fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_WhitespaceName_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = "   " };

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that plugin with duplicate page tags fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_DuplicatePageTags_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = "Test Plugin" };
        plugin.AddPage(new PluginPageDescriptor("page1", "Page 1", null, typeof(object)));
        plugin.AddPage(new PluginPageDescriptor("page1", "Page 1 Duplicate", null, typeof(object)));

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that plugin with empty page tag fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_EmptyPageTag_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = "Test Plugin" };
        plugin.AddPage(new PluginPageDescriptor("", "Page 1", null, typeof(object)));

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that plugin with null page type fails validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_NullPageType_ReturnsFalse()
    {
        var plugin = new MockPlugin { Name = "Test Plugin" };
        plugin.AddPage(new PluginPageDescriptor("page1", "Page 1", null, null!));

        var isValid = PluginValidator.ValidatePlugin(plugin);

        Assert.That(isValid, Is.False);
    }

    /// <summary>
    /// Test that reserved page tags are warned but don't fail validation.
    /// </summary>
    [Test]
    public void ValidatePlugin_ReservedPageTag_WarnsButReturnsTrue()
    {
        var plugin = new MockPlugin { Name = "Test Plugin" };
        plugin.AddPage(new PluginPageDescriptor("overview", "Overview Page", null, typeof(object)));

        var isValid = PluginValidator.ValidatePlugin(plugin);

        // Should warn but still be valid
        Assert.That(isValid, Is.True);
    }

    /// <summary>
    /// Test that multiple plugins can be validated.
    /// </summary>
    [Test]
    public void ValidatePlugins_MultiplePlugins_CountsValidOnes()
    {
        var valid = new MockPlugin { Name = "Valid Plugin 1" };
        valid.AddPage(new PluginPageDescriptor("page1", "Page 1", null, typeof(object)));

        var invalid = new MockPlugin { Name = "" };

        var validCount = PluginValidator.ValidatePlugins(new[] { valid, invalid });

        Assert.That(validCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Test that empty plugin list returns zero valid count.
    /// </summary>
    [Test]
    public void ValidatePlugins_EmptyList_ReturnsZero()
    {
        var validCount = PluginValidator.ValidatePlugins(Array.Empty<IPlugin>());

        Assert.That(validCount, Is.EqualTo(0));
    }
}
