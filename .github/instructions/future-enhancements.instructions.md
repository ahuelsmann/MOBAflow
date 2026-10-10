# Historical quality improvement proposals

> Reference material, not an active backlog or authorization to install tools,
> change dependencies, add hooks or modify CI.
> Current work is tracked by [GitHub issues](https://github.com/ahuelsmann/MOBAflow/issues)
> and the [quality programme](../../plans/QUALITY-AND-REFACTORING-PLAN.md).

Earlier session proposals covered architecture decision records, benchmarks,
coverage reports, API documentation, dependency scanning, logging dashboards and
load testing. Their priority, effort estimates and implementation claims were
not verified against current code and must not be treated as project status.

## Current validation entry points

- [AGENTS.md](../../AGENTS.md) defines scope-based validation and launch safety.
- [Quality CI](../workflows/quality.yml) contains the implemented checks, including
  desktop coverage and Domain mutation testing.
- [CONTRIBUTING.md](../../CONTRIBUTING.md) documents the active test workflow.
- [Sonar policy](sonarqube-pre-pr.instructions.md) requires GitHub PR analysis.
  Local deterministic secrets scans remain separate.
- [Quick reference](quick-reference.md) links current repository tooling.

The repository does not use Git hooks for pre-commit validation, commit messages,
pre-push tests or post-checkout package restore. Historical claims that these
hooks were already implemented were incorrect. Do not install local Sonar or
Vortex code-analysis hooks as a substitute for the PR gate.

## How to revisit a proposal

Before implementation, establish the requirement and inspect the existing
solution, CI and packages. Reuse current tooling where it meets the need.
Any new cross-cutting engineering mechanism needs the repository's
[Spec Kit classification](spec-kit-governance.instructions.md).

Potential areas from the historical list:

| Area | Question to resolve first |
| --- | --- |
| Decision records | Which durable decision is missing from existing architecture documentation? |
| Benchmarks | Which measured regression needs a reproducible benchmark? |
| Coverage and mutation testing | Which untested contract is relevant, and which project is actually mutated? |
| API documentation | Which implemented endpoint or integration contract needs documentation? |
| Dependency scanning | Which risk is not already covered by NuGet audit or CI? |
| Logging and load testing | Which concrete diagnostic or capacity requirement justifies additional tooling? |

No historical session number, percentage target or tool name creates a new
implementation task. Do not claim a proposed service, workflow or dashboard is
shipped until its implementation and relevant checks are verified.
