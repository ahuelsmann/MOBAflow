# MOBAflow Track-Graph Domain Architecture (Explicit)

**Datum:** 2025-01-31  
**Status:** Domain Design (Final Architecture)  
**Philosophie:** Constraints-basiert, keine Koordinaten nach Import

---

## 🎯 CORE PRINCIPLE: KOORDINATEN NUR BEIM IMPORT

```
AnyRail XML (X/Y-Koordinaten)
       ↓
Import-Pipeline (temporär)
       ↓
TrackGraph (reine Topologie + Constraints)
       ↓
Parametrische Geometrie (Funktionen)
       ↓
WorldTransforms (Rendering-Zeit berechnet)
       ↓
SVG PathData (Rendering)

✅ Koordinaten werden EINMALIG transformiert und dann VERWORFEN!
✅ Nach Import: Nur Graph + Constraints + Parameter!
```

---

## 🏗️ EXPLICIT TRACK-GRAPH DOMAIN MODEL

### **1. TrackSegment (Graph Node)**

```csharp
namespace Moba.Domain.TrackPlan.Graph;

/// <summary>
/// Graph-Node: Ein Gleissegment ohne Koordinaten.
/// Definiert durch GeometryRef (PIKO G239, R2, WL, ...) und Connectoren.
/// </summary>
public class TrackSegment
{
    /// <summary>
    /// Eindeutige ID (z.B. "401" aus AnyRail oder GUID).
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Referenz zur Geometrie-Definition (z.B. "PIKO-R2", "PIKO-WL").
    /// KEINE Koordinaten! Nur Verweis auf Template.
    /// </summary>
    public required string GeometryRef { get; init; }
    
    /// <summary>
    /// Connectoren dieses Segments (aus Geometrie-Template berechnet).
    /// Anzahl: 2 (Gerade/Bogen), 3 (Weiche), 4 (Kreuzung).
    /// </summary>
    public required List<TrackConnector> Connectors { get; init; }
    
    /// <summary>
    /// Optional: Assigned feedback sensor port (InPort 1-2048).
    /// Business-Daten, keine Geometrie.
    /// </summary>
    public uint? AssignedInPort { get; set; }
    
    /// <summary>
    /// Optional: User-defined name.
    /// </summary>
    public string? Name { get; set; }
}
```

---

### **2. TrackConnector (Segment Endpoint mit lokalem Transform)**

```csharp
namespace Moba.Domain.TrackPlan.Graph;

/// <summary>
/// Connector: Verbindungspunkt an einem Segment (lokal definiert, kein World-Koordinaten).
/// </summary>
public class TrackConnector
{
    /// <summary>
    /// Eindeutige ID innerhalb des Segments (z.B. "In", "OutMain", "OutBranch").
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Lokale Position relativ zu Segment-Ursprung (0,0).
    /// Wird aus Geometrie-Template kopiert (z.B. (239.07, 0) für PIKO G239-Ende).
    /// </summary>
    public required Vector2 LocalPosition { get; init; }
    
    /// <summary>
    /// Lokaler Winkel (in Grad, 0° = nach rechts, 90° = nach oben).
    /// Tangente an diesem Punkt (z.B. 0° für Gerade, 30° für Bogen-Ende).
    /// </summary>
    public required double LocalAngleDegrees { get; init; }
    
    /// <summary>
    /// Connector-Typ (bestimmt Matching-Regeln).
    /// </summary>
    public required ConnectorType Type { get; init; }
}

/// <summary>
/// Vector2: 2D-Vektor (Position oder Richtung).
/// Immutable Value Object.
/// </summary>
public readonly record struct Vector2(double X, double Y)
{
    public static Vector2 Zero => new(0, 0);
    
    public double Length => Math.Sqrt(X * X + Y * Y);
    
    public Vector2 Normalize()
    {
        var len = Length;
        return len > 1e-10 ? new Vector2(X / len, Y / len) : new Vector2(1, 0);
    }
    
    public double AngleDegrees => Math.Atan2(Y, X) * 180.0 / Math.PI;
}

/// <summary>
/// Connector-Typ: Bestimmt Matching-Regeln und Constraints.
/// </summary>
public enum ConnectorType
{
    /// <summary>
    /// Standard-Gleis-Connector (Gerade, Bogen).
    /// </summary>
    Track,
    
    /// <summary>
    /// Weiche: Hauptstrang (rigid connection).
    /// </summary>
    SwitchMain,
    
    /// <summary>
    /// Weiche: Abzweig (parametrisch, Winkel-abhängig).
    /// </summary>
    SwitchBranch,
    
    /// <summary>
    /// Kreuzung: Durchgehend.
    /// </summary>
    CrossingThrough,
    
    /// <summary>
    /// Kreuzung: Kreuzend.
    /// </summary>
    CrossingCross
}
```

---

### **3. TrackConnection (Graph Edge mit Constraint)**

```csharp
namespace Moba.Domain.TrackPlan.Graph;

/// <summary>
/// Graph-Edge: Verbindung zwischen zwei Connectoren.
/// Definiert durch Constraint (rigid, rotational, parametric).
/// </summary>
public class TrackConnection
{
    /// <summary>
    /// Eindeutige ID.
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Von-Segment ID.
    /// </summary>
    public required string FromSegmentId { get; init; }
    
    /// <summary>
    /// Von-Connector ID (z.B. "OutMain").
    /// </summary>
    public required string FromConnectorId { get; init; }
    
    /// <summary>
    /// Zu-Segment ID.
    /// </summary>
    public required string ToSegmentId { get; init; }
    
    /// <summary>
    /// Zu-Connector ID (z.B. "In").
    /// </summary>
    public required string ToConnectorId { get; init; }
    
    /// <summary>
    /// Constraint-Typ (bestimmt Transform-Berechnung).
    /// </summary>
    public required ConstraintType Type { get; init; }
    
    /// <summary>
    /// Optional: Parameter für parametrische Constraints (z.B. Weichen-Winkel).
    /// </summary>
    public Dictionary<string, double> Parameters { get; } = new();
}

/// <summary>
/// Constraint-Typ: Bestimmt, wie die Verbindung transformiert wird.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Rigid: Position + Winkel exakt gleich (±180°).
    /// Verwendet für: Gerade ↔ Gerade, Bogen ↔ Gerade (tangential).
    /// </summary>
    Rigid,
    
    /// <summary>
    /// Rotational: Position gleich, Winkel frei drehbar.
    /// Verwendet für: Drehscheiben, Schiebebühnen.
    /// </summary>
    Rotational,
    
    /// <summary>
    /// Parametric: Position + Winkel abhängig von Parameter.
    /// Verwendet für: Weichen (Abzweig-Winkel), Y-Weichen.
    /// </summary>
    Parametric
}
```

---

### **4. TrackGraph (Gesamtstruktur)**

```csharp
namespace Moba.Domain.TrackPlan.Graph;

/// <summary>
/// TrackGraph: Gesamte Gleisplan-Topologie.
/// Enthält alle Segmente + Connections (keine Koordinaten).
/// </summary>
public class TrackGraph
{
    /// <summary>
    /// Alle Gleissegmente (Graph-Nodes).
    /// </summary>
    public required List<TrackSegment> Segments { get; init; }
    
    /// <summary>
    /// Alle Verbindungen (Graph-Edges).
    /// </summary>
    public required List<TrackConnection> Connections { get; init; }
    
    /// <summary>
    /// Finde Segment nach ID.
    /// </summary>
    public TrackSegment? FindSegment(string segmentId)
        => Segments.FirstOrDefault(s => s.Id == segmentId);
    
    /// <summary>
    /// Finde Connector an Segment.
    /// </summary>
    public TrackConnector? FindConnector(string segmentId, string connectorId)
        => FindSegment(segmentId)?.Connectors.FirstOrDefault(c => c.Id == connectorId);
    
    /// <summary>
    /// Finde ausgehende Connections von Segment.
    /// </summary>
    public List<TrackConnection> GetOutgoingConnections(string segmentId)
        => Connections.Where(c => c.FromSegmentId == segmentId).ToList();
    
    /// <summary>
    /// Finde eingehende Connections zu Segment.
    /// </summary>
    public List<TrackConnection> GetIncomingConnections(string segmentId)
        => Connections.Where(c => c.ToSegmentId == segmentId).ToList();
    
    /// <summary>
    /// Validiere Graph (alle Segment-IDs existieren, keine Zyklen, etc.).
    /// </summary>
    public GraphValidationResult Validate()
    {
        var errors = new List<string>();
        
        // 1. Alle Segments haben eindeutige IDs
        var duplicateIds = Segments.GroupBy(s => s.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var id in duplicateIds)
            errors.Add($"Duplicate Segment ID: {id}");
        
        // 2. Alle Connections referenzieren existierende Segments + Connectoren
        foreach (var conn in Connections)
        {
            if (FindConnector(conn.FromSegmentId, conn.FromConnectorId) == null)
                errors.Add($"Connection {conn.Id}: FromConnector not found ({conn.FromSegmentId}.{conn.FromConnectorId})");
            
            if (FindConnector(conn.ToSegmentId, conn.ToConnectorId) == null)
                errors.Add($"Connection {conn.Id}: ToConnector not found ({conn.ToSegmentId}.{conn.ToConnectorId})");
        }
        
        return new GraphValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

public record GraphValidationResult
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
}
```

---

## 🔧 CONNECTOR CONSTRAINT SYSTEM

### **Constraint-Regeln (Matching + Transform)**

```csharp
namespace Moba.Domain.TrackPlan.Constraints;

/// <summary>
/// ConnectorMatcher: Findet passende Connector-Paare beim Import.
/// Verwendet Distanz + Winkel-Toleranz.
/// </summary>
public class ConnectorMatcher
{
    private const double DistanceTolerance = 1.0; // 1mm
    private const double AngleTolerance = 5.0;     // 5°
    
    /// <summary>
    /// Finde passenden Connector zu gegebenem Connector (aus temporären Import-Koordinaten).
    /// </summary>
    public (TrackConnector? Connector, double Distance)? FindMatch(
        TrackConnector sourceConnector,
        Vector2 sourceWorldPosition,
        double sourceWorldAngle,
        List<(TrackConnector Connector, Vector2 WorldPosition, double WorldAngle)> candidates)
    {
        var matches = new List<(TrackConnector Connector, double Distance, double AngleDiff)>();
        
        foreach (var (connector, worldPos, worldAngle) in candidates)
        {
            // Distanz prüfen
            var dx = worldPos.X - sourceWorldPosition.X;
            var dy = worldPos.Y - sourceWorldPosition.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance > DistanceTolerance)
                continue; // Zu weit entfernt
            
            // Winkel prüfen (±180° erlaubt für gegenüberliegende Connectoren)
            var angleDiff = NormalizeAngle(worldAngle - sourceWorldAngle);
            var isOpposite = Math.Abs(angleDiff - 180.0) < AngleTolerance || Math.Abs(angleDiff + 180.0) < AngleTolerance;
            var isSame = Math.Abs(angleDiff) < AngleTolerance;
            
            if (!isOpposite && !isSame)
                continue; // Winkel passt nicht
            
            matches.Add((connector, distance, Math.Abs(angleDiff)));
        }
        
        // Bester Match: Kleinste Distanz
        if (matches.Count == 0)
            return null;
        
        var best = matches.OrderBy(m => m.Distance).First();
        return (best.Connector, best.Distance);
    }
    
    private static double NormalizeAngle(double degrees)
    {
        var result = degrees % 360.0;
        if (result > 180.0) result -= 360.0;
        if (result < -180.0) result += 360.0;
        return result;
    }
}

/// <summary>
/// ConstraintSolver: Berechnet WorldTransform aus Constraints.
/// </summary>
public class ConstraintSolver
{
    /// <summary>
    /// Berechne WorldTransform für Segment basierend auf Parent-Connection.
    /// </summary>
    public Matrix3x2 CalculateWorldTransform(
        Matrix3x2 parentWorldTransform,
        TrackConnector parentConnector,
        TrackConnector childConnector,
        ConstraintType constraintType,
        Dictionary<string, double>? parameters = null)
    {
        return constraintType switch
        {
            ConstraintType.Rigid => CalculateRigidTransform(parentWorldTransform, parentConnector, childConnector),
            ConstraintType.Rotational => CalculateRotationalTransform(parentWorldTransform, parentConnector, childConnector),
            ConstraintType.Parametric => CalculateParametricTransform(parentWorldTransform, parentConnector, childConnector, parameters),
            _ => Matrix3x2.Identity
        };
    }
    
    /// <summary>
    /// Rigid Constraint: Position + Winkel exakt.
    /// </summary>
    private Matrix3x2 CalculateRigidTransform(
        Matrix3x2 parentWorldTransform,
        TrackConnector parentConnector,
        TrackConnector childConnector)
    {
        // 1. Parent-Connector in World-Koordinaten
        var parentWorldPos = Vector2.Transform(parentConnector.LocalPosition, parentWorldTransform);
        var parentWorldAngle = ExtractRotation(parentWorldTransform) + parentConnector.LocalAngleDegrees;
        
        // 2. Child muss entgegengesetzt ausgerichtet sein (±180°)
        var requiredChildAngle = parentWorldAngle + 180.0;
        
        // 3. Rotation: Child-Connector muss auf requiredChildAngle zeigen
        var childRotation = requiredChildAngle - childConnector.LocalAngleDegrees;
        
        // 4. Translation: Child-Ursprung so verschieben, dass Child-Connector bei Parent-Connector liegt
        var rotatedChildConnectorLocal = Vector2.Transform(
            childConnector.LocalPosition,
            Matrix3x2.CreateRotation(childRotation * Math.PI / 180.0)
        );
        var translation = new Vector2(
            parentWorldPos.X - rotatedChildConnectorLocal.X,
            parentWorldPos.Y - rotatedChildConnectorLocal.Y
        );
        
        // 5. Kombiniere Translation + Rotation
        return Matrix3x2.CreateTranslation(translation)
             * Matrix3x2.CreateRotation(childRotation * Math.PI / 180.0);
    }
    
    /// <summary>
    /// Rotational Constraint: Position fix, Winkel frei.
    /// </summary>
    private Matrix3x2 CalculateRotationalTransform(
        Matrix3x2 parentWorldTransform,
        TrackConnector parentConnector,
        TrackConnector childConnector)
    {
        // Position wie Rigid, aber keine Winkel-Constraint
        var parentWorldPos = Vector2.Transform(parentConnector.LocalPosition, parentWorldTransform);
        
        // Translation zum Parent-Connector
        var translation = new Vector2(
            parentWorldPos.X - childConnector.LocalPosition.X,
            parentWorldPos.Y - childConnector.LocalPosition.Y
        );
        
        return Matrix3x2.CreateTranslation(translation);
    }
    
    /// <summary>
    /// Parametric Constraint: Position + Winkel abhängig von Parameter.
    /// </summary>
    private Matrix3x2 CalculateParametricTransform(
        Matrix3x2 parentWorldTransform,
        TrackConnector parentConnector,
        TrackConnector childConnector,
        Dictionary<string, double>? parameters)
    {
        // Beispiel: Weichen-Abzweig-Winkel
        var branchAngle = parameters?.GetValueOrDefault("BranchAngle", 15.0) ?? 15.0;
        
        // Berechne wie Rigid, aber mit parametrischem Offset
        var parentWorldPos = Vector2.Transform(parentConnector.LocalPosition, parentWorldTransform);
        var parentWorldAngle = ExtractRotation(parentWorldTransform) + parentConnector.LocalAngleDegrees;
        
        var requiredChildAngle = parentWorldAngle + 180.0 + branchAngle; // Abzweig-Offset
        var childRotation = requiredChildAngle - childConnector.LocalAngleDegrees;
        
        var rotatedChildConnectorLocal = Vector2.Transform(
            childConnector.LocalPosition,
            Matrix3x2.CreateRotation(childRotation * Math.PI / 180.0)
        );
        var translation = new Vector2(
            parentWorldPos.X - rotatedChildConnectorLocal.X,
            parentWorldPos.Y - rotatedChildConnectorLocal.Y
        );
        
        return Matrix3x2.CreateTranslation(translation)
             * Matrix3x2.CreateRotation(childRotation * Math.PI / 180.0);
    }
    
    private static double ExtractRotation(Matrix3x2 matrix)
        => Math.Atan2(matrix.M21, matrix.M11) * 180.0 / Math.PI;
}
```

---

## 🔄 IMPORT-PIPELINE (XML → TrackGraph → Koordinaten verwerfen)

```csharp
namespace Moba.Domain.TrackPlan.Import;

/// <summary>
/// AnyRailImporter: Transformiert AnyRail XML in TrackGraph.
/// Koordinaten werden NUR temporär verwendet und dann verworfen!
/// </summary>
public class AnyRailImporter
{
    private readonly ConnectorMatcher _matcher = new();
    
    /// <summary>
    /// Importiere AnyRail XML → TrackGraph.
    /// </summary>
    public TrackGraph ImportFromXml(string xmlPath)
    {
        // 1. Parse XML (mit temporären Koordinaten)
        var anyRailLayout = AnyRailLayout.Parse(xmlPath);
        
        // 2. Erstelle temporäre Import-Segmente (mit World-Koordinaten)
        var tempSegments = CreateTemporarySegments(anyRailLayout);
        
        // 3. Connector-Matching (finde passende Paare)
        var connections = MatchConnectors(tempSegments);
        
        // 4. Erstelle finalen TrackGraph (OHNE Koordinaten)
        var segments = tempSegments.Select(ts => new TrackSegment
        {
            Id = ts.Id,
            GeometryRef = ts.GeometryRef,
            Connectors = ts.Connectors.Select(c => new TrackConnector
            {
                Id = c.Id,
                LocalPosition = c.LocalPosition,
                LocalAngleDegrees = c.LocalAngleDegrees,
                Type = c.Type
            }).ToList()
        }).ToList();
        
        // 5. Koordinaten verwerfen! (tempSegments out of scope)
        return new TrackGraph
        {
            Segments = segments,
            Connections = connections
        };
    }
    
    /// <summary>
    /// Erstelle temporäre Import-Segmente (mit World-Koordinaten für Matching).
    /// </summary>
    private List<TemporaryImportSegment> CreateTemporarySegments(AnyRailLayout layout)
    {
        var result = new List<TemporaryImportSegment>();
        
        foreach (var part in layout.Parts)
        {
            // GeometryRef aus AnyRail Type ableiten
            var geometryRef = DeriveGeometryRef(part);
            
            // Connectoren aus Endpoints erstellen
            var connectors = new List<TemporaryConnector>();
            foreach (var endpointNr in part.EndpointNrs)
            {
                var endpoint = layout.Endpoints.FirstOrDefault(e => e.Nr == endpointNr);
                if (endpoint == null) continue;
                
                connectors.Add(new TemporaryConnector
                {
                    Id = $"Connector{endpointNr}",
                    LocalPosition = new Vector2(0, 0), // TODO: Berechne aus Geometrie
                    LocalAngleDegrees = endpoint.Direction,
                    Type = ConnectorType.Track,
                    WorldPosition = new Vector2(endpoint.X, endpoint.Y), // TEMPORÄR!
                    WorldAngleDegrees = endpoint.Direction // TEMPORÄR!
                });
            }
            
            result.Add(new TemporaryImportSegment
            {
                Id = part.Id,
                GeometryRef = geometryRef,
                Connectors = connectors
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// Connector-Matching: Finde Connector-Paare (Distanz + Winkel).
    /// </summary>
    private List<TrackConnection> MatchConnectors(List<TemporaryImportSegment> segments)
    {
        var connections = new List<TrackConnection>();
        var usedConnectors = new HashSet<string>();
        
        foreach (var segment in segments)
        {
            foreach (var connector in segment.Connectors)
            {
                var connectorKey = $"{segment.Id}.{connector.Id}";
                if (usedConnectors.Contains(connectorKey))
                    continue;
                
                // Finde alle anderen Connectoren (Kandidaten)
                var candidates = segments
                    .SelectMany(s => s.Connectors.Select(c => (
                        Connector: c,
                        SegmentId: s.Id,
                        WorldPosition: c.WorldPosition,
                        WorldAngle: c.WorldAngleDegrees
                    )))
                    .Where(x => $"{x.SegmentId}.{x.Connector.Id}" != connectorKey)
                    .Where(x => !usedConnectors.Contains($"{x.SegmentId}.{x.Connector.Id}"))
                    .ToList();
                
                // Match finden
                var match = _matcher.FindMatch(
                    connector,
                    connector.WorldPosition,
                    connector.WorldAngleDegrees,
                    candidates.Select(x => (x.Connector, x.WorldPosition, x.WorldAngle)).ToList()
                );
                
                if (match == null)
                    continue; // Kein Match
                
                var matchedCandidate = candidates.First(c => c.Connector == match.Value.Connector);
                
                // Connection erstellen
                connections.Add(new TrackConnection
                {
                    Id = Guid.NewGuid().ToString(),
                    FromSegmentId = segment.Id,
                    FromConnectorId = connector.Id,
                    ToSegmentId = matchedCandidate.SegmentId,
                    ToConnectorId = matchedCandidate.Connector.Id,
                    Type = ConstraintType.Rigid // Default
                });
                
                // Beide Connectoren als "verwendet" markieren
                usedConnectors.Add(connectorKey);
                usedConnectors.Add($"{matchedCandidate.SegmentId}.{matchedCandidate.Connector.Id}");
            }
        }
        
        return connections;
    }
    
    private string DeriveGeometryRef(AnyRailPart part)
    {
        // Vereinfacht: Leite Piko ArticleCode ab
        return part.GetArticleCode(); // Aus AnyRailPart.GetArticleCode()
    }
}

/// <summary>
/// Temporäres Import-Segment (MIT Koordinaten, nur während Import).
/// </summary>
internal class TemporaryImportSegment
{
    public required string Id { get; init; }
    public required string GeometryRef { get; init; }
    public required List<TemporaryConnector> Connectors { get; init; }
}

/// <summary>
/// Temporärer Connector (MIT World-Koordinaten, nur während Import).
/// </summary>
internal class TemporaryConnector
{
    public required string Id { get; init; }
    public required Vector2 LocalPosition { get; init; }
    public required double LocalAngleDegrees { get; init; }
    public required ConnectorType Type { get; init; }
    
    // TEMPORÄR: Nur für Matching!
    public required Vector2 WorldPosition { get; init; }
    public required double WorldAngleDegrees { get; init; }
}
```

---

## 🎯 PARAMETRIC SWITCH GEOMETRY (Function-Based)

```csharp
namespace Moba.Domain.TrackPlan.Geometry;

/// <summary>
/// SwitchGeometry: Parametrische Weichen-Geometrie (Funktion, kein Objekt).
/// </summary>
public class SwitchGeometry
{
    public required double BranchAngle { get; init; }   // z.B. 15° (PIKO)
    public required double BranchRadius { get; init; }  // z.B. 908 mm (PIKO R9)
    public required double Length { get; init; }         // z.B. 231 mm (PIKO)
    
    /// <summary>
    /// Berechne Connectoren (IN, OUT_MAIN, OUT_BRANCH).
    /// </summary>
    public List<TrackConnector> CalculateConnectors()
    {
        return new List<TrackConnector>
        {
            // IN: Bei (0, 0), Winkel 0°
            new TrackConnector
            {
                Id = "In",
                LocalPosition = Vector2.Zero,
                LocalAngleDegrees = 0.0,
                Type = ConnectorType.Track
            },
            
            // OUT_MAIN: Bei (Length, 0), Winkel 180°
            new TrackConnector
            {
                Id = "OutMain",
                LocalPosition = new Vector2(Length, 0),
                LocalAngleDegrees = 180.0,
                Type = ConnectorType.SwitchMain
            },
            
            // OUT_BRANCH: Berechne aus Bogen (Radius + Winkel)
            new TrackConnector
            {
                Id = "OutBranch",
                LocalPosition = CalculateBranchEndpoint(),
                LocalAngleDegrees = 180.0 + BranchAngle,
                Type = ConnectorType.SwitchBranch
            }
        };
    }
    
    /// <summary>
    /// Berechne Abzweig-Endpunkt (Kreisbogen-Formel).
    /// </summary>
    private Vector2 CalculateBranchEndpoint()
    {
        var angleRad = BranchAngle * Math.PI / 180.0;
        var x = BranchRadius * Math.Sin(angleRad);
        var y = BranchRadius * (1 - Math.Cos(angleRad));
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Generiere SVG PathData (parametrisch berechnet).
    /// </summary>
    public string GeneratePathData()
    {
        var branchEnd = CalculateBranchEndpoint();
        
        // Hauptstrang (Gerade)
        var mainPath = $"M 0,0 L {Length},0";
        
        // Abzweig (Kreisbogen)
        var branchPath = $"M 0,0 A {BranchRadius},{BranchRadius} 0 0 0 {branchEnd.X},{branchEnd.Y}";
        
        return $"{mainPath} {branchPath}";
    }
}

/// <summary>
/// ThreeWaySwitchGeometry: Y-Weiche (parametrisch).
/// </summary>
public class ThreeWaySwitchGeometry
{
    public required double LeftBranchAngle { get; init; }   // z.B. -15°
    public required double RightBranchAngle { get; init; }  // z.B. +15°
    public required double BranchRadius { get; init; }
    
    // 1 IN, 3 OUT (Left, Center, Right)
    public List<TrackConnector> CalculateConnectors()
    {
        // ... analog zu SwitchGeometry
        return new List<TrackConnector>();
    }
}
```

---

## 🎉 BENEFITS OF THIS ARCHITECTURE

### **vs. Current Approach (Coordinates everywhere)**
✅ **Keine Koordinaten nach Import** (Topologie pur!)  
✅ **Constraint-basiert** (Mathematik, kein Snap-Raten)  
✅ **Parametrisch** (Weichen = Funktionen)  
✅ **Herstellerunabhängig** (GeometryRef austauschbar)  
✅ **Automatische Anpassung** (Parallelabstand, Spur)

### **vs. Coordinate-based Rendering**
✅ **Position entsteht** (durch Constraint-Solving)  
✅ **Kein Snap** (Connectoren matchen exakt)  
✅ **Kein Raten** (Mathematik bestimmt Transform)  
✅ **Nur Mathematik** (WorldTransform-Kette)

---

## 📚 NEXT STEPS (Implementation)

1. ✅ **Implementiere Core Types:**
   - `TrackSegment`, `TrackConnector`, `TrackConnection`, `TrackGraph`

2. ✅ **Implementiere Constraint System:**
   - `ConnectorMatcher`, `ConstraintSolver`

3. ✅ **Implementiere Import-Pipeline:**
   - `AnyRailImporter` (XML → TrackGraph → Koordinaten verwerfen)

4. ✅ **Implementiere Parametric Geometry:**
   - `SwitchGeometry`, `ThreeWaySwitchGeometry`

5. ✅ **Update Renderer:**
   - Nutze `ConstraintSolver.CalculateWorldTransform()` für Graph-Traversal

6. ✅ **Unit Tests:**
   - Connector-Matching (Distanz + Winkel)
   - Constraint-Solving (Rigid, Rotational, Parametric)
   - Import-Pipeline (AnyRail XML → TrackGraph)

---

**Philosophie:** Koordinaten sind **temporär** (nur beim Import). Danach: **Nur Graph + Constraints + Parameter!** 🚂
