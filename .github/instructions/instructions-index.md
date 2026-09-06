---
description: 'Task-scoped navigation for MOBAflow agent instructions.'
applyTo: '**'
---

# Instruction index

[AGENTS.md](../../AGENTS.md) owns the shared workflow, architecture constraints and validation policy.
Read only the entries needed for the task. Copilot may load matching `applyTo` files; other agents should open
relevant files explicitly. Examples illustrate patterns; current source code defines the actual APIs.

| Task | Guidance |
| --- | --- |
| Runtime, layers, data flow | [Architecture](architecture.instructions.md) |
| Backend behavior and I/O | [Backend](backend.instructions.md) |
| Z21 parsing, connection, commands | [Z21](z21-backend.instructions.md) |
| DI registrations and page lifetime | [DI](di-pattern-consistency.instructions.md) |
| Observable state and commands | [MVVM](mvvm-best-practices.instructions.md), [auto-save](auto-save-pattern.instructions.md) |
| WinUI views and styling | [WinUI](winui.instructions.md), [Fluent](fluent-design.instructions.md) |
| XAML inclusion/compiler failures | [Page registration](xaml-page-registration.instructions.md) |
| Android UI and resources | [MAUI](maui.instructions.md) |
| Tests and test doubles | [Testing](test.instructions.md) |
| C# conventions | [Naming](naming-conventions.instructions.md), [comments](self-explanatory-code-commenting.instructions.md), [encoding](no-special-chars.instructions.md) |
| Instruction maintenance | [Writing instructions](instructions.instructions.md) |
| Independent repository write tasks | [Worktree isolation](git-worktree-isolation.instructions.md) |
| Commits and pull requests | [Secrets and Sonar gates](sonarqube-pre-pr.instructions.md) |
| Feature specifications and plans | [Spec Kit workflow](../../docs/SPEC-KIT.md), [governance](spec-kit-governance.instructions.md) |

## Further references

- [Project overview](../../README.md), [architecture documentation](../../docs/ARCHITECTURE.md),
  [project reference](../../docs/PROJECT-REFERENCE.md), [build performance](../../docs/BUILD-PERFORMANCE.md)
- [Validation entry points](quick-reference.md), [GitHub CI](../workflows/quality.yml).
  `.azure-pipelines/` contains retained legacy definitions, not a second Git remote or default workflow.
- [Visual Studio setup](vs-setup.instructions.md) is optional environment guidance.
- `copilot-tips.instructions.md`, `future-enhancements.instructions.md`, `visual-summary.md` and
  `summary-hooks-packages-sonarqube.md` are historical/reference material, not active task requirements or a backlog.
  Open work is tracked in GitHub issues, milestones and Kanban.
