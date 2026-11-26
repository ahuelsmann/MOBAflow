# MOBAflow pages at a glance

This page lists every page of MOBAflow Desktop in the order and grouping of the
navigation pane, with what each page is for. MOBAsmart's four tabs follow at the
end. Details are in the [MOBAflow user guide](MOBAFLOW-USER-GUIDE.md) and the
[MOBAsmart user guide](MOBASMART-USER-GUIDE.md).

A **Preview** badge in the navigation marks a page that is usable but may still
change. In the shipped settings, **Timetable** and **Event Manager** carry it.
**Settings > Feature Toggles** can show or hide feature pages (except **Overview**,
which is always available); changes apply immediately without restarting the app.
Train Control and Journeys are shown in bold.

## MOBAflow Desktop

### Overview

| Page | What it is for |
| --- | --- |
| **Overview** | The selected project's operating state: Z21 telemetry and track power, feedback counters with their settings, lap progress, and the REST API, MOBAsmart sync and connected clients. |

### Train Control

| Page | What it is for |
| --- | --- |
| **Train Control** | Drive one project locomotive: speed slider (14, 28, or 128 DCC steps), direction, acceleration ramp, functions F0-F31, emergency stop, and the timetable stops of the current journey. |

### Journeys

| Page | What it is for |
| --- | --- |
| **Journeys** | Define journeys as an ordered list of stations and switch a journey active or inactive. |
| **Timetable** (Preview) | Plan services and manage the current operating session: services with their station stops, and recording of arrivals and departures. |
| **Workflows** | Author reusable workflows as a list of actions, as a list or a graph, and validate, dry-run and trace them. |
| **Event Manager** (Preview) | Assign workflows to a journey's events by InPort and trigger count, and reset the saved InPort counters. |
| **Journey Map** | Show the route and progress of the active journey. |

### Solution and rolling stock

| Page | What it is for |
| --- | --- |
| **Solution** | Manage the projects of the solution and assign each project its Z21, with the Z21 Finder. |
| **Stations** | Maintain the reusable stations with their platforms and optional city data. |
| **Locomotives** | Maintain locomotives: name, digital address, photo, functions, decoder/CV snapshots and printable passports; show where a locomotive is used. |
| **Passenger Wagons** | Maintain the passenger wagon library. |
| **Goods Wagons** | Maintain the goods wagon library. |
| **Trains** | Compose ordered trains from locomotives and wagons. |

### Track and signals

| Page | What it is for |
| --- | --- |
| **Track Plan** | Edit the track plan with Piko A pieces: place, connect, assign feedback points, validate and export SVG. |
| **Signal Box** | Lay out and operate signals and switches against the connected Z21, including Viessmann multiplex mappings. |
| **ESP32 Display** | Connect to one network display, read its capabilities and send test patterns. |
| **Matrix Images** | Draw 5x5 color images that are stored in the project. |

### Monitoring

| Page | What it is for |
| --- | --- |
| **Monitor** | Follow Z21 traffic and runtime activity as it happens. |
| **Recorder** | Record runtime events and operator commands with markers and notes, export or import a recording, and replay it without touching the layout. |

### Help and settings

| Page | What it is for |
| --- | --- |
| **Help** | Built-in help topics for getting started, train control, automation, the signal box, the track plan, monitoring, configuration and troubleshooting. |
| **Info** | What MOBAflow is, its applications, version and links. |
| **Settings** | Z21 connection timing, ESP32 display address, speech synthesis, REST API, application behavior, layout reset, feedback points, health check and feature toggles. |

The **Messages** area at the bottom of the main window is not a page; it lists
project diagnostics on every page.

## MOBAsmart tabs

| Tab | What it is for |
| --- | --- |
| **Counter** | Choose the project, see Z21 telemetry, switch track power, set up and read the lap counter, and upload a locomotive photo to MOBAflow. |
| **SignalBox** | Operate the signals and switches of the selected MOBAflow project. |
| **Engines** | Browse the synchronized locomotives with their photos and choose the one to drive. |
| **Control** | Drive the selected locomotive: STOP, speed slider and presets, direction and functions F0-F31. |
