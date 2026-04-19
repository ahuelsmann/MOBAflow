// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Moba.SharedUI.ViewModel.Dialogs;
using Moba.TrackLibrary.PikoA;
using Moba.TrackLibrary.PikoA.Import;

[TestFixture]
internal class VisionImportDialogViewModelTest
{
    private static TrackCatalogEntry Entry(string code) =>
        PikoACatalog.All.First(e => e.Code == code);

    private static VisionTrackCandidate Match(string code, double x, double y) =>
        new(Entry(code), x, y, RawText: code, SourceLineIndex: 0);

    private static VisionUnresolvedToken Token(string raw, double x, double y) =>
        new(raw, x, y, SourceLineIndex: 0);

    private static VisionExtractionResult Extraction(
        IEnumerable<VisionTrackCandidate> matches,
        IEnumerable<VisionUnresolvedToken> unresolved) =>
        new(matches.ToList(), unresolved.ToList());

    [Test]
    public void AllMatchesAreSelectedByDefault()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(
                new[] { Match("G231", 0, 0), Match("R2", 10, 10) },
                Array.Empty<VisionUnresolvedToken>()),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        Assert.That(vm.Matches.All(m => m.IsSelected), Is.True);
        Assert.That(vm.SelectedCount, Is.EqualTo(2));
    }

    [Test]
    public void DeselectingMatchDecrementsSelectedCount()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(
                new[] { Match("G231", 0, 0), Match("R2", 10, 10) },
                Array.Empty<VisionUnresolvedToken>()),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        vm.Matches[0].IsSelected = false;

        Assert.That(vm.SelectedCount, Is.EqualTo(1));
    }

    [Test]
    public void ResolvingUnresolvedIncrementsSelectedCount()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(
                Array.Empty<VisionTrackCandidate>(),
                new[] { Token("G23", 100, 100), Token("Ri", 200, 200) }),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        Assert.That(vm.SelectedCount, Is.Zero);

        vm.Unresolved[0].SelectedResolution = Entry("G231");
        Assert.That(vm.SelectedCount, Is.EqualTo(1));

        vm.Unresolved[1].SelectedResolution = Entry("R1");
        Assert.That(vm.SelectedCount, Is.EqualTo(2));
    }

    [Test]
    public void BuildImportList_OnlyIncludesSelectedMatches()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(
                new[] { Match("G231", 10, 20), Match("R2", 30, 40), Match("WL", 50, 60) },
                Array.Empty<VisionUnresolvedToken>()),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        vm.Matches[1].IsSelected = false;

        var list = vm.BuildImportList();

        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list.Select(c => c.CatalogEntry.Code), Is.EquivalentTo(new[] { "G231", "WL" }));
    }

    [Test]
    public void BuildImportList_IncludesResolvedUnresolvedAtOriginalPosition()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(
                Array.Empty<VisionTrackCandidate>(),
                new[] { Token("G23", 786, 254), Token("Ri", 1826, 214) }),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        vm.Unresolved[0].SelectedResolution = Entry("G231");
        // vm.Unresolved[1] bleibt bewusst unresolved → soll NICHT importiert werden

        var list = vm.BuildImportList();

        Assert.That(list.Count, Is.EqualTo(1));
        var candidate = list[0];
        Assert.That(candidate.CatalogEntry.Code, Is.EqualTo("G231"));
        Assert.That(candidate.CenterX, Is.EqualTo(786));
        Assert.That(candidate.CenterY, Is.EqualTo(254));
        Assert.That(candidate.RawText, Is.EqualTo("G23"));
    }

    [Test]
    public void IsValidScale_TracksPixelsPerMillimeter()
    {
        var vm = new VisionImportDialogViewModel(
            Extraction(Array.Empty<VisionTrackCandidate>(), Array.Empty<VisionUnresolvedToken>()),
            imageWidthPixels: 0,
            imageHeightPixels: 0);

        Assert.That(vm.IsValidScale, Is.True);  // 1.0 default

        vm.PixelsPerMillimeter = 2.5;
        Assert.That(vm.IsValidScale, Is.True);

        vm.PixelsPerMillimeter = 0;
        Assert.That(vm.IsValidScale, Is.False);

        vm.PixelsPerMillimeter = double.NaN;
        Assert.That(vm.IsValidScale, Is.False);
    }

    [Test]
    public void ImportList_CanBeFedDirectlyIntoVisionTrackPlanImporter()
    {
        // End-to-end smoke test: dialog → importer → plan
        var vm = new VisionImportDialogViewModel(
            Extraction(
                new[] { Match("G231", 100, 200), Match("R2", 300, 400) },
                new[] { Token("G23", 500, 600) }),
            imageWidthPixels: 2000,
            imageHeightPixels: 800);

        vm.Unresolved[0].SelectedResolution = Entry("G239");

        var plan = new EditableTrackPlan();
        VisionTrackPlanImporter.Import(plan, vm.BuildImportList(), vm.PixelsPerMillimeter);

        Assert.That(plan.Segments.Count, Is.EqualTo(3));
        Assert.That(plan.Segments.Select(s => s.X),
            Is.EquivalentTo(new[] { 100d, 300d, 500d }));
    }
}
