---
description: Validate a MOBAflow change with targeted restore, build, and test commands.
---

# Validate Change

Use this workflow after implementing or reviewing a change in MOBAflow.

1. Identify the affected project or layer before suggesting commands.
2. Prefer targeted commands over solution-level commands.
3. Use the real project names from this repository:
   - `MOBAflow` for the WinUI desktop app
   - `MOBApi` for the REST API
   - `MOBAsmart` for the Android MAUI app
4. Prefer these validation commands when appropriate:
   - `dotnet restore <project>.csproj`
   - `dotnet build <project>.csproj`
   - `dotnet test Test/Test.csproj`
5. Do not assume that solution-level restore is the safest option.
6. If the change is limited to cross-platform shared logic, prefer validating the affected library plus `Test/Test.csproj`.
7. If the change affects UI behavior, mention relevant manual checks in addition to build and test commands.
8. End with a short validation checklist tailored to the affected files.
