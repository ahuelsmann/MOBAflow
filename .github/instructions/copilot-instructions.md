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
- [implementation-workflow.instructions.md](./implementation-workflow.instructions.md) - **PFLICHT:** 5-Schritte-Workflow (Analyse → Best Practices → Fluent Design → Plan → Code)

### Architecture & Patterns
- [architecture.instructions.md](./architecture.instructions.md) - Layers, data flow, interfaces
- [mvvm-best-practices.instructions.md](./mvvm-best-practices.instructions.md) - MVVM with CommunityToolkit
- [fluent-design.instructions.md](./fluent-design.instructions.md) - Fluent Design System
- [self-explanatory-code-commenting.instructions.md](./self-explanatory-code-commenting.instructions.md) - Code commenting

### Terminal & Tools
- [terminal.instructions.md](./terminal.instructions.md) - **WICHTIG:** PowerShell-Regeln, erlaubte/verbotene Befehle

### TODOs & Session Knowledge
- [todos.instructions.md](./todos.instructions.md) - Aktive TODOs und Entscheidungen

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
