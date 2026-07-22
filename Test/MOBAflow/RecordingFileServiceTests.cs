#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBAflow;

using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using global::Moba.SharedUI.Interface;
using global::Moba.WinUI.Service;

using Moq;

internal sealed class RecordingFileServiceTests
{
    private string _temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"mobaflow-recorder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [Test]
    public async Task ExportAndImport_Should_RoundTripValidatedArtifactAtomically()
    {
        var path = Path.Combine(_temporaryDirectory, "yard-run.json");
        var picker = new Mock<IFilePickerService>();
        picker.Setup(service => service.SaveRecordingFileAsync(It.IsAny<string>())).ReturnsAsync(path);
        picker.Setup(service => service.BrowseForRecordingFileAsync()).ReturnsAsync(path);
        var serializer = new RecordingArtifactSerializer();
        var service = new RecordingFileService(picker.Object, serializer);
        var artifact = await CreateArtifactAsync();

        var exported = await service.ExportAsync(artifact);
        var imported = await service.ImportAsync();

        Assert.Multiple(() =>
        {
            Assert.That(exported.Succeeded, Is.True);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            Assert.That(imported.Succeeded, Is.True);
            Assert.That(imported.Artifact, Is.Not.Null);
            Assert.That(imported.Artifact!.Metadata.SessionId, Is.EqualTo(artifact.Metadata.SessionId));
            Assert.That(imported.Artifact.Entries.Select(entry => entry.Sequence), Is.EqualTo(artifact.Entries.Select(entry => entry.Sequence)));
        });
    }

    [Test]
    public async Task Import_Should_ReturnPreciseValidationFailure()
    {
        var path = Path.Combine(_temporaryDirectory, "invalid.json");
        await File.WriteAllTextAsync(path, "{}");
        var picker = new Mock<IFilePickerService>();
        picker.Setup(service => service.BrowseForRecordingFileAsync()).ReturnsAsync(path);
        var service = new RecordingFileService(picker.Object, new RecordingArtifactSerializer());

        var result = await service.ImportAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.WasCancelled, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("$.format"));
        });
    }

    private static async Task<RecordingArtifact> CreateArtifactAsync()
    {
        await using var session = new RecordingSessionService(TimeProvider.System);
        session.Start(new RecordingSessionStartRequest("Yard run", "1.0"));
        session.AddNote("Portable context");
        return (await session.StopAsync()).Artifact!;
    }
}
#endif