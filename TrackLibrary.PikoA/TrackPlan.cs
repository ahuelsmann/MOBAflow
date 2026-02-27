namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Connection between two port pairs in a TrackPlan.
/// Used by the renderer to determine entry ports and chaining.
/// </summary>
public record PortConnection(Guid SourceSegment, string SourcePort, Guid TargetSegment, string TargetPort);

/// <summary>
/// Result of a track plan builder with segments and start configuration.
/// Contains all track segments and the initial render angle.
/// </summary>
public class TrackPlanResult
{
    /// <summary>Alle Gleissegmente des Track Plans (WR, R9, etc.)</summary>
    public required List<Segment> Segments { get; init; }

    /// <summary>
    /// Start angle for rendering in degrees.
    /// 0° = right, 90° = up, 180° = left, 270° = down
    /// </summary>
    public required double StartAngleDegrees { get; init; }

    /// <summary>
    /// All port connections between segments.
    /// Used by the renderer to understand logical chaining.
    /// </summary>
    public required List<PortConnection> Connections { get; init; }
}

/// <summary>
/// Fluent builder for track plan construction with track connections.
///
/// Enables declarative definition of track connections:
/// <code>
/// var plan = new TrackPlanBuilder()
///     .Start(180)
///     .Add&lt;WR&gt;().FromC
///     .ToA&lt;R9&gt;().FromB
///     .ToA&lt;R9&gt;()
///     .Create();
/// </code>
/// </summary>
public class TrackPlanBuilder
{
    private readonly List<TrackNode> _tracks = new();
    private readonly List<Connection> _connections = new();
    private double _startAngleDegrees;

    /// <summary>
    /// Sets the start angle for rendering the track plan.
    /// </summary>
    /// <param name="angleDegrees">Angle in degrees (0 = right, 90 = up, 180 = left, 270 = down)</param>
    /// <returns>Builder for fluent API chaining</returns>
    public TrackPlanBuilder Start(double angleDegrees)
    {
        _startAngleDegrees = angleDegrees;
        return this;
    }

    /// <summary>
    /// Adds a new track segment of type T.
    /// </summary>
    /// <typeparam name="T">Track type (e.g. WR, R9) - must inherit from Segment</typeparam>
    /// <returns>TrackBuilder for port connections</returns>
    public TrackBuilder<T> Add<T>() where T : Segment, new()
    {
        var node = new TrackNode { Type = typeof(T) };
        _tracks.Add(node);
        return new TrackBuilder<T>(this, node);
    }

    /// <summary>
    /// Internal helper: adds track segment based on type.
    /// </summary>
    internal TrackNode AddTrack(Type type)
    {
        var node = new TrackNode { Type = type };
        _tracks.Add(node);
        return node;
    }

    /// <summary>
    /// Internal helper: registers connection between two ports.
    /// </summary>
    internal void AddConnection(TrackNode source, string sourcePort, TrackNode target, string targetPort)
    {
        _connections.Add(new Connection(source, sourcePort, target, targetPort));
    }

    /// <summary>
    /// Creates the final TrackPlanResult with all segments and connections.
    ///
    /// Process:
    /// 1. Instantiates all track objects with unique GUID
    /// 2. Sets all port connections based on connection list
    /// 3. Converts internal connections to PortConnection records with GUIDs
    /// 4. Returns with start angle
    /// </summary>
    /// <returns>Immutable TrackPlanResult with all segments</returns>
    public TrackPlanResult Create()
    {
        var instances = new Dictionary<TrackNode, Segment>();

        foreach (var node in _tracks)
        {
            var instance = (Segment)Activator.CreateInstance(node.Type)!;
            instance.No = Guid.NewGuid();
            instances[node] = instance;
        }

        var portConnections = new List<PortConnection>();

        foreach (var conn in _connections)
        {
            var sourceInstance = instances[conn.Source];
            var targetInstance = instances[conn.Target];

            var sourceProp = sourceInstance.GetType().GetProperty(conn.SourcePort);
            var targetProp = targetInstance.GetType().GetProperty(conn.TargetPort);

            if (sourceProp != null && targetProp != null)
            {
                sourceProp.SetValue(sourceInstance, targetInstance.No);
                targetProp.SetValue(targetInstance, sourceInstance.No);
            }

            // Export connection with GUIDs for renderer
            portConnections.Add(new PortConnection(
                sourceInstance.No,
                conn.SourcePort,
                targetInstance.No,
                conn.TargetPort
            ));
        }

        return new TrackPlanResult
        {
            Segments = instances.Values.ToList(),
            StartAngleDegrees = _startAngleDegrees,
            Connections = portConnections
        };
    }

    /// <summary>
    /// Internal node for track tracking during builder construction.
    /// </summary>
    internal class TrackNode
    {
        /// <summary>CLR type of the track segment (e.g. typeof(WR), typeof(R9))</summary>
        public Type Type { get; set; } = null!;
    }

    /// <summary>
    /// Internal record for connection information between two port pairs.
    /// </summary>
    private record Connection(TrackNode Source, string SourcePort, TrackNode Target, string TargetPort);
}

/// <summary>
/// Generic builder for track type T with port chaining.
/// Provides fluent API: .FromA, .FromB, .FromC, .FromD
/// </summary>
/// <typeparam name="T">Track type (e.g. WR, R9)</typeparam>
public class TrackBuilder<T> where T : Segment, new()
{
    private readonly TrackPlanBuilder _plan;
    private readonly TrackPlanBuilder.TrackNode _currentTrack;

    /// <summary>Ctor: Initialisiert mit Plan und aktuellem Gleis-Node</summary>
    internal TrackBuilder(TrackPlanBuilder plan, TrackPlanBuilder.TrackNode currentTrack)
    {
        _plan = plan;
        _currentTrack = currentTrack;
    }

    /// <summary>Startet Portverbindung von Port A</summary>
    public PortBuilder FromA => new(_plan, _currentTrack, "PortA");

    /// <summary>Startet Portverbindung von Port B</summary>
    public PortBuilder FromB => new(_plan, _currentTrack, "PortB");

    /// <summary>Startet Portverbindung von Port C</summary>
    public PortBuilder FromC => new(_plan, _currentTrack, "PortC");

    /// <summary>Startet Portverbindung von Port D</summary>
    public PortBuilder FromD => new(_plan, _currentTrack, "PortD");

    /// <summary>
    /// Mehrere parallele Port-Verbindungen definieren.
    /// </summary>
    /// <param name="branches">Lambda expressions for parallel paths</param>
    /// <returns>Builder for further chaining</returns>
    public TrackBuilder<T> Connections(params Func<TrackBuilder<T>, object>[] branches)
    {
        foreach (var branch in branches)
        {
            branch(this);
        }
        return this;
    }

    /// <summary>Erstellt finalen TrackPlanResult - delegiert an PlanBuilder</summary>
    public TrackPlanResult Create() => _plan.Create();
}

/// <summary>
/// Builder for port-to-port connections between track surfaces.
///
/// Provides fluent API: .ToA&lt;R9&gt;, .ToB&lt;R9&gt;, .ToC&lt;WR&gt;, .ToD&lt;R9&gt;
/// </summary>
public class PortBuilder
{
    private readonly TrackPlanBuilder _plan;
    private readonly TrackPlanBuilder.TrackNode _sourceTrack;
    private readonly string _sourcePort;

    /// <summary>Ctor: Initialisiert mit Quell-Gleis und Quell-Port</summary>
    internal PortBuilder(TrackPlanBuilder plan, TrackPlanBuilder.TrackNode sourceTrack, string sourcePort)
    {
        _plan = plan;
        _sourceTrack = sourceTrack;
        _sourcePort = sourcePort;
    }

    /// <summary>Verbindet zum Port A des Ziel-Gleises</summary>
    public TrackBuilder<TTarget> ToA<TTarget>() where TTarget : Segment, new()
        => ConnectTo<TTarget>("PortA");

    /// <summary>Verbindet zum Port B des Ziel-Gleises</summary>
    public TrackBuilder<TTarget> ToB<TTarget>() where TTarget : Segment, new()
        => ConnectTo<TTarget>("PortB");

    /// <summary>Verbindet zum Port C des Ziel-Gleises</summary>
    public TrackBuilder<TTarget> ToC<TTarget>() where TTarget : Segment, new()
        => ConnectTo<TTarget>("PortC");

    /// <summary>Verbindet zum Port D des Ziel-Gleises</summary>
    public TrackBuilder<TTarget> ToD<TTarget>() where TTarget : Segment, new()
        => ConnectTo<TTarget>("PortD");

    /// <summary>
    /// Interner Hilfsmethode: Erstellt Verbindung und Ziel-Gleis.
    /// </summary>
    private TrackBuilder<TTarget> ConnectTo<TTarget>(string targetPort) where TTarget : Segment, new()
    {
        var targetNode = _plan.AddTrack(typeof(TTarget));
        _plan.AddConnection(_sourceTrack, _sourcePort, targetNode, targetPort);
        return new TrackBuilder<TTarget>(_plan, targetNode);
    }
}