# RF-26: Split Common by responsibility

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/290
**Spec Kit**: Not applicable - structural refactoring; the only persisted-format change is the internal feature-toggle collection, loaded with defaults per repository policy
**Status**: Active; slices 1 and 3 integrated through #295; slice 4a (runtime contracts, #310) and slice 4b (discovery) in review; slice 2 waits for RF-24 slice 6

## Purpose and scope

[Issue #290](https://github.com/ahuelsmann/MOBAflow/issues/290) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies (RF-26 needs RF-19,
which is integrated, and feeds RF-20 rules and RF-30 layout). This is the single RF-26 child plan.

`Common` keeps neutral infrastructure only: events and the event bus, paths, logging sinks, file-system
and configuration loading, task helpers. UI types move to `SharedUI` (when a shared ViewModel or both hosts
use them) or to the single host that uses them. Wire contracts shared by MOBApi, MOBAflow and MOBAsmart move
to a small contracts project. No runtime ownership, protocol, hub method name, discovery payload, settings
default or user-visible behavior changes.

## Start gate and coordination

- RF-19 is integrated (#165). RF-20 architecture tests exist (#183) and gain the new boundary rules.
- Open Codex PRs touch the same area: #267 (tests for `MobaApiRuntimeDeployment` and
  `RuntimeCommandValidator`) and #281 (`Common` mutation project and `PhotoPathHelperTests`);
  #288 touches only `quality/analyzer-baseline.json`. In a CLI exchange on 2026-10-15 Codex proposed
  starting with the display and settings slices and moving runtime/discovery contracts only after #267
  (and preferably #281) are integrated. The contracts slice re-checks those PRs before it starts.

## Source inventory and proposed owners

Consumers were counted from `using` directives and type references on main `1d45aec6`.

| Area in `Common` | Consumers | Proposed owner |
| --- | --- | --- |
| `Display/FunctionBacklightColor`, `HexColor`, `LocomotiveFunctionAppearanceResolver` | SharedUI, MOBAflow, MOBAsmart | `SharedUI` (function-button appearance) |
| `Display/KsSignalAspectNames`, `KsSignalScreenLayout`, `KsSignalScreenVisualState` | MOBAflow, MOBAsmart | `SharedUI` |
| `Display/GaugeThemeAppearance` | MOBAsmart | `SharedUI` (keeps its tests in the portable suite) |
| `Display/LedMatrix5x5State` | MOBAflow | `SharedUI` (keeps its tests in the portable suite) |
| `Configuration/AppSettings.Sections`: `LayoutSettings` and page layout classes, `TabVisibilitySettings`, `FeatureToggleSettings`; `FeatureToggleRegistry`, `SettingsPageLayoutSettings`, `GridSplitterLayoutSettings` (`PageLayoutSettings`, `ColumnState`) | SharedUI ViewModels, MOBAflow views/services, MOBAsmart tab host | `SharedUI` UI settings |
| `Navigation/IFeatureTogglePageProvider`, `NavigationCategory` | SharedUI, MOBAflow | `SharedUI` (provider contract), MOBAflow (categories) |
| `Configuration/TrainControlHost`, `TrainControlHostSettings` | SharedUI train control, both hosts | `SharedUI/TrainControl` (with the settings root in slice 2, because `AppSettings` holds them) |
| `Configuration/RestApiRecentEndpointHistory`, `Runtime/MobileBackgroundKeepAlivePolicy` | SharedUI | `SharedUI` |
| `Platform/IPlatformCapability`, `GridSplitterLayoutSettings` class | none | remove |
| `Z21/Z21BroadcastRequirements` | Backend tests only | remove; duplicates `Z21Protocol.BroadcastFlags` in Backend |
| `Runtime/*` snapshots, `RemoteRuntimeCommands`, `RuntimeCommandValidator`, `RuntimeHubContract` (hub interfaces), `RuntimeJsonSerializer`, snapshot filter/merge helpers | Backend, SharedUI, MOBAflow, MOBAsmart, MOBApi | contracts project |
| `Discovery/*` protocol types (`DiscoveryResponseParser`, `MobApiUdpDiscoveryResponder`, endpoint and candidate builders, `MobaApiRuntimeDeployment`, path resolver) | Backend, SharedUI, hosts, MOBApi | contracts project for wire format; host-specific discovery helpers to their owner |
| `Events`, `Extension`, `IO`, `Path`, `Serilog`, `Validation`, `Recording`, `Multiplex`, neutral `AppSettings` sections | Backend and all hosts | stay in `Common` (re-checked in slice 5) |

`AppSettings` sections used outside the UI stay in `Common`: Backend reads `Counter`, `Speech` and
`SignalBox`; Speech.Base reads `Speech`; hosts read `Z21`, `RestApi`, `Application` and `HealthCheck`.

## Design decisions and alternatives

- **UI settings root.** Shared ViewModels in `SharedUI` read layout and toggle settings, so `SharedUI` is the
  lowest common owner (moving them into MOBAflow would force the shared ViewModels to depend on the WinUI host).
  A `SharedUI` settings root derives from the neutral `AppSettings` and adds `Layout`, `FeatureToggles`,
  host train-control settings and display settings. Hosts register the one instance under both types, so
  Backend keeps receiving `AppSettings` and the JSON file keeps its property names.
  Rejected: a second settings file (two save paths, split auto-save) and moving `AppSettings` out of `Common`
  (Backend and Speech.Base need the neutral sections).
- **Keyed feature toggles.** The 19 `Is…PageAvailable`/`…PageLabel` pairs and the property accessors in
  `FeatureToggleRegistry` become one dictionary keyed by the page key the registry already uses, with registry
  defaults for missing keys; the visual-editor flag stays a plain property.
  Existing stored toggle values are not migrated; they load with defaults (no released users).
- **Contracts project.** A new `Contracts` project (namespace `Moba.Contracts`) depends only on `Domain` where
  snapshots carry domain enums. It contains wire types and protocol identifiers, not transports, hosted services
  or runtime ownership. MOBApi then references `Contracts` and only the parts of `Common` it uses.
- **Namespaces follow folders.** Moved types take the namespace of their new folder; no type forwarding or
  aliases are kept.

## Independently reviewable delivery slices

Each slice is one PR (stacked when a slice depends on an unmerged one), deletes the old location in the same
change, updates analyzer baselines narrowly (moved diagnostics travel with their file) and adds or extends an
RF-20 architecture rule for the boundary it creates.

1. **Display appearance helpers.** Move all `Common/Display` types to `SharedUI/Appearance`
   (`Moba.SharedUI.Appearance`); architecture rule: `Common` contains no `Display` namespace.
2. **UI settings and navigation.** Starts after RF-24 slice 6 (#219), which centralizes layout persistence
   in one boundary; moving the layout types earlier would churn the shell partials RF-24 removes.
   Introduce the `SharedUI` settings root, move layout, tab visibility, feature toggles,
   `FeatureToggleRegistry`, navigation types and the train-control host settings, replace the per-page
   toggle properties with the keyed collection and update both `SettingsService` implementations and
   `WinUiConfigurationBuilder`. Tests: settings round trip on both hosts, missing-key defaults, toggle
   registry lookup.
3. **Remaining host-specific and unused types.** Move REST endpoint history and the mobile keep-alive policy
   to `SharedUI/Mobile`; remove `Z21BroadcastRequirements` (tests use `Z21Protocol.BroadcastFlags`),
   `IPlatformCapability` and the unused `GridSplitterLayoutSettings` class.
4. **Contracts project.** 4a (#310): `Contracts` (`Moba.Contracts`, references `Domain`) takes everything from
   `Common/Runtime`; `Common` references `Contracts` because its events carry snapshots. 4b: the MOBApi discovery
   wire protocol (`DiscoveryResponseParser`, `MobApiEndpoint`, `MobApiHealthProbe`) moves to `Contracts/Discovery`;
   Z21 discovery types (`DiscoveredZ21`, `IZ21DiscoveryService`) join `Backend/Discovery`; `RestApiDiscoveryCandidateBuilder`
   moves to `SharedUI/Mobile`; the unused `NullZ21DiscoveryService` is removed. The UDP responder (used by MOBApi and
   by MOBAflow, whose MOBApi reference is optional), the network helpers and the MOBApi path/deployment helpers stay
   in `Common` as neutral infrastructure.
5. **Common boundary and documentation.** Re-check what is left in `Common`, add the final RF-20 rules,
   update `docs/ARCHITECTURE.md` and the repository map in `AGENTS.md`, replace the RF-26 anchor in the
   umbrella plan with a delivery note and remove this plan.

## Validation strategy

- Every slice: portable and Windows test suites, the cross-platform, Windows and Android analyzer gates,
  and the Android Release build when MOBAsmart or SharedUI change.
- Settings slice: round-trip and default-loading tests for both `SettingsService` implementations; the
  WinUI toggle configuration path; manual check of page availability is not required for review but is
  listed for the maintainer.
- Contracts slice: MOBApi build and its integration tests, hub method name and serializer tests, discovery
  parser and responder tests; mutation project configuration builds.
- No MOBAflow or MOBAsmart launch, device deployment or hardware action is part of validation.

## Risks, compatibility and publication

- **Persisted settings:** slice 2 changes the stored feature-toggle shape; old values load as defaults.
  All other property names stay unchanged.
- **XAML bindings:** moved types used in XAML (`x:Bind` to settings or appearance helpers) need namespace
  updates; compile both apps for slices 1 and 2.
- **Merge churn:** moving many files conflicts with open PRs. Slices stay small, and slice 4 waits for #267/#281.
- **Analyzer baselines:** moved diagnostics must move with their file, not be refreshed wholesale.
- **Mutation coverage (slices 1 and 3):** the active Common lane mutates only `Common.csproj` and compiles
  `Test/Common/**`. The 8 appearance and 2 mobile helpers and their 8 test files now live in `SharedUI`,
  whose `shared-ui` lane is still `planned`, so these helpers are no longer mutation-tested. Their tests
  still run in the portable and Windows suites. The Common lane baseline ran 1010 tests in #281 and 848 in
  #295 CI at `64e199f1`; because the lane's inputs differ, the scores are not comparable. Thresholds,
  exclusions and lanes are unchanged; activating a SharedUI lane is separate work.

## Downstream sequence and completion

RF-30 (repository and test layout) builds on the final project layout. RF-20 rules added here stay in force.
Completion: issue #290 acceptance boxes checked, slice 5 integrated, this plan removed.
