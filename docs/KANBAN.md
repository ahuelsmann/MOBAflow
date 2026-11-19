# GitHub Kanban

MOBAflow uses one public GitHub Project named
[Kanban](https://github.com/users/ahuelsmann/projects/1). It is a view of work
from this repository, not a second project inside MOBAflow. Issues and pull
requests remain the work items; the Kanban adds shared fields and useful views.

## Fields

| Field          | Values                                                                                   |
| -------------- | ---------------------------------------------------------------------------------------- |
| Status         | Inbox, Planned, Ready, In progress, In review, Done                                      |
| Priority       | P0, P1, P2, P3                                                                           |
| Area           | MOBAflow, MOBAsmart, MOBApi, MOBAdisplay, Track plan, Z21, Documentation, Infrastructure |
| Type           | Feature, Bug, Refactoring, Documentation, Security, Release                              |
| Target release | First stable release, Future                                                            |
| Risk class     | H, M, L (see risk classes in master coordination; review-fix rounds keep their slice's) |
| Locked areas   | Text: projects or folders the open slice locks, for example `SharedUI/ViewModel`        |
| Writer         | Text: the one named Senior Developer who pushes to the slice branch                     |

Risk class, Locked areas and Writer are filled for items that represent an open slice (a Draft PR) and mirror the
slice header block of the PR body. Status doubles as the slice state: Ready, In progress and In review count as open.
The COO sets these fields; the PR header is the source if the two disagree.

## Views

1. **Current work** filters the board to Ready, In progress, and In review.
2. **Product areas** groups work by Area.
3. **Quality & maintenance** collects Infrastructure work, including security and dependency maintenance.
4. **Roadmap** groups work by Target release.
5. **Completed** filters the board to Done.

GitHub Projects cannot combine different fields with an `OR` filter. Security
and dependency-maintenance items therefore use the Infrastructure area so they
appear together in **Quality & maintenance**.

## Slices, locks and WIP limit

The flow in [master coordination](../.github/instructions/master-coordination.instructions.md#board-locks-and-wip-limit)
uses the Current work view for its checks: a new slice may start only in an area that no open item lists under
Locked areas, and at most three code slices are open. Locks, design Go, handovers and merges are logged as PR
comments; the item fields hold only the current state.

## Automation

The enabled GitHub Project workflows automatically add repository issues, react
to linked and merged pull requests, and keep closed work synchronized. The
scheduled repository workflow marks inactive issues and pull requests as
`stale` after 60 days for review, but never closes them automatically.

## Operating rules

- Product behavior is tracked in one focused issue; epics only aggregate status.
- Every implementation pull request links its issue and lists exact validation.
- Milestones define committed release scope; Target release organizes the Kanban.
- Coverage values are quoted only from archived Cobertura artifacts.
- Release acceptance links a signed-tag workflow run, draft release, and manual compatibility evidence.
- The roadmap communicates outcomes, not unverified dates.
