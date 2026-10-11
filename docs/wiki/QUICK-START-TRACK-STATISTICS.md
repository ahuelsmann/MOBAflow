# Track statistics quick start

Track statistics count Z21 feedback events per input and calculate lap timing and
progress. They can run without a loaded solution.

## Configure the counter

In MOBAflow Desktop, open **Overview** and use **Counter Settings**. In
MOBAsmart, use **Lap Counter Setup** on the Counter tab.

Set:

- the number of feedback points to display;
- the target lap count;
- whether duplicate-event timer filtering is enabled; and
- the filter interval.

Changes are persisted automatically; there is no separate Save button.

Counts are also saved locally and restored after an app restart. Timing values
are not restored: the first new activation starts a new timing history.
Desktop and Android keep separate local counter files; resetting the phone's
counters does not reset the desktop's counters.

## Input mapping

The counter uses a direct mapping:

| Z21 input | Counter row |
| --- | --- |
| InPort 1 | Feedback Point 1 |
| InPort 2 | Feedback Point 2 |
| InPort 3 | Feedback Point 3 |

Increase the configured feedback-point count if a higher input should appear in
the statistics. InPort `0` is treated as disabled/not assigned.

## Operate

1. Connect the app directly to the Z21.
2. Confirm the Z21 status is online.
3. Trigger a feedback sensor.
4. Verify that its row updates count, progress, last feedback and timing data.

The timer filter suppresses repeated events from the same input inside the
configured interval. This is useful for long trains or noisy contacts, but an
interval that is too long can also hide legitimate laps.

Use **Set** on a counter row to enter a non-negative whole number, or **Reset**
to set that input to zero. These corrections are saved and clear the input's
timing history. **Reset all counters** clears all counts and timing history.
Neither restoring saved counts nor setting them manually generates feedback.

If loading the saved counts fails, the app reports the problem and counting
stays disabled. Resolve the storage problem and retry, or intentionally reset
all counters to replace the saved values.

## Relationship to journeys

Standalone statistics do not require a solution. On the desktop, the same InPort
counters drive journey events: an active journey runs an event's workflow when
an accepted feedback activation brings the counter to its configured count.
Resetting a count can let a later activation reach that count again; the reset
itself does not run a workflow. Android's local lap counters do not replace or
reset these desktop counters. Edit journey events in the desktop Event Manager.

## Related documentation

- [MOBAflow Desktop guide](MOBAFLOW-USER-GUIDE.md)
- [MOBAsmart guide](MOBASMART-USER-GUIDE.md)
- [JSON validation](../JSON-VALIDATION.md)
