// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Path;

[TestFixture]
[NonParallelizable] // Solution-directory selection is shared process state.
internal sealed class PhotoPathContractTests
{
    private static readonly Guid EntityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private DirectoryInfo _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("MOBAflow.PhotoPath.");
        PhotoPathHelper.SetSolutionDirectory(null);
    }

    [TearDown]
    public void TearDown()
    {
        PhotoPathHelper.SetSolutionDirectory(null);
        // Only the directory returned by CreateTempSubdirectory belongs to this fixture.
        _root.Delete(recursive: true);
    }

    [TestCase(" LOCOMOTIVES ", "locomotives")]
    [TestCase("passenger-wagons", "wagons")]
    [TestCase(" GOODS-WAGONS ", "wagons")]
    public void NormalizeCategory_AcceptsKnownAliases(string category, string expected)
    {
        Assert.That(PhotoPathHelper.NormalizeCategory(category), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("unknown")]
    public void NormalizeCategory_RejectsInvalidCategoryWithHelpfulError(string? category)
    {
        var exception = Assert.Throws<ArgumentException>(() => PhotoPathHelper.NormalizeCategory(category!));

        Assert.That(exception!.ParamName, Is.EqualTo("category"));
        Assert.That(exception.Message, Does.Contain("category"));
        if (category == "unknown")
        {
            Assert.That(exception.Message, Does.Contain("unknown"));
        }
        else
        {
            Assert.That(exception.Message, Does.Contain("must not be empty"));
        }
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase(" \t", "")]
    [TestCase(" .JPEG ", ".JPEG")]
    [TestCase(" png ", ".png")]
    public void ToStorageRelativePath_NormalizesExtensionAndCategory(string? extension, string expectedExtension)
    {
        var relative = PhotoPathHelper.ToStorageRelativePath("passenger-wagons", EntityId, extension!);

        Assert.That(relative, Is.EqualTo($"photos/wagons/{EntityId}{expectedExtension}"));
    }

    [TestCase(" PHOTOS\\wagons\\abc.png ", "photos/wagons/abc.png")]
    [TestCase(" photos//wagons/abc.png ", "photos/wagons/abc.png")]
    [TestCase(" locomotives/abc.jpg ", "photos/locomotives/abc.jpg")]
    public void NormalizeStoredRelativePath_UsesCanonicalPrefixAndSeparators(string path, string expected)
    {
        Assert.That(PhotoPathHelper.NormalizeStoredRelativePath(path), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("photos/")]
    [TestCase("photos\\")]
    [TestCase("../outside.jpg")]
    [TestCase("photos/wagons/../outside.jpg")]
    public void NormalizeStoredRelativePath_RejectsEmptyAndTraversalPaths(string? path)
    {
        Assert.That(PhotoPathHelper.NormalizeStoredRelativePath(path!), Is.Empty);
    }

    [Test]
    public void NormalizeStoredRelativePath_RejectsPlatformRootedPath()
    {
        Assert.That(PhotoPathHelper.NormalizeStoredRelativePath(Path.Combine(_root.FullName, "abc.jpg")), Is.Empty);
    }

    [TestCase(" PHOTOS/locomotives/abc.jpg")]
    [TestCase(" \tPHOTOS\\locomotives/abc.jpg")]
    public void ToFullPath_AcceptsWhitespaceAndCaseInsensitivePrefix(string relative)
    {
        Assert.That(PhotoPathHelper.ToFullPath(_root.FullName, relative),
            Is.EqualTo(Path.Combine(_root.FullName, "locomotives", "abc.jpg")));
    }

    [Test]
    public void ToFullPath_RejectsNullArgumentWithParameterName([Values(true, false)] bool nullBase)
    {
        var exception = Assert.Throws<ArgumentNullException>(() => PhotoPathHelper.ToFullPath(
            nullBase ? null! : _root.FullName, nullBase ? "photos/abc.jpg" : null!));

        Assert.That(exception!.ParamName, Is.EqualTo(nullBase ? "baseDir" : "relativePath"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("locomotives/abc.jpg")]
    public void TryGetStorageRelativePath_RejectsMissingOrNonRootedInput(string? fullPath)
    {
        if (fullPath == null)
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                PhotoPathHelper.TryGetStorageRelativePath(_root.FullName, fullPath!, out _));
            Assert.That(exception!.ParamName, Is.EqualTo("fullPath"));
            return;
        }

        Assert.That(PhotoPathHelper.TryGetStorageRelativePath(_root.FullName, fullPath, out var relative), Is.False);
        Assert.That(relative, Is.Null);
    }

    [Test]
    public void TryGetStorageRelativePath_RejectsNullStorageRoot()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            PhotoPathHelper.TryGetStorageRelativePath(null!, Path.Combine(_root.FullName, "abc.jpg"), out _));

        Assert.That(exception!.ParamName, Is.EqualTo("baseDir"));
    }

    [Test]
    public void TryGetStorageRelativePath_RejectsExactParentDirectory()
    {
        var storageRoot = Path.Combine(_root.FullName, "storage");

        Assert.That(PhotoPathHelper.TryGetStorageRelativePath(storageRoot, _root.FullName, out var relative), Is.False);
        Assert.That(relative, Is.Null);
    }

    [Test]
    public void TryGetStorageRelativePath_NormalizesRootTrailingSeparatorAndFullPath()
    {
        var fullPath = Path.Combine(_root.FullName, "locomotives", "..", "wagons", "abc.jpg");

        Assert.That(PhotoPathHelper.TryGetStorageRelativePath(_root.FullName + Path.DirectorySeparatorChar,
            fullPath, out var relative), Is.True);
        Assert.That(relative, Is.EqualTo("photos/wagons/abc.jpg"));
    }

    [TestCase("photos/locomotives/abc.jpg", false)]
    [TestCase("photos/locomotives/abc.jpg", true)]
    [TestCase("photos\\locomotives/abc.jpg", true)]
    [TestCase("locomotives/abc.jpg", true)]
    public void TryResolvePhotoFullPathUnderBase_FindsBothStorageLayouts(string relative, bool nestedPhotos)
    {
        var file = nestedPhotos
            ? WritePhoto("storage", "photos", "locomotives", "abc.jpg")
            : WritePhoto("storage", "locomotives", "abc.jpg");
        var baseDir = Path.Combine(_root.FullName, "storage");

        Assert.That(PhotoPathHelper.TryResolvePhotoFullPathUnderBase(baseDir, relative, out var resolved), Is.True);
        Assert.That(resolved, Is.EqualTo(file));
    }

    [Test]
    public void TryResolvePhotoFullPathUnderBase_PrefersDirectStorageLayout()
    {
        var direct = WritePhoto("storage", "locomotives", "abc.jpg");
        WritePhoto("storage", "photos", "locomotives", "abc.jpg");

        Assert.That(PhotoPathHelper.TryResolvePhotoFullPathUnderBase(Path.Combine(_root.FullName, "storage"),
            "photos/locomotives/abc.jpg", out var resolved), Is.True);
        Assert.That(resolved, Is.EqualTo(direct));
    }

    [TestCase("../outside/abc.jpg")]
    [TestCase("photos/../outside/abc.jpg")]
    [TestCase("../storage-other/abc.jpg")]
    public void TryResolvePhotoFullPathUnderBase_RejectsExistingOutsideFile(string relative)
    {
        WritePhoto("outside", "abc.jpg");
        WritePhoto("storage-other", "abc.jpg");
        var storageRoot = Path.Combine(_root.FullName, "storage");
        Directory.CreateDirectory(storageRoot);

        Assert.That(PhotoPathHelper.TryResolvePhotoFullPathUnderBase(storageRoot, relative, out var resolved), Is.False);
        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void TryResolvePhotoFullPathUnderBase_RejectsExistingAbsoluteOutsideFile()
    {
        var outside = WritePhoto("outside", "abc.jpg");

        Assert.That(PhotoPathHelper.TryResolvePhotoFullPathUnderBase(Path.Combine(_root.FullName, "storage"),
            outside, out var resolved), Is.False);
        Assert.That(resolved, Is.Null);
    }

    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("photos/locomotives/missing.jpg")]
    public void TryResolvePhotoFullPathUnderBase_MissingFileOrPathReturnsFalse(string relative)
    {
        Assert.That(PhotoPathHelper.TryResolvePhotoFullPathUnderBase(_root.FullName, relative, out var resolved), Is.False);
        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void TryResolvePhotoFullPathUnderBase_RejectsNullArguments([Values(true, false)] bool nullBase)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            PhotoPathHelper.TryResolvePhotoFullPathUnderBase(nullBase ? null! : _root.FullName,
                nullBase ? "photos/abc.jpg" : null!, out _));

        Assert.That(exception!.ParamName, Is.EqualTo(nullBase ? "baseDir" : "normalizedRelativePath"));
    }

    [TestCase("passenger-wagons", " .png ", "wagons", ".png")]
    [TestCase("goods-wagons", "jpeg", "wagons", ".jpeg")]
    [TestCase("locomotives", "", "locomotives", "")]
    public void TryBuildPhotoUploadFullPath_ReturnsPathWithoutCreatingFiles(
        string category, string extension, string folder, string suffix)
    {
        Assert.That(PhotoPathHelper.TryBuildPhotoUploadFullPath(_root.FullName, category, EntityId, extension,
            out var fullPath, out var relative), Is.True);
        Assert.That(fullPath, Is.EqualTo(Path.Combine(_root.FullName, folder, $"{EntityId}{suffix}")));
        Assert.That(relative, Is.EqualTo($"photos/{folder}/{EntityId}{suffix}"));
        Assert.That(Directory.GetFileSystemEntries(_root.FullName), Is.Empty);
    }

    [Test]
    public void TryBuildPhotoUploadFullPath_RejectsExtensionEscapingStorageRoot()
    {
        Assert.That(PhotoPathHelper.TryBuildPhotoUploadFullPath(_root.FullName, "locomotives", EntityId,
            ".jpg/../../../outside.jpg", out var fullPath, out var relative), Is.False);
        Assert.That(fullPath, Is.Null);
        Assert.That(relative, Is.Null);
    }

    [Test]
    public void TryBuildPhotoUploadFullPath_RejectsNullStorageRoot()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            PhotoPathHelper.TryBuildPhotoUploadFullPath(null!, "locomotives", EntityId, ".jpg", out _, out _));

        Assert.That(exception!.ParamName, Is.EqualTo("baseDir"));
    }

    [TestCase("photos/locomotives/abc.jpg")]
    [TestCase("photos/locomotives/abc.jpg?v=2&ignored=1")]
    public void TryResolveExistingPhotoFullPath_PrefersConfiguredRootOverSolution(string relative)
    {
        var configured = WritePhoto("configured", "locomotives", "abc.jpg");
        WritePhoto("solution", "photos", "locomotives", "abc.jpg");
        PhotoPathHelper.SetSolutionDirectory(Path.Combine(_root.FullName, "solution", "solution.json"));

        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(
            $" {Path.Combine(_root.FullName, "configured")} ", relative), Is.EqualTo(configured));
    }

    [Test]
    public void TryResolveExistingPhotoFullPath_MissingConfiguredFileFallsBackToSolution()
    {
        var solutionPhoto = WritePhoto("solution", "photos", "locomotives", "abc.jpg");
        PhotoPathHelper.SetSolutionDirectory(Path.Combine(_root.FullName, "solution", "solution.json"));

        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(Path.Combine(_root.FullName, "missing"),
            "photos/locomotives/abc.jpg"), Is.EqualTo(solutionPhoto));
    }

    [Test]
    public void TryResolveExistingPhotoFullPath_SwitchingSolutionDoesNotUsePreviousFolder()
    {
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var photo = WritePhoto("first", "photos", "locomotives", fileName);
        PhotoPathHelper.SetSolutionDirectory(Path.Combine(_root.FullName, "first", "solution.json"));
        var relative = $"photos/locomotives/{fileName}";
        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(null, relative), Is.EqualTo(photo));

        PhotoPathHelper.SetSolutionDirectory(Path.Combine(_root.FullName, "second", "solution.json"));

        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(null, relative), Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void SetSolutionDirectory_EmptyInputClearsPreviousSelection(string? solution)
    {
        var fileName = $"{Guid.NewGuid():N}.jpg";
        WritePhoto("solution", "photos", "locomotives", fileName);
        PhotoPathHelper.SetSolutionDirectory(Path.Combine(_root.FullName, "solution", "solution.json"));

        PhotoPathHelper.SetSolutionDirectory(solution);

        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(null, $"photos/locomotives/{fileName}"), Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("?v=2")]
    [TestCase("photos/../../outside.jpg")]
    public void TryResolveExistingPhotoFullPath_RejectsMissingAndInvalidPath(string? relative)
    {
        Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(_root.FullName, relative), Is.Null);
    }

    [Test]
    public void ResolvePhotoBaseDirectory_TrimsConfiguredRoot()
    {
        Assert.That(PhotoPathHelper.ResolvePhotoBaseDirectory($" {_root.FullName} \t"), Is.EqualTo(_root.FullName));
    }

    [Test]
    public void PhotoResolution_FallsBackToAppPhotosWithoutChangingExistingFiles()
    {
        var appBase = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var photosRoot = Path.Combine(appBase, "photos");
        var rootExisted = Directory.Exists(photosRoot);
        var ownedFolder = Path.Combine(photosRoot, $"photo-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(ownedFolder);
        var file = Path.Combine(ownedFolder, "abc.jpg");

        try
        {
            File.WriteAllText(file, "test photo");
            var relative = $"photos/{Path.GetFileName(ownedFolder)}/abc.jpg";

            Assert.That(PhotoPathHelper.ResolvePhotoBaseDirectory(null), Is.EqualTo(appBase));
            Assert.That(PhotoPathHelper.TryResolveExistingPhotoFullPath(
                Path.Combine(_root.FullName, "missing"), relative), Is.EqualTo(file));
        }
        finally
        {
            // Delete only this test's file and empty folders; never recursively delete bundled photos.
            File.Delete(file);
            Directory.Delete(ownedFolder);
            if (!rootExisted && !Directory.EnumerateFileSystemEntries(photosRoot).Any())
            {
                Directory.Delete(photosRoot);
            }
        }
    }

    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase(" \t", null)]
    [TestCase("?v=7", "7")]
    [TestCase("photos/abc.jpg?other=1&V=2&v=3", "2")]
    [TestCase("photos/abc.jpg?other=1&&v=", "")]
    [TestCase("photos/abc.jpg?other=1&version=2", null)]
    [TestCase("photos/abc.jpg?other=1&preview=2", null)]
    [TestCase("v=filename-without-query", null)]
    public void TryExtractVersionQuery_OnlyUsesFirstExactVersionKey(string? relative, string? expected)
    {
        Assert.That(PhotoPathHelper.TryExtractVersionQuery(relative), Is.EqualTo(expected));
    }

    private string WritePhoto(params string[] segments)
    {
        var path = Path.Combine([_root.FullName, .. segments]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "test photo");
        return path;
    }
}
