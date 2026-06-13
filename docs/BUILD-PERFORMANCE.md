# Build Performance

This page documents the fast local build path and the measurements used when
optimizing MOBAflow build times.

## Recommended Local Workflow

Use `FastDebug` for everyday edit/build cycles:

```bash
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

The `BuildMOBApiDependency=false` and `CopyMOBApiToOutput=false` properties are
intended for UI compile checks. They skip the REST API build dependency and the
post-build copy into the WinUI output folder. Do not use them when validating a
full app run that starts `MOBApi` from the MOBAflow output directory.

The VS Code debug configuration also targets `FastDebug` and uses the `build`
task as its `preLaunchTask`, so starting a debug session follows the same fast
compile-check path.

For cross-platform work, build the project closest to the change:

```bash
dotnet build Backend/Backend.csproj -c FastDebug
dotnet build MOBApi/MOBApi.csproj -c FastDebug
dotnet test Test/Test.csproj -c FastDebug
```

## Measuring Build Times

Create a binary log whenever a build feels unexpectedly slow. The log can be
opened with MSBuild Structured Log Viewer.

```bash
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -bl:artifacts/logs/mobaflow-fastdebug.binlog
dotnet build MOBApi/MOBApi.csproj -c FastDebug -bl:artifacts/logs/mobapi-fastdebug.binlog
dotnet test Test/Test.csproj -c FastDebug -bl:artifacts/logs/test-fastdebug.binlog
```

Capture these scenarios before and after build-system changes:

- Clean build after deleting `bin` and `obj`.
- Second build without source changes.
- Build after a `SharedUI` change.
- Build after a `MOBAflow`-only XAML or code-behind change.

## Configuration Notes

- `FastDebug` disables expensive checks that are not needed in normal edit/build
  loops, but Release still enforces warnings-as-errors and full documentation
  generation.
- `MOBAflow` JSON validation is incremental and remains enabled by default for
  Debug and Release. `FastDebug` skips it unless explicitly overridden with
  `/p:ValidateJsonConfiguration=true`.
- Azure Pipelines keep Release validation, SonarQube analysis, coverage, and
  test publication enabled.
