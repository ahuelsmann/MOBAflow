# Spec Kit workflow

MOBAflow uses [GitHub Spec Kit](https://github.com/github/spec-kit) for
specification-driven feature development. The repository is initialized with
Spec Kit 1.0.4, the Codex skills integration, and PowerShell workflow scripts.

## Install the CLI

Install `uv`, then install the repository's pinned Spec Kit version:

```powershell
winget install --id astral-sh.uv -e
uv tool install specify-cli --from "git+https://github.com/github/spec-kit.git@v1.0.4"
specify version
```

Restart the terminal if `uv` or `specify` is not immediately available after
installation.

## Run a feature workflow with Codex

Open Codex from the repository root so it discovers the skills under
`.agents/skills/`. The normal production workflow is:

1. `$speckit-specify <feature description>`
2. `$speckit-clarify` when material requirements remain ambiguous
3. `$speckit-plan`
4. `$speckit-checklist` when an additional requirements quality gate is useful
5. `$speckit-tasks`
6. `$speckit-analyze`
7. `$speckit-implement`
8. `$speckit-converge`

Use `$speckit-taskstoissues` when an approved task list should be published as
dependency-ordered GitHub issues. The generated task issues use the `T###:`
title convention and remain traceable to the originating feature artifacts.

Feature artifacts are stored under `specs/NNN-feature-name/`. The active feature
is recorded by Spec Kit in `.specify/feature.json`.

The project constitution in `.specify/memory/constitution.md` mirrors the
mandatory repository rules. It does not replace `AGENTS.md`,
`.github/copilot-instructions.md`, or the scoped files under
`.github/instructions/`; agents must continue loading those instructions first.

## Issue and plan governance

New repository issues must classify whether Spec Kit is required and may link an
existing feature directory or tracking issue. Pull requests that change Spec Kit
artifacts, standalone plans, issue forms, or the governance automation are checked
by `.github/workflows/spec-kit-governance.yml`.

Standalone plans belong in `plans/`, must reference their GitHub issue, and must
declare whether Spec Kit is required. Completed standalone plans are deleted; the
closed GitHub issue and Git history are the permanent record. Feature work managed
with Spec Kit keeps `spec.md`, `plan.md`, and `tasks.md` together under `specs/`.

## Balanced secrets scanning

Before reading files that are likely to contain credentials, tokens, private keys,
certificates, connection strings, or deployment secrets, run:

```powershell
sonar analyze secrets <path>
```

Ordinary source files, tests, Markdown documentation, schemas, and templates do
not require an individual pre-read scan unless their context indicates that they
may contain secrets. Before a commit or pull request, every changed file must pass
the deterministic secrets scan.

## Validate the installation

```powershell
specify version
specify integration status
specify check
```

`specify integration status` should report `codex` as the default integration
with no missing or modified managed files. The project-specific template
overrides under `.specify/templates/overrides/` are intentionally not managed by
the CLI and survive integration upgrades.

## Use GitHub Copilot instead

The checked-in default integration is Codex. If the team deliberately changes
the repository default to GitHub Copilot, switch it with the CLI and review the
managed-file changes:

```powershell
specify integration switch copilot --script ps --integration-options="--skills"
```

To switch back:

```powershell
specify integration switch codex --script ps
```

Commit the resulting integration changes so every contributor uses the same
agent workflow.

## Upgrade Spec Kit

Upgrade deliberately and keep the repository version pinned:

```powershell
uv tool install specify-cli --force --from "git+https://github.com/github/spec-kit.git@vX.Y.Z"
specify integration upgrade codex
specify integration status
```

Review generated changes before committing. Do not overwrite the project
constitution or template overrides during an upgrade.

### Version 1.0.4 migration notes

The CLI installation and the checked-in Codex integration are updated separately.
On Windows, run the `uv tool install` command above directly if `specify self
upgrade` cannot replace its own running Python installation.

Managed skills, scripts, and core templates use LF line endings through
`.gitattributes`. This keeps their manifest checksums stable in Windows worktrees
and avoids reporting line-ending conversions as local customizations.

The constitution command now updates the constitution and its impact report only;
it no longer automatically propagates policy edits into templates. MOBAflow's
custom templates remain in `.specify/templates/overrides/`. Review them explicitly
when changing project policy and run the governance checks below. The plan and
analysis commands read the current constitution when they run.

```powershell
./scripts/Test-SpecKitGovernance.Tests.ps1
./scripts/Test-InstructionConsistency.ps1
```

No extensions are installed in the current repository. If extensions are added,
review their updates separately with `specify extension update` after upgrading
the integration.
