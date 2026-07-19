# MOBAflow documentation

MOBAflow is a local-first ecosystem for operating and automating a model
railroad around a Roco Z21 command station.

## Choose an app

| App or component | Use it for | Guide |
| --- | --- | --- |
| **MOBAflow Desktop** | Create and operate layouts on Windows: rolling stock, journeys, feedback sequences, workflows, track plans, signal boxes, monitoring and displays | [MOBAflow user guide](MOBAFLOW-USER-GUIDE.md) |
| **MOBAsmart** | Operate from Android: lap counter, Z21 status and power, mobile signal box, locomotive fleet and train control | [MOBAsmart user guide](MOBASMART-USER-GUIDE.md) |
| **MOBApi** | Connect MOBAflow and MOBAsmart on the local network and expose REST/SignalR integration points | [Project reference](../PROJECT-REFERENCE.md#mobapi-endpoints) |
| **Remote display stack** | Develop and test RGB565 rendering, UDP transport and ESP32-S3 receiver firmware (preview integration) | [Display protocol](../../MOBAdisplay/docs/protocol.md) |

MOBAsmart can connect directly to the Z21 for feedback and locomotive control.
Connecting it to MOBAflow through MOBApi additionally provides the active
solution, fleet photos, signal-box data and live desktop runtime state.

## User guides

- [Installation and setup](INSTALLATION.md)
- [MOBAflow Desktop](MOBAFLOW-USER-GUIDE.md)
- [MOBAsmart](MOBASMART-USER-GUIDE.md)
- [Piper TTS setup](PIPER-TTS-SETUP.md)
- [Track statistics quick start](QUICK-START-TRACK-STATISTICS.md)
- [Viessmann signal mapping](VIESSMANN-SIGNAL-MAPPING.md)
- [Hardware and liability notes](../HARDWARE-DISCLAIMER.md)

## Developer documentation

- [Architecture](../ARCHITECTURE.md)
- [Project reference](../PROJECT-REFERENCE.md)
- [JSON validation](../JSON-VALIDATION.md)
- [Build performance](../BUILD-PERFORMANCE.md)
- [Spec Kit workflow](../SPEC-KIT.md)
- [Third-party notices](../THIRD-PARTY-NOTICES.md)
- [Contributing](../../CONTRIBUTING.md)

## Project status and support

MOBAflow is under active development. Some desktop navigation items carry a
**Preview** badge and may still change.

- Report bugs and request features through
  [GitHub Issues](https://github.com/ahuelsmann/MOBAflow/issues).
- Follow planned work on the
  [public Kanban](https://github.com/users/ahuelsmann/projects/1).
- Check [tags](https://github.com/ahuelsmann/MOBAflow/tags) and the
  [changelog](../../CHANGELOG.md) for version history.
- For Z21 hardware support, contact the hardware vendor. For safe operation,
  read the [hardware disclaimer](../HARDWARE-DISCLAIMER.md).
