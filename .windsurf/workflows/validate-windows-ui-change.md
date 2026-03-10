---
description: Validate a Windows or WinUI-focused MOBAflow change with project-specific checks.
---

# Validate Windows UI Change

Use this workflow for WinUI, XAML, ViewModel, desktop process-launch, or `MOBApi` integration changes that are driven from the desktop app.

1. Determine whether the change touches:
   - XAML layout or resources
   - WinUI code-behind
   - ViewModels in `SharedUI`
   - desktop startup, DI, or `MOBApi` launch behavior
2. Prefer targeted commands such as:
   - `dotnet restore MOBAflow/MOBAflow.csproj`
   - `dotnet build MOBAflow/MOBAflow.csproj`
   - `dotnet restore MOBApi/MOBApi.csproj`
   - `dotnet build MOBApi/MOBApi.csproj`
   - `dotnet test Test/Test.csproj`
3. If XAML was changed, explicitly check for:
   - `ThemeResource` usage instead of hardcoded colors
   - consistency with the surrounding page structure
   - whether the impacted page should or should not use splitter-column behavior
4. If EventBus, UI threading, or startup DI changed, explicitly check whether the existing UI-dispatch EventBus pattern is still respected.
5. If the change affects `MOBApi` startup or integration from `MOBAflow`, mention both build validation and a short manual smoke test.
6. If behavior changed, suggest the most relevant automated tests and the minimum manual verification steps.
7. End with a concise pass/fail checklist for desktop validation.
