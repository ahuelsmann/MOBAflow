# Feature Specification: MOBAflow web demo

**Feature Directory**: `020-web-demo`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/430

**Created**: 2026-11-14

**Status**: Draft; clarified and narrowed to a visual showcase on 2026-11-17 (see [Clarifications](#clarifications)), review findings folded in on 2026-11-18; no [NEEDS CLARIFICATION] remains. No tasks and no implementation exist; the plan is in [plan.md](plan.md).

**Spec Kit**: Required

**Input**: Issue #430, requested by the maintainer on 2026-11-09: an interested user asked for a demo application. A web demo lets people try MOBAflow without installing the WinUI desktop app and without hardware: ASP.NET Core as the backend, React as the frontend, without a Z21 connection, showing the pages Track Plan, Train Control, Workflows and Signal Box. The maintainer prioritized the specification on 2026-11-14: it starts now, in parallel to the refactoring programme #47; implementation stays after #47. On 2026-11-17 the maintainer narrowed the demo to a visual showcase: only navigation, theme and the Train Control slider are interactive; everything else is a prepared, static view.

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #430

**Affected Platforms**: A new, separate web application in a new folder `MOBAdemo/`. MOBAflow, MOBAsmart, MOBApi, MOBAdisplay and the shared libraries are not changed; the demo uses its own static example data and assets.

**Out of Scope**: Any simulation of a railway (motion, signals, turnouts, track power, feedback); workflow execution; importing or reading a desktop solution; a generator that derives the demo's data from shipped project files at build or run time; full keyboard operation of the demo (pointer input is enough; a basic accessibility floor stays, FR-041 to FR-047); any connection to a Z21 or other hardware; the Backend Z21 runtime; MOBApi's real runtime endpoints; creating, editing or saving content; accounts and sign-in; persistence on the server; a mobile layout (MOBAsmart is the mobile app); the pages other than the four demo pages; localization; speech or sound output; creating Azure resources, secrets, publish profiles or workflows as part of this specification.

**Sensitive Data**: None is stored. The demo keeps no personal data, sets no account and must not show host paths, ports, machine names or real endpoints. Reference screenshots from the maintainer's machine show local paths and are therefore not committed.

**Data and API Effects**: No persisted format, configuration default, Z21 protocol or existing API changes. The demo host serves static files only; it has no API of its own and exposes no runtime command path.

**Related Work**: #47 (refactoring programme; implementation of this feature starts after it), #406 (MOBAdcc) and #407 (WiThrottle) as related product ideas, RF-16 #374 (accessibility and themes of the WinUI pages this demo mirrors).

## Clarifications

### Session 2026-11-09 (maintainer decisions recorded in #430)

- Q: Does the demo use a Z21 or a simulated command station? → A: No. A demo-only runtime without the Z21 protocol and without a simulated command station behind the `IZ21` roles. The motion model of this answer is superseded by the decision of 2026-11-17 (no simulation).
- Q: May visitors create or edit content? → A: No. A read-only showcase; visitors cannot create or edit workflows, track plans or other content. The simulated actions this answer allowed (drive, set turnouts and signals, run sample workflows) are superseded by the decision of 2026-11-17.
- Q: Is state stored? → A: No persistence and no accounts. Each visitor's state lives in memory only.
- Q: Where does the code live? → A: A new folder `MOBAdemo/` with the React app, its own Node/npm build and its own CI job, outside the WinUI and Android build graphs.
- Q: Which UI language? → A: English, as in the apps.
- Q: Who may open it? → A: Anyone; public, anonymous, no account and no sign-in.
- Q: What must never be reachable? → A: MOBApi's real runtime endpoints and any real hardware. The demo host serves static files only.

### Session 2026-11-14 (maintainer decisions)

All decisions of this session are recorded as maintainer decisions in comments on #430: [prioritization and hosting](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6882822768) (19:14 UTC), [design closeness](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883059858) (19:33), [status bar](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883112112) (19:37), [navigation](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883124336) (19:38), [landing page and narrow windows](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883248364) (19:48), [browser simulation, session lifetime, visitors and abuse protection](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883283475) (19:51), and [accent color, photos, deployment and initial theme](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883411696) (20:01). They reached this specification through the Chief Operating Officer session.

- Q: Where is the demo hosted? → A: Azure App Service `mobaflow-demo.azurewebsites.net`, Free tier F1, region France Central, ASP.NET Core on .NET 10 under Linux, prepared by the Technical Director.
- Q: How closely does the web UI follow MOBAflow's look? → A: As close as possible to the WinUI pages, in Light and Dark like the app. The four pages stay: Train Control, Workflows, Track Plan, Signal Box.
- Q: What does the status bar show? → A: The layout and position of the WinUI status bar with demo texts instead of host details: no local solution path, no REST API port, no machine or host details.
- Q: Are the navigation items outside the demo hidden or disabled? → A: Shown greyed out (disabled), not hidden, in the same order and grouping as the WinUI navigation; a visitor can see why an item is unavailable.
- Q: Which page opens first? → A: Train Control with a flagship sample locomotive preselected.
- Q: Where does the per-visitor simulation run, and what does the host do? → A: Superseded on 2026-11-17: there is no simulation. What stays: the ASP.NET Core host in `MOBAdemo/` serves static files; no per-visitor server state, no SignalR, no runtime endpoints.
- Q: How long does a visitor's state live? → A: Only as long as the visit (the browser tab); a reload starts fresh. After 2026-11-17 the only state is the user interface itself (current page, slider position, notices).
- Q: Do 12 concurrent visitors work on the Free tier? → A: Yes. Because the host only serves files, concurrency is not a server limit. At least 12 concurrent visitors are a supported requirement. The daily outgoing bandwidth of 165 MB limits the demo, not concurrency; a sustained higher demand needs a paid tier, which is a later maintainer decision and out of scope.
- Q: Which abuse is conceivable and what protects against it? → A: See [Threats and abuse protection](#threats-and-abuse-protection): proportionate measures, no CAPTCHA, no accounts.
- Q: Which accent color does the demo use? → A: A fixed magenta taken from the maintainer's reference screenshots, the same on every machine. It applies only where WinUI uses the system accent; the explicit non-accent colors of `MOBAflow/App.xaml` stay as they are. The exact value is sampled from the reference screenshots in the plan phase.
- Q: Which sample content and photos may be published? → A: The locomotive photos are the maintainer's own photographs and may be published in the demo, resized as WebP within the transfer budget. Since 2026-11-17 the demo uses its own static example data instead of the shipped sample data.
- Q: When is the demo deployed? → A: By a manually started deployment first; automatic deployment on merge later.
- Q: Which theme does a first visit show? → A: Dark, not the browser's preference. A manual toggle choice is remembered in the browser.
- Q: How much accessibility does the demo need? → A: Mouse or pointer input is enough for the demo, with a basic accessibility floor kept (about 20:30 UTC). This narrows the Head of UI/UX advice: keyboard operation of the canvases, the slider key map, app-specific keyboard shortcuts and keyboard navigation through the disabled navigation entries are dropped; accessible names, visible focus, the browser's default keyboard behavior of ordinary controls, non-color cues, reduced motion and the contrast of disabled entries stay (FR-041 to FR-046).
- Q: What happens in a browser window narrower than 1024 pixels? → A: The demo keeps the desktop geometry (minimum width 1024 pixels, horizontal scrolling) with a dismissible notice "Desktop UI demo - best on a screen at least 1024 px wide". There is no stacked mobile layout.

### Session 2026-11-17 (maintainer decision)

Recorded on #430 ([decision comment](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6950953706)), confirmed by the maintainer on 2026-11-17 after the Chief Project Manager's handover of 2026-11-16. It supersedes the browser simulation of 2026-11-14 and the simulated actions of 2026-11-09.

- Q: How interactive is the demo? → A: A visual showcase. Navigation between the four pages and the theme selection are interactive. The Train Control speed slider moves only itself; the gauge and the locomotive do not change. Track Plan, Signal Box and Workflows are prepared, static views without railway simulation or workflow execution.
- Q: Where does the content come from? → A: From dedicated static example data and assets made for the demo. There is no import of a desktop solution, no snapshot generator and no simulation.

### Session 2026-11-14 (Head of UI/UX advice)

The Head of UI/UX (Gemini Argon via Antigravity) was consulted on the maintainer's request; the Chief Operating Officer session relayed the advice. It is folded into User Story 3 and the requirements FR-020 to FR-046, and its refinement of the same day (after it was informed of the maintainer decisions) into FR-076 and FR-077. It is advice, not a maintainer decision; the maintainer's decision that pointer input is enough narrows its accessibility part, and what remains of it in FR-041 to FR-046 is still attributed to this advice. Its review of this draft on #455 (2026-11-14) found four deviations from the WinUI sources; they are corrected in FR-022, FR-028, FR-029, FR-031, FR-036 and FR-044 and were checked against `MOBAflow/View/MainWindow.xaml`, `MOBAflow/View/TrainControlPage.xaml` and `MOBAflow/Service/NavigationRegistration.cs`.

## Hosting constraints (Azure App Service Free tier F1)

The hosting decision makes the following limits part of the design. They were read from Microsoft Learn on 2026-11-14 and must be re-read before implementation, because Microsoft changes them.

| Limit of the Free tier | Value | Consequence for the demo | Source |
| --- | --- | --- | --- |
| Compute | Shared with other customers' apps; intended for development and testing | No performance guarantee; no always-running background work | [App Service quotas and metrics](https://learn.microsoft.com/azure/app-service/web-sites-monitor) |
| CPU time per day | 60 minutes, reset at midnight UTC | Server work per visitor must be close to zero | [Azure App Service limits](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits) |
| CPU time per 5 minutes | 3 minutes | No sustained server-side work | same |
| Quota exceeded | The app is stopped until the quota resets; every request gets HTTP 403 | The demo can be offline for the rest of a UTC day; the visitor sees a platform page, not ours | [App Service quotas and metrics](https://learn.microsoft.com/azure/app-service/web-sites-monitor) |
| Memory | 1,024 MB per App Service plan; the app stops temporarily when exceeded | No per-visitor server state that grows with the number of visitors | [Azure App Service limits](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits) |
| Outgoing bandwidth | 165 MB per day | The binding limit: the size of a first visit decides how many visitors a day can be served (FR-049, FR-057) | same |
| Storage | 1 GB | Small deployment; no log growth | same |
| WebSockets per instance (Linux) | 5 | A live connection per visitor would serve at most five visitors; the demo uses none (FR-070) | same |
| Scale out | 1 shared instance | No scaling | same |
| Deployment slots | Not available below the Standard tier | Every deployment replaces the live app; no staging slot | [Set up staging environments](https://learn.microsoft.com/azure/app-service/deploy-staging-slots) |
| Always On | Not available in Free and Shared; starts at Basic | An idle app is not kept running; the first request after an idle period starts it (FR-052) | [Azure App Service limits](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#azure-app-service-limits) |
| Service level agreement | None for Free and Shared | No availability promise | same |
| Custom domain | Needs a paid tier, not Free (F1) | The address stays `mobaflow-demo.azurewebsites.net`, with the platform's HTTPS certificate for that host name | [Set up an existing custom domain](https://learn.microsoft.com/azure/app-service/app-service-web-tutorial-custom-domain#prerequisites) |

Not stated in the limits table: how long an idle app stays loaded and how long a cold start takes. The specification assumes cold starts (FR-052); the plan phase measures them on the real app.

## Threats and abuse protection

The demo has no accounts, stores no data and takes no input that reaches the server; it serves static files only. Defacement through stored content, data leaks and account abuse are therefore not possible by design. The realistic risks and their answers:

| Risk | Effect | Answer |
| --- | --- | --- |
| Traffic flooding or bots | The daily bandwidth or CPU quota is used up; the platform answers HTTP 403 for the rest of the UTC day | Rate limiting per client address protects the CPU quota and bounds what a single client can download (FR-056); small and cacheable transfers (FR-057 to FR-059). A flood from many addresses can still use up the 165 MB; this residual risk is accepted on the Free tier (FR-075) |
| Hotlinking of photos and other files from other sites | Bandwidth is spent on other sites' visitors | Resized, compressed images (FR-058) and a cross-origin resource policy that keeps files for the demo's own pages (FR-071) |
| Scraping | Bandwidth use; the content itself is public sample data | Rate limiting; the indexing choice in FR-073 |
| Misconfiguration exposing host details or real endpoints | Leaks paths, ports or a route to a real runtime | No dynamic endpoint (FR-070), no server banner or detailed error page (FR-072), FR-002 and FR-030 |
| Supply-chain or script-injection risk in the frontend | Malicious code runs in visitors' browsers | A committed lockfile and a dependency audit in CI (FR-074), a content security policy without inline scripts (FR-071) |

Not planned, because they would be out of proportion for a demo: CAPTCHA, accounts, a web application firewall.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See Train Control as in MOBAflow (Priority: P1)

A visitor opens the demo and lands on Train Control, laid out as in MOBAflow, with a flagship example locomotive selected. The visitor can drag the speed slider; the slider and its own value move, while the gauge and the locomotive keep their prepared values.

**Why this priority**: Train Control is the page the maintainer named first and the landing page; it carries most of the visual identity (cards, gauges, functions).

**Independent Test**: Open the demo in a browser without any hardware or account; Train Control shows the example locomotive, its functions and the gauges; dragging the slider moves only the slider.

**Acceptance Scenarios**:

1. **Given** a first visit, **When** the page has loaded, **Then** Train Control is shown with the flagship example locomotive selected, its functions F0 to F31 listed with the names from the example data, and the gauges at their prepared values.
2. **Given** Train Control, **When** the visitor drags the speed slider, **Then** the slider thumb and its own value text follow, and the speedometer, the digital readout, the functions and the locomotive card stay unchanged.
3. **Given** Train Control, **When** the visitor clicks any other control (a locomotive card, a function, the direction toggle, EMERGENCY STOP, a cab-control tile), **Then** nothing changes and a hint explains that the control works in the MOBAflow desktop app.

---

### User Story 2 - Look at the prepared views (Priority: P2)

A visitor opens Track Plan, Signal Box and Workflows and sees each as a prepared view: the example layout, a signal box with a selected signal and its aspect tiles, and an example workflow as a graph.

**Why this priority**: These pages show the breadth of MOBAflow; as static views they need no runtime and cost little.

**Independent Test**: Open each of the three pages; each shows its prepared content with all panes of the app; clicking inside them changes nothing.

**Acceptance Scenarios**:

1. **Given** Track Plan, **When** the page has loaded, **Then** the example layout is drawn completely with the toolbar, catalogue, context pane and footer of the app.
2. **Given** Signal Box, **When** the page has loaded, **Then** the example track elements, turnouts and a Ks signal are drawn, and the context pane shows the selected signal with its aspect tiles and the prepared aspect marked.
3. **Given** Workflows, **When** the page has loaded, **Then** an example workflow is shown with the trigger node connected to its action nodes, and its properties are shown read-only.
4. **Given** any of the three pages, **When** the visitor clicks a tool, a tile, a node or an element, **Then** nothing changes and a hint explains that the control works in the MOBAflow desktop app.

---

### User Story 3 - Recognize MOBAflow (Priority: P1)

A visitor who knows screenshots of MOBAflow, or later installs it, recognizes the same application: the navigation with all its entries, the command bar, the Messages bar, the status bar, and each of the four pages with the same panes and controls, in Light and Dark.

**Why this priority**: The maintainer decided that the web pages look as close as possible to the WinUI pages. A demo that looks like a different product does not advertise MOBAflow.

**Independent Test**: Compare each of the four pages in Dark and Light at 1920 by 1080 pixels and 100 % scale side by side with the reference screenshot of the WinUI app that the maintainer provides, using the checklist in FR-024.

**Acceptance Scenarios**:

1. **Given** the demo in Dark, **When** a reviewer compares a page with its reference screenshot, **Then** every item of the fidelity checklist (FR-024) matches, and only the permitted differences (FR-025) remain.
2. **Given** the navigation, **When** the visitor clicks one of the four demo entries, **Then** that page opens; **When** the visitor points at an entry outside the demo, **Then** it is greyed out in its WinUI position and explains that it is available in the desktop app.
3. **Given** the theme toggle, **When** the visitor switches between Dark and Light, **Then** the whole demo changes theme without reloading and keeps the current page and slider position.
4. **Given** the status bar, **When** the visitor reads it, **Then** it shows demo texts and no path, port, machine name or endpoint.

### Edge Cases

- The daily CPU or bandwidth quota is used up: the platform answers every request with HTTP 403 until midnight UTC. The demo cannot show its own page then; this is accepted on the Free tier (FR-075).
- First request after an idle period: the host starts cold. The visitor sees a loading state, not an empty or broken shell.
- A file fails to load after the first page (for example a photo or a page's example data): the page shows its frame and a Warning entry in the Messages bar, not a blank area.
- A browser window narrower than 1024 pixels: the desktop layout stays, with horizontal scrolling and the dismissible notice (FR-033).
- A visitor with reduced-motion preference: no animated transitions; a flashing aspect is shown as steady with the text "Flashing" (FR-046).
- A visitor without JavaScript: the page states that the demo needs JavaScript.
- A request for an address that belongs to MOBApi's runtime contract (for example a runtime command or hub path): the demo host does not serve it.
- Many requests from one source in a short time: the host limits them (FR-056) and answers with the standard "too many requests" status; other visitors are not limited by it.
- A deployment while visitors are active: there is no staging slot, so the app restarts; an open page keeps working, and a reload afterwards shows the new version.

## Requirements *(mandatory)*

### Functional Requirements

**Scope and safety**

- **FR-001**: The demo MUST run without a Z21, without the Z21 protocol and without the Backend Z21 runtime; no code path of the demo can send a command to a command station.
- **FR-002**: The demo host MUST NOT expose MOBApi's runtime endpoints, runtime commands, solution endpoints or runtime hub, and MUST NOT be able to reach a MOBApi instance or any hardware.
- **FR-003**: The demo MUST NOT simulate a railway or run workflows: there is no motion model, no runtime state of locomotives, signals, turnouts or track power, and no workflow execution. Every page shows a prepared state from the example data.
- **FR-005**: Visitors MUST be able to interact only with: the navigation between the four demo pages, the theme toggle, the Train Control speed slider (it moves only itself, FR-036) and the dismissible narrow-window notice (FR-033, decided on 2026-11-14). Every other control is inert (FR-035), including the expander of the Messages bar, which is shown expanded (FR-031).
- **FR-006**: Visitors MUST NOT be able to create, change, delete, import, export or save any content.
- **FR-007**: The demo MUST NOT persist visitor state on the server and MUST NOT use accounts, sign-in or tracking identifiers.
- **FR-010**: The demo MUST show dedicated static example data and assets made for it: an example layout, example locomotives with their function names as data (German names such as "Stirnlicht" are example data, not UI text), an example timetable, an example workflow and an example signal box. It MUST NOT read, import or derive data from a desktop solution at build time or at run time. The one derived asset, the example layout, is rendered once from the shipped sample track plan outside the build (FR-040). Until the maintainer provides own photographs, the locomotive images are self-drawn SVG placeholders; catalogue images are not used (COO decision 2026-11-23, #430). Own photographs are added later as WebP (FR-058).
- **FR-012**: The only state is the user interface of the open tab (current page, slider position and whether the narrow-window notice was dismissed). It lives only as long as the visit; a reload starts fresh. Nothing but a manual theme choice is written to the browser's storage, so that a returning visitor keeps Light or Dark.

Removed with the narrowing of 2026-11-17: FR-004 (safe start of simulated locomotives), FR-008 (Reset), FR-009 (visual speech and sound actions) and FR-011 (browser simulation). Their numbers are not reused.

**Shell**

- **FR-020**: The UI text MUST be English. Sample data keeps its own language.
- **FR-021**: The demo MUST support Light and Dark like the app, with a theme toggle that switches without a reload and keeps the current page and slider position. A first visit MUST show Dark, independent of the browser's color-scheme preference; a visitor's manual choice is remembered in the browser (FR-012).
- **FR-022**: The shell MUST reproduce MOBAflow's structure: a left navigation pane with icon and label entries in MOBAflow's order and grouping (Overview; Train Control; Journeys, Timetable, Event Manager, Journey Map; Solution, Workflows, Stations, Locomotives, Passenger Wagons, Goods Wagons, Trains; Track Plan, Signal Box, ESP32 Display, Matrix Images; Monitor, Recorder; Help, Info, Settings) with their Preview badges; a top command bar; a Messages bar; a status bar at the bottom. The entries, order and icons are taken from the application's page registration (`MOBAflow/Service/NavigationRegistration.cs`), and the Preview badges from the feature toggle registry (`SharedUI/Configuration/FeatureToggleRegistry.cs`) with the labels of the shipped `MOBAflow/appsettings.json`, not from this list. This includes the bold entries (`IsBold`: Train Control and Journeys) and the custom path icon of Signal Box (`PathIconData`).
- **FR-023**: Colors, typography, spacing and icons MUST be derived from the repository's WinUI resources (`MOBAflow/App.xaml` theme dictionaries for Light, Dark and High Contrast, `MOBAflow/Resources/`, `MOBAflow/Themes/`, the Segoe Fluent icon glyphs of the page registration) and mapped to named design tokens, so that a later change in the app can be followed. The accent color is not defined in the repository: MOBAflow uses the Windows system accent, which is magenta on the maintainer's machine. The demo MUST use one fixed magenta accent, the same for every visitor, sampled from the maintainer's reference screenshots in the plan phase. It applies only where WinUI uses the system accent (`SystemAccentColor` and the `AccentFillColor*` and `AccentTextFillColor*` brushes); the explicit non-accent colors defined in `MOBAflow/App.xaml` (for example `TrackPlanStrokeSelectedBrush` and the `MobaPanelAccent*Brush` values) are kept as they are.
- **FR-024**: Fidelity MUST be checkable against eight reference screenshots of the WinUI app supplied by the maintainer (four pages, each in Dark and Light, 1920 by 1080 pixels, 100 % scale). Per page and theme the following MUST match: shell structure; navigation entries, order, grouping, icons and Preview badges; command bar; Messages bar and status bar layout; surface and accent colors by value; the complete inventory of controls on the page; the arrangement of its panes.
- **FR-025**: Permitted differences are limited to: font rendering and fallback (Segoe UI Variable, Segoe UI, system font), the browser's native scrollbars, the Mica window material shown as a flat tint, and the content differences this specification requires (disabled entries and controls, demo texts in the status bar).
- **FR-026**: Reference screenshots from the maintainer's machine MUST NOT be committed, because they show local paths. If reference images are wanted in the repository, they are produced without host details.
- **FR-027**: Navigation entries outside the demo MUST be shown disabled (greyed out), not hidden, in their WinUI position. They MUST be exposed as disabled to assistive technology with the description "Available in the MOBAflow desktop app (not included in this web demo)", do nothing on activation, and show that explanation on hover. Keyboard navigation through the disabled entries is not required. Their text MUST keep a contrast of at least 3:1 in Light and Dark. The four demo entries are shown at full contrast with the accent selection indicator.
- **FR-028**: The top bar MUST reproduce the main window's layout: the Light/Dark toggle switch on the left, which is active, and the command bar on the right with New, Open, Simulate with its inline "Feedback Point (InPort)" number box, and Reset. The command-bar controls are shown in their WinUI appearance and are inert (FR-035); New and Open carry the explanation "Fixed example project in the web demo".
- **FR-029**: The status bar MUST keep the four segments of the main window's status bar, with their labels and demo texts: (1) "Solution: MOBAflow demo layout"; (2) the Z21 status "Z21: Not connected (web demo)" and the "Track Power" toggle switch in its prepared position, inert; (3) "REST API: Not available in the web demo"; (4) "Speech: Not available in the web demo" with its provider dropdown shown inert. No segment shows a local path, a port, a machine name or an endpoint.
- **FR-030**: No status bar text and no other UI text, tooltip, message, page source comment or error message MUST expose a host path, port, machine name, internal address or real endpoint.
- **FR-031**: The Messages bar MUST reproduce the main window's expander with its summary text, the Errors, Warnings and Information filter toggle buttons with their counts, and the Refresh button. It is shown expanded; the expander, the filters and Refresh are inert (FR-035). On the first visit it holds one pre-populated Info entry: "Visual web demo: prepared example views, no hardware connected. Switch between the pages and the theme and try the speed slider; the other controls work in the MOBAflow desktop app." This orientation text MUST be visible immediately. There is no modal dialog and no marketing header.

- **FR-032**: A visitor MUST land on Train Control with the flagship example locomotive selected, showing the prepared values of the example data.
- **FR-033**: The full layout with all panes MUST be shown from a window width of 1280 pixels. From 1024 to 1279 pixels the navigation collapses to its compact rail and side panes may fold. Below 1024 pixels the demo MUST keep the desktop geometry with a minimum width of 1024 pixels and horizontal scrolling and show a dismissible notice "Desktop UI demo - best on a screen at least 1024 px wide"; there is no stacked mobile layout.

**Read-only presentation**

- **FR-035**: Toolbars, catalogues and property panes MUST NOT be hidden. Every control outside FR-005 is shown in its WinUI appearance in the prepared state and is inert: activating it changes nothing, and a hint on pointer hover and focus explains "Available in the MOBAflow desktop app". To assistive technology such controls are exposed as unavailable with that description. Property fields are shown read-only at full legibility.
- **FR-036**: Train Control MUST show: the page title, the series search box, the EMERGENCY STOP button, the locomotive list as cards with photo and name, the direction toggle, the DCC speed steps selection, the acceleration ramp toggle, the timetable and stops list, the three cab-control tiles (Brake toggle, Door release toggle and the Door blocking status indicator), the speedometer gauge (0 to 400 km/h) with digital readout, the speed slider (0 to 126), the current gauge with its values, and the functions list F0 to F31 with icon, number and name, all in the prepared state of the example data. Active: only the speed slider; dragging it moves its thumb and its own value text and nothing else. Everything else is inert (FR-035).
- **FR-037**: Workflows MUST show the three panes of the app in a prepared state: the workflows list with the example workflow selected, the actions pane in the Graph view with the trigger node connected to the action nodes, its toolbar, and the properties pane with read-only fields. All of it is inert (FR-035); nothing runs.
- **FR-038**: Track Plan MUST show, in a prepared state, the toolbar, the Piko A-Track catalogue, the canvas with the example layout fitted to the view, the zoom slider with percentage, the context pane and the footer with status text and coordinates. All of it is inert (FR-035).
- **FR-039**: Signal Box MUST show, in a prepared state, the elements toolbox, the canvas with the example track elements, turnouts and a Ks signal, the context pane with the selected signal's aspect tiles (the prepared aspect marked) and the multiplexer configuration, and the element count footer. All of it is inert (FR-035).
- **FR-040**: The example layout on Track Plan MUST be a static SVG asset with the same track geometry as in MOBAflow, rendered from the shipped sample track plan `MOBAflow/track-plan.json` with the existing `TrackPlan.Renderer` by a test or tool, without launching the app, and committed with the other assets; it is not produced at build time or at run time. If the renderer's output cannot be made free of inline styles (FR-071), a hand-drawn SVG of the same layout is the fallback. Decided in the review of the plan on 2026-11-18.

**Accessibility (basic floor)**

The demo is operated primarily by mouse or pointer (maintainer decision of 2026-11-14). The following floor stays, because standard HTML and control behavior provide it at little cost.

- **FR-041**: Every control, active or inert, MUST have an accessible name that a screen reader reads, including EMERGENCY STOP, the function buttons, the aspect tiles with their aspect code, the theme switch and the disabled navigation entries with their description (FR-027). Focus MUST be visible. The navigation entries of the demo pages, the theme switch and the speed slider MUST be reachable with Tab and operable with the keyboard as browsers do by default.
- **FR-042**: The speed slider MUST expose its range 0 to 126 and a value text of the form "Step X". It is operated with the pointer; the browser's default slider keys are sufficient, and a custom key map is not required.
- **FR-043**: The gauges MUST expose their prepared value as text. EMERGENCY STOP is shown as a button and is inert like every control outside FR-005.
- **FR-044**: Function buttons and the Brake and Door release tiles MUST expose their prepared pressed state; the Door blocking indicator and other tiles that only show status convey their state by icon or text in addition to color.
- **FR-045**: Color MUST NOT be the only cue: the aspect tiles show the aspect code as text and the light arrangement, workflow nodes show their state as text or icon, and status elements carry text or an icon.
- **FR-046**: With the reduced-motion preference set, the demo MUST NOT animate transitions, and a flashing aspect MUST be shown as steady with the text "Flashing".
- **FR-047**: Not part of the demo: keyboard operation of canvas elements, a custom slider key map (Page Up, Page Down, Home, End), the desktop app's keyboard shortcuts (for example for emergency stop and track power), and keyboard navigation through the disabled navigation entries.

**Hosting, build and deployment**

- **FR-049**: The demo MUST support at least 12 concurrent visitors. The daily bandwidth quota, not concurrency, limits the demo: at the transfer budget of FR-057 the 165 MB allow 110 first visits per day (165 MB divided by 1.5 MB), fewer when visitors load more than the first page's files. A sustained higher demand needs a paid tier; that is a later maintainer decision and out of scope.
- **FR-050**: The demo MUST be deployable to the decided host: Azure App Service `mobaflow-demo.azurewebsites.net`, Free tier F1, France Central, ASP.NET Core on .NET 10 under Linux.
- **FR-051**: The demo MUST stay within the Free tier limits in normal use: server work per visitor close to zero (static files only), no per-visitor server memory, no WebSocket or other live connection, and a first visit within the transfer budget of FR-057.
- **FR-052**: The demo MUST tolerate a cold start: the first response after an idle period shows a loading state and then the complete shell.
- **FR-053**: The code MUST live in `MOBAdemo/` with its own Node/npm build for the React app and its own CI job; it MUST NOT be part of the WinUI or Android build graphs, and building MOBAflow or MOBAsmart MUST NOT require Node.
- **FR-054**: Deployment to the Azure web app MUST be possible through a GitHub Actions workflow without a secret in the repository. It is started manually at first; automatic deployment on merge follows later as its own change. The workflow and its credentials are created in the implementation, not by this specification.
- **FR-055**: Because the tier has no deployment slot, a deployment replaces the running app; the demo MUST come back without manual steps.
- **FR-056**: The host MUST limit requests per client address with ASP.NET Core rate limiting: a limit of 60 requests per minute on all requests that are not static files (404s and rejected methods included) and a limit on static-file requests that is a request count sized to the transfer budget: ten first-visit equivalents per hour, which approximates about 15 MB per hour at the average file size of a first visit (decision of 2026-11-24; a byte-charged limit was dropped, so a client that repeatedly fetches only the largest file can exceed 15 MB per hour, bounded by FR-075). The values are fixed in the plan phase. This protects the CPU quota and bounds a single client's bandwidth; it does not prevent a distributed flood from using up the daily 165 MB, which is an accepted residual risk on the Free tier (FR-075).
- **FR-078**: The host MUST determine the client address for rate limiting from the forwarded headers of the App Service front end, with ASP.NET Core forwarded headers handling enabled for that front end: on App Service on Linux, TLS ends at the platform front end and the app sees the front end's address unless it processes the forwarded headers, and ASP.NET Core ignores forwarded headers from proxies it has not been told to trust. The front end has no fixed address range to list, so the host accepts the forwarded client address from the platform front end. The trade-off is accepted and documented: a forged forwarded address could at most dodge or misdirect the rate limit, because nothing else in the demo depends on the client address. The deployment slice (slice 20) verifies on the real app that the limit acts per visitor and not on the front end as a whole.
- **FR-057**: A first visit MUST transfer at most 1.5 MB compressed, including the application, the example data of the landing page and the locomotive photos shown there. The budget MUST be checked in CI or by a documented, repeatable measurement, and a change that exceeds it fails that check.
- **FR-058**: Responses MUST be compressed with Brotli or gzip; own locomotive photographs, once provided, MUST be resized to their displayed size and delivered as WebP; until then the locomotive images are self-drawn SVG placeholders. All other images MUST be compressed as well. Images are imported from `src/assets/photos` so that the served file names are content-hashed (COO decision 2026-11-23, #430).
- **FR-059**: Static files MUST carry content-hashed file names and long-lived cache headers, so that a repeat visit transfers almost nothing; the entry document is revalidated so that a new deployment is picked up.
- **FR-076**: The application's scripts and style sheets together MUST NOT exceed 200 KB compressed with Brotli. This ceiling lies inside the first-visit budget of FR-057 and MUST be checked the same way: in CI or by a documented, repeatable measurement, with a change that exceeds it failing the check.
- **FR-077**: To keep FR-076, the shell (navigation, command bar, split panes, cards, Messages bar, status bar) and the page views (gauges, workflow graph, track plan, signal box) MUST be built with plain style sheets whose custom properties carry the tokens of FR-023. A component library may supply interactive primitives only (for example slider, switch, tooltip, badge) and only through imports that bring in the used parts; a whole component or icon package MUST NOT be bundled, and only the icons the demo shows (about 30) are included.
- **FR-070**: The host MUST serve static files and a health response only: no dynamic endpoint, and no endpoint that accepts visitor data or changes server state.
- **FR-071**: The host MUST send security headers: a content security policy without inline scripts and with `frame-ancestors 'none'`, `X-Content-Type-Options: nosniff`, a `Referrer-Policy`, and a cross-origin resource policy that keeps images and other files for the demo's own pages.
- **FR-072**: The host MUST NOT send a server banner and MUST NOT show detailed error pages; an error shows a short neutral page.
- **FR-073**: The demo MUST ship a `robots.txt`. Default: allow indexing of the start page only. The maintainer may choose otherwise without a change to this specification.
- **FR-074**: The `MOBAdemo/` CI job MUST install from a committed lockfile and run an npm dependency audit that fails on known high or critical vulnerabilities.
- **FR-075**: Quota exhaustion on the Free tier is accepted: the platform then answers HTTP 403 until the reset. The operator documentation in `MOBAdemo/` MUST say that quota use and exhaustion are visible in the Azure portal's quota view of the web app.

### Key Entities

- **Example data**: The static, read-only content made for the demo: example locomotives (name, photo, function list with names, prepared speed and function states), an example timetable, an example workflow with its nodes and their positions, an example signal box (elements, turnout positions, a signal with its aspects and the prepared aspect), and the texts of the status bar and the Messages bar.
- **Example layout**: The static SVG asset of the example track plan (FR-040).
- **Navigation model**: The entries, order, groups, icons, bold flags and Preview badges of the app's navigation, with the four demo entries marked as available.
- **View state**: The open tab's current page, slider position and notice state, and the remembered theme choice.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time visitor opens all four pages and switches the theme within 60 seconds of the page being ready, without instructions outside the page.
- **SC-002**: For each of the eight reference screenshots, a reviewer finds every item of the fidelity checklist (FR-024) matching; any difference is one of the permitted differences (FR-025).
- **SC-003**: A search of the delivered pages, scripts and responses finds no host path, port, machine name or internal address, and a request to any MOBApi runtime path is not served.
- **SC-004**: None of the inert controls changes anything: a check of every control listed in FR-028, FR-029 and FR-035 to FR-039 leaves the page identical apart from its hint.
- **SC-005**: Dragging the speed slider changes only the slider and its value text; the gauges, the readout and the rest of the page stay identical.
- **SC-006**: A first visit transfers at most 1.5 MB compressed, of which scripts and style sheets are at most 200 KB, so that 110 first visits fit into the daily bandwidth of the Free tier; a repeat visit with a warm browser cache transfers under 100 KB.
- **SC-011**: Twelve visitors using the demo at the same time each get a responsive page, and the host's CPU and memory use does not rise with their number beyond serving the files.
- **SC-007**: After the first load, switching between the pages requests at most the static files not loaded yet, so that the daily CPU quota is not the limiting factor.
- **SC-008**: After an idle period, the complete shell is usable within 15 seconds of the first request on a normal home connection; a warm page is usable within 3 seconds.
- **SC-009**: Every task of User Stories 1 to 3 can be completed with a mouse or pointer alone; every control has an accessible name; the demo's navigation entries, the theme switch and the slider are reachable with Tab; and an automated accessibility check of the four pages in both themes reports no critical finding for names, contrast and non-color cues.
- **SC-010**: Building MOBAflow and MOBAsmart works on a machine without Node, and the `MOBAdemo/` CI job runs without the WinUI or Android workloads.

## Open questions

None. The questions Q1 to Q10 of the first draft, the initial theme and the indexing default were decided on 2026-11-14; the scope was narrowed on 2026-11-17. Left to the plan phase, as stated where they occur: the exact accent value sampled from the reference screenshots (FR-023), the rate-limit values (FR-056) and the details of rendering the example layout asset (FR-040). The maintainer may change the `robots.txt` default (FR-073) at any time.

## Assumptions

- Implementation starts after the refactoring programme #47; this specification does not authorize it.
- The host references no MOBAflow project; the example layout asset is rendered once from the shipped sample track plan with the existing renderer, outside the demo's build (FR-040).
- The Technical Director provides the Azure web app; the specification creates no Azure resource, secret, publish profile or workflow.
- The maintainer provides the eight reference screenshots outside the repository.
- A desktop browser in a current version (Edge, Chrome, Firefox, Safari) with JavaScript is the target; phones are not.
- Cold starts occur on the Free tier, because Always On is not available there; their duration is measured in the plan phase.
- "Read-only" means no content change; the slider position, the current page and the theme are view state, not content.
- The Free tier limits in this document were read on 2026-11-14 and are re-read before implementation.
