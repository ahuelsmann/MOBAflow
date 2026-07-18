# Open Issues Implementation Plan

This plan turns the seven open issues into independently reviewable slices. Shared rules apply to every slice: domain and backend logic stay platform-neutral, persisted additions remain optional and backwards compatible, UI code only projects structured results, and each behavior change is protected by automated tests.

## Issue #4 — Feedback-triggered locomotive whistle

### Scope

- Add an optional `LocomotiveWhistleRule` collection to a project. A rule uses a stable locomotive ID, feedback input, function F0–F31, a non-negative delay, a positive active duration and an enabled flag.
- Implement a platform-neutral automation service which consumes feedback events, resolves the current locomotive address and sends momentary function commands through a narrow gateway.
- Repeated feedback restarts the delay or extends the active period. Replacing the project, disconnecting or disposing the service cancels work and makes a best-effort function-off call.
- Keep rule editing separate from transport details so desktop and mobile hosts can share the behavior.

### Acceptance and tests

- Old project JSON loads without rules; new rules survive a save/load round trip.
- Invalid function, delay and duration values are rejected before activation.
- Tests cover missing locomotives, delayed on/off, retriggering, parallel rules, cancellation, disconnect and F0/F31 boundaries.
- Diagnostics describe ignored rules without issuing a command.

## Issue #10 — GitHub-native Control Center

### Scope

- Add a concise roadmap and project-status section linking product issues, quality checks, releases and contribution guidance.
- Add issue forms for actionable feature work and release acceptance, plus repository labels that distinguish product, quality, documentation and release work.
- Add safe GitHub automation for issue hygiene and release-note validation. Project-board configuration that requires organization-level credentials is documented as an explicit maintainer action.

### Acceptance and tests

- All public links resolve from the repository start page and GitHub Pages.
- Workflow YAML parses and uses least-privilege permissions.
- No badge claims a result that is not produced by an automated check.

## Issue #12 — Digital address conflicts

### Scope

- Keep `DigitalAddressConflictDetector` as the single platform-neutral source of truth.
- Treat locomotive primary addresses as exclusive, including locomotives assigned to a double-traction train.
- Deliberately allow passenger and goods wagon function decoders to share an address so coach lighting can be grouped.
- Model coordinated multiple traction at train/command level instead of inferring it from duplicate locomotive addresses.
- Aggregate current project findings in a shell-level message panel above the status bar, separate from continuous application and Z21 logs.
- Keep errors, warnings and information independently filterable and retain stable target IDs for future page navigation.
- Treat ordinary locomotive/accessory address duplication as a warning; invalid ranges and duplicate feedback InPorts remain errors.

### Acceptance and tests

- Empty, single and multiple-conflict states are represented explicitly.
- Ordering is deterministic and findings retain navigation targets.
- Existing domain/range tests remain green; view-model tests cover refresh after project edits.
- Tests prove that shared wagon addresses are accepted and duplicate locomotive addresses remain visible in double traction.
- Signal-box feedback points require an assigned, unique InPort; configured track-plan feedback points require positive, unique InPorts.

## Issue #13 — Maintenance history and reminders

### Scope

- Introduce a maintenance application service with an injected reference time, validation and due-state calculation (`NotScheduled`, `Upcoming`, `Due`, `Overdue`).
- Validate non-negative counters, positive intervals, meaningful descriptions and ISO 4217 currency codes.
- Surface current counters, history and reminder summaries in the existing locomotive-management UI and passport projection.

### Acceptance and tests

- Date, operating-hour and distance schedules behave deterministically at their boundaries.
- Invalid edits cannot partially mutate persisted data.
- Old and new project files round-trip; optional data stays optional.

## Issue #14 — Decoder profiles and CV backups

### Scope

- Add a platform-neutral CV service for validation, atomic snapshot replacement, comparison and deterministic JSON import/export.
- Enforce unique CV numbers and protocol-aware CV/value ranges without talking to hardware.
- Present snapshot metadata, validation errors and comparisons through the locomotive-management view model.

### Acceptance and tests

- Invalid or duplicate rows report exact input positions and leave the profile unchanged.
- Import/export is deterministic and round-trips all supported metadata.
- Comparison reports added, removed and changed CVs in numeric order.

## Issue #15 — Digital locomotive library and passport

### Scope

- Keep the existing Locomotives page as the only management surface.
- Extend the platform-neutral passport with maintenance status and decoder snapshot counts.
- Add a deterministic, printable HTML renderer with escaped user input. QR remains optional and is not required for this slice.

### Acceptance and tests

- Complete and partial locomotives render without failure.
- Output is deterministic, escapes user content and never embeds local paths, API addresses or secrets.
- Passport projection and rendering have direct unit tests.

## Issue #16 — Locomotive management and release workflow epic

### Scope

- Treat the epic as the integration and acceptance track for issues #12–#15 and Release Studio.
- Add a changelog, release-candidate checklist, compatibility checks and a machine-readable dependency/security report to CI.
- Update .NET to the current patched servicing release, align GitHub Actions runtimes, correct SourceLink metadata and document the actual architecture and build path.
- Validate release packaging without creating a public release. A signed-tag end-to-end run remains a maintainer-controlled final acceptance step because it requires the maintainer's signing key.

### Acceptance and tests

- Windows quality CI builds the explicit desktop graph, publishes tests and Cobertura coverage, and archives a transitive vulnerability report.
- Release documentation names every artifact, configuration-scrubbing rule and manual compatibility check.
- A draft release can only be created from a valid signed version tag; the final signed-tag exercise is recorded rather than simulated.

## Delivery and review sequence

1. Domain and backend services with unit tests.
2. Shared locomotive-management projection and thin WinUI presentation.
3. Repository control-center, release and documentation changes.
4. Full desktop test suite and coverage generation.
5. Static review of dependency direction, persistence compatibility, cancellation, input escaping, workflow permissions and release safety.
6. Draft pull request. Issues are only closed after CI and the relevant acceptance criteria are proven; the signed release-studio exercise remains unchecked until run by a maintainer.
