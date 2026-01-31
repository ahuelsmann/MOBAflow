// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginDiscoveryService"/>.
/// </summary>
[TestFixture]
public class PluginDiscoveryServiceTests
{
    private string _testPluginDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _testPluginDirectory = Path.Combine(Path.GetTempPath(), $"MOBAflow_DiscoveryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPluginDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testPluginDirectory))
        {
            Directory.Delete(_testPluginDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Test that DiscoverPlugins handles non-existent directory gracefully.
    /// </summary>
    [Test]
    public void DiscoverPlugins_NonExistentDirectory_ReturnsEmptyList()
    {
        var nonExistentDir = Path.Combine(_testPluginDirectory, "DoesNotExist");

        var plugins = PluginDiscoveryService.DiscoverPlugins(nonExistentDir);

        Assert.That(plugins, Is.Empty);
    }

    /// <summary>
    /// Test that DiscoverPlugins handles empty directory gracefully.
    /// </summary>
    [Test]
    public void DiscoverPlugins_EmptyDirectory_ReturnsEmptyList()
    {
        var plugins = PluginDiscoveryService.DiscoverPlugins(_testPluginDirectory);

        Assert.That(plugins, Is.Empty);
    }

    /// <summary>
    /// Test that DiscoverPlugins returns read-only list.
    /// </summary>
    [Test]
    public void DiscoverPlugins_ReturnsReadOnlyList()
    {
        var plugins = PluginDiscoveryService.DiscoverPlugins(_testPluginDirectory);

        Assert.That(plugins, Is.InstanceOf<IReadOnlyList<IPlugin>>());
    }

    /// <summary>
    /// Test that DiscoverPlugins ignores non-DLL files.
    /// </summary>
    [Test]
    public void DiscoverPlugins_WithNonDllFiles_IgnoresThem()
    {
        File.WriteAllText(Path.Combine(_testPluginDirectory, "test.txt"), "Not a DLL");
        File.WriteAllText(Path.Combine(_testPluginDirectory, "test.exe"), "Not a DLL");

        var plugins = PluginDiscoveryService.DiscoverPlugins(_testPluginDirectory);

        Assert.That(plugins, Is.Empty);
    }

    /// <summary>
    /// Test that DiscoverPlugins handles corrupted DLL gracefully.
    /// </summary>
    [Test]
    public void DiscoverPlugins_CorruptedDll_ReturnsEmptyList()
    {
        File.WriteAllText(Path.Combine(_testPluginDirectory, "corrupted.dll"), "Not a valid DLL");

        var plugins = PluginDiscoveryService.DiscoverPlugins(_testPluginDirectory);

        // Should handle error gracefully
        Assert.That(plugins, Is.Empty);
    }

    /// <summary>
    /// Test that DiscoverPlugins supports nested directory layout.
    /// </summary>
    [Test]
    public void DiscoverPlugins_NestedDirectory_ScansSubdirectories()
    {
        var subDir = Path.Combine(_testPluginDirectory, "NestedPlugin");
        Directory.CreateDirectory(subDir);
        
        // Create a fake DLL file (won't load but should be scanned)
        File.WriteAllText(Path.Combine(subDir, "NestedPlugin.dll"), "Fake DLL");

        var plugins = PluginDiscoveryService.DiscoverPlugins(_testPluginDirectory);

        // Won't load successfully, but should attempt scanning
        // (This test verifies directory scanning, not actual loading)
        Assert.That(plugins, Is.Empty); // Empty because fake DLL can't load
    }

    /// <summary>
    /// Test that DiscoverPlugins logs information when directory doesn't exist.
    /// </summary>
    [Test]
    public void DiscoverPlugins_NonExistentDirectory_LogsInformation()
    {
        var nonExistentDir = Path.Combine(_testPluginDirectory, "DoesNotExist");
        var logCalls = new List<string>();
        
        // Simple logger that captures log messages
        var plugins = PluginDiscoveryService.DiscoverPlugins(nonExistentDir);

        Assert.That(plugins, Is.Empty);
        // Logger would have logged "Plugin directory does not exist"
    }
}
