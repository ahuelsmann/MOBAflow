// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Describes the type of cargo carried by a freight consist.
/// </summary>
public enum CargoType
{
    /// <summary>Unspecified cargo type.</summary>
    None,

    /// <summary>General goods.</summary>
    General,

    /// <summary>Containerized freight.</summary>
    Container,

    /// <summary>Coal shipments.</summary>
    Coal,

    /// <summary>Raw ore material.</summary>
    Ore,

    /// <summary>Wood or timber loads.</summary>
    Wood,

    /// <summary>Crude or refined oil products.</summary>
    Oil,

    /// <summary>Liquefied or compressed gas.</summary>
    Gas,

    /// <summary>Loose bulk goods.</summary>
    BulkGoods,

    /// <summary>Gravel or stone aggregate.</summary>
    Gravel,

    /// <summary>Grain or agricultural bulk.</summary>
    Grain,

    /// <summary>Chemical products.</summary>
    Chemicals,

    /// <summary>Finished automobiles.</summary>
    Automobiles,

    /// <summary>Livestock transport.</summary>
    Livestock,

    /// <summary>Refrigerated or temperature-controlled goods.</summary>
    Refrigerated,

    /// <summary>Liquid transport in tank wagons.</summary>
    Tanker,
}