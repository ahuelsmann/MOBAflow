# RF-13 / Issue #91: Track-plan editor extraction

## Outcome

Move track-plan editing behavior out of `TrackPlanPage` into a platform-neutral
editor coordinator and `TrackPlanViewModel`. Keep WinUI code-behind limited to
pointer and keyboard adaptation, pointer capture, coordinate conversion, canvas
layout, and drawing API calls.

## Evidence and current boundaries

- RF-06/#90 is merged on `main` at `a9dd6d4c`.
- `TrackPlanPage.xaml.cs` contains 1,951 lines and
  `TrackPlanPage.EditorFeatures.cs` contains 530 lines.
- `EditableTrackPlan`, `TrackPlanInteractionService`, `SelectionService`, and
  `UndoRedoService<T>` already provide partial platform-neutral boundaries.
- The page still owns selection identity, snapshot history, validation messages,
  insertion completion, implicit-connection healing, continuous rotation
  mutations, and direct control command wiring.
- The page retains obsolete hit-test helpers even though
  `TrackPlanInteractionService.HitTest` is authoritative.
- `TrackPlanSolutionBinder` remains the single solution persistence boundary.

## Work packages

### WP1 - Characterize and coordinate editor state

- Add a platform-neutral `TrackPlanEditorService` under
  `Backend/Service/TrackPlan`.
- Make it own selection coordination, document snapshots, bounded undo/redo,
  dirty-state transitions, validation, and all `EditableTrackPlan` mutations
  used by the editor.
- Preserve `TrackPlanSolutionBinder` as the solution load/save owner.
- Add unit tests for selection, insertion, movement, snap connection,
  disconnection, deletion, rotation, validation, undo/redo, restore, and dirty
  state.

### WP2 - Move commands and observable state into the ViewModel

- Make `TrackPlanViewModel` depend on the editor service.
- Expose selected-track projection, command availability, validation status,
  history state, and editor status text.
- Route delete, disconnect, rotate, feedback assignment, undo, redo, and
  validation through ViewModel commands.
- Keep all user-visible strings in English.

### WP3 - Thin the WinUI page

- Bind toolbar actions to ViewModel commands where WinUI event adaptation is not
  required.
- Use page handlers only to translate pointer/keyboard input, manage capture,
  convert coordinates, and update canvas visuals.
- Remove direct `EditableTrackPlan` mutation calls and duplicate hit-test,
  topology, validation, and history behavior from code-behind.
- Preserve snapping, rigid-group movement, toolbox insertion, rotation drag,
  selection, feedback assignment, undo/redo, and validation behavior.

### WP4 - Architecture protection and validation

- Add source-level architecture tests that reject direct editor-model mutation,
  validation logic, or feature command ownership in TrackPlanPage code-behind.
- Run focused editor/ViewModel tests, complete `Test` suites, Release analyzer
  ratchets, Windows desktop build, formatting checks, and secret scans.
- Attempt local Sonar analysis against `github/main`; if the tenant blocks
  agentic local analysis, record the exact capability limitation on the draft
  PR.
- Create a draft PR, require all remote checks green, and require zero Sonar
  `OPEN` or `CONFIRMED` PR findings before review and merge.

## Non-goals

- No interlocking behavior from Issue #34.
- No signal-box property editing from RF-14/#92.
- No visual redesign or broad accessibility work from RF-16.
- No changes to feature issues #30 through #36 beyond the refactored boundary
  consumed later by Issue #34.
- Do not start MOBAflow or MOBAsmart.

## Risks and mitigations

- **Gesture history drift:** capture one snapshot per completed pointer gesture
  and test no-op gestures.
- **Selection divergence:** keep `SelectionService` authoritative and remove the
  page-owned selected identifier.
- **Persistence duplication:** leave solution persistence in
  `TrackPlanSolutionBinder`; the editor service only exposes dirty state and
  document restoration.
- **Source-generator command regressions:** build both cross-platform and
  Windows targets and keep command state tests.
- **Large diff:** keep commits aligned with WP1 through WP4 and remove superseded
  code in the same work package that replaces it.

## Completion

RF-13 is complete when Issue #91 acceptance criteria pass, the draft PR has all
required checks green, Sonar reports zero open or confirmed PR findings, the PR
is merged to `main`, and this completed plan is deleted in the final branch
commit.
