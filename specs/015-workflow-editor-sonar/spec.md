# Workflow editor quality corrections and acceptance preparation

## Governance and Traceability

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378
**Spec Kit**: Required
**Status**: Bounded correction phase of existing Draft PR #388

## Context and scope

The graphical workflow editor already exists at commit d6aa332c10036172965845987143084fa881e66d.
This specification records the newly assigned correction phase, not a retrospective claim that
the original implementation followed this plan. No existing Issue #378 Spec Kit package was found.
The older `specs/002-workflow-action-sequences/` package belongs to Issue #132 and is not repurposed.

The approved Option A remains a visual projection of the ordered action list. This phase fixes
13 OPEN/CONFIRMED Sonar findings and prepares practical acceptance without starting applications.
It adds no feature, dependencies, runtime command, domain schema, migration or execution path.

## User stories and acceptance

### US1 - Preserve editor behavior while clearing quality findings (P1)

As a workflow author, I retain the existing selection, theme refresh, reset zoom, action status
and parameter summaries after the implementation becomes easier to maintain.

1. The theme callback retains the WinUI event contract and rebuilds the same visuals.
2. Valid, invalid-with-message and invalid-without-message status text remains identical.
3. Announcement summaries retain empty-message handling and the 28-character truncation boundary.
4. Journey-stop summaries retain next-stop, unset-target and eight-character station-ID branches.
5. Script summaries retain missing-path, missing-arguments and cross-platform filename branches.
6. Nullable command checks execute only when the library exists and CanExecute returns true.

### US2 - Prepare an executable acceptance handoff (P2)

As the delivery coordinator, I receive a commit-bound build location, verified startup constraints,
numbered Graph/List and Light/Dark steps, expected results and evidence fields before any launch.
The handoff states unverified startup isolation explicitly; it does not claim a safe configuration
or passed UI acceptance without evidence. Apps, API hosts, workflows and hardware are not started.

## Functional requirements

- **FR-001**: Correct the 13 current findings in the canvas and the three affected action ViewModels,
  without suppression, baseline increases, weakened assertions or behavior changes.
- **FR-002**: Preserve event bindings, instance-specific zoom state, command null behavior and all
  ThemeResource key values while simplifying callbacks, conditions and repeated literals.
- **FR-003**: Preserve the observable summary cases in US1 with meaningful regression coverage.
- **FR-004**: Prepare the US2 acceptance package; actual UI execution remains a separate open gate.
- **FR-005**: Keep the existing branch/worktree/Draft PR, one writer, protected operator solution,
  current CI/Sonar checks and explicit source/process handoff boundaries.

## Success criteria and edges

- **SC-001**: Relevant tests and WinUI compilation/analyzer checks pass; each filtered run executes tests.
- **SC-002**: The resulting PR commit has green required CI and a green Sonar check plus zero
  OPEN/CONFIRMED issues. A green check alone does not meet this criterion.
- **SC-003**: Documentation records the actual build and remaining manual acceptance separately.

Edges include null library, false CanExecute, whitespace/empty strings, messages of 28 and 29
characters, empty/valid target IDs, both file separators and absent script arguments.
Platforms are portable SharedUI and Windows WinUI. JSON, defaults, public API, Z21 and persisted
layout behavior remain unchanged. `MOBAflow/solution.json` is excluded from all handling.
