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

## [ACTIVE] Instruction Files (4 - Production-Ready)

### 1. Architecture & Patterns

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