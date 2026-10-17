// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

/// <summary>
/// The null file roles for hosts without files: solution access never persists and every picker cancels.
/// </summary>
[TestFixture]
internal sealed class NullFileServicesTests
{
    private readonly NullSolutionIoService _solutionIo = new();

    [Test]
    public void SolutionIo_CannotPersist()
    {
        Assert.That(_solutionIo.CanPersist, Is.False);
    }

    [Test]
    public async Task SolutionIo_ReportsEveryOperationAsNotSupported()
    {
        var created = await _solutionIo.NewSolutionAsync(hasUnsavedChanges: true).ConfigureAwait(false);
        var loaded = await _solutionIo.LoadAsync().ConfigureAwait(false);
        var loadedFromPath = await _solutionIo.LoadFromPathAsync("/some/path.json").ConfigureAwait(false);
        var saved = await _solutionIo.SaveAsync(new Solution(), "/some/path.json").ConfigureAwait(false);
        var savedAs = await _solutionIo.SaveAsAsync(new Solution()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((created.success, created.userCancelled), Is.EqualTo((false, false)));
            Assert.That(created.error, Does.Contain("not supported"));
            Assert.That((loaded.solution, loaded.path), Is.EqualTo(((Solution?)null, (string?)null)));
            Assert.That(loaded.error, Does.Contain("not supported"));
            Assert.That(loadedFromPath.solution, Is.Null);
            Assert.That(loadedFromPath.error, Does.Contain("not supported"));
            Assert.That((saved.success, saved.path), Is.EqualTo((false, (string?)null)));
            Assert.That(saved.error, Does.Contain("not supported"));
            Assert.That((savedAs.success, savedAs.path), Is.EqualTo((false, (string?)null)));
            Assert.That(savedAs.error, Does.Contain("not supported"));
        }
    }

    private static IEnumerable<TestCaseData> Pickers()
    {
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.BrowseForRecordingFileAsync())).SetArgDisplayNames("open recording");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.SaveRecordingFileAsync("recording"))).SetArgDisplayNames("save recording");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.BrowseForJsonFileAsync())).SetArgDisplayNames("open json");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.SaveJsonFileAsync("track-plan.json"))).SetArgDisplayNames("save json");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.SaveHtmlFileAsync("passport"))).SetArgDisplayNames("save html");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.BrowseForXmlFileAsync())).SetArgDisplayNames("open xml");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.SaveXmlFileAsync("layout.xml"))).SetArgDisplayNames("save xml");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.BrowseForAudioFileAsync())).SetArgDisplayNames("open audio");
        yield return new TestCaseData((Func<IFilePickerService, Task<string?>>)(picker => picker.BrowseForPhotoAsync())).SetArgDisplayNames("open photo");
    }

    [TestCaseSource(nameof(Pickers))]
    public async Task FilePicker_BehavesAsCancelled(Func<IFilePickerService, Task<string?>> pick)
    {
        var result = await pick(new NullFilePickerService()).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }
}
