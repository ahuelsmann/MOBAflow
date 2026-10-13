// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Photo capture, endpoint choice and status texts of the MOBAsmart photo upload.
/// </summary>
[TestFixture]
internal sealed class MobilePhotoUploaderTests
{
    private static readonly MobaflowEndpoint KnownEndpoint = new("192.168.0.20", 5001);
    private static readonly (string? ip, int? port) NotFound = (null, null);

    private AppSettings _settings = null!;
    private Mock<IPhotoCaptureService> _capture = null!;
    private Mock<IPhotoUploadService> _upload = null!;
    private Mock<IRestDiscoveryService> _discovery = null!;
    private MobilePhotoUploader _uploader = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AppSettings();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        _capture = new Mock<IPhotoCaptureService>();
        _capture.Setup(capture => capture.CapturePhotoAsync()).ReturnsAsync("photo.jpg");
        _upload = new Mock<IPhotoUploadService>();
        _upload
            .Setup(upload => upload.UploadPhotoAsync(It.IsAny<string>(), It.IsAny<int>(), "photo.jpg", "latest", It.IsAny<Guid>()))
            .ReturnsAsync((true, "photos/latest/1.jpg", null));
        _discovery = new Mock<IRestDiscoveryService>();
        _discovery
            .Setup(discovery => discovery.DiscoverServerAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotFound);
        var locator = TestEndpointLocators.Create(_settings, settingsService.Object, _discovery.Object);
        _uploader = new MobilePhotoUploader(_capture.Object, _upload.Object, locator);
    }

    [Test]
    public async Task CaptureAndUploadAsync_UploadsToTheKnownEndpointAndRemembersIt()
    {
        var result = await UploadAsync(KnownEndpoint).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(new PhotoUploadResult(true, "photos/latest/1.jpg")));
            Assert.That(_settings.RestApi.RecentIpAddresses, Does.Contain("192.168.0.20"));
        }

        _discovery.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CaptureAndUploadAsync_SearchesTheLanWithoutAKnownEndpoint()
    {
        _discovery
            .Setup(discovery => discovery.DiscoverServerAsync("192.168.0.111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("192.168.0.30", 5001));

        var result = await UploadAsync(knownEndpoint: null).ConfigureAwait(false);

        Assert.That(result.Success, Is.True);
        _upload.Verify(upload => upload.UploadPhotoAsync("192.168.0.30", 5001, "photo.jpg", "latest", It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task CaptureAndUploadAsync_ExplainsWhenMobaflowIsNotFound()
    {
        var result = await UploadAsync(knownEndpoint: null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Does.StartWith("⚠️ REST server not found"));
        }

        _upload.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CaptureAndUploadAsync_StopsWhenNoPhotoWasTaken()
    {
        _capture.Setup(capture => capture.CapturePhotoAsync()).ReturnsAsync((string?)null);

        var result = await UploadAsync(KnownEndpoint).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(new PhotoUploadResult(false, "Capture cancelled or not available.")));
        _upload.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CaptureAndUploadAsync_ReportsUploadErrorsAndExceptionsAsStatus()
    {
        _upload
            .Setup(upload => upload.UploadPhotoAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync((false, null, "Server rejected the photo"));
        var rejected = await UploadAsync(KnownEndpoint).ConfigureAwait(false);

        _capture.Setup(capture => capture.CapturePhotoAsync()).ThrowsAsync(new InvalidOperationException("Camera busy"));
        var failed = await UploadAsync(KnownEndpoint).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rejected, Is.EqualTo(new PhotoUploadResult(false, "Server rejected the photo")));
            Assert.That(failed, Is.EqualTo(new PhotoUploadResult(false, "Error: Camera busy")));
            Assert.That(_settings.RestApi.RecentIpAddresses, Is.Empty);
        }
    }

    private Task<PhotoUploadResult> UploadAsync(MobaflowEndpoint? knownEndpoint) =>
        _uploader.CaptureAndUploadAsync(knownEndpoint, "192.168.0.111", CancellationToken.None);
}
