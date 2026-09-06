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

## Relationship to journeys

Standalone statistics do not require a solution. Journeys use their own ordered
`FeedbackSequence`, edited in the desktop Event Manager; they no longer depend
on one global `Journey.InPort` value.

## Related documentation

- [MOBAflow Desktop guide](MOBAFLOW-USER-GUIDE.md)
- [MOBAsmart guide](MOBASMART-USER-GUIDE.md)
- [JSON validation](../JSON-VALIDATION.md)
