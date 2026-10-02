# Quickstart: Validate RF-19

Run all commands from the repository root. App start, device deployment and hardware actions are not
authorized by this feature; the manual checks below need a separate approval.

## Automated checks per slice

```text
dotnet build MOBApi/MOBApi.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~RuntimeCommandValidator|FullyQualifiedName~RuntimeCommandAdmission|FullyQualifiedName~RuntimeCommandsController"
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

Slice 3 additionally runs the anonymous-access and process fixtures and needs both UI hosts:

```text
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~ClientsController|FullyQualifiedName~MobApiProcess|FullyQualifiedName~DiscoveryResponseParser"
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

A filtered run must execute tests; zero executed tests is not a pass.

## Structural checks

- Search the solution for `Authorize`, `ControlPlane`, `Pairing`, `Credential`, `CompatibilityRead`,
  `HostBootstrap`, `Fingerprint`, `ServerIdentity`, `ZXing` and `https` endpoints; only unrelated hits
  (for example ESP32 provisioning) may remain.
- `scripts/Test-LineEndings.ps1 -Staged`, `scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main`,
  secrets scan of every changed file, final diff review.
- GitHub CI including the analyzer baselines and SonarCloud with zero OPEN/CONFIRMED issues.

## Manual checks (after approval to start the apps)

1. Start MOBAflow with `AutoStartWebApp` on; start MOBAsmart on a phone in the same network with no
   stored address and switch on the MOBAflow connection (it is off by default). Discovery connects
   without any pairing prompt; solution and runtime state appear.
2. Restart MOBAsmart on the same phone; it reconnects to the stored address without another pairing or
   setup step.
3. Drive a locomotive and switch a function from MOBAsmart; MOBAflow executes the commands.
4. Check the MOBAflow settings page in Light and Dark theme: no pairing or credential section remains.
5. Check MOBAsmart in Light and Dark theme: the bottom tab bar has no pairing tab, and every remaining
   tab opens its intended page and shows the correct selection state.

## Results

Slice 1 (#167, #170) and slice 2 (#171) recorded their results in their pull requests.

Slice 3, local run on Windows 11 on 2026-09-30:

- MOBApi build: 0 warnings, 0 errors. MOBAflow FastDebug build: 0 warnings, 0 errors.
  MOBAsmart Android FastDebug build: 0 errors, 45 warnings (unchanged from `main`).
- Portable suite: 1,631 passed, 4 skipped (three live E2E tests need a manually started MOBApi,
  one photo test needs bundled photos). `MobApiProcessTests` ran all three real-process cases.
- Windows suite: 1,685 passed, 0 skipped.
- Analyzer gates reproduced locally for `net10.0`, `net10.0-windows10.0.22621.0` and the Android
  Release build (full rebuilds); all three baselines only shrink apart from new test fixtures
  and one CA1515 entry for the public `LocalMobApiClient`.
- All MOBApi request URIs are built by `MobApiEndpoint`; SonarCloud S5332 (plain HTTP) is expected
  there by the operating model and is accepted in SonarCloud rather than suppressed in code.
- Structural search: no authentication, pairing, credential, certificate or ZXing code remains;
  remaining hits are unrelated (multiplexer address pairing, display test fingerprints).
- Pending: manual checks 1-5 above, because starting the apps was not authorized.
