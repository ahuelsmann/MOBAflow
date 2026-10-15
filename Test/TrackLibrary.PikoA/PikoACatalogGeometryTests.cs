// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackLibrary.PikoA;

using Moba.TrackLibrary.Base;
using Moba.TrackLibrary.PikoA;

/// <summary>
/// Consistency of the Piko A catalogue: every entry has connectors and a drawing path that agree with each other.
/// </summary>
[TestFixture]
internal sealed class PikoACatalogGeometryTests
{
    private const double ToleranceMm = 0.5;

    private static IEnumerable<TestCaseData> CatalogEntries() =>
        PikoACatalog.All.Select(entry => new TestCaseData(entry).SetArgDisplayNames(entry.Code));

    [TestCaseSource(nameof(CatalogEntries))]
    public void Entry_HasUniquePortsIncludingPortA(TrackCatalogEntry entry)
    {
        var ports = SegmentPortGeometry.GetPorts(entry.CreateInstance());
        var names = ports.Select(port => port.PortName).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(names, Is.Unique);
            Assert.That(names, Does.Contain("PortA"));
        }
    }

    [TestCaseSource(nameof(CatalogEntries))]
    public void Entry_PortsLieOnTheDrawnPathBounds(TrackCatalogEntry entry)
    {
        var segment = entry.CreateInstance();
        var path = SegmentLocalPathBuilder.GetPath(segment);
        var (minX, minY, maxX, maxY) = TrackPathGeometry.GetBounds(path);

        Assert.That(path, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            foreach (var port in SegmentPortGeometry.GetPorts(segment))
            {
                Assert.That(port.LocalX, Is.InRange(minX - ToleranceMm, maxX + ToleranceMm), $"{port.PortName} X");
                Assert.That(port.LocalY, Is.InRange(minY - ToleranceMm, maxY + ToleranceMm), $"{port.PortName} Y");
            }
        }
    }
}
