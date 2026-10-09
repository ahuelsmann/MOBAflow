# Implementation Plan: MOBAflow 1.0 release, setup and getting started

**Branch**: `codex/issue-144-release-1-0-spec` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #144

**Spec Kit**: Required

**Status**: Deferred (clarified 2026-10-09; no release date; slices below wait until the maintainer schedules 1.0)

**Input**: Feature specification from `/specs/010-release-1-0/spec.md`

## Summary

Turn the existing Release Studio ZIP into a user-installable 1.0 release with an MSIX package for the Microsoft
Store plus the ZIP, a signed MOBAsmart APK and the Play Store route, rewrite the
installation docs for users, add a beginner start guide, refresh screenshots, align README, wiki, project website
and MOBAnews, and document a marketing plan. Work that does not depend on RF-23 starts now; documentation that
describes runtime and Z21 setup waits for RF-23. The whole feature is deferred: no release is planned in the
foreseeable future (spec Q3).

## Technical Context

**Language/Version**: GitHub Actions (PowerShell steps), MSIX packaging of the WinUI app (Q1), Markdown, static
HTML (`docs/index.html`).

**Primary Dependencies**: Existing `release-studio.yml`, `cliff.toml`, `scripts/` release validators. MSIX
packaging uses the Windows App SDK tooling already in the build; no new NuGet package. Store submission is manual
through Partner Center at first.

**Storage/Protocols**: None.

**Testing**: Existing release validator scripts; a new validation step that checks the MSIX package exists and
contains the same application files as the ZIP; documentation link check (shared with #197 CI-4 if it lands first).

**Target Platforms**: Windows x64 (MOBAflow), Android (MOBAsmart APK and AAB, Q2).

**Affected Layers**: `.github/workflows/release-studio.yml`, MSIX manifest and packaging settings in `MOBAflow`,
`docs/wiki/`, `docs/index.html`, `README.md`, `docs/RELEASE-STUDIO.md`, `MOBAflow` Info page link (Q4).

**Performance Goals**: N/A (release packaging).

**Data and API Effects**: None for application data. Settings and data paths are checked under MSIX
(packaged apps may see virtualised app-data folders); user data stays on uninstall where Windows allows it.

**Scale/Scope**: One MSIX package plus ZIP, one APK plus AAB, documentation set of five sources.

## Constitution Check

- [x] Layer and platform boundaries are preserved (packaging and docs only; one link on the Info page).
- [x] EventBus UI-thread marshalling unchanged.
- [x] Async flows unchanged.
- [x] UI change limited to a link via an existing command or hyperlink; Light/Dark checked.
- [x] All user-visible UI strings are English; the German website follows Q5.
- [x] Data and API effects are explicit (none for app data).
- [x] Release validation covers the MSIX package; documentation uses structural and link checks.
- [x] Validation follows the `AGENTS.md` matrix (Release checks for packaging changes).
- [x] Reuses Release Studio and its validators.
- [x] Spec and plan reference #144.
- [x] Artifacts stay below `specs/`.
- [x] Signing material stays in GitHub secrets; changed files are scanned before commit.
- [x] Sonar runs only in GitHub CI.
- [x] PRs stay draft until SonarCloud is green with zero OPEN/CONFIRMED issues.

## Delivery Slices

### Can start now (independent of RF-23)

1. **Scope and claims audit**: inventory every scope claim and link in README, wiki, `docs/index.html` and
   MOBAnews; list contradictions (for example, the website still says "clone the repository and install .NET 10";
   the issue text still calls #148 an open draft). Output: `specs/010-release-1-0/research.md`.
2. **MSIX package in Release Studio**: check the Store developer account cost first; switch MOBAflow to a packaged
   build for the release, verify settings, solution and counter paths under MSIX, attach the package and the ZIP
   to the draft release with checksums; release validator checks package content against the ZIP;
   `docs/RELEASE-STUDIO.md` updated. Verified with a release-candidate tag in a draft release (never published
   automatically); Store submission by the maintainer.
3. **MOBAsmart APK and Play Store**: re-enable the Android Release AAB job (#195), add a signed APK step with the
   same restore and publish steps, keystore from GitHub secrets, AAB validated with
   `scripts/Test-AndroidAppBundle.ps1`; the maintainer starts the Play Store closed test.
4. **Marketing document**: channels (German model railroad forums, YouTube, club newsletters, MOBAnews), target
   groups, first three actions, MOBAnews update rhythm; follows the MOBAnews editorial rules from the issue.
5. **Website roles and links** (after Q4/Q5, still open): project website becomes the entry point with download and start
   guide links; MOBAflow Info page links to website and MOBAnews.

### After RF-23 (#191) slice 5 and #188

6. **Installation and start guide**: rewrite `docs/wiki/INSTALLATION.md` for the release download; move the source
   build to developer docs; write `docs/wiki/START-GUIDE.md` covering Z21 per project, first locomotive, first
   journey and event-plan entry, first workflow.
7. **Screenshots**: maintainer captures current screenshots (app launch needs approval); README and website
   updated.
8. **Release candidate**: signed tag `1.0.0-rc.1`, Release Studio draft, clean-machine install check, hardware
   smoke check with a Z21, notes edited; then `1.0.0`.

Each slice is one draft PR from its own worktree; slices 1, 4 and 5 are documentation-only.

## Dependencies and Sequencing

- Slices 6 to 8 depend on RF-23 (Z21 per project, switch warning) and #188.
- Slice 2 touches only `release-studio.yml`; it does not overlap #197, which changes `quality.yml` and the change
  scope script. Coordinate if #197 CI-4 (actionlint, link checks) touches the release workflow.
- Deferred (spec Q3): no slice starts until the maintainer schedules 1.0; RF-24 to RF-31 do not wait for it.
- Q4 and Q5 are answered when the release is scheduled.

## Validation Strategy

- **Automated tests**: Release validator scripts on a release-candidate tag; documentation link check.
- **Builds**: Release Studio run on a release-candidate tag (Release configuration, Windows tests included).
- **Manual checks**: clean Windows 11 install from the MSIX package, start, uninstall; start guide walkthrough; screenshots in Light and
  Dark (all maintainer-led, with launch approval).
- **Regression checks**: user data untouched by install and uninstall; ZIP still produced.
- **Secrets scan**: workflow and packaging files; no signing material or keystore in the repository.
- **Sonar gates**: SonarCloud green, zero OPEN/CONFIRMED issues per PR.
- **Issue traceability**: every PR references #144.
