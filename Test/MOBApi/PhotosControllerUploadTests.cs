// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;

using Moq;

using System.Text;
using System.Text.Json;

/// <summary>
/// Verifies photo uploads from MOBAsmart: accepted files land in the category folder of the configured photo storage
/// and are announced to clients; empty, foreign or misplaced uploads are rejected without writing a file.
/// </summary>
[TestFixture]
internal sealed class PhotosControllerUploadTests
{
    private const string PhotosPathVariable = "MOBAFLOW_PHOTOS_PATH";
    private static readonly Guid EntityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private string _photoRoot = string.Empty;
    private Mock<IClientProxy> _allClients = null!;
    private PhotosController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _photoRoot = Path.Combine(Path.GetTempPath(), "mobaflow-photo-upload-test", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(PhotosPathVariable, _photoRoot);
        _allClients = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients.SetupGet(clients => clients.All).Returns(_allClients.Object);
        var hubContext = new Mock<IHubContext<PhotoHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(hubClients.Object);
        _controller = new PhotosController(hubContext.Object);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(PhotosPathVariable, null);
        if (Directory.Exists(_photoRoot))
        {
            Directory.Delete(_photoRoot, recursive: true);
        }
    }

    [Test]
    public void Constructor_WithoutHubContext_Throws()
    {
        Assert.That(() => new PhotosController(null!), Throws.ArgumentNullException);
    }

    [TestCase("locomotives", "locomotives")]
    [TestCase("passenger-wagons", "wagons")]
    public async Task Upload_StoresThePhotoInItsCategoryFolderAndAnnouncesIt(string category, string folder)
    {
        var result = await _controller.Upload(Photo("Loco.JPG", "image-bytes"), category, EntityId).ConfigureAwait(false);

        var expectedPath = $"photos/{folder}/{EntityId}.jpg";
        var storedFile = Path.Combine(_photoRoot, folder, $"{EntityId}.jpg");
        var body = Body(result);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.GetProperty("success").GetBoolean(), Is.True);
            Assert.That(body.GetProperty("photoPath").GetString(), Is.EqualTo(expectedPath));
            Assert.That(File.Exists(storedFile), Is.True);
            Assert.That(await File.ReadAllTextAsync(storedFile).ConfigureAwait(false), Is.EqualTo("image-bytes"));
            _allClients.Verify(
                clients => clients.SendCoreAsync(
                    "PhotoUploaded",
                    It.Is<object?[]>(args => args.Length == 2 && Equals(args[0], expectedPath)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Upload_WhenNoClientCanBeNotified_StillSucceeds()
    {
        _allClients.Setup(clients => clients.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("no clients"));

        var result = await _controller.Upload(Photo("loco.png", "png"), "locomotives", EntityId).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Body(result).GetProperty("success").GetBoolean(), Is.True);
            Assert.That(File.Exists(Path.Combine(_photoRoot, "locomotives", $"{EntityId}.png")), Is.True);
        }
    }

    [Test]
    public async Task Upload_WithoutFile_IsRejected()
    {
        var result = await _controller.Upload(null, "locomotives", EntityId).ConfigureAwait(false);

        AssertRejected(result, "No file uploaded");
    }

    [Test]
    public async Task Upload_WithEmptyFile_IsRejected()
    {
        var result = await _controller.Upload(Photo("loco.jpg", string.Empty), "locomotives", EntityId).ConfigureAwait(false);

        AssertRejected(result, "No file uploaded");
    }

    [TestCase("loco.exe")]
    [TestCase("loco")]
    public async Task Upload_WithAnUnsupportedType_IsRejectedAndNamesTheAllowedTypes(string fileName)
    {
        var result = await _controller.Upload(Photo(fileName, "data"), "locomotives", EntityId).ConfigureAwait(false);

        AssertRejected(result, "Invalid type. Allowed: .jpg, .jpeg, .png, .bmp, .gif, .webp");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("signals")]
    public async Task Upload_WithAnUnknownCategory_IsRejected(string? category)
    {
        var result = await _controller.Upload(Photo("loco.jpg", "data"), category, EntityId).ConfigureAwait(false);

        AssertRejected(result, "Invalid photo category or path.");
    }

    [TestCase(".png", "image/png")]
    [TestCase(".bmp", "image/bmp")]
    [TestCase(".gif", "image/gif")]
    [TestCase(".webp", "image/webp")]
    [TestCase(".jpeg", "image/jpeg")]
    [TestCase(".tiff", "application/octet-stream")]
    public void GetFile_ServesEachPhotoTypeWithItsContentType(string extension, string contentType)
    {
        var folder = Path.Combine(_photoRoot, "locomotives");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"{EntityId}{extension}"), "image");

        var result = _controller.GetFile($"photos/locomotives/{EntityId}{extension}");

        Assert.That((result as PhysicalFileResult)?.ContentType, Is.EqualTo(contentType));
    }

    [TestCase("", "Path is required")]
    [TestCase("photos/../secret.jpg", "Invalid path")]
    public void GetFile_WithAMissingOrUnsafePath_NamesTheProblem(string path, string error)
    {
        var result = _controller.GetFile(path);

        Assert.That(Error(result as BadRequestObjectResult), Is.EqualTo(error));
    }

    private static string? Error(ObjectResult? result) =>
        result is null ? null : JsonSerializer.SerializeToElement(result.Value).GetProperty("error").GetString();

    private static FormFile Photo(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static JsonElement Body(IActionResult result)
    {
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
    }

    private void AssertRejected(IActionResult result, string error)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(result as BadRequestObjectResult), Is.EqualTo(error));
            Assert.That(Directory.Exists(_photoRoot), Is.False, "A rejected upload must not create the photo storage.");
            _allClients.Verify(
                clients => clients.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
