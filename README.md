<div align="center">

# MOBAflow

### Event-driven automation for model railroads

Control trains, plan journeys and automate layout events across Windows and
Android, with an evolving ESP32-S3 display stack.

[![License: MIT](https://img.shields.io/badge/license-MIT-2ea44f?style=flat-square)](LICENSE)
[![Windows](https://img.shields.io/badge/desktop-Windows-0078D4?style=flat-square&logo=windows)](#mobaflow-desktop)
[![Android](https://img.shields.io/badge/mobile-Android-3DDC84?style=flat-square&logo=android&logoColor=white)](#mobasmart)
[![Quality](https://github.com/ahuelsmann/MOBAflow/actions/workflows/quality.yml/badge.svg?branch=main)](https://github.com/ahuelsmann/MOBAflow/actions/workflows/quality.yml)

[Website](https://ahuelsmann.github.io/MOBAflow/) ·
[Features](#features) ·
[Screenshots](#screenshots) ·
[User guides](docs/wiki/INDEX.md) ·
[Roadmap](ROADMAP.md) ·
[Contribute](CONTRIBUTING.md)

<a href="docs/images/mobaflow-overview.png">
  <img src="docs/images/mobaflow-overview.png" alt="MOBAflow desktop application showing live railroad state and journey automation" width="900" />
</a>

</div>

MOBAflow turns feedback from a Roco Z21 layout into useful actions: advance a
journey, run a workflow, play an announcement or update a signal. Project data
and layout communication stay on the local network.

## The apps

### MOBAflow Desktop

The Windows control center is where layouts are created, operated and monitored.

- Control locomotive speed, direction and functions F0-F31.
- Manage locomotives, passenger and goods wagons, train consists, maintenance
  plans, decoder snapshots and printable locomotive passports.
- Create stations, journeys and ordered feedback sequences; inspect progress in
  the Journey Map and edit feedback behavior in the Event Manager.
- Build reusable workflows for commands, audio, announcements, signals, scripts,
  journey transitions and destination displays.
- Draw or import track plans, connect Piko A track pieces, validate topology and
  export the result as SVG.
- Configure and operate a signal box, including Viessmann multiplex signals.
- Monitor Z21 traffic, feedback, track power and system telemetry.
- Design reusable 5x5 matrix images and inspect supported display models.
- Generate local announcements with Piper TTS or Windows Speech.

[Read the MOBAflow user guide](docs/wiki/MOBAFLOW-USER-GUIDE.md)

### MOBAsmart

The Android companion keeps the most useful controls close to the layout. It can
connect directly to the Z21 and optionally synchronize with MOBAflow through
MOBApi.

- **Counter:** feedback-point lap counting, timer filtering, telemetry and track
  power.
- **Signal Box:** use the active signal and turnout plan synchronized from
  MOBAflow.
- **Engines:** browse the synchronized locomotive fleet and its photos.
- **Control:** select a locomotive, drive it and switch functions F0-F31.
- Discover MOBAflow on the LAN, receive live runtime snapshots and keep a local
  cache for faster startup.
- Capture and upload rolling-stock photos to the desktop library.

[Read the MOBAsmart user guide](docs/wiki/MOBASMART-USER-GUIDE.md)

### MOBApi

MOBApi is the local bridge between MOBAflow, MOBAsmart and other integrations. It
provides REST endpoints and SignalR hubs for solution synchronization, runtime
snapshots, remote commands, journey progress, feedback sequences, client status
and photos. MOBAsmart can discover it automatically on the same LAN.

MOBApi can run by itself or be started automatically by MOBAflow.

### Remote displays

`MOBAdisplay` provides RGB565 renderers and UDP frame transport, and the current
PlatformIO firmware receives the line protocol, stores Wi-Fi credentials locally
and displays completed frames. This is still an integration foundation: the
desktop Display page currently identifies supported models, while the destination
display workflow action is skipped unless a display service is wired in. The
older standalone Arduino sketch remains a hardware color-test only.

## Features

| Area | What it adds to an operating session |
| --- | --- |
| Z21 integration | Direct UDP control, feedback events, track power and live telemetry |
| Journeys | Ordered stations, feedback sequences, progress persistence and end-of-journey behavior |
| Event Manager | Visual editing of feedback steps and stop transitions |
| Workflows | Sequential or parallel actions triggered by layout events |
| Rolling stock | Locomotives, wagons, consists, photos, maintenance and decoder records |
| Track plan | AnyRail import, drag and drop, snapping, topology validation, Undo/Redo and SVG export |
| Signal box | Signals, switches, routes and Viessmann aspect control |
| Mobile operation | Lap counter, locomotive control, signal box and synchronized project data |
| Local audio | WAV effects plus offline Piper TTS and Windows Speech announcements |
| Displays | LED matrix images plus preview-stage RGB565 rendering, UDP transport and ESP32 firmware |

Features marked **Preview** inside the desktop navigation are available for
testing but may still change.

## Screenshots

<table>
  <tr>
    <td><a href="docs/images/train-control.png"><img src="docs/images/train-control.png" alt="Train Control with locomotive selection, speed and F0-F31 functions" /></a></td>
    <td><a href="docs/images/journey-management.png"><img src="docs/images/journey-management.png" alt="Journey editor with stations and journey properties" /></a></td>
  </tr>
  <tr>
    <td align="center"><strong>Train control</strong></td>
    <td align="center"><strong>Journey management</strong></td>
  </tr>
  <tr>
    <td><a href="docs/images/trackplan-editor.png"><img src="docs/images/trackplan-editor.png" alt="Visual track plan editor with Piko A track library" /></a></td>
    <td><a href="docs/images/display-page.png"><img src="docs/images/display-page.png" alt="Remote display configuration and preview" /></a></td>
  </tr>
  <tr>
    <td align="center"><strong>Track plan</strong></td>
    <td align="center"><strong>Remote displays</strong></td>
  </tr>
</table>

## Status and documentation

MOBAflow is under active development. GitHub is the live source for changing
project information:

- [Tags](https://github.com/ahuelsmann/MOBAflow/tags) and
  [changelog](CHANGELOG.md) for versions
- [Roadmap](ROADMAP.md), [Kanban](https://github.com/users/ahuelsmann/projects/1)
  and [issues](https://github.com/ahuelsmann/MOBAflow/issues) for planned work
- [User documentation](docs/wiki/INDEX.md) for installation and app guides
- [Project reference](docs/PROJECT-REFERENCE.md) and
  [architecture](docs/ARCHITECTURE.md) for technical details

## Safety and network scope

MOBAflow can send commands to real model railroad hardware. Read the
[hardware and liability notes](docs/HARDWARE-DISCLAIMER.md) before operating a
layout.

MOBAflow, MOBAsmart and MOBApi are intended for a trusted private LAN. MOBApi
does not currently provide API-key authentication and must not be exposed to the
public internet.

MOBAflow is an independent open-source project. Product names and trademarks
belong to their respective owners; see the
[third-party notices](docs/THIRD-PARTY-NOTICES.md).

## License

MOBAflow is available under the [MIT License](LICENSE).
