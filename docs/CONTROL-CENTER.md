# GitHub Control Center

The repository start page, [roadmap](../ROADMAP.md), issues, pull requests,
quality runs and releases form MOBAflow's public control center. Repository files
define the repeatable part; the GitHub Project itself is configured once by a
maintainer because organization-level Project permissions are not available to
repository workflows.

## Recommended GitHub Project configuration

Create one public project named **MOBAflow Control Center** and add these fields:

| Field | Values |
| --- | --- |
| Status | Inbox, Ready, In progress, In review, Done |
| Priority | P0, P1, P2, P3 |
| Area | Product, Runtime, Hardware, Quality, Documentation, Release |
| Target | Now, Next, Later |

Views:

1. **Roadmap** — table grouped by Target, sorted by Priority.
2. **Current work** — board grouped by Status and filtered to Now.
3. **Release readiness** — table filtered to Area = Release or label `release`.
4. **Quality debt** — table filtered to Area = Quality and not Done.

Built-in Project workflows should set new issues to Inbox, linked pull requests
to In review and merged pull requests to Done. They must not auto-close product
issues unless the pull request explicitly uses a closing keyword.

## Repository labels

`.github/labels.yml` is the canonical label catalogue. Apply it with a trusted
label-sync integration or create the six labels once in repository settings.
Issue forms and release acceptance use only catalogue labels.

## Operating rules

- Product behavior is tracked in one focused issue; epics only aggregate status.
- Every implementation pull request links its issue and lists exact validation.
- Coverage values are quoted only from archived Cobertura artifacts.
- Release acceptance links a signed-tag workflow run, draft release and manual
  compatibility evidence.
- The roadmap communicates outcomes, not unverified dates.
