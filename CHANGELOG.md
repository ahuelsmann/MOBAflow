# Changelog

Notable user-facing changes are recorded here. MOBAflow versions are identified
by signed [Git tags](https://github.com/ahuelsmann/MOBAflow/tags). GitHub release
packages are not published yet.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
the version tags follow [Semantic Versioning](https://semver.org/).

## Unreleased

### Added

- Event Manager for visually editing ordered journey feedback sequences.
- Feedback-sequence progress persistence and MOBApi endpoints for mobile/runtime
  synchronization.
- Isolated runtime deployment for the MOBApi process started by MOBAflow.
- PlatformIO ESP32-S3 firmware with Wi-Fi provisioning, UDP line-frame receive,
  RGB565 framebuffer assembly and ST7789 output.
- Spec Kit workflow, focused mutation-test lanes and coverage thresholds.

### Changed

- Journeys now use explicit ordered feedback steps instead of one global
  `Journey.InPort` trigger.
- Stations remain physical project entities; the former virtual-station and
  station-list-mode concepts were removed.
- JSON validation and domain conversion for current workflow payloads and track
  layout data were tightened.
- Documentation was reconciled with the current apps, runtime, API and display
  firmware; the README now focuses on apps and features.

## 0.2.0 - 2026-07-18

### Added

- Locomotive maintenance plans and deterministic due-state calculation.
- Decoder/CV snapshots with validation, comparison and export.
- Printable, privacy-safe locomotive passports.
- Feedback-triggered locomotive whistle rules with delay, duration and
  cancellation handling.
- Digital-address conflict detection and structured project diagnostics.
- GitHub-native roadmap, Kanban guidance, issue forms, quality workflow and
  signed-tag Release Studio.
- Transitive dependency/vulnerability reporting and release-package checks.

### Changed

- Updated the repository to the .NET 10 SDK and current servicing package line.
- Consolidated shared UI control through `IMobaRuntime` and immutable runtime
  snapshots.
- Improved WinUI layout persistence, navigation and rolling-stock management.

## 0.1.1 - 2026-02-26

### Added

- MinVer-based version calculation from plain signed Semantic Version tags.
- Build and documentation updates for the Windows, Android and API projects.

### Changed

- Improved repository configuration and development guidance.

## Earlier preview history

Before the signed `0.1.1` tag, the repository established the initial MOBAflow
preview: direct Z21 UDP communication, journeys, workflows, track-plan and
signal-box experiments, Windows/Android hosts, MOBApi discovery, speech/audio,
runtime snapshots and the first display renderer.

Detailed commit-level history remains available in
[Git](https://github.com/ahuelsmann/MOBAflow/commits/main/).
