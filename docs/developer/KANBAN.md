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
| Target release | First stable release, Future                                                             |
| Risk class     | H, M, L (review-fix rounds keep their slice's class)                                     |
| Locked areas   | Text: projects or folders the open slice locks, for example `SharedUI/ViewModel`         |
| Writer         | Text: the one named Senior Developer who pushes to the slice branch                      |

Risk class, Locked areas and Writer are filled for items that represent an open slice (a Draft PR) and mirror the
slice header block of the PR body. The COO adds each slice PR to the project when the slice starts and sets these
fields. The PR header is the source of truth; if the Kanban field differs, the Kanban field is corrected to match.

Status for slices: **Planned** = slice cut, design note awaiting Go; **Ready** = Go given, the writer may start
(this is not the PR's "Ready for review"); **In progress** = implementation and review fixes; **In review** = PR
marked ready for review, final review and armed auto-merge; **Done** = merged. Open slices are the items with
Risk class set and a Status from Planned to In review.

Status for issues (items without Risk class): **Planned** = accepted backlog; **Ready** = the maintainer's pull
trigger for the COO to take the issue in; **In progress** = claimed by the COO, in analysis, waiting for a
maintainer decision or in slice delivery; **Done** = closed, delivered or decided against. Only the COO claims
Ready issues (its scheduled monitoring session, Claude Monitor, does this on its behalf). It claims an issue when
its analysis or first slice can start and moves it to In progress at once. Issues that need specification or an
open product, UX or architecture decision start with a Decision & Steering Triad analysis and a concise
recommendation, not with code, even when the WIP limit is reached. Slices cut from an issue follow the WIP limit,
locks, design Go and veto rules in
[master coordination](../../.github/instructions/master-coordination.instructions.md#board-locks-and-wip-limit).

## Views

1. **Current work** filters the board to Planned, Ready, In progress, and In review (open slices and issues).
2. **Product areas** groups work by Area.
3. **Quality & maintenance** collects Infrastructure work, including security and dependency maintenance.
4. **Roadmap** groups work by Target release.
5. **Completed** filters the board to Done.

GitHub Projects cannot combine different fields with an `OR` filter. Security
and dependency-maintenance items therefore use the Infrastructure area so they
appear together in **Quality & maintenance**.

## Slices, locks and WIP limit

The flow in [master coordination](../../.github/instructions/master-coordination.instructions.md#board-locks-and-wip-limit)
uses the open slices (items with Risk class set) for its checks: a new slice may start only in an area that no
open item lists under Locked areas, and at most three code slices (documentation-only slices excluded) are open.
Locks, design Go, handovers and merges are logged as comments on the Draft PR; the item fields hold only the
current state.

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
