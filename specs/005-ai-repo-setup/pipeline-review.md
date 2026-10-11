# GitHub pipeline review
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Observed**: 2026-09-25; main 2b65379fb4f4c6cb23394ea9d44467c6801f027f.
Read-only configuration/API/log review. No settings changed, jobs retried or cancelled.

## Workflow matrix
| Workflow | Trigger | Jobs / runner | Timeout | Permissions |
| --- | --- | --- | --- | --- |
| Quality | main push, every PR, manual | Instructions Linux; Desktop Windows; Display Linux; Android Windows; Mutation Linux | 5/60/45/60/20 min | contents:read |
| Spec Kit governance | issue opened/edited/reopened; filtered PR opened/synchronize/reopened | Issue and plan validation, Linux | 5/10 min | global contents:read; issues:write on the issue job |
| Deploy MOBAflow website | main push for docs/** or own YAML; manual | Pages, Linux | 20 min | contents:read, pages:write, id-token:write |
| Release Studio | manual signed SemVer tag | Windows build/test/package; draft release only | 60 min | contents:write |
| Stale work visibility | Monday 04:17 UTC; manual | mark after 60 days, no auto-close | 10 min | contents:read, issues:write, pull-requests:write |

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
Owners decided on 2026-10-09: CI-02 to CI-04 move to #209, CI-05 and CI-06 to #197.

- [x] CI-01 (high): done without a change from this review. Earlier on 2026-09-25 the API showed
  protected:false, branch protection 404 "Branch not protected", repository rulesets [] and effective main rules [];
  token admin:true ruled out a permission blind spot. Later that day (18:03) the owner created the active
  repository ruleset "main": pull request required, squash merge only, resolved review threads, six required
  checks including SonarCloud, no force-push and no deletion. The observation was correct at its time and is
  historical. Whether the required check names still match after #197 is checked in #209.
- [x] CI-02 (medium, #209): remove global issues:write from spec-kit-governance.yml; retain it only on the
  issue job, which already has job-level rights. Done: issue validation still works and PR plan job is read-only.
  Done by #198 (2026-10-08): the workflow grants only contents: read, and issues: write stays on the issue job.
  The issue validation run for #209 itself passed on 2026-10-09.
- [x] CI-03 (low, #209): add explicit timeouts to Pages and stale jobs based on observed runtime.
  Done: both jobs have bounded runtime and normal runs pass. Pages has timeout-minutes: 20, above
  deploy-pages' own 10-minute timeout, and stale has timeout-minutes: 10; observed runtimes on main
  were under 2 minutes for Pages and under 15 seconds for the weekly stale run.
- [x] CI-04 (medium, #209): decide reproducible SDK/workload pins. global.json specifies SDK10.0.302 with
  rollForward latestFeature; actual PR154 used SDK/workload10.0.401, no workloadVersion.
  --skip-manifest-update is not a workload pin. Done: clean runner uses the agreed versions and
  Windows/Android Release tests, analyzers and bundle checks pass.
  Owner decided on 2026-10-10: pin the feature band. global.json names SDK 10.0.401 with rollForward
  latestPatch, so patches of the 10.0.4xx band apply and a new band is a reviewed change. The Android job
  installs workload set 10.0.401.1 with `dotnet workload install maui-android --version`; a workloadVersion
  in global.json would fail every build on machines without that set. The Windows and Linux jobs run on the
  pinned band. The Android job is disabled since #195, so its pin is first exercised when it is re-enabled.
- [x] CI-05 (medium, #197 CI-3): evaluate scoped docs-only CI. Preserve required-check names/results;
  do not introduce permanently pending filtered checks. Done: docs, shared code, WinUI and Android
  sample diffs receive the intended jobs without bypassing product gates.
  Completed in #197: #202 added dependency-aware selection and 35 passing regression checks;
  documentation-only #221 reported skipped jobs without leaving required checks pending.
- [x] CI-06 (existing main, #197 CI-1): triage the two failed Sonar rating conditions against current main findings.
  Keep product fixes out of repository-setup scope unless they block the current PR gate.
  Done: current main Sonar passes with evidence and no suppressed valid findings.
  Completed in #197: main 934c9dcc has a green Sonar gate, Reliability/Security A,
  zero new bugs and zero new vulnerabilities; the analysis revision matches that commit.

## Follow-up verification after #197 (2026-10-11, #209)

The [active main ruleset](https://github.com/ahuelsmann/MOBAflow/rules/24007434)
still requires these six checks. The API comparison on main
934c9dcc8839e0789085b45dbc0dd84a206733d5 matched both the exact names and
GitHub App IDs against the reported check runs:

| Required context | App ID | Main result |
| --- | --- | --- |
| Repository instruction consistency | 15368 | success |
| Windows desktop build, tests, and coverage | 15368 | success |
| Display protocol host, parser, and firmware | 15368 | success |
| Android Release AAB | 15368 | skipped by the existing distribution policy |
| Domain mutation testing | 15368 | success |
| SonarCloud Code Analysis | 12526 | success |

Evidence: [main Quality](https://github.com/ahuelsmann/MOBAflow/actions/runs/38150639327)
and the Sonar check on the same commit. No check was missing or pending, and no
ruleset change was needed. Common mutation testing and CodeQL also passed; they
are not additional contexts in this ruleset.

CI-02 is implemented by #198 and CI-03 by #216. The bounded Pages workflow
passed on this main revision in
[run 38150639287](https://github.com/ahuelsmann/MOBAflow/actions/runs/38150639287).
The next scheduled stale run remains the runtime confirmation of its new bound;
the earlier successful runs predate #216. It was not triggered manually because
it can label and comment on unrelated issues and pull requests.

CI-04 retains the owner's #218 feature-band decision and workload set 10.0.401.1.
Quality now offers manual `validate_android=true` validation on a clean Windows
runner, including a workload-version check, Release AAB publish, analyzer baseline
and bundle validation. Regular PR/main runs keep Android skipped. Until that
manual run succeeds, the Android pin is configured but its execution is not
claimed as verified.

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
