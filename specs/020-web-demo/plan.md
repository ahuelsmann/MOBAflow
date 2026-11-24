# Implementation Plan: MOBAflow web demo

**Branch**: `020-web-demo` | **Date**: 2026-11-14, revised 2026-11-16, 2026-11-17 and 2026-11-18 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #430

**Spec Kit**: Required

**Status**: Draft. The product folder is `MOBAdemo/` (renamed from `MOBAweb/` in #508). Slices 1, 2 and 18a are delivered (#505, #506, #507): the host skeleton, the React scaffold and the host hardening. No workflow, no Azure change and no `tasks.md` exist yet.

**Input**: Feature specification from `/specs/020-web-demo/spec.md`, narrowed on 2026-11-17 to a visual showcase.

**Decision record**: The maintainer decisions behind the specification are linked in its Clarifications section. Two shape this plan most: the demo is operated by mouse with a basic accessibility floor ([comment of 2026-11-14](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6883730125)), and it is a visual showcase without simulation, workflow execution, solution import or snapshot generator ([comment of 2026-11-17](https://github.com/ahuelsmann/MOBAflow/issues/430#issuecomment-6950953706)).

**Note**: Written by hand from the plan template override; `$speckit-plan` was not run. Research results are folded into [Open technical choices](#open-technical-choices) instead of a separate `research.md`.

## Summary

A public web demo of four MOBAflow pages (Train Control, Workflows, Track Plan, Signal Box) that looks like the WinUI app and needs no hardware. It is a visual showcase: navigation between the four pages, the theme toggle and the dismissible narrow-window notice are interactive, the Train Control speed slider moves only itself, the Messages bar is shown expanded with an inert expander, and everything else is a prepared, inert view built from static example data made for the demo (spec FR-003, FR-005, FR-010, FR-031, FR-035).

Technical approach in one line: a React app with a plain-CSS shell and tokens mapped from MOBAflow's WinUI resources renders committed example data and assets; a small ASP.NET Core host only serves the built files, with compression, cache headers, security headers and rate limiting. The host runs on the Azure App Service Free tier, whose daily 165 MB of outgoing data is the binding limit, so the plan keeps a first visit within 1.5 MB with at most 200 KB of scripts and style sheets (FR-057, FR-076).

## Technical Context

**Language/Version**: Host: .NET 10 / C# (repository defaults, `Microsoft.NET.Sdk.Web` like MOBApi). Frontend: TypeScript with React; Node version pinned in `MOBAdemo/` (choice T1).

**Primary Dependencies**: New: React, a build tool and a test runner for the frontend (T1), optionally individual Fluent UI React v9 primitives (T3). The host references no repository project: no `Domain`, no `Backend`, no `MOBApi`, no SignalR. The example layout asset is rendered once from the shipped sample `MOBAflow/track-plan.json` with the existing `TrackPlan.Renderer` by a portable NUnit test, outside the demo's build and without launching the app (T2).

**Storage/Protocols**: None on the server. Static files over HTTPS. The browser stores only a manual theme choice (FR-012).

**Testing**: NUnit for the host (portable target, Linux CI). A frontend unit test runner for the view logic and the example data (T1). A browser test for smoke, budget and accessibility-floor checks (T6).

**Target Platforms**: ASP.NET Core on .NET 10 under Linux (Azure App Service Free tier F1); current desktop browsers.

**Affected Layers**: A new folder `MOBAdemo/`. No change of behavior in Domain, Common, Backend, SharedUI, MOBAflow, MOBAsmart, MOBApi or MOBAdisplay. The solution file, the architecture dependency table and CI gain the new project.

**Performance Goals**: First visit at most 1.5 MB compressed, of which scripts and style sheets at most 200 KB with Brotli; repeat visit under 100 KB; switching pages requests at most files not loaded yet; usable within 15 seconds after a cold start and 3 seconds warm (SC-006 to SC-008, SC-011).

**Data and API Effects**: No existing JSON format, configuration default, API or Z21 protocol changes. The example data is a demo-only format inside `MOBAdemo/`, versioned with the app; it is not a MOBAflow file format and is never read by the apps.

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
- [x] The design reuses what exists: `TrackPlan.Renderer` and the shipped sample track plan for the layout asset, the page registration and the feature toggle registry as the sources of the navigation model, the WinUI resources for the tokens.
- [x] The specification and plan reference the authoritative GitHub issue (#430).
- [x] Spec Kit artifacts remain below `specs/`.
- [x] Sensitive files: none are read. The implementation adds no secret to the repository; deployment credentials live in GitHub and Azure (FR-054). Changed files are scanned before commit in local sessions; cloud sessions record the missing local scan.
- [x] Sonar code analysis runs only in GitHub CI. Open point: whether the SonarCloud project analyses TypeScript under `MOBAdemo/` is checked when the first frontend code arrives (T7).
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

### Source Code (planned; slices 1, 2 and 18a exist)

```text
MOBAdemo/
|-- Host/                      # ASP.NET Core host (Moba.Demo): static files, health, headers, rate limiting
|-- app/                       # React + TypeScript frontend (own package.json and lockfile)
|   |-- src/shell/             # navigation, command bar, Messages bar, status bar
|   |-- src/pages/             # train-control, workflows, track-plan, signal-box
|   |-- src/tokens/            # CSS custom properties mapped from MOBAflow's WinUI resources
|   |-- src/examples/          # example data (typed JSON) and the navigation model
|   `-- public/assets/         # example layout SVG, locomotive photos (WebP), icons
|-- build.(ps1|sh)             # builds the app, precompresses it and copies it into Host/wwwroot
`-- README.md                  # build, run, budgets, operator notes (quota view, deployment), how assets are produced
Test/MOBAdemo/                 # NUnit (portable target): Host/, Build/, Navigation/, TrackPlanAsset/
.github/workflows/             # one new CI job and, later, one manually started deployment workflow
```

**Structure Decision**: Everything demo-specific lives in `MOBAdemo/`; no shared project is changed for the demo. There is no generator project: example data and assets are committed files, and the one derived asset (the layout SVG) is rendered by a test outside the build (T2). `Moba.slnx`, the dependency table in `docs/developer/ARCHITECTURE.md` and the repository map in `AGENTS.md` gain the host project in the implementation.

## Design

### Example data and assets

| File | Content | How it is made |
| --- | --- | --- |
| `src/examples/locomotives.json` | Flagship and further example locomotives: name, photo file, function list F0 to F31 (number, icon, name), prepared states (selected locomotive, direction, speed step, gauge values, pressed functions, cab-control tiles) | Written for the demo; function names may follow the maintainer's real locomotives |
| `src/examples/timetable.json` | Stops with arrival and departure times and the current stop | Written for the demo |
| `src/examples/workflow.json` | One workflow: trigger and action nodes with name, type, summary, step number, state and fixed graph positions; read-only property values | Written for the demo |
| `src/examples/signal-box.json` | Elements, turnouts with their position, one Ks signal with its available aspects, the prepared aspect and the multiplexer values shown | Written for the demo |
| `src/examples/navigation.json` | Entries, order, groups, icons, bold flags, Signal Box path icon, Preview badges; the four demo entries marked available | Entries from `MOBAflow/Service/NavigationRegistration.cs`; badges from `SharedUI/Configuration/FeatureToggleRegistry.cs` defaults as overridden by the labels in the shipped `MOBAflow/appsettings.json`; guarded by a drift test (T4) |
| `src/examples/texts.json` | Status bar segments, the first-visit Messages entry, hints | Written for the demo (spec FR-029, FR-031, FR-035) |
| `public/assets/track-plan.svg` | The example layout fitted to the view, without `style` attributes | Rendered from the shipped sample `MOBAflow/track-plan.json` with `TrackPlan.Renderer` by a portable NUnit test, cleaned and committed (T2) |
| `src/assets/photos/*` | Locomotive images, imported as modules so served names are content-hashed | Self-drawn placeholder SVGs (`placeholder-diesel.svg`, `placeholder-electric.svg`) until the maintainer provides own photographs, which are then resized and converted to WebP; catalogue images are not used (decision of 2026-11-23 on #430) |

A unit test validates each example file against its TypeScript type and the rules the pages rely on (exactly one selected locomotive, 32 functions, a signal with its prepared aspect among its aspects, workflow nodes with positions), so that a broken edit fails the build instead of emptying a page.

### Interaction model

- **Active** (FR-005): navigation between the four demo pages, theme toggle (Dark on first visit, a manual choice remembered in `localStorage`), the speed slider (moves its thumb and its own "Step X" value text only), dismissing the narrow-window notice.
- **Shown expanded, inert** (FR-031): the Messages bar is rendered expanded with the first-visit entry; its expander, filters and Refresh button are inert like every other control.
- **Inert** (FR-035): every other control keeps its WinUI appearance in the prepared state, does nothing on activation, shows the hint "Available in the MOBAflow desktop app" on hover and focus, and is exposed to assistive technology as unavailable with that description. One shared inert-control wrapper implements this for all pages, so that the behavior is tested once.
- **View state**: current page, slider position and notice state live in memory for the open tab only (FR-012).

### Frontend construction

- Shell and page views in plain CSS with custom properties (FR-077). `src/tokens/` holds one file per theme transcribed from `MOBAflow/App.xaml`, `MOBAflow/Resources/` and the Fluent 2 values WinUI uses; the accent token starts as a named placeholder value in slice 2 and receives the magenta sampled from the reference screenshots in slice 20 (T5).
- Icons: only the glyphs the demo shows (about 30), as inline SVG; the Signal Box entry uses its path data from the page registration.
- Page views: the track plan is the committed SVG; the signal box, the workflow graph and the gauges are SVG drawn by the frontend from the example data.

### Host

- Serves the built frontend from `wwwroot` and a health response; nothing else (FR-070).
- Precompressed files (Brotli, gzip) produced by the build script, so that the host spends no CPU on compression.
- Cache headers: content-hashed files immutable for a year; the entry document revalidated.
- Security headers per FR-071; no server banner; neutral error page (FR-072); `robots.txt` (FR-073). The content security policy uses `style-src 'self'` without `'unsafe-inline'`: the frontend and the committed SVG carry no `style` attributes or inline style elements. Fluent UI React v9 styles its components with Griffel, which injects style elements at run time; under this policy a Fluent primitive is therefore only usable if its styles can be extracted to a static style sheet at build time. Plain CSS is the default (T3).
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
| T2 | Producing the example layout SVG | A portable NUnit test in `Test/MOBAdemo/TrackPlanAsset` loads the shipped sample `MOBAflow/track-plan.json`, renders it with `PlacedTrackPlanSvgRenderer`, moves `style` attributes into classes and fits the view box. In its normal mode it asserts that the committed `track-plan.svg` equals the rendering (a drift check); an explicit switch rewrites the file. No app launch, so a cloud session can do it. Fallback if the renderer output cannot be made free of inline styles: a hand-drawn SVG of the same layout, with the test reduced to the asset rules | Exporting from the running app needs a launch approval and a local session; a generator at build time was excluded on 2026-11-17 |
| T3 | Component library | Plain CSS and small own components (slider, switch, tooltip) by default. A Fluent UI React v9 primitive only if its measured cost stays within the ceiling and its Griffel styles are extracted to a static style sheet at build time, so that `style-src 'self'` holds; the slice that adds one documents the CSP check | Importing the component or icon packages as a whole breaks FR-076 and FR-077; Griffel's run-time style injection would need `'unsafe-inline'` |
| T4 | Navigation model | Transcribe `navigation.json` from `NavigationRegistration.cs` (titles, order, categories, icons, bold flags, path icon, feature toggle key) and the badges from `FeatureToggleRegistry` (portable, called directly) with the labels of the shipped `MOBAflow/appsettings.json`; a portable NUnit test parses the registration source, reads both badge sources and fails when `navigation.json` differs | Reading MOBAflow types needs the Windows build; a hand-written list without the test drifts from the app |
| T5 | Accent value | Slice 2 defines the accent tokens with a named placeholder value; slice 20 samples the magenta from the maintainer's reference screenshots and sets the hex value with the derived Light/Dark accent variants WinUI computes | Using Windows' own magenta palette value without the screenshots would be a guess |
| T6 | Budget and browser checks | Playwright in the `MOBAdemo` CI job, which runs every spec folder under `MOBAdemo/app/e2e/`: shell checks and byte sums (slice 10), one folder per page with smoke, SC-004 and axe-core in both themes (slices 13, 15, 16, 17), cross-page checks including SC-007 (slice 19) | A documented manual measurement is allowed by FR-057 but does not stop a regression |
| T7 | Sonar scope | Keep SonarCloud as the single analysis; check on the first frontend pull request whether TypeScript under `MOBAdemo/` is analysed and adjust the project configuration only through the existing CI | A second analyser is against the repository policy |
| T8 | Rate-limit values and unit | Per client address: 60 requests per minute that are not static files, and a static limit counted in transferred bytes, about ten first-visit equivalents (15 MB) per hour, as a `PartitionedRateLimiter` token bucket charged with the precompressed file size of each static response. If the built-in rate-limiting middleware can only take one permit per request, the static branch acquires its permits from the same limiter in a few lines of its own; the rate-limiting slice verifies this first | A static limit by request count does not hold the 15 MB: a count sized for many small files lets a client fetch the largest file that many times |
| T9 | Forwarded headers on App Service | Enable ASP.NET Core forwarded headers handling for the platform front end (the documented environment switch for App Service on Linux or explicit options), and verify on the real app that the limiter keys on the visitor's address | Without it every visitor shares the front end's address and one limit |
| T10 | Deployment | A manually started GitHub Actions workflow that builds, runs the checks and deploys with federated credentials (no stored publish profile); automatic deployment on merge later as its own change | A publish profile is a long-lived secret; FR-054 excludes secrets in the repository and prefers none at all |
| T11 | Test placement | .NET tests in the existing portable test project under `Test/MOBAdemo/`, one subfolder per slice (`Host`, `Build`, `Navigation`, `TrackPlanAsset`); frontend tests inside `MOBAdemo/app` | A separate .NET test project is cleaner but waits for RF-30's test split |

Not verified in this session and therefore marked for the owning slice: the forwarded-headers switch for App Service on Linux (T9), deployment with federated credentials (T10), and whether the rate-limiting middleware takes more than one permit per request (T8).

## Validation Strategy

- **Automated tests**: NUnit for the host (headers, cache policy, no route beyond files and health, rate limiting with a forwarded client address, the static byte limit under repeated downloads of the largest file, no MOBApi route served), the navigation drift test and the layout asset test. Vitest for the view logic (theme default, persistence and page and slider kept across a switch, layout widths and the narrow-window notice, slider moves only itself, inert controls do nothing and carry their hint, the expanded Messages bar, navigation availability) and the example data rules. Playwright for the budgets, a smoke run per page, SC-004, SC-005, SC-007 and the accessibility floor. The [requirement coverage](#requirement-coverage) table maps every requirement to its slice and test.
- **Builds**: `dotnet build` and `dotnet test` of the host on Linux; `npm ci`, `npm run build`, `npm test` in `MOBAdemo/app`; the WinUI and Android builds must stay independent of Node (SC-010), shown by their unchanged lanes.
- **Manual checks**: the side-by-side comparison with the eight reference screenshots (FR-024, SC-002) by the maintainer or the Head of UI/UX; a first visit after an idle period on the real app (SC-008); the forwarded-headers check (T9). None is a merge gate by itself beyond what the repository rules say.
- **Regression checks**: the example data test and the navigation drift test; the architecture test covers the new project's references; the budget checks protect the Free tier.
- **Secrets scan**: no secret is added; local sessions scan changed files, cloud sessions record the missing local scan in the pull request.
- **Sonar gates**: SonarCloud green on the current commit with zero OPEN/CONFIRMED issues before Ready.
- **Issue traceability**: #430 for the feature; one pull request per slice of the table below; task issues, where generated, carry canonical `TNNN:` titles.

## Implementation slices

The implementation is cut for the small-batch flow (master coordination): each slice is one pull request with one concern, at most about 25 files or 400 changed lines (generated files such as the lockfile and committed binary assets excepted), mergeable on its own and finished within a day. Each slice names the spec requirements it delivers, writes its acceptance tests first and locks only the paths listed. The Chief Operating Officer decides the final cut, the risk class, the locks and the order on the board; this table is the proposal. Removed against the earlier cut of 2026-11-16: the snapshot generator, its drawing and navigation slices, the simulation core, the second Track Plan slice (moving locomotives), the second Workflows slice (graph run and Simulate) and all Simulate and Reset wiring. Changed on 2026-11-18: the shell and Train Control are split into smaller slices, a page is enabled only in its last slice, each page brings its own browser checks, and the layout asset is rendered by a test instead of exported from the running app.

**Feature flag `enabledPages`.**

- *What and where:* a compile-time constant list in `MOBAdemo/app/src/config/enabledPages.ts`, composed from one constant per page in `MOBAdemo/app/src/pages/<page>/enabled.ts`. Slice 2 creates all five files with every page off. A page slice changes only its own `enabled.ts`, so parallel page slices share no file. The flag is not a runtime setting and not changeable by a visitor; the WinUI feature toggles do not apply, because the demo is a separate application.
- *Default:* every page off. A demo page that is off appears in the navigation as a disabled entry, with the mechanism FR-027 requires anyway, so `main` always builds a coherent demo.
- *When it changes:* a page is switched on in the last slice of that page, in the same pull request in which its browser checks pass (slices 13, 15, 16, 17). No page is enabled while a part of it is missing.
- *When it is removed:* slice 19 deletes the list and the per-page constants once all four pages are enabled; the non-demo entries stay disabled through the navigation model.
- *Public exposure* is separate from the flag: nothing is reachable from outside before the deployment slice and the maintainer's go.

| # | Slice | Delivers | Tests written first | Locked paths | Needs |
| --- | --- | --- | --- | --- | --- |
| 1 | Host skeleton | `MOBAdemo/Host` (serves the files of `wwwroot` and a health response), solution entry, dependency table row, repository map row; no npm or Node step in the project file | Host tests: serves the entry document; serves no MOBApi runtime, solution or hub path (FR-002, SC-003); no server banner (FR-072); architecture test: the host references no repository project (FR-001); the project builds without Node (FR-053, SC-010) | `MOBAdemo/Host`, `Test/MOBAdemo/Host`, `Moba.slnx`, the `MOBAdemo` rows in `docs/developer/ARCHITECTURE.md` and `AGENTS.md`, `quality/analyzer-baseline*.json` | #47 complete |
| 2 | Frontend scaffold | `MOBAdemo/app` with the toolchain (T1) and lockfile; Light and Dark token files with the accent placeholder (T5); `enabledPages` and the five `enabled.ts` files; loading and failed-load states; the "needs JavaScript" notice; the import rule against whole component and icon packages | Unit tests: both themes define the same token keys, including the accent tokens (FR-023); every page is off; the loading state shows until the app is ready (FR-052); a failed file shows a visible error (edge case); the built entry document contains the no-JavaScript notice (edge case); the lint rule fails on a whole-package import (FR-077) | `MOBAdemo/app` (new) | 1 |
| 3 | Build script | One documented command that builds the app, precompresses the output (FR-058) with content-hashed names (FR-059) and copies it into the host's `wwwroot`; CI and the deployment call the same command | Script test on Linux: after the command the host serves the built app; Brotli and gzip variants exist; a search of the produced files finds no host path, port, machine name or internal address (SC-003, FR-030) | `MOBAdemo/build.*`, `Test/MOBAdemo/Build`, `quality/analyzer-baseline*.json` | 2 |
| 4 | CI job | The `MOBAdemo` job: .NET build and tests, the command of slice 3, unit tests, dependency audit, the 200 KB check; the job needs no WinUI or Android workload (FR-053, FR-074, FR-076, SC-010) | The job itself; a deliberately oversized bundle and a known vulnerable package fail in a throwaway branch | `.github/workflows`, `scripts/Get-QualityChangeScope.ps1` | 3; local session (workflow files) |
| 5 | Navigation model | `navigation.json` with the drift test (T4) | Drift test: titles, order, categories, icons, bold flags, path icon and badges equal the registration, the feature toggle registry and the shipped labels; a change of any of the three fails the test (FR-022) | `MOBAdemo/app/src/examples/navigation.json`, `Test/MOBAdemo/Navigation`, `quality/analyzer-baseline*.json` | 2 |
| 6 | Example data | `locomotives.json`, `timetable.json`, `workflow.json`, `signal-box.json`, `texts.json` with their types, and the locomotive images (placeholder SVGs until own photographs exist; FR-010, FR-058) | Unit tests of the example data rules (exactly one selected locomotive, 32 functions, the prepared aspect among the signal's aspects, workflow nodes with positions); photo size limits; texts contain no host detail (FR-030) | `MOBAdemo/app/src/examples` except `navigation.json`, `MOBAdemo/app/src/assets/photos` | 2 |
| 7 | Shell navigation and theme | Navigation pane from the model with disabled entries and their explanation and the selection indicator; switching between enabled pages; theme toggle with Dark default and remembered choice (FR-012, FR-021, FR-022, FR-027) | Unit tests: order, groups, bold entries and badges from the model; a disabled entry has its accessible description, does nothing and shows its hint (FR-027, FR-041); contrast of disabled entries of at least 3:1 from the tokens; first visit shows Dark regardless of the browser preference; a manual choice is stored and is the only stored value; a reload starts on the landing page with the stored theme; switching the theme keeps the current page (FR-021, User Story 3 scenario 3) | `MOBAdemo/app/src/shell/navigation`, `MOBAdemo/app/src/shell/theme`, `MOBAdemo/app/src/tokens` | 5 |
| 8 | Shell layout and inert controls | Window-width modes and the narrow-window notice (FR-033); the shared inert-control wrapper (FR-035) | Unit and component tests (FR-033): from 1280 px the full layout with all panes; from 1024 to 1279 px the compact navigation rail and folded side panes; below 1024 px a minimum width of 1024 px and the notice with its exact text; dismissing the notice hides it for the tab and a reload shows it again (FR-012). Inert wrapper: activation changes nothing, the hint shows on hover and focus, the control is exposed as unavailable with the hint as description (FR-035, FR-041) | `MOBAdemo/app/src/shell/layout`, `MOBAdemo/app/src/shell/inert` | 7 |
| 9 | Shell bars | Command bar (New, Open, Simulate with its number box, Reset; all inert), Messages bar shown expanded with the first-visit entry and inert expander, filters and Refresh, status bar with the four demo segments (FR-005, FR-028 to FR-031) | Unit tests: the Messages bar is expanded on load with the first-visit entry visible (FR-031); activating the expander, a filter or Refresh changes nothing and shows the hint (FR-005, FR-035); a failed file load adds a Warning entry (edge case); the status bar segments carry the exact demo texts and no host detail (FR-029, FR-030); every command-bar and status-bar control changes nothing (SC-004). May be split into command and status bar / Messages bar if it grows past the size limit | `MOBAdemo/app/src/shell/bars` | 6, 8 |
| 10 | Browser check foundation | Playwright and axe-core in the `MOBAdemo` job, running every folder under `MOBAdemo/app/e2e/`; the shell checks | Browser tests: a cold load shows the loading state and then the shell (FR-052); with JavaScript off the notice is shown (edge case); first-visit and repeat-visit byte sums within the budgets (FR-057, FR-076, SC-006); a search of every response for host details (SC-003, FR-030); the three width modes at 1280, 1100 and 900 px (FR-033); axe-core on the shell in both themes (SC-009) | `MOBAdemo/app/e2e/shell`, `MOBAdemo/app/playwright.config.*`, `MOBAdemo/app/package.json`, `.github/workflows` | 4, 9; local session (workflow files) |
| 11 | Train Control: frame and lists | Page frame and title, series search box, EMERGENCY STOP, locomotive cards, functions list F0 to F31, direction toggle, DCC speed steps, ramp toggle, all inert in the prepared state (FR-036) | Component tests: User Story 1 scenario 1 for these parts (selected flagship locomotive, 32 functions with names); scenario 3 (every control inert with hint); accessible names (FR-041); prepared pressed states of the function buttons (FR-044); EMERGENCY STOP is an inert button (FR-043) | `MOBAdemo/app/src/pages/train-control/frame` | 9 |
| 12 | Train Control: slider | The speed slider, the only active control of the page (FR-036, FR-042) | Component tests: User Story 1 scenario 2 and SC-005 (dragging changes only the thumb and the "Step X" text; gauges, readout, functions and cards identical); range 0 to 126 and value text (FR-042); with shell and page rendered, a theme switch keeps the page and the slider position (FR-021, User Story 3 scenario 3) | `MOBAdemo/app/src/pages/train-control/slider` | 11 |
| 13 | Train Control: gauges, tiles, timetable; page on | Speedometer with digital readout, current gauge, cab-control tiles with the Door blocking indicator, timetable and stops list; Train Control as landing page (FR-032); Train Control switched on | Component tests: gauges expose their prepared value as text (FR-043); tiles convey state by icon or text and expose pressed states (FR-044, FR-045); no animation under reduced motion (FR-046); the first visit lands on Train Control with the flagship locomotive (FR-032). Browser tests in `e2e/train-control`: smoke, SC-004 for every control of the page, SC-005, axe-core in both themes (SC-009) | `MOBAdemo/app/src/pages/train-control/gauges`, `MOBAdemo/app/src/pages/train-control/enabled.ts`, `MOBAdemo/app/e2e/train-control` | 10, 12 |
| 14 | Example layout asset | `track-plan.svg` rendered from the shipped sample `MOBAflow/track-plan.json` by a portable NUnit test (T2); the README records how to regenerate it | Asset test: the committed file equals the rendering of the sample; no `style` attributes; no host details; file size within the budget (FR-040, FR-071) | `MOBAdemo/app/public/assets/track-plan.svg`, `Test/MOBAdemo/TrackPlanAsset`, `MOBAdemo/README.md`, `quality/analyzer-baseline*.json` | 2 |
| 15 | Track Plan; page on | The prepared view of FR-038 on the layout asset; Track Plan switched on | Component tests: User Story 2 scenarios 1 and 4; every control inert (FR-035). Browser tests in `e2e/track-plan`: smoke, SC-004, axe-core in both themes | `MOBAdemo/app/src/pages/track-plan`, `MOBAdemo/app/e2e/track-plan` | 9, 10, 14 |
| 16 | Signal Box; page on | The prepared view of FR-039; Signal Box switched on | Component tests: User Story 2 scenarios 2 and 4; aspect code as text and the light arrangement (FR-045); a flashing aspect is steady with "Flashing" under reduced motion (FR-046). Browser tests in `e2e/signal-box`: smoke, SC-004, axe-core in both themes | `MOBAdemo/app/src/pages/signal-box`, `MOBAdemo/app/e2e/signal-box` | 9, 10 |
| 17 | Workflows; page on | The prepared view of FR-037 with the graph from `workflow.json`; Workflows switched on | Component tests: User Story 2 scenarios 3 and 4; node state as text or icon (FR-045). Browser tests in `e2e/workflows`: smoke, SC-004, axe-core in both themes | `MOBAdemo/app/src/pages/workflows`, `MOBAdemo/app/e2e/workflows` | 9, 10 |
| 18 | Host hardening and rate limiting | Precompressed serving, cache headers, security headers with `style-src 'self'`, neutral error page, `robots.txt`; request limiter and byte-charged static limiter, forwarded headers (FR-051, FR-056, FR-059, FR-070 to FR-073, FR-078, T8, T9) | Host tests per header and route; no cookie and no endpoint accepting visitor data (FR-007, FR-070); no WebSocket upgrade (FR-051); per-client limits with a forwarded address; repeated downloads of the largest file hit the byte limit; another client is not limited (edge case) | `MOBAdemo/Host`, `Test/MOBAdemo/Host`, `quality/analyzer-baseline*.json` | 1. May be split into hardening and rate limiting |
| 19 | Cross-page checks and flag removal | Removal of `enabledPages` and the per-page constants; the browser checks that need all four pages | Browser tests in `e2e/cross-page`: SC-007 with a network assertion (after the first load, open the four pages in turn twice while recording requests: the first round requests only static files not loaded before and nothing outside the static files, no file twice; the second round requests nothing); on each page a theme switch keeps the page and the slider position (FR-021); navigation through all four entries (User Story 3 scenario 2); axe-core on four pages in both themes (SC-009); the first-visit byte sum with all pages on (SC-006) | `MOBAdemo/app/src/config`, `MOBAdemo/app/src/pages/*/enabled.ts`, `MOBAdemo/app/e2e/cross-page` | 13, 15, 16, 17 |
| 20 | Deployment and accent | Manually started deployment workflow and operator README (FR-050, FR-054, FR-055, FR-075); the sampled accent value replaces the placeholder (T5, FR-023) | A deployment to the prepared web app, verified by the browser smoke run against the public address; a reload of an open page after a redeployment shows the new version (edge case); recorded on the real app: the forwarded-headers check (T9, FR-078), the cold-start time (SC-008) and twelve parallel smoke runs (SC-011, FR-049) | `.github/workflows`, `MOBAdemo/README.md`, `MOBAdemo/app/src/tokens` | 18, 19; local session; Technical Director's Azure setup; maintainer's go; the reference screenshots |
| 21 | Fidelity corrections | Corrections from the side-by-side comparison with the reference screenshots (FR-024, FR-025, SC-002), as several small slices if needed | Component tests for each corrected item where it is a value (token, text, order); the comparison itself is a human check | the page or shell folder concerned | 20; the reference screenshots |

**Order and parallel work** (within the limits of three open code slices and two local builds; the COO schedules):

1. Slice 1 first. Then 2 and 18 in parallel.
2. After 2: 3, 5, 6 and 14 in parallel. Slices 1, 3, 5, 14 and 18 each add test fixtures to the shared analyzer baselines (`quality/analyzer-baseline*.json`), so the COO locks those files for one of them at a time.
3. After 3: 4 (local session). After 5: 7, then 8. After 6 and 8: 9. After 4 and 9: 10 (local session).
4. After 9: 11, then 12. After 10 and 12: 13.
5. After 10: 15 (also after 14), 16 and 17 in any order, each in its own folders.
6. After 13, 15, 16 and 17: 19. After 18 and 19: 20 (local session). After 20: 21.

No two slices that may run at the same time lock the same path: `Test/MOBAdemo` is split per slice, the page flag is one file per page, `e2e` is one folder per slice, and the Train Control slices are sequential.

## Requirement coverage

Every requirement, success criterion and edge case of the specification is delivered and tested in the slice named here, or listed below as deferred on purpose.

| Requirement | Slices (test) |
| --- | --- |
| FR-001 | 1 (architecture test); 19 (SC-007 network assertion: no request outside the static files) |
| FR-002 | 1, 18 (host tests) |
| FR-003 | 11 to 17 (prepared state; SC-004 and SC-005 checks show nothing changes) |
| FR-005 | 7 (navigation, theme), 8 (notice), 9 (inert expander), 12 (slider); SC-004 checks in 9, 13, 15, 16, 17 |
| FR-006 | 8 (inert wrapper), 18 (no endpoint accepts data); SC-004 checks |
| FR-007 | 7 (only the theme is stored), 18 (no cookie, no data endpoint) |
| FR-010 | 6, 14 |
| FR-012 | 7 (stored value, reload), 8 (notice per tab) |
| FR-020 | deferred (review) |
| FR-021 | 7 (default, persistence, page kept), 12 (slider kept), 19 (browser, every page) |
| FR-022 | 5 (drift test), 7 |
| FR-023 | 2 (token keys), 20 (accent value), 21 |
| FR-024, FR-025 | 21 (human check, deferred) |
| FR-026 | deferred (review) |
| FR-027 | 7 |
| FR-028 | 9 |
| FR-029 | 9 |
| FR-030 | 3, 6, 9, 10 |
| FR-031 | 9 |
| FR-032 | 13 |
| FR-033 | 8 (width modes, notice), 10 (browser at three widths) |
| FR-035 | 8 (wrapper), 9, 11, 13, 15, 16, 17 |
| FR-036 | 11, 12, 13 |
| FR-037 | 17 |
| FR-038 | 15 |
| FR-039 | 16 |
| FR-040 | 14 |
| FR-041 | 7, 8, 11; axe-core in 10, 13, 15, 16, 17, 19 |
| FR-042 | 12 |
| FR-043 | 11, 13 |
| FR-044 | 11, 13 |
| FR-045 | 13, 16, 17 |
| FR-046 | 13, 16 |
| FR-047 | nothing to build (exclusion) |
| FR-049 | 20 (twelve parallel smoke runs) |
| FR-050 | 20 |
| FR-051 | 18 (static only, no WebSocket); 10, 19 (budgets) |
| FR-052 | 2, 10 |
| FR-053 | 1, 4 |
| FR-054, FR-055 | 20 |
| FR-056 | 18 |
| FR-057 | 10, 19 |
| FR-058 | 3, 6 |
| FR-059 | 3, 18 |
| FR-070 to FR-073 | 18 |
| FR-074 | 4 |
| FR-075 | 20 (README) |
| FR-076 | 4, 10 |
| FR-077 | 2 (lint rule); T3 |
| FR-078 | 18; 20 (real front end) |
| SC-001 | 21 (human check, deferred) |
| SC-002 | 21 (human check, deferred) |
| SC-003 | 1, 3, 10 |
| SC-004 | 9, 13, 15, 16, 17 |
| SC-005 | 12, 13 |
| SC-006 | 10, 19 |
| SC-007 | 19 |
| SC-008 | 20 (measured, deferred) |
| SC-009 | 10, 13, 15, 16, 17, 19 |
| SC-010 | 1, 4 |
| SC-011 | 20 (measured, deferred) |
| Edge case: quota used up | accepted (FR-075); 20 documents it |
| Edge case: cold start | 2, 10 |
| Edge case: a file fails to load | 2, 9 |
| Edge case: narrow window | 8, 10 |
| Edge case: reduced motion | 13, 16 |
| Edge case: no JavaScript | 2, 10 |
| Edge case: MOBApi runtime path | 1, 18 |
| Edge case: many requests from one source | 18 |
| Edge case: deployment while visitors are active | 20 |

**Deferred on purpose, without an automated test before slice 20:** SC-001 (a first-time visitor opens all four pages and switches the theme within 60 seconds) and SC-002 with FR-024 and FR-025 (side-by-side comparison) are human checks in slice 21; SC-008 (cold start within 15 seconds), SC-011 with FR-049 (twelve concurrent visitors) and the per-visitor behavior of the rate limit behind the real front end (T9, FR-078) can only be measured on the deployed app in slice 20; FR-020 (English UI) and FR-026 (no reference screenshot committed) are review checks of every slice; FR-047 is an exclusion; quota exhaustion cannot be provoked and is accepted (FR-075).

**First slice once #47 is complete:** slice 1. It touches no existing code area except the solution file, two table rows and the analyzer baselines, needs no Node and no workflow change, and can be written and proved in a cloud session on Linux CI.

**What a cloud session cannot do:** slices 4, 10 and 20 change workflow files and need a local session; slice 20 also needs the Azure setup and slice 21 the reference screenshots. Everything else, including the layout asset (slice 14), is portable.

## Risks

- **Bandwidth quota**: a few heavy visitors or a bot can end the day for everyone; mitigations are the budgets, caching and rate limiting; the residual risk is accepted (spec FR-075).
- **Fidelity without the app**: agents cannot start MOBAflow without approval; the reference screenshots and the XAML sources are the main sources. Deviations are found in the fidelity review, not earlier.
- **A showcase that looks interactive**: inert controls in their active appearance can surprise visitors. The hint on hover and focus and the first-visit Messages entry explain it; the Head of UI/UX reviews the wording.
- **Drift from the app**: the example data and tokens are copies. The navigation drift test covers the navigation and the asset test the layout; tokens and page layouts follow the app only through the fidelity review.
- **Workflow files from the cloud**: a cloud session cannot push workflow changes; the CI job, the browser check foundation and the deployment workflow need a local session.
- **New toolchain**: Node, npm and a browser test runner are new to the repository's CI; they stay inside the `MOBAdemo` job and its dependency audit (FR-074).

## Complexity Tracking

No Constitution Check item needs an exception.
