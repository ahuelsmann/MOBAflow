# Implementation Plan: Remove the MOBApi control-plane security

**Branch**: `codex/issue-165-remove-control-plane-security` | **Date**: 2026-09-26 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #165

**Spec Kit**: Required

**Status**: Ready for tasks

**Input**: Feature specification from `specs/006-remove-control-plane-security/spec.md`

## Summary

MOBAflow targets a trusted home network, so the merged RF-03 control plane is removed: no pairing,
tokens, credentials, host enrollment, HTTPS identity, certificate pinning, read migration or
source-address checks remain. Before the removal, one command admission step validates every remote
command on REST and SignalR and a bounded queue replaces the unbounded one, because neither protection
exists today (see [research.md](research.md)).

## Technical Context

**Language/Version**: .NET 10 / C# (repository defaults)

**Primary Dependencies**: ASP.NET Core, SignalR, CommunityToolkit.Mvvm, NUnit, Moq. No additions;
`ZXing.Net` and `ZXing.Net.Maui.Controls` are removed.

**Storage/Protocols**: REST and SignalR over plain HTTP; UDP discovery. Solution JSON unchanged.

**Testing**: NUnit and Moq; controller and hub tests instantiate them directly, as the existing
`Test/MOBApi` fixtures do.

**Target Platforms**: ASP.NET Core host, Windows/WinUI, Android/MAUI, cross-platform libraries.

**Affected Layers**: Common, SharedUI, MOBApi, MOBAflow, MOBAsmart, Test; documentation and plans.

**Performance Goals**: N/A. The change removes per-request authentication work; the queue bound
(128) caps memory use.

**Data and API Effects**: See [contracts/mobapi-api.md](contracts/mobapi-api.md) and
[data-model.md](data-model.md). Security endpoints, the HTTPS port and discovery identity fields are
removed without compatibility paths; leftover security files are ignored. Removed settings fields are
ignored on load. Configuration defaults, `AutoStartWebApp`, safe locomotive startup, Z21 behavior and
persisted layouts are unchanged. `Solution.CurrentSchemaVersion` is unchanged.

**Scale/Scope**: About 45 product files deleted and 25 changed in five projects; about 4,700 test lines
deleted. ESP32 provisioning (RF-02) is excluded.

## Constitution Check

*GATE: Checked before Phase 0 research and re-checked after Phase 1 design.*

- [x] Layer and platform boundaries are preserved; the validator lives in `Common/Runtime` next to the
  envelope, and MOBApi still references only `Common`.
- [x] EventBus UI-thread marshalling remains centralized in `UiThreadEventBusDecorator`.
- [x] Async flows use `await` without sync-over-async.
- [x] UI behavior uses ViewModel commands; removed UI leaves existing theme resources intact.
- [x] All user-visible UI strings are English.
- [x] Data and API effects are explicit; superseded models are removed without legacy paths or migrations.
- [x] Meaningful regression tests cover admission, queue bounds and anonymous access; removed
  security tests are deleted rather than rewritten.
- [x] Validation commands follow the change matrix in `AGENTS.md` and match the affected platforms.
- [x] The design reuses existing envelopes, controllers, hubs, discovery and DI patterns.
- [x] The specification and plan reference the authoritative GitHub issue.
- [x] Spec Kit artifacts remain below `specs/`; the RF-03 standalone plan is deleted in slice 3.
- [x] No secret-bearing input is needed; all changed files are scanned before commit.
- [x] Sonar code analysis runs only in GitHub CI; no local Sonar/Vortex analysis or analysis hooks.
- [x] Each PR remains draft until SonarCloud is green for its current commit with zero OPEN/CONFIRMED issues.

## Project Structure

### Documentation (this feature)

```text
specs/006-remove-control-plane-security/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/mobapi-api.md
|-- checklists/requirements.md
`-- tasks.md
```

### Source Code

```text
Common/Runtime/          # new RuntimeCommandValidator next to RuntimeCommandEnvelope
Common/Security/         # deleted completely
Common/Discovery/        # identity and fingerprint removed from responder and parser
Common/Events/           # RemotePairingCompletedEvent deleted
SharedUI/                # RemotePairingViewModel, IPairingCameraAccess deleted; MauiViewModel pairing code removed
MOBApi/Security/         # deleted completely
MOBApi/Controllers/      # security and host-enrollment controllers deleted; policies removed; admission used
MOBApi/Hubs/             # policies removed; admission used before forwarding
MOBApi/Service/          # RuntimeCommandQueue bounded; new RuntimeCommandAdmission
MOBApi/Program.cs        # HTTPS listener, bootstrap, authentication and middleware removed
MOBAflow/Service/        # HostControlPlaneSession, RestApiPairingHost, RestApiQrCodeImageFactory deleted;
                         # process, sync, hub, consumer and firewall code use plain HTTP
MOBAflow/ViewModel/      # RestApiPairingViewModel deleted
MOBAflow/View/           # pairing and credential section removed from SettingsPage
MOBAsmart/Service/       # PinnedRemoteControlTransport, MauiRemoteControlCredentialStore, PairingCameraAccess deleted
MOBAsmart/View/          # PairingPage deleted; tab bar and host page without pairing
Test/                    # security tests deleted; admission, queue and anonymous-access tests added
quality/                 # analyzer-baseline entries for deleted files removed
docs/, README.md, SECURITY.md, plans/  # scope statement; security design and RF-03 plan deleted
```

**Structure Decision**: Removal follows ownership: each host deletes its own security adapters, shared
contracts leave `Common/Security` entirely, and the only addition is the admission step that both
transports share.

## Delivery slices

1. **Command admission** (FR-006, FR-007): add `RuntimeCommandValidator`, a MOBApi admission service
   and the bounded queue; route REST commands, journey reset and SignalR commands through it before
   forwarding or queueing. Independent of the removal.
2. **Read migration** (FR-001): delete `CompatibilityRead*`, the evidence verifier, their middleware,
   endpoints, registration and tests.
3. **Control-plane removal** (FR-002 to FR-005, FR-008 to FR-011): delete the remaining security code
   across MOBApi, Common, SharedUI, MOBAflow and MOBAsmart; switch every client to plain HTTP; remove
   packages, settings, UI, tests and baseline entries; update documentation and delete the RF-03 plan and
   security design record.

Each slice is its own draft PR, builds on its own and keeps the apps connectable.

## Validation Strategy

- **Automated tests**: new `RuntimeCommandValidatorTests` (limits and enum values),
  `RuntimeCommandAdmissionTests` (REST and SignalR equivalence, queue full, nothing forwarded when
  invalid) and anonymous-access checks in the existing `Test/MOBApi` controller tests; update
  `DiscoveryResponseParserTests`, `RestApiStatusServiceTests` and `MauiViewModelInitializationTests`;
  delete the security-only fixtures listed in research.
- **Builds**: `dotnet build MOBApi/MOBApi.csproj` for slices 1 and 2; slice 3 additionally builds
  MOBAflow (FastDebug) and MOBAsmart (Android FastDebug) as listed in [quickstart.md](quickstart.md).
- **Manual checks**: connection, remote driving and the settings page in Light and Dark theme, only
  after explicit approval to start the apps.
- **Regression checks**: discovery, manual address, solution sync, runtime snapshot, photo upload,
  configuration defaults and `AutoStartWebApp`.
- **Secrets scan**: every changed file before each commit and PR.
- **Sonar gates**: GitHub SonarCloud check green with zero OPEN/CONFIRMED issues on each slice PR.
- **Issue traceability**: every slice PR references #165; tasks are tracked in [tasks.md](tasks.md).

## Complexity Tracking

No constitution exceptions are needed. The admission service is the only new abstraction; it replaces
two divergent validation paths rather than adding a layer.
