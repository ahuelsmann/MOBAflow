# MOBAflow Editor Behavior Documentation
A complete reference for user interaction behavior in the MOBAflow track layout editor.

---

# 1. Overview

The editor defines how users interact with tracks, selections, groups, snapping, and transformations.

---

# 2. Selection Behavior

## 2.1 Single Selection
Click selects one edge.

## 2.2 Multi-Selection
Ctrl+Click adds/removes individual tracks to/from selection.

## 2.3 Shift+Click Path Selection
Shift+Click on a second track selects the shortest path between the first selected track and the clicked track using BFS traversal.

## 2.4 Triple-Click Connected Group
Triple-click selects all tracks transitively connected to the clicked track.

## 2.5 Rectangle Selection
Click and drag on empty canvas creates selection rectangle.
All tracks within rectangle are selected on release.

## 2.6 Esc to Clear
Pressing Esc clears all selections.

## 2.7 Ctrl+A Select All
Selects all tracks in the layout.

## 2.8 Group Selection
Multiple selected edges form a temporary group.
Group is visualized with green dashed bounding box.

---

# 3. Selection Bounding Box

When multiple tracks are selected:
- Green dashed rectangle surrounds all selected tracks
- Rotation handle (circle) appears above the box
- Connecting line from handle to box

---

# 4. Dragging Behavior

## 3.1 Single Edge Drag
Moves only the selected edge.

## 3.2 Group Drag
Moves all selected edges.

## 3.3 Snapping During Drag
Snap is evaluated continuously.

---

# 4. Rotation Behavior

- Adjusts RotationDeg  
- Recomputes port offsets  
- Does not modify topology  

---

# 5. Deletion Behavior

Deleting an edge:
- Removes the edge  
- Removes orphaned nodes  
- Updates topology  

---

# 6. Locking Behavior

Locked edges:
- Cannot be moved  
- Cannot be rotated  
- Still participate in topology  

---

# 7. Double-Click Behavior

## 7.1 Switch Toggle
Double-click on a switch track toggles its switch state.

---

# 8. Context Menu Actions

Right-click on a track opens context menu:
- Delete - Remove track from layout
- Rotate 15° - Rotate track by 15 degrees
- Mirror - Flip track horizontally
- Lock/Unlock - Toggle lock state
- Add/Remove Feedback Point - Set feedback sensor
- Toggle Isolator - Add/remove isolator at port
- Assign Section - Open section assignment dialog
- Properties - Open properties panel

---

# 9. Keyboard Shortcuts

- Esc - Clear selection
- Delete - Delete selected tracks
- Ctrl+A - Select all tracks
- Ctrl+Z - Undo (planned)
- Ctrl+Y - Redo (planned)
- Ctrl+S - Save layout
- G - Toggle grid visibility
- P - Toggle port visibility

---

# 10. Properties Panel

Shows:
- Track ID  
- Template ID  
- Position  
- Rotation  
- Feedback point  

---

# 11. Validation Panel

Shows:
- Geometry violations  
- Connection issues  
- Open ends  

---

# 12. Section Editor

Dialog for managing sections:
- Name input field
- Color palette (12 preset colors)
- Function dropdown (Block, Station, Siding, etc.)
- Track list with checkboxes
- Add/Remove tracks from section

---

# 13. Summary

The editor behavior system defines:
- How users interact with tracks  
- Selection modes (single, multi, path, connected group)
- How transformations work  
- How snapping integrates  
- How groups behave  
- Context menu actions
- Keyboard shortcuts
- Section management
- How validation is surfaced
