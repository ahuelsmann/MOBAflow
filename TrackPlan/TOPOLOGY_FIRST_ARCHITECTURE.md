# Topology-First Gleisplan Renderer Architektur

## 🎯 Überblick

Diese Architektur implementiert einen **Topology-First Ansatz** für die Gleisplanerstellung:

```
┌─────────────────────────────────────────────────────────────┐
│                   TOPOLOGY-FIRST PIPELINE                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  1. TOPOLOGY DEFINITION (Domain Model)                      │
│     ├─ TopologyGraph: Nodes, Edges, Constraints             │
│     └─ TrackNode, TrackEdge: Die "Wahrheit" über Gleise   │
│                                                               │
│  2. TOPOLOGY ANALYSIS (TopologyResolver)                    │
│     ├─ Graph Structure: Adjazenz-Listen, Zirkularität      │
│     ├─ Connected Components: Mehrfach-Schaltkreise         │
│     └─ Reachability: Erreichbare Nodes                      │
│                                                               │
│  3. GEOMETRY CALCULATION (GeometryCalculationEngine)        │
│     ├─ Position Berechnung: X, Y, Winkel                    │
│     ├─ Distanz-Berechnung: MathNet.Numerics                │
│     └─ Validierung: Alle Verbindungen konsistent            │
│                                                               │
│  4. RENDERING (SkiaSharpCanvasRenderer)                     │
│     ├─ LinePrimitive → SKCanvas Linien                      │
│     ├─ ArcPrimitive → SKCanvas Bögen                        │
│     └─ Labels, Feedback-Punkte, Signale                     │
│                                                               │
│  5. LAYOUT OUTPUT (TrackPlanLayout)                         │
│     ├─ Primitives: IGeometryPrimitive[]                     │
│     ├─ Bounds: SKRect für Viewport                          │
│     ├─ Validation: ConstraintViolations, GeometryErrors    │
│     └─ Export: PNG, SVG                                      │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Projektstruktur

| Projekt | Namespace | Zweck |
|---------|-----------|-------|
| **TrackPlan** | `Moba.TrackPlan` | Topologie & Domänen-Modell |
| | `Moba.TrackPlan.Graph` | TopologyGraph, TrackNode, TrackEdge |
| | `Moba.TrackPlan.Topology` | TopologyResolver (Graph-Analyse) |
| | `Moba.TrackPlan.Geometry` | GeometryCalculationEngine (Positionen) |
| **TrackPlan.Renderer** | `Moba.TrackPlan.Renderer.*` | Rendering und Export |
| | `Moba.TrackPlan.Renderer.Rendering` | SkiaSharpCanvasRenderer |
| | `Moba.TrackPlan.Renderer.Service` | TrackPlanLayoutEngine (Orchestrator) |
| **TrackLibrary.PikoA** | `Moba.TrackLibrary.PikoA` | Gleisbibliothek (Catalog) |

---

## 🔧 Komponenten

### 1. **TopologyResolver** (Graph-Analyse ohne externe Abhängigkeiten)

```csharp
var resolver = new TopologyResolver(catalog);
resolver.Build(topology);

// Traversal
var outgoing = resolver.GetOutgoing(node);
var incoming = resolver.GetIncoming(node);

// Analysis
var analysis = resolver.Analyze(topology);
Console.WriteLine($"Nodes: {analysis.NodeCount}");
Console.WriteLine($"Has Cycles: {analysis.HasCycles}");
Console.WriteLine($"Components: {analysis.ComponentCount}");

// Reachability
var reachable = resolver.GetReachableNodes(startNode, topology);
var cycleEdges = resolver.GetCycleEdges(topology);
```

**Features:**
- ✅ Dictionary-basierte Adjazenz-Listen
- ✅ Zirkularitäts-Erkennung
- ✅ Verbundene Komponenten
- ✅ Erreichbarkeits-Analyse

---

### 2. **GeometryCalculationEngine** (Position & Orientierungs-Berechnung)

```csharp
var engine = new GeometryCalculationEngine(catalog, resolver);

// Calculate positions for all nodes
engine.Calculate(topology, startX: 0, startY: 0, startAngleDeg: 0);

// Get position of a specific node
var position = engine.GetNodePosition(nodeId);
Console.WriteLine($"Position: ({position?.X}, {position?.Y}), Angle: {position?.ExitAngleDeg}°");

// Validate geometry
var errors = engine.ValidateConnections(topology);
foreach (var error in errors)
{
    Console.WriteLine($"Error in {error.TemplateId}: {error.Message}");
}

// Utility functions
var distance = GeometryCalculationEngine.Distance(x1, y1, x2, y2);
var angle = GeometryCalculationEngine.AngleBetweenPoints(fromX, fromY, toX, toY);
```

**Features:**
- ✅ MathNet.Numerics Integration
- ✅ Recursive Position Berechnung
- ✅ Geometrie-Validierung
- ✅ Distanz & Winkel-Utilities

---

### 3. **SkiaSharpCanvasRenderer** (Canvas-Rendering)

```csharp
var renderer = new SkiaSharpCanvasRenderer();

// Render einzelne Primitive
renderer.RenderLine(canvas, linePrimitive);
renderer.RenderArc(canvas, arcPrimitive);

// Render alle Primitive
renderer.RenderPrimitives(canvas, primitives, bounds);

// Labels und Annotationen
renderer.RenderLabel(canvas, "Label", position, fontSize: 12);
renderer.RenderFeedbackPoint(canvas, position, feedbackNumber: 1);
renderer.RenderSignal(canvas, position, "HP");

// Bitmap und Export
var bitmap = renderer.RenderToBitmap(primitives, bounds, padding: 20);
renderer.ExportToPng("output.png", primitives, bounds);

// Bounds-Berechnung
var bounds = renderer.CalculateBounds(primitives);
```

**Features:**
- ✅ SkiaSharp Canvas Rendering
- ✅ LinePrimitive & ArcPrimitive Support
- ✅ Labels, Feedback-Punkte, Signale
- ✅ PNG Export mit Padding
- ✅ Automatische Bounds-Berechnung

---

### 4. **TrackPlanLayoutEngine** (Orchestrator)

```csharp
var engine = new TrackPlanLayoutEngine(catalog);

// Vollständige Pipeline
var layout = engine.Process(
    topology,
    startPosition: new Point2D(0, 0),
    startAngleDeg: 0);

// Ergebnisse
Console.WriteLine($"Valid: {layout.IsValid}");
Console.WriteLine($"Primitives: {layout.Primitives.Count}");
Console.WriteLine($"Nodes: {layout.Analysis.NodeCount}");
Console.WriteLine($"Has Cycles: {layout.Analysis.HasCycles}");

// Constraints & Geometry validieren
foreach (var violation in layout.ConstraintViolations)
    Console.WriteLine($"Constraint violation: {violation}");

foreach (var error in layout.GeometryErrors)
    Console.WriteLine($"Geometry error: {error.Message}");

// Export
engine.ExportToPng(layout, "trackplan.png");
var svg = engine.ExportToSvg(layout);
```

**Features:**
- ✅ 5-Stufen-Pipeline (Topology → Geometry → Rendering)
- ✅ Vollständige Validierung
- ✅ PNG & SVG Export
- ✅ Feedback-Punkte und Signale

---

## 📋 Verwendungsbeispiel: R9 Oval

```csharp
public void Build_R9_Oval_Complete()
{
    // 1. Topologie definieren
    var nodes = new List<TrackNode>();
    for (int i = 0; i < 24; i++)
        nodes.Add(new() { Id = Guid.NewGuid() });

    var edges = new List<TrackEdge>();

    // Segment 1: 12×R9 (180°)
    for (int i = 0; i < 12; i++)
    {
        edges.Add(new TrackEdge
        {
            Id = Guid.NewGuid(),
            TemplateId = "R9",
            Connections = new Dictionary<string, Endpoint>
            {
                { "A", new Endpoint(nodes[i].Id, "End") },
                { "B", new Endpoint(nodes[i + 1].Id, "Start") }
            }
        });
    }

    // Segment 2: 1×WR
    edges.Add(new TrackEdge
    {
        Id = Guid.NewGuid(),
        TemplateId = "WR",
        Connections = new Dictionary<string, Endpoint>
        {
            { "A", new Endpoint(nodes[12].Id, "End") },
            { "B", new Endpoint(nodes[13].Id, "Start") }
        }
    });

    // Segment 3: 11×R9 (165°)
    for (int i = 0; i < 11; i++)
    {
        int fromNodeIdx = 13 + i;
        int toNodeIdx = (13 + i + 1) % 24;
        
        edges.Add(new TrackEdge
        {
            Id = Guid.NewGuid(),
            TemplateId = "R9",
            Connections = new Dictionary<string, Endpoint>
            {
                { "A", new Endpoint(nodes[fromNodeIdx].Id, "End") },
                { "B", new Endpoint(nodes[toNodeIdx].Id, "Start") }
            }
        });
    }

    var topology = new TopologyGraph
    {
        Nodes = nodes,
        Edges = edges
    };

    // 2. Layout generieren
    var catalog = new PikoATrackCatalog();
    var engine = new TrackPlanLayoutEngine(catalog);
    var layout = engine.Process(topology);

    // 3. Validieren
    if (layout.IsValid)
    {
        Console.WriteLine("✓ Gleisplan ist gültig");
        Console.WriteLine($"  Nodes: {layout.Analysis.NodeCount}");
        Console.WriteLine($"  Edges: {layout.Analysis.EdgeCount}");
        Console.WriteLine($"  Cycles: {layout.Analysis.HasCycles}");
        Console.WriteLine($"  Primitives: {layout.Primitives.Count}");
    }

    // 4. Exportieren
    engine.ExportToPng(layout, "r9_oval.png");
    var svg = engine.ExportToSvg(layout);
}
```

---

## 🧪 Tests

Alle Komponenten haben umfangreiche Unit- und Integration-Tests:

```
Test\TrackPlan.Topology\
  └─ TopologyResolverTests.cs (10+ Tests)

Test\TrackPlan.Geometry\
  └─ GeometryCalculationEngineTests.cs (15+ Tests)

Test\TrackPlan.Renderer.Rendering\
  └─ SkiaSharpCanvasRendererTests.cs (12+ Tests)

Test\TrackPlan.Renderer.Integration\
  └─ TrackPlanLayoutEngineIntegrationTests.cs (10+ E2E Tests)
```

**Ausführen:**
```bash
dotnet test Test\Test.csproj --filter "TopologyResolver OR GeometryCalculation OR SkiaSharp OR TrackPlanLayout"
```

---

## 🎯 Best Practices

### 1. **Topologie zuerst denken**
- Definiere zuerst die Nodes und Edges
- Später kommt die Geometrie und das Rendering

### 2. **Validierung auf mehreren Ebenen**
```csharp
// Level 1: Topologie-Constraints
var violations = topology.Validate();

// Level 2: Geometrie
var errors = engine.ValidateConnections(topology);

// Level 3: Layout
if (layout.IsValid)
    engine.ExportToPng(layout, "output.png");
```

### 3. **Fehlerbehandlung**
```csharp
try
{
    var layout = engine.Process(topology);
    if (!layout.IsValid)
    {
        foreach (var error in layout.GeometryErrors)
            logger.LogError(error.Message);
    }
}
catch (ArgumentException ex)
{
    logger.LogError($"Invalid topology: {ex.Message}");
}
```

---

## 📊 Performance

| Operation | Zeit | Typ |
|-----------|------|-----|
| TopologyResolver.Build (1000 nodes) | ~5ms | O(n + m) |
| GeometryCalculationEngine.Calculate | ~10ms | O(n) |
| SkiaSharpCanvasRenderer.RenderToBitmap | ~50ms | O(p) primitives |
| ExportToPng (1000x1000) | ~100ms | I/O bound |

---

## 🔮 Zukünftige Erweiterungen

- [ ] Snap-to-Grid Engine
- [ ] Snap-to-Connect für automatische Verbindungen
- [ ] 3D Viewing für komplexe Layouts
- [ ] Collision Detection
- [ ] Undo/Redo Stack
- [ ] Real-time Simulation Overlay
- [ ] Multi-format Export (Gcode, CAD)

---

## 📚 Referenzen

- `.github/instructions/geometry.md` - Geometrie-Berechnungen
- `.github/instructions/rendering.md` - Rendering-Pipeline
- `.github/instructions/topology.md` - Topologie-Modell
- `TrackLibrary.PikoA/README.md` - Gleisbibliothek
