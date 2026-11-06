# Implementation Plan: The Piper timeout covers the whole run

**Source Issue**: #391

**Spec Kit**: Required

**Chronology**: Written on 2026-10-24 after the implementation in `941806e6`; it records the approach that was taken.

Input: [spec.md](spec.md).

## Approach

1. In `Speech.Piper/PiperProcessRunner.RunAsync`, create a `CancellationTokenSource(timeout, TimeProvider)` before the
   process starts and link it with the caller's token.
2. Pass the linked token to the stdin write, both output reads and `WaitForExitAsync`; register a kill of the process
   tree on it, so a blocked write ends with a cancellation or a broken pipe.
3. Map a cancellation or broken pipe caused by the timeout to the existing timeout result; turn a broken pipe caused by
   the caller into an `OperationCanceledException`.
4. Register the new test file in the planned `speech-piper` mutation lane.

## Constitution Check

- The change stays inside the Windows-only `Speech.Piper` assembly; the provider contract is unchanged.
- The existing `TimeProvider` seam is kept; no new dependency, no legacy path.
- Tests use a temporary batch file standing in for Piper; no real Piper, speech output or hardware is used.

## Validation Strategy

- Windows-only tests (`#if WINDOWS`, `[Platform("Win")]`): a stand-in that never reads stdin times out; one that reads
  returns its output; caller cancellation throws. The timeout test is checked to hang on the previous code.
- Windows suite with a hang guard, analyzer gates, mutation lane registration, documentation, Spec Kit, line-ending and
  secrets checks; CI and Sonar.
