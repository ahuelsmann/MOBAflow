namespace Moba.TrackLibrary.PikoA;

using Moba.TrackLibrary.Base;

/// <summary>
/// Ergebnis eines Track Plan Builders mit Segmenten und Startkonfiguration.
/// Enthält alle Gleissegmente und den initialen Renderwinkel.
/// </summary>
public class TrackPlanResult
{
    /// <summary>Alle Gleissegmente des Track Plans (WR, R9, etc.)</summary>
    public required List<Segment> Segments { get; init; }
    
    /// <summary>
    /// Startwinkel für das Rendering in Grad
    /// 0° = rechts, 90° = oben, 180° = links, 270° = unten
    /// </summary>
    public required double StartAngleDegrees { get; init; }
}

/// <summary>
/// Fluent Builder für TrackPlan-Konstruktion mit Gleis-Verbindungen.
/// 
/// Ermöglicht deklarative Definition von Gleisverbindungen:
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
    private double _startAngleDegrees = 0;

    /// <summary>
    /// Setzt den Startwinkel für das Rendering des Track Plans.
    /// </summary>
    /// <param name="angleDegrees">Winkel in Grad (0 = rechts, 90 = oben, 180 = links, 270 = unten)</param>
    /// <returns>Builder für Fluent API Verkettung</returns>
    public TrackPlanBuilder Start(double angleDegrees)
    {
        _startAngleDegrees = angleDegrees;
        return this;
    }

    /// <summary>
    /// Fügt ein neues Gleissegment des Typs T hinzu.
    /// </summary>
    /// <typeparam name="T">Gleistyp (z.B. WR, R9) - muss Segment erben</typeparam>
    /// <returns>TrackBuilder für Port-Verbindungen</returns>
    public TrackBuilder<T> Add<T>() where T : Segment, new()
    {
        var node = new TrackNode { Type = typeof(T) };
        _tracks.Add(node);
        return new TrackBuilder<T>(this, node);
    }

    /// <summary>
    /// Interner Hilfsmethode: Fügt Gleissegment basierend auf Type hinzu.
    /// </summary>
    internal TrackNode AddTrack(Type type)
    {
        var node = new TrackNode { Type = type };
        _tracks.Add(node);
        return node;
    }

    /// <summary>
    /// Interner Hilfsmethode: Registriert Verbindung zwischen zwei Ports.
    /// </summary>
    internal void AddConnection(TrackNode source, string sourcePort, TrackNode target, string targetPort)
    {
        _connections.Add(new Connection(source, sourcePort, target, targetPort));
    }

    /// <summary>
    /// Erstellt das finale TrackPlanResult mit allen Segmenten und Verbindungen.
    /// 
    /// Prozess:
    /// 1. Instantiiert alle Track-Objekte mit eindeutiger GUID
    /// 2. Setzt alle Port-Verbindungen basierend auf Connection-Liste
    /// 3. Rückgabe mit Start-Winkel
    /// </summary>
    /// <returns>Immutable TrackPlanResult mit allen Segmenten</returns>
    public TrackPlanResult Create()
    {
        var instances = new Dictionary<TrackNode, Segment>();

        foreach (var node in _tracks)
        {
            var instance = (Segment)Activator.CreateInstance(node.Type)!;
            instance.No = Guid.NewGuid();
            instances[node] = instance;
        }

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
        }

        return new TrackPlanResult
        {
            Segments = instances.Values.ToList(),
            StartAngleDegrees = _startAngleDegrees
        };
    }

    /// <summary>
    /// Interner Knoten für Gleis-Tracking während Builder-Konstruktion.
    /// </summary>
    internal class TrackNode
    {
        /// <summary>CLR-Typ des Gleissegments (z.B. typeof(WR), typeof(R9))</summary>
        public Type Type { get; set; } = null!;
    }

    /// <summary>
    /// Interner Record für Verbindungsinformation zwischen zwei Port-Paaren.
    /// </summary>
    private record Connection(TrackNode Source, string SourcePort, TrackNode Target, string TargetPort);
}

/// <summary>
/// Generischer Builder für Gleis-Typ T mit Port-Verkettung.
/// Stellt fluent API bereit: .FromA, .FromB, .FromC, .FromD
/// </summary>
/// <typeparam name="T">Gleistyp (z.B. WR, R9)</typeparam>
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
    /// <param name="branches">Lambda-Ausdrücke für parallele Pfade</param>
    /// <returns>Builder für weitere Verkettung</returns>
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
/// Builder für Port-zu-Port Verbindungen zwischen Gleisoberflächen.
/// 
/// Stellt fluent API bereit: .ToA&lt;R9&gt;, .ToB&lt;R9&gt;, .ToC&lt;WR&gt;, .ToD&lt;R9&gt;
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
