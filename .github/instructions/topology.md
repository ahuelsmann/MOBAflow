# MOBAflow Topology System Documentation
A complete reference for the connectivity graph used in the MOBAflow track layout editor.

---

# 1. Overview

The topology system models how track pieces connect through ports and nodes.  
It ensures consistent connectivity, supports snapping, and enables validation.

---

# 2. Core Concepts

## 2.1 Nodes
A node represents a connection point shared by one or more ports.

Properties:
- NodeId (Guid)
- Ports: list of TrackPortRef

A node may contain:
- 1 port → open end
- 2 ports → normal connection
- 3+ ports → junction

## 2.2 Edges
An edge represents a placed track piece.

Properties:
- EdgeId (Guid)
- Template (TrackTemplate)
- Position (Point2D)
- RotationDeg (double)
- Connections: map PortId → Endpoint

## 2.3 Endpoints
An endpoint links a port to a node.

Endpoint:
- NodeId
- PortId

---

# 3. Graph Rules

## 3.1 Ports belong to nodes
Every port is assigned to exactly one node.

## 3.2 Nodes may contain multiple ports
This allows:
- switches
- crossings
- multi-track junctions

## 3.3 Edges never connect directly
Edges connect only through nodes.

## 3.4 Node merging
When two ports snap together:
- Their nodes merge
- All ports from both nodes are unified

---

# 4. Operations

## 4.1 Connect
Steps:
1. Identify nodes of both ports
2. If different → merge nodes
3. Update endpoints
4. Remove obsolete node

## 4.2 Disconnect
Steps:
1. Create new node for the port
2. Assign port to new node
3. Remove old node if empty

## 4.3 Query Connected Edges
Given an edge:
- Traverse nodes
- Collect all edges sharing those nodes

## 4.4 Query Open Ports
A port is open if:
- Its node contains only one port

---

# 5. Node Merging Algorithm

1. Let A and B be nodes  
2. Create new node C  
3. Add all ports from A and B to C  
4. Update all endpoints referencing A or B  
5. Delete A and B  

---

# 6. Topology Validation

## 6.1 Open Ends
Nodes with only one port.

## 6.2 Overloaded Nodes
Nodes with too many ports for the track system.

## 6.3 Impossible Angles
Ports connected with incompatible tangents.

## 6.4 Duplicate Connections
Two ports from the same edge in the same node.

---

# 7. Integration with Snapping

When snapping:
- Port positions are compared
- Tangents are validated
- If valid → topology is updated

---

# 8. Integration with Rendering

Nodes are not rendered directly, but:
- Ports are rendered visually
- Node structure determines connectivity overlays

---

# 9. Integration with Editor Behavior

Dragging:
- Does not modify topology until snap occurs

Deleting:
- Removes edge
- Removes orphaned nodes

Rotating:
- Does not affect topology

---

# 10. Sections

Sections group related tracks for operational purposes (blocks, stations, sidings).

## 10.1 Section Model

Properties:
- Id (Guid) - Unique identifier
- Name (string) - Display name for the section
- Color (string) - Hex color code for visualization (e.g., "#FF6B6B")
- Function (enum) - Operational function
- TrackIds (List<Guid>) - Track edge IDs belonging to this section

## 10.2 Section Functions

- Block - Standard block section
- Station - Station platform
- Siding - Passing siding
- Yard - Classification yard
- Staging - Staging area
- MainLine - Main line section
- Industrial - Industrial spur

## 10.3 Section Rules

- A track can belong to at most one section
- Section color is displayed as track overlay in editor
- Section labels appear at centroid of member tracks
- Section function determines operational behavior

---

# 11. Isolators

Isolators mark electrical isolation points at track ports.

## 11.1 Isolator Model

Properties:
- Id (Guid) - Unique identifier
- EdgeId (Guid) - The track edge containing the isolator
- PortId (string) - The port where the isolator is placed

## 11.2 Isolator Rules

- Placed at specific ports on track edges
- Indicates break in electrical conductivity
- Used for block detection and signaling
- Rendered as two triangles facing each other (▶◀)

---

# 12. Summary

The topology system provides:
- A robust graph model
- Clean port-to-node mapping
- Node merging logic
- Validation rules
- Section grouping for operations
- Isolator placement for electrical isolation
- Integration with snapping and rendering

It is the backbone of connectivity in MOBAflow.
