// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Domain;

/// <summary>
/// Adapts the existing PIKO A catalog to the renderer- and editor-neutral track-library port.
/// Existing segment types remain the source of PIKO-specific geometry.
/// </summary>
public sealed class PikoATrackLibrary : ITrackLibrary
{
    public const string Id = "piko-a";

    private readonly IReadOnlyList<TrackDefinition> _definitions = PikoACatalog.All
        .Select(CreateDefinition)
        .ToList();

    public string LibraryId => Id;

    public string DisplayName => "PIKO A-Track";

    public IReadOnlyList<TrackDefinition> Definitions => _definitions;

    public bool TryGetDefinition(string templateId, out TrackDefinition definition)
    {
        definition = _definitions.FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    private static TrackDefinition CreateDefinition(TrackCatalogEntry entry)
    {
        var segment = entry.CreateInstance();
        var connectors = SegmentPortGeometry.GetPorts(segment)
            .Select(port => new ConnectorDefinition(port.PortName, port.PortName))
            .ToList();
        return new TrackDefinition(Id, entry.Code, entry.DisplayName, entry.Category.ToString(), connectors);
    }
}
