# MOBAflow track plan system

The track plan is the physical layout editor used by MOBAflow Desktop. Its
persisted representation is `Domain.TrackPlanDocument`; the WinUI editor adapts
that model to the Piko A geometry and neutral renderer libraries.

## Components

| Project | Responsibility |
| --- | --- |
| `Domain` | Persisted `TrackPlanDocument`, segment and connection data |
| `TrackLibrary.Base` | Track-piece abstractions and shared path geometry |
| `TrackLibrary.PikoA` | Piko A catalog, editable plan, snapping and spatial lookup |
| `TrackPlan.Renderer` | Platform-neutral rendering and SVG export primitives |
| `Backend/Service/TrackPlan` | Projects feedback and timer state onto the track plan |
| `MOBAflow/View/TrackPlanPage` | WinUI editor, gestures, commands and Win2D drawing |

The neutral renderer must not depend on a concrete track catalog. Catalog
geometry flows from `TrackLibrary.PikoA` through adapters into the renderer.

## Current editor features

- drag track pieces from the toolbox;
- move and rotate placed segments;
- snap compatible ports together;
- disconnect selected connections;
- assign feedback inputs;
- validate topology and segment constraints;
- Undo/Redo;
- zoom, fit-to-view and reset zoom;
- import AnyRail XML; and
- export SVG.

The currently included catalog is Piko A. Roco Line, Tillig and Märklin
catalogs are not currently implemented.

## Persistence

The active editor plan is synchronized with the selected project through
`TrackPlanSolutionBinder`. `TrackPlanDocumentMapper` translates between the
editable Piko A representation and the platform-neutral domain document.

Do not persist WinUI or Win2D objects. Coordinates, catalog identifiers,
rotations, ports and connections belong in the domain document.

## Runtime projection

Track feedback is resolved by `ITrackFeedbackLookup` and projected into
`RailroadState`. This keeps live occupancy/feedback state out of the persisted
track geometry while allowing the UI renderer to highlight current layout
state.

## Editor shortcuts

| Key | Action |
| --- | --- |
| Delete / Backspace | Delete selection |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| Ctrl++ / Ctrl+- | Zoom in / out |
| Ctrl+0 | Fit plan to viewport |
| Ctrl+1 | Reset zoom |
| R | Disconnect selected connection |

## Extending the catalog

New track systems should provide their own catalog and geometry implementation
without adding vendor-specific types to `TrackPlan.Renderer`. Add unit tests for
geometry, snapping, serialization round trips and renderer integration.
