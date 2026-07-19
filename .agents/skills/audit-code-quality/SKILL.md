---
name: audit-code-quality
description: Perform a read-only, repository-wide code-quality audit against official and authoritative standards, including ISO/IEC 25010, Microsoft .NET analyzers and design guidance, .NET testing guidance, NIST SSDF, Windows accessibility guidance, and C++ Core Guidelines where applicable. Use when Codex must assess an entire repository or codebase, establish a quality baseline, review maintainability, reliability, security, architecture, tests, coverage, dependencies, C#/XAML/C/C++ code, or produce an evidence-backed quality report without changing code.
---

# Audit Code Quality

Assess the complete repository with reproducible evidence. Keep the audit read-only unless the user separately asks for fixes.

## Load the audit basis

1. Locate the repository root and read every applicable `AGENTS.md` plus repository instruction files before running checks.
2. Read [references/standards.md](references/standards.md) completely.
3. Read [references/report-template.md](references/report-template.md) completely.
4. Treat repository rules as mandatory local acceptance criteria. Treat official standards as the external quality model.
5. Record the commit, branch, dirty state, operating system, SDK/tool versions, date, and unavailable platform workloads.

Never describe a locally defined threshold or score as an official threshold. ISO/IEC 25010 defines a quality model, not a universal repository score.

## Establish complete scope

Run the evidence collector from the repository root:

```powershell
pwsh -NoProfile -File <skill-dir>/scripts/collect-quality-evidence.ps1 -RepositoryPath .
```

Use its JSON output to establish source languages, projects, tests, quality configuration, and cross-cutting risk candidates. Inspect all source categories it discovers. Exclude generated, vendored, package-cache, build-output, and third-party code from quality ratings, but report the exclusions and their counts.

Define coverage in three separate layers:

- **Inventory coverage:** all discovered source files were classified.
- **Automated coverage:** projects and files reached by successful analyzers, builds, tests, coverage, and dependency scans.
- **Review coverage:** files or components inspected semantically by Codex.

Do not claim that every line was manually reviewed. A whole-repository audit means deterministic inventory, repository-wide automated checks where supported, cross-cutting searches across all source, and risk-based semantic review of every major component.

## Execute repository-native quality gates

Prefer documented repository commands and CI definitions over invented commands. Respect platform restrictions and project-scoped build instructions.

Run, when supported and already configured:

1. Release builds with compiler warnings and analyzers enabled.
2. Formatting verification with `dotnet format ... --verify-no-changes`; never let the audit format files.
3. Unit and integration tests using the repository's documented command.
4. Coverage collection using the repository's configured collector and thresholds.
5. Dependency vulnerability auditing, including transitive dependencies.
6. Existing mutation tests, architecture tests, lint tools, Sonar, CodeQL, or firmware checks.
7. Code metrics already available in the environment: maintainability index, cyclomatic complexity, class coupling, inheritance depth, and source lines.

Do not install tools, update packages, restore prohibited solution graphs, start external services, or change configuration solely for the audit without explicit permission. If a check cannot run, state `Not verified`, give the exact reason, and use narrower evidence without treating absence of evidence as a pass.

Capture command, exit code, target, duration, and relevant output for every executed gate. Distinguish pre-existing dirty changes from audit artifacts.

## Review the code semantically

Cover each major production project and source language. Prioritize externally reachable code, concurrency and event handling, parsers and serialization, persistence, network and process boundaries, platform adapters, large or complex files, shared state, unsafe/native code, and code weakly covered by tests.

Evaluate at least:

- correctness, invariants, error handling, cancellation, resource lifetime, and async/thread safety;
- architecture boundaries, dependency direction, cohesion, coupling, duplication, and public API design;
- input validation, authentication/authorization where relevant, secret handling, unsafe deserialization, command/query construction, dependency risk, logging, and privacy;
- performance risks supported by evidence, avoiding speculative micro-optimization;
- test isolation, determinism, readability, behavioral coverage, branch coverage, and mutation strength where available;
- C# nullable analysis, analyzer suppressions, warning policy, and `.editorconfig` consistency;
- XAML binding correctness, localization policy, theme resources, keyboard access, automation names, contrast, and platform guidance;
- C/C++ ownership, bounds, lifetime, casts, concurrency, error handling, and compiler/static-analysis coverage.

Validate every search hit in context before reporting it. A pattern match is a candidate, not a finding.

## Classify evidence and findings

Label evidence as one of:

- `Executed`: directly observed command result.
- `Static`: verified in source at a cited file and line.
- `Configuration`: enabled or disabled by repository configuration.
- `Documented`: claimed by repository documentation but not independently run.
- `Inference`: reasoned conclusion; state the premises and lower confidence.

Use these severities:

- `Critical`: credible immediate security, data-loss, safety, or release-blocking risk.
- `High`: likely defect, major quality-gate failure, or systemic architectural risk.
- `Medium`: material maintainability, test, portability, accessibility, or reliability gap.
- `Low`: localized improvement with limited current impact.

Every finding must contain a concise title, severity, confidence, affected scope, file and line evidence, violated local rule or authoritative standard, impact, and a concrete remediation plus verification approach. Merge duplicates under one root cause. Never report stylistic preference as a defect unless a repository rule or configured analyzer supports it.

## Rate quality transparently

Rate each applicable quality area with this audit rubric:

- `4 - Strong`: relevant gates pass and corroborating implementation evidence exists.
- `3 - Adequate`: controls work with only limited, non-systemic gaps.
- `2 - At risk`: material gaps or incomplete verification reduce confidence.
- `1 - Deficient`: systemic failures or high-severity findings dominate.
- `0 - Critical`: credible critical risk or absence of essential controls.
- `NV - Not verified`: evidence is insufficient or the platform cannot be assessed.

Always add `High`, `Medium`, or `Low` confidence and explain the rating. Do not compute an aggregate score unless the user explicitly requests one. If requested, publish the weights and formula and label them as an audit-specific rubric.

## Produce the report

Follow [references/report-template.md](references/report-template.md). Match the user's language while keeping commands, identifiers, and official rule names unchanged.

Lead with the conclusion and the highest risks. Include all Critical and High findings. Include Medium and Low findings in a complete appendix or machine-readable artifact when volume would obscure the main report. Include positive controls only when verified; avoid generic praise.

End with:

1. a prioritized remediation sequence;
2. the exact verification commands for the next audit;
3. limitations and unverified platforms;
4. source links close to the claims they support.
