# Implementation plan

**Source Issue**: #139

**Spec Kit**: Required

Source: [Issue #139](https://github.com/ahuelsmann/MOBAflow/issues/139).
The clarified requirements in [spec.md](spec.md) take precedence over older issue text.

1. Keep InPortCounterService as the sole local counter owner. Observe configured input
   count changes and add per-input revision checks for explicit corrections/removal.
2. Introduce an injectable asynchronous counter store. Platform hosts select distinct
   application-data files named inport-counters.json. Tests and isolated runtimes remain
   in-memory unless they explicitly provide a store.
3. Restore before runtime connection; buffer feedback during loading. Serialize writes,
   coalesce pending snapshots, and drain writes during asynchronous disposal. Surface
   load/save errors in snapshots and explicit command results.
4. Extend the existing local command gateway and shared statistics row with set/reset.
   Mobile routes these commands to its local runtime in every connection mode.
5. Add controls to existing statistics cards; validate integer text directly as ulong.
6. Cover state transitions, persistence failure/recovery, and command routing with
   targeted tests; run affected builds, line-ending and specification checks.

## Constitution Check

Keep platform storage paths in host registration, asynchronous IO behind the backend
store interface, and commands/projection in SharedUI. No platform dependency enters
the domain or backend. No application/hardware start is part of automated validation.

## Validation Strategy

Run the portable net10.0 suite excluding LiveE2E tests against a running host, compile
WinUI and Android, and run repository line-ending/specification checks. GitHub CI and
Sonar remain separate gates on the draft PR. Manual UI and layout acceptance remain open.

## Analysis before implementation

Current main already has one shared counter service and independent mobile projection.
Gaps are persistence, configured-input reconciliation, per-input commands, and controls.
Existing all-reset generation protects delayed journey evaluation; individual correction
needs a per-input revision so unrelated queued feedback remains valid. Startup restoration
must precede auto-connect and must not raise Counted. File replacement must preserve the
previous complete file when a write fails. No solution/domain persistence changes needed.
