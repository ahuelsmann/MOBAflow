// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Controllers;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class PhotosControllerTests
{
    [Test]
    public void GetFile_ReturnsBadRequest_WhenPathMissing()
    {
        var controller = new PhotosController();

        var result = controller.GetFile(string.Empty);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        var controller = new PhotosController();

        var result = controller.GetFile("photos/locomotives/missing.jpg");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void GetFile_ReturnsBadRequest_WhenPathUsesTraversal()
    {
        var controller = new PhotosController();

        var result = controller.GetFile("photos/../../windows/win.ini");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetFile_ReturnsBadRequest_WhenPathIsAbsolute()
    {
        var controller = new PhotosController();

        var result = controller.GetFile(@"C:\Windows\win.ini");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetFile_ReturnsPhysicalFile_WhenPhotoExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mobaflow-photo-test", Guid.NewGuid().ToString("N"));
        var locomotivesDir = Path.Combine(tempDir, "locomotives");
        Directory.CreateDirectory(locomotivesDir);
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var fullPath = Path.Combine(locomotivesDir, fileName);
        File.WriteAllText(fullPath, "fake-image");

        Environment.SetEnvironmentVariable("MOBAFLOW_PHOTOS_PATH", tempDir);
        try
        {
            var controller = new PhotosController();
            var result = controller.GetFile($"photos/locomotives/{fileName}");

            Assert.That(result, Is.InstanceOf<PhysicalFileResult>());
            var physical = (PhysicalFileResult)result;
            Assert.That(physical.ContentType, Is.EqualTo("image/jpeg"));
            Assert.That(physical.FileName, Is.EqualTo(fullPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOBAFLOW_PHOTOS_PATH", null);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
