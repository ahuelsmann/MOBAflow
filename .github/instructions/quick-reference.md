# Development reference

[AGENTS.md](../../AGENTS.md) contains current build commands, validation choices and completion criteria.
[The instruction index](instructions-index.md) routes to task-specific technical guidance.

- SDK and targets: `global.json`, `Moba.slnx` and the affected `.csproj`.
- NuGet package versions: `Directory.Packages.props`; build defaults: `Directory.Build.props`.
- Local tasks: `.vscode/tasks.json`; check arguments against the project before reuse.
- Desktop build/tests/coverage: [GitHub quality workflow](../workflows/quality.yml) and
  [Azure quality pipeline](../../.azure-pipelines/quality.yml).
- Release definitions: `.github/workflows/release-studio.yml`, `.azure-pipelines/release.yml`.
- Configuration JSON validation: `MOBAflow/Build/ValidateJsonConfiguration.targets`.
- Open work: Azure DevOps project MOBAflow. Historical tooling proposals do not authorize dependency installation
  or create additional local completion gates.

Use English Conventional Commits when a commit is requested; changelog tooling uses those messages.