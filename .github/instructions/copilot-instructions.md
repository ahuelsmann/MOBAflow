---
description: 'Central index and knowledge map for all instruction documents. References only existing files; see .copilot-todos.md for dynamic cross-session knowledge.'
applyTo: '**'
---

# Copilot Instructions Index

**Purpose:** Central index for all instruction documents.  
**Audience:** Copilot (primary), Developers (secondary).  
**Style:** Strict, deterministic, machine‑optimized.

**Important:** This is a minimal index of **active files**. For dynamic cross-session knowledge and complete inventory, see [.copilot-todos.md](./.copilot-todos.md).

---

## Project Overview: MOBAflow

**MOBAflow** is an event-driven automation solution for model railroads (German: Modellbahn). The system enables complex workflow sequences, train control with station announcements, and real-time feedback monitoring via direct UDP connection to the Roco Z21 Digital Command Station.

### Core Features

| Feature | Description |
|---------|-------------|
| **Z21 Direct UDP Control** | Real-time communication with Roco Z21 command station |
| **Journey Management** | Define train routes with multiple stations |
| **Text-to-Speech** | Azure Cognitive Services & Windows Speech for announcements |
| **Workflow Automation** | Event-driven action sequences |
| **MOBAtps Track Plan System** | Visual track layout editor with drag & drop |
| **Track Libraries** | Extensible track system support (Piko A-Gleis active, more planned) |
| **Multi-Platform** | WinUI (Windows), MAUI (Android), Blazor (Web) |

### Technology Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 10 |
| **UI Frameworks** | WinUI 3, .NET MAUI, Blazor Server |
| **MVVM** | CommunityToolkit.Mvvm |
| **Logging** | Serilog (File + In-Memory Sink) |
| **Speech** | Azure Cognitive Services, Windows Speech API |
| **Networking** | Direct UDP to Z21 (no external dependencies) |
| **Testing** | NUnit |

### Architecture (Clean Architecture)

```
Domain (Pure POCOs)
  ↑
Backend (Platform-independent logic)
  ↑
SharedUI (Base ViewModels)
  ↑
WinUI / MAUI / Blazor (Platform-specific)
```

### Track Plan System Architecture

```
TrackPlan (Domain)
  ↑
TrackPlan.Renderer (Geometry/Layout)
  ↑
TrackPlan.Editor (ViewModels/Commands)
  ↑
TrackLibrary.PikoA (Track Templates)
```

### Key Terminology

| Term | Meaning |
|------|---------|
| **MOBA** | Short for **Mo**dell**ba**hn (Model Railroad) |
| **MOBAflow** | Main WinUI desktop application |
| **MOBAsmart** | Mobile app (MAUI/Android) |
| **MOBAdash** | Browser-based dashboard (Blazor) |
| **MOBAtps** | Track Plan System |
| **Z21** | Roco Z21 Digital Command Station (DCC controller) |
| **Journey** | A train route with multiple stations |
| **Workflow** | Event-driven action sequence |
| **FeedbackPoint** | Track sensor for train detection |

### Build & Run Commands

```bash
# Build all
dotnet restore && dotnet build

# Run WinUI (Windows Desktop)
dotnet run --project WinUI

# Run WebApp (Blazor Dashboard)
dotnet run --project WebApp

# Run Tests
dotnet test
```

---

## [ACTIVE] Instruction Files (7 - Production-Ready)

### 1. Architecture & Patterns

- [architecture.instructions.md](./architecture.instructions.md)  
  **Detailed architecture overview**: Layers, data flow, key interfaces, project structure, plugin system.

- [mvvm-best-practices.instructions.md](./mvvm-best-practices.instructions.md)  
  **MVVM patterns with CommunityToolkit.Mvvm**: Attributes, commands, property notifications, ViewModel lifecycle.

- [fluent-design.instructions.md](./fluent-design.instructions.md)  
  **Fluent Design System for WinUI 3**: Materials, spacing, typography, icons, theming.

- [self-explanatory-code-commenting.instructions.md](./self-explanatory-code-commenting.instructions.md)  
  Guidelines for writing self-documenting code with minimal comments. Explains WHY, not WHAT.

### 2. Dynamic Index & Knowledge Bridge

- [.copilot-todos.md](./.copilot-todos.md)  
  **AUTHORITATIVE for cross-session knowledge.** Contains:
  - Session histories and learned patterns
  - TODO lists and pending work
  - Technical discoveries and decision logs
  - Instruction file status and planned additions
  - ReSharper warnings analysis and fixes

#### 🚨 KRITISCHE REGELN für .copilot-todos.md

> **Diese Regeln sind VERBINDLICH und dürfen NIEMALS verletzt werden!**

```
┌──────────────────────────────────────────────────────────────────┐
│  REGEL 1: NIEMALS offene Tasks löschen                          │
│  ─────────────────────────────────────────────────────────────── │
│  ❌ VERBOTEN: Sektionen mit ⏳ Status löschen oder überschreiben │
│  ❌ VERBOTEN: "altes durch neues ersetzen" bei TODO-Listen      │
│  ❌ VERBOTEN: Fortschritts-Tracking entfernen                   │
│                                                                   │
│  ✅ ERLAUBT: Neue Sektionen HINZUFÜGEN (append)                 │
│  ✅ ERLAUBT: Status von ⏳ auf ✅ ändern (mit Datum)            │
│  ✅ ERLAUBT: Erledigte Tasks (✅) nach 30 Tagen archivieren     │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│  REGEL 2: Vollständig lesen vor Änderungen                       │
│  ─────────────────────────────────────────────────────────────── │
│  Vor JEDER Änderung an .copilot-todos.md:                        │
│  1. Komplette Datei lesen (alle Sektionen)                       │
│  2. Prüfen welche Aufgaben noch ⏳ offen sind                    │
│  3. Keine bereits ✅ erledigten Empfehlungen wiederholen         │
│  4. Neue Inhalte am Ende der passenden Sektion hinzufügen        │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│  REGEL 3: Thematische Sektionen beibehalten                      │
│  ─────────────────────────────────────────────────────────────── │
│  Diese Sektionen MÜSSEN immer existieren:                        │
│  • 🚨 SECURITY (vor GitHub-Release)                              │
│  • 📋 OFFENE AUFGABEN ÜBERSICHT                                  │
│  • 📄 DOKUMENTATION & REPOSITORY                                 │
│  • 🧹 CODE-BEREINIGUNG                                           │
│  • 🧪 TESTS                                                       │
│  • 🚀 FEATURE-BACKLOG                                            │
│  • ✅ ABGESCHLOSSEN (letzte 7 Tage)                              │
│  • 🤖 FÜR COPILOT: Session-Regeln                                │
└──────────────────────────────────────────────────────────────────┘
```

**Konsequenz bei Regelverletzung:** Datenverlust wie in Commit 2a22af7, wo ~150 Zeilen wichtiger Checklisten verloren gingen.

### 3. Terminal & PowerShell Standards

- [terminal.instructions.md](./terminal.instructions.md)  
  Hard rules for PowerShell 7 terminal usage in Copilot. Command chaining, syntax requirements, error handling.

### 4. Project Overview

- [README.md](../../README.md)  
  Authoritative source for project overview, architecture, and high-level context.

---

## [DRAFT] PLACEHOLDER Files (19 - Need Substantial Content)

These files exist but contain placeholder/incomplete content. **Do not reference** until content is verified:

**Track Planning (5):**
- geometry.md, topology.md, snapping.md, rendering.md, editor-behavior.md

**Architecture & Patterns (4):**
- backend.instructions.md, collections.instructions.md, di-pattern-consistency.instructions.md, dotnet-framework.instructions.md

**UI & UX (4):**
- winui.instructions.md, maui.instructions.md, blazor.instructions.md, xaml-page-registration.instructions.md

**Testing & Quality (2):**
- test.instructions.md, hasunsavedchanges-patterns.instructions.md

**DevOps & Automation (2):**
- github-actions-ci-cd-best-practices.instructions.md, powershell.instructions.md

**Copilot Behavior (2):**
- instructions.instructions.md, prompt.instructions.md

---

## [INACTIVE] DEPRECATED Files (1 - Do Not Use)

- **no-terminal.instructions.md** (Replaced by `terminal.instructions.md` - See [ACTIVE] section above)

---

## YAML Frontmatter Standard

All instruction files MUST have YAML Frontmatter with these fields:

```yaml
---
description: 'One-line description of the instruction file'
applyTo: '** (applies to all) or specific glob patterns'
---

# Title
[Content follows...]
```

**Example:**
```yaml
---
description: 'Guidelines for self-explanatory code with minimal comments'
applyTo: '**'
---

# Self-explanatory Code Commenting
...
```

---

## Rules for This Index

- MUST reference only **[ACTIVE] files** in primary documentation.
- MUST NOT cite [DRAFT] or [INACTIVE] files as authoritative sources.
- MUST maintain YAML Frontmatter across all instruction files.
- When adding new instruction files: update this index AND [.copilot-todos.md](./.copilot-todos.md) with status.
- For planned files (not yet created), document in [.copilot-todos.md](./.copilot-todos.md) first.
- To promote [DRAFT] to [ACTIVE]: Move from DRAFT section to ACTIVE section above, remove from DRAFT list.

---

## Complete Inventory Reference

For complete tracking of all 24 instruction files, their status, and cross-session history, see:
**[.copilot-todos.md](./.copilot-todos.md) - INSTRUCTION FILES STATUS (Dynamischer Index)**

---

# End of File