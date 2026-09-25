# GitHub pipeline review
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Observed**: 2026-09-25; main 2b65379fb4f4c6cb23394ea9d44467c6801f027f.
Read-only configuration/API/log review. No settings changed, jobs retried or cancelled.

## Workflow matrix
| Workflow | Trigger | Jobs / runner | Timeout | Permissions |
| --- | --- | --- | --- | --- |
| Quality | main push, every PR, manual | Instructions Linux; Desktop Windows; Display Linux; Android Windows; Mutation Linux | 5/60/45/60/20 min | contents:read |
| Spec Kit governance | issue opened/edited/reopened; filtered PR opened/synchronize/reopened | Issue and plan validation, Linux | 5/10 min | global issues:write plus contents:read |
| Deploy MOBAflow website | main push for docs/** or own YAML; manual | Pages, Linux | no explicit limit | contents:read, pages:write, id-token:write |
| Release Studio | manual signed SemVer tag | Windows build/test/package; draft release only | 60 min | contents:write |
| Stale work visibility | Monday 04:17 UTC; manual | mark after 60 days, no auto-close | no explicit limit | contents:read, issues:write, pull-requests:write |

Additional hosted checks: CodeQL default setup (Actions, C/C++, C#, JS/TS, Python),
SonarCloud external analysis, Dependabot, dependency submission, Copilot and Pages system workflow.
Source: [.github/workflows](../../.github/workflows).
Quality's full matrix currently runs for documentation, shared .NET, WinUI and Android changes alike.
Governance runs only for its configured PR paths; a successful issues-event run is not PR artifact validation.

## Observed runs
- [PR154 Quality](https://github.com/ahuelsmann/MOBAflow/actions/runs/36130988570) succeeded on
  1645347c85f2e52cde9fcca442c0af9f5bd91148; PR154 merged at 11:57:04 UTC.
- [Referenced Android job](https://github.com/ahuelsmann/MOBAflow/actions/runs/36130988570/job/108057952187?pr=154):
  11:43:58-11:55:37 UTC, 11m39s total. Workload restore 4m59s, dependency restore 4s,
  Release AAB publish 5m14s. Analyzer baseline, bundle validation and archival passed.
  NuGet cache hit; this was progressing work, not a hang.
- [Main Quality](https://github.com/ahuelsmann/MOBAflow/actions/runs/36132189420):
  at the initial observation four jobs passed and Android publish was running.
  Refreshed later in this task: all five jobs completed successfully, including Android.
- Main CodeQL passed, but [SonarCloud main](https://sonarcloud.io/dashboard?id=ahuelsmann_MOBAflow2&branch=main)
  failed: New Code Reliability C and Security C, required A. PR154's green Sonar does not validate main.
- Prior main Quality36131518246 cancelled around the newer push, consistent with configured concurrency.
  Cancellation is not a successful validation.

## Findings and concrete follow-up tasks
These are separately reviewable follow-ups, not authorization to weaken checks or change administration.
Issue145 tracks their disposition until assigned to dedicated work.

- [ ] CI-01 (high): main has no enforced merge gates. API evidence: protected:false, branch protection404
  “Branch not protected”, repository rulesets[], effective main rules[]. Token admin:true rules out a
  permission blind spot. Agree required check names, review/bypass policy and filtered-workflow behavior,
  then configure branch protection in a separately authorized task.
  Done: a disposable failing PR cannot merge; a fully passing current commit can.
- [ ] CI-02 (medium): remove global issues:write from spec-kit-governance.yml; retain it only on the
  issue job, which already has job-level rights. Done: issue validation still works and PR plan job is read-only.
- [ ] CI-03 (low): add explicit timeouts to Pages and stale jobs based on observed runtime.
  Done: both jobs have bounded runtime and normal runs pass.
- [ ] CI-04 (medium): decide reproducible SDK/workload pins. global.json specifies SDK10.0.302 with
  rollForward latestFeature; actual PR154 used SDK/workload10.0.401, no workloadVersion.
  --skip-manifest-update is not a workload pin. Done: clean runner uses the agreed versions and
  Windows/Android Release tests, analyzers and bundle checks pass.
- [ ] CI-05 (medium): evaluate scoped docs-only CI after CI-01. Preserve required-check names/results;
  do not introduce permanently pending filtered checks. Done: docs, shared code, WinUI and Android
  sample diffs receive the intended jobs without bypassing product gates.
- [ ] CI-06 (existing main): triage the two failed Sonar rating conditions against current main findings.
  Keep product fixes out of repository-setup scope unless they block the current PR gate.
  Done: current main Sonar passes with evidence and no suppressed valid findings.

## Positive controls and limits
Actions in repository YAML are pinned to full commit SHAs; Dependabot checks them weekly.
Repo default token is read-only and token-based PR approval is disabled.
Repository-wide action SHA enforcement is off; YAML pinning is the observed protection.
Android NuGet cache points to .nuget/packages, consistent with NuGet.Config.
Cache restore worked; most time was workload installation and publish.
Quality jobs run in parallel, have explicit timeouts and cancel superseded runs.
This review does not claim a production release, live UI/hardware validation or green current-commit PR checks.

## Primary guidance
- [Workflow syntax, permissions and timeouts](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax)
- [Required checks and filters](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/troubleshooting-required-status-checks)
- [Workload sets](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-sets)
