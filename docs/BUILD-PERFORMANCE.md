# Build Performance

Practical guidance for faster local build and deploy loops in MOBAflow and MOBAsmart.

## MOBAflow (WinUI)

**Fast daily compile check:**

```bash
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore \
  /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

**Why the default build feels slow**

- WinUI XAML compilation and Windows App SDK (self-contained output is large).
- Default build also compiles MOBApi and copies it into the WinUI output folder.

**Tips**

- Use VS Code task `build` (FastDebug) for iteration.
- Use `dotnet watch run --project MOBAflow/MOBAflow.csproj -c FastDebug` while editing.
- Add Windows Defender exclusions for `bin/`, `obj/`, and `.nuget/`.
- Build a single `.csproj`, not the full solution.

## MOBAsmart (Android)

**AndroidX package pins**

- `Xamarin.AndroidX.Startup.StartupRuntime` is required for MAUI initialization providers.
- Older manual pins for `Tracing` and `Concurrent.Futures` were removed with MAUI 10.0.71; restore them only if a build fails with a missing AndroidX type.

**Fast daily build (recommended):**

```bash
dotnet restore MOBAsmart/MOBAsmart.csproj -f net10.0-android
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

Fast deploy is enabled by default for Debug and FastDebug (Visual Studio F5 and CLI).
Assemblies are pushed over adb on incremental deploys instead of rebuilding a full APK.

**Reliable deploy (opt-in, before release testing):**

```bash
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore \
  /p:MobaReliableDeploy=true -t:Run
```

Use reliable deploy when fast deploy behaves inconsistently on a device.

**Visual Studio**

- F5 with **Debug** or **FastDebug** uses fast deploy automatically.
- Project Properties > Android > Options should show fast deployment enabled for Debug.
- For a one-off reliable APK install: build with `/p:MobaReliableDeploy=true` or add
  `MobaReliableDeploy` to MSBuild properties in the VS build settings.

**Why Android deploy feels slow**

- First build compiles MAUI, Android resources, DEX, and packages an APK.
- ~125 function-symbol PNGs are processed at build time (now capped at 32x32).
- Sound workflow WAV files (~3.4 MB) are excluded from the Android package.
- USB install + app startup add time on top of compile time.

**Already enabled in `MOBAsmart/Build/AndroidDevPerformance.props`**

- `android-arm64` only (single device ABI)
- No AOT / no ProGuard in Debug and FastDebug
- Incremental manifest merge and native library build
- APK (not AAB) for local Debug/FastDebug
- Analyzers off in Debug/FastDebug

**Tips**

- Prefer **FastDebug** over **Debug** for UI iteration.
- Use VS Code tasks `build:mobasmart` (fast deploy default) or
  `build:mobasmart:reliable-deploy`.
- Keep the phone connected over USB 3; Wi-Fi deploy is slower.
- Close the Android emulator if you deploy to a physical device.
- Exclude `bin/`, `obj/`, and `.nuget/` from real-time antivirus scanning.

## Measuring locally

```bash
dotnet build <project>.csproj -bl:build.binlog
```

Open `build.binlog` with [MSBuild Structured Log Viewer](https://msbuildlog.com/).

## CI note

Azure DevOps PR validation builds MOBAflow + Test in Release with SonarQube and
coverage. That pipeline is intentionally slower than local FastDebug iteration.
