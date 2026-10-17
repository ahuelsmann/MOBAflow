# Data model: Train consist card

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

This is a proposed in-memory presentation contract. Existing Domain models and serialized Solution data are unchanged.

## TrainCardSnapshot

Immutable result colocated with `SharedUI/Service/TrainCardBuilder.cs`.

| Field | Constraint |
| --- | --- |
| ProjectId, TrainId | Preserve the observed GUIDs; never generate replacement source identities. |
| ProjectName, TrainName | Copy user-authored text; blank values get explicit unnamed display labels without writing back. |
| CreatedAt | Capture once as DateTimeOffset with a visible offset; never recapture for printing. |
| SourceLabel | Exactly "Planned consist · Edited Solution data". |
| SourceState | Available or Unavailable; an unavailable occurrence collection is not an empty consist. |
| SourceDiagnostics | Immutable copied list; invalid project/train identity is disclosed. |
| Positions | Immutable copied sequence; one element per source occurrence, including malformed/null slots. |

No Domain object, editor ViewModel, mutable collection, delegates to source data, runtime handle or command is retained. The builder checks project membership and duplicate project IDs against the owning Solution, then resolves vehicles only in the selected project. A stale selected train outside the current project is Unavailable. Empty/duplicate project or train IDs are disclosed; their observed data may still be shown but cannot claim unique external diagnostic mapping.

## TrainCardPosition

| Field | Constraint |
| --- | --- |
| SourceIndex | Zero-based original index; never compact after an invalid reference. |
| Position | Exactly SourceIndex + 1, including repeated references. |
| VehicleId, VehicleKind | Preserve the raw reference values when present; null slots retain their index and a null-entry diagnostic. |
| Name, KindLabel, IdentityText | Copied display strings; full GUID/type remain available and are not claimed to be a physical identity. |
| IsReversed | Copy the occurrence's `Vehicle.IsReversed`; presented as text ("Reversed"), never by visual mirroring or color alone. |
| ConfiguredDccAddress | Copy the nullable locomotive DigitalAddress only; null displays "DCC not configured", never a fabricated number. |
| Diagnostics | Immutable position-local list, including reference location and a text explanation. |

Resolution: select the declared kind's collection in this project only. A non-empty ID with exactly one matching record resolves. Zero matches are MissingReference; duplicate matches are AmbiguousReference; empty ID, undefined kind or null occurrence is InvalidReference. A match in another kind or project is never a substitute. Do not reject all rows because one row is invalid. Optional manufacturer/article metadata and repeat badges are not required.

## TrainCardDiagnosticLocation

Copied source scope, project ID, train ID, occurrence index and field path. Use `vehicles[index].vehicleId` for target-ID diagnostics, `vehicles[index].vehicleKind` for unknown kinds and `vehicles[index]` for null slots. Present position as index + 1. Location never depends on parsing display text. LTU002 association is restricted to compatible missing-locomotive findings with unambiguous source identities; other kinds use card-local diagnoses without adding global Messages categories.

## Print request/result and page fragments

`ITrainCardPrintService` accepts the already materialized snapshot and cancellation. It returns an explicit Completed, Cancelled, Unsupported or Failed status; a spooler completion is not physical-print acceptance.

A measured fragment records position/field identity, ordered text range, measured extent and continuation association. Page planning consumes fragments in order, reserves measured header/footer extents, and reports page x of y. No content fragment can disappear, duplicate or change its copied data. If a fragment cannot fit even on an empty usable page, return Failed with an explanation rather than an infinite sequence of blank pages. Retrying/re-paginating keeps CreatedAt and all source fields unchanged.

## Lifetime

Closed -> Open with a new copied snapshot -> Printing -> Open -> Closed.

Cancellation/failure returns to the same Open snapshot. Close releases transient state and print registrations; no domain write, card history, background refresh or deferred apply exists. This is ephemeral UI state required to display/print a card, not persistent per-run workflow state.
