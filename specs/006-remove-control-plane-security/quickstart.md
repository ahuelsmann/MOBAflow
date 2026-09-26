# Quickstart: Validate RF-19

Run all commands from the repository root. App start, device deployment and hardware actions are not
authorized by this feature; the manual checks below need a separate approval.

## Automated checks per slice

```text
dotnet build MOBApi/MOBApi.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~RuntimeCommandAdmission|FullyQualifiedName~RuntimeCommandsController|FullyQualifiedName~RuntimeHub"
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

Slice 3 additionally needs both UI hosts:

```text
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
   stored address. Discovery connects without any pairing prompt; solution and runtime state appear.
2. Enter the PC address manually on a second phone; it connects and stores the address as recent.
3. Drive a locomotive and switch a function from MOBAsmart; MOBAflow executes the commands.
4. Check the MOBAflow settings page in Light and Dark theme: no pairing or credential section remains.

## Results

Record the actual command results, skipped lanes and remaining manual checks here during delivery.
