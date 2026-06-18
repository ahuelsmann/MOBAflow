// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

using Domain;

/// <summary>
/// REST fallback command envelope stored in MOBApi when SignalR is unavailable.
/// </summary>
public sealed record RuntimeCommandEnvelope
{
    public Guid CommandId { get; init; } = Guid.NewGuid();

    public RuntimeCommandType Type { get; init; }

    public string? ClientId { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Guid? SignalId { get; init; }

    public SignalAspect? SignalAspect { get; init; }

    public int? LocomotiveAddress { get; init; }

    public int? Speed { get; init; }

    public bool? Forward { get; init; }

    public int? FunctionIndex { get; init; }

    public bool? FunctionIsOn { get; init; }
}

/// <summary>
/// Supported remote runtime commands from MOBAsmart to MOBAflow.
/// </summary>
public enum RuntimeCommandType
{
    SetSignalAspect,
    SetLocomotiveDrive,
    SetLocomotiveFunction
}
