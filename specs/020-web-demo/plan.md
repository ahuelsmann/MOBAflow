# Implementation Plan: MOBAflow web demo

**Branch**: `020-web-demo` | **Date**: 2026-11-14, revised 2026-11-16 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #430

**Spec Kit**: Required

**Status**: Draft. Planning only: no `MOBAweb/` code, no workflow, no Azure change and no `tasks.md` exist. The first implementation slice starts once the refactoring programme #47 is complete.

**Input**: Feature specification from `/specs/020-web-demo/spec.md`

**Decision record**: The maintainer decisions behind the specification are linked in its Clarifications section. The decision that the demo is operated by mouse, with a basic accessibility floor (spec FR-041 to FR-047), is recorded in [this comment on #430](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883730125) (2026-11-14, 20:27 UTC).

**Note**: Written by hand from the plan template override; `$speckit-plan` was not run. Research results are folded into [Open technical choices](#open-technical-choices) instead of a separate `research.md`.

## Summary

A public, read-only web demo of four MOBAflow pages (Train Control, Workflows, Track Plan, Signal Box) that looks like the WinUI app and needs no hardware. The decided shape (spec FR-011, FR-012): the simulation runs in the visitor's browser; a small ASP.NET Core host serves the static React build and one read-only sample snapshot; no per-visitor server state, no live connection, no runtime endpoint. The host runs on the Azure App Service Free tier, whose daily 165 MB of outgoing data is the binding limit, so the plan is built around a first-visit budget of 1.5 MB with at most 200 KB of scripts and style sheets (FR-057, FR-076).

Technical approach in one line: a build step turns the shipped sample project into a static snapshot (JSON, one track plan SVG, resized WebP photos); a React app with a plain-CSS shell and tokens mapped from MOBAflow's WinUI resources runs a small TypeScript simulation over that snapshot; the host only serves files, with compression, cache headers, security headers and rate limiting.

## Technical Context

**Language/Version**: Host: .NET 10 / C# (repository defaults, `Microsoft.NET.Sdk.Web` like MOBApi). Frontend: TypeScript with React; Node version pinned in `MOBAweb/` (choice T1).

**Primary Dependencies**: Reused: `Domain` (sample project and `TrackPlanDocument`), `TrackPlan.Renderer` with `TrackLibrary.Base` and `TrackLibrary.PikoA` (track plan SVG), the shipped sample data (`MOBAflow/solution.json`, `MOBAflow/track-plan.json`, `MOBAflow/photos/`). New: React, a build tool and a test runner for the frontend (T1), optionally individual Fluent UI React v9 primitives (T3). No `Backend`, no `MOBApi`, no SignalR.

**Storage/Protocols**: None on the server. One read-only snapshot over HTTPS. The browser stores only the theme choice (FR-012).

**Testing**: NUnit for the snapshot generator and the host (portable target, Linux CI). A frontend unit test runner for the simulation (T1). A browser test for smoke, budget and accessibility-floor checks (T6).

**Target Platforms**: ASP.NET Core on .NET 10 under Linux (Azure App Service Free tier F1); current desktop browsers.

**Affected Layers**: A new folder `MOBAweb/`. No change of behavior in Domain, Common, Backend, SharedUI, MOBAflow, MOBAsmart, MOBApi or MOBAdisplay. The solution file, the architecture dependency table and CI gain the new project(s).

**Performance Goals**: First visit at most 1.5 MB compressed, of which scripts and style sheets at most 200 KB with Brotli; repeat visit under 100 KB; no server request for the simulation after the first load; usable within 15 seconds after a cold start and 3 seconds warm (SC-006 to SC-008, SC-011).

**Data and API Effects**: No existing JSON format, configuration default, API or Z21 protocol changes. The snapshot is a new, demo-only read model, versioned with the build; it is not MOBApi's runtime contract.

**Scale/Scope**: Four pages, one sample project (2 locomotives, 2 workflows, one track plan, one signal-box plan in the shipped sample), at least 12 concurrent visitors, about 110 first visits per day within the Free tier.

## Constitution Check

*GATE: Every item MUST pass before Phase 0 research and be re-checked after Phase 1 design.*

- [x] Layer and platform boundaries are preserved. The demo host references only platform-neutral libraries; no WinUI or MAUI type, no `Backend` Z21 runtime. The new project gets its row in the documented dependency table that `SolutionArchitectureTests` enforces.
- [x] EventBus UI-thread marshalling remains centralized in `UiThreadEventBusDecorator`. Not touched; the demo does not use the EventBus.
- [x] Async flows use `await` without sync-over-async. Applies to the host and the generator.
- [x] UI behavior uses ViewModel commands; UI resources support Light and Dark themes. The web UI is not WinUI; the equivalent rule here: behavior lives in the simulation and state modules, components only render and forward input, and every color comes from a Light/Dark token (FR-021, FR-023).
- [x] All user-visible UI strings are English (FR-020); sample data keeps its language.
- [x] Data and API effects are explicit; no legacy path or migration.
- [x] Meaningful regression tests cover changed behavior (see Validation Strategy); this documentation-only change uses structural checks.
- [x] Validation commands follow the change matrix in `AGENTS.md`; the frontend adds its own npm commands, documented with the implementation.
- [x] The design reuses established services, helpers and serializers: `TrackPlan.Renderer` for the drawing, the Domain serializers for the sample, `PhotoPathHelper` rules for photo paths where the generator resolves them.
- [x] The specification and plan reference the authoritative GitHub issue (#430).
- [x] Spec Kit artifacts remain below `specs/`.
- [x] Sensitive files: none are read. The implementation adds no secret to the repository; deployment credentials live in GitHub and Azure (FR-054). Changed files are scanned before commit in local sessions; cloud sessions record the missing local scan.
- [x] Sonar code analysis runs only in GitHub CI. Open point: whether the SonarCloud project analyses TypeScript under `MOBAweb/` is checked when the first frontend code arrives (T7).
- [x] The PR remains draft until SonarCloud is green with zero OPEN/CONFIRMED issues.

Re-check after design: the one tension is the constitution's wording "UI behavior uses ViewModel commands", written for WinUI and MAUI. It is met in spirit as described above and needs no exception; if the maintainer wants the constitution to name web UIs, that is a separate amendment.

## Project Structure

### Documentation (this feature)

```text
specs/020-web-demo/
|-- spec.md
|-- plan.md          # this file
`-- tasks.md         # not yet; the slice table in this plan is the cut the tasks follow
```

No `research.md`, `data-model.md`, `quickstart.md` or `contracts/`: the governance check does not require them, the recent features in this repository carry `spec.md`, `plan.md` and `tasks.md` only, and the content they would hold is short enough for the sections below.

### Source Code (planned; nothing exists yet)

```text
MOBAweb/
|-- Host/                      # ASP.NET Core host (Moba.Web): static files, snapshot, headers, rate limiting
|-- SnapshotGenerator/         # .NET console tool, build time only: sample -> snapshot.json, track-plan.svg, photos/*.webp
|-- app/                       # React + TypeScript frontend (own package.json and lockfile)
|   |-- src/shell/             # navigation, command bar, Messages bar, status bar
|   |-- src/pages/             # train-control, workflows, track-plan, signal-box
|   |-- src/simulation/        # motion model, functions, signals, turnouts, workflow runner, reset
|   |-- src/tokens/            # CSS custom properties mapped from MOBAflow's WinUI resources
|   `-- src/snapshot/          # snapshot types and loader
`-- README.md                  # build, run, budgets, operator notes (quota view, deployment)
Test/MOBAweb/                  # NUnit: generator and host tests (portable target)
.github/workflows/             # one new CI job and, later, one manually started deployment workflow
```

**Structure Decision**: Everything demo-specific lives in `MOBAweb/`; shared projects are referenced, never changed for the demo. The generator is separate from the host so that the deployed host carries no renderer and does no work per request (see T2). The project names and whether the generator is its own project or a build target of the host are settled in the tasks phase. `Moba.slnx`, the dependency table in `docs/ARCHITECTURE.md` and the repository map in `AGENTS.md` gain the new projects in the implementation.

## Design

### Snapshot (read model)

Produced at build time from the shipped sample and served as static files:

| Part | Content | Source |
| --- | --- | --- |
| `snapshot.json` | Demo project name; locomotives (id, name, function labels, symbols and colors F0 to F31, photo file name, the gauge scale of 400 km/h and the locomotive's maximum speed as the marker on it); workflows with their steps (name, type, summary, order) and precomputed graph positions; the signal-box plan (elements, turnouts with initial position, signals with available and initial aspects); the navigation model (entries, order, groups, icons, bold flags, badges, demo or disabled); status bar and Messages texts | `MOBAflow/solution.json` through the Domain serializers; `MOBAflow/Service/NavigationRegistration.cs` for the navigation model (T4) |
| `track-plan.svg` | The sample layout drawn by `TrackPlan.Renderer`, with a stable id per track piece so that the frontend can select pieces and place locomotives | `MOBAflow/track-plan.json` or the project's `trackPlan` |
| `track-path.json` | The ordered centre-line path of the layout in drawing coordinates, for the motion model | derived by the generator from the same geometry |
| `photos/*.webp` | Locomotive photos resized to their displayed size | `MOBAflow/photos/locomotives/` (the maintainer's own photographs) |

The generator fails the build when the sample lacks what a page needs (for example a signal with aspects), so that a change of the shipped sample cannot silently empty a demo page.

### Browser simulation

- **State**: one in-memory store per tab: locomotives (speed step, direction, ramp target, functions, path position), track power, signals, turnouts, workflow runs, messages. Reset replaces it with the initial state built from the snapshot. Only the theme goes to `localStorage`.
- **Motion model**: speed step 0 to 126 maps linearly to the gauge's full scale of 0 to 400 km/h, as the app does (`TrainControlDccSpeed.SpeedStepToKmh` with `DefaultSpeedGaugeMaxKmh = 400`); the locomotive's maximum speed is only the marker on the gauge, not the end of the scale; position advances along `track-path.json` with the frame clock, wrapping around the layout; the gradual ramp moves the actual step toward the target by a fixed step per interval, with the defaults of `TrainControlSettings` (step 5 every 100 ms); emergency stop sets step and target to 0 at once; track power off stops all motion.
- **Workflow runner**: plays the steps in order with their delays; announcement and sound steps produce a notification and a Messages entry (FR-009); steps that would act on a locomotive or signal act on the simulated state.
- **Feedback simulation**: Simulate with the chosen feedback point produces a Messages entry and, where the sample binds that point to a workflow, starts it (slice 17). This is the visible effect that spec FR-028 left to this phase.
- **Current gauge**: plausible values derived from the number of moving locomotives, marked as simulated.

### Frontend construction

- Shell and domain canvases in plain CSS with custom properties (FR-077). `src/tokens/` holds one file per theme generated or transcribed from `MOBAflow/App.xaml`, `MOBAflow/Resources/` and the Fluent 2 values WinUI uses; the accent token is the fixed magenta sampled from the reference screenshots (T5).
- Icons: only the glyphs the demo shows (about 30), as inline SVG; the Signal Box entry uses its path data from the page registration.
- Canvases: the track plan is the generated SVG; the signal box and the workflow graph are SVG drawn by the frontend from the snapshot; the gauges are SVG.
- Read-only presentation, disabled navigation entries and the accessibility floor follow spec FR-027, FR-035 to FR-047.

### Host

- Serves the built frontend and the snapshot files; no controller beyond a health response for the platform.
- Compression (Brotli, gzip) with precompressed files produced by the build, so that the host spends no CPU on compression.
- Cache headers: content-hashed files immutable for a year; the entry document and the snapshot manifest revalidated.
- Security headers per FR-071; no server banner; neutral error page (FR-072); `robots.txt` (FR-073). The content security policy uses `style-src 'self'` without `'unsafe-inline'`: the frontend and the generated SVG carry no `style` attributes or inline style elements; they are styled through classes and presentation attributes.
- Rate limiting per client address (FR-056) with forwarded headers handling for the App Service front end (FR-078), as rate-limiter policies partitioned by client address (`PartitionedRateLimiter`): a request-count limiter for non-static requests and a token-bucket limiter for static files whose tokens are bytes. A static request is charged with the size of the precompressed file it will send, known before the response starts, so the per-client cap of about 15 MB per hour holds even when one client downloads the largest file again and again (T8).
- The host project never invokes npm or Node: it serves a prepared `wwwroot` folder, and `dotnet build` of the host works on a machine without Node (SC-010). Producing that folder is the separate build step of slice 6.

### Budgets

| Budget | Limit | First estimate | Check |
| --- | --- | --- | --- |
| Scripts and style sheets, Brotli | 200 KB | React and React DOM about 45 KB; app, simulation and styles about 80 to 120 KB; optional primitives up to 30 KB | CI step on the build output (T6) |
| First visit, compressed | 1.5 MB | scripts and styles 200 KB; snapshot and track data under 60 KB; track plan SVG under 60 KB; 2 photos about 60 KB; icons inline | CI browser test that sums the transferred bytes of a cold load |
| Repeat visit | 100 KB | entry document and revalidation only | same test, second load |

The estimates are assumptions to be measured with the first build; the limits are requirements.

## Open technical choices

Each has a recommendation; none blocks the first slices, and each is confirmed or changed in the slice that owns it.

| # | Choice | Recommendation | Alternatives and why not |
| --- | --- | --- | --- |
| T1 | Frontend toolchain | Vite with React and TypeScript, Vitest for unit tests, npm with a committed lockfile. The major versions of Node (the current LTS at scaffold time) and React are pinned in `package.json` (`engines`, exact dependency majors) and `.nvmrc` and in CI; a major upgrade is its own slice | A heavier framework adds server features the demo must not use and weight against the 200 KB ceiling |
| T2 | Where the track plan drawing and the snapshot are produced | At build time by a .NET generator; the host serves files only | Producing them in the host at start-up costs cold-start time and CPU quota and puts the renderer into the public host; the spec keeps both options open (Assumptions) |
| T3 | Component library | None for the shell; individual Fluent UI React v9 primitives (slider, switch, tooltip, badge) only if their measured cost stays within the ceiling, otherwise small own components | Importing the component or icon packages as a whole breaks FR-076 and FR-077 |
| T4 | Navigation model | Generated from `NavigationRegistration.cs` by the snapshot generator, so that order, icons, bold flags and badges cannot drift | A hand-written list in the frontend drifts from the app; the registration needs MOBAflow types today, so the generator may have to read the source file or a small extracted table; decided in the tasks phase |
| T5 | Accent value | Sample the magenta from the maintainer's reference screenshots and record the hex value as the accent token, with the derived Light/Dark accent variants WinUI computes | Using Windows' own magenta palette value without the screenshots would be a guess |
| T6 | Budget and browser checks | Playwright in the `MOBAweb` CI job: cold and warm load byte sums, a smoke run of each page, and an automated accessibility check with axe-core for names, contrast and non-color cues | A documented manual measurement is allowed by FR-057 but does not stop a regression |
| T7 | Sonar scope | Keep SonarCloud as the single analysis; check on the first frontend pull request whether TypeScript under `MOBAweb/` is analysed and adjust the project configuration only through the existing CI | A second analyser is against the repository policy |
| T8 | Rate-limit values and unit | Per client address: 60 non-static requests per minute, and a static limit counted in transferred bytes, about ten first-visit equivalents (15 MB) per hour, as a `PartitionedRateLimiter` token bucket charged with the precompressed file size of each static response. If the built-in rate-limiting middleware can only take one permit per request, the static branch acquires its permits from the same limiter in a few lines of its own; slice 19 verifies this first | A static limit by request count does not hold the 15 MB: a count sized for many small files lets a client fetch the largest file that many times. A hand-written counter outside the rate-limiter abstraction duplicates what the framework provides. Values are tuned on the real app; spec FR-056 fixes the intent |
| T9 | Forwarded headers on App Service | Enable ASP.NET Core forwarded headers handling for the platform front end (the documented environment switch for App Service on Linux or explicit options), and verify on the real app that the limiter keys on the visitor's address | Without it every visitor shares the front end's address and one limit |
| T10 | Deployment | A manually started GitHub Actions workflow that builds, runs the checks and deploys with federated credentials (no stored publish profile); automatic deployment on merge later as its own change | A publish profile is a long-lived secret; FR-054 excludes secrets in the repository and prefers none at all |
| T11 | Test placement | Generator and host tests in the existing portable test project under `Test/MOBAweb/`; frontend tests inside `MOBAweb/app` | A separate .NET test project is cleaner but waits for RF-30's test split |

## Validation Strategy

- **Automated tests**: NUnit for the generator (snapshot content from the shipped sample, failure when a page's data is missing, stable ids in the SVG, photo sizes) and the host (headers, cache policy, no route beyond files and health, rate limiting with a forwarded client address, the static byte limit under repeated downloads of the largest file, no MOBApi route served). Vitest for the simulation (motion model, ramp, emergency stop, safe start at speed 0, functions, track power, signals and turnouts, workflow runner, reset, isolation of state from storage). Playwright for the budgets, a smoke run per page and the accessibility floor.
- **Builds**: `dotnet build` and `dotnet test` of the new projects on Linux; `npm ci`, `npm run build`, `npm test` in `MOBAweb/app`; the WinUI and Android builds must stay independent of Node (SC-010), shown by their unchanged lanes.
- **Manual checks**: the side-by-side comparison with the eight reference screenshots (FR-024, SC-002) by the maintainer or the Head of UI/UX; a first visit after an idle period on the real app (SC-008); the forwarded-headers check (T9). None is a merge gate by itself beyond what the repository rules say.
- **Regression checks**: the generator test pins the parts of the shipped sample the demo depends on; the architecture test covers the new project's references; the budget checks protect the Free tier.
- **Secrets scan**: no secret is added; local sessions scan changed files, cloud sessions record the missing local scan in the pull request.
- **Sonar gates**: SonarCloud green on the current commit with zero OPEN/CONFIRMED issues before Ready.
- **Issue traceability**: #430 for the feature; one pull request per slice of the table below; task issues, where generated, carry canonical `TNNN:` titles.

## Implementation slices

The implementation is cut for the small-batch flow of 2026-11-16 (master coordination): each slice is one pull request with one concern, at most about 25 files or 400 changed lines (generated files such as the lockfile excepted), mergeable on its own and finished within a day. Each slice names the spec requirements it delivers instead of a separate design note, writes its acceptance tests first and locks only the folders listed. The Chief Operating Officer decides the final cut, the locks and the order on the board; this table is the proposal. Slices that would have exceeded the size limit are already split (shell 8 and 9, Train Control 11 and 12, Track Plan 14 and 15, Workflows 16 and 17).

**Feature flag `enabledPages`.**

- *What and where:* a compile-time constant list of page ids in `MOBAweb/app/src/config/enabledPages.ts`. It is not a runtime setting, not part of the snapshot and not changeable by a visitor. The WinUI feature toggles do not apply, because the demo is a separate application.
- *Default:* empty. A demo page that is not in the list appears in the navigation as a disabled entry, with the mechanism FR-027 requires anyway, so `main` always builds a coherent demo.
- *When it changes:* each page slice adds its own page id in the same pull request in which the page passes its acceptance tests (slices 11, 13, 14 and 16). A page split over two slices is enabled by the first one that makes it usable; the second completes it.
- *When it is removed:* slice 22 deletes the list and its checks once all four pages are enabled; the non-demo entries stay disabled through the navigation model.
- *Public exposure* is separate from the flag: nothing is reachable from outside before the deployment slice and the maintainer's go.

| # | Slice | Delivers | Tests written first | Locked areas | Needs |
| --- | --- | --- | --- | --- | --- |
| 1 | Host skeleton | `MOBAweb/Host` (ASP.NET Core, serves the files of a `wwwroot` folder and a health response), solution entry, dependency table row, repository map row. The project file contains no npm or Node step | Host tests: serves the entry document, serves no MOBApi runtime path, no server banner; the project builds without Node | `MOBAweb/Host`, `Test/MOBAweb`, `Moba.slnx`, the `MOBAweb` rows in `docs/ARCHITECTURE.md` and `AGENTS.md` | #47 complete |
| 2 | Snapshot data | `MOBAweb/SnapshotGenerator` producing `snapshot.json` (locomotives with the 400 km/h gauge scale and the maximum-speed marker, workflows, signal-box plan, texts) from the shipped sample | Generator tests pinning the sample content and the failure on missing page data (FR-010) | `MOBAweb/SnapshotGenerator`, `Test/MOBAweb` | 1 |
| 3 | Snapshot drawing and photos | Track plan SVG with stable ids and without `style` attributes, track path, WebP photos (SkiaSharp is already a repository dependency) | Generator tests: ids per track piece, closed path, no inline styles, photo size limits (FR-040, FR-058) | `MOBAweb/SnapshotGenerator`, `Test/MOBAweb` | 2 |
| 4 | Navigation model | The generator emits the navigation model (entries, order, groups, icons, bold flags, Signal Box path icon, Preview badges) from the page registration (T4) | Generator tests: the model equals the registration's entries and order; a change of the registration changes the model (FR-022) | `MOBAweb/SnapshotGenerator`, `Test/MOBAweb` | 2 |
| 5 | Frontend scaffold | `MOBAweb/app` with the toolchain (T1), lockfile, Light and Dark token files, `enabledPages`, the snapshot loader with a loading state and a failed-load state, the "needs JavaScript" notice in the entry document, and the import rule that forbids whole component and icon packages | Unit tests: both themes define the same token keys; `enabledPages` defaults to empty; the loader shows the loading state until the snapshot arrives (FR-052) and a visible error when a file fails to load (edge case); the built entry document contains the no-JavaScript notice (edge case); the lint rule fails on a whole-package import (FR-077) | `MOBAweb/app` (new) | 1 |
| 6 | Generate-then-build step | One documented command (a script in `MOBAweb/`) that runs the generator, builds the app, precompresses the output and places it into the host's `wwwroot`; the host project itself stays free of npm. The same command is what CI and the deployment call | Script test on Linux: after the command the host serves the built app and the snapshot; a search of the produced files finds no host path, port, machine name or internal address (SC-003, FR-030) | `MOBAweb/` build script, `Test/MOBAweb` | 3, 4, 5 |
| 7 | CI job | The `MOBAweb` job: .NET build and tests, the command of slice 6, unit tests, dependency audit, the 200 KB check (FR-053, FR-074, FR-076) | The job itself; a deliberately oversized bundle fails in a throwaway branch | `.github/workflows`, `scripts/Get-QualityChangeScope.ps1` | 6; local session (workflow files) |
| 8 | Shell frame | Navigation from the generated model with disabled entries and their explanation, theme toggle with Dark default and remembered choice, layout widths and the narrow-window notice (FR-021 to FR-027, FR-033) | Unit tests: navigation order, bold entries and disabled state; disabled entries carry the accessible description and do nothing; theme default and persistence; contrast of disabled entries from the tokens (FR-027) | `MOBAweb/app/src/shell/navigation`, `MOBAweb/app/src/tokens` | 4, 5 |
| 9 | Shell bars | Command bar (New and Open disabled with their explanation; Simulate with its number box and Reset present but disabled until slices 11 and 17 wire them), Messages bar with expander, filters and the first-visit entry, status bar with the four demo segments (FR-028 to FR-031) | Unit tests: status texts and all shell texts contain no host detail (FR-030); first-visit entry visible; a failed file load adds a Warning entry (edge case); New and Open change nothing (SC-004) | `MOBAweb/app/src/shell/bars` | 8 |
| 10 | Simulation core | State store, motion model against the 400 km/h scale, ramp, emergency stop, functions, track power, reset to the initial state; no UI (FR-003, FR-004, FR-008, FR-011, FR-012) | Unit tests from the acceptance scenarios of User Stories 1 and 6, including safe start at speed 0 and no demo state in browser storage | `MOBAweb/app/src/simulation` | 5 |
| 11 | Train Control: controls | Locomotive list, search, direction, speed slider, EMERGENCY STOP, ramp toggle, functions, Brake and Door release tiles and the Door blocking indicator; landing page (FR-032); the Reset button is wired to the simulation; the Track Power toggle in the status bar is wired; page enabled | Component tests: scenarios 1 to 4 of User Story 1; Reset restores the initial state (User Story 6); accessible names and pressed states (FR-041 to FR-044); the read-only speed-steps selection changes nothing and carries the read-only mark (FR-035, SC-004) | `MOBAweb/app/src/pages/train-control`, the Reset and Track Power wiring in `src/shell/bars` | 9, 10 |
| 12 | Train Control: gauges and stops | Speedometer with digital readout and maximum-speed marker, current gauge marked as simulated, timetable and stops list | Component tests: scenario 5 of User Story 1 (ramp shown step by step); gauge value exposed without per-frame announcements (FR-043); with reduced motion the gauges change without animation (FR-046) | `MOBAweb/app/src/pages/train-control` | 11 |
| 13 | Signal Box | Signal and turnout state in the simulation and the page of FR-039; page enabled | Tests from User Story 2; aspect code as text and single-choice group (FR-045); a flashing aspect is steady with the text "Flashing" under reduced motion (FR-046); toolbox, rotation and multiplexer controls change nothing and carry the read-only mark (FR-035, SC-004) | `MOBAweb/app/src/pages/signal-box`, `MOBAweb/app/src/simulation/signals` | 9, 10 |
| 14 | Track Plan: view | The page of FR-038 on the generated SVG: zoom, Fit, grid, labels, selection into the context pane, browsable catalogue; page enabled | Tests from scenarios 1 to 3 of User Story 3; editing tools and catalogue dragging change nothing and carry the read-only mark (FR-035, SC-004); pan and zoom do not animate under reduced motion (FR-046) | `MOBAweb/app/src/pages/track-plan` | 3, 9 |
| 15 | Track Plan: live | Moving locomotives on the plan, Validate and the validation overlay | Test from scenario 4 of User Story 3: a moving locomotive's position advances along the path | `MOBAweb/app/src/pages/track-plan` | 10, 14 |
| 16 | Workflows: list and run | Workflow runner in the simulation; workflows list with search, actions as a list, read-only properties, run; page enabled | Tests from scenarios 1 to 4 of User Story 4; announcement shown as text (FR-009); add, delete, move and property edits change nothing and carry the read-only mark (FR-035, SC-004) | `MOBAweb/app/src/pages/workflows`, `MOBAweb/app/src/simulation/workflows` | 9, 10 |
| 17 | Workflows: graph and Simulate | Graph view with the trigger node, automatic layout and zoom; the Simulate button and its number box are wired: a Messages entry and the start of the workflow bound to that feedback point | Component tests: List and Graph show the same steps and states; Simulate with a bound feedback point starts the workflow, with an unbound one only the Messages entry (FR-028) | `MOBAweb/app/src/pages/workflows`, the Simulate wiring in `src/shell/bars` | 16 |
| 18 | Host hardening | Serving of the precompressed files, cache headers, security headers with `style-src 'self'`, neutral error page, `robots.txt` (FR-058, FR-059, FR-070 to FR-073) | Host tests per header and route; no route beyond files and health | `MOBAweb/Host`, `Test/MOBAweb` | 1 |
| 19 | Rate limiting | Request limiter and byte-charged static limiter as partitioned policies, forwarded headers (FR-056, FR-078, T8, T9) | Host tests: per-client limits with a forwarded address; repeated downloads of the largest file hit the byte limit; other clients are not limited | `MOBAweb/Host`, `Test/MOBAweb` | 18 |
| 20 | Browser checks | Playwright with axe-core: first-visit and repeat-visit byte sums, a smoke run per page, the accessibility floor, the cold-load loading state, the no-JavaScript notice, and a search of every response for host details (FR-057, SC-003, SC-006, SC-009, FR-052) | The checks themselves | `MOBAweb/app/e2e`, `.github/workflows` | 11; local session (workflow files) |
| 21 | Deployment | Manually started deployment workflow and operator README (FR-054, FR-055, FR-075) | A deployment to the prepared web app, verified by the smoke run against the public address; the forwarded-headers check on the real app (T9) and the cold-start time (SC-008) recorded | `.github/workflows`, `MOBAweb/README.md` | 18 to 20; local session; Technical Director's Azure setup; maintainer's go |
| 22 | Flag removal and fidelity fixes | Removal of `enabledPages`; corrections from the side-by-side comparison with the reference screenshots (FR-024, SC-002), as several small slices if needed | Updated component tests where behavior changes; the navigation tests no longer depend on the flag | the page or shell folder concerned, `src/config` | 11, 13, 14, 16; the reference screenshots |

**Requirements without an automated test, deferred on purpose:** SC-001 (a first-time visitor drives within 60 seconds) and SC-002 (side-by-side comparison) are human checks in slice 22; SC-008 (cold start within 15 seconds) and the per-visitor behavior of the rate limit behind the real front end (T9) can only be measured on the deployed app in slice 21; SC-011 (twelve concurrent visitors) follows from the design (no per-visitor server work) and is checked in slice 21 by twelve parallel smoke runs, not earlier.

**Parallel work without overlapping locks** (within the limit of three open code slices): 2 and 5 after 1; 3 and 4 after 2 (both lock the generator, so one after the other); 8 and 10 after 5; 13, 14 and 16 in any order after 9 and 10, each in its own folder; 18 and 19 any time after 1.

**First slice once #47 is complete:** slice 1. It touches no existing code area except the solution file and two table rows, needs no Node and no workflow change, and can be written and proved in a cloud session on Linux CI.

**What a cloud session cannot do:** slices 7, 20 and 21 change workflow files and need a local session; everything else is portable.

## Risks

- **Bandwidth quota**: a few heavy visitors or a bot can end the day for everyone; mitigations are the budgets, caching and rate limiting; the residual risk is accepted (spec FR-075).
- **Fidelity without the app**: agents cannot start MOBAflow; the reference screenshots and the XAML sources are the only sources. Deviations are found in the fidelity review, not earlier.
- **Sample drift**: the shipped sample changes for other reasons; the generator test catches missing data, but texts and counts shown in the demo change with it.
- **Renderer reuse**: `PlacedTrackPlanSvgRenderer` and `SvgExporter` were written for export, not for interactive ids; if they cannot emit stable ids per piece without a change, the generator post-processes the SVG instead of changing the shared renderer for the demo.
- **Workflow files from the cloud**: a cloud session cannot push workflow changes; the CI job and the deployment workflow need a local session.
- **New toolchain**: Node, npm and a browser test runner are new to the repository's CI; they stay inside the `MOBAweb` job and its dependency audit (FR-074).

## Complexity Tracking

No Constitution Check item needs an exception.
