# Preserved editor contract

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

- Theme changes redraw the dot grid and rebuild the current graph using the same resource keys.
- Reset zoom targets the current control's CanvasScrollViewer: offsets 0/0 and zoom factor 1.0.
- A missing library never executes settings/layout commands; CanExecute=false never executes them.
- Summary strings and notifications retain all US1 cases in [spec.md](../spec.md).
- List and graph remain projections of the same ordered actions and selected action.
- Read-only trigger display and delay badges do not start workflows or control hardware.
- In the WF388-LIGHT-01 follow-up, generated graph text, icons, borders, ports, status and
  sequence/delay visuals use element-scoped ThemeResource setters in control-local styles.
  Selection preserves existing card backgrounds and accent border emphasis in both themes.
- Theme switching changes presentation only; it does not select, reorder, persist or execute an
  action. Actual Light/Dark verification is recorded for the tested head, never inherited from f5.

- From the 2026-11-07 review fixes: the canvas extent is the larger of 1600 x 1200 and the
  outermost node plus a 60 px margin, so every auto-laid-out or dragged node stays scrollable.
  A pressed card focuses the graph root so Alt+Up/Alt+Down/Delete reach the existing handler.
  Each card's automation name is "Step #N: <name or type>" and follows renames and reorders.
  The last action's delay is shown as a badge below its bottom port.

No REST, SignalR, Z21 or persistence contract is changed.
