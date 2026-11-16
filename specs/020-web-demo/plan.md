# Implementation Plan: MOBAflow web demo

**Branch**: `020-web-demo` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #430

**Spec Kit**: Required

**Status**: Draft. Planning only: no `MOBAweb/` code, no workflow, no Azure change and no `tasks.md` exist. Implementation starts after the refactoring programme #47.

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
`-- tasks.md         # not yet; created by the tasks phase after this plan is accepted
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
| `snapshot.json` | Demo project name; locomotives (id, name, function labels, symbols and colors F0 to F31, photo file name, maximum speed for the gauge); workflows with their steps (name, type, summary, order) and precomputed graph positions; the signal-box plan (elements, turnouts with initial position, signals with available and initial aspects); the navigation model (entries, order, groups, icons, bold flags, badges, demo or disabled); status bar and Messages texts | `MOBAflow/solution.json` through the Domain serializers; `MOBAflow/Service/NavigationRegistration.cs` for the navigation model (T4) |
| `track-plan.svg` | The sample layout drawn by `TrackPlan.Renderer`, with a stable id per track piece so that the frontend can select pieces and place locomotives | `MOBAflow/track-plan.json` or the project's `trackPlan` |
| `track-path.json` | The ordered centre-line path of the layout in drawing coordinates, for the motion model | derived by the generator from the same geometry |
| `photos/*.webp` | Locomotive photos resized to their displayed size | `MOBAflow/photos/locomotives/` (the maintainer's own photographs) |

The generator fails the build when the sample lacks what a page needs (for example a signal with aspects), so that a change of the shipped sample cannot silently empty a demo page.

### Browser simulation

- **State**: one in-memory store per tab: locomotives (speed step, direction, ramp target, functions, path position), track power, signals, turnouts, workflow runs, messages. Reset replaces it with the initial state built from the snapshot. Only the theme goes to `localStorage`.
- **Motion model**: speed step 0 to 126 maps linearly to 0 up to the locomotive's maximum speed; position advances along `track-path.json` with the frame clock, wrapping around the layout; the gradual ramp moves the actual step toward the target by a fixed step per interval, with the defaults of `TrainControlSettings` (step 5 every 100 ms); emergency stop sets step and target to 0 at once; track power off stops all motion.
- **Workflow runner**: plays the steps in order with their delays; announcement and sound steps produce a notification and a Messages entry (FR-009); steps that would act on a locomotive or signal act on the simulated state.
- **Feedback simulation**: Simulate with the chosen feedback point produces a Messages entry and, where the sample binds that point to a workflow or journey step, starts it. The exact visible effect is fixed with the tasks (spec FR-028 leaves it to this phase): recommended minimum is the Messages entry plus the bound workflow.
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
- Security headers per FR-071; no server banner; neutral error page (FR-072); `robots.txt` (FR-073).
- Rate limiting per client address (FR-056) with forwarded headers handling for the App Service front end (FR-078): a request-count limit for non-static requests and a transferred-bytes limit for static files (T8), so that the per-client cap of about 15 MB per hour holds even when one client downloads the largest file again and again.

### Budgets

| Budget | Limit | First estimate | Check |
| --- | --- | --- | --- |
| Scripts and style sheets, Brotli | 200 KB | React and React DOM about 45 KB; app, simulation and styles about 80 to 120 KB; optional primitives up to 30 KB | CI step on the build output (T6) |
| First visit, compressed | 1.5 MB | scripts and styles 200 KB; snapshot and track data under 60 KB; track plan SVG under 60 KB; 2 photos about 60 KB; icons inline | CI browser test that sums the transferred bytes of a cold load |
| Repeat visit | 100 KB | entry document and revalidation only | same test, second load |

The estimates are assumptions to be measured with the first build; the limits are requirements.

## Open technical choices

Each has a recommendation; none blocks the tasks phase, and each is confirmed or changed there.

| # | Choice | Recommendation | Alternatives and why not |
| --- | --- | --- | --- |
| T1 | Frontend toolchain | Vite with React and TypeScript, Vitest for unit tests, Node LTS pinned in `.nvmrc` and in CI, npm with a committed lockfile | A heavier framework adds server features the demo must not use and weight against the 200 KB ceiling |
| T2 | Where the track plan drawing and the snapshot are produced | At build time by a .NET generator; the host serves files only | Producing them in the host at start-up costs cold-start time and CPU quota and puts the renderer into the public host; the spec keeps both options open (Assumptions) |
| T3 | Component library | None for the shell; individual Fluent UI React v9 primitives (slider, switch, tooltip, badge) only if their measured cost stays within the ceiling, otherwise small own components | Importing the component or icon packages as a whole breaks FR-076 and FR-077 |
| T4 | Navigation model | Generated from `NavigationRegistration.cs` by the snapshot generator, so that order, icons, bold flags and badges cannot drift | A hand-written list in the frontend drifts from the app; the registration needs MOBAflow types today, so the generator may have to read the source file or a small extracted table; decided in the tasks phase |
| T5 | Accent value | Sample the magenta from the maintainer's reference screenshots and record the hex value as the accent token, with the derived Light/Dark accent variants WinUI computes | Using Windows' own magenta palette value without the screenshots would be a guess |
| T6 | Budget and browser checks | Playwright in the `MOBAweb` CI job: cold and warm load byte sums, a smoke run of each page, and an automated accessibility check for names, contrast and non-color cues | A documented manual measurement is allowed by FR-057 but does not stop a regression |
| T7 | Sonar scope | Keep SonarCloud as the single analysis; check on the first frontend pull request whether TypeScript under `MOBAweb/` is analysed and adjust the project configuration only through the existing CI | A second analyser is against the repository policy |
| T8 | Rate-limit values and unit | Per client address: 60 non-static requests per minute, and a static limit counted in transferred bytes, about ten first-visit equivalents (15 MB) per hour. The static limit is charged with the response size of each file, so that repeated downloads of the largest file (worst case) are covered; a custom rate-limiter policy or a small middleware keeps the per-client byte counter | A static limit by request count does not hold the 15 MB: a count sized for many small files lets a client fetch the largest file that many times. Values are tuned on the real app; spec FR-056 fixes the intent |
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
- **Issue traceability**: #430 for the feature; task issues from the tasks phase with canonical `TNNN:` titles.

## Implementation slices

The implementation is cut for the small-batch flow of 2026-11-16 (master coordination): each slice is one pull request with one concern, at most about 25 files or 400 changed lines (generated files such as the lockfile excepted), mergeable on its own and finished within a day. Each slice names the spec requirements it delivers instead of a separate design note, writes its acceptance tests first and locks only the folders listed. The Chief Operating Officer decides the final cut, the locks and the order on the board; this table is the proposal.

**Feature flag.** The demo is a separate application, so the WinUI feature toggles do not apply. Its switch is a list `enabledPages` in the demo's own configuration (default: empty). A page that is not enabled appears in the navigation as a disabled entry, with the mechanism FR-027 requires anyway, so `main` always builds a coherent demo while pages arrive one by one. The public deployment (slice 15) happens only after the maintainer's go; until then nothing of the demo is reachable from outside.

| # | Slice | Delivers | Tests written first | Locked areas | Needs |
| --- | --- | --- | --- | --- | --- |
| 1 | Host skeleton | `MOBAweb/Host` (ASP.NET Core, serves a placeholder page and a health response), solution entry, dependency table row, repository map row | Host tests: serves the entry document, serves no MOBApi runtime path, no server banner | `MOBAweb/Host`, `Test/MOBAweb`, `Moba.slnx`, the `MOBAweb` rows in `docs/ARCHITECTURE.md` and `AGENTS.md` | nothing; first slice |
| 2 | Snapshot data | `MOBAweb/SnapshotGenerator` producing `snapshot.json` (locomotives, workflows, signal-box plan, texts) from the shipped sample | Generator tests pinning the sample content and the failure on missing page data (FR-010) | `MOBAweb/SnapshotGenerator`, `Test/MOBAweb` | 1 |
| 3 | Snapshot drawing and photos | Track plan SVG with stable ids, track path, WebP photos (SkiaSharp is already a repository dependency) | Generator tests: ids per track piece, closed path, photo size limits (FR-040, FR-058) | `MOBAweb/SnapshotGenerator`, `Test/MOBAweb` | 2 |
| 4 | Frontend scaffold | `MOBAweb/app` with the toolchain (T1), lockfile, Light and Dark token files, `enabledPages`, an empty page frame, unit test setup | Unit tests: token files define the same keys in both themes, `enabledPages` defaults to empty | `MOBAweb/app` (new) | 1 |
| 5 | CI job | The `MOBAweb` job: .NET build and tests, `npm ci`, build, unit tests, dependency audit, the 200 KB check (FR-053, FR-074, FR-076) | The job itself; a deliberately oversized bundle fails in a throwaway branch | `.github/workflows`, `scripts/Get-QualityChangeScope.ps1` | 4; local session (workflow files) |
| 6 | Shell | Navigation from the generated model with disabled entries, command bar, Messages bar, status bar, theme toggle with remembered choice (FR-021 to FR-031, FR-033) | Unit tests: navigation order and disabled state, theme default Dark and persistence, status texts contain no host detail (FR-030) | `MOBAweb/app/src/shell`, `MOBAweb/app/src/tokens` | 4, 2 |
| 7 | Simulation core | State store, motion model, ramp, emergency stop, functions, track power, Reset; no UI (FR-003, FR-004, FR-008, FR-011, FR-012) | Unit tests from the acceptance scenarios of User Stories 1 and 6 | `MOBAweb/app/src/simulation` | 4 |
| 8 | Train Control page | The page of FR-036 wired to the simulation; landing page (FR-032); page enabled | Component tests: scenarios of User Story 1, read-only speed-steps selection, accessible names (FR-041 to FR-044) | `MOBAweb/app/src/pages/train-control` | 6, 7 |
| 9 | Signal Box | Signal and turnout state in the simulation and the page of FR-039; page enabled | Tests from User Story 2; aspect code as text (FR-045) | `MOBAweb/app/src/pages/signal-box`, signal part of `src/simulation` | 6, 7 |
| 10 | Track Plan | The page of FR-038 on the generated SVG with zoom, fit, selection and moving locomotives; page enabled | Tests from User Story 3 | `MOBAweb/app/src/pages/track-plan` | 3, 6, 7 |
| 11 | Workflows | Workflow runner in the simulation and the page of FR-037 with list and graph; page enabled | Tests from User Story 4; announcement shown as text (FR-009) | `MOBAweb/app/src/pages/workflows`, workflow part of `src/simulation` | 6, 7 |
| 12 | Host hardening | Precompressed files, cache headers, security headers, neutral error page, `robots.txt` (FR-058, FR-059, FR-070 to FR-073) | Host tests per header and route | `MOBAweb/Host`, `Test/MOBAweb` | 1 |
| 13 | Rate limiting | Request limit, transferred-bytes limit, forwarded headers (FR-056, FR-078, T8, T9) | Host tests: per-client limits with a forwarded address, repeated downloads of the largest file | `MOBAweb/Host`, `Test/MOBAweb` | 12 |
| 14 | Browser checks | Playwright: first-visit and repeat-visit byte sums, smoke run per page, accessibility floor (FR-057, SC-006, SC-009) | The checks themselves | `MOBAweb/app/e2e`, `.github/workflows` | 8; local session (workflow files) |
| 15 | Deployment | Manually started deployment workflow and operator README (FR-054, FR-055, FR-075) | A deployment to the prepared web app, verified by the smoke run against the public address | `.github/workflows`, `MOBAweb/README.md` | 12 to 14; local session; Technical Director's Azure setup; maintainer's go |
| 16 | Fidelity fixes | Corrections from the side-by-side comparison with the reference screenshots (FR-024, SC-002); several small slices if needed | Updated component tests where behavior changes | the page or shell folder concerned | 8 to 11; the reference screenshots |

**Parallel work without overlapping locks** (within the limit of three open code slices): 2 and 4 after 1; 3, 6 and 7 after 4; 8 to 11 one after another or two at a time, because each page has its own folder and the simulation parts of 9 and 11 are separate files; 12 and 13 any time after 1.

**First slice after this plan merges:** slice 1. It touches no existing code area except the solution file and two table rows, needs no Node and no workflow change, and can be written and proved in a cloud session on Linux CI.

**What a cloud session cannot do:** slices 5, 14 and 15 change workflow files and need a local session; everything else is portable.

## Risks

- **Bandwidth quota**: a few heavy visitors or a bot can end the day for everyone; mitigations are the budgets, caching and rate limiting; the residual risk is accepted (spec FR-075).
- **Fidelity without the app**: agents cannot start MOBAflow; the reference screenshots and the XAML sources are the only sources. Deviations are found in the fidelity review, not earlier.
- **Sample drift**: the shipped sample changes for other reasons; the generator test catches missing data, but texts and counts shown in the demo change with it.
- **Renderer reuse**: `PlacedTrackPlanSvgRenderer` and `SvgExporter` were written for export, not for interactive ids; if they cannot emit stable ids per piece without a change, the generator post-processes the SVG instead of changing the shared renderer for the demo.
- **Workflow files from the cloud**: a cloud session cannot push workflow changes; the CI job and the deployment workflow need a local session.
- **New toolchain**: Node, npm and a browser test runner are new to the repository's CI; they stay inside the `MOBAweb` job and its dependency audit (FR-074).

## Complexity Tracking

No Constitution Check item needs an exception.
