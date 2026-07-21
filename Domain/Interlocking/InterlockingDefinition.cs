// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// Persisted operational definitions shared by every interlocking presentation.
/// Runtime occupancy, locks, confirmations, and route state are deliberately excluded.
/// </summary>
public sealed class InterlockingDefinition
{
    public List<TurnoutDefinition> Turnouts { get; set; } = [];

    public List<SignalDefinition> Signals { get; set; } = [];

    public List<BlockDefinition> Blocks { get; set; } = [];

    public List<OperationalConnection> Connections { get; set; } = [];

    public List<RouteDefinition> Routes { get; set; } = [];

    public List<OperationalBinding> Bindings { get; set; } = [];
}

/// <summary>
/// Directed operational topology edge. Bidirectional edges may be traversed in either direction.
/// </summary>
public sealed class OperationalConnection
{
    public Guid FromOperationalId { get; set; }

    public Guid ToOperationalId { get; set; }

    public bool IsBidirectional { get; set; } = true;
}

/// <summary>
/// Hardware-independent turnout definition with explicit command and confirmation mappings.
/// </summary>
public sealed class TurnoutDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int DecoderAddress { get; set; }

    public TurnoutKind Kind { get; set; } = TurnoutKind.TwoWay;

    public List<TurnoutCommandMapping> Commands { get; set; } = [];

    public List<TurnoutConfirmationMapping> Confirmations { get; set; } = [];
}

/// <summary>
/// One semantic turnout position mapped to the existing Z21 turnout command shape.
/// </summary>
public sealed class TurnoutCommandMapping
{
    public TurnoutPosition Position { get; set; }

    public List<TurnoutAccessoryCommand> Commands { get; set; } = [];
}

/// <summary>
/// One raw accessory command in the ordered sequence for a semantic turnout position.
/// </summary>
public sealed class TurnoutAccessoryCommand
{
    public int AddressOffset { get; set; }

    public int Output { get; set; }

    public bool Activate { get; set; } = true;

    public bool Queue { get; set; }
}

/// <summary>
/// Maps a low-level turnout information response back to a semantic position.
/// </summary>
public sealed class TurnoutConfirmationMapping
{
    public TurnoutPosition Position { get; set; }

    public List<TurnoutFeedbackCondition> Conditions { get; set; } = [];
}

/// <summary>
/// One feedback condition required to confirm a semantic turnout position.
/// </summary>
public sealed class TurnoutFeedbackCondition
{
    public int FunctionAddress { get; set; }

    public bool OutputPosition { get; set; }
}

/// <summary>
/// Operational signal configuration used by protected route decisions.
/// </summary>
public sealed class SignalDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public SignalAspect SafeAspect { get; set; } = SignalAspect.Hp0;

    public SignalSystemType SignalSystem { get; set; } = SignalSystemType.Ks;

    public bool IsMultiplexed { get; set; }

    public string? MultiplexerArticleNumber { get; set; }

    public string? MainSignalArticleNumber { get; set; }

    public string? DistantSignalArticleNumber { get; set; }

    public int BaseAddress { get; set; }
}

/// <summary>
/// Persisted block boundaries and explicit occupancy feedback sources.
/// </summary>
public sealed class BlockDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public BlockDirection Direction { get; set; } = BlockDirection.Bidirectional;

    public List<Guid> BoundaryElementIds { get; set; } = [];

    public List<BlockFeedbackInput> FeedbackInputs { get; set; } = [];
}

/// <summary>
/// One configured observation that explicitly proves occupied or clear state.
/// </summary>
public sealed class BlockFeedbackInput
{
    public int InPort { get; set; }

    public BlockFeedbackRole Role { get; set; }

    public bool ActiveState { get; set; } = true;
}

/// <summary>
/// Persisted route definition. All IDs refer to objects in the same interlocking definition.
/// </summary>
public sealed class RouteDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid EntryElementId { get; set; }

    public Guid ExitElementId { get; set; }

    public List<Guid> PathElementIds { get; set; } = [];

    public List<RouteTurnoutRequirement> TurnoutRequirements { get; set; } = [];

    public List<Guid> ProtectedBlockIds { get; set; } = [];

    public List<RouteSignalRequirement> SignalRequirements { get; set; } = [];

    public List<Guid> ConflictingRouteIds { get; set; } = [];
}

/// <summary>
/// Required semantic position for one turnout in a route.
/// </summary>
public sealed class RouteTurnoutRequirement
{
    public Guid TurnoutId { get; set; }

    public TurnoutPosition Position { get; set; }
}

/// <summary>
/// Configured proceed aspect for one signal protected by a route.
/// </summary>
public sealed class RouteSignalRequirement
{
    public Guid SignalId { get; set; }

    public SignalAspect ProceedAspect { get; set; }
}

/// <summary>
/// Connects one operational identity to its physical and logical representations.
/// </summary>
public sealed class OperationalBinding
{
    public Guid OperationalId { get; set; }

    public List<Guid> TrackSegmentIds { get; set; } = [];

    public List<Guid> SignalBoxElementIds { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TurnoutKind
{
    TwoWay,
    ThreeWay
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TurnoutPosition
{
    Straight,
    DivergingLeft,
    DivergingRight
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockDirection
{
    Bidirectional,
    Forward,
    Reverse
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockFeedbackRole
{
    Occupied,
    Clear
}
