# RF-06 Effective Release Analyzer Gate

## Status

- Primary issue: [#90](https://github.com/ahuelsmann/MOBAflow/issues/90)
- Parent programme: [#47](https://github.com/ahuelsmann/MOBAflow/issues/47)
- Programme package: RF-06
- Priority: P1
- Programme milestone: Milestone 2 - Make quality requirements enforceable
- Status: In progress
- Baseline commit: `ec556b253359545137c9b8c6f5a00d46dee576cb`
- Branch: `codex/90-release-analyzer-gate`
- Worktree: `C:\Repo\ahuelsmann\MOBAflow-rf06`

## Outcome

Release and CI builds execute the configured .NET analyzers, retain strict compiler-warning
handling, and reject every new or reintroduced analyzer diagnostic without hiding the
historical diagnostic inventory.

RF-06 is complete only when the deterministic baseline and the enforcement slice are both
merged. Completion unblocks RF-13, RF-14, and the RF-06-dependent portions of RF-07,
RF-09, RF-10, and RF-15.

## Verified baseline

The baseline was measured with:

```powershell
dotnet build .\Test\Test.csproj `
  -c Release `
  -f net10.0 `
  --no-restore `
  -m:1 `
  -t:Rebuild `
  -p:RunAnalyzersDuringBuild=true `
  -p:TreatWarningsAsErrors=false `
  -p:EnforceCodeStyleInBuild=true `
  -p:AnalysisMode=All `
  -p:UseSharedCompilation=false
```

Verified configuration:

- `Directory.Build.props` sets `RunAnalyzersDuringBuild=false` for every configuration.
- Release already sets `AnalysisMode=All`, `EnforceCodeStyleInBuild=true`, and
  `TreatWarningsAsErrors=true`, so the intended gate is configured but disabled.
- `MOBAsmart.csproj` repeats `RunAnalyzersDuringBuild=false` and disables build-time code
  style analysis.
- The current Quality workflow performs a Release desktop build but does not prove that
  analyzer execution occurred.
- Microsoft documents `RunAnalyzersDuringBuild` as the build-time analyzer switch,
  `CodeAnalysisTreatWarningsAsErrors=false` as the way to keep CA diagnostics visible
  without allowing `TreatWarningsAsErrors` to promote the historical backlog, and
  `ErrorLog` as the compiler option for SARIF diagnostics.

The first deterministic cross-platform inventory contains 1,472 unique diagnostics across
61 rules:

| Project | Diagnostics |
| --- | ---: |
| Backend | 500 |
| SharedUI | 483 |
| MOBApi | 130 |
| Domain | 129 |
| Sound | 61 |
| Common | 55 |
| MOBAdisplay | 55 |
| TrackLibrary.PikoA | 38 |
| TrackPlan.Renderer | 19 |
| TrackLibrary.Base | 2 |

Highest-volume rules:

| Rule | Diagnostics | Classification |
| --- | ---: | --- |
| CA1848 | 330 | Performance / logging |
| CA1062 | 152 | Reliability / public boundary validation |
| CA1873 | 119 | Performance / logging |
| CA2007 | 117 | Async library policy |
| CA1031 | 114 | Reliability / intentional exception boundaries require review |
| CA1002 | 86 | Public API design |
| CA1515 | 75 | Encapsulation |
| CA2227 | 74 | Public API design / serialization compatibility |
| CA1707 | 63 | Naming |
| CA1305 | 45 | Globalization |

High-priority rules present in the inventory include CA5394, CA3003, CA2000, CA2213,
CA1001, CA1063, CA1416, CA1849, CA2016, and CA1508. These are reviewed before style,
naming, logging-performance, or broad API-design changes.

## Design decisions

### Analyzer visibility

- Release builds set `RunAnalyzersDuringBuild=true`.
- CI passes an explicit analyzer-gate property so logs prove that the gate is active.
- Compiler warnings remain errors.
- Historical CA diagnostics remain visible as warnings through
  `CodeAnalysisTreatWarningsAsErrors=false`; they are not suppressed.
- Code-style diagnostics remain enabled in Release.

### Deterministic baseline ratchet

- Compiler and analyzer diagnostics are emitted as SARIF with the `ErrorLog` option.
- A repository script normalizes diagnostics to repository-relative paths and compares
  grouped fingerprints consisting of project, target framework, rule, path, message, and
  occurrence count.
- The committed baseline contains only normalized fingerprints and counts, never source
  excerpts, absolute paths, tokens, or credentials.
- A new fingerprint, an increased count, or a reintroduced removed fingerprint fails CI.
- A decreased count also requires the same pull request to refresh the baseline. This
  prevents fixed capacity from silently becoming allowance for later regressions.
- Generated and external diagnostics are excluded only through compiler-supported generated
  code handling or an explicitly documented path rule.
- No `NoWarn`, broad `.editorconfig` disablement, ruleset exclusion, or quality-gate
  reduction is used to make the baseline pass.

### Risk-first remediation

Correctness and reliability are handled before style:

1. security and path/randomness diagnostics;
2. undisposed resources and invalid dispose patterns;
3. platform compatibility and async/cancellation misuse;
4. invalid null-state and unreachable-code findings;
5. remaining boundary validation findings where the contract is unambiguous;
6. only then performance, API-design, naming, and formatting debt.

Compatibility-sensitive domain collection warnings are not changed mechanically. JSON
contracts, persistence behavior, bindings, and public API consumers must be characterized
before such a refactoring.

## Delivery slices

### Slice 1 - Inventory and ratchet foundation

Deliver:

- deterministic SARIF collection;
- normalized baseline schema and comparison script;
- the complete cross-platform baseline generated from the pinned SDK;
- script tests covering new, increased, decreased, removed, malformed, generated, and
  absolute-path diagnostics;
- documented local inventory and verification commands.

This slice changes no production behavior and does not yet claim RF-06 completion.

### Slice 2 - Correctness remediation and enforcement

Deliver:

- fixes for the reviewed security, resource-lifetime, platform, async, cancellation, and
  invalid-state diagnostics;
- Release `RunAnalyzersDuringBuild=true`;
- `CodeAnalysisTreatWarningsAsErrors=false` for the historical CA backlog while compiler
  warnings remain errors;
- removal of the unconditional MOBAsmart analyzer disablement;
- Quality-workflow analyzer collection and baseline comparison;
- proof that one synthetic new diagnostic fails the gate;
- green desktop, Android, test, coverage, mutation, CodeQL, and SonarCloud checks.

This slice completes RF-06 and unblocks RF-13 and RF-14.

### Later debt reduction

Lower-risk warnings remain visible and are reduced through their owning RF package:

- RF-08 owns MAUI binding diagnostics.
- RF-10 owns mechanical formatting.
- RF-11 through RF-15 own architecture and API-shape changes.
- RF-17 owns instruction consistency.

Their future fixes must update the analyzer baseline in the same pull request.

## Validation

Minimum local validation:

- analyzer-baseline script tests;
- Release cross-platform analyzer rebuild and baseline comparison;
- full `net10.0` and Windows test suites;
- MOBAsmart Release or FastDebug compile as appropriate;
- scoped `dotnet format --verify-no-changes`;
- changed-file secret scans;
- `git diff --check`.

Mandatory pre-merge validation:

- draft pull request against current `main`;
- local Sonar attempt against the actual base;
- Windows build, tests, coverage, and dependency audit;
- Android Release AAB;
- Domain mutation tests;
- CodeQL;
- green SonarCloud quality gate;
- zero `OPEN` or `CONFIRMED` Sonar issues for the pull request.

## Rollback

- Slice 1 is additive and can be reverted without changing production behavior.
- Slice 2 can temporarily disable only the new baseline-comparison workflow step if a
  compiler/SARIF incompatibility is proven. Analyzer execution remains enabled so
  diagnostics stay visible.
- Do not restore the unconditional repository-wide or MOBAsmart analyzer disablement as a
  long-term rollback.

## References

- [Microsoft.NET.Sdk MSBuild properties](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props)
- [C# compiler warning and ErrorLog options](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/errors-warnings)
- [Configure .NET code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options)
- [Disable or enable build-time analysis](https://learn.microsoft.com/visualstudio/code-quality/disable-code-analysis)
