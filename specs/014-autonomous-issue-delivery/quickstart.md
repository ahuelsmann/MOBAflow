# Planning validation and later operational entry

Source: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). This is a validation guide, not a launch/provisioning instruction.

## Inspect this planning package

Prerequisite: use this issue's dedicated worktree/branch and own successful standalone secrets scans before EVERY file/script/input read, including implicit script inputs. A failed/interrupted scan is not permission to proceed. Scan current changed documents and script dependencies again after edits.

Feature resolution uses the supported process-local settings below to preserve the assignment's documents-only scope; it does not write .specify/feature.json or change shared configuration:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = 'specs/014-autonomous-issue-delivery'
$env:SPECIFY_FEATURE_NO_PERSIST = '1'
$env:SPECIFY_FEATURE = 'codex/issue-337-cloud-planning'
./.specify/scripts/powershell/check-prerequisites.ps1 -Json -PathsOnly
./.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireSpec -RequireTasks -IncludeTasks
```

Expected: FEATURE_DIR resolves here; spec/plan/tasks and design documents exist. No missing artifact or functional clarification marker; every FR/SC/C input maps in traceability.md. All T tasks remain unchecked. No extensions.yml/hooks at the planning base. Specify, clarify, plan, tasks and read-only analyze are the only workflow phases applied.

For text normalization, after separate successful scans of the script, .gitattributes and all files it will read:

```powershell
./scripts/Test-LineEndings.ps1 -Path specs/014-autonomous-issue-delivery -Fix
./scripts/Test-LineEndings.ps1 -Path specs/014-autonomous-issue-delivery
```

After staging the exact changed files (and after fresh scans), run the staged check and diff check. Staged validation also reads the index bytes, so scan those exact changed file versions before use:

```powershell
./scripts/Test-LineEndings.ps1 -Staged
git diff --cached --check
```

Run Test-SpecKitGovernance.ps1 in PullRequest mode with this feature's explicit ChangedFiles; scan all script inputs, including the issue templates it reads. For focused links, after staging and fresh input scans, Test-DocumentationLinks.ps1 -Root specs/014-autonomous-issue-delivery checks tracked documents within this folder. Review public source links against research; do not run a repository-wide read without its own full scan coverage.

Expected: positive structural/link/text checks pass on actual changed content, no unrelated tracked changes. No .NET build/test, negative gate fixture, image canary, app/device start, new advisory/review run or provider probe. Local Sonar is secrets/result access only; actual code analysis belongs to PR CI.

## Planning PR

Create Draft only, attach it, retain exact commit/base and current checks. Current-commit Sonar success plus zero OPEN/CONFIRMED issues is required before Senior Ready. Missing/NONE is not pass. Report legitimate documentation-only skips separately from executed CI. Do not close #337: planning is one stage of the still-open issue.

## Later execution entry (not authorized by this guide)

1. Obtain verified planning integration/accepted handover, recorded D1-D6 decisions as applicable and a current Senior allocation.
2. Qualify chosen account/model/scanner/identity/endpoint boundary and reproducible images. No worker starts while required evidence is unknown.
3. Execute [qualification](qualification.md) under its separate grants; begin with isolated fakes, then scoped authorized provider tests.
4. Accept the PC/cutover/acceptance maps before one specifically approved PC-off pilot.
5. Carry exact evidence through existing Claude review and Senior verification; retain contexts until accepted closure.

No implementation command is supplied because source, provider and environment have not been chosen or provisioned. Future tasks reference proposed paths and cannot be executed merely because this guide exists.
