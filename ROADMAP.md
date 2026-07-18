# MOBAflow roadmap

This roadmap describes intended user outcomes. GitHub issues, milestones, and
the public [Kanban](https://github.com/users/ahuelsmann/projects/1) remain the
source of truth for scope and progress. Priorities may move when testing,
hardware safety, or maintainability requires it.

## Now: reliable locomotive operations (target 0.2.0)

The next release target focuses on making everyday locomotive operation easier
to prepare, verify, and maintain:

- manage locomotives as a reusable digital library;
- detect conflicting digital addresses before they disrupt a layout;
- keep decoder profiles, CV backups, and maintenance information together;
- produce reviewable Windows release candidates through GitHub Releases; and
- keep build, test, roadmap, and release status discoverable from the repository.

Follow the live [0.2.0 milestone](https://github.com/ahuelsmann/MOBAflow/milestone/1)
for its exact scope and the [open issues](https://github.com/ahuelsmann/MOBAflow/issues)
for individual work items.

## Next: safer and simpler layout setup

After the current target, planned outcomes include:

- guided setup for local configuration and Z21 connectivity;
- clearer diagnostics for hardware, network, and feedback problems;
- a more complete mobile operating experience in MOBAsmart; and
- broader track-plan libraries and validation support.

These outcomes are direction, not committed release scope. They become release
commitments only after assignment to a GitHub milestone.

## Later: connected operating sessions

Longer-term exploration includes richer multi-device status, display workflows,
and timetable-aware automation. New proposals should start with the operating
problem and user benefit in a
[feature request](https://github.com/ahuelsmann/MOBAflow/issues/new?template=feature_request.yml).

## How progress is maintained

- [Kanban](https://github.com/users/ahuelsmann/projects/1) shows planned, active, and completed work.
- [GitHub issues](https://github.com/ahuelsmann/MOBAflow/issues) contain concrete work.
- [Milestones](https://github.com/ahuelsmann/MOBAflow/milestones) define actual release scope.
- [GitHub Releases](https://github.com/ahuelsmann/MOBAflow/releases) contain published versions.
- [CHANGELOG.md](CHANGELOG.md) records notable repository changes.
- [Quality workflow](https://github.com/ahuelsmann/MOBAflow/actions/workflows/quality.yml)
  reports current build and test health.

No progress percentages or copied issue counts are maintained here, so the
roadmap cannot silently disagree with GitHub.
