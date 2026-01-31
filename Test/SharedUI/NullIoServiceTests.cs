// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.SharedUI;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

/// <summary>
/// Tests for NullIoService - the NullObject implementation of IIoService.
/// Verifies that all methods return expected "not supported" or null results.
/// </summary>
[TestFixture]
public class NullIoServiceTests
{
    private IIoService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new NullIoService();
    }

    [Test]
    public async Task NewSolutionAsync_ReturnsNotSupported()
    {
        var result = await _service.NewSolutionAsync(hasUnsavedChanges: false);

        Assert.That(result.success, Is.False);
        Assert.That(result.userCancelled, Is.False);
        Assert.That(result.error, Does.Contain("not supported"));
    }

    [Test]
    public async Task LoadAsync_ReturnsNotSupported()
    {
        var result = await _service.LoadAsync();

        Assert.That(result.solution, Is.Null);
        Assert.That(result.path, Is.Null);
        Assert.That(result.error, Does.Contain("not supported"));
    }

    [Test]
    public async Task LoadFromPathAsync_ReturnsNotSupported()
    {
        var result = await _service.LoadFromPathAsync("/some/path.json");

        Assert.That(result.solution, Is.Null);
        Assert.That(result.path, Is.Null);
        Assert.That(result.error, Does.Contain("not supported"));
    }

    [Test]
    public async Task SaveAsync_ReturnsNotSupported()
    {
        var solution = new Solution();

        var result = await _service.SaveAsync(solution, "/some/path.json");

        Assert.That(result.success, Is.False);
        Assert.That(result.path, Is.Null);
        Assert.That(result.error, Does.Contain("not supported"));
    }

    [Test]
    public async Task BrowseForJsonFileAsync_ReturnsNull()
    {
        var result = await _service.BrowseForJsonFileAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task BrowseForXmlFileAsync_ReturnsNull()
    {
        var result = await _service.BrowseForXmlFileAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SaveXmlFileAsync_ReturnsNull()
    {
        var result = await _service.SaveXmlFileAsync("test.xml");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task BrowseForAudioFileAsync_ReturnsNull()
    {
        var result = await _service.BrowseForAudioFileAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task BrowseForPhotoAsync_ReturnsNull()
    {
        var result = await _service.BrowseForPhotoAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SavePhotoAsync_ReturnsNull()
    {
        var result = await _service.SavePhotoAsync("/source/photo.jpg", "locomotives", Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPhotoFullPath_ReturnsNull()
    {
        var result = _service.GetPhotoFullPath("photos/locomotives/123.jpg");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPhotoFullPath_WithNullPath_ReturnsNull()
    {
        var result = _service.GetPhotoFullPath(null);

        Assert.That(result, Is.Null);
    }
}
