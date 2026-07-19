# Spec Kit workflow

MOBAflow uses [GitHub Spec Kit](https://github.com/github/spec-kit) for
specification-driven feature development. The repository is initialized with
Spec Kit 0.13.0, the Codex skills integration, and PowerShell workflow scripts.

## Install the CLI

Install `uv`, then install the repository's pinned Spec Kit version:

```powershell
winget install --id astral-sh.uv -e
uv tool install specify-cli --from "git+https://github.com/github/spec-kit.git@v0.13.0"
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

Feature artifacts are stored under `specs/NNN-feature-name/`. The active feature
is recorded by Spec Kit in `.specify/feature.json`.

The project constitution in `.specify/memory/constitution.md` mirrors the
mandatory repository rules. It does not replace `AGENTS.md`,
`.github/copilot-instructions.md`, or the scoped files under
`.github/instructions/`; agents must continue loading those instructions first.

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

Spec Kit currently does not mark a simultaneous Codex and Copilot installation
as multi-install safe. Switch the repository integration instead of installing
both at once:

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
