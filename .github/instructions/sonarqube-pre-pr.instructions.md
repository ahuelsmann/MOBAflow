---
description: 'Mandatory local and remote SonarQube quality gates for every pull request.'
applyTo: '**'
---

# SonarQube Pre-PR Gate

Every MOBAflow pull request must attempt local Sonar analysis before publication and pass
the remote SonarCloud analysis before review. Every PR starts as a draft. This prevents new
quality debt from being introduced while keeping historical `main` findings in their
dedicated RF work packages.

## Before creating the draft pull request

1. Verify that the Sonar CLI is authenticated:

   ```powershell
   sonar auth status
   ```

2. Fetch and identify the actual PR base. Do not assume the remote is named `origin`.
3. Run local analysis for the complete branch change set:

   ```powershell
   sonar analyze --base <remote>/main --force --format json -p ahuelsmann_MOBAflow2
   ```

4. If analysis succeeds, resolve every new actionable finding and repeat it until clean.
5. If the organization does not support local agentic analysis, record the exact capability
   error in the PR `Validation` section. This limitation permits only a draft PR so the
   remote SonarCloud analysis can run.

Do not create even a draft PR when Sonar authentication is unavailable. Never lower a
quality gate, suppress a valid finding, or exclude a changed file merely to make the
analysis pass.

## Draft pull request gate

1. Create the pull request as a draft.
2. Wait for the SonarCloud PR analysis to finish.
3. Require the SonarCloud check to be green.
4. Verify that the PR contains no unresolved findings:

   ```powershell
   sonar list issues -p ahuelsmann_MOBAflow2 --format toon --statuses OPEN,CONFIRMED --pull-request <number>
   ```

5. Require `total: 0` before marking the PR ready for review. If remote analysis finds an
   issue that local analysis missed, fix it on the same branch and repeat both the focused
   tests and Sonar checks.

Findings already present on `main` are not silently folded into an unrelated PR. Track and
prioritize them through the RF quality programme unless they block the current quality gate
or the changed code directly depends on them.
