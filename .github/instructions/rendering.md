# MOBAflow Rendering System Documentation
A complete reference for how tracks, ports, and overlays are rendered.

---

# 1. Overview

Rendering converts geometric primitives into visual elements on the canvas.  
It uses world coordinates, rotation, scaling, and styling.

---

# 2. Rendering Pipeline

1. Compute world coordinates  
2. Apply rotation  
3. Apply display scale  
4. Draw geometry  
5. Draw ports  
6. Draw overlays  

---

# 3. Straight Track Rendering

Rendered as a Line:
- X1 = world.From.X
- Y1 = world.From.Y
- X2 = world.To.X
- Y2 = world.To.Y
- StrokeThickness = 3

---

# 4. Curve Rendering

Rendered using:
- PathGeometry
- PathFigure
- ArcSegment

Arc parameters:
- Center
- Radius
- StartAngle
- SweepAngle

---

# 5. Port Rendering

Ports are rendered as small circles:
- Width = 8
- Height = 8
- Fill = orange

Position:
- Centered on world port position

---

# 6. Selection Rendering

Selected tracks may show:
- Highlighted stroke
- Glow effect
- Bounding box
- Port highlights

---

# 7. Group Rendering

Groups may show:
- Shared bounding box
- Multi-selection highlight

---

# 8. Z-Order and Layers

Tracks may be assigned:
- Layers (background, foreground)
- Z-index within layer

---

# 9. Rendering Performance

Optimizations:
- Cache geometry
- Avoid unnecessary redraws
- Use lightweight primitives

---

# 10. Summary

The rendering system provides:
- Accurate visual representation
- Smooth interaction feedback
- Layered drawing
- Integration with geometry and topology
