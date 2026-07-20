# MOBAflow project reference

**Scope:** current repository structure, app boundaries, runtime behavior,
persisted data, integrations and build entry points.
**Last reviewed:** 2026-07-20

## Products

| Product | Current role |
| --- | --- |
| **MOBAflow** | WinUI desktop host for editing, operating, monitoring, speech and display workflows |
| **MOBAsmart** | Android host with a local Z21 runtime plus optional MOBAflow/MOBApi synchronization |
| **MOBApi** | Standalone ASP.NET Core cache/bridge for solution data, runtime state, commands, progress, photos and clients |
| **MOBAdisplay** | Rendering/transport library referenced by MOBAflow plus working PlatformIO receiver firmware; end-to-end app integration remains preview-stage |

## Repository structure

```text
Domain/                 Persisted models, enums and JSON converters
Common/                 Configuration, events, validation, discovery and helpers
Backend/                Z21 protocol, runtime, journey and workflow services
SharedUI/               Cross-platform ViewModels and UI-facing services
Sound/                  Audio and speech abstractions/implementations
MOBAflow/               Windows WinUI app
MOBAsmart/              Android MAUI app
MOBApi/                 REST and SignalR host
MOBAdisplay/            RGB565 rendering, UDP transport and firmware prototypes
TrackLibrary.Base/      Shared track geometry contracts
TrackLibrary.PikoA/     Piko A catalog and editable track-plan implementation
TrackPlan.Renderer/     Platform-neutral render and SVG primitives
Test/                   NUnit test project
MutationTest/           Focused Stryker.NET lanes
docs/                   User, developer, legal and protocol documentation
plans/                  Standalone project, quality, refactoring and roadmap plans
.github/workflows/      Public quality, pages and release workflows
.azure-pipelines/       Additional Azure DevOps quality/release workflows
```

## Architecture

```mermaid
flowchart TD
    WinUI["MOBAflow Desktop"] --> SharedUI
    Android["MOBAsmart"] --> SharedUI
    SharedUI --> Runtime["IMobaRuntime / MobaRuntimeService"]
    Runtime --> Z21["Roco Z21 via UDP"]
    Runtime --> Domain
    Runtime --> Sound
    WinUI --> Display["MOBAdisplay"]
    WinUI <-->|"REST + SignalR"| API["MOBApi"]
    Android <-->|"REST + SignalR"| API
    Android -->|"direct UDP"| Z21
    API --> Common
```

MOBApi intentionally references `Common` only. It does not host the Backend
runtime; MOBAflow publishes solution and runtime state into its caches and
consumes queued remote commands.

### Layer responsibilities

| Layer | Responsibility |
| --- | --- |
| `Domain` | Serializable models and workflow payloads without platform dependencies |
| `Common` | Configuration, EventBus contracts, validation, discovery and runtime DTOs |
| `Backend` | Runtime orchestration, Z21 communication, feedback, journeys and workflows |
| `SharedUI` | Observable state and commands shared by WinUI and MAUI |
| App hosts | Platform UI, lifecycle, files, network wiring and process management |

## Runtime and threading

`IMobaRuntime` is implemented by the partial `MobaRuntimeService`:

- `Backend/Service/MobaRuntimeService.cs`
- `Backend/Service/MobaRuntimeService.RuntimeApi.cs`
- `Backend/Service/MobaRuntimeService.Z21Handlers.cs`
- `Backend/Service/MobaRuntimeService.AutoConnect.cs`

The runtime owns Z21 state, project activation, journey sessions, feedback
projection and immutable snapshots. Runtime projects are cloned from the editor
graph so execution does not mutate the solution being edited.

EventBus traffic follows one UI marshalling boundary:

```text
Z21/background publisher
  -> EventBus
  -> UiThreadEventBusDecorator
  -> ViewModel handler on the UI thread
```

ViewModels must not add dispatcher calls inside EventBus handlers.

### MOBAsmart hybrid runtime

MOBAsmart owns a local `IMobaRuntime` for direct Z21 feedback and commands. It
also connects to `/runtime-hub` and the MOBApi REST surface:

- locomotive commands prefer the local Z21 when connected;
- remote commands are the fallback when the desktop runtime is active;
- signal-box/domain state prefers the active MOBAflow session; and
- cached solution/fleet/signal-box data is used while reconnecting.

`MobileRuntimeCoordinator`, `RuntimeHubRemoteClient`, `SolutionRemoteLoader` and
`MobileSolutionStore` implement this behavior.

## Persisted model

`Solution.CurrentSchemaVersion` is currently `4`. A solution contains projects;
each project can contain:

- locomotives, passenger wagons, goods wagons and train consists;
- locomotive maintenance plans, decoder snapshots and whistle rules;
- stations and platforms;
- workflows;
- journeys with ordered stations and explicit `FeedbackSequence` steps;
- a `TrackPlanDocument`;
- a `SignalBoxPlan`;
- dated timetable services and their project-wide turnaround policy; and
- 5x5 matrix images.

The current sample is `MOBAflow/solution.json`. `MOBAflow/data.json` contains
shared master data loaded through `MasterDataStore`.

### Journeys and feedback

`Journey.FeedbackSequence` replaces the older single-input journey model. Each
`JourneyFeedbackStep` represents an ordered feedback occurrence and its stop
transition behavior. Runtime progress is stored separately by
`JourneyRuntimeStateStore` and exposed through snapshots and MOBApi.

Timetable definitions remain part of the solution. Operator holds,
cancellations, actual times and live train/journey assignments are stored in a
separate project-scoped timetable session file. Runtime journey snapshots can
infer arrivals only when exactly one nonterminal service owns that journey;
departures remain manual dispatcher decisions.

The high-level flow is:

```text
Z21 feedback
  -> JourneyManager
  -> match next feedback-sequence step
  -> update JourneySessionState
  -> optional station transition/workflow
  -> publish snapshot and progress
```

## Workflows

`ActionExecutor` dispatches actions to `IWorkflowActionHandler` implementations.

| Action type | Current runtime behavior |
| --- | --- |
| `Announcement` | Generate station-aware text and speak it locally |
| `Audio` | Play a local WAV file |
| `Command` | Send configured raw command bytes to the Z21 |
| `ExecuteScript` | Execute a PowerShell script with arguments |
| `SelectSignalAspect` | Resolve and send the configured multiplex signal command |
| `TrainDestinationDisplay` | Registered, but currently logs/skips when no display service is configured |
| `ChangeJourneyStop` | Move the active journey to the next or selected station |
| `Matrix` | Persisted enum value; no handler is registered |

Sequential workflows await each action before the next. Parallel workflows use
cumulative `DelayAfterMs` start offsets and await the group.

## Track plan

`Domain.TrackPlanDocument` is the persisted neutral format.
`TrackPlanDocumentMapper` maps it to the editable Piko A representation.
`TrackPlanSolutionBinder` keeps the selected project and editor synchronized.

The dependency direction is:

```text
TrackLibrary.Base -> TrackPlan.Renderer
TrackLibrary.PikoA -> adapters/editor
MOBAflow -> Win2D input and drawing
```

Vendor-specific geometry must not be added to the neutral renderer.

## Display pipeline

The display library can render labels and clocks into RGB565 frames and send
them through `UdpLineFrameSender`.

```text
Display configuration
  -> SkiaFrameRenderer / ClockRenderer
  -> RGB565 frame
  -> UDP line packets
  -> ESP32-S3 target
```

The line protocol sends `HOST_VER`, `DISPLAY_META`, `FRAME_START`, one packet per
row and `FRAME_DONE`; see `MOBAdisplay/docs/protocol.md`. MOBAflow registers the
sender and scheduler, but the current Display page is a model/resolution
selector and no production destination-display workflow service is wired into
the default action handler.

`MOBAdisplay/esp32/src/main.cpp` implements the current 240x280 receiver: Wi-Fi
setup/status endpoints, row assembly, RGB565 framebuffer allocation and TFT
presentation. `MOBAdisplay/MobaDisplay/MobaDisplay.ino` is an older standalone
color-test sketch and does not implement networking.

## MOBApi endpoints

MOBApi maps controllers plus two SignalR hubs.

| Route | Purpose |
| --- | --- |
| `GET /api/status` | API/runtime host status and clients |
| `GET/PUT /api/solution` | Current solution cache |
| `GET /api/solution/meta` | Solution metadata |
| `GET/PUT /api/runtime-settings` | Z21/runtime settings shared by the desktop host |
| `GET /api/runtime/meta` | Runtime cache metadata |
| `GET/PUT /api/runtime/snapshot` | Current runtime snapshot |
| `POST /api/runtime/commands/signal-aspect` | Queue/forward a signal command |
| `POST /api/runtime/commands/locomotive/drive` | Queue/forward a drive command |
| `POST /api/runtime/commands/locomotive/function` | Queue/forward a function command |
| `GET /api/runtime/commands/pending` | Consume pending commands |
| `GET /api/runtime/journeys/{id}/feedback-progress` | Read journey progress |
| `POST /api/runtime/journeys/{id}/feedback-progress/reset` | Reset journey progress |
| `GET/PUT /api/projects/{projectId}/journeys/{journeyId}/feedback-sequence` | Read/update a feedback sequence |
| `POST /api/clients/register` | Register a client |
| `POST /api/clients/unregister` | Unregister a client |
| `GET /api/photos/health` | Photo service health |
| `GET /api/photos/file` | Serve a stored photo |
| `POST /api/photos/upload` | Store a photo and notify clients |
| `/runtime-hub` | Live runtime and solution notifications |
| `/photos-hub` | Photo notifications |

The default HTTP port is `5001`. `UdpDiscoveryService` runs in standalone MOBApi
unless `MOBAFLOW_DISCOVERY_IN_WINUI` indicates that the WinUI host owns
discovery.

MOBAflow starts MOBApi from an isolated copy of its build output so updates and
cleanup do not race the running process.

## App surfaces

### MOBAflow navigation

The current page registration includes Overview, Solution, Locomotives,
Passenger Wagons, Goods Wagons, Trains, Workflows, Stations, Journeys, Timetable, Event
Manager, Journey Map, Train Control, Track Plan, Signal Box, Display
Configurations, Matrix Images, Monitor, Help, Info and Settings.

Page availability and Preview labels are controlled by
`AppSettings.FeatureToggles` and `FeatureToggleRegistry`.

### MOBAsmart tabs

MOBAsmart exposes Counter, SignalBox, Engines and Control. The pages are mounted
lazily by `AppTabHostPage` to reduce startup work.

## Configuration

| File | Role |
| --- | --- |
| `global.json` | Required .NET SDK feature band |
| `Directory.Packages.props` | Central package versions |
| `Directory.Build.props` / `.targets` | Shared build policy |
| `version.json` | MinVer settings |
| `MOBAflow/appsettings.json` | Shipped desktop defaults |
| `MOBAflow/appsettings.schema.json` | Settings schema |
| `MOBAflow/Build/Schemas/*.schema.json` | Build-time data schemas |

Important application sections are `Z21`, `RestApi`, `Speech`, `Application`,
`Counter`, `HealthCheck`, `Display`, `SignalBox`, `TrainControl`, `Layout` and
`FeatureToggles`.

## Build and test entry points

```powershell
dotnet build MOBAflow/MOBAflow.csproj
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj --framework net10.0-android
dotnet build MOBApi/MOBApi.csproj
dotnet test Test/Test.csproj
```

The WinUI project requires Windows tooling and MOBAsmart requires the Android
MAUI workload. Cross-platform projects and most tests can be built separately.
See `docs/BUILD-PERFORMANCE.md` for the clean Android Release AAB workflow,
fast local configurations, and coverage commands. For `dotnet restore`, `-f`
means `--force`; use `--framework` with `dotnet build` or `dotnet publish` when
framework selection is required.

Public checks are defined in `.github/workflows/`; additional Azure DevOps
pipelines remain under `.azure-pipelines/`.

## Legal and third-party surface

MOBAflow is MIT licensed. Direct package versions are managed centrally and
their license surface is summarized in `docs/THIRD-PARTY-NOTICES.md`. External
product names such as Roco Z21, Piko A, AnyRail and ESP32 identify compatibility
only; MOBAflow is independent from their vendors.
