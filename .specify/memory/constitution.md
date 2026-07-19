# MOBAflow Constitution

## Core Principles

### I. Preserve Architecture and Threading Boundaries

Every change MUST respect the existing layer boundaries. `Common`, `Domain`, and
`Backend` remain platform-independent and MUST NOT reference WinUI or MAUI.
Shared ViewModels depend on `IMobaRuntime`, not on transport-specific clients.
Z21 events cross into the UI through `UiThreadEventBusDecorator`; EventBus
handlers MUST update state directly and MUST NOT add another dispatcher call.
Platform-specific behavior belongs in the corresponding host or adapter layer.

### II. Keep Asynchronous and UI Behavior Correct

Asynchronous work MUST use `await`; `.Result`, `.Wait()`, and equivalent sync-over-
async patterns are prohibited. UI behavior belongs in ViewModels and commands.
Code-behind and UserControls may translate platform input into commands, but MUST
NOT own feature or domain behavior. WinUI visuals MUST use `ThemeResource` values
and be verified in both Light and Dark themes. All shipped user-visible UI text
MUST be English; announcement voice and language remain user-configurable.

### III. Tests Are a Delivery Requirement

Every new or changed behavior MUST have an automated unit or integration test.
Tests use NUnit, Moq, and established fakes such as `FakeUdpClientWrapper`, follow
Arrange-Act-Assert, and cover regression-prone parsing, paths, configuration,
serialization, and state transitions. Platform UI changes MUST cover testable
logic below the UI layer and document focused manual checks where automation is
not practical. `dotnet test Test/Test.csproj` MUST pass before a feature is
considered complete; platform-specific build limitations MUST be stated rather
than silently skipped.

### IV. Specifications Must Be Testable and Compatible

Each feature specification MUST define independently testable user scenarios,
explicit acceptance criteria, edge cases, measurable outcomes, and the affected
platforms. Plans MUST identify compatibility effects on existing JSON data,
configuration defaults, public APIs, Z21 behavior, and persisted layouts.
Breaking changes require an explicit migration path and justification. Existing
defaults and serialized data MUST remain compatible unless the approved
specification deliberately changes them.

### V. Prefer Simple, Traceable Changes

Implement the smallest coherent design that satisfies the approved
specification. Reuse existing services, helpers, DI patterns, and action
executors; do not duplicate established behavior. Dependencies use constructor
injection and established singleton/transient lifetimes. Public APIs require XML
documentation. Comments explain intent or constraints, not obvious mechanics.
Identifiers are ASCII-only and follow the repository naming conventions.

## Technical and Product Constraints

- The repository targets .NET 10 and contains WinUI, MAUI/Android, ASP.NET Core,
  cross-platform libraries, tests, display tooling, and ESP32 firmware.
- Windows-only and Android-only projects MUST be validated on matching hosts.
  Cross-platform projects and `Test/Test.csproj` remain the portable validation
  path.
- Master data is served by `MasterDataStore`; runtime project state comes from
  immutable `IMobaRuntime.CurrentSnapshot` snapshots.
- Star-sized resizable layout values MUST be persisted as star values; pixel
  widths are reserved for intentionally fixed UI regions.
- UI resources MUST meet the repository's Fluent Design and accessibility rules.
- No source-code TODO comments are permitted. Follow-up work belongs in the
  project's authoritative Azure DevOps backlog.

## Development Workflow and Quality Gates

Work follows the repository's six-step workflow: analyse, research, plan,
implement, validate, and document. Spec Kit maps to it as follows:

1. `$speckit-specify` records user value, scope, compatibility, and acceptance
   criteria.
2. `$speckit-clarify` resolves material ambiguity before technical design.
3. `$speckit-plan` records architecture, platform scope, risks, and validation.
4. `$speckit-tasks` creates traceable implementation and mandatory test tasks.
5. `$speckit-analyze` checks cross-artifact consistency before implementation.
6. `$speckit-implement` executes tasks without bypassing repository rules.
7. Validation includes relevant targeted builds, `dotnet test Test/Test.csproj`,
   formatting/static checks, and Light/Dark theme checks for UI changes.
8. Documentation and changelog updates are included whenever behavior or the
   contributor workflow changes.

The Constitution Check in every plan MUST pass before research and again after
design. Any exception MUST be recorded in the plan's Complexity Tracking table
with the reason, rejected simpler alternative, risk, and compensating validation.

## Governance

This constitution codifies the project rules in `AGENTS.md`,
`.github/copilot-instructions.md`, and the scoped files under
`.github/instructions/`. Those repository instructions remain authoritative and
MUST be loaded before work begins. A constitutional amendment requires an
explicit rationale, updates to affected Spec Kit templates or instructions, and
a migration note when existing specifications are affected. Version changes use
semantic versioning: MAJOR for incompatible governance changes, MINOR for new or
materially expanded principles, and PATCH for clarifications. Every plan and
review MUST verify compliance; unresolved violations block implementation.

**Version**: 1.0.0 | **Ratified**: 2026-07-19 | **Last Amended**: 2026-07-19
