// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;

/// <summary>Read-only access to current definitions of matching existing project owners; never creates a runtime.</summary>
public interface ILocomotiveTrainRuntimeUsageReader
{
    /// <summary>Copies category A from the actual owner, preserving explicit unknown coverage for B and C.</summary>
    Task<LocomotiveTrainRuntimeObservation> ReadAsync(
        LocomotiveTrainUsageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rechecks the receipt's existing identities without rescanning, I/O or lifecycle operations.</summary>
    Task<LocomotiveTrainObservationValidity> IsCurrentAsync(
        LocomotiveTrainRuntimeObservation observation, CancellationToken cancellationToken = default);
}

/// <summary>One request-local scope; ExpectedProject guards identity and never supplies runtime occurrence data.</summary>
public sealed record LocomotiveTrainUsageRequest(Guid RequestId, Project ExpectedProject, Guid ProjectId, Guid? LocomotiveId = null)
{
    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(ExpectedProject);
        if (RequestId == Guid.Empty || ProjectId == Guid.Empty || ProjectId != ExpectedProject.Id || LocomotiveId == Guid.Empty)
            throw new ArgumentException("A usage request needs non-empty, matching request/project/target identities.");
    }
}

/// <summary>Validity of an historical observation, not a reservation or deletion permission.</summary>
public enum LocomotiveTrainObservationValidity
{
    /// <summary>The bound identities still match at revalidation time.</summary>
    Current,
    /// <summary>The response/receipt belongs to another or replaced scope.</summary>
    Stale,
    /// <summary>The owner is no longer accessible.</summary>
    Unavailable
}

/// <summary>Aggregate runtime evidence can never be Complete while B and C are uninspected.</summary>
public enum LocomotiveTrainRuntimeCoverage
{
    /// <summary>Current definitions were read, but other categories remain unknown.</summary>
    Partial,
    /// <summary>Current definitions could not be completely read.</summary>
    Unknown
}

/// <summary>Copied runtime definition evidence and its short-lived, backend-owned identity receipt.</summary>
public sealed record LocomotiveTrainRuntimeObservation(
    Guid RequestId,
    Guid ProjectId,
    DateTimeOffset ObservedAtUtc,
    LocomotiveTrainReferenceResult Definition,
    [property: System.Text.Json.Serialization.JsonIgnore] LocomotiveTrainUsageReceipt? Receipt = null)
{
    /// <summary>Category A: current loaded definitions only.</summary>
    public LocomotiveTrainCoverage LoadedDefinitionCoverage => Definition.Coverage;
    /// <summary>Category B is deliberately not inferred from current definitions.</summary>
    public LocomotiveTrainCoverage RetainedDefinitionsCoverage { get; } =
        new(LocomotiveTrainCoverageState.Unknown, LocomotiveTrainCoverageReason.NotInspectedInSlice);
    /// <summary>Category C is deliberately not inferred from fleet, speed or traces.</summary>
    public LocomotiveTrainCoverage OperationalUsageCoverage { get; } =
        new(LocomotiveTrainCoverageState.Unknown, LocomotiveTrainCoverageReason.NotInspectedInSlice);
    /// <summary>At most Partial; no complete runtime-use or non-use promise exists.</summary>
    public LocomotiveTrainRuntimeCoverage RuntimeCoverage => Definition.Coverage.State == LocomotiveTrainCoverageState.Complete
        ? LocomotiveTrainRuntimeCoverage.Partial : LocomotiveTrainRuntimeCoverage.Unknown;

    /// <summary>Creates explicitly unavailable evidence without a successful empty definition result.</summary>
    public static LocomotiveTrainRuntimeObservation Unknown(
        LocomotiveTrainUsageRequest request, LocomotiveTrainCoverageReason reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(request.RequestId, request.ProjectId, DateTimeOffset.UtcNow,
            new(request.ProjectId, request.ProjectId.ToString("D"), LocomotiveTrainSource.LoadedRuntimeDefinition,
                request.LocomotiveId.HasValue ? LocomotiveTrainTargetState.Ambiguous : LocomotiveTrainTargetState.NotRequested,
                [], new(LocomotiveTrainCoverageState.Unknown, reason)));
    }
}

/// <summary>Opaque binding to existing owner/definition objects, with no public construction or persisted registry.</summary>
public sealed class LocomotiveTrainUsageReceipt
{
    internal LocomotiveTrainUsageReceipt(object host, object owner, object context, object definition)
    {
        Host = host;
        Owner = owner;
        Context = context;
        Definition = definition;
    }

    internal object Host { get; }
    internal object Owner { get; }
    internal object Context { get; }
    internal object Definition { get; }
    internal LocomotiveTrainRuntimeObservation? Observation { get; set; }
}
