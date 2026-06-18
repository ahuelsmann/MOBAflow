// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Path;

/// <summary>
/// Tests for PhotoPathHelper to prevent path-separator regressions (e.g. DirectorySeparator vs DirectorySeparatorChar).
/// </summary>
[TestFixture]
internal class PhotoPathHelperTests
{
    [Test]
    public void ToFullPath_Strips_photos_prefix_slash()
    {
        var baseDir = @"C:\MOBAflow\Photos";
        var result = PhotoPathHelper.ToFullPath(baseDir, "photos/locomotives/abc.jpg");

        Assert.That(result, Does.Contain(Path.DirectorySeparatorChar.ToString()));
        Assert.That(result, Is.EqualTo(Path.Combine(baseDir, "locomotives", "abc.jpg")));
    }

    [Test]
    public void ToFullPath_Strips_photos_prefix_backslash()
    {
        var baseDir = @"C:\MOBAflow\Photos";
        var result = PhotoPathHelper.ToFullPath(baseDir, "photos\\wagons\\def.png");

        Assert.That(result, Is.EqualTo(Path.Combine(baseDir, "wagons", "def.png")));
    }

    [Test]
    public void ToFullPath_Normalizes_forward_slashes_to_platform_separator()
    {
        var baseDir = @"C:\Base";
        var result = PhotoPathHelper.ToFullPath(baseDir, "photos/a/b/c.jpg");

        Assert.That(result, Does.Contain(Path.DirectorySeparatorChar.ToString()));
        Assert.That(result, Is.EqualTo(Path.Combine(baseDir, "a", "b", "c.jpg")));
    }

    [Test]
    public void ToFullPath_Without_photos_prefix_combines_as_is()
    {
        var baseDir = @"C:\Photos";
        var result = PhotoPathHelper.ToFullPath(baseDir, "latest/xyz.jpg");

        Assert.That(result, Is.EqualTo(Path.Combine(baseDir, "latest", "xyz.jpg")));
    }

    [Test]
    public void ToFullPath_Throws_on_null_baseDir()
    {
        Assert.Throws<ArgumentNullException>(() => PhotoPathHelper.ToFullPath(null!, "photos/a.jpg"));
    }

    [Test]
    public void ToFullPath_Throws_on_null_relativePath()
    {
        Assert.Throws<ArgumentNullException>(() => PhotoPathHelper.ToFullPath(@"C:\Base", null!));
    }

    [Test]
    public void ToFullPath_Uses_DirectorySeparatorChar()
    {
        var baseDir = Path.Combine("C:", "MOBAflow", "Photos");
        var result = PhotoPathHelper.ToFullPath(baseDir, "photos/test.jpg");

        Assert.That(result, Does.Contain(Path.DirectorySeparatorChar));
    }

    [Test]
    public void ToFullPath_With_empty_relativePath_returns_baseDir_combined_with_empty()
    {
        var baseDir = @"C:\Photos";
        var result = PhotoPathHelper.ToFullPath(baseDir, "");

        Assert.That(result, Is.EqualTo(baseDir));
    }

    [Test]
    public void ToFullPath_With_photos_only_strips_to_empty_under_base()
    {
        var baseDir = @"C:\Base";
        var result = PhotoPathHelper.ToFullPath(baseDir, "photos/");

        Assert.That(result, Is.EqualTo(baseDir));
    }

    [Test]
    public void NormalizeCategory_WithWagonsAlias_ReturnsWagons()
    {
        var result = PhotoPathHelper.NormalizeCategory("wagons");

        Assert.That(result, Is.EqualTo("wagons"));
    }

    [Test]
    public void ToStorageRelativePath_ReturnsPhotosPrefixedPath()
    {
        var entityId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var result = PhotoPathHelper.ToStorageRelativePath("locomotives", entityId, "jpg");

        Assert.That(result, Is.EqualTo("photos/locomotives/11111111-2222-3333-4444-555555555555.jpg"));
    }

    [Test]
    public void NormalizeStoredRelativePath_AddsPhotosPrefixAndNormalizesBackslashes()
    {
        var result = PhotoPathHelper.NormalizeStoredRelativePath("wagons\\abc.png");

        Assert.That(result, Is.EqualTo("photos/wagons/abc.png"));
    }

    [Test]
    public void TryGetStorageRelativePath_WhenPathIsInsideBaseDir_ReturnsPhotosRelativePath()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Photos");
        var fullPath = Path.Combine(baseDir, "locomotives", "abc.jpg");

        var success = PhotoPathHelper.TryGetStorageRelativePath(baseDir, fullPath, out var relativePath);

        Assert.That(success, Is.True);
        Assert.That(relativePath, Is.EqualTo("photos/locomotives/abc.jpg"));
    }

    [Test]
    public void TryGetStorageRelativePath_WhenPathIsOutsideBaseDir_ReturnsFalse()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Photos");
        var outsidePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "abc.jpg");

        var success = PhotoPathHelper.TryGetStorageRelativePath(baseDir, outsidePath, out var relativePath);

        Assert.That(success, Is.False);
        Assert.That(relativePath, Is.Null);
    }
}