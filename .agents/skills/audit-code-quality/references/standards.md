# Authoritative quality basis

Use the sources below as the external audit basis. Verify current versions online when network access is available. Cite the exact source used in the report.

## Quality model

- [ISO/IEC 25010:2023 product quality model](https://www.iso.org/standard/78176.html) defines nine product-quality characteristics for ICT and software products. Use it to organize the assessment, not to claim certification or an official numeric score.
- A source-code audit can provide strong evidence for maintainability and security, partial evidence for functional suitability, performance efficiency, compatibility, reliability, flexibility, interaction capability, and safety, but cannot conclusively validate runtime or user outcomes without execution evidence.

## .NET and C#

- [.NET code analysis overview](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview): use SDK Roslyn `CAxxxx` and `IDExxxx` diagnostics as the primary automated C#/VB quality evidence.
- [.NET analyzer rule categories](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories): cover design, documentation, globalization, maintainability, naming, performance, reliability, security, style, and usage as applicable.
- [.NET maintainability rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/maintainability-warnings): include excessive complexity, coupling, inheritance, dead conditions, and maintainability-index rules when enabled.
- [.NET security rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/security-warnings): inspect security diagnostics and justify any suppression.
- [.NET reliability rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/reliability-warnings): inspect disposal, threading, stream handling, and other reliability diagnostics.
- [.NET Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/): apply to public and reusable APIs. Treat `Do`, `Consider`, `Avoid`, and `Do not` with their intended strength and context.
- [`dotnet format`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format): use `--verify-no-changes` for non-mutating `.editorconfig` and analyzer verification.

## Metrics

- [Microsoft code metrics](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-values): use maintainability index, cyclomatic complexity, class coupling, inheritance depth, and source/executable lines as risk indicators, not proofs of quality.
- [Cyclomatic complexity guidance](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity): high complexity indicates higher testing and maintenance risk. Prefer configured analyzer thresholds such as CA1502 over an invented cutoff.
- [Maintainability index meaning](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-maintainability-index-range-and-meaning): report the tool and formula/version because implementations and thresholds can differ.

Do not estimate complexity or maintainability index by visual inspection. If no supported metrics tool ran, mark metrics as `Not verified` and use file size or branch density only as hotspot-selection heuristics.

## Tests and coverage

- [.NET unit-testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices): assess tests for speed, isolation, repeatability, self-checking behavior, timely creation, clear naming, and Arrange-Act-Assert structure.
- [.NET code coverage guidance](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage): line, branch, and method coverage measure execution, not test quality. Report coverage alongside assertion quality, boundary cases, and mutation results.

Never infer adequate test quality from a percentage alone. Prefer repository ratchets and risk-based gaps over a universal coverage target.

## Secure development and dependencies

- [NIST SP 800-218 SSDF 1.1](https://csrc.nist.gov/pubs/sp/800/218/final): assess secure-development evidence, protection of software, production of well-secured software, and vulnerability response practices that are observable in the repository.
- Use supported package-manager vulnerability commands and existing CI security scans. Record the resolved target framework and whether transitive dependencies were included.
- Do not print possible secret values. Report only the file, line, secret category, and remediation after validating the finding.

## Windows XAML accessibility

- [Microsoft Windows accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview) and [accessibility checklist](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-checklist): inspect keyboard access, screen-reader information, accessible names, text scaling, high contrast, and automated accessibility regression coverage.
- Source inspection cannot verify real contrast, focus order, screen-reader output, or dynamic states completely. Require runtime/manual validation for those claims.

## C and C++ firmware

- [C++ Core Guidelines](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines.html): assess interfaces, resource management, memory safety, concurrency, error handling, expressions, and performance. These are authoritative industry guidelines, not an ISO language conformance certificate.
- Prefer compiler warnings, sanitizers, static analyzers, and firmware tests configured by the repository. Mark hardware-only behavior as unverified when no device or simulator evidence exists.

## Repository rules

Repository `AGENTS.md`, `.editorconfig`, build properties, CI quality gates, architecture tests, and project instruction files define local acceptance criteria. Apply the stricter compatible rule. If a local rule conflicts with an external recommendation, report the conflict and its practical impact instead of silently choosing one.
