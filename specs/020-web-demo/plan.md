# Implementation Plan: MOBAflow web demo

**Branch**: `020-web-demo` | **Date**: 2026-11-14, revised 2026-11-16 and 2026-11-18 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #430

**Spec Kit**: Required

**Status**: Draft. Planning only: no `MOBAweb/` code, no workflow, no Azure change and no `tasks.md` exist. The first implementation slice starts once the refactoring programme #47 is complete.

**Input**: Feature specification from `/specs/020-web-demo/spec.md`, narrowed on 2026-11-17 to a visual showcase.

**Decision record**: The maintainer decisions behind the specification are linked in its Clarifications section. Two shape this plan most: the demo is operated by mouse with a basic accessibility floor ([comment of 2026-11-14](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883730125)), and it is a visual showcase without simulation, workflow execution, solution import or snapshot generator ([comment of 2026-11-17](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6950953706)).

**Note**: Written by hand from the plan template override; `$speckit-plan` was not run. Research results are folded into [Open technical choices](#open-technical-choices) instead of a separate `research.md`.

## Summary

A public web demo of four MOBAflow pages (Train Control, Workflows, Track Plan, Signal Box) that looks like the WinUI app and needs no hardware. It is a visual showcase: navigation between the four pages and the theme toggle are interactive, the Train Control speed slider moves only itself, and everything else is a prepared, inert view built from static example data made for the demo (spec FR-003, FR-005, FR-010, FR-035).

Technical approach in one line: a React app with a plain-CSS shell and tokens mapped from MOBAflow's WinUI resources renders committed example data and assets; a small ASP.NET Core host only serves the built files, with compression, cache headers, security headers and rate limiting. The host runs on the Azure App Service Free tier, whose daily 165 MB of outgoing data is the binding limit, so the plan keeps a first visit within 1.5 MB with at most 200 KB of scripts and style sheets (FR-057, FR-076).

## Technical Context

**Language/Version**: Host: .NET 10 / C# (repository defaults, `Microsoft.NET.Sdk.Web` like MOBApi). Frontend: TypeScript with React; Node version pinned in `MOBAweb/` (choice T1).

**Primary Dependencies**: New: React, a build tool and a test runner for the frontend (T1), optionally individual Fluent UI React v9 primitives (T3). The host references no repository project: no `Domain`, no `Backend`, no `MOBApi`, no SignalR. The example layout asset is produced once with the existing `TrackPlan.Renderer` through MOBAflow's Track Plan SVG export (T2), outside the demo's build.

**Storage/Protocols**: None on the server. Static files over HTTPS. The browser stores only a manual theme choice (FR-012).

**Testing**: NUnit for the host (portable target, Linux CI). A frontend unit test runner for the view logic and the example data (T1). A browser test for smoke, budget and accessibility-floor checks (T6).

**Target Platforms**: ASP.NET Core on .NET 10 under Linux (Azure App Service Free tier F1); current desktop browsers.

**Affected Layers**: A new folder `MOBAweb/`. No change of behavior in Domain, Common, Backend, SharedUI, MOBAflow, MOBAsmart, MOBApi or MOBAdisplay. The solution file, the architecture dependency table and CI gain the new project.

**Performance Goals**: First visit at most 1.5 MB compressed, of which scripts and style sheets at most 200 KB with Brotli; repeat visit under 100 KB; switching pages requests at most files not loaded yet; usable within 15 seconds after a cold start and 3 seconds warm (SC-006 to SC-008, SC-011).

**Data and API Effects**: No existing JSON format, configuration default, API or Z21 protocol changes. The example data is a demo-only format inside `MOBAweb/`, versioned with the app; it is not a MOBAflow file format and is never read by the apps.

**Scale/Scope**: Four pages, one set of example data (a flagship locomotive and one or two more, one timetable, one workflow, one layout, one signal box), at least 12 concurrent visitors, about 110 first visits per day within the Free tier.

## Constitution Check

*GATE: Every item MUST pass before Phase 0 research and be re-checked after Phase 1 design.*

- [x] Layer and platform boundaries are preserved. The demo host references no repository project and no WinUI or MAUI type. The new project gets its row in the documented dependency table that `SolutionArchitectureTests` enforces.
- [x] EventBus UI-thread marshalling remains centralized in `UiThreadEventBusDecorator`. Not touched; the demo does not use the EventBus.
- [x] Async flows use `await` without sync-over-async. Applies to the host.
- [x] UI behavior uses ViewModel commands; UI resources support Light and Dark themes. The web UI is not WinUI; the equivalent rule here: the little behavior there is (navigation, theme, slider, notices) lives in small state modules, components only render and forward input, and every color comes from a Light/Dark token (FR-021, FR-023).
- [x] All user-visible UI strings are English (FR-020); example data keeps its language where it mirrors real data (function names).
- [x] Data and API effects are explicit; no legacy path or migration.
- [x] Meaningful regression tests cover changed behavior (see Validation Strategy); this documentation-only change uses structural checks.
- [x] Validation commands follow the change matrix in `AGENTS.md`; the frontend adds its own npm commands, documented with the implementation.
- [x] The design reuses what exists: the Track Plan SVG export for the layout asset, the page registration as the source of the navigation model, the WinUI resources for the tokens.
- [x] The specification and plan reference the authoritative GitHub issue (#430).
- [x] Spec Kit artifacts remain below `specs/`.
- [x] Sensitive files: none are read. The implementation adds no secret to the repository; deployment credentials live in GitHub and Azure (FR-054). Changed files are scanned before commit in local sessions; cloud sessions record the missing local scan.
- [x] Sonar code analysis runs only in GitHub CI. Open point: whether the SonarCloud project analyses TypeScript under `MOBAweb/` is checked when the first frontend code arrives (T7).
- [x] The PR remains draft until SonarCloud is green with zero OPEN/CONFIRMED issues.

Re-check after design: the constitution's wording "UI behavior uses ViewModel commands" was written for WinUI and MAUI; it is met in spirit as described above and needs no exception.

## Project Structure

### Documentation (this feature)

```text
specs/020-web-demo/
|-- spec.md
|-- plan.md          # this file
`-- tasks.md         # not yet; the slice table in this plan is the cut the tasks follow
```

No `research.md`, `data-model.md`, `quickstart.md` or `contracts/`: the governance check does not require them, and the content they would hold is short enough for the sections below.

### Source Code (planned; nothing exists yet)

```text
MOBAweb/
|-- Host/                      # ASP.NET Core host (Moba.Web): static files, health, headers, rate limiting
|-- app/                       # React + TypeScript frontend (own package.json and lockfile)
|   |-- src/shell/             # navigation, command bar, Messages bar, status bar
|   |-- src/pages/             # train-control, workflows, track-plan, signal-box
|   |-- src/tokens/            # CSS custom properties mapped from MOBAflow's WinUI resources
|   |-- src/examples/          # example data (typed JSON) and the navigation model
|   `-- public/assets/         # example layout SVG, locomotive photos (WebP), icons
|-- build.(ps1|sh)             # builds the app, precompresses it and copies it into Host/wwwroot
`-- README.md                  # build, run, budgets, operator notes (quota view, deployment), how assets are produced
Test/MOBAweb/                  # NUnit: host tests and the navigation drift test (portable target)
.github/workflows/             # one new CI job and, later, one manually started deployment workflow
```

**Structure Decision**: Everything demo-specific lives in `MOBAweb/`; no shared project is changed for the demo. There is no generator project: example data and assets are committed files, and the one derived asset (the layout SVG) is produced outside the build (T2). `Moba.slnx`, the dependency table in `docs/ARCHITECTURE.md` and the repository map in `AGENTS.md` gain the host project in the implementation.

## Design

### Example data and assets

| File | Content | How it is made |
| --- | --- | --- |
| `src/examples/locomotives.json` | Flagship and further example locomotives: name, photo file, function list F0 to F31 (number, icon, name), prepared states (selected locomotive, direction, speed step, gauge values, pressed functions, cab-control tiles) | Written for the demo; function names may follow the maintainer's real locomotives |
| `src/examples/timetable.json` | Stops with arrival and departure times and the current stop | Written for the demo |
| `src/examples/workflow.json` | One workflow: trigger and action nodes with name, type, summary, step number, state and fixed graph positions; read-only property values | Written for the demo |
| `src/examples/signal-box.json` | Elements, turnouts with their position, one Ks signal with its available aspects, the prepared aspect and the multiplexer values shown | Written for the demo |
| `src/examples/navigation.json` | Entries, order, groups, icons, bold flags, Signal Box path icon, Preview badges; the four demo entries marked available | Transcribed from `MOBAflow/Service/NavigationRegistration.cs`, guarded by a drift test (T4) |
| `src/examples/texts.json` | Status bar segments, the first-visit Messages entry, hints | Written for the demo (spec FR-029, FR-031, FR-035) |
| `public/assets/track-plan.svg` | The example layout fitted to the view, without `style` attributes | Exported once from MOBAflow's Track Plan page (SVG export of `TrackPlan.Renderer`), cleaned and committed (T2) |
| `public/assets/photos/*.webp` | Locomotive photos at their displayed size | Resized and converted once from the maintainer's photographs, committed |

A unit test validates each example file against its TypeScript type and the rules the pages rely on (exactly one selected locomotive, 32 functions, a signal with its prepared aspect among its aspects, workflow nodes with positions), so that a broken edit fails the build instead of emptying a page.

### Interaction model

- **Active** (FR-005): navigation between the four demo pages, theme toggle (Dark on first visit, a manual choice remembered in `localStorage`), the speed slider (moves its thumb and its own "Step X" value text only), the Messages bar expander, the narrow-window notice.
- **Inert** (FR-035): every other control keeps its WinUI appearance in the prepared state, does nothing on activation, shows the hint "Available in the MOBAflow desktop app" on hover and focus, and is exposed to assistive technology as unavailable with that description. One shared inert-control wrapper implements this for all pages, so that the behavior is tested once.
- **View state**: current page, slider position, expander and notice state live in memory for the open tab only (FR-012).

### Frontend construction

- Shell and page views in plain CSS with custom properties (FR-077). `src/tokens/` holds one file per theme transcribed from `MOBAflow/App.xaml`, `MOBAflow/Resources/` and the Fluent 2 values WinUI uses; the accent token is the fixed magenta sampled from the reference screenshots (T5).
- Icons: only the glyphs the demo shows (about 30), as inline SVG; the Signal Box entry uses its path data from the page registration.
- Page views: the track plan is the committed SVG; the signal box, the workflow graph and the gauges are SVG drawn by the frontend from the example data.

### Host

- Serves the built frontend from `wwwroot` and a health response; nothing else (FR-070).
- Precompressed files (Brotli, gzip) produced by the build script, so that the host spends no CPU on compression.
- Cache headers: content-hashed files immutable for a year; the entry document revalidated.
- Security headers per FR-071; no server banner; neutral error page (FR-072); `robots.txt` (FR-073). The content security policy uses `style-src 'self'` without `'unsafe-inline'`: the frontend and the committed SVG carry no `style` attributes or inline style elements.
- Rate limiting per client address (FR-056) with forwarded headers handling for the App Service front end (FR-078), as rate-limiter policies partitioned by client address (`PartitionedRateLimiter`): a request-count limiter for requests that are not static files and a token-bucket limiter for static files whose tokens are bytes. A static request is charged with the size of the precompressed file it will send, known before the response starts, so the per-client cap of about 15 MB per hour holds even when one client downloads the largest file again and again (T8).
- The host project never invokes npm or Node: it serves a prepared `wwwroot` folder, and `dotnet build` of the host works on a machine without Node (SC-010). Producing that folder is the build script's job (slice 3).

### Budgets

| Budget | Limit | First estimate | Check |
| --- | --- | --- | --- |
| Scripts and style sheets, Brotli | 200 KB | React and React DOM about 45 KB; app and styles about 60 to 100 KB; optional primitives up to 30 KB | CI step on the build output (T6) |
| First visit, compressed | 1.5 MB | scripts and styles 200 KB; example data under 30 KB; 2 or 3 photos about 90 KB; icons inline; other pages' assets loaded when opened | CI browser test that sums the transferred bytes of a cold load |
| Repeat visit | 100 KB | entry document and revalidation only | same test, second load |

The estimates are assumptions to be measured with the first build; the limits are requirements.

## Open technical choices

Each has a recommendation; none blocks the first slices, and each is confirmed or changed in the slice that owns it.

| # | Choice | Recommendation | Alternatives and why not |
| --- | --- | --- | --- |
| T1 | Frontend toolchain | Vite with React and TypeScript, Vitest for unit tests, npm with a committed lockfile. The major versions of Node (the current LTS at scaffold time) and React are pinned in `package.json` (`engines`, exact dependency majors) and `.nvmrc` and in CI; a major upgrade is its own slice | A heavier framework adds server features the demo must not use and weight against the 200 KB ceiling |
| T2 | Producing the example layout SVG | Export it once from MOBAflow's Track Plan page ("Export SVG"), remove `style` attributes and host details, commit it; the README records the steps. A local session does this, because it needs the desktop app with the maintainer's launch approval | A one-off console command that calls `TrackPlan.Renderer` directly avoids launching the app but adds code to maintain; a generator at build time was excluded on 2026-11-17 |
| T3 | Component library | None for the shell; individual Fluent UI React v9 primitives (slider, switch, tooltip) only if their measured cost stays within the ceiling, otherwise small own components | Importing the component or icon packages as a whole breaks FR-076 and FR-077 |
| T4 | Navigation model | Transcribe `navigation.json` from `NavigationRegistration.cs`; a portable NUnit test parses the registration source for titles, order, categories, bold flags and badges and fails when `navigation.json` differs | Reading MOBAflow types needs the Windows build; a hand-written list without the test drifts from the app |
| T5 | Accent value | Sample the magenta from the maintainer's reference screenshots and record the hex value as the accent token, with the derived Light/Dark accent variants WinUI computes | Using Windows' own magenta palette value without the screenshots would be a guess |
| T6 | Budget and browser checks | Playwright in the `MOBAweb` CI job: cold and warm load byte sums, a smoke run of each page, the slider check of SC-005, the inert-control check of SC-004, and an automated accessibility check with axe-core for names, contrast and non-color cues | A documented manual measurement is allowed by FR-057 but does not stop a regression |
| T7 | Sonar scope | Keep SonarCloud as the single analysis; check on the first frontend pull request whether TypeScript under `MOBAweb/` is analysed and adjust the project configuration only through the existing CI | A second analyser is against the repository policy |
| T8 | Rate-limit values and unit | Per client address: 60 requests per minute that are not static files, and a static limit counted in transferred bytes, about ten first-visit equivalents (15 MB) per hour, as a `PartitionedRateLimiter` token bucket charged with the precompressed file size of each static response. If the built-in rate-limiting middleware can only take one permit per request, the static branch acquires its permits from the same limiter in a few lines of its own; the rate-limiting slice verifies this first | A static limit by request count does not hold the 15 MB: a count sized for many small files lets a client fetch the largest file that many times |
| T9 | Forwarded headers on App Service | Enable ASP.NET Core forwarded headers handling for the platform front end (the documented environment switch for App Service on Linux or explicit options), and verify on the real app that the limiter keys on the visitor's address | Without it every visitor shares the front end's address and one limit |
| T10 | Deployment | A manually started GitHub Actions workflow that builds, runs the checks and deploys with federated credentials (no stored publish profile); automatic deployment on merge later as its own change | A publish profile is a long-lived secret; FR-054 excludes secrets in the repository and prefers none at all |
| T11 | Test placement | Host and drift tests in the existing portable test project under `Test/MOBAweb/`; frontend tests inside `MOBAweb/app` | A separate .NET test project is cleaner but waits for RF-30's test split |

Not verified in this session and therefore marked for the owning slice: the forwarded-headers switch for App Service on Linux (T9), deployment with federated credentials (T10), and whether the rate-limiting middleware takes more than one permit per request (T8).

## Validation Strategy

- **Automated tests**: NUnit for the host (headers, cache policy, no route beyond files and health, rate limiting with a forwarded client address, the static byte limit under repeated downloads of the largest file, no MOBApi route served) and the navigation drift test. Vitest for the view logic (theme default and persistence, slider moves only itself, inert controls do nothing and carry their hint, navigation availability) and the example data rules. Playwright for the budgets, a smoke run per page, SC-004, SC-005 and the accessibility floor.
- **Builds**: `dotnet build` and `dotnet test` of the host on Linux; `npm ci`, `npm run build`, `npm test` in `MOBAweb/app`; the WinUI and Android builds must stay independent of Node (SC-010), shown by their unchanged lanes.
- **Manual checks**: the side-by-side comparison with the eight reference screenshots (FR-024, SC-002) by the maintainer or the Head of UI/UX; a first visit after an idle period on the real app (SC-008); the forwarded-headers check (T9). None is a merge gate by itself beyond what the repository rules say.
- **Regression checks**: the example data test and the navigation drift test; the architecture test covers the new project's references; the budget checks protect the Free tier.
- **Secrets scan**: no secret is added; local sessions scan changed files, cloud sessions record the missing local scan in the pull request.
- **Sonar gates**: SonarCloud green on the current commit with zero OPEN/CONFIRMED issues before Ready.
- **Issue traceability**: #430 for the feature; one pull request per slice of the table below; task issues, where generated, carry canonical `TNNN:` titles.

## Implementation slices

The implementation is cut for the small-batch flow (master coordination): each slice is one pull request with one concern, at most about 25 files or 400 changed lines (generated files such as the lockfile and committed binary assets excepted), mergeable on its own and finished within a day. Each slice names the spec requirements it delivers, writes its acceptance tests first and locks only the folders listed. The Chief Operating Officer decides the final cut, the risk class, the locks and the order on the board; this table is the proposal. Removed against the earlier cut of 2026-11-16: the snapshot generator, its drawing and navigation slices, the simulation core, the second Track Plan slice (moving locomotives), the second Workflows slice (graph run and Simulate) and all Simulate and Reset wiring.

**Feature flag `enabledPages`.**

- *What and where:* a compile-time constant list of page ids in `MOBAweb/app/src/config/enabledPages.ts`. It is not a runtime setting and not changeable by a visitor. The WinUI feature toggles do not apply, because the demo is a separate application.
- *Default:* empty. A demo page that is not in the list appears in the navigation as a disabled entry, with the mechanism FR-027 requires anyway, so `main` always builds a coherent demo.
- *When it changes:* each page slice adds its page id in the same pull request in which the page passes its acceptance tests (slices 9, 11, 12, 13).
- *When it is removed:* slice 17 deletes the list once all four pages are enabled; the non-demo entries stay disabled through the navigation model.
- *Public exposure* is separate from the flag: nothing is reachable from outside before the deployment slice and the maintainer's go.

| # | Slice | Delivers | Tests written first | Locked areas | Needs |
| --- | --- | --- | --- | --- | --- |
| 1 | Host skeleton | `MOBAweb/Host` (ASP.NET Core, serves the files of `wwwroot` and a health response), solution entry, dependency table row, repository map row. The project file contains no npm or Node step | Host tests: serves the entry document, serves no MOBApi runtime path, no server banner; the project builds without Node | `MOBAweb/Host`, `Test/MOBAweb`, `Moba.slnx`, the `MOBAweb` rows in `docs/ARCHITECTURE.md` and `AGENTS.md` | #47 complete |
| 2 | Frontend scaffold | `MOBAweb/app` with the toolchain (T1), lockfile, Light and Dark token files, `enabledPages`, the loading and failed-load states, the "needs JavaScript" notice, the import rule against whole component and icon packages | Unit tests: both themes define the same token keys; `enabledPages` defaults to empty; the loading state shows until the app is ready (FR-052) and a failed file shows a visible error (edge case); the built entry document contains the no-JavaScript notice (edge case); the lint rule fails on a whole-package import (FR-077) | `MOBAweb/app` (new) | 1 |
| 3 | Build script | One documented command that builds the app, precompresses the output and copies it into the host's `wwwroot`; the same command is what CI and the deployment call | Script test on Linux: after the command the host serves the built app; a search of the produced files finds no host path, port, machine name or internal address (SC-003, FR-030) | `MOBAweb/build.*`, `Test/MOBAweb` | 2 |
| 4 | CI job | The `MOBAweb` job: .NET build and tests, the command of slice 3, unit tests, dependency audit, the 200 KB check (FR-053, FR-074, FR-076) | The job itself; a deliberately oversized bundle fails in a throwaway branch | `.github/workflows`, `scripts/Get-QualityChangeScope.ps1` | 3; local session (workflow files) |
| 5 | Navigation model | `navigation.json` transcribed from the page registration, with the drift test (T4) | Drift test: titles, order, categories, bold flags and badges equal the registration; a change of the registration fails the test (FR-022) | `MOBAweb/app/src/examples/navigation.json`, `Test/MOBAweb` | 2 |
| 6 | Example data | `locomotives.json`, `timetable.json`, `workflow.json`, `signal-box.json`, `texts.json` with their types, and the photos as WebP (FR-010, FR-058) | Unit tests of the example data rules; photo size limits; texts contain no host detail (FR-030) | `MOBAweb/app/src/examples`, `MOBAweb/app/public/assets/photos` | 2 |
| 7 | Shell frame and inert controls | Navigation from the model with disabled entries and their explanation, theme toggle with Dark default and remembered choice, layout widths and the narrow-window notice, and the shared inert-control wrapper (FR-021 to FR-027, FR-033, FR-035) | Unit tests: navigation order, bold entries, disabled state with accessible description and no action; theme default and persistence; contrast of disabled entries from the tokens; an inert control changes nothing, shows its hint and is exposed as unavailable (FR-035, FR-041) | `MOBAweb/app/src/shell/navigation`, `MOBAweb/app/src/shell/inert`, `MOBAweb/app/src/tokens` | 5 |
| 8 | Shell bars | Command bar (New, Open, Simulate with its number box, Reset; all inert), Messages bar with expander, filters and the first-visit entry, status bar with the four demo segments (FR-028 to FR-031) | Unit tests: shell texts contain no host detail (FR-030); first-visit entry visible immediately; a failed file load adds a Warning entry (edge case); command-bar controls change nothing (SC-004) | `MOBAweb/app/src/shell/bars` | 6, 7 |
| 9 | Train Control: layout and slider | Page frame, locomotive list, functions list, direction, speed steps, ramp toggle, EMERGENCY STOP, the speed slider that moves only itself; landing page (FR-032); page enabled | Component tests: scenario 1 and 2 of User Story 1 (prepared state, slider moves only itself, SC-005); scenario 3 (inert controls); slider range and "Step X" value text (FR-042); accessible names and prepared pressed states (FR-041, FR-044) | `MOBAweb/app/src/pages/train-control` | 8 |
| 10 | Train Control: gauges, tiles, timetable | Speedometer with digital readout, current gauge, cab-control tiles with the Door blocking indicator, timetable and stops list | Component tests: gauges expose their prepared value as text (FR-043); tiles convey state by icon or text (FR-044, FR-045); no animation under reduced motion (FR-046) | `MOBAweb/app/src/pages/train-control` | 9 |
| 11 | Signal Box | The prepared view of FR-039; page enabled | Tests from scenario 2 and 4 of User Story 2; aspect code as text and the light arrangement (FR-045); a flashing aspect is steady with "Flashing" under reduced motion (FR-046); all controls inert (SC-004) | `MOBAweb/app/src/pages/signal-box` | 8 |
| 12 | Track Plan | The prepared view of FR-038 on the committed layout SVG (the asset itself comes from slice 14); page enabled with a placeholder layout until then | Tests from scenario 1 and 4 of User Story 2; all controls inert (SC-004); the SVG has no `style` attributes (CSP) | `MOBAweb/app/src/pages/track-plan` | 8 |
| 13 | Workflows | The prepared view of FR-037 with the graph from `workflow.json`; page enabled | Tests from scenario 3 and 4 of User Story 2; node state as text or icon (FR-045); all controls inert (SC-004) | `MOBAweb/app/src/pages/workflows` | 8 |
| 14 | Example layout asset | `track-plan.svg` exported from MOBAflow and cleaned (T2); the README records how | Asset test: no `style` attributes, no host details, file size within the budget | `MOBAweb/app/public/assets/track-plan.svg`, `MOBAweb/README.md` | 12; local session with the maintainer's launch approval |
| 15 | Host hardening and rate limiting | Precompressed serving, cache headers, security headers with `style-src 'self'`, neutral error page, `robots.txt`; request limiter and byte-charged static limiter, forwarded headers (FR-056, FR-058, FR-059, FR-070 to FR-073, FR-078, T8, T9) | Host tests per header and route; per-client limits with a forwarded address; repeated downloads of the largest file hit the byte limit; other clients are not limited | `MOBAweb/Host`, `Test/MOBAweb` | 1 |
| 16 | Browser checks | Playwright with axe-core: first-visit and repeat-visit byte sums, a smoke run per page, SC-004 and SC-005, the accessibility floor, the cold-load loading state, the no-JavaScript notice, and a search of every response for host details (FR-057, SC-003, SC-006, SC-009, FR-052) | The checks themselves | `MOBAweb/app/e2e`, `.github/workflows` | 9; local session (workflow files) |
| 17 | Deployment, flag removal and fidelity | Manually started deployment workflow and operator README (FR-054, FR-055, FR-075); removal of `enabledPages`; corrections from the side-by-side comparison with the reference screenshots (FR-024, SC-002), as several small slices if needed | A deployment to the prepared web app, verified by the smoke run against the public address; the forwarded-headers check on the real app (T9) and the cold-start time (SC-008) recorded; navigation tests no longer depend on the flag | `.github/workflows`, `MOBAweb/README.md`, `src/config`, the page or shell folder concerned | 11 to 16; local session; Technical Director's Azure setup; maintainer's go; the reference screenshots |

Slice 15 may be split into hardening and rate limiting if it grows past the size limit.

**Requirements without an automated test, deferred on purpose:** SC-001 (a first-time visitor opens all four pages and switches the theme within 60 seconds) and SC-002 (side-by-side comparison) are human checks in slice 17; SC-008 (cold start within 15 seconds) and the per-visitor behavior of the rate limit behind the real front end (T9) can only be measured on the deployed app in slice 17; SC-011 (twelve concurrent visitors) follows from the design (static files only) and is checked in slice 17 by twelve parallel smoke runs, not earlier.

**Parallel work without overlapping locks** (within the limit of three open code slices): 2 and 15 after 1; 3, 5 and 6 after 2; 7 after 5; 8 after 6 and 7; 9, 11, 12 and 13 in any order after 8, each in its own folder (10 after 9).

**First slice once #47 is complete:** slice 1. It touches no existing code area except the solution file and two table rows, needs no Node and no workflow change, and can be written and proved in a cloud session on Linux CI.

**What a cloud session cannot do:** slices 4, 16 and 17 change workflow files, and slice 14 needs the desktop app; they need a local session. Everything else is portable.

## Risks

- **Bandwidth quota**: a few heavy visitors or a bot can end the day for everyone; mitigations are the budgets, caching and rate limiting; the residual risk is accepted (spec FR-075).
- **Fidelity without the app**: agents cannot start MOBAflow without approval; the reference screenshots and the XAML sources are the main sources. Deviations are found in the fidelity review, not earlier.
- **A showcase that looks interactive**: inert controls in their active appearance can surprise visitors. The hint on hover and focus and the first-visit Messages entry explain it; the Head of UI/UX reviews the wording.
- **Drift from the app**: the example data and tokens are copies. The navigation drift test covers the navigation; tokens and page layouts follow the app only through the fidelity review.
- **Workflow files from the cloud**: a cloud session cannot push workflow changes; the CI job and the deployment workflow need a local session.
- **New toolchain**: Node, npm and a browser test runner are new to the repository's CI; they stay inside the `MOBAweb` job and its dependency audit (FR-074).

## Complexity Tracking

No Constitution Check item needs an exception.
