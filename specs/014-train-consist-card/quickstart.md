# Validation guide: Train consist card

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

**Current state**: Planning only. No product implementation, prototype, UI launch, rendered card, print job, device or hardware check has been performed. The commands/scenarios below describe later implementation validation and are not authorization to run the app.

## Prerequisites for later implementation

Obtain the existing queue's implementation allocation and the required explicit app-launch/print acceptance assignment. Use the assigned isolated worktree and current repository instructions. Read SDK/targets from global.json and project files; do not install or upgrade anything as part of this plan.

Do not use MOBAflow/solution.json or any private project data. Build synthetic fixtures in test memory. For actual UI acceptance, provide a specifically assigned synthetic session/fixture through the existing supported loading path; do not hand-edit a user's active file or alter editor duplicate rules to create test data.

## Synthetic baseline

One project, one selected train, fixed creation time `2026-10-17T21:00:00+02:00`. Use deterministic, distinct valid GUIDs; include another project and other-kind records with intentionally equal IDs in boundary cases.

| Position | Reference | Expected |
| --- | --- | --- |
| 1 | Locomotive L1, "Regional 01", configured DCC 3 | Full name/type/ID and configured address |
| 2 | Passenger wagon P1, "Regional coach" | Own identity and kind |
| 3 | Goods wagon G1, a long name with markup and unbroken text | Full wrapped data, no execution/truncation |
| 4 | Locomotive L2, "Regional 01", no DCC, reversed | Distinct full ID, explicit unconfigured address, "Reversed" text |
| 5 | Passenger wagon P1 again | Separate occurrence at position 5 |
| 6 | Missing goods-wagon ID G404 | Diagnostic at position 6 |
| 7 | Goods wagon G2, "Regional coach" | Own typed identity despite similar name |

Also test: empty list; missing locomotive with compatible LTU002 coordinates; same GUID only in another project; same GUID only in another kind; empty target ID; undefined kind; null slot; duplicate target IDs; invalid project/train IDs; blank names; stale train selection; unavailable/null occurrence list. Repeat a reference more than twice. Mutate the original model after generation to prove the card is copied. Do not make a null source appear empty.

## Automated checks after implementation

Create the intended fixtures before using these filters, and confirm nonzero discovery/execution.

```powershell
dotnet build SharedUI/SharedUI.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~TrainCard"
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
```

Require strict mocks to prove zero calls to Solution saves, runtime owner start/update and operator-command paths, with before/after source equality. Verify the print fake receives the exact generated snapshot and timestamp. Pagination fixtures compare the complete ordered fragment sequence before and after pagination, verify page count/continuation metadata, fit-boundary and overheight cases, and prove progress or explicit failure for impossible page geometry. Existing editor tests must remain unchanged in behavior. CI retains applicable full-suite/analyzer requirements.

## Actual Windows UI acceptance, later and explicitly assigned

1. Open the selected train card with keyboard input. Verify provenance, timestamp, all seven positions, full identities, "Reversed" text at position 4 and local diagnosis; no editable train controls.
2. Traverse the dialog and actions using Tab/Shift+Tab; open/cancel print preparation and verify the card stays open with focus on Print; Close and verify focus returns to the invoking control. Repeat open/close and print cancellation to detect duplicate handlers or stale cards.
3. Inspect Light and Dark at normal and 200% Windows text size, including narrow viewport and a 300-character/unbroken name. Reach all content through wrapping/scrolling.
4. If the source changes while a card is open, verify it retains its copied contents. Explicit Close/reopen generates a fresh timestamp and contents.
5. Check absent/stale selection, print unsupported/error status and explicit retry. Do not create a runtime or operate the layout to perform these checks.

## Actual A5 output acceptance, later and explicitly assigned

Use A5 portrait on a supporting driver. Record actual page size/imageable margins and the Windows text-size setting used. Compare all screen fields with native preview and the resulting digital A5 output. Use the 7-position fixture, a 60-position fixture and one single position whose wrapped content exceeds one page; also exercise a train/project name taller than one page and option changes.

Verify all text including "Reversed" at position 4, page x of y, the bounded repeated card identifier with the complete identity continuing from page 1, uninterrupted positions and identified continuations. Body text must be at least 11 points and must scale up, not shrink, when the Windows text size is enlarged. Impossible imageable area must return an honest failure. An ordinary 7-position card is not required to fit one sheet.

A physical A5 print remains a separate actual check. Record printer/media/scale, evidence of margins/readability/content completeness and outcome. Digital output, native preview and spooler success cannot substitute for it. No railroad hardware acceptance is required by this feature.

## Evidence record

Record commit/comparison, date/time+zone, configuration, fixture, executed command/action, actual result, and limits. Keep rows separate for automated tests, Windows compile, keyboard/focus, Light/Dark/200%, digital A5, physical A5 and CI/Sonar. All feature rows are currently **NOT EXECUTED**. If actual print acceptance is unavailable, name the exact remaining check rather than marking the feature complete.

## Documentation and integration

Update docs/wiki/MOBAFLOW-USER-GUIDE.md, docs/PROJECT-REFERENCE.md and docs/ARCHITECTURE.md when the feature actually exists. Preserve the edited-data and non-certification boundary, and reassess after review fixes against integrated main. Follow the existing Chief Project Manager Ready and Claude review/fix/re-review/integration route.
