# CI/CD Guidelines and Examples

This document provides CI best practices and sample pipelines for both GitHub Actions and Azure DevOps. Use these as templates and adapt to your environment.

## Principles
- Run unit tests and static checks on every PR.
- Keep CI fast: run only required projects per job (SharedUI, Backend, Test, WebApp).\
- Use platform-specific runners when necessary (Windows for WinUI).\
- Protect secrets via CI secret storage (GitHub Secrets / Azure Key Vault / Azure Pipelines Library).
- Fail fast: stop pipeline on build or test failures.

---

## GitHub Actions (recommended for GitHub-hosted repo)

Place workflow file at `.github/workflows/ci.yml`.

Key points:
- Use `windows-latest` to build .NET 10 projects that target Windows (WinUI).\
- Restore, build and run tests.\
- Cache NuGet packages for faster runs.

Sample workflow:

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-$(date +%Y%m%d)-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: |
            nuget-${{ runner.os }}-

      - name: Restore
        run: dotnet restore ./Moba.slnx

      - name: Build
        run: dotnet build ./Moba.slnx -c Release --no-restore

      - name: Run tests
        run: dotnet test ./Test/Test.csproj -c Release --no-build --verbosity normal

      - name: Publish test results (optional)
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: Test/TestResults || ./Test/TestResults
```

Notes:
- If your repo stays in Azure DevOps for now, you can still keep this workflow and enable it after migration.
- To run platform-specific builds (Android MAUI), add separate jobs using macos or ubuntu runners and install Android SDK/NDK (requires more setup).

---

## Azure DevOps (sample YAML)

Place pipeline YAML in `azure-pipelines.yml` at repo root or define pipeline in Azure DevOps.

Sample:

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- task: NuGetCommand@2
  inputs:
    restoreSolution: 'Moba.slnx'

- script: dotnet build Moba.slnx -c Release --no-restore
  displayName: Build solution

- script: dotnet test Test/Test.csproj -c Release --no-build --verbosity normal
  displayName: Run tests
```

---

## Additional Checks to Add
- markdownlint or Super-Linter for docs quality
- link-checker for internal doc links
- code analysis / static analyzers (SonarCloud, Roslyn analyzers)
- packaging and release jobs (optional)

---

## Secrets & Keys
- Use GitHub Secrets or Azure DevOps variable groups for keys
- Prefer managed identities / Key Vault for production secrets

---

## Summary
- Start with a simple build+test CI and expand later.
- Use Windows runner for WinUI; keep MAUI builds separate.
- Add linting and coverage as next steps.