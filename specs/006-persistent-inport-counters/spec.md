# Persistent local InPort counters

**Source Issue**: #139

**Spec Kit**: Required

## Governance and Traceability

Source: [Issue #139](https://github.com/ahuelsmann/MOBAflow/issues/139).
Decision: operator clarification on 2026-09-25 supersedes the issue's older
startup-zero, non-persistent, and host-counter requirements and the counter
lifetime described in spec 001.

## Requirements

- MOBAflow and MOBAsmart each own independent local counters, outside solution.json.
- Restore counts before accepting feedback. App, page, project, and solution changes
  never reset them. Timer history is transient and does not span application starts.
- The configured InPorts 1 through CountOfFeedbackPoints define the counter inventory.
  New inputs immediately start at zero; removed inputs and their saved counts disappear.
  Feedback for an unconfigured input cannot recreate a removed counter.
- Both statistics views offer individual set/reset and reset-all. Values are unsigned
  64-bit integers, entered without floating-point rounding. Invalid values are rejected.
- Restore, correction, reset, and inventory changes never publish counted feedback or
  execute workflows. Subsequent accepted feedback increments from the corrected value.
- Corrections invalidate undelivered activations for that input, while other inputs
  retain their queued activations. Reset-all invalidates all old activations.
- A real activation reaching an event-plan target again may execute that event again.
- Persist accepted feedback and explicit changes in order with atomic file replacement.
  Expose persistence errors; never silently replace unreadable state with zero counts.

## Acceptance

Verify restart restoration, independent app stores, inventory changes before feedback,
set/reset without events, repeated target execution, stale feedback isolation, concurrent
feedback during loading/saving, corrupt files, failed writes, and exact integer input.
Build shared logic, Windows UI, and Android UI without starting either app or hardware.

## Boundaries

No host/mobile counter synchronization, solution schema changes, new feedback source
types, or journey scheduler redesign. RFID/camera integration remains Issue #142.
