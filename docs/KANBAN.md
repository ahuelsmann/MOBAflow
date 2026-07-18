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
| Target release | 0.2.0, Future                                                                            |

## Views

1. **Current work** filters the board to Ready, In progress, and In review.
2. **Product areas** groups work by Area.
3. **Quality & maintenance** collects Infrastructure work, including security and dependency maintenance.
4. **Roadmap** groups work by Target release.
5. **Completed** filters the board to Done.

GitHub Projects cannot combine different fields with an `OR` filter. Security
and dependency-maintenance items therefore use the Infrastructure area so they
appear together in **Quality & maintenance**.

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
