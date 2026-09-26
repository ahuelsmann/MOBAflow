---

description: "Task list for removing the MOBApi control-plane security"
---

# Tasks: Remove the MOBApi control-plane security

**Input**: Design documents from `specs/006-remove-control-plane-security/`

**GitHub Issue**: #165

**Prerequisites**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/mobapi-api.md](contracts/mobapi-api.md), [quickstart.md](quickstart.md)

**Tests**: Admission, queue bounds and anonymous access get regression tests. Security-only tests are
deleted with the code they cover. Documentation edits use structural checks.

**Organization**: Phases follow the three delivery slices of the plan; each slice is one draft PR.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: May run in parallel because it touches different files and has no unmet dependency.
- **[Story]**: Maps the task to a user story of the spec.

## Phase 1: Analysis and Setup

- [ ] T001 Re-verify the inventory in `specs/006-remove-control-plane-security/research.md` against current `main` before each slice
- [ ] T002 Confirm the focused test filters in `specs/006-remove-control-plane-security/quickstart.md` execute tests

---

## Phase 2: Slice 1 - Command admission (User Story 2, Priority P1)

**Goal**: Every remote command is validated once and queued with a bound, on REST and SignalR.

**Independent Test**: Valid commands reach the host or queue; invalid ones and a full queue are rejected
on both transports.

### Tests for User Story 2

- [ ] T003 [P] [US2] Add validation tests for every command type and limit in `Test/Common/RuntimeCommandValidatorTests.cs`
- [ ] T004 [P] [US2] Add admission tests for invalid values, queue full and no forwarding of invalid commands in `Test/MOBApi/RuntimeCommandAdmissionTests.cs`
- [ ] T005 [P] [US2] Add REST status-code tests (`400`, `429`, `202`) for commands and journey reset in `Test/MOBApi/RuntimeCommandsControllerTests.cs`

### Implementation for User Story 2

- [ ] T006 [US2] Add `RuntimeCommandValidator` next to the envelope in `Common/Runtime/RuntimeCommandValidator.cs`
- [ ] T007 [US2] Bound the queue to 128 entries with non-blocking `TryEnqueue` in `MOBApi/Service/RuntimeCommandQueue.cs`
- [ ] T008 [US2] Add the admission service and its DI registration in `MOBApi/Service/RuntimeCommandAdmission.cs` and `MOBApi/Program.cs`
- [ ] T009 [US2] Route REST commands through admission in `MOBApi/Controllers/RuntimeCommandsController.cs` and `MOBApi/Controllers/JourneyProgressController.cs`
- [ ] T010 [US2] Route SignalR commands through admission before forwarding in `MOBApi/Hubs/RuntimeHub.cs`
- [ ] T011 [US2] Validate slice 1 with the MOBApi build and focused plus portable test runs from `specs/006-remove-control-plane-security/quickstart.md`

**Checkpoint**: Slice 1 PR is green in CI and SonarCloud.

---

## Phase 3: Slice 2 - Remove the read migration (User Story 1, Priority P1)

**Goal**: No anonymous-read migration or GitHub evidence lookup remains.

- [ ] T012 [US1] Delete `MOBApi/Security/CompatibilityRead*.cs` and `MOBApi/Security/GitHubIssueEvidenceVerifier.cs`
- [ ] T013 [US1] Remove the read-migration middleware, endpoints and registrations from `MOBApi/Program.cs`, `MOBApi/Controllers/ControlPlaneSecurityController.cs` and `MOBApi/Security/ControlPlaneSecurityServiceCollectionExtensions.cs`
- [ ] T014 [US1] Delete the read-migration test cases in `Test/MOBApi/ControlPlaneSecurityTests.cs` and `Test/Integration/AuthenticatedControlPlaneProcessTests.cs`
- [ ] T015 [US1] Remove analyzer-baseline entries of the deleted files in `quality/analyzer-baseline*.json`
- [ ] T016 [US1] Validate slice 2 with the MOBApi build and the portable test run

**Checkpoint**: Slice 2 PR is green in CI and SonarCloud.

---

## Phase 4: Slice 3 - Remove the control plane (User Stories 1 and 3)

**Goal**: MOBAsmart connects without setup steps; no security code, UI or documentation remains.

**Independent Test**: A client without stored data reads, controls and uploads photos over plain HTTP;
structural search finds no security types.

### Tests for User Story 1

- [ ] T017 [P] [US1] Add anonymous-access tests for read, host publish, command and photo upload endpoints in `Test/MOBApi/SolutionControllerTests.cs`, `Test/MOBApi/StatusControllerTests.cs`, `Test/MOBApi/RuntimeCommandsControllerTests.cs` and `Test/MOBApi/PhotosControllerTests.cs`
- [ ] T018 [P] [US1] Update discovery tests without identity fields in `Test/Common/DiscoveryResponseParserTests.cs`
- [ ] T019 [P] [US1] Update host-service and mobile initialization tests in `Test/MOBAflow/RestApiStatusServiceTests.cs` and `Test/SharedUI/MauiViewModelInitializationTests.cs`

### Implementation for User Story 1

- [ ] T020 [US1] Delete the remaining files in `MOBApi/Security/`, `MOBApi/Controllers/ControlPlaneSecurityController.cs` and `MOBApi/Controllers/HostEnrollmentController.cs`
- [ ] T021 [US1] Remove policies and loopback checks from `MOBApi/Controllers/*.cs` and `MOBApi/Hubs/*.cs`
- [ ] T022 [US1] Remove the HTTPS listener, host bootstrap, authentication and authorization from `MOBApi/Program.cs`
- [ ] T023 [US1] Remove identity fields from `Common/Discovery/MobApiUdpDiscoveryResponder.cs`, `Common/Discovery/DiscoveryResponseParser.cs`, `MOBApi/Service/UdpDiscoveryService.cs` and `MOBAflow/Service/UdpDiscoveryResponder.cs`
- [ ] T024 [US1] Delete `Common/Security/` and `Common/Events/RemotePairingCompletedEvent.cs`
- [ ] T025 [US1] Delete `MOBAflow/Service/HostControlPlaneSession.cs`, `MOBAflow/Service/RestApiPairingHost.cs`, `MOBAflow/Service/RestApiQrCodeImageFactory.cs` and `MOBAflow/ViewModel/RestApiPairingViewModel.cs`
- [ ] T026 [US1] Switch `MOBAflow/Service/RestApiProcessService.cs`, `RestApiSolutionSyncService.cs`, `RestApiRuntimeHubService.cs`, `RestApiRuntimeCommandConsumerService.cs`, `RuntimeHubHostClient.cs`, `PhotoHubClient.cs` and `FirewallHelper.cs` to plain HTTP without host session
- [ ] T027 [US1] Remove the pairing section from `MOBAflow/View/SettingsPage.xaml`, `MOBAflow/View/SettingsPage.xaml.cs` and `MOBAflow/Controls/SelectedObjectWorkbench.xaml`, and registrations from `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`
- [ ] T028 [US1] Delete `SharedUI/ViewModel/RemotePairingViewModel.cs` and `SharedUI/Interface/IPairingCameraAccess.cs`; remove pairing code from `SharedUI/ViewModel/MauiViewModel.cs`, `SharedUI/Service/SolutionRemoteLoader.cs`, `SharedUI/Interface/IPhotoServices.cs` and `SharedUI/ViewModel/InterlockingControlViewModel.Presentation.cs`
- [ ] T029 [US1] Delete `MOBAsmart/Service/PinnedRemoteControlTransport.cs`, `MauiRemoteControlCredentialStore.cs`, `PairingCameraAccess.cs` and `MOBAsmart/View/PairingPage.xaml(.cs)`
- [ ] T030 [US1] Use plain HTTP clients in `MOBAsmart/Service/RuntimeHubRemoteClient.cs`, `MobiLanHttpClientFactory.cs` and `RestApiDiscoveryService.cs`; remove pairing from `MOBAsmart/Controls/AppBottomTabBar.xaml(.cs)`, `MOBAsmart/View/AppTabHostPage.xaml.cs`, `MOBAsmart/MauiProgram.cs` and `MOBAsmart/Extensions/*.cs`
- [ ] T031 [US1] Remove `ZXing.Net` and `ZXing.Net.Maui.Controls` from `MOBAflow/MOBAflow.csproj`, `MOBAsmart/MOBAsmart.csproj` and `Directory.Packages.props`
- [ ] T032 [US1] Delete security-only tests `Test/MOBApi/ControlPlaneSecurityTests.cs`, `Test/MOBApi/HostCredentialServiceTests.cs`, `Test/Common/RemoteControlClientSecurityTests.cs`, `Test/Common/HostBootstrapProtocolTests.cs`, `Test/SharedUI/RemotePairingViewModelTests.cs`, `Test/Integration/AuthenticatedControlPlaneProcessTests.cs` and `Test/MOBAsmart/PinnedRemoteControlTransportTests.cs`
- [ ] T033 [US1] Remove analyzer-baseline entries of deleted files in `quality/analyzer-baseline*.json`

### Implementation for User Story 3

- [ ] T034 [P] [US3] State the operating scope and fork recommendation in `README.md` and `SECURITY.md`
- [ ] T035 [P] [US3] Remove pairing and credential content from `docs/ARCHITECTURE.md`, `docs/PROJECT-REFERENCE.md` and `docs/wiki/*.md`
- [ ] T036 [US3] Delete `docs/MOBAPI-SECURITY-DESIGN.md` and `plans/50-authenticated-control-plane.md`; point RF-19 in `plans/QUALITY-AND-REFACTORING-PLAN.md` to this feature

### Validation

- [ ] T037 Run the structural search, portable tests, Windows tests and the MOBApi, MOBAflow and MOBAsmart builds from `specs/006-remove-control-plane-security/quickstart.md`
- [ ] T038 Record actual results and pending manual checks in `specs/006-remove-control-plane-security/quickstart.md`

---

## Phase 5: Publication gates (every slice)

- [ ] T039 Run `scripts/Test-LineEndings.ps1 -Staged`, `scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main` and the secrets scan of every changed file
- [ ] T040 Keep each slice PR draft until the GitHub Sonar quality gate (SonarCloud) is green with zero OPEN/CONFIRMED issues on its current commit
- [ ] T041 Close #165 after slice 3 merges and mark RF-19 complete in #47

## Dependencies

- Slice 1 (T003-T011) has no dependency and starts first.
- Slice 2 (T012-T016) is independent of slice 1 but is merged after it.
- Slice 3 (T017-T038) requires slice 2 to be merged.
- Manual checks from the quickstart need explicit approval to start the apps.
