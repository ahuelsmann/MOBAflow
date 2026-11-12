---
description: 'Local secrets scanning and GitHub-only SonarCloud code analysis for pull requests.'
applyTo: '**'
---

# SonarCloud PR Gate

Sonar code analysis runs only through the GitHub PR pipeline. Do not run local
`sonar analyze` code analysis, `sonar analyze agentic`, or Vortex analysis hooks, and
do not install or enable those hooks. Repeating that analysis locally adds an
unnecessary prerequisite to the existing CI gate.

Every PR starts as a draft and must pass SonarCloud before review. Local builds,
tests, analyzer baseline checks, and deterministic secrets scans remain required
as specified in AGENTS.md; cloud sessions without a local scanner follow
[Cloud sessions without a local scanner](#cloud-sessions-without-a-local-scanner). The Sonar CLI may scan secrets and read remote results;
neither operation is a local Sonar code analysis.

## Balanced secrets scan

Scan a file before reading it when its name, location, or context indicates that it may
contain credentials, tokens, private keys, certificates, connection strings, or deployment
secrets. Ordinary source files, tests, Markdown documentation, schemas, and templates do
not require an individual pre-read scan unless such an indication exists.

Before every commit or pull request, run `sonar analyze secrets <path>` for each changed
file. A positive finding is a hard stop: do not read, commit, or publish the file; rotate
the exposed credential at its source of truth and remove it from the repository.

### Cloud sessions without a local scanner

Cloud sessions, such as the Executive Assistant, have no Sonar CLI and no local scanner.
For them the local scan is not required; the two protections are handled separately:

- **Pre-read:** do not open, print or copy files whose name, location or context indicates
  secrets (for example `.env`, `*.pfx`, `*.pem`, publish profiles, `appsettings.*.json` with
  credentials, `secrets.json`). If a task needs such a file, stop and hand it to a local session.
- **Pre-commit:** GitHub Secret Scanning and Push Protection check every push. A push blocked
  by Push Protection is a hard stop: do not bypass it; remove the value and report the finding
  without exposing it.
- **Record:** state in the PR that no local secrets scan ran, and list any changed file that is
  not ordinary source, test, documentation or schema content.

Local sessions keep the local pre-read and pre-commit scans above.

## Before creating the draft pull request

1. Confirm that every changed file passed the deterministic secrets scan, or, in a cloud
   session without a local scanner, that the PR records the missing scan.
2. Fetch and identify the actual PR base. Do not assume the remote is named `origin`.
3. Run the relevant local validation required by AGENTS.md and document its results.

Vortex availability and local code-analysis permissions are not prerequisites for
publication or review. Do not retry a Vortex 403 or request a subscription for this workflow.

In a local session, do not create even a draft PR when Sonar authentication is unavailable.
A cloud session without the Sonar CLI may create the draft and reads the SonarCloud check
and its PR comment through the GitHub REST API instead. Never lower a
quality gate, suppress a valid finding, or exclude a changed file merely to make the
analysis pass.

## Draft pull request gate

1. Create the pull request as a draft.
2. Wait for the SonarCloud PR analysis of the current PR commit to finish.
3. Require the SonarCloud check to be green.
4. Verify that the PR contains no unresolved findings:

   ```powershell
   sonar list issues -p ahuelsmann_MOBAflow2 --format toon --statuses OPEN,CONFIRMED --pull-request <number>
   ```

   A cloud session without the Sonar CLI reads the SonarCloud check of the current commit
   and the SonarCloud PR comment ("0 New issues") through the GitHub REST API instead.

5. Require `total: 0` before marking the PR ready for review. Fix new actionable findings
   on the same branch, run relevant local tests, and push the fix so GitHub runs the
   SonarCloud analysis again. Verify the new commit's check and issue count before review.

Findings already present on `main` are not silently folded into an unrelated PR. Track and
prioritize them through the RF quality programme unless they block the current quality gate
or the changed code directly depends on them.
