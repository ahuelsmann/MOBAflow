# Feature Specification: Solution session with one runtime per project

**Feature Directory**: `008-project-runtimes`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/191

**Created**: 2026-10-06

**Status**: Draft

**Spec Kit**: Required

**Input**: User description: "Every project of a solution gets its own runtime, and every runtime needs its own Z21
connection; that belongs in the user wiki. Switching the solution or project warns first and then sets everything to
speed 0. No runtime copy of the project: a signal is one object with master data and its runtime aspect. Keep it
simple. Someone might want to operate two layouts at the same time." (RF-23, maintainer, 2026-10-06)

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #191

**Parent Programme**: https://github.com/ahuelsmann/MOBAflow/issues/47 (package RF-23; fixes #190)

**Affected Platforms**: MOBAflow (Windows), MOBAsmart (Android), MOBApi, shared libraries (`Domain`, `Common`,
`Backend`, `SharedUI`).

**Out of Scope**: Splitting `MainWindowViewModel` into page ViewModels (RF-24); recording gaps (#188); new Z21
protocol features; more than one Z21 per project; operating one project from several MOBAflow PCs; internet access.

**Sensitive Data**: None. Tests use synthetic projects, addresses and fakes for the Z21.

**Data and API Effects**: Each project stores its Z21 address and port in the solution file; the Z21 address,
port and recent-address list leave the app settings. Persisted feedback counters are stored per project. MOBApi
commands, runtime snapshots and runtime settings carry a project identifier. There is no migration: an existing
solution starts with projects that have no Z21 address, and removed setting fields are ignored on load
(`Solution.CurrentSchemaVersion` is bumped only if the plan shows that loading requires it). Safe locomotive
startup (speed zero, no restored movement) is unchanged.

## Clarifications

### Session 2026-10-06

- Q: How many runtimes run at once? → A: One runtime per project of the loaded solution; all run at the same time,
  for example to operate two layouts in parallel.
- Q: Where is the Z21 connection configured? → A: Every runtime has its own Z21 connection, configured in its
  project. Two projects never share a Z21. The user wiki must document this rule.
- Q: What happens when two projects enter the same Z21 address? → A: Project diagnostics report the conflict and the
  second runtime does not connect until the address is unique.
- Q: How long does a runtime live? → A: As long as its solution. Switching or closing the solution discards all
  runtimes. A runtime created again for the same project takes over the existing Z21 connection.
- Q: What happens before a switch or close? → A: A dialog warns about the consequences. Only after confirmation are
  all locomotives of every runtime set to speed 0; then the switch happens. Cancelling keeps everything running.
- Q: Does the runtime keep a copy of the project? → A: No. Master data (for example a signal's name, id, location,
  address and multiplexer) stays in the project; runtime values (signal aspect, current station, counters) belong to
  the runtime, keyed by id. Editor changes therefore take effect without re-activation.
- Q: What do MOBAflow pages show? → A: The runtime of the project selected in the UI; the other runtimes keep running
  in the background.
- Q: What does MOBAsmart do? → A: The user selects one project of the synchronized solution; MOBAsmart controls that
  project's Z21 directly and shows its state from MOBAflow.
- Q: How is the work delivered? → A: One specification, implemented in reviewable slices.

### Session 2026-10-08

- Q: The runtime reads journeys, stations and workflows on background threads while the editor changes them on
  the UI thread; sharing one object would race. How do they meet? → A: The runtime keeps an invisible snapshot
  of the master data that is refreshed immediately after every saved change, without re-activation. Journey
  progress, running workflows and signal aspects are kept by id; a running workflow finishes with the master
  data it started with. For the user there is one project and no stale data.
- Q: How does a project get its Z21? → A: A Z21 finder on the solution page searches the network for every Z21
  and lists IP address and serial number. The user drags a Z21 that no project uses yet onto a project (or
  double-clicks it for the selected project); the address can also be typed in the project properties. Each Z21
  belongs to one project. MOBAflow never searches or replaces a project's Z21 on its own, because with several
  Z21 on the network an automatic search could connect a project to another layout.
- Q: Which project is "active"? → A: Only MOBAsmart selects one project. In MOBAflow every project runs with its
  own Z21 at the same time; the project selected in the UI only decides what the pages show.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Operate two layouts at the same time (Priority: P1)

An operator has a solution with two projects, each describing a layout with its own Z21. After loading the solution,
both layouts run: feedback, journeys, announcements and signals of each layout are handled by its own runtime and
never affect the other layout.

**Why this priority**: This is the new capability that motivates the feature and the reason runtime and project
belong together.

**Independent Test**: With two fake Z21 endpoints, load a solution with two projects; feedback on one endpoint
advances only that project's journey and counters, and a locomotive command for one project reaches only its Z21.

**Acceptance Scenarios**:

1. **Given** a solution with projects A and B with different Z21 addresses, **When** it is loaded, **Then** two
   runtimes exist and each connects to its own Z21.
2. **Given** both runtimes run, **When** feedback arrives from A's Z21, **Then** only A's journeys, counters and
   workflows react.
3. **Given** project B has no Z21 address, **When** the solution is loaded, **Then** B's runtime stays
   disconnected and reports the missing address, while A runs normally.
4. **Given** A and B enter the same Z21 address, **When** the solution is loaded, **Then** diagnostics report the
   conflict and only one runtime connects.

---

### User Story 2 - Switch or close a solution safely (Priority: P1)

An operator opens another solution or closes MOBAflow while trains are running. MOBAflow warns first. After
confirmation every locomotive of every runtime is set to speed 0, the runtimes are discarded, and the new solution
starts its own runtimes.

**Why this priority**: Discarding runtimes while trains move is a safety risk on real hardware.

**Independent Test**: With fake Z21 endpoints and moving locomotives, request a solution switch; cancelling keeps all
runtimes running; confirming sends speed 0 to every known locomotive before the runtimes are discarded.

**Acceptance Scenarios**:

1. **Given** runtimes are running, **When** the operator opens another solution, **Then** a warning dialog explains
   that all trains will stop.
2. **Given** the warning is shown, **When** the operator cancels, **Then** nothing changes.
3. **Given** the warning is shown, **When** the operator confirms, **Then** all locomotives receive speed 0, the old
   runtimes are discarded and the new solution's runtimes start.
4. **Given** the same solution is loaded again, **When** its runtimes are created, **Then** each takes over the
   existing connection to its Z21 instead of reconnecting.

---

### User Story 3 - Edit the layout while it runs (Priority: P2)

An operator edits master data of the running project, for example a signal's address in the signal box or a station
name. The change takes effect immediately; running journeys, workflows and the current signal aspects are kept.

**Why this priority**: It removes the hidden "runtime copy" and the gap where signal-box changes were not seen by the
runtime.

**Independent Test**: While a workflow runs, change a signal's address in the editor model and set an aspect; the
command uses the new address, the workflow is not cancelled and journey progress is kept.

**Acceptance Scenarios**:

1. **Given** a running journey, **When** a station name changes, **Then** the journey keeps its position.
2. **Given** a signal with aspect "Hp1", **When** its address changes, **Then** the aspect stays "Hp1" and the next
   command uses the new address.

---

### User Story 4 - Select the project on MOBAsmart and in MOBAflow (Priority: P2)

In MOBAflow, overview, monitor, signal box and train control show the runtime of the selected project. On MOBAsmart
the operator selects one project of the synchronized solution and controls its layout.

**Why this priority**: Without a clear selection, every display and command would be ambiguous.

**Independent Test**: Select project B in MOBAflow; the status bar and pages show B's runtime while A keeps running.
On MOBAsmart, select project B; snapshots and commands carry B's identifier and the direct Z21 connection uses B's
address.

**Acceptance Scenarios**:

1. **Given** two running projects, **When** the operator selects B, **Then** all runtime pages show B and A keeps
   running.
2. **Given** MOBAsmart selects B, **When** it sends a signal command through MOBApi, **Then** MOBAflow applies it to
   B's runtime only.

---

### User Story 5 - Understand the rule (Priority: P3)

A user reads the wiki and learns that each project is a layout with its own runtime and its own Z21, how to enter the
Z21 address per project and what happens when switching solutions.

**Why this priority**: The maintainer requires the rule in the user wiki.

**Independent Test**: The MOBAflow user guide and installation page describe the rule, the per-project Z21 setting
and the switch warning; no page still says to enter the Z21 address in the app settings.

### Edge Cases

- A solution without projects starts no runtime; pages show "no project".
- A project is added or removed while the solution runs: its runtime is created or discarded with the same warning
  and speed-0 rule as a switch.
- A project's Z21 address changes while connected: its runtime disconnects from the old Z21 and connects to the new
  one; locomotives on the old Z21 are set to speed 0 first.
- MOBAflow is closed with the window's close button: the same warning and speed-0 sequence applies.
- A command from MOBAsmart names a project that no longer exists: MOBApi rejects it.
- Two MOBAsmart phones select different projects: each sees and controls only its project.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A solution session MUST own the loaded solution, the selected project and journey, the dirty state and
  auto-save; ViewModels and host services MUST reach the solution only through it. Besides the shell, only WinUI pages
  that bind their XAML to `MainWindowViewModel` and the few consumers of other shell state named in the architecture
  test may still depend on it until RF-24 gives them page ViewModels.
- **FR-002**: The session MUST create one runtime per project of the loaded solution and discard all of them when the
  solution is switched or closed.
- **FR-003**: Each project MUST store its Z21 address and port; the app settings MUST no longer hold a Z21 address,
  port or recent-address list. MOBAflow MUST offer a network search that lists every Z21 with IP address and serial
  number and lets the user assign a Z21 that no other project uses; it MUST NOT assign or replace a Z21 by itself.
- **FR-004**: Each runtime MUST use only its project's Z21; two projects with the same address MUST be reported in
  project diagnostics, and the later runtime MUST NOT connect while the conflict exists.
- **FR-005**: A runtime created again for a project whose Z21 is still connected MUST take over that connection.
- **FR-006**: Before a switch or close with running runtimes, MOBAflow MUST show a warning; after confirmation every
  known locomotive of every runtime MUST receive speed 0 before the runtimes are discarded; cancelling MUST keep the
  current state.
- **FR-007**: The runtime MUST see every saved editor change immediately: it reads master data from a snapshot of
  the project that is refreshed after each saved change without re-activation, so editor and runtime threads never
  share mutable objects. Runtime values MUST be held by the runtime keyed by entity id and MUST NOT be written into
  the saved solution.
- **FR-008**: Editor changes to master data MUST take effect without re-activating the project and without cancelling
  running workflows or resetting journey progress.
- **FR-009**: MOBAflow runtime pages and the status bar MUST show the runtime of the selected project.
- **FR-010**: MOBApi commands, runtime snapshots and runtime settings MUST carry a project identifier; MOBAflow MUST
  apply a remote command only to the named project's runtime.
- **FR-011**: MOBAsmart MUST let the user select one project of the synchronized solution and use that project's Z21
  address for its direct connection.
- **FR-012**: Persisted feedback counters MUST be stored per project.
- **FR-013**: The user wiki MUST document one runtime and one Z21 per project, the per-project Z21 setting and the
  switch warning.
- **FR-014**: Feature behavior MUST be covered by automated tests with fake Z21 endpoints; no test may require
  hardware.

### Key Entities *(include if feature involves data)*

- **Solution session**: owns the loaded solution, selection, dirty state, auto-save and the project runtimes.
- **Project runtime**: the runtime of one project; holds runtime values and its Z21 connection.
- **Project Z21 endpoint**: address and port stored in the project.
- **Runtime values**: per-entity state such as signal aspect, current station and feedback counters, keyed by id.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In automated tests, two projects with two fake Z21 endpoints run independently: 0 cross-project
  reactions to feedback or commands.
- **SC-002**: Confirming a switch sends speed 0 to 100% of known locomotives before any runtime is discarded;
  cancelling changes nothing.
- **SC-003**: Editing master data while a workflow runs cancels 0 workflows and resets 0 journeys.
- **SC-004**: No service or ViewModel depends on `MainWindowViewModel`, checked by an architecture test; the remaining
  WinUI page dependencies are listed in the test and removed by RF-24.
- **SC-005**: The wiki pages describe the rule; no page tells the user to enter the Z21 address in the app settings.

## Assumptions

- MOBAflow has no released users (AGENTS.md), so there is no migration: existing solutions start with projects
  without a Z21 address, and the user enters it once per project.
- Each Z21 is reachable from the MOBAflow PC; the number of projects per solution is small (a few layouts).
- Starting MOBAflow or MOBAsmart, live track power, locomotive movement and firmware are not authorized by this
  specification; manual checks need separate approval.
- Recording per project and the open recording gaps are handled in #188.
