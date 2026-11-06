# Feature Specification: The Piper timeout covers the whole run

**Feature Directory**: `018-piper-timeout`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/391

**Created**: 2026-10-24

**Status**: Implemented in draft [PR #395](https://github.com/ahuelsmann/MOBAflow/pull/395) at `941806e6`.

**Spec Kit**: Required

**Chronology**: The defect was found by reading the code in the architecture audit on 2026-10-23. The implementation
(`941806e6`, 2026-10-24 15:55 CEST) came first, with a test that hung on the previous code. This specification, the
plan, the tasks and the analysis were written afterwards on 2026-10-24, at the maintainer's request through the Chief
Project Manager. They record what was decided and verified; they do not claim an earlier planning run.

**Input**: Issue #391.

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #391

**Related Work**: #389 and #390 (the other audit findings). [specs/009-tts-providers](../009-tts-providers/spec.md)
(#136) defines the speech provider contract that uses `PiperProcessRunner`; that contract is unchanged.

## User Scenarios & Testing

### User Story 1 - A hanging Piper ends after the configured timeout (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a Piper process that stops reading its input (hanging start-up, or a long text that fills the pipe buffer),
   **When** an announcement is synthesized, **Then** the run ends after the configured timeout, the process tree is
   killed and the result reports "Piper timed out after N seconds."
2. **Given** Piper reads its input and exits normally, **Then** its output is returned as before.
3. **Given** the caller cancels while the text is being written, **Then** an `OperationCanceledException` is thrown.

## Requirements

- **FR-001**: The timeout MUST start before the text is written to stdin and cover writing, closing stdin, reading the
  output and waiting for the exit.
- **FR-002**: On timeout or caller cancellation, the process tree MUST be killed.
- **FR-003**: The timeout result and the caller-cancellation exception MUST stay as before; `ProbeAsync` follows the same
  rules.

## Success Criteria

- **SC-001**: A test with a stand-in that never reads stdin times out; it hangs on the previous code.
- **SC-002**: The speech provider contract and its callers are unchanged.
