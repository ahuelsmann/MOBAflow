// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginMetadata"/> record.
/// </summary>
[TestFixture]
public class PluginMetadataTests
{
    /// <summary>
    /// Test that PluginMetadata can be created with minimal data.
    /// </summary>
    [Test]
    public void Constructor_WithNameOnly_Succeeds()
    {
        var metadata = new PluginMetadata("Test Plugin");

        Assert.That(metadata.Name, Is.EqualTo("Test Plugin"));
        Assert.That(metadata.Version, Is.Null);
        Assert.That(metadata.Author, Is.Null);
        Assert.That(metadata.Description, Is.Null);
        Assert.That(metadata.MinimumHostVersion, Is.Null);
        Assert.That(metadata.Dependencies, Is.Null);
    }

    /// <summary>
    /// Test that PluginMetadata can be created with full data.
    /// </summary>
    [Test]
    public void Constructor_WithAllData_Succeeds()
    {
        var dependencies = new[] { "Dep1", "Dep2" };
        var metadata = new PluginMetadata(
            Name: "Test Plugin",
            Version: "1.0.0",
            Author: "John Doe",
            Description: "A test plugin",
            MinimumHostVersion: "3.15",
            Dependencies: dependencies
        );

        Assert.That(metadata.Name, Is.EqualTo("Test Plugin"));
        Assert.That(metadata.Version, Is.EqualTo("1.0.0"));
        Assert.That(metadata.Author, Is.EqualTo("John Doe"));
        Assert.That(metadata.Description, Is.EqualTo("A test plugin"));
        Assert.That(metadata.MinimumHostVersion, Is.EqualTo("3.15"));
        Assert.That(metadata.Dependencies, Is.EqualTo(dependencies));
    }

    /// <summary>
    /// Test that two PluginMetadata records with same data are equal.
    /// </summary>
    [Test]
    public void Equality_SameData_AreEqual()
    {
        var metadata1 = new PluginMetadata("Test Plugin", "1.0.0", "John Doe");
        var metadata2 = new PluginMetadata("Test Plugin", "1.0.0", "John Doe");

        Assert.That(metadata1, Is.EqualTo(metadata2));
        Assert.That(metadata1.GetHashCode(), Is.EqualTo(metadata2.GetHashCode()));
    }

    /// <summary>
    /// Test that two PluginMetadata records with different data are not equal.
    /// </summary>
    [Test]
    public void Equality_DifferentData_AreNotEqual()
    {
        var metadata1 = new PluginMetadata("Plugin 1");
        var metadata2 = new PluginMetadata("Plugin 2");

        Assert.That(metadata1, Is.Not.EqualTo(metadata2));
    }

    /// <summary>
    /// Test that PluginMetadata with operator works correctly.
    /// </summary>
    [Test]
    public void WithExpression_CreatesNewInstance_WithModifiedProperty()
    {
        var original = new PluginMetadata("Test Plugin", "1.0.0");
        var modified = original with { Version = "2.0.0" };

        Assert.That(modified.Name, Is.EqualTo("Test Plugin"));
        Assert.That(modified.Version, Is.EqualTo("2.0.0"));
        Assert.That(original.Version, Is.EqualTo("1.0.0")); // Original unchanged
    }

    /// <summary>
    /// Test that Dependencies property preserves collection reference.
    /// </summary>
    [Test]
    public void Dependencies_PreservesCollectionReference()
    {
        var deps = new List<string> { "Dep1", "Dep2" };
        var metadata = new PluginMetadata("Test", Dependencies: deps);

        Assert.That(metadata.Dependencies, Is.SameAs(deps));
    }
}

/// <summary>
/// Unit tests for <see cref="PluginPageDescriptor"/> record.
/// </summary>
[TestFixture]
public class PluginPageDescriptorTests
{
    /// <summary>
    /// Test that PluginPageDescriptor can be created.
    /// </summary>
    [Test]
    public void Constructor_ValidData_Succeeds()
    {
        var descriptor = new PluginPageDescriptor("tag1", "Page 1", "\uE8F1", typeof(object));

        Assert.That(descriptor.Tag, Is.EqualTo("tag1"));
        Assert.That(descriptor.Title, Is.EqualTo("Page 1"));
        Assert.That(descriptor.IconGlyph, Is.EqualTo("\uE8F1"));
        Assert.That(descriptor.PageType, Is.EqualTo(typeof(object)));
    }

    /// <summary>
    /// Test that PluginPageDescriptor can have null icon.
    /// </summary>
    [Test]
    public void Constructor_NullIcon_Succeeds()
    {
        var descriptor = new PluginPageDescriptor("tag1", "Page 1", null, typeof(object));

        Assert.That(descriptor.IconGlyph, Is.Null);
    }

    /// <summary>
    /// Test that two PluginPageDescriptor records with same data are equal.
    /// </summary>
    [Test]
    public void Equality_SameData_AreEqual()
    {
        var desc1 = new PluginPageDescriptor("tag1", "Page 1", "\uE8F1", typeof(object));
        var desc2 = new PluginPageDescriptor("tag1", "Page 1", "\uE8F1", typeof(object));

        Assert.That(desc1, Is.EqualTo(desc2));
        Assert.That(desc1.GetHashCode(), Is.EqualTo(desc2.GetHashCode()));
    }

    /// <summary>
    /// Test that two PluginPageDescriptor records with different tags are not equal.
    /// </summary>
    [Test]
    public void Equality_DifferentTag_AreNotEqual()
    {
        var desc1 = new PluginPageDescriptor("tag1", "Page 1", null, typeof(object));
        var desc2 = new PluginPageDescriptor("tag2", "Page 1", null, typeof(object));

        Assert.That(desc1, Is.Not.EqualTo(desc2));
    }

    /// <summary>
    /// Test that PluginPageDescriptor with operator works correctly.
    /// </summary>
    [Test]
    public void WithExpression_CreatesNewInstance_WithModifiedProperty()
    {
        var original = new PluginPageDescriptor("tag1", "Page 1", null, typeof(object));
        var modified = original with { Title = "Page 1 Modified" };

        Assert.That(modified.Tag, Is.EqualTo("tag1"));
        Assert.That(modified.Title, Is.EqualTo("Page 1 Modified"));
        Assert.That(original.Title, Is.EqualTo("Page 1")); // Original unchanged
    }

    /// <summary>
    /// Test that PluginPageDescriptor preserves Type reference.
    /// </summary>
    [Test]
    public void PageType_PreservesTypeReference()
    {
        var pageType = typeof(string);
        var descriptor = new PluginPageDescriptor("tag1", "Page 1", null, pageType);

        Assert.That(descriptor.PageType, Is.SameAs(pageType));
    }
}
