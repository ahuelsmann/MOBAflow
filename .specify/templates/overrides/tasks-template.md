---

description: "MOBAflow task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`

**Prerequisites**: `spec.md` and `plan.md` are required; use `research.md`, `data-model.md`, `contracts/`, and `quickstart.md` when present.

**Tests**: Test tasks are MANDATORY for every new or changed behavior. Platform UI work also needs testable logic below the UI layer and focused manual checks where automation is impractical.

**Organization**: Group tasks by user story so each story remains independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: May run in parallel because it touches different files and has no unmet dependency.
- **[Story]**: Maps the task to a user story, for example `[US1]`.
- Every task MUST name exact repository paths.

## Path Conventions

- Domain and shared logic: `Domain/`, `Common/`, `Backend/`, `SharedUI/`
- Platform hosts: `MOBAflow/`, `MOBAsmart/`, `MOBApi/`, `MOBAdisplay/`
- Supporting projects: `Sound/`, `TrackLibrary.Base/`, `TrackLibrary.PikoA/`, `TrackPlan.Renderer/`
- Tests: `Test/`
- Feature documents: `specs/[###-feature-name]/`

<!-- Replace every sample below with concrete tasks. Do not retain placeholders. -->

## Phase 1: Analysis and Setup

**Purpose**: Confirm affected code, existing patterns, platform scope, and test locations.

- [ ] T001 Inspect the exact affected implementations and repository instructions
- [ ] T002 Identify regression tests and platform-specific validation commands

---

## Phase 2: Foundational Work

**Purpose**: Complete shared prerequisites that block all user stories.

- [ ] T003 [P] Add or update shared contracts/models in [exact path]
- [ ] T004 [P] Add failing tests for shared behavior in `Test/[exact path]`
- [ ] T005 Implement the shared behavior in [exact path]

**Checkpoint**: Shared behavior passes focused tests and preserves compatibility.

---

## Phase 3: User Story 1 - [Title] (Priority: P1)

**Goal**: [User value delivered]

**Independent Test**: [How this story is verified on its own]

### Tests for User Story 1

- [ ] T006 [P] [US1] Add a failing unit/integration test in `Test/[exact path]`
- [ ] T007 [P] [US1] Define required platform/manual checks in `specs/[###-feature-name]/quickstart.md`

### Implementation for User Story 1

- [ ] T008 [US1] Implement domain/backend behavior in [exact path]
- [ ] T009 [US1] Adapt ViewModel/API behavior in [exact path]
- [ ] T010 [US1] Adapt platform UI/host behavior in [exact path]
- [ ] T011 [US1] Run focused tests and independently validate the acceptance scenarios

**Checkpoint**: User Story 1 is independently functional and tested.

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [User value delivered]

**Independent Test**: [How this story is verified on its own]

### Tests for User Story 2

- [ ] T012 [P] [US2] Add a failing unit/integration test in `Test/[exact path]`

### Implementation for User Story 2

- [ ] T013 [US2] Implement behavior in [exact path]
- [ ] T014 [US2] Run focused tests and independently validate the acceptance scenarios

**Checkpoint**: User Stories 1 and 2 are independently functional and tested.

---

## Final Phase: Validation and Documentation

- [ ] TXXX Run `dotnet test Test/Test.csproj`
- [ ] TXXX [P] Run exact affected project builds, including required Windows/Android builds on matching hosts
- [ ] TXXX [P] Validate Light and Dark themes for changed UI
- [ ] TXXX [P] Verify compatibility for JSON, configuration defaults, APIs, protocols, and persisted layouts as applicable
- [ ] TXXX Update root documentation, changelog, and public API XML documentation as applicable
- [ ] TXXX Re-run the Constitution Check and record any approved exceptions

## Dependencies and Execution Order

- Analysis precedes foundational work.
- Foundational work blocks user stories that depend on shared contracts or state.
- Within a story, failing tests precede implementation; shared models precede services; services precede adapters and UI.
- Tasks marked `[P]` may run concurrently only when they do not edit the same files or depend on unfinished work.
- Each story MUST reach its checkpoint before it is reported complete.

## Notes

- Reuse existing helpers, services, DI registrations, serializers, and test fakes.
- Never add `.Result`, `.Wait()`, redundant UI dispatch, hardcoded UI colors, source-code TODO comments, or non-English UI strings.
- Keep follow-up work in Azure DevOps rather than code comments.
- Use Conventional Commits for commits created from these tasks.
