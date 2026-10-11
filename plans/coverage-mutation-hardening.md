# Coverage and mutation-test hardening strategy

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/47
**Spec Kit**: Not applicable - risk-based triage and regression tests for existing contracts; new product behavior and cross-cutting engineering features require their own workflow classification before implementation.

Improve tests so that they detect meaningful faults. Protect signal state, commands
and data first. A higher aggregate score must not hide critical untested contracts.

This proposal describes the explicitly requested coverage/Stryker work. It does not
replace the [quality programme](QUALITY-AND-REFACTORING-PLAN.md#rf-09-coverage-and-mutation-ratchets)
or RF-09 acceptance. Programme ownership, binding work packages and acceptance remain
in GitHub. Adopting this as an official RF-09 child plan requires coordination and
links; this document does not change GitHub issues.

## Planning baseline

Historical local measurements from 2026-10-09 on `codex/coverage-mutation-tests`,
based on `d45e14c5a3cace855f1c2672746cc26615b8814f` plus the tests present at that point:

| Measurement | Common | Domain |
| --- | ---: | ---: |
| Mutation score | 50.66% | 91.58% |
| Killed by failing tests | 762 | 358 |
| Detected through timeout | 6 | 1 |
| Survived | 430 | 25 |
| No coverage in this test group | 318 | 8 |
| Local threshold at the planning baseline | 50% | 90% |

Generated, untracked baseline reports:
`MutationTest/StrykerOutput/Common-final-gate/reports/` and
`MutationTest/StrykerOutput/Domain-verified/reports/`.
Later changes require fresh reports for their actual source/test scope.
These numbers are not a permanent current-status statement.

The Windows suite passed 1,798 tests with no failures/skips. Combined unit/integration
coverage was 53.79% of lines and 43.72% of branches, excluding Android.
Line coverage: Domain 99.71%, Common 78.62%, SharedUI 59.59%.

Stryker's score includes detected timeouts divided by valid mutants. Compile errors
and ignored mutants are outside that denominator. A surviving mutant is not proof
of a production defect. See [Stryker states and metrics](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/).

## Classify each open mutation

| Category | Action | Evidence |
| --- | --- | --- |
| Relevant existing test not included | Include it and required helpers in the correct group | Tests discovered; fresh full-group report |
| Meaningful change not detected | Strengthen an assertion or add a missing behavior case | Test passes original code and detects the mutation |
| Confirmed production defect | Establish expected behavior, add a failing regression, apply the smallest fix | Red-before/green-after test and affected-consumer checks |
| Proven equivalent mutation | Explain valid input range and observable behavior | No relevant difference for all allowed inputs |
| Contract or ownership unclear | Keep open and inspect callers/requirements | No speculative fix or premature equivalence claim |

Equivalent does not mean only that existing examples return the same result.
Messages or error precedence can be observable. Stryker cannot reliably decide
equivalence automatically. See [equivalent mutants](https://stryker-mutator.io/docs/mutation-testing-elements/equivalent-mutants/).

Record project, path, method/source location, mutation kind/replacement, report/commit,
contract, risk and test or rationale. Mutant IDs alone are unstable between runs.

Do not exclude whole files, strings or mutation types, or remove production code just
to raise a score. Keep equivalent residual cases visible unless a narrow, reviewable
exception is separately justified. Scope/exclusion changes must remain explicit in comparisons.

## Original package order

These packages describe the original strategy. The current delivery closes the
already-started Domain/Common increment; it does not start the deferred Backend,
API, SharedUI or platform groups.

### Package 0: Test mapping and comparable baseline

Before writing tests, determine whether NoCoverage means a missing behavior test or
a missing test-project link. Initially Common included only `Test/Common/**/*.cs`;
Backend recording fixtures and `MultiplexerCommandResolverTests.cs` also exercised
Common code but were not included.

Reuse relevant existing files/helpers before duplicating assertions. Measure the
full Common group afterward and distinguish mapping improvements from new-test gains.
Survived/NoCoverage describe this concrete group, not automatically the whole solution.

Acceptance: Required tests/helpers are mapped, discovered and runnable. Open mutants
are assigned to a package. Registering a file in a planned group is not active protection.

### Package 1: Signal state and commands

Prioritize control values over labels/messages.

| Contract | Baseline survived / no coverage | Behavior cases |
| --- | ---: | --- |
| `SignalBoxSnapshotMerge.cs` | 12 / 5 | Different incoming/cached values; incoming wins, cache fills gaps; unknown IDs, empty lists, prior elements and unchanged inputs |
| `MultiplexerHelper.cs` | 55 / 19 | Independent expected offset/output/activation; unknown articles, unsupported aspects, defaults and maximum offset |
| `MultiplexerDefinition.cs` | 6 / 1 | Supported/missing mappings and exact command values, not mere existence |
| `RuntimeCommandValidator.cs` | 7 / 0 | Multiple invalid fields, meaningful errors and precedence only where contractually required |

Use an independently specified reference table for signal commands, never expectations
generated from the same production table. Reuse concrete Backend tests; range checks
alone do not prove mapping correctness.

Acceptance: Every relevant mutant has a detecting test or documented equivalence.
An unexplained control-relevant mutant is not accepted solely because the global score
passes. RF-09's later Z21 ordering/overload, API validation and bounded queue checks
need their own project mutation groups; linked Backend tests do not mutate Backend code.

### Package 2: Data and shared functions

| Area | Baseline examples: survived / no coverage | Strategy |
| --- | --- | --- |
| Persistence/settings | `AppSettings.Sections.cs` 69 / 3; `AppSettings.cs` 10 / 0; `PhotoPathHelper.cs` 28 / 29 | Missing/explicit JSON, safe defaults, load failures and path types; do not touch user photos/data |
| Recording/filtering | `RecordingFilter.cs` 18 / 0; `RecordingArtifact.cs` 10 / 0; `RecordingSession.cs` 0 / 32 | Include/exclude decisions, bounds, order and output; reuse lifecycle/limit tests |
| Discovery | `RestApiDiscoveryCandidateBuilder.cs` 45 / 12; `MobApiUdpDiscoveryResponder.cs` 14 / 66; `SubnetCandidateBuilder.cs` 14 / 1 | Invalid addresses/responses, duplicates, cancellation/failures and deterministic network/time fakes |
| Appearance | `LocomotiveFunctionAppearanceResolver.cs` 55 / 14; `FunctionBacklightColor.cs` 13 / 2 | Explicit on/off, unknown functions, colors and both themes; not only non-null assertions |

Keep other Common cases, including EventBus, snapshots and JSON validation, in the
inventory. These sample counts are not completed diagnoses. Labels, formatting and
logs usually rank below incorrect control state or data loss; actual impact decides.

Acceptance: Close real gaps, strengthen result assertions and explain remaining cases.
Improve a full Common run without narrowing mutation scope. Coordinate async tests
deterministically, not with sleeps.

### Package 3: Domain residual cases

Classify all 25 baseline survivors and eight uncovered mutants. Prioritize
`Solution.cs` (five survivors), `TrackPlanDocument.cs` and
`TrackPlanDocumentMapper.cs` (three each): load failures, unchanged existing data,
empty/full models, IDs/directions and serialization. Then inspect timetable,
workflow and remaining model fields.

Investigate the two `MatrixImage.cs` boundary-comparison mutants for equivalence,
not automatically as bugs. At 25 cells, an extra internal step may have no observable
effect; valid inputs and side effects must still be considered. Do not invoke unused
internal descriptor helpers through reflection merely to create coverage.

Acceptance: No unexplained meaningful mutation in the selected contracts. Domain
stays above the existing local 90% floor; 95% is a measurement goal, not artificial-test pressure.

### Package 4: Deferred groups and larger coverage gaps

RF-09's later order is Backend/API, then SharedUI. Reuse NUnit files, measure complete
projects, establish a baseline and verify actual CI execution. SharedUI priorities
include routing, project switches and late responses.

Then include other suitable projects. Windows/Android require appropriate separate
runs; Stryker.NET does not mutate ESP32 firmware. Architecture guards need adversarial
inputs, not only the correct repository state. Classify cross-cutting CI mechanisms
through Spec Kit before implementation.

Long-term acceptance: Relevant existing/new test files are actually included, not only
registered as planned, and their production projects are mutated. Document concrete
technical limits. An integration test need not kill a mutant individually; registration
alone also does not prove effectiveness.

## Targets and regression protection

Original Common milestones: 60%, 70%, 80%, measured in full runs. At the baseline
denominator of 1,516 valid mutants and 768 detected, those targets require at least
142, 294 and 445 additional detections. Recalculate after mapping/source changes and
keep versions, scope, timeouts and exclusions visible.

Raise CI floors only after measured improvement; never lower them to pass.
Critical contracts have separate behavioral acceptance. Timeouts count as detected
for Stryker but are not a substitute for precise assertions.

Original Windows line-coverage goals: 55%, then 60%. At 42,916 measurable lines and
23,087 covered lines, at least 517 and 2,663 additional covered lines are needed.
Domain/Common alone cannot provide a large solution-wide gain; future Backend/API/
SharedUI work is needed. These goals concern combined Windows unit/integration
coverage. A unit-only figure needs reliable test classification first.

## Delivery checks per increment

1. Inspect mutations/callers and classify contract/risk.
2. Reuse tests or add focused NUnit behavior cases: boundaries, nulls, false values,
   failures and conflicting state. Do not derive expectations from the implementation.
3. Run affected regular tests; they must be discovered and pass.
4. Run focused mutations, then the full affected group. A focused score is not a
   group score and does not justify lowering its threshold.
5. Run relevant broader tests/coverage under [AGENTS.md](../AGENTS.md), reporting scope/skips.
   Plan-only changes do not need .NET runs.
6. Compare killed, survived, uncovered, line/branch coverage, test scope and residual
   rationale. Distinguish configuration from green remote CI.
7. Deliver cohesive draft PRs with separate secrets, line-ending, Quality CI and
   current-commit SonarCloud gates.

Use fakes or owned temporary data. No app launch, train movement or layout action
is authorized by this plan. Run regular builds/tests and Stryker sequentially in
one worktree because they share intermediate build files.

## Existing group commands

Run from the dedicated worktree root:

```powershell
dotnet test MutationTest/Domain.MutationTests.csproj -c Debug
dotnet test MutationTest/Common/Common.MutationTests.csproj -c Debug

Push-Location MutationTest
try {
    dotnet stryker --skip-version-check
    dotnet stryker --config-file stryker-common-config.json --skip-version-check
}
finally {
    Pop-Location
}
```

A focused diagnostic run may add `--mutate "**/SignalBoxSnapshotMerge.cs"`.
Keep full configurations unchanged; the focused result is not overall acceptance.
See [Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration/).

For broad coverage, use [CONTRIBUTING.md](../CONTRIBUTING.md).
Gates are defined in [coverage-thresholds.json](../Test/coverage-thresholds.json),
and groups in [mutation-lanes.json](../MutationTest/mutation-lanes.json).

## Started increment and remaining scope

The delivered increment links Common-relevant Recording/Multiplexer tests, strengthens
Domain/default/load/converter cases and Common signal, command, color, sink,
photo-path and discovery contracts. The quick-window address-boundary defect has
a regression test and a minimal fix that rejects out-of-range octets before byte conversion.

The accompanying configuration activates Domain/Common, raises their measured
floors to 90%/63%, and adds the Windows Common CI run. It does not activate the
deferred production groups or claim whole-solution mutation protection.
Final counts and CI status belong to the delivery PR's validation record, not the
historical baseline above.
