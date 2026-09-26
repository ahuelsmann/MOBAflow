# Validation record

Date: 2026-09-25. Base: main at `87abe7ae58fd41bc5be690000e7e18852a79339d`.

## Automated checks

- `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter 'TestCategory!=LiveE2E'`
  passed: 1,729 tests passed, one skipped, zero failures.
- `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false`
  passed with zero warnings and zero errors.
- `dotnet build MOBAsmart/MOBAsmart.csproj -c FastDebug -f net10.0-android`
  passed; Android emits 45 XC0025 warnings for bindings with explicit Source.
- Changed-file secrets scan, line-ending validation, `git diff --check`, and
  `scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest` passed.

Coverage includes restart restoration, independent stores, exact ulong values,
inventory add/remove/re-add, no workflow execution from correction or restore,
reaching an event target again, stale activation isolation, feedback buffering with
original arrival times, coalesced writes, persistence barriers under continuing
feedback, corrupt input files, write failure/recovery, and mobile command routing.

## Review follow-up (2026-09-26)

- The cross-platform and Windows analyzer gates (`-p:MobaAnalyzerGate=true`, Release)
  match their refreshed baselines; production diagnostics were fixed and only test-project
  entries (CA2007, CA1812, CsWinRT1030) were added, following existing test conventions.
- Added regressions for an unreadable file not blocking runtime start/connect, reset-all
  recovery, and a cleared (NaN) feedback point count. The net10.0 suite excluding LiveE2E
  passed with 1,732 tests and one skipped; the Android Release publish matched the
  MOBAsmart analyzer baseline.

## Remaining acceptance

- GitHub CI and Sonar quality gate for the published PR commit.
- Manual desktop/mobile layout, editing, reset, restart, and real-layout acceptance.
  Neither application was launched as part of this change.
- LiveE2E tests require an authenticated running MOBApi host. The initial broad run
  encountered authorization failures in these tests; the final automated acceptance
  run excludes the LiveE2E category.

## Storage behavior

Each host registers its own application-data `inport-counters.json`. A missing file
starts new counters at zero. An unreadable file is reported and remains untouched;
counting stays disabled while runtime start and Z21 connection continue. Reset-all
explicitly replaces it with zero counts and resumes counting.
Only counts persist; filter/lap timestamps start fresh on app startup and after an
explicit correction. Removing an input deletes its saved count. Re-adding it starts
at zero. Asynchronous writes use atomic replacement; abrupt process termination can
interrupt a pending save. Normal desktop shutdown drains the writer; mobile also
requests a flush when sleeping and drains it during graceful destruction.
