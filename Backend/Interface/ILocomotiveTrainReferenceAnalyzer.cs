// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;
using Domain.Enum;
using System.Collections.Immutable;

/// <summary>Reads only direct locomotive references in one project definition, without I/O or mutation.</summary>
public interface ILocomotiveTrainReferenceAnalyzer
{
    /// <summary>Copies all entries, or entries for one non-empty locomotive ID, from the specified source.</summary>
    LocomotiveTrainReferenceResult Analyze(Project project, LocomotiveTrainSource source, Guid? locomotiveId = null);
}

/// <summary>The definition from which copied occurrences were read.</summary>
public enum LocomotiveTrainSource
{
    /// <summary>The current editable model, including unsaved changes.</summary>
    EditedProjectData,
    /// <summary>The actual current clone owned by an existing runtime.</summary>
    LoadedRuntimeDefinition
}

/// <summary>Existence of the requested target in this source, independent of other sources.</summary>
public enum LocomotiveTrainTargetState
{
    /// <summary>The query requested all entries.</summary>
    NotRequested,
    /// <summary>Exactly one locomotive has the requested ID.</summary>
    Exists,
    /// <summary>No locomotive has the requested ID.</summary>
    Missing,
    /// <summary>Invalid identities prevent an exact answer.</summary>
    Ambiguous
}

/// <summary>A successfully inspected category or an explicitly unavailable category.</summary>
public enum LocomotiveTrainCoverageState
{
    /// <summary>The stated scope was completely read.</summary>
    Complete,
    /// <summary>The scope could not be completely read.</summary>
    Unknown
}

/// <summary>A structured explanation for incomplete evidence.</summary>
public enum LocomotiveTrainCoverageReason
{
    /// <summary>Successful category observation.</summary>
    None,
    /// <summary>Retained definitions and operational use are outside this slice.</summary>
    NotInspectedInSlice,
    /// <summary>Empty or duplicated identity prevents exact locations.</summary>
    InvalidIdentity,
    /// <summary>No accessible existing owner is available.</summary>
    OwnerUnavailable,
    /// <summary>The matching GUID belongs to another loaded model.</summary>
    ContextMismatch,
    /// <summary>The remote protocol has no owner usage query.</summary>
    UnsupportedRemote,
    /// <summary>The read failed and provides no successful empty result.</summary>
    QueryFailed,
    /// <summary>The observed owner or definition is no longer current.</summary>
    StaleObservation
}

/// <summary>Coverage of one source/category; Unknown always has a concrete reason.</summary>
public sealed record LocomotiveTrainCoverage(LocomotiveTrainCoverageState State, LocomotiveTrainCoverageReason Reason);

/// <summary>An exact copied occurrence, never a mutable domain object.</summary>
public sealed record LocomotiveTrainLocation(
    LocomotiveTrainSource Source,
    Guid ProjectId,
    string ProjectName,
    Guid TrainId,
    string TrainName,
    int VehicleIndex,
    Guid TargetId)
{
    /// <summary>The only relationship kind supported by this query.</summary>
    public TrainVehicleKind TargetKind { get; } = TrainVehicleKind.Locomotive;
    /// <summary>One-based position for operator-facing text.</summary>
    public int VehiclePosition => VehicleIndex + 1;
    /// <summary>Zero-based field path in the containing train.</summary>
    public string FieldPath => $"vehicles[{VehicleIndex}].vehicleId";
}

/// <summary>A rule and a stable occurrence identity; valid uses are neutral, missing references are errors.</summary>
public sealed record LocomotiveTrainFinding(string RuleCode, string FindingId, LocomotiveTrainLocation Location, string Message)
{
    /// <summary>Whether this entry refers to a missing locomotive in this source.</summary>
    public bool IsMissingReference => RuleCode == "LTU002";
}

/// <summary>A fully materialized immutable result for exactly one project and source.</summary>
public sealed record LocomotiveTrainReferenceResult(
    Guid ProjectId,
    string ProjectName,
    LocomotiveTrainSource Source,
    LocomotiveTrainTargetState TargetState,
    ImmutableArray<LocomotiveTrainFinding> Findings,
    LocomotiveTrainCoverage Coverage);
