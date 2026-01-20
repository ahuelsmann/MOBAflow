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

## [ACTIVE] Instruction Files

### Workflow & Prozess
- [implementation-workflow.instructions.md](./implementation-workflow.instructions.md) - **PFLICHT:** 5-Schritte-Workflow
- [terminal.instructions.md](./terminal.instructions.md) - PowerShell-Regeln
- [powershell.instructions.md](./powershell.instructions.md) - PowerShell-Details
- [editor-behavior.md](./editor-behavior.md) - Editor-Verhalten

### Architecture & Patterns
- [architecture.instructions.md](./architecture.instructions.md) - Layers, data flow
- [di-pattern-consistency.instructions.md](./di-pattern-consistency.instructions.md) - DI Patterns
- [backend.instructions.md](./backend.instructions.md) - Backend Services
- [dotnet-framework.instructions.md](./dotnet-framework.instructions.md) - .NET Framework

### MVVM
- [mvvm.instructions.md](./mvvm.instructions.md) - MVVM Grundlagen
- [mvvm-best-practices.instructions.md](./mvvm-best-practices.instructions.md) - MVVM Details
- [collections.instructions.md](./collections.instructions.md) - Collections & ObservableCollection
- [hasunsavedchanges-patterns.instructions.md](./hasunsavedchanges-patterns.instructions.md) - Dirty-Tracking

### UI Frameworks
- [winui.instructions.md](./winui.instructions.md) - WinUI 3 Grundlagen
- [winui-compact.instructions.md](./winui-compact.instructions.md) - WinUI Kompakt
- [fluent-design.instructions.md](./fluent-design.instructions.md) - Fluent Design System
- [xaml-page-registration.instructions.md](./xaml-page-registration.instructions.md) - XAML Page Registration
- [winui3-best-practices-steps-4-12.md](./winui3-best-practices-steps-4-12.md) - WinUI Best Practices
- [winui3-vsm-detailed-guide.md](./winui3-vsm-detailed-guide.md) - Visual State Manager
- [blazor.instructions.md](./blazor.instructions.md) - Blazor WebApp
- [maui.instructions.md](./maui.instructions.md) - .NET MAUI

### TrackPlan System (MOBAtps)
- [geometry.md](./geometry.md) - Gleisgeometrie
- [rendering.md](./rendering.md) - Rendering Pipeline
- [snapping.md](./snapping.md) - Snap-Logik
- [topology.md](./topology.md) - Topologie-System

### Code Quality
- [self-explanatory-code-commenting.instructions.md](./self-explanatory-code-commenting.instructions.md) - Kommentare
- [no-special-chars.instructions.md](./no-special-chars.instructions.md) - Zeichensatz
- [test.instructions.md](./test.instructions.md) - Unit Tests

### CI/CD
- [github-actions-ci-cd-best-practices.instructions.md](./github-actions-ci-cd-best-practices.instructions.md) - GitHub Actions

### Meta & TODOs
- [instructions.instructions.md](./instructions.instructions.md) - Instruction-Regeln
- [prompt.instructions.md](./prompt.instructions.md) - Prompt-Guidelines
- [todos.instructions.md](./todos.instructions.md) - Aktive TODOs
- [todos/](./todos/) - TODO-Unterordner (5 Dateien)

### Project Overview
- [README.md](../../README.md) - Project documentation

---

## Terminal-Regeln (Kurzfassung)

**ERLAUBT:**
- `dotnet build`, `dotnet test`
- `git status`, `git diff`
- `Select-String` für Suchen

**VERBOTEN:**
- Dateien erstellen/ändern via Terminal
- XAML via Terminal schreiben
- Komplexe Datei-Operationen

→ Siehe [terminal.instructions.md](./terminal.instructions.md) für Details

---

## TODO-Datei Regeln

1. Datei KOMPLETT lesen vor Aenderungen
2. Offene Tasks NIEMALS loeschen
3. Erledigte Tasks mit Datum markieren
4. Nur HINZUFUEGEN, nicht ueberschreiben

---

## YAML Frontmatter Standard

```yaml
---
description: 'One-line description'
applyTo: '**'
---
```

---

# End of File
