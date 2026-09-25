# MOBAflow Quality and Refactoring Programme Plan

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/47
**Spec Kit**: Not applicable - programme coordination plan; plan-required child issues own their issue-specific planning artifacts

## Purpose

This is the single technical umbrella plan for the MOBAflow quality and
refactoring programme RF-01 through RF-30.

GitHub issue #47 owns programme status, priority, milestones, child tracking,
acceptance criteria, and completion evidence. This plan owns the stable
technical execution model: work-package outcomes, dependencies, shared risks,
rollback principles, validation expectations, and the minimum contract for
issue-specific child plans.

The programme uses dependency-ordered, independently reviewable work packages.
It does not authorize a big-bang rewrite or the combination of unrelated
mechanical and behavioral changes.

## Historical source

The programme originated from the repository-wide quality audit performed on
2026-07-20 against `main` at
`a4b9b77ca42901d01e9b1cd01cbcd56be32b1bc2`. That baseline explains the
programme's origin but is not a current status snapshot. Current status and
acceptance evidence live only in GitHub issue #47 and its child issues.

## Ownership and scope boundaries

### GitHub issue #47 owns

- programme status, priority, milestones, and assignees;
- child-issue tracking and programme-level acceptance criteria;
- stakeholder decisions and completion evidence;
- the authoritative answer when issue state and plan text differ.

### This umbrella plan owns

- stable technical outcomes for RF-01 through RF-30;
- hard and recommended dependencies between packages;
- downstream consumers and cross-package sequencing;
- programme-wide risks, stop conditions, and rollback principles;
- shared quality, security, validation, manual, and hardware gates;
- the minimum content required in every plan-required child plan.

### Child issues and child plans own

- committed package scope and package-specific acceptance criteria;
- affected projects, files, interfaces, and delivery slices;
- architecture decisions and alternatives;
- compatibility, migration, telemetry, and package-specific rollback;
- exact validation commands, expected results, and acceptance evidence.

Until a package has a linked child issue and plan, the provisional technical
anchors in this umbrella plan remain authoritative. Creating that child must
reconcile and transfer the anchors, then replace the package's provisional
section here with the child-plan link in the same planning change. This keeps
one current owner for the detail without requiring reconstruction from Git
history.

### Explicitly out of scope

- Product behavior owned by feature issues #30 through #36.
- Silent expansion of a quality package into feature delivery.
- Duplicate implementation plans for the same issue.
- Status checklists, merge histories, or live progress metrics duplicated from
  GitHub.

RF packages may create or improve boundaries consumed by feature work, but they
must not implement that feature behavior. In particular, RF-13 and RF-14 expose
testable boundaries consumed by issue #34; their child plans must not absorb
issue #34 product scope.

## Operating scope decision

MOBAflow targets a private household on a trusted home network. On 2026-09-25
the maintainer withdrew RF-03/#50: MOBAflow, MOBAsmart and MOBApi intentionally
provide no authentication, authorization, user management, pairing or transport
certificate pinning. Installations that need these capabilities fork the
repository. RF-19 removes the already merged control-plane code.

Robustness at the API boundary is not a security feature and remains in scope:
MOBApi validates command values (addresses, speeds, function indices,
identifiers, enums and payload sizes) before they enter the runtime, and the
remote command queue stays bounded. ESP32 provisioning protection from RF-02/#48
is unaffected by this decision.

## Architecture principles for RF-19 through RF-30

The architecture review of 2026-09-25 added RF-19 through RF-30. They share
these target rules, which keep the solution object-oriented and understandable
for human developers:

- Each class has one reason to change. Partial files organise code; they do
  not count as a separation of responsibilities.
- Page ViewModels depend on small, named services, never on
  `MainWindowViewModel`. The shell ViewModel owns only navigation and shell state.
- Required dependencies are required constructor parameters. Hosts register
  null objects explicitly; classes do not create fallbacks with `?? new`.
- Commands reach the runtime through exactly one port.
- Domain classes own the rules that need only domain data.
- A project's name, root namespace and responsibility match. `SharedUI` holds
  only code that both UI hosts use.
- Solution-wide architecture tests enforce the project and namespace rules.

## Execution model

The five programme milestones are dependency groups, not strictly serialized
phases. A package may proceed when:

1. its hard dependencies are complete;
2. its child issue exists and declares its Spec Kit workflow;
3. a plan-required child has exactly one linked issue-specific plan;
4. its dedicated branch and worktree are ready;
5. no programme stop condition applies.

Independent packages may run in parallel. Recommended dependencies inform
sequencing but do not block work unless the child issue promotes them to hard
dependencies.

## Work-package map

| Package | Tracking issue | Stable technical outcome |
| --- | --- | --- |
| RF-01 | [#49](https://github.com/ahuelsmann/MOBAflow/issues/49) | Every ESP32 UDP packet is classified and parsed with explicit bounds. |
| RF-02 | [#48](https://github.com/ahuelsmann/MOBAflow/issues/48) | Provisioning is explicitly activated, authenticated, time-limited, and fail-closed. |
| RF-03 | [#50](https://github.com/ahuelsmann/MOBAflow/issues/50) | Withdrawn by the operating scope decision; superseded by RF-19. |
| RF-04 | [#43](https://github.com/ahuelsmann/MOBAflow/issues/43) | Z21 events are processed in deterministic FIFO order with defined overload and shutdown behavior. |
| RF-05 | [#51](https://github.com/ahuelsmann/MOBAflow/issues/51) | A clean Android Release restore produces a validated AAB locally and in mandatory CI. |
| RF-06 | [#90](https://github.com/ahuelsmann/MOBAflow/issues/90) | Release and CI run an explicit analyzer baseline and reject new unapproved diagnostics. |
| RF-07 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Every supported delivery target has a reproducible mandatory clean-build lane. |
| RF-08 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Project-owned MAUI XAML bindings are compiled and protected against binding regressions. |
| RF-09 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Risk-critical behavior has non-decreasing coverage and mutation protection. |
| RF-10 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Formatting drift fails CI without obscuring functional changes. |
| RF-11 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Train-control responsibilities are delegated to focused, platform-neutral, testable collaborators. |
| RF-12 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Mobile session, discovery, runtime projection, synchronization, and upload concerns are separated. |
| RF-13 | [#91](https://github.com/ahuelsmann/MOBAflow/issues/91) | Track-plan editor operations are platform-neutral and WinUI remains a thin input and rendering adapter. |
| RF-14 | [#92](https://github.com/ahuelsmann/MOBAflow/issues/92) | Signal-box property changes flow through commands and ViewModels rather than direct control mutation. |
| RF-15 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Unused synchronous and unsafe path abstractions are removed or hardened. |
| RF-16 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Critical workflows pass accessibility, keyboard, contrast, and theme acceptance. |
| RF-17 | [#116](https://github.com/ahuelsmann/MOBAflow/issues/116) | Repository guidance and executable engineering gates describe the same rules. |
| RF-18 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Critical throughput, load, telemetry, recovery, and endurance behavior is measured. |
| RF-19 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | MOBApi and its clients contain no authentication, pairing or credential code; command validation and queue bounds remain. |
| RF-20 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Architecture tests enforce the project dependency and namespace rules of the whole solution. |
| RF-21 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Unused and misplaced types are removed or moved to the project that owns them. |
| RF-22 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | ViewModels send runtime commands through one port without compatibility facades or local fallbacks. |
| RF-23 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | A dedicated solution session owns the loaded solution, selection, dirty state and auto-save. |
| RF-24 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | `MainWindowViewModel` is a shell; each page area has its own focused ViewModel. |
| RF-25 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Constructors declare real dependencies; no hidden optional services, fallbacks or mutable static hooks. |
| RF-26 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | `Common` is split by responsibility: shared contracts, host-owned UI settings and presentation helpers. |
| RF-27 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Domain classes own the rules and state transitions that need only domain data. |
| RF-28 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | The track plan has one model and editor boundary; the WinUI page is a thin adapter. |
| RF-29 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Platform-specific sound and file services live in platform adapters with one role per class. |
| RF-30 | [#47](https://github.com/ahuelsmann/MOBAflow/issues/47) until child creation | Repository, documentation and test layout let a developer find product code, docs and tests directly. |

The phrase "until child creation" is traceability, not workflow status. Issue #47
remains authoritative for whether a child package is proposed, active, blocked,
or complete.

## Provisional technical anchors for packages without child plans

These anchors preserve stable technical sequence and acceptance intent; they do
not authorize implementation. The execution-model gates still apply. A future
child issue may refine an anchor when it records the decision and rationale,
updates issue #47, and remains inside the package and feature-scope boundaries.

### RF-07: Complete the CI platform matrix

Sequence:

1. inventory every supported deliverable and document its clean local command;
2. make cross-platform .NET, Windows/WinUI, Android/MAUI Release, and ESP32
   PlatformIO builds repeatable;
3. add resolved transitive dependency auditing and the applicable test and
   coverage gates;
4. add mutation and formatting lanes only after RF-09 and RF-10 establish their
   baselines.

Acceptance anchor: every supported deliverable is reproduced from a clean
checkout with the documented local command and an equivalent mandatory CI lane;
required artifacts and tests are retained as workflow evidence.

### RF-08: Compile MAUI XAML bindings

Sequence:

1. inventory project-owned binding-context boundaries and current warnings;
2. add correct `x:DataType` declarations and enable source-binding compilation
   incrementally;
3. fix resulting binding errors instead of suppressing `XC0025` globally;
4. promote the clean project-owned warning baseline into CI.

Acceptance anchor: Android Release has no `XC0025` warnings in project-owned
XAML; locomotive selection, signal aspects, function toggles, and control-page
state retain regression coverage; measured startup and interaction performance
does not regress.

### RF-09: Coverage and mutation ratchets

Sequence:

1. protect Z21 ordering and overload behavior;
2. protect MOBApi command validation and remote command queue bounds;
3. establish the Backend and API mutation lanes;
4. extend measured lanes to Common and SharedUI;
5. extend further only where the tooling is technically suitable.

Acceptance anchor: existing coverage thresholds do not decrease without an
approved rationale; security and concurrency paths include negative and
failure-path assertions; every activated mutation lane records a baseline and
adopts a non-decreasing ratchet.

### RF-10: Formatting baseline and gate

Sequence:

1. wait for the RF-06 analyzer baseline to stabilize;
2. define generated, vendored, and firmware-library exclusions;
3. normalize formatting, line endings, and final newlines in a dedicated
   mechanical change;
4. add `dotnet format --verify-no-changes` to mandatory CI.

Acceptance anchor: the baseline change contains no functional or architecture
work, `.editorconfig` and documented commands agree, and the mandatory
formatting gate rejects subsequent drift.

### RF-11: Decompose `TrainControlViewModel`

After characterization tests, extract in this order:

1. locomotive selection and fleet projection;
2. speed conversion, ramping, and command debounce;
3. function state and appearance;
4. brake and door state machines;
5. journey and station projection;
6. telemetry aggregation and peaks;
7. host-specific settings persistence.

Acceptance anchor: `TrainControlViewModel` coordinates focused,
platform-neutral collaborators; state transitions are independently testable
with controlled time and cancellation; WinUI and MAUI differences remain
explicit; each superseded path is removed after equivalence is proven.

### RF-12: Decompose `MauiViewModel`

After characterization tests, separate:

1. discovery and endpoint selection;
2. SignalR session and reconnect lifecycle;
3. remote snapshot projection;
4. solution synchronization;
5. photo capture and upload orchestration;
6. application and network lifecycle handling;
7. move the remaining mobile root ViewModel from `SharedUI` into `MOBAsmart`,
   because no other host uses it.

Acceptance anchor: the root mobile ViewModel is a composition boundary rather
than the owner of networking, storage, runtime projection, and UI state
transitions; reconnect, cancellation, failure, and lifecycle behavior is
independently tested; superseded paths are removed.

### RF-15: Remove or harden latent legacy services

Sequence:

1. reconcile current references and issue #36 ownership before changing display
   sender code; consume its cutover rather than repeating feature-owned work;
2. remove an unreferenced synchronous sender only after all required behavior
   uses `IFrameSender.SendFrameAsync`;
3. remove an unused photo-storage abstraction or route every remaining path
   through `PhotoPathHelper` with canonical-root containment;
4. add traversal cases for parent segments, rooted paths, alternate separators,
   and encoded edge cases before retaining any path abstraction;
5. remove duplicate utilities only after reference and behavior checks.

Acceptance anchor: no required caller depends on the removed synchronous path;
remaining path operations cannot escape their configured root; relevant
behavior and failure tests pass; no product behavior from issue #36 is absorbed.

### RF-16: Accessibility and themes

Sequence:

1. inventory critical workflows plus icon-only and custom-drawn controls;
2. add accessible names, help text, roles, patterns, and automation peers where
   native semantics are insufficient;
3. complete keyboard operation, focus order, visible focus, Narrator, and text
   scaling behavior;
4. replace general-purpose literal colors with theme resources while retaining
   fixed railway signal colors only where semantically required;
5. run Accessibility Insights and Light, Dark, and High Contrast acceptance.

Acceptance anchor: the critical workflow inventory has retained evidence for
keyboard, Narrator, text scaling, focus, contrast, and all required themes, with
no unresolved critical accessibility failure.

### RF-17: Repository instruction cleanup

Sequence:

1. inventory repository guidance against actual project, build, test, and CI
   behavior;
2. replace stale framework, synchronous-wait, and workflow guidance with the
   enforced NUnit, async, and GitHub Actions model;
3. add an executable consistency check where deterministic enforcement is
   practical and assign review ownership for the remainder.

Acceptance anchor: contributor and agent instructions agree with executable
commands and mandatory CI; contradictory guidance is removed; the consistency
check or explicit review owner detects future drift.

### RF-18: Performance and operational verification

Sequence:

1. benchmark EventBus throughput and runtime-snapshot serialization;
2. load-test SignalR behavior and the bounded remote command queue;
3. measure MAUI startup and binding costs;
4. run firmware endurance scenarios with packet loss and Wi-Fi reconnection;
5. verify structured telemetry for dropped events, rejected commands, queue
   overload, recovery, and failed shutdown.

Acceptance anchor: each critical scenario records its environment, baseline,
limit, agreed regression threshold, and normal and failure results; required
hardware evidence covers recovery and endurance; the work consumes RF-04,
RF-07, and RF-19 guarantees without reopening their implementation scope.

### RF-19: Remove the MOBApi control-plane security

Sequence:

1. characterize the command validation and queue bounds that remain, so their
   behavior survives the removal;
2. remove the anonymous-read migration (`CompatibilityRead*`) and the GitHub
   issue evidence verifier;
3. remove pairing, QR codes, credentials, access tokens, protected document
   storage, host enrollment and host bootstrap verification from MOBApi,
   `Common/Security`, MOBAflow and MOBAsmart;
4. replace HTTPS server identity and certificate pinning with plain HTTP on the
   local network, and simplify discovery and client registration;
5. remove the pairing and security UI, settings, tests, analyzer-baseline
   entries, `plans/50-authenticated-control-plane.md` and
   `docs/MOBAPI-SECURITY-DESIGN.md`; state the operating scope in `README.md`,
   `SECURITY.md`, `docs/ARCHITECTURE.md`, `docs/PROJECT-REFERENCE.md` and the
   user guides.

Acceptance anchor: no authentication, authorization, pairing, credential or
certificate code remains; MOBAflow and MOBAsmart connect to MOBApi without
setup steps; invalid command values are rejected before the runtime and queue
overflow stays bounded; Windows and Android builds and affected tests pass.

### RF-20: Solution-wide architecture guards

Sequence:

1. record the intended project dependency direction and the namespace-to-project
   rule in `docs/ARCHITECTURE.md`;
2. extend the existing `TrackLayoutArchitectureTests` approach to all projects;
3. start with rules the current code satisfies, and add every further rule in
   the package that makes it pass.

Acceptance anchor: a forbidden project reference or namespace leak fails the
test suite with a message that names the rule.

### RF-21: Remove dead and misplaced code

Sequence:

1. remove the unused hardware placeholder types in `Domain/ESP`,
   `Domain/Matrix` and `Domain/Display`;
2. move the `Moba.TrackPlan.Renderer` types declared in `TrackLibrary.PikoA`
   into a namespace of their project;
3. move `Common/Speech/PiperPronunciationNormalizer` into `Sound`;
4. remove `.azure-pipelines/`, because GitHub is the only CI host.

Acceptance anchor: each removed type had no reference; moved types keep their
behavior and tests; builds of all affected targets pass.

### RF-22: One runtime command port

Sequence:

1. decide between `IRuntimeCommandGateway` and the unused role interfaces of
   `IMobaRuntime`, and name each role after what it really does;
2. route every ViewModel command through the chosen port and register its
   local, recording and remote implementations only in the hosts;
3. remove the unused interfaces, the backward-compatible aggregate comment and
   the `new LocalRuntimeCommandGateway(...)` fallbacks in ViewModels.

Acceptance anchor: one documented command path exists; ViewModels never create
a gateway; recording and remote routing keep their tests.

### RF-23: Extract the solution session

Sequence:

1. characterize loading, saving, auto-save, dirty state, project selection and
   journey selection;
2. move this state into a solution session service that implements
   `IProjectContext` and `IJourneySelectionContext`;
3. inject that service instead of `MainWindowViewModel` into page ViewModels,
   WinUI pages and host services;
4. make the registered `Solution` accessible only through the session.

Acceptance anchor: no ViewModel, page or service except the shell depends on
`MainWindowViewModel`; save, auto-save and selection behavior is unchanged.

### RF-24: Decompose `MainWindowViewModel`

After RF-23, extract in this order:

1. settings page (`MainWindowViewModel.Settings.cs`);
2. rolling stock: locomotives, wagons and trains;
3. stations and journeys;
4. workflows;
5. counter, diagnostics, health and synchronization status;
6. layout panel persistence as a service instead of per-page code.

Acceptance anchor: `MainWindowViewModel` keeps only shell responsibilities; each
extracted ViewModel has its own tests and is registered through DI; the removed
partial files are deleted rather than moved.

### RF-25: Explicit dependencies

Proceed project by project, starting with `Backend` and ending with the hosts:

1. make optional constructor dependencies required and register the needed
   null objects in the host;
2. remove `?? new` fallbacks and convenience constructors such as the second
   `MobaRuntimeService` and `JourneyManagerFactory` constructors;
3. replace mutable static hooks such as `LanIpv4AddressHelper.AugmentAddresses`
   and `SegmentPlanPathBuilder.ScaleMmToPx` with injected services;
4. let the existing DI container validators cover every changed registration.

Acceptance anchor: constructors show every real dependency; tests use explicit
fakes; the DI validators for WinUI and MAUI pass.

### RF-26: Split `Common` by responsibility

Sequence:

1. create a contracts project for runtime snapshots, remote runtime commands,
   hub method names and the discovery protocol shared by MOBApi, MOBAflow and
   MOBAsmart;
2. move WinUI page layout settings to MOBAflow and replace the per-page feature
   toggle properties with one keyed collection;
3. move display appearance helpers from `Common/Display` to `SharedUI` or the
   host that uses them;
4. leave only neutral infrastructure (events, paths, logging, configuration
   loading) in `Common`, and add the resulting rules to RF-20.

Acceptance anchor: MOBApi references only what it uses; `Common` contains no
UI layout or presentation types; stored settings load with default values for
removed fields.

### RF-27: Move domain rules into the domain model

Sequence:

1. inventory rules in Backend services and ViewModels that need only domain
   data, for example journey stop transitions, workflow structure validation
   and address conflicts;
2. move each rule into the owning domain class behind a characterization test;
3. keep persistence, time and hardware effects out of `Domain`.

Acceptance anchor: moved rules are tested at the domain level; services
delegate to domain methods instead of duplicating the rule; the JSON format
stays unchanged.

### RF-28: Consolidate the track-plan model

Sequence:

1. document the current track-plan types in `Domain`, `TrackLibrary.Base`,
   `TrackPlan.Renderer`, `TrackLibrary.PikoA`, `Backend/Service/TrackPlan` and
   `SharedUI`;
2. move the editable plan and interaction logic out of the Piko A catalogue
   into a catalogue-neutral editor boundary;
3. move editor-only services (selection, undo and redo) out of `Backend`;
4. reduce `TrackPlanPage.xaml.cs` to input and rendering adaptation.

Acceptance anchor: catalogue projects contain only catalogue geometry; the
existing track-plan architecture tests and the RF-13 editor behavior pass.

### RF-29: Platform adapters for sound and files

Sequence:

1. move Windows speech and sound playback out of the `Sound` project used by
   Android into the Windows host or a Windows adapter project;
2. split `IoService` into one class per role: solution files, file picking and
   photo storage.

Acceptance anchor: the Android build contains no Windows-only speech packages;
each file service class implements one role; affected tests pass.

### RF-30: Repository and test layout

Sequence:

1. separate the reference skill snapshot and other agent tooling from product
   documentation, and move Office files out of `docs/`;
2. group `docs/` into developer and user documentation;
3. split `Test/Test.csproj` by target: portable core and API tests, Windows
   desktop tests and integration tests; move `Test/Analysis` out of the test
   project;
4. align root namespaces with project names (`MOBAflow`, `MOBAsmart`,
   `MOBAdisplay`) and folder names (`Converter`/`Converters`, `Interface`).

Acceptance anchor: a new developer finds the architecture, build and test
entry points from `README.md`; each test project builds for its own targets;
CI runs every test project.

## Dependency graph

| Package | Hard prerequisites | Recommended prerequisites | Downstream consumers |
| --- | --- | --- | --- |
| RF-01 | None | None | RF-02, RF-07 |
| RF-02 | None | RF-01 | Display and provisioning consumers |
| RF-03 | Withdrawn | None | Superseded by RF-19 |
| RF-04 | None | None | RF-09, RF-11, RF-18 |
| RF-05 | None | None | RF-06, RF-07, RF-08, RF-12 |
| RF-06 | None | RF-05 | RF-07, RF-09, RF-10, RF-13, RF-14, RF-15 |
| RF-07 | RF-01, RF-05, RF-06 | None | RF-18 |
| RF-08 | RF-05 | None | MAUI release quality |
| RF-09 | RF-04, RF-06 | RF-19 | Validation and concurrency regression protection |
| RF-10 | RF-06 | None | Repository-wide formatting enforcement |
| RF-11 | RF-04 | None | Train-control maintainability |
| RF-12 | RF-05 | RF-19, RF-22 | Mobile maintainability |
| RF-13 | RF-06 | None | Issue #34 Slice 6, RF-16 |
| RF-14 | RF-06 | None | Issue #34 Slice 7, RF-16 |
| RF-15 | RF-06 | None | Legacy-service and path-safety cleanup |
| RF-16 | RF-13, RF-14 | None | Product-quality acceptance |
| RF-17 | None | None | Agent and contributor consistency |
| RF-18 | RF-04, RF-07 | RF-19 | Operational release confidence |
| RF-19 | None | None | RF-09, RF-12, RF-18, RF-26 |
| RF-20 | None | None | RF-21, RF-26, RF-28, RF-30 |
| RF-21 | None | RF-20 | Architecture guard rules |
| RF-22 | None | None | RF-12, RF-23, RF-24 |
| RF-23 | None | RF-22 | RF-24 |
| RF-24 | RF-23 | None | RF-25, RF-27 |
| RF-25 | None | RF-22, RF-23 | Explicit DI in all projects |
| RF-26 | RF-19 | RF-20 | RF-30 |
| RF-27 | None | RF-24 | Object-oriented domain model |
| RF-28 | None | RF-20 | Track-plan maintainability |
| RF-29 | None | None | Android package size and file-service clarity |
| RF-30 | None | RF-20, RF-26 | Contributor orientation |

The explicit issue #34 unblock path is:

`RF-06/#90 -> RF-13/#91 and RF-14/#92 -> issue #34 Slices 6-8`

RF-13 and RF-14 remain separate plan-required packages and may run in parallel
after RF-06 satisfies their hard gate.

## Milestone outcomes

### Milestone 1: Eliminate immediate boundary risks

RF-01, RF-02, RF-04 and RF-05 close parser bounds, provisioning,
event-ordering, and Android Release risks; RF-03 is withdrawn. Each package must leave a permanent
test, CI, security, or release guard.

### Milestone 2: Make quality requirements enforceable

RF-06 through RF-10 turn analyzers, platform builds, compiled bindings,
coverage, mutation, and formatting into repeatable non-regression gates.
Baselines must be measured before thresholds are enforced, and no gate may be
lowered merely to obtain a green result.

### Milestone 3: Reduce architectural concentration

RF-11 through RF-15 begin with characterization tests and end with deletion of
the superseded path. Moving methods between partial files does not complete an
extraction. Platform-neutral behavior belongs in focused collaborators, while
WinUI and MAUI remain explicit adapters.

### Milestone 4: Complete product-quality work

RF-16 through RF-18 establish accessibility, theme, guidance, performance,
telemetry, and endurance evidence under realistic operating and failure
conditions.

### Milestone 5: Clarify responsibilities and structure

RF-19 through RF-30 remove the withdrawn control plane and apply the
architecture principles above. Recommended order: RF-19 and RF-20 first, then
RF-21 and RF-22, then RF-23 before RF-24, with RF-25 through RF-30 following
their recommended prerequisites. Every extraction starts with
characterization tests and ends with deletion of the superseded path.

## Programme risk register

| Risk and trigger | Mitigation | Stop or escalation condition |
| --- | --- | --- |
| Plan and issue drift: the plan states workflow status or conflicts with an issue. | GitHub remains authoritative; keep status out of this plan and reconcile technical text against the issue. | Stop dependent planning until the contradiction is resolved. |
| Dependency bypass: implementation begins before a hard prerequisite or plan gate. | Validate the dependency graph, issue, plan, branch, and worktree before implementation. | Stop the package and return it to planning. |
| Feature-scope leakage: an RF change starts implementing behavior from #30 through #36. | Keep feature behavior in its owning issue and expose only the required technical boundary. | Split or move the behavior before review. |
| Unreviewable or irreversible slice: a change crosses responsibilities or lacks characterization tests. | Deliver one responsibility per independently buildable slice and retain a tested rollback point. | Split the change before implementation or publication. |
| Required platform or hardware lane is unavailable. | Record the exact environment limitation and keep the acceptance gate visible. | The package remains open or blocked; unavailable is never reported as passed. |
| Removing the control plane also removes command validation or queue bounds. | Characterize validation and queue behavior before RF-19 removes security code. | Stop the removal slice until invalid commands are rejected again. |
| A documented quality gate is not enforced in the real merge path. | Verify committed CI configuration and remote results, not generated setup alone. | Do not complete the package until the mandatory gate is demonstrably active. |

Review this register at every milestone transition and whenever a new security,
hardware-safety, compatibility, or release finding appears.

## Rollback principles

- Every delivery slice must be independently reviewable and technically
  reversible to its last green, tested boundary.
- Superseded behavior is removed only after equivalent characterization and
  regression tests pass.
- Architecture and quality changes may return to the last green path when their
  child plan's rollback criteria are met.
- Provisioning and hardware-control rollback must remain fail-safe. A rollback
  may disable or restrict a new capability, but it must never restore
  unvalidated commands or an unbounded unsafe path.
- Compatibility and data migrations require explicit forward and backward
  behavior in the child plan before production changes begin.

## Shared quality and publication gates

Every RF change, including planning and governance changes, follows the current
repository instructions rather than copying command variants into this plan.

Before commit or pull request:

- every changed file passes the deterministic secrets scan;
- a positive secret finding is a hard stop: do not read, commit, or publish the
  file; rotate the credential at its source and remove it;
- local Sonar analysis targets the actual pull-request base;
- actionable findings are fixed rather than hidden through gate reduction,
  broad suppression, or changed-file exclusion;
- affected automated tests and clean builds pass.

Publication and review:

- every pull request starts as a draft;
- SonarCloud must be green;
- the pull request must have zero `OPEN` or `CONFIRMED` Sonar findings before
  review;
- remote CI and required platform lanes must be complete, not merely configured;
- an unavailable scan, platform, device, or manual lane remains an explicit open
  gate.

The detailed commands and capability-limitation handling live in
`.github/copilot-instructions.md`,
`.github/instructions/sonarqube-pre-pr.instructions.md`, and
`.github/instructions/spec-kit-governance.instructions.md`.

## Validation matrix

Each child plan selects every affected row, supplies exact commands and expected
results, and records where the evidence will be retained.

| Change area | Minimum local validation | Mandatory pre-merge validation |
| --- | --- | --- |
| Common, Domain, Backend | Focused NUnit fixtures for changed behavior and failure paths | Complete affected `Test/Test.csproj` graph and clean Release build |
| Z21 and EventBus | Ordering, overload, cancellation, failure, and shutdown tests | Stress tests and complete Backend regression suite |
| MOBApi and SignalR | Controller and hub integration plus negative validation and queue-bound tests | API regression tests, dependency audit, and load evidence |
| WinUI and SharedUI | Focused platform-neutral ViewModel or service tests and the documented FastDebug compile check | Windows Release build, desktop tests, and required manual UI acceptance |
| MAUI and Android | Focused shared/mobile tests and documented clean restore | Release AAB build, artifact validation, and affected-device acceptance |
| ESP32 | Host-native parser/protocol tests and PlatformIO build | Target-board smoke, negative, recovery, and endurance checks as required |
| XAML and accessibility | Binding build, keyboard path, focus, and automation-name inspection | Narrator, text scaling, Light, Dark, High Contrast, and Accessibility Insights acceptance |
| CI, plans, and governance | Governance tests, link/path review, and clean diff checks | Real workflow execution plus shared Sonar and secrets gates |
| Performance and operations | Focused benchmark or load scenario | Documented environment, baseline, limit, telemetry, and agreed regression threshold |

A skipped required lane is not a pass. The child issue remains open or blocked
until the evidence exists or the owning issue explicitly changes the acceptance
contract.

## Manual and hardware acceptance

A child plan may omit manual or hardware acceptance only when it explains why
the package affects neither. When either is required, evidence includes:

- device or machine model, operating system, firmware and application version,
  and tested commit;
- normal, negative, recovery, cancellation, and interruption scenarios;
- expected and actual results;
- remaining limitations and follow-up ownership.

UI acceptance additionally covers keyboard operation, focus order, Narrator,
text scaling, Light, Dark, and High Contrast. Building or running an emulator
does not replace real-device evidence when the acceptance criteria require
hardware.

Starting the MOBAflow WinUI application always requires explicit prior user
approval. Build, restore, test, and planning authorization do not imply launch
authorization.

## Minimum contract for plan-required child plans

Before implementation, each plan-required child plan must contain:

1. the authoritative GitHub issue and required Spec Kit classification;
2. purpose, committed scope, and explicit out-of-scope boundaries;
3. technical context, affected projects and interfaces, and resolved unknowns;
4. hard and recommended dependencies plus downstream consumers;
5. design decisions, rationale, and alternatives considered;
6. compatibility, migration, security, and telemetry effects;
7. risks, mitigations, stop conditions, and rollback;
8. automated test strategy with exact commands and expected results;
9. manual and hardware acceptance requirements or a reasoned not-applicable
   statement;
10. secrets, local Sonar, draft-PR, remote SonarCloud, and CI gates;
11. independently reviewable delivery slices and evidence ownership;
12. completion cleanup, including deletion of the standalone plan after the
    issue closes.

No `NEEDS CLARIFICATION` item may remain when implementation begins. Child plans
reference shared repository rules instead of maintaining divergent copies.

## Programme completion and cleanup

The programme may close only when GitHub issue #47 demonstrates:

- every RF child satisfies its issue-specific acceptance criteria;
- all P0 boundary, security, and release risks are closed;
- supported delivery targets build reproducibly through mandatory CI;
- analyzer, dependency, formatting, coverage, mutation, Sonar, and secrets gates
  are enforced as agreed;
- hardware-control commands are validated and bounded at the API boundary;
- RF-19 through RF-30 meet their acceptance anchors or child acceptance criteria;
- event ordering, overload, cancellation, and shutdown are deterministic;
- platform-neutral behavior has moved out of the identified UI hotspots;
- critical workflows have accessibility, theme, manual, and hardware evidence;
- operational limits and failure telemetry are documented;
- repository guidance and executable gates describe the same engineering rules.

Live progress metrics and merge history remain in GitHub and CI. After issue #47
closes and durable rules have moved to current documentation or automated
guards, delete this standalone plan. The closed issue and Git history retain the
programme record.
