# Feature Specification: MOBAflow 1.0 release, setup and getting started

**Feature Directory**: `010-release-1-0`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/144

**Created**: 2026-10-08

**Status**: Draft (clarification pending)

**Spec Kit**: Required

**Input**: User description (issue #144, translated): "Define the MVP scope for MOBAflow 1.0 and prepare the release
with a setup and the entry for users: README screenshots, setup for consumers, user wiki, start guide for
beginners, marketing for railway and model railroad fans, MOBAnews and the GitHub Pages project website."

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #144

**Confirmed scope decision**: [issue comment of 2026-09-24](https://github.com/ahuelsmann/MOBAflow/issues/144#issuecomment-5814129537)

**Related Work**: #191 (RF-23: one runtime and one Z21 per project, changes installation and setup docs), #188
(recorder gaps, fixed before release), #195 (Android release AAB job disabled until distribution starts), #143,
#34 (explicitly not part of 1.0).

**Affected Platforms**: MOBAflow (Windows) packaging and release workflow, MOBAsmart distribution (Q2),
documentation (`README.md`, `docs/wiki/`, `docs/index.html`), external MOBAnews website.

**Out of Scope**: New product features; vehicle statistics (#143); route reservations and interlocking (#34);
maintenance; automatic journey end, repeat or follow-up journeys; a workflow action to set counters; Microsoft
Store or Play Store publication unless Q1/Q2 choose it.

**Sensitive Data**: Code-signing certificates and Android keystores, if Q1/Q2 require them; they stay in GitHub
secrets or on the maintainer's machine and never enter the repository.

**Data and API Effects**: None for `solution.json`. Release Studio gains an installer artifact (Q1). Documentation
and website content change.

## Confirmed MVP Scope for 1.0 (from the issue, verified on main `d45e14c5`)

- Journeys are active or inactive. On InPort feedback, every active journey's event-plan entries are matched on
  InPort and current counter value and start their workflows; without a match nothing happens.
- No automatic journey end, repeat or follow-up journey: removed with #148 (merged).
- No route reservations: removed with #149 (merged). Collision avoidance is the user's responsibility.
- No maintenance including calendar maintenance: removed with #152 (merged).
- No vehicle statistics: removed with #125 (merged); re-evaluation only in #143.
- Every app keeps its InPort counters locally, outside `solution.json`, and restores them after a restart (#163,
  decision of 2026-09-25 in `specs/006-persistent-inport-counters`).
- New since the issue was written: one runtime and one Z21 per project (#191). The release documents this rule.

## Clarifications

### Session 2026-10-08 (open, answers from the maintainer required)

Each question lists the recommended answer. The plan is drafted with the recommended answers.

- Q1: What does "setup" mean for Windows? → Recommended: a classic installer (Inno Setup) that wraps the existing
  self-contained x64 publish from Release Studio, adds Start-menu and uninstall entries, and is attached to the
  draft release next to the ZIP. Without a code-signing certificate Windows SmartScreen shows a warning; the start
  guide explains it. Alternatives: keep only the ZIP; MSIX (needs a trusted certificate); Velopack (adds automatic
  updates).
- Q2: How is MOBAsmart distributed with 1.0? → Recommended: a signed APK attached to the GitHub release for
  sideloading, with install steps in the start guide; Play Store later. Alternative: MOBAsmart is not part of the
  1.0 release.
- Q3: When is 1.0 released relative to the refactoring programme? → Recommended: after RF-23 (#191) and #188 are
  merged, before RF-24 to RF-31, because those packages change neither behavior nor the file format. From 1.0 on,
  persisted-format changes need a migration decision (today `AGENTS.md` allows breaking changes because there are
  no released users).
- Q4: Which role does each website play? → Recommended: the GitHub Pages project website is the entry point with
  download and start guide link; the user wiki is the reference; MOBAnews carries progress articles. MOBAflow's
  Info page links to the project website and MOBAnews; no news content is embedded in the app.
- Q5: In which language are the user-facing documents written? → Recommended: README, wiki and start guide in
  English (matches the app's English UI); the project website and MOBAnews stay German for the German-speaking
  community, each linking to the English guide. Today the project website is German and the wiki English.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Download and install (Priority: P1)

A model railroader without programming knowledge downloads MOBAflow from the release page, installs it and starts
it.

**Why this priority**: Today MOBAflow is distributed as source code only (`docs/wiki/INSTALLATION.md`); without a
setup there are no users.

**Independent Test**: On a clean Windows 11 machine without the .NET SDK, the installer from a release candidate
installs, starts and uninstalls MOBAflow (maintainer check, launch approved for that run).

**Acceptance Scenarios**:

1. **Given** the published 1.0 release, **When** the user runs the installer, **Then** MOBAflow starts from the
   Start menu without installing an SDK.
2. **Given** an installed MOBAflow, **When** the user uninstalls it, **Then** program files are removed and user
   data (solution files, settings, counters) stays.

---

### User Story 2 - First successful operation (Priority: P1)

A beginner follows the start guide from prerequisites through installation and first setup (Z21 per project,
first locomotive, first journey with one event-plan entry) to the first automated announcement or locomotive
command.

**Why this priority**: The confirmed MVP behavior is only useful if a beginner can reach it.

**Independent Test**: A person who did not write the guide reaches the first workflow execution with a Z21, using only
the guide.

**Acceptance Scenarios**:

1. **Given** a fresh installation, **When** the user follows the start guide, **Then** every screenshot and
   setting name matches the released app.

---

### User Story 3 - Consistent public information (Priority: P2)

README, user wiki, start guide, project website and MOBAnews describe the same scope and link to each other; none
promises excluded or merely proposed features.

**Why this priority**: Contradicting pages cost trust at the first contact.

**Independent Test**: A link and claim check over all five sources finds no broken link and no 1.0 claim outside
the confirmed scope.

---

### User Story 4 - Reach the community (Priority: P3)

The maintainer has a documented, prioritised list of channels and first actions to announce 1.0.

**Independent Test**: The marketing document names channels, target groups, first three actions and how MOBAnews
is updated.

### Edge Cases

- Installing over an older ZIP installation: the guide explains that the ZIP folder can be deleted; user data
  locations are unchanged.
- A user without a Z21: the start guide shows what works without hardware and states that operation needs a Z21.
- SmartScreen or antivirus warnings for an unsigned installer: documented with the checksum from `SHA256SUMS.txt`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Release Studio MUST produce the Windows setup chosen in Q1 from the signed tag, together with the
  existing ZIP and checksums, without publishing automatically.
- **FR-002**: The setup MUST install a self-contained MOBAflow that runs without a .NET SDK and MUST keep user data
  on uninstall.
- **FR-003**: `docs/wiki/INSTALLATION.md` MUST describe download and installation of the release instead of a
  source build; the source build moves to developer documentation.
- **FR-004**: A beginner start guide MUST cover prerequisites, installation, the Z21 per project, first
  locomotive, first journey with an event-plan entry and first workflow.
- **FR-005**: README screenshots MUST show the released UI.
- **FR-006**: README, wiki, start guide, project website and MOBAnews MUST agree with the confirmed 1.0 scope and
  link to each other as decided in Q4.
- **FR-007**: A marketing document MUST list prioritised channels and next actions and follow the MOBAnews
  editorial rules from the issue.
- **FR-008**: MOBAsmart distribution MUST follow Q2; if included, the release contains a signed APK and install
  steps.

### Key Entities

- **Release package**: tag, installer, ZIP, checksums, release notes.
- **Documentation set**: README, user wiki, start guide, project website, MOBAnews.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A clean Windows machine installs and starts 1.0 with the setup in one pass (maintainer check).
- **SC-002**: The start guide leads a first-time user to a first executed workflow without outside help.
- **SC-003**: The link and claim check across the documentation set reports zero broken links and zero
  out-of-scope claims.
- **SC-004**: The release notes and website promise no feature listed under Out of Scope.

## Assumptions

- The existing Release Studio workflow (`.github/workflows/release-studio.yml`) stays the only release path.
- Screenshots and clean-machine install checks need the maintainer's explicit launch approval and are done by the
  maintainer or in an approved run.
- Hardware checks with a real Z21 are maintainer-led.
