# Development reference

[AGENTS.md](../../AGENTS.md) contains current build commands, validation choices and completion criteria.
[The instruction index](instructions-index.md) routes to task-specific technical guidance.

- SDK and targets: `global.json`, `Moba.slnx` and the affected `.csproj`.
- NuGet package versions: `Directory.Packages.props`; build defaults: `Directory.Build.props`.
- Local tasks: `.vscode/tasks.json`; check arguments against the project before reuse.
- Desktop build/tests/coverage and analyzer baselines: [GitHub quality workflow](../workflows/quality.yml).
- Release definitions: `.github/workflows/release-studio.yml`; `.azure-pipelines/` is retained legacy material.
- Commits and PRs: [secrets scanning and Sonar gates](sonarqube-pre-pr.instructions.md).
- Configuration JSON validation: `MOBAflow/Build/ValidateJsonConfiguration.targets`.
- Open work: GitHub issues, milestones and Kanban. Historical tooling proposals do not authorize dependency installation
  or create additional local completion gates.

Use English Conventional Commits when a commit is requested; changelog tooling uses those messages.
