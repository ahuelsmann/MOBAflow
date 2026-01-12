# MOBAflow Geometry System Documentation
A comprehensive reference for all geometric concepts, transformations, snapping rules, rendering behavior, topology integration, and validation logic used in the MOBAflow track layout editor.

---

# 1. Coordinate System

## 1.1 Units
- All geometric values are expressed in millimeters (mm).
- Display coordinates are scaled using DisplayScale.

## 1.2 Origin and Axes
- Origin: top-left corner of the canvas
- Positive X: right
- Positive Y: down

## 1.3 Rotation
- Rotation is expressed in degrees, clockwise
- Internally converted to radians for trigonometric operations

---

# 2. Geometric Primitives

## 2.1 LinePrimitive
Represents a straight track segment.

Properties:
- From (Point2D)
- To (Point2D)

Derived values:
- Length
- Tangent vector
- Normal vector

## 2.2 ArcPrimitive
Represents a circular arc.

Properties:
- Center (Point2D)
- Radius (double)
- StartAngleRad (double)
- SweepAngleRad (double)

Derived values:
- Start point
- End point
- Tangent at start/end
- Arc length

---

# 3. Track Geometry

## 3.1 GeometryKind
- Straight
- Curve
- Switch

## 3.2 Straight Geometry
Defined by LengthMm.

Ports:
- A = (0, 0)
- B = (LengthMm, 0)

## 3.3 Curve Geometry
Defined by RadiusMm and SweepAngleDeg.

Ports:
- A = (0, 0)
- B = computed using radius and sweep angle

## 3.4 Switch Geometry
Defined by LengthMm, SweepAngleDeg, IsSymmetric.

Switches may have:
- 2 ports (simple)
- 3 ports (branching)

---

# 4. Port System

## 4.1 Port Definition
A port defines:
- Local offset (Point2D)
- Local tangent (Vector2D)
- Port ID (e.g., A, B)

## 4.2 Local Coordinates
Ports are defined in unrotated local track space.

## 4.3 World Coordinates
Computed as:
worldPos = track.Position + Rotate(localOffset, rotationDeg)

## 4.4 Tangent Transformation
Tangents are rotated but not translated.

---

# 5. Transformations

## 5.1 Rotation Matrix
Clockwise rotation:
x' = x * cos(θ) – y * sin(θ)
y' = x * sin(θ) + y * cos(θ)

## 5.2 Translation
Applied after rotation.

## 5.3 Scaling
Applied only for rendering:
screenX = worldX * DisplayScale
screenY = worldY * DisplayScale

---

# 6. Tangents and Directionality

## 6.1 Straight Tangents
Normalized vector from From → To.

## 6.2 Arc Tangents
Computed using derivative of circle:
dx/dθ = –r * sin(θ)
dy/dθ =  r * cos(θ)

## 6.3 Opposing Tangents
Used for snap validation:
dot(tA, tB) < –0.95

---

# 7. Snapping System

## 7.1 Snap Conditions
A snap occurs when:
1. Ports are within a distance threshold
2. Tangents are opposite
3. Track systems match

## 7.2 Snap Distance
Typical threshold: 3 mm.

## 7.3 Snap Process
1. User drags track
2. Compute world positions of all ports
3. Compare with all placed ports
4. If match → snap
5. Update topology graph

## 7.4 Snap Result
- Track position updated
- Ports connected
- Nodes merged

---

# 8. Topology Integration

## 8.1 Nodes
Represent connection points.

## 8.2 Edges
Represent placed track pieces.

## 8.3 Port-to-Node Mapping
Each port belongs to exactly one node.

## 8.4 Node Merging
Occurs when two ports snap together.

---

# 9. Rendering

## 9.1 Rendering Pipeline
1. Compute world coordinates
2. Apply rotation
3. Apply scaling
4. Draw geometry
5. Draw ports
6. Draw overlays

## 9.2 Straight Rendering
Rendered as a Line.

## 9.3 Curve Rendering
Rendered using PathGeometry + ArcSegment.

## 9.4 Port Rendering
Small circles centered on port world coordinates.

---

# 10. Validation Rules

## 10.1 Geometry Validation
- Invalid sweep angles
- Negative radius
- Zero-length tracks

## 10.2 Topology Validation
- Open ends
- Overlapping nodes
- Impossible angles

## 10.3 Layout Validation
- Overlapping tracks
- Self-intersections

---

# 11. Utility Functions

## 11.1 Rotate Point
Rotate(Point2D p, double deg)

## 11.2 Compute Arc Endpoint
ComputeArcEnd(double radius, double sweepDeg)

## 11.3 Compute Tangent
ComputeTangent(TrackGeometry geom, string portId)

---

# 12. Group Movement (Geometry Implications)

## 12.1 Groups vs. Connections
- Connections do not imply group movement
- Groups are explicit user-defined sets of edges

## 12.2 Delta Movement
edges[edgeId].Position += delta

---

# 13. Editor Behavior (Geometry-Relevant Parts)

## 13.1 Dragging
- Moves a single edge unless grouped
- Snapping may adjust final position

## 13.2 Rotation
- Adjusts RotationDeg
- Recomputes port offsets

## 13.3 Deletion
- Removes edge
- Removes orphaned nodes

---

# 14. Rendering Examples

## 14.1 Straight Track
Line from (x1, y1) to (x2, y2)

## 14.2 Port Rendering
Ellipse centered at world port position

---

# 15. Future Extensions
- 3D elevation
- Superelevation
- Parametric curves
- Multi-radius curves
- Dynamic port generation

---

# 16. Summary
This document defines the complete geometry system used in MOBAflow, including coordinate system, primitives, ports, transformations, tangents, snapping, rendering, validation, topology integration, group movement, and editor behavior.
