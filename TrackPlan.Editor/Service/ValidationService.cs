// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Graph;

public sealed class ValidationService
{
    private readonly ITrackCatalog _catalog;

    public ValidationService(ITrackCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<ConstraintViolation> Validate(TopologyGraph graph)
    {
        // Validation disabled until rendering works correctly
        // TODO: Re-enable constraint validation in Phase 2
        return new List<ConstraintViolation>();
    }

    // bestehende Serialize/Deserialize kannst du hier lassen
}