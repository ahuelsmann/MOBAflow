// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackPlan.Graph;

using System.Text.Json;

public sealed class SerializationService
{
    public string Serialize(TopologyGraph graph)
        => JsonSerializer.Serialize(graph, new JsonSerializerOptions
        {
            WriteIndented = true
        });

    public TopologyGraph Deserialize(string json)
        => JsonSerializer.Deserialize<TopologyGraph>(json)
           ?? throw new InvalidOperationException("Invalid graph JSON.");
}