# Validation guide: MOBAsmart fixed STOP

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304

## Automated and compile checks

From the dedicated task-worktree root:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~TrainControlViewModelPerformanceTests|FullyQualifiedName~TrainControlHostSelectionTests|FullyQualifiedName~MobileRuntimeCoordinatorTests|FullyQualifiedName~RuntimeCommandGatewayRoutingTests|FullyQualifiedName~MauiViewModelControlTabTests|FullyQualifiedName~RuntimeCommandPathArchitectureTests"
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

Confirm intended discovery/execution, not zero tests. ControlPage stays included as active MauiXaml. FastDebug does not validate Release/AAB.

## Android manual gate

The user explicitly authorized the UI/UX expert to start an isolated Android emulator and MOBAsmart for 200% system text, Light/Dark and TalkBack. Use synthetic locomotive identities, never live track power. Record app SHA, device/OS, actual available tab dimensions and Android text scale. The UI/UX expert alone operates the emulator.

1. Test Light/Dark x100/150/200% system text in narrow portrait (360-unit width baseline), including host navigation chrome. Record both full viewport and available Control content height; include a short-height case with 480 units of available Control content.
2. At 0/25/50/75/100% of list scrolling, confirm bounded name/address/scoped STOP without overlap or horizontal scrolling. Within the function section at least one complete row must fit; speed/direction controls may occupy the top of the shared scroller.
3. Use similarly prefixed names "Harbor Pilot North Platform Service" and "Harbor Pilot South Platform Service", plus a very long unbroken name, with distinct DCC addresses. Confirm wrapping to two visual lines then visible ellipsis, full accessible name, and independently readable DCC/STOP. Do not truncate the stored name.
4. Reach F0-F31, speed, direction and All off. At F31 switch A-to-B/B-to-A, verify current target and activate STOP once through an isolated command target.
5. Cover no/invalid address, disconnected/remote availability and changing status; confirm existing disabled behavior.
6. Check >=44-unit targets and actual accessible names/meaning/On-Off with Android accessibility navigation. Do not infer exactly-once transport from command activation.
7. Record observations/screenshots/failures. Browser/compile evidence is not Android acceptance; failures block product acceptance.

## Delivery record

Implementation will record exact command results, convergence and pending manual/CI gates here. No app, emulator, debugger, device or hardware start is implied by the commands above.

- Before product edits: planned filtered portable test run executed 39 tests, all passed, 0 skipped; Test/TestResults/issue304-baseline.trx. Four newly added cases cover both directions from a nonzero sent speed after selecting B, plus two invalid addresses. These assertions verify command target/state, not network send cardinality or UI activation.
- Android restore passed with the installed SDK/workloads; no portable workload-resolver workaround used.
- The selected STOP and function surfaces use minimum 48-unit targets, exceeding the repository minimum. Actual measured areas and accessibility activation remain pending device checks. Include a 320-unit-width stress case in addition to the 360-unit baseline; record any limit.

## Implemented validation results

- After the product changes, the same filtered net10.0 run passed 39 tests, 0 failures, 0 skips; Test/TestResults/issue304-final.trx. All six named fixtures executed, including the four new STOP cases. No workload-resolver workaround was needed.
- Android restore passed. The first parallel FastDebug build failed with CS2012 on a locked Domain/obj/FastDebug/net10.0/Domain.dll. Repeating the documented build with `-m:1 -nr:false` passed in 1 minute 58 seconds: 0 errors, 45 warnings (including XC0025 for existing explicit-Source bindings). No warning gate or project/package setting was weakened. The real net10.0-android XAML compiled; no app was started.
- Sonar auth status verified Connected to SonarCloud before publication. Local Sonar code analysis/hooks were not run.
- Architekt completed plan and amended-plan reviews without a concrete violation. Designer/UIUX statically reviewed the actual mobile diff; the Designer's unsupported Engines-tab promise was removed from the guide. Static review is not Android visual acceptance.
- The selected-locomotive command implementation and local/remote gateway remain unchanged. The nonzero-speed regression waits for its motion setup via a bounded TaskCompletionSource; its assertions cover target, zero speed, direction and function preservation. It does not claim a single transport telegram or UI gesture/device execution.

Pending gates: actual Android Light/Dark/system-text/short-height/long-name/F31 checks, measured touch areas and TalkBack activation; current-head CI/Sonar after draft publication. The browser pilot provides design context only. The newly authorized emulator check is in progress; no real layout or hardware is used.

## Isolated emulator build and fixtures

- Reliable x64 build passed with 0 errors and 45 warnings in 6 minutes 10 seconds: restore with `-r android-x64 -p:MobaReliableDeploy=true`, then the FastDebug command above with `-p:RuntimeIdentifier=android-x64 -p:MobaReliableDeploy=true -m:1 -nr:false`. No project settings changed.
- Signed APK: `MOBAsmart/bin/FastDebug/net10.0-android/android-x64/com.mobaflow.mobasmart-Signed.apk`, 57,363,252 bytes; SHA256 `A8B72DB7B60BA7F4F9F912F7DAFA88029D8E5099D8E422CA02BF73249A15620A`. Built from base `6888b0d9239bee3b623fbca0f6ae9d71fe7ec185` plus this product diff before commit.
- [Synthetic solution](fixtures/mobile-solution.json) and [cache metadata](fixtures/mobile-solution-meta.json) use the existing MobileSolutionStore cache format. They contain four artificial locomotives, 32 function labels and loopback-only Z21 configuration. With the app stopped, copy both into its existing AppDataDirectory/mobile-cache directory in the fresh isolated emulator. No product fake mode was added. Keep airplane mode and verified network disconnection; offline STOP may correctly remain disabled.
- UI/UX reports Android 15/API35, emulator-5584, 360x640 dp (720x1280 pixels), font_scale=2.0, airplane mode on, Wi-Fi/data off and no IP route before app launch. Actual fixture loading and UI acceptance are not yet confirmed in this record.

## Repository validation and convergence

- Internal documentation links passed: 477 links. Spec Kit PullRequest governance passed. Changed-file secrets, staged line-ending checks and diff whitespace review passed (final staging rechecked before commit).
- Convergence inspected all 8 functional requirements, 4 success criteria, 7 acceptance scenarios, 15 existing tasks, 7 plan decision groups and 6 constitution principles. No additional product-code gap was found. Phase 7 tasks T016/T017 retain the partially completed Android and publication/CI evidence; this is not a full product acceptance claim.

## Historical publication evidence: 2026-10-15, 21:48 CEST

The following record was originally committed as `18aa971d8d1e250ea21c0bb52314a5477ef69cfe` at 2026-10-15T21:48:41+02:00. It is restored here on 2026-10-22 from the preserved original worktree. These are historical observations, not current-head validation results.

- Implementation commit: `a27664b2910a2ea4fa631ff17a268cd83728c79d`; base `6888b0d9239bee3b623fbca0f6ae9d71fe7ec185`. The explicit task-branch push succeeded; no main push was performed.
- [Draft PR #308](https://github.com/ahuelsmann/MOBAflow/pull/308) was created, linked to #304 and attached to the implementation chat. Designer and Architekt completed read-only final assessments of that implementation commit without concrete findings; the earlier independent Pruefer assessment covered the unchanged product diff. These assessments do not certify a later comparison or replace its required independent review.
- At that observation, the initial GitHub checks were queued/running. Sonar PR status was `NONE` with no conditions; the issues query returned zero OPEN/CONFIRMED. This was not an acceptance result because analysis of that head had not completed. Installed Sonar CLI 1.4.0 lacked `quality-gate status`; the read-only `sonar api get /api/qualitygates/project_status` endpoint supplied the status. No local code analysis or integration hooks were run.
- The PR was kept draft. Those local results did not authorize a Ready or merge action.

### Bounded documentation recovery: 2026-10-22

Only this historical publication record and the completed T014 publication checkbox are recovered in the new isolated technical-handoff context, starting at PR head `321d6d26862ccae72962234bdbcd666e63043309`. The original local commits `18aa971d` and `c1d0d8b6`, their worktree and ignored acceptance artifacts remain preserved. No historical main merge is imported here.

The earlier implementation, build and emulator entries above retain their original scope and do not describe a currently running acceptance session or authorize restarting one. T006/T010/T016 remain open for practical acceptance; T015/T017 remain open for current-comparison CI/Sonar and delivery evidence. This documentation recovery makes no new build, test, CI/Sonar or device-acceptance claim. Main integration and technical validation require the next centrally allocated serial phase; this phase performs no remote publication, review, Ready or merge action.

## Technical integration and local validation: 2026-10-22

The separately allocated technical phase merged main `0ed1bbcf8a0b67a46ec4cd05920f171b1c2d85c4` once, without conflicts, as `b2b4887aeacbe2385aa9de60f22dcde4c0f3d164`. Current runtime factories, Android discovery registration, MAUI 10.0.120, Test SDK 18.11.0 and the #383 metrics tests were retained. The strict portable gate identified two additional CA2007 diagnostics in the new STOP test. Adding `ConfigureAwait(false)` to its bounded motion wait and command await fixes those diagnostics without changing the platform-neutral assertions, product behavior or analyzer baselines. The tested file has Git blob `3137e3f36677e4cc2589855bb7e8d313ee425f7f`.

All .NET commands ran serially, using SDK/workload set 10.0.402. No app, emulator, device or hardware was started.

```powershell
dotnet restore Test/Test.csproj -p:TargetFrameworks=net10.0 -p:IncludeMobaSmartTests=false --disable-parallel -m:1 -nr:false
dotnet build Test/Test.csproj -c Release -p:TargetFrameworks=net10.0 -f net10.0 --no-restore -m:1 -nr:false -t:Rebuild -p:IncludeMobaSmartTests=false -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false
# After the two-await correction, repeat the build without -t:Rebuild.
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release/net10.0 -BaselinePath quality/analyzer-baseline.json
dotnet test Test/Test.csproj -c Release -p:TargetFrameworks=net10.0 -f net10.0 --no-build --no-restore -m:1 -nr:false -p:IncludeMobaSmartTests=false --logger "trx;LogFileName=issue304-current-main-portable.trx"
dotnet restore MOBAsmart/MOBAsmart.csproj --property:Configuration=Release --force-evaluate -m:1 -nr:false
dotnet publish MOBAsmart/MOBAsmart.csproj --framework net10.0-android --configuration Release --no-restore -m:1 -nr:false -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release -BaselinePath quality/analyzer-baseline.android.json
```

- Portable Release compilation passed. The full analyzer graph matched the unchanged baseline: **3,208 diagnostics in 1,129 groups**. Its SARIF files were then preserved under `artifacts/issue304-integration-20261022/portable-analyzer-Release`, leaving a separate clean output root for the Android graph.
- Full portable Release tests: **3,034 executed and passed, zero failures**. The TRX discovers 3,035 cases; the additional explicitly marked real-display test `SustainedRefresh_MeetsAcceptanceThresholds` is `NotExecuted`. All six feature fixtures ran (39/39), including both current-target/nonzero-speed directions and both invalid-address cases. The three current `RuntimeCommandMetricsTests` also passed. TRX: `Test/TestResults/issue304-current-main-portable.trx`.
- Android Release restore and publish passed. Evaluated properties are `AnyCPU`, `AnyCPU`, `android-arm64;android-x64` and `aab`. Its complete separate analyzer graph matched the unchanged Android baseline: **1,500 diagnostics in 549 groups**.
- Both publish-directory bundles passed `scripts/Test-AndroidAppBundle.ps1 -BundlePath <bundle>`: required manifest/resources/Dex entries and exactly `arm64-v8a`/`x86_64` native ABIs. No APK was produced in that directory. `com.mobaflow.mobasmart-Signed.aab`: 48,111,874 bytes, SHA256 `ECA33E5CC51BD0909E8E7718B1E25D82E0E6A6E6760E74571788D08FB1E724CF`; unsigned `com.mobaflow.mobasmart.aab`: 47,910,499 bytes, SHA256 `F1CA94B94E04E121F51A60E4F141930C4E3E2E47A5EF797A439BA1D551A74475`.
- Local Windows compilation/tests were not run: the Windows test graph references the WinUI project, which copies `MOBAflow/solution.json` as content. The explicit user-data boundary forbids reading, hashing, copying or staging that file; neither the project graph nor its content-copy behavior was bypassed. Regular Windows CI evidence remains required.

All build/test processes had ended before the subsequent, centrally qualified documentation-only main update `f2f388d3213b442a13169dd41c554ee1dbf1a3a6` was merged as `2bdd0073df10dd8947077386b8446701020bd9b2`. That incoming change affects only `plans/318-explicit-dependencies.md`; product, tests, packages, build settings and baselines are unchanged. The compilation/test inputs above therefore remain unchanged; no repeat build is claimed for the later documentation snapshot.

Documentation assessment: the merged `docs/wiki/MOBASMART-USER-GUIDE.md` describes the actual fixed header and selected-locomotive STOP. Runtime/gateway ownership is unchanged, so no further architecture-document correction is required for this delta. Integration owners must confirm this assessment against the eventual integrated main.

Practical acceptance remains **OPEN** under [#365](https://github.com/ahuelsmann/MOBAflow/issues/365): Android Light/Dark, 200% text, short viewport, long names, A-to-B-to-A at F31 and TalkBack are not established by these local gates. T006/T010/T016 and current-comparison CI/Sonar T015/T017 remain open. The PR remains Draft; this record does not claim Ready, independent review or integration.
