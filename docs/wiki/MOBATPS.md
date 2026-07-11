# MOBAtps - Track Plan System

> Status note (2026-07): This page formerly described planned `TrackPlan`,
> `TrackPlan.Domain`, `TrackPlan.Geometry`, and `TrackPlan.Editor` projects.
> Those projects are not part of the active solution. The implemented editor is
> currently `Domain` + `TrackLibrary.Base` + `TrackLibrary.PikoA` +
> `TrackPlan.Renderer` + `SharedUI` + `MOBAflow`. The target architecture is
> being introduced evolutionarily through the library-neutral contracts in
> `Domain/TrackLayout.cs`; this document must not be used as evidence that the
> historical projects still exist.

> Last Updated: 2026-01-09

## Overview

MOBAtps (Track Planner System) is MOBAflow's visual track layout
editor. It provides a full-featured drag-and-drop interface for
designing model railroad layouts.

## Architecture

```text
TrackPlan (Domain Layer)
    - TopologyGraph (Nodes + Edges + Endcaps)
    - Constraints (Validation rules)
    - ITrackCatalog (Track library interface)
        |
        v
TrackPlan.Renderer (Geometry Layer)
    - Layout Engines
    - Geometry Primitives
    - World Transforms
        |
        v
TrackPlan.Editor (Application Layer)
    - ViewModels
    - Commands
    - Controllers
    - Services
        |
        v
TrackLibrary.PikoA (Track Library)
    - Piko A-Gleis Templates
    - Article Metadata
    - Physical Constants
```

## Projects

- **`TrackPlan`**: Domain models (`TopologyGraph`, constraints)
- **`TrackPlan.Renderer`**: Geometry calculation and layout engines
- **`TrackPlan.Editor`**: Editor view models, commands, services
- **`TrackLibrary.PikoA`**: Piko A-Gleis track templates

## Domain Model

### TopologyGraph

The core data structure representing a track layout:

```csharp
TopologyGraph
  - Nodes: List<TrackNode>      // Connection points
  - Edges: List<TrackEdge>      // Track pieces
  - Endcaps: List<Endcap>       // Open ends
  - Constraints: List<ITopologyConstraint>
```

### TrackNode

A connection point in the graph:

```csharp
TrackNode
  - Id: Guid
  - Ports: List<TrackPort>      // Available connection ports
```

### TrackEdge

A track piece connecting nodes:

```csharp
TrackEdge
  - Id: Guid
  - TemplateId: string          // e.g., "G231", "BWL"
  - Connections: Dictionary<string, Endpoint>
  - FeedbackPointNumber: int?   // Z21 feedback address
```

## Track Libraries

Track systems are modular. Each manufacturer's track system is a
separate library implementing `ITrackCatalog`:

### ITrackCatalog Interface

```csharp
public interface ITrackCatalog
{
    string SystemId { get; }           // e.g., "PikoA"
    string SystemName { get; }         // e.g., "Piko A-Gleis"
    string Manufacturer { get; }
    string Scale { get; }              // e.g., "H0"
    
    IReadOnlyList<TrackTemplate> Templates { get; }
    IEnumerable<TrackTemplate> Straights { get; }
    IEnumerable<TrackTemplate> Curves { get; }
    IEnumerable<TrackTemplate> Switches { get; }
    
    TrackTemplate? GetById(string id);
}
```

### Available Libraries

- **TrackLibrary.PikoA**: Active. Templates include `G231`, `G119`,
  `G62`, `G56`, `G31`, `R1-R9`, `BWL`, `BWR`, `K30`
- **TrackLibrary.RocoLine**: Planned
- **TrackLibrary.Tillig**: Planned
- **TrackLibrary.Maerklin**: Planned

### Creating a New Track Library

1. Create new project `TrackLibrary.{Name}`
2. Reference `TrackPlan` project
3. Implement `ITrackCatalog` interface
4. Define templates in separate files:
   - `Template/StraightTemplates.cs`
   - `Template/CurveTemplates.cs`
   - `Template/SwitchTemplates.cs`
5. Add metadata in `Metadata/` folder

## Editor Features

### TrackPlanEditorPage2

The main editor UI in WinUI:

- **Toolbox**: Drag templates from a categorized list
- **Canvas**: Drop and arrange tracks in a `3000x2000` work area
- **Properties**: Edit selected track properties
- **Grid Snap**: Align to the `50mm` grid
- **Port Snap**: Auto-connect nearby track ends
- **Zoom/Pan**: Navigate large layouts
- **Validation**: Check for constraint violations
- **Theme Support**: Light and dark mode colors

### Keyboard Shortcuts

- **Delete**: Remove selected track
- **Ctrl+N**: New plan
- **Ctrl+S**: Save plan
- **Ctrl+Z**: Undo (planned)

## Constraints

Validation rules that check graph consistency:

- **`DuplicateFeedbackPointNumberConstraint`**: No duplicate feedback
  addresses
- **More planned**

## Integration with Domain.Project

The track plan is stored in `Domain.Project.TrackPlan`:

```csharp
public class Project
{
    // ... other properties
    
    /// <summary>
    /// Track layout for this project (MOBAtps).
    /// </summary>
    public TopologyGraph? TrackPlan { get; set; }
}
```

## Future Enhancements

- [ ] More track libraries (Roco, Tillig, Maerklin)
- [ ] AnyRail import to TopologyGraph
- [ ] Undo/Redo support
- [ ] Copy/Paste tracks
- [ ] Track grouping
- [ ] Layer management
- [ ] Export to image/PDF
