---
description: 'TrackPlan Topologie und Graph-Struktur'
applyTo: '**/TrackPlan/**/*.cs'
---

# TrackPlan Topologie

## Graph-Struktur

```
TopologyGraph
├── Nodes[]       - Verbindungspunkte (Knoten im Graph)
├── Edges[]       - Gleisstücke (Kanten im Graph)
├── Endcaps[]     - Prellböcke/Gleisenden
├── Sections[]    - Blöcke/Abschnitte
├── Isolators[]   - Trennstellen
└── Constraints[] - Validierungsregeln
```

## TrackNode

Ein Knoten repräsentiert einen Verbindungspunkt zwischen Gleisen:

```csharp
class TrackNode
{
    Guid Id;
    List<TrackPort> Ports;  // Meist 1-2 Ports
}
```

## TrackEdge

Eine Kante repräsentiert ein physisches Gleisstück:

```csharp
class TrackEdge
{
    Guid Id;
    string TemplateId;                        // z.B. "G231"
    Dictionary<string, Endpoint> Connections; // Port → Node-Verbindung
    int? FeedbackPointNumber;                 // Rückmelder-Nummer
}
```

## TrackPort

Ein Port ist ein Anschlusspunkt an einem Gleis:

```csharp
class TrackPort
{
    string Id;  // "A", "B", "C" etc.
}
```

## TrackEnd (Template)

Definiert die lokale Position und Richtung eines Ports im Template:

```csharp
class TrackEnd
{
    string Id;           // "A", "B", "C"
    Point2D Position;    // Lokale Position relativ zum Gleis-Origin (mm)
    double AngleDeg;     // Ausgangswinkel (0° = rechts, zeigt nach außen)
}
```

## Verbindungen

Zwei Ports sind verbunden, wenn sie denselben TrackNode teilen:

```
  Edge1.Port[A] ──→ Node ←── Edge2.Port[B]
```

```csharp
bool AreConnected(Guid edge1Id, string port1Id, Guid edge2Id, string port2Id)
{
    var endpoint1 = edge1.Connections[port1Id];
    var endpoint2 = edge2.Connections[port2Id];
    
    return endpoint1.NodeId == endpoint2.NodeId;
}
```

## Verbindung herstellen

```csharp
void Connect(Guid edge1Id, string port1Id, Guid edge2Id, string port2Id)
{
    // 1. Alten Node von edge2.port2 finden
    var oldNode = FindNode(edge2.Connections[port2Id].NodeId);
    
    // 2. edge2.port2 auf edge1's Node umhängen
    var targetNodeId = edge1.Connections[port1Id].NodeId;
    edge2.Connections[port2Id] = new Endpoint(targetNodeId, port2Id);
    
    // 3. Alten Node löschen wenn verwaist
    if (IsNodeOrphaned(oldNode))
        Graph.Nodes.Remove(oldNode);
}
```

## Section (Block)

Ein Abschnitt gruppiert mehrere Gleise für Blocksteuerung:

```csharp
class Section
{
    Guid Id;
    string Name;           // "Block 1"
    string Function;       // "Track", "Station", "Siding"
    string Color;          // "#FF6B6B"
    HashSet<Guid> TrackIds; // Enthaltene Gleise
}
```

## Isolator

Markiert eine elektrische Trennstelle an einem Port:

```csharp
class Isolator
{
    Guid Id;
    Guid EdgeId;
    string PortId;
}
```

## Endcap (Prellbock)

Markiert ein Gleisende ohne Verbindungsmöglichkeit:

```csharp
class Endcap
{
    Guid Id;
    Guid EdgeId;
    string PortId;
}
```

## Constraints (Validierung)

```csharp
interface ITopologyConstraint
{
    IEnumerable<ConstraintViolation> Validate(TopologyGraph graph);
}

// Implementierungen:
// - DuplicateFeedbackPointNumberConstraint
// - GeometryConnectionConstraint
```

## Traversierung

```csharp
// Alle verbundenen Gleise finden (BFS)
HashSet<Guid> FindConnectedTracks(Guid startEdgeId)
{
    var visited = new HashSet<Guid>();
    var queue = new Queue<Guid>();
    queue.Enqueue(startEdgeId);
    
    while (queue.Count > 0)
    {
        var edgeId = queue.Dequeue();
        if (!visited.Add(edgeId)) continue;
        
        var edge = Graph.Edges.First(e => e.Id == edgeId);
        foreach (var (portId, endpoint) in edge.Connections)
        {
            // Finde andere Gleise am selben Node
            foreach (var other in Graph.Edges)
            {
                if (other.Id == edgeId) continue;
                foreach (var (otherPort, otherEndpoint) in other.Connections)
                {
                    if (otherEndpoint.NodeId == endpoint.NodeId)
                        queue.Enqueue(other.Id);
                }
            }
        }
    }
    
    return visited;
}
```
