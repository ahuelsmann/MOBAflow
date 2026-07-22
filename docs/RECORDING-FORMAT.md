# MOBAflow recording format

Recorder artifacts use the `.mobarecording.json` extension and are independent of solution files. Version 1 uses the format identifier
`mobaflow-recording` and accepts exactly `formatVersion` `1.0`.

## Canonical representation

The dedicated writer emits UTF-8 JSON with LF line endings and a fixed property order. Entity references are sorted by stable kind and GUID. Payload object
properties are sorted ordinally, numeric values are normalized, timestamps use UTC ISO-8601 with seven fractional digits, GUIDs and SHA-256 values use lowercase
text, and journal order is preserved.

`integrity.entriesSha256` is the lowercase SHA-256 of the compact canonical `entries` array. Session metadata, application version, project identity, options,
summary, and formatting whitespace are intentionally excluded from that hash.

## Payload allow-list

Only explicitly registered recording mappers may create persisted payloads. A mapper owns a stable lowercase dotted type key and a compact JSON object containing
only fields approved for that event. Payloads must never contain arbitrary event or runtime objects, raw Z21 packets, credentials, tokens, network endpoints, file
paths or contents, audio, scripts, photos, exception messages, or stack traces.

Known imported type keys are checked by their registered `IRecordingPayloadValidator`. The importer never resolves a CLR type name from JSON. Unknown type keys are
retained for display but are always marked `displayOnly`; they cannot become replay operations.

Free-text filtering searches mapper-provided `displayText`, not raw payload JSON.

### Initial replay-applicable event schemas

The first capture slice registers these exact type keys. Property names are case-sensitive; missing, duplicate, unknown, or incorrectly typed properties make a known payload invalid.

| Type key | Allowed payload properties |
| --- | --- |
| `z21.connection.established`, `z21.connection.lost` | `connected` |
| `z21.track-power.changed` | `isOn` |
| `z21.xbus-status.changed` | `emergencyStop`, `trackOff`, `shortCircuit`, `programming` |
| `z21.system-state.changed` | current, temperature, voltage, and central-state scalar values |
| `z21.feedback.activated` | `inPort` |
| `z21.signal-aspect.changed` | `signalId`, `aspect`, `previousAspect` |
| `z21.switch-position.changed` | `switchId`, `isLeft`, `previousPosition` |
| `runtime.state.changed` | connection, track-power, connection-attempt, disconnect, emergency, short-circuit, programming, and acknowledgement booleans only |
| `journey.transition` | project/journey/run IDs, transition kind, feedback progress, optional input/station, station index, and active state |

The runtime mapper intentionally excludes broad snapshot collections, operator status text, serial/firmware values, endpoints, and mutable domain/runtime objects. Journey entries originate from immutable events published at authoritative `JourneyManager` transitions before legacy mutable callbacks.

## Isolated replay

RecorderPage replays validated artifacts only through `IRecordingReplayService`. The service creates a fresh `IIsolatedReplayRuntime` directly; that runtime has no production service-provider scope and no dependency on `IMobaRuntime`, `IZ21`, the root EventBus, network clients, speakers, displays, scripts, or production file services. It projects allow-listed payloads into private in-memory state only.

`IRecordingReplaySafetyGate` reads the production runtime snapshot but is not an execution target. Play, single-step, and seek fail closed while a live Z21 connection is active, and playback rechecks the gate after every scheduled wait before applying the next entry.

Replay preserves journal order and supports 0.25x, 0.5x, 1x, 2x, 4x, and 8x speeds. A speed change affects future waits only. Display-only and unknown entries advance position as visible skips but are never applied. Seek resets the isolated runtime and reapplies from the beginning with delays suppressed. Pause cancels the current wait before the next entry, while reset/cancel discards projected state and returns the loaded artifact to position zero.

## Defensive limits

| Limit | Version 1 value |
| --- | ---: |
| JSON nesting depth | 32 |
| Session name or type key | 128 characters |
| Category, source, severity, or entity kind | 64 characters |
| Application version | 64 characters |
| Project display name | 256 characters |
| Display text, marker, or note | 4 KiB characters |
| Canonical payload per entry | 16 KiB |
| Entity references per entry | 64 |
| Default entries per session/import | 250,000 |
| Default artifact/import byte ceiling | 64 MiB |

Import limits may be configured below these ceilings for constrained hosts. The byte ceiling is checked before parsing. Entry count is checked before allocating the
entry collection. The importer also rejects duplicate or unknown envelope fields, noncanonical IDs and timestamps, invalid or decreasing sequences and elapsed
offsets, inconsistent summaries, invalid known payloads, and integrity mismatches.
