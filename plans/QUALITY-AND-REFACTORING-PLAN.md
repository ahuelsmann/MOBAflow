# MOBAflow Quality and Refactoring Programme Plan

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/47
**Spec Kit**: Not applicable - programme coordination plan; plan-required child issues own their issue-specific planning artifacts

## Purpose

This is the single technical umbrella plan for the MOBAflow quality and
refactoring programme RF-01 through RF-18.

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

- stable technical outcomes for RF-01 through RF-18;
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

## Execution model

The four programme milestones are dependency groups, not strictly serialized
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
| RF-03 | [#50](https://github.com/ahuelsmann/MOBAflow/issues/50) | Only paired and authorized clients can observe or control protected runtime and hardware state. |
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
2. protect MOBApi authentication, authorization, validation, and throttling;
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
6. application and network lifecycle handling.

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
2. load-test SignalR behavior and its admission and rate limits;
3. measure MAUI startup and binding costs;
4. run firmware endurance scenarios with packet loss and Wi-Fi reconnection;
5. verify structured telemetry for dropped events, authentication failures,
   queue overload, recovery, and failed shutdown.

Acceptance anchor: each critical scenario records its environment, baseline,
limit, agreed regression threshold, and normal and failure results; required
hardware evidence covers recovery and endurance; the work consumes RF-03,
RF-04, and RF-07 guarantees without reopening their implementation scope.

## Dependency graph

| Package | Hard prerequisites | Recommended prerequisites | Downstream consumers |
| --- | --- | --- | --- |
| RF-01 | None | None | RF-02, RF-07 |
| RF-02 | None | RF-01 | Display and provisioning consumers |
| RF-03 | Security design before production behavior | None | RF-09, RF-12, RF-18 |
| RF-04 | None | None | RF-09, RF-11, RF-18 |
| RF-05 | None | None | RF-06, RF-07, RF-08, RF-12 |
| RF-06 | None | RF-05 | RF-07, RF-09, RF-10, RF-13, RF-14, RF-15 |
| RF-07 | RF-01, RF-05, RF-06 | None | RF-18 |
| RF-08 | RF-05 | None | MAUI release quality |
| RF-09 | RF-03, RF-04, RF-06 | None | Security and concurrency regression protection |
| RF-10 | RF-06 | None | Repository-wide formatting enforcement |
| RF-11 | RF-04 | None | Train-control maintainability |
| RF-12 | RF-03, RF-05 | None | Mobile maintainability and secure client evolution |
| RF-13 | RF-06 | None | Issue #34 Slice 6, RF-16 |
| RF-14 | RF-06 | None | Issue #34 Slice 7, RF-16 |
| RF-15 | RF-06 | None | Legacy-service and path-safety cleanup |
| RF-16 | RF-13, RF-14 | None | Product-quality acceptance |
| RF-17 | None | None | Agent and contributor consistency |
| RF-18 | RF-03, RF-04, RF-07 | None | Operational release confidence |

The explicit issue #34 unblock path is:

`RF-06/#90 -> RF-13/#91 and RF-14/#92 -> issue #34 Slices 6-8`

RF-13 and RF-14 remain separate plan-required packages and may run in parallel
after RF-06 satisfies their hard gate.

## Milestone outcomes

### Milestone 1: Eliminate immediate boundary risks

RF-01 through RF-05 close parser bounds, provisioning, control-plane,
event-ordering, and Android Release risks. Each package must leave a permanent
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

## Programme risk register

| Risk and trigger | Mitigation | Stop or escalation condition |
| --- | --- | --- |
| Plan and issue drift: the plan states workflow status or conflicts with an issue. | GitHub remains authoritative; keep status out of this plan and reconcile technical text against the issue. | Stop dependent planning until the contradiction is resolved. |
| Dependency bypass: implementation begins before a hard prerequisite or plan gate. | Validate the dependency graph, issue, plan, branch, and worktree before implementation. | Stop the package and return it to planning. |
| Feature-scope leakage: an RF change starts implementing behavior from #30 through #36. | Keep feature behavior in its owning issue and expose only the required technical boundary. | Split or move the behavior before review. |
| Unreviewable or irreversible slice: a change crosses responsibilities or lacks characterization tests. | Deliver one responsibility per independently buildable slice and retain a tested rollback point. | Split the change before implementation or publication. |
| Required platform or hardware lane is unavailable. | Record the exact environment limitation and keep the acceptance gate visible. | The package remains open or blocked; unavailable is never reported as passed. |
| Security migration creates incompatible or unprotected clients. | Define compatibility windows, credential ownership, rotation, revocation, recovery, and telemetry before rollout. | Fail closed; never restore anonymous or unprotected control. |
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
- Security, provisioning, and hardware-control rollback must remain fail-closed.
  A rollback may disable or restrict a new capability, but it must never restore
  anonymous control, open credential access, or an unbounded unsafe path.
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
| MOBApi and SignalR | Controller and hub integration plus negative authentication, authorization, bounds, replay, and throttling tests | API security tests, dependency audit, and load or rate-limit evidence |
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
- hardware-control operations require authenticated and authorized clients;
- event ordering, overload, cancellation, and shutdown are deterministic;
- platform-neutral behavior has moved out of the identified UI hotspots;
- critical workflows have accessibility, theme, manual, and hardware evidence;
- operational limits and failure telemetry are documented;
- repository guidance and executable gates describe the same engineering rules.

Live progress metrics and merge history remain in GitHub and CI. After issue #47
closes and durable rules have moved to current documentation or automated
guards, delete this standalone plan. The closed issue and Git history retain the
programme record.
