// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Moba.TrackLibrary.PikoA;
using Moba.TrackLibrary.PikoA.Import;

[TestFixture]
internal class VisionTrackPlanImporterTest
{
    private static TrackCatalogEntry Entry(string code) =>
        PikoACatalog.All.First(e => e.Code == code);

    private static VisionTrackCandidate Candidate(string code, double x, double y) =>
        new(Entry(code), x, y, RawText: code, SourceLineIndex: 0);

    [Test]
    public void Import_AddsOneSegmentPerCandidate()
    {
        var plan = new EditableTrackPlan();
        var candidates = new[]
        {
            Candidate("G231", 100, 200),
            Candidate("R2",   300, 400),
            Candidate("WL",   500, 600),
        };

        var added = VisionTrackPlanImporter.Import(plan, candidates);

        Assert.That(added, Is.EqualTo(3));
        Assert.That(plan.Segments.Count, Is.EqualTo(3));
    }

    [Test]
    public void Import_MapsPixelCoordinatesWithDefaultScale()
    {
        var plan = new EditableTrackPlan();
        VisionTrackPlanImporter.Import(plan, new[] { Candidate("G231", 123.0, 456.0) });

        var seg = plan.Segments.Single();
        Assert.That(seg.X, Is.EqualTo(123.0));
        Assert.That(seg.Y, Is.EqualTo(456.0));
        Assert.That(seg.RotationDegrees, Is.Zero);
        Assert.That(seg.InPort, Is.Null);
    }

    [Test]
    public void Import_AppliesPixelsPerMillimeterScale()
    {
        var plan = new EditableTrackPlan();
        VisionTrackPlanImporter.Import(plan, new[] { Candidate("G231", 500, 1000) }, pixelsPerMillimeter: 2.0);

        var seg = plan.Segments.Single();
        Assert.That(seg.X, Is.EqualTo(250.0));
        Assert.That(seg.Y, Is.EqualTo(500.0));
    }

    [Test]
    public void Import_GeneratesUniqueSegmentIdsEvenForSameCode()
    {
        // Regression guard: AddSegment dedupes by Segment.No. Every candidate must produce a
        // fresh Segment instance with its own Guid; otherwise the second add would be ignored.
        var plan = new EditableTrackPlan();
        var candidates = Enumerable.Range(0, 5)
            .Select(i => Candidate("G231", i * 100, 0))
            .ToArray();

        var added = VisionTrackPlanImporter.Import(plan, candidates);

        Assert.That(added, Is.EqualTo(5));
        Assert.That(plan.Segments.Count, Is.EqualTo(5));
        Assert.That(plan.Segments.Select(s => s.Segment.No).Distinct().Count(), Is.EqualTo(5));
    }

    [Test]
    public void Import_EmptyInput_AddsNothing()
    {
        var plan = new EditableTrackPlan();
        var added = VisionTrackPlanImporter.Import(plan, Array.Empty<VisionTrackCandidate>());

        Assert.That(added, Is.Zero);
        Assert.That(plan.Segments, Is.Empty);
    }

    [Test]
    public void Import_RejectsNonPositiveScale()
    {
        var plan = new EditableTrackPlan();
        var candidates = new[] { Candidate("G231", 0, 0) };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VisionTrackPlanImporter.Import(plan, candidates, pixelsPerMillimeter: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VisionTrackPlanImporter.Import(plan, candidates, pixelsPerMillimeter: -1.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VisionTrackPlanImporter.Import(plan, candidates, pixelsPerMillimeter: double.NaN));
    }

    [Test]
    public void ResolveUnresolved_TurnsTokenIntoCandidateWithGivenCatalogEntry()
    {
        var token = new VisionUnresolvedToken("G23", 786.0, 254.0, SourceLineIndex: 28);
        var entry = Entry("G231");

        var candidate = VisionTrackPlanImporter.ResolveUnresolved(token, entry);

        Assert.That(candidate.CatalogEntry, Is.SameAs(entry));
        Assert.That(candidate.CenterX, Is.EqualTo(786.0));
        Assert.That(candidate.CenterY, Is.EqualTo(254.0));
        Assert.That(candidate.RawText, Is.EqualTo("G23"));
        Assert.That(candidate.SourceLineIndex, Is.EqualTo(28));
    }
}
