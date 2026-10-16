# Presentation contract: existing Messages

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implementation and the authorized focused desktop Light/Dark and keyboard acceptance of existing Messages were completed on product source 8e40f8a5. Actual observations and the broader checks not performed (manual reload/receipt-race, model editing, screen reader, 200-percent system font/DPI, Android/TalkBack and hardware) are recorded in the [quickstart](../quickstart.md).

## Read interaction

The locomotive page exposes one read-only `Show usage` command for an actual selected locomotive/project. The focused SharedUI projection captures edited-data findings on the established model thread, then queries the matching runtime owner asynchronously. No Delete command is modified. The existing Messages area receives the current query's source-qualified use/coverage rows; no new page or navigation destination.

The existing locomotive header's magnifying-glass button and existing context menu forward this same command. The Messages expander opens for the result; its coverage summary sits above the existing filtered list.

The query remains callable for any project in the loaded solution, including a non-selected project's background diagnostics. This never equates same-ID locomotives across projects. Normal project diagnostics use the pure analyzer for missing-target errors; valid uses appear only for an explicit usage query.

## Visible information

- `Edited project data` and `Loaded runtime definition` name the source and project.
- A valid use states `Used in: Train <name> - vehicle <position>` with locomotive identity and exact structured location. It is neutral Information, not an error/warning.
- A missing reference states `Missing locomotive reference: Train <name> - vehicle <position>` as Error; preserve target ID when no name exists.
- Source-qualified zero says `No train references found in this loaded definition`, never `Unused` or `Safe to delete`.
- Runtime summary always shows the separate loaded-definition (A), retained-definition (B) and operative-use (C) coverage and successful observation time (check time for Unknown). B/C read `Not checked in this slice`. Failure/unavailability states the reason.

Names are user data and remain unchanged/escaped by the UI binding/rendering path. Do not show local paths or raw runtime objects. Position displayed is 1-based; underlying location index is 0-based.

## Existing list behavior

Use ProjectDiagnosticsService for missing-target findings and the existing Messages collection/filter path for projection. Stable source-scoped identities replace active results on refresh. Keep editor and runtime findings separate. Repair removes old findings only after successful refresh of that source; failed runtime refresh replaces old A-Complete evidence with visible Unknown, not stale success.

Error/warning filters apply to actual severities. A short coverage summary inside the existing Messages expander remains visible regardless of filtering Information rows, so hiding neutral uses cannot hide Unknown runtime coverage. This is a small addition to the existing area, not a separate diagnostics platform. If no valid query is selected, clear prior query rows/summary while normal project diagnostics remain. Normal Unknown coverage statuses carry typed coverage and remain visible independently of all severity filters even without an explicit query; this does not raise neutral use severity or fabricate precise missing-reference errors.

No click-through object selection/navigation is promised. Locations provide enough text to identify the train/entry for manual inspection.

## Lifecycle/threading

Use existing session load/model/selection notifications. A request-local cancellation/context invalidates every result from an earlier load, even when Solution/ProjectId values match. Use the backend receipt check immediately before applying runtime A; wrong/disposed/replaced owners are discarded/Unknown. Runtime snapshot callbacks only trigger coalesced receipt checks, not use inference.

Raw all-runtime callbacks marshal UI changes through existing IUiDispatcher; existing decorated EventBus subscriptions need no extra dispatch. Dispose unsubscribes and cancels. The original Delete/Save/Reset/command behavior is unchanged by running the query. Automatic saving that independently follows an operator edit is not an analysis side effect; tests isolate the analysis invocation.

## Acceptance

Test filters, repeated refresh, corrected references, selection changes, late replies and partial source failure with fakes. Later inspect existing Messages in Light/Dark and by keyboard after separately authorized app launch. No safety improvement or human reaction-time measurement is claimed.
