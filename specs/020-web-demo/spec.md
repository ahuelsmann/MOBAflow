# Feature Specification: MOBAflow web demo

**Feature Directory**: `020-web-demo`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/430

**Created**: 2026-11-14

**Status**: Draft; clarified where the issue, the repository and the recorded decisions settle a question. Ten questions are open for the maintainer (see [Open questions](#open-questions-for-the-maintainer)). No plan, no tasks and no implementation exist.

**Spec Kit**: Required

**Input**: Issue #430, requested by the maintainer on 2026-11-09: an interested user asked for a demo application. A web demo lets people try MOBAflow without installing the WinUI desktop app and without hardware: ASP.NET Core as the backend, React as the frontend, without a Z21 connection, showing the pages Track Plan, Train Control, Workflows and Signal Box. Prioritized on 2026-11-14 (verbatim): "Spezifikation jetzt" - the specification starts now, in parallel to the refactoring programme #47; implementation stays after #47.

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #430

**Affected Platforms**: A new, separate web application in a new folder `MOBAweb/`. MOBAflow, MOBAsmart, MOBApi, MOBAdisplay and the shared libraries are not changed in behavior; the demo reads shared libraries and shipped sample data.

**Out of Scope**: Any connection to a Z21 or other hardware; the Backend Z21 runtime; MOBApi's real runtime endpoints; creating, editing or saving content (workflows, track plans, signal-box layouts, locomotives); accounts and sign-in; persistence on the server; a mobile layout (MOBAsmart is the mobile app); the pages other than the four demo pages; localization; server-side speech or sound output; creating Azure resources, secrets, publish profiles or workflows as part of this specification.

**Sensitive Data**: None is stored. The demo keeps no personal data, sets no account and must not show host paths, ports, machine names or real endpoints. Reference screenshots from the maintainer's machine show local paths and are therefore not committed.

**Data and API Effects**: No persisted format, configuration default, Z21 protocol or existing API changes. The demo adds its own read-only demo endpoints in its own host; they are not MOBApi's runtime contract and expose no runtime command path to a real runtime.

**Related Work**: #47 (refactoring programme; implementation of this feature starts after it), #406 (MOBAdcc) and #407 (WiThrottle) as related product ideas, RF-16 #374 (accessibility and themes of the WinUI pages this demo mirrors).

## Clarifications

### Session 2026-11-09 (maintainer decisions recorded in #430)

- Q: Does the demo use a Z21 or a simulated command station? → A: No. A demo-only runtime without the Z21 protocol and without a simulated command station behind the `IZ21` roles. Simulated locomotives need a simple motion model (speed to position) so that Train Control and Track Plan look alive.
- Q: May visitors create or edit content? → A: No. A read-only showcase: visitors trigger simulated actions (drive, set turnouts and signals, run sample workflows) but cannot create or edit workflows, track plans or other content.
- Q: Is state stored? → A: No persistence and no accounts. Each visitor's state lives in memory only.
- Q: Where does the code live? → A: A new folder `MOBAweb/` with the React app, its own Node/npm build and its own CI job, outside the WinUI and Android build graphs.
- Q: Which UI language? → A: English, as in the apps.
- Q: Who may open it? → A: Anyone; public, anonymous, no account and no sign-in.
- Q: What must never be reachable? → A: MOBApi's real runtime endpoints and any real hardware. The demo host only simulates.

### Session 2026-11-14 (maintainer decisions)

The first decision is recorded in #430 by the maintainer. The following three reached this specification through the Chief Operating Officer session, which reports them as maintainer decisions of 2026-11-14; they are recorded here as such and are confirmed or corrected by the maintainer in the review of this specification.

- Q: Where is the demo hosted? → A: Azure App Service `mobaflow-demo.azurewebsites.net`, Free tier F1, region France Central, ASP.NET Core on .NET 10 under Linux, prepared by the Technical Director.
- Q: How closely does the web UI follow MOBAflow's look? → A: As close as possible to the WinUI pages, in Light and Dark like the app. The four pages stay: Train Control, Workflows, Track Plan, Signal Box.
- Q: What does the status bar show? → A: The layout and position of the WinUI status bar with demo texts instead of host details: no local solution path, no REST API port, no machine or host details.
- Q: Are the navigation items outside the demo hidden or disabled? → A: Shown greyed out (disabled), not hidden, in the same order and grouping as the WinUI navigation; a visitor can see why an item is unavailable.

### Session 2026-11-14 (Head of UI/UX advice)

The Head of UI/UX (Gemini Argon via Antigravity) was consulted on the maintainer's request; the Chief Operating Officer session relayed the advice. It is folded into User Story 5 and the requirements FR-020 to FR-046. It is advice, not a maintainer decision: where it touches an open question, it appears as the recommended default under [Open questions](#open-questions-for-the-maintainer).

## Hosting constraints (Azure App Service Free tier F1)

The hosting decision makes the following limits part of the design. They were read from Microsoft Learn on 2026-11-14 and must be re-read before implementation, because Microsoft changes them.

| Limit of the Free tier | Value | Consequence for the demo | Source |
| --- | --- | --- | --- |
| Compute | Shared with other customers' apps; intended for development and testing | No performance guarantee; no always-running background work | [App Service quotas and metrics](https://learn.microsoft.com/azure/app-service/web-sites-monitor) |
| CPU time per day | 60 minutes, reset at midnight UTC | Server work per visitor must be close to zero | [Azure App Service limits](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits) |
| CPU time per 5 minutes | 3 minutes | No sustained server-side simulation loop | same |
| Quota exceeded | The app is stopped until the quota resets; every request gets HTTP 403 | The demo can be offline for the rest of a UTC day; the visitor sees a platform page, not ours | [App Service quotas and metrics](https://learn.microsoft.com/azure/app-service/web-sites-monitor) |
| Memory | 1,024 MB per App Service plan; the app stops temporarily when exceeded | No per-visitor server state that grows with the number of visitors | [Azure App Service limits](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits) |
| Outgoing bandwidth | 165 MB per day | The size of the first page load decides how many visitors a day can be served | same |
| Storage | 1 GB | Small deployment; no log growth | same |
| WebSockets per instance (Linux) | 5 | A live connection per visitor (for example a SignalR hub over WebSockets) serves at most five visitors | same |
| Scale out | 1 shared instance | No scaling | same |
| Deployment slots | Not available below the Standard tier | Every deployment replaces the live app; no staging slot | [Set up staging environments](https://learn.microsoft.com/azure/app-service/deploy-staging-slots) |
| Custom domain | Needs a paid tier, not Free (F1) | The address stays `mobaflow-demo.azurewebsites.net`, with the platform's HTTPS certificate for that host name | [Set up an existing custom domain](https://learn.microsoft.com/azure/app-service/app-service-web-tutorial-custom-domain#prerequisites) |

Not verified in an official limits table: that "Always On" is unavailable in the Free tier and that an idle app is unloaded and cold-starts on the next request. A Microsoft Q&A answer states that Always On needs the Basic tier or higher. The specification assumes cold starts (FR-052) and the plan phase verifies the behavior on the real app.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Drive a simulated locomotive (Priority: P1)

A visitor opens the demo, sees the Train Control page as in MOBAflow, selects a locomotive, sets direction and speed, switches functions and uses the emergency stop. The speedometer follows the speed, and the locomotive moves on the sample track plan.

**Why this priority**: Driving is the core of MOBAflow and the page the maintainer named first. It proves the demo runtime and the motion model.

**Independent Test**: Open the demo in a browser without any hardware or account, select a sample locomotive, move the speed slider and press EMERGENCY STOP; the gauge, the readout and the locomotive state change accordingly.

**Acceptance Scenarios**:

1. **Given** the demo is open on Train Control, **When** the visitor selects a locomotive from the list, **Then** its card is shown as selected and its functions F0 to F31 are listed with the names from the sample data.
2. **Given** a selected locomotive at speed 0, **When** the visitor moves the speed slider to a step between 1 and 126, **Then** the speedometer and the digital readout show the matching speed and the locomotive starts to move in the chosen direction.
3. **Given** a moving locomotive, **When** the visitor presses EMERGENCY STOP, **Then** its speed is 0 immediately, without a confirmation dialog.
4. **Given** a selected locomotive, **When** the visitor toggles a function, **Then** the function shows as switched on and stays on until toggled again or until Reset.
5. **Given** the gradual acceleration ramp is on, **When** the visitor changes the speed, **Then** the displayed speed approaches the target step by step instead of jumping.

---

### User Story 2 - Operate the signal box (Priority: P1)

A visitor opens Signal Box, sees a prepared sample with tracks, turnouts and a Ks signal, selects the signal and chooses an aspect, and throws a turnout. The canvas shows the new state.

**Why this priority**: It shows MOBAflow's interlocking view with visible, immediate feedback and needs no other page.

**Independent Test**: Open Signal Box, select the sample signal, choose another aspect tile and select a turnout and switch it; the drawing changes.

**Acceptance Scenarios**:

1. **Given** the sample signal shows Hp0, **When** the visitor selects it and chooses the tile Ks1, **Then** the signal on the canvas shows Ks1 and the tile Ks1 is marked as selected.
2. **Given** a sample turnout in the straight position, **When** the visitor switches it, **Then** the canvas shows the diverging position.
3. **Given** any element, **When** the visitor tries to add, move, rotate or delete an element, **Then** nothing changes and the control explains that editing is disabled in the demo.

---

### User Story 3 - See the sample track plan (Priority: P2)

A visitor opens Track Plan and sees the sample layout drawn as in MOBAflow, can zoom, fit the layout to the window and select a track piece to see its details. Running locomotives appear on the plan.

**Why this priority**: The plan gives the other pages a place; it reuses the existing renderer and is view-only.

**Independent Test**: Open Track Plan, zoom in and out, press Fit and select a track piece; the context pane shows its definition.

**Acceptance Scenarios**:

1. **Given** Track Plan is open, **When** the page has loaded, **Then** the sample layout is drawn completely and the zoom percentage is shown.
2. **Given** the plan is zoomed in, **When** the visitor presses Fit, **Then** the whole layout is visible again.
3. **Given** the plan, **When** the visitor selects a track piece, **Then** the context pane shows its status and definition, and the catalogue stays browsable.
4. **Given** a locomotive is moving (User Story 1), **When** the visitor looks at the plan, **Then** the locomotive's position on the layout advances.

---

### User Story 4 - Run a sample workflow (Priority: P2)

A visitor opens Workflows, selects a sample workflow, sees its actions as a list or a graph and runs it. The steps are marked one after another; actions that would speak or play a sound show their text instead.

**Why this priority**: Workflows are MOBAflow's automation; the demo shows them without letting visitors change them.

**Independent Test**: Open Workflows, select a sample workflow, switch between List and Graph and run it; each step shows Idle, Running and Completed in order.

**Acceptance Scenarios**:

1. **Given** the sample workflows, **When** the visitor selects one, **Then** its actions appear with name, type and summary, and its properties appear read-only.
2. **Given** a selected workflow, **When** the visitor runs it, **Then** the steps change from Idle to Running to Completed in order, and the Messages bar records the run.
3. **Given** a running workflow with an announcement action, **When** that step runs, **Then** the announcement text is shown to the visitor; no audio is produced by the server.
4. **Given** a selected workflow, **When** the visitor tries to add, delete or move an action or to edit a property, **Then** nothing changes and the control explains that editing is disabled in the demo.

---

### User Story 5 - Recognize MOBAflow (Priority: P1)

A visitor who knows screenshots of MOBAflow, or later installs it, recognizes the same application: the navigation with all its entries, the command bar, the Messages bar, the status bar, and each of the four pages with the same panes and controls, in Light and Dark.

**Why this priority**: The maintainer decided that the web pages look as close as possible to the WinUI pages. A demo that looks like a different product does not advertise MOBAflow.

**Independent Test**: Compare each of the four pages in Dark and Light at 1920 by 1080 pixels and 100 % scale side by side with the reference screenshot of the WinUI app that the maintainer provides, using the checklist in FR-024.

**Acceptance Scenarios**:

1. **Given** the demo in Dark, **When** a reviewer compares a page with its reference screenshot, **Then** every item of the fidelity checklist (FR-024) matches, and only the permitted differences (FR-025) remain.
2. **Given** the navigation, **When** the visitor looks at an entry outside the demo, **Then** it is greyed out in its WinUI position and explains on hover or focus that it is available in the desktop app.
3. **Given** the theme toggle, **When** the visitor switches between Dark and Light, **Then** the whole demo changes theme without reloading and without losing the simulated state.
4. **Given** the status bar, **When** the visitor reads it, **Then** it shows demo texts and no path, port, machine name or endpoint.

---

### User Story 6 - Start over (Priority: P3)

A visitor who has changed speeds, aspects and turnouts presses Reset and gets the initial sample state back. Closing the browser tab discards everything.

**Why this priority**: It makes the read-only, no-persistence decisions visible and gives a way out of any confusing state.

**Independent Test**: Change state on three pages, press Reset, and check that all four pages show the initial sample state.

**Acceptance Scenarios**:

1. **Given** changed state, **When** the visitor presses Reset, **Then** every locomotive is at speed 0 with its functions off, signals and turnouts show their initial positions, and no workflow is running.
2. **Given** a visitor's state, **When** another visitor uses the demo at the same time, **Then** neither sees the other's changes.

### Edge Cases

- The daily CPU or bandwidth quota is used up: the platform answers every request with HTTP 403 until midnight UTC. The demo cannot show its own page then (see open question Q8).
- First request after an idle period: the host starts cold. The visitor sees a loading state, not an empty or broken shell.
- The connection to the host drops after the first load: what already runs in the browser keeps working; anything that needs the host fails with an Info or Warning entry in the Messages bar, not silently.
- A browser window narrower than the supported width (see open question Q7).
- A visitor with reduced-motion preference: gauges do not animate, flashing aspects do not flash (FR-046).
- A visitor without JavaScript: the page states that the demo needs JavaScript.
- A request for an address that belongs to MOBApi's runtime contract (for example a runtime command or hub path): the demo host does not serve it.
- Many requests from one source in a short time: the host limits them (see open question Q4) without affecting other visitors more than the shared quota already does.
- A deployment while visitors are active: there is no staging slot, so the app restarts; simulated state in a visitor's browser survives only as far as the chosen backend shape allows (Q1).

## Requirements *(mandatory)*

### Functional Requirements

**Demo runtime and safety**

- **FR-001**: The demo MUST run without a Z21, without the Z21 protocol and without the Backend Z21 runtime; no code path of the demo can send a command to a command station.
- **FR-002**: The demo host MUST NOT expose MOBApi's runtime endpoints, runtime commands, solution endpoints or runtime hub, and MUST NOT be able to reach a MOBApi instance or any hardware.
- **FR-003**: The demo MUST simulate locomotives with a simple motion model: a speed step and a direction produce a speed in km/h and an advancing position on the sample track plan; emergency stop sets the speed to 0 immediately; the gradual ramp approaches a target speed over time.
- **FR-004**: The demo MUST start every visitor with all locomotives at speed 0 and no function switched on, as MOBAflow's safe startup does.
- **FR-005**: Visitors MUST be able to trigger simulated actions only: drive locomotives, switch functions F0 to F31, emergency stop, set signal aspects, switch turnouts, run sample workflows, select objects, zoom, change the theme and reset.
- **FR-006**: Visitors MUST NOT be able to create, change, delete, import, export or save workflows, track plans, signal-box layouts, locomotives or any other content.
- **FR-007**: The demo MUST NOT persist visitor state on the server and MUST NOT use accounts, sign-in or tracking identifiers; each visitor's state is isolated from every other visitor's.
- **FR-008**: Reset MUST restore the visitor's initial sample state on all four pages.
- **FR-009**: Speech and sound actions MUST be shown visually (a notification and a Messages entry with the text or sound name); the server produces no audio.
- **FR-010**: The demo MUST show shipped sample content only: a sample layout, sample locomotives with their function names as data (German names such as "Stirnlicht" are sample data, not UI text), sample workflows and a prepared signal-box sample. Which shipped files and which locomotive photos may be published is open question Q9.

**Shell**

- **FR-020**: The UI text MUST be English. Sample data keeps its own language.
- **FR-021**: The demo MUST support Light and Dark like the app, with a theme toggle in the command bar that switches without a reload and without losing state. [NEEDS CLARIFICATION: initial theme - recommended default Dark, as in the reference; alternative: follow the browser's preference.]
- **FR-022**: The shell MUST reproduce MOBAflow's structure: a left navigation pane with icon and label entries in MOBAflow's order and grouping (Overview; Train Control; Journeys, Timetable, Event Manager, Journey Map; Solution, Workflows, Stations, Locomotives, Passenger Wagons, Goods Wagons, Trains; Track Plan, Signal Box, ESP32 Display, Matrix Images; Monitor, Recorder; Help, Info, Settings) with their Preview badges; a top command bar; a Messages bar; a status bar at the bottom. The entries, order, icons and badges are taken from the application's page registration (`MOBAflow/Service/NavigationRegistration.cs`), not from this list.
- **FR-023**: Colors, typography, spacing and icons MUST be derived from the repository's WinUI resources (`MOBAflow/App.xaml` theme dictionaries for Light, Dark and High Contrast, `MOBAflow/Resources/`, `MOBAflow/Themes/`, the Segoe Fluent icon glyphs of the page registration) and mapped to named design tokens, so that a later change in the app can be followed. The accent color is not defined in the repository: MOBAflow uses the Windows system accent (`SystemAccentColor`), which is magenta on the maintainer's machine. The fixed accent value of the demo is open question Q5.
- **FR-024**: Fidelity MUST be checkable against eight reference screenshots of the WinUI app supplied by the maintainer (four pages, each in Dark and Light, 1920 by 1080 pixels, 100 % scale). Per page and theme the following MUST match: shell structure; navigation entries, order, grouping, icons and Preview badges; command bar; Messages bar and status bar layout; surface and accent colors by value; the complete inventory of controls on the page; the arrangement of its panes.
- **FR-025**: Permitted differences are limited to: font rendering and fallback (Segoe UI Variable, Segoe UI, system font), the browser's native scrollbars, the Mica window material shown as a flat tint, and the content differences this specification requires (disabled entries and controls, demo texts in the status bar).
- **FR-026**: Reference screenshots from the maintainer's machine MUST NOT be committed, because they show local paths. If reference images are wanted in the repository, they are produced without host details.
- **FR-027**: Navigation entries outside the demo MUST be shown disabled (greyed out), not hidden, in their WinUI position. They MUST stay reachable in keyboard order, be exposed as disabled to assistive technology, do nothing on activation, and explain on hover and on focus: "Available in the MOBAflow desktop app (not included in this web demo)". Their text MUST keep a contrast of at least 3:1 in Light and Dark. The four demo entries are shown at full contrast with the accent selection indicator.
- **FR-028**: The command bar MUST offer the theme toggle, Simulate and Reset as active controls. New and Open MUST be shown disabled with the explanation "Fixed sample project in the web demo". What Simulate does in the demo follows the app's feedback simulation and is detailed in the plan phase.
- **FR-029**: The status bar MUST keep the layout and position of the WinUI status bar and show demo texts: the project as the demo layout (for example "Project: MOBAflow demo layout"), the command station as simulated (for example "Z21: simulated (demo)"), Track Power reflecting the simulated state (for example "Track Power: On (virtual)"), the runtime as a demo (for example "Runtime: Simulated (in-memory)") and speech as not available in the demo. The final wording is fixed in the plan phase within these meanings.
- **FR-030**: No status bar text and no other UI text, tooltip, message, page source comment or error message MUST expose a host path, port, machine name, internal address or real endpoint.
- **FR-031**: The Messages bar MUST show its error, warning and information counters and one pre-populated Info entry on the first visit: "Interactive web demo: simulated runtime, no hardware connected. Drive locomotives, switch signals or run workflows; use Reset in the top bar at any time." There is no modal dialog and no marketing header.

**Read-only presentation**

- **FR-035**: Toolbars, catalogues and property panes MUST NOT be hidden. Controls that would change content are shown disabled with the explanation "Editing is disabled in the read-only demo"; property fields are shown read-only at full legibility with a "Read-only in demo" mark.
- **FR-036**: Train Control MUST show: the page title, the series search box, the EMERGENCY STOP button, the locomotive list as cards with photo and name, the direction toggle, the DCC speed steps selection, the acceleration ramp toggle, the timetable and stops list, the three mode tiles, the speedometer gauge (0 to 400 km/h) with digital readout, the speed slider (0 to 126), the current gauge with its values, and the functions list F0 to F31 with icon, number and name. Active: search, locomotive selection, EMERGENCY STOP, direction, speed slider, acceleration ramp, functions, mode tiles. Read-only: the DCC speed steps selection. The current gauge shows plausible simulated values, marked as simulated.
- **FR-037**: Workflows MUST show the three panes of the app: the workflows list with search, the actions pane with the List and Graph views, its toolbar, the graph with the trigger node connected to action nodes, automatic layout and zoom, and the properties pane. Active: selection, search, List/Graph switch, zoom, automatic layout, run. Disabled: add, delete and move. Properties are read-only.
- **FR-038**: Track Plan MUST show the toolbar, the Piko A-Track catalogue, the canvas with the sample layout, the zoom slider with percentage, the context pane and the footer with status text and coordinates. Active: Fit, zoom, grid, labels, Validate, the validation overlay and selecting elements into the context pane. Disabled: Undo, Redo, Export, Snap, Disconnect, Delete and dragging from the catalogue, which stays browsable and carries the note "Read-only sample layout".
- **FR-039**: Signal Box MUST show the elements toolbox, the canvas with the prepared sample, the context pane with the aspect tiles of the selected signal and the multiplexer configuration, and the element count footer. Active: selecting elements, setting signal aspects, switching turnouts. Disabled or read-only: dragging from the toolbox, rotation, the multiplexer configuration.
- **FR-040**: The sample layout on Track Plan MUST be drawn from the same track geometry as in MOBAflow, produced by the existing renderer (`TrackPlan.Renderer`), not by a second, hand-written drawing.

**Accessibility**

- **FR-041**: Every active control MUST be operable by keyboard with a visible focus indicator, and every state MUST be conveyed by text or shape in addition to color.
- **FR-042**: The speed slider MUST expose its range 0 to 126 and a value text of the form "Step X (Y km/h)" and respond to Arrow keys by 1, Page Up and Page Down by 10, and Home and End.
- **FR-043**: The gauges MUST expose their value without announcing every animation frame. EMERGENCY STOP MUST be a real button early in the keyboard order, labelled "Emergency stop for the selected locomotive".
- **FR-044**: Function buttons and mode tiles MUST expose their pressed state; tiles that only show status are not interactive.
- **FR-045**: Workflow graph nodes, track plan elements and signal-box elements MUST be focusable by keyboard; a workflow node announces its step, name and state (Idle, Running, Completed). The aspect tiles form one single-choice group and show the aspect code as text and the light arrangement in addition to color.
- **FR-046**: With the reduced-motion preference set, gauges MUST change without animation, a flashing aspect MUST be shown as steady with the text "Flashing", and pan and zoom MUST NOT animate.

**Hosting, build and deployment**

- **FR-050**: The demo MUST be deployable to the decided host: Azure App Service `mobaflow-demo.azurewebsites.net`, Free tier F1, France Central, ASP.NET Core on .NET 10 under Linux.
- **FR-051**: The demo MUST stay within the Free tier limits in normal use: server work per visitor close to zero, no per-visitor server memory that grows without bound, no design that needs more than the five WebSocket connections of the tier, and a first page load small enough for the 165 MB daily bandwidth (target in SC-006).
- **FR-052**: The demo MUST tolerate a cold start: the first response after an idle period shows a loading state and then the complete shell.
- **FR-053**: The code MUST live in `MOBAweb/` with its own Node/npm build for the React app and its own CI job; it MUST NOT be part of the WinUI or Android build graphs, and building MOBAflow or MOBAsmart MUST NOT require Node.
- **FR-054**: Deployment to the Azure web app MUST be possible through a GitHub Actions workflow without a secret in the repository. The workflow, its credentials and its trigger are created in the implementation, not by this specification (trigger: open question Q10).
- **FR-055**: Because the tier has no deployment slot, a deployment replaces the running app; the demo MUST come back without manual steps.
- **FR-056**: The host MUST apply basic abuse protection suitable for a public, anonymous site (values: open question Q4) and MUST offer no endpoint that changes server state shared between visitors.

**Open by design** (each is a [NEEDS CLARIFICATION] with a recommended default in [Open questions](#open-questions-for-the-maintainer)):

- **FR-060**: Where the per-visitor simulation runs and what the demo host references [NEEDS CLARIFICATION: Q1].
- **FR-061**: How long a visitor's state lives [NEEDS CLARIFICATION: Q2].
- **FR-062**: How many visitors are served at once [NEEDS CLARIFICATION: Q3].
- **FR-063**: The abuse protection values [NEEDS CLARIFICATION: Q4].
- **FR-064**: The accent color value [NEEDS CLARIFICATION: Q5].
- **FR-065**: The page a visitor lands on [NEEDS CLARIFICATION: Q6].
- **FR-066**: The behavior below the supported window width [NEEDS CLARIFICATION: Q7].
- **FR-067**: What happens when the daily quota is used up [NEEDS CLARIFICATION: Q8].
- **FR-068**: The sample content and locomotive photos that may be published [NEEDS CLARIFICATION: Q9].
- **FR-069**: The deployment trigger [NEEDS CLARIFICATION: Q10].

### Key Entities

- **Demo session**: One visitor's simulated state: locomotives, signals, turnouts, running workflows, theme. Isolated per visitor, in memory only, discarded at the end of its lifetime (Q2), restorable by Reset.
- **Sample project**: The fixed, read-only content every session starts from: sample layout, locomotives, workflows, signal-box sample.
- **Simulated locomotive**: Name, photo, function list with names, speed step, direction, ramp setting, switched functions, position on the layout.
- **Sample track plan**: Placed track pieces of the Piko A-Track catalogue and the drawing produced from them.
- **Signal-box sample**: Track elements, turnouts with a position, signals with a selected aspect out of their available aspects.
- **Sample workflow**: A trigger and ordered actions with name, type and summary, and a run state per step (Idle, Running, Completed).
- **Message**: An error, warning or information entry of the Messages bar.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time visitor drives a locomotive (selects it, sets a speed, stops it) within 60 seconds of the page being ready, without instructions outside the page.
- **SC-002**: For each of the eight reference screenshots, a reviewer finds every item of the fidelity checklist (FR-024) matching; any difference is one of the permitted differences (FR-025).
- **SC-003**: A search of the delivered pages, scripts and responses finds no host path, port, machine name or internal address, and a request to any MOBApi runtime path is not served.
- **SC-004**: None of the content-changing controls changes anything: a check of every disabled or read-only control listed in FR-036 to FR-039 leaves the sample content identical.
- **SC-005**: After Reset, all four pages show the initial sample state; two visitors using the demo at the same time never see each other's changes.
- **SC-006**: The first page load transfers at most 1.5 MB with compression, so that at least 100 first visits fit into the daily bandwidth of the Free tier; a repeat visit transfers under 100 KB. [NEEDS CLARIFICATION: these two targets are a proposal derived from the 165 MB limit; the maintainer may set others with Q3.]
- **SC-007**: One visitor using the demo for ten minutes uses less than 2 seconds of server CPU time, so that the daily CPU quota is not the limiting factor.
- **SC-008**: After an idle period, the complete shell is usable within 15 seconds of the first request on a normal home connection; a warm page is usable within 3 seconds.
- **SC-009**: Every active control of the four pages is operable by keyboard alone, and an automated accessibility check of the four pages in both themes reports no critical finding.
- **SC-010**: Building MOBAflow and MOBAsmart works on a machine without Node, and the `MOBAweb/` CI job runs without the WinUI or Android workloads.

## Open questions for the maintainer

Each question has a recommended default and the alternatives. Answering with the letters is enough (for example "Q1 A, Q2 A, ...").

| # | Question | Recommended default | Alternatives |
| --- | --- | --- | --- |
| Q1 | **Backend shape.** Where does the per-visitor simulation run, and what is the host? | **A:** A separate small ASP.NET Core host in `MOBAweb/` that serves the React app and a read-only snapshot of the sample project; the simulation runs in the visitor's browser. It protects the Free tier (no connection per visitor, almost no server CPU or memory) and makes Reset instant. The host references no `Backend` Z21 runtime. Repository evidence: `TrackPlan.Renderer` references `TrackLibrary.Base`, and the sample layout uses the Piko A catalogue, so the reference set is `Domain`, `Contracts`, `TrackPlan.Renderer` plus `TrackLibrary.Base` and `TrackLibrary.PikoA`, not only the first three. | **B:** Same host, but the simulation runs on the server per visitor; closer to the real runtime, but each visitor costs server CPU and memory, and live updates over WebSockets serve at most five visitors on F1. **C:** A demo mode inside MOBApi; rejected by the security decision unless the maintainer reopens it. |
| Q2 | **Session lifetime.** How long does a visitor's state live? | **A** (with Q1 A): as long as the browser tab is open; a reload or closing the tab starts fresh; nothing is kept on the server. | **B:** kept in the browser across reloads until Reset. **C** (with Q1 B): server session, discarded after 15 minutes without activity. |
| Q3 | **Concurrent visitors.** Is there a limit? | **A** (with Q1 A): no fixed limit; the daily bandwidth is the real ceiling (about 100 first visits per day at the SC-006 target). | **B** (with Q1 B): a fixed cap, for example 20 sessions with polling or 5 with a live connection; further visitors see "The demo is busy, please try again in a few minutes". |
| Q4 | **Abuse protection.** Which limits apply? | **A:** per source address at most 60 requests per minute and 600 per hour to the host, cacheable static files, no endpoint that writes, standard security headers; with Q1 B additionally one session per source address. | **B:** stricter values (for example 30 per minute). **C:** no application limit, rely on the platform quota only (not recommended: one client can use up the day). |
| Q5 | **Accent color.** MOBAflow uses the Windows accent of the machine it runs on; the repository defines no magenta. Which fixed value does the demo use? | **A:** the magenta of the maintainer's reference screenshots, taken by value from them and recorded as the demo's accent token. | **B:** Windows' default blue (`#0078D4`), which is what most MOBAflow users see. **C:** follow the visitor's system accent where the browser offers it. |
| Q6 | **Landing page.** Which page opens first? | **A:** Train Control with a flagship sample locomotive preselected. | **B:** Track Plan. **C:** Overview is not part of the demo, so not available. |
| Q7 | **Narrow windows.** What happens below 1024 pixels width? | **A:** keep the desktop layout with a minimum width of 1024 pixels and horizontal scrolling, plus a dismissible note "Desktop UI demo - best on a screen at least 1024 px wide". From 1280 pixels the full layout; from 1024 to 1279 pixels the navigation collapses to its compact rail and side panes may fold. | **B:** a stacked mobile layout; it would not look like the WinUI pages and MOBAsmart is the mobile app. |
| Q8 | **Quota used up.** When the daily CPU or bandwidth quota is exhausted, the platform shows HTTP 403 until midnight UTC. | **A:** accept it for the Free tier and document it on #430 and in the README of `MOBAweb/`. | **B:** move to a paid tier (Basic) if it happens regularly; that also allows a custom domain and Always On. **C:** put a free content delivery service in front for the static files; more setup, another party. |
| Q9 | **Sample content.** Which shipped data and photos are published? | **A:** the shipped sample solution and track plan (`MOBAflow/solution.json`, `MOBAflow/track-plan.json`) reduced to a few locomotives and workflows, with locomotive photos only where the maintainer confirms the right to publish them; otherwise neutral placeholder images. | **B:** a new, smaller sample made for the demo. |
| Q10 | **Deployment trigger.** When is the demo deployed? | **A:** manually started workflow first; automatic deployment on changes to `MOBAweb/**` on `main` once the demo is stable. | **B:** automatic from the start. |

Also to confirm in review: the three decisions of 2026-11-14 that reached this specification through the Chief Operating Officer session (design closeness, status bar texts, disabled navigation entries), and the initial theme (FR-021).

## Assumptions

- Implementation starts after the refactoring programme #47; this specification does not authorize it.
- The Technical Director provides the Azure web app; the specification creates no Azure resource, secret, publish profile or workflow.
- The maintainer provides the eight reference screenshots outside the repository.
- A desktop browser in a current version (Edge, Chrome, Firefox, Safari) with JavaScript is the target; phones are not.
- Cold starts occur on the Free tier; this is assumed, not verified in an official limits table.
- The simple motion model is a visual simulation; it does not claim the timing of real decoders, and the current gauge shows simulated values.
- "Read-only" means no content change; simulated runtime state (speed, aspects, turnout positions) is not content.
- The Free tier limits in this document were read on 2026-11-14 and are re-read before implementation.
