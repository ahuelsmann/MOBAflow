# MOBAflow Snapping System Documentation
A complete reference for the snapping logic used in the MOBAflow track layout editor.

---

# 1. Overview

Snapping ensures that track pieces connect cleanly and realistically.  
It aligns ports, validates tangents, and updates topology.

---

# 2. Snap Conditions

A snap occurs when:

## 2.1 Distance Threshold
Ports must be within a small distance (typically 3 mm).

## 2.2 Tangent Compatibility
Tangents must be nearly opposite:
dot(tA, tB) < -0.95

## 2.3 Same Track System
Only tracks from the same catalog/system may snap.

---

# 3. Snap Process

1. User drags a track  
2. Compute world positions of all ports  
3. Compare with all placed ports  
4. Evaluate distance  
5. Evaluate tangent alignment  
6. If both match → snap  
7. Update topology graph  

---

# 4. Port Position Calculation

worldPos = track.Position + Rotate(localOffset, rotationDeg)

---

# 5. Tangent Alignment

Tangents are rotated but not translated.

Alignment check:
dot(tA, tB) < -0.95

---

# 6. Snap Result

- Track position is updated  
- Ports are connected  
- Nodes are merged  
- Topology graph is updated  

---

# 7. Snap Preview

The editor may show:
- Highlighted port
- Ghosted preview position
- Alignment indicators

---

# 8. Snap Cancellation

Snap is cancelled if:
- Distance exceeds threshold  
- Tangents are incompatible  
- User drags away  

---

# 9. Integration with Editor Behavior

Dragging:
- Snap is evaluated continuously

Rotation:
- Recomputes port positions and tangents

Deletion:
- Removes connections created by snapping

---

# 10. Summary

The snapping system ensures:
- Clean alignment  
- Realistic geometry  
- Correct topology  
- Smooth user experience  

It is tightly integrated with geometry, topology, and rendering.
