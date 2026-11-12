# Implementation Plan: Trains keep running when MOBAflow closes

**Source Issue**: #426

**Spec Kit**: Required

Input: [spec.md](spec.md).

## Callers of the shared runtime teardown

`ProjectRuntimeHost.RemoveCoreAsync` discards one runtime. Before this change it always set the known locomotives to
speed 0 first. Its callers and what they do after this change:

| Caller | Used for | Stop |
| --- | --- | --- |
| `LoadAsync` | New or opened solution, runtime start | kept |
| `RemoveAsync` | Project removed | kept |
| `ReplaceAsync` | Z21 assignment of a project changed | kept |
| `ReconcileZ21OwnersAsync` | Z21 ownership moves to an earlier project | kept |
| `ReleaseAsync` (new) | MOBAflow closes | none |
| `DisposeAsync` | Service provider disposed at the end of the process | none |

Before this change the shutdown used `LoadAsync([])` through `ISolutionSession.StopRuntimesAsync`.

## Approach

1. `IProjectRuntimeHost.ReleaseAsync` discards all runtimes without a stop and closes the unused Z21 connections
   (`Z21.DisconnectAsync` sends only LAN_LOGOFF). `RemoveCoreAsync` gets an explicit `stopLocomotives` argument, so
   every caller states its choice.
2. `ProjectRuntimeHost.DisposeAsync` discards without a stop. It runs only when the application's services are
   disposed, which covers a shutdown whose workflow failed or timed out.
3. `ISolutionSession.StopRuntimesAsync` becomes `ReleaseRuntimesAsync` and calls `ReleaseAsync`.
4. `MainWindowViewModel.PrepareForShutdownAsync` no longer calls `ConfirmStopTrainsAsync`. The save prompt, the
   observer release and the disconnect timeout stay.
5. Crash path: `App.OnUnhandledException` only logs and is not changed; no stop is added anywhere.
6. Documentation: `docs/wiki/MOBAFLOW-USER-GUIDE.md`, `docs/wiki/INSTALLATION.md`, `docs/ARCHITECTURE.md` and a
   supersession note in `specs/008-project-runtimes/spec.md`.

## Constitution Check

- No legacy path: the old shutdown stop and its session method are replaced, not kept beside the new one.
- Shared ViewModel logic stays free of WinUI types; the host interface stays in `Backend`.
- Safe startup is untouched; no new dependency.
- Tests use fake Z21s only. No app launch, hardware or track power.

## Validation Strategy

- `ProjectRuntimeHostTests`: release and host disposal with connected runtimes and driving locomotives send no
  locomotive, emergency-stop or track-power command and disconnect each Z21 once; the existing stop tests for load,
  remove, replace and manual disconnect stay.
- `MainWindowViewModelShutdownTests`: no dialog with a connected runtime, one release, no load/remove/replace; the
  new-solution confirmation test stays.
- FastDebug build of MOBAflow, portable and Windows test suites, analyzer baseline gates, line-ending and secrets
  checks; CI and Sonar on the draft PR.
