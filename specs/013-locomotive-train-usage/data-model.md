# Data model: locomotive-to-train usage

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

All additions below are in-memory library/read models, not new persisted Domain fields.

## Existing relationship

`Project.Trains -> Train.Vehicles -> Vehicle(VehicleKind, VehicleId)` points to `Project.Locomotives` only when VehicleKind is Locomotive. Train.Vehicles is ordered; every position is meaningful, including repeated target references. Resolve within the same project. Never resolve through names, DCC addresses or a different vehicle collection.

## Proposed read values

| Value | Fields and constraints |
| --- | --- |
| Target identity | Current loaded context, non-empty ProjectId, fixed kind Locomotive, non-empty TargetId; a requested absent target is Missing, not unused |
| Occurrence location | Source category, ProjectId/name, non-empty unique TrainId/name, zero-based VehicleIndex, FieldPath `vehicles[index].vehicleId`, TargetId and display position index+1 |
| Finding | RuleCode, FindingId, Kind Use/MissingReference, Location, English message; copied values only |
| Coverage | Category EditedData/A/B/C, State Complete/Unknown, concrete Reason when Unknown, observation time when actually observed |
| Observation | RequestId, ProjectId, TargetState, immutable findings/coverage, UTC observation time and opaque validity receipt for runtime A |
| Request context | One cancellation-owned read invocation associated with the current session load, requested project/target; replaced on invalidation and never serialized |

Rule codes: `LTU001` means a valid locomotive use; `LTU002` means a train entry points to a missing locomotive. Coverage conditions are separate structured statuses, not synthetic missing-reference errors. Rule IDs remain constant across instances; finding IDs incorporate source, project, train, vehicle index and target. Observation time/request IDs do not change finding identity.

Preserve one occurrence per entry. Sort deterministically by project ID, source category, train ID and vehicle index; sort coverage by fixed category order EditedData/A/B/C. Do not merge equal-looking editor and runtime findings. UI shows 1-based positions while FieldPath/identity use the actual 0-based index; insertion can legitimately change subsequent occurrence IDs.

Empty/duplicate train IDs make a precise owner ambiguous. Return explicit invalid/unknown scope for that project/source, not fabricated unique IDs or A Complete. Empty VehicleId is a missing-target finding. A same-GUID wagon entry is ignored by this relationship; an unknown requested target has its own TargetState, and existing dangling entries are still diagnosable.

## Coverage invariants

- Complete means this category was successfully analyzed for the requested scope, including a valid empty result.
- Unknown always carries a reason; it is not represented by null, an empty list or a green success state.
- EditedData and A have independent states. A Complete plus B/C Unknown yields aggregate runtime Partial; A Unknown yields aggregate runtime Unknown. No aggregate runtime Complete exists in this slice.
- Target existence is source-specific: it can exist in the editor and be missing in A or the reverse.
- B/C are always Unknown / NotInspectedInSlice here. Never invoke RequestFactory or use traces/fleet/speed as substitutes.

## Receipt and ownership

The receipt binds existing host, owner and active-definition/context identities. Only the backend can create or inspect it. Its lifetime ends when its result/query is replaced or disposed; no global token registry retains it. It cannot recreate owners, prolong their operational lifetime, mutate them, be saved, or authorize deletion. [Runtime contract](contracts/runtime-query.md) specifies capture and revalidation.

The expected editable project is only an identity guard against a foreign loaded context. Its contents do not supply runtime findings. No mutable Project/Train/Vehicle escapes from the owner reader.

## Mapping to existing diagnostics

- MissingReference maps to existing Error severity, source and target IDs, preserving typed occurrence metadata for exact text/projection.
- Use maps to neutral Information only for the explicit usage query; it is excluded from error/warning counts.
- Unknown coverage is an explicit source-qualified information/status row; it cannot disappear under an empty-success summary. It must remain evident when error filters are changed, as specified in the presentation contract.
- Existing ProjectDiagnostic fields stay useful; add only the metadata required by this relationship. Do not parse Message to recover IDs, sort keys or navigation.

## Data effects

No changes to solution.json, CurrentSchemaVersion, serializers, configuration defaults, layouts or physical device state. No migration, compatibility path, history or secondary runtime state is introduced.
