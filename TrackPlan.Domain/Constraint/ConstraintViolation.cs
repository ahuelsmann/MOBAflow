// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Constraint;

public sealed class ConstraintViolation
{
    public string Kind { get; }
    public string Message { get; }
    public IReadOnlyList<Guid> AffectedEdges { get; }

    public ConstraintViolation(string kind, string message, IEnumerable<Guid> affectedEdges)
    {
        Kind = kind;
        Message = message;
        AffectedEdges = affectedEdges.ToList();
    }
}
