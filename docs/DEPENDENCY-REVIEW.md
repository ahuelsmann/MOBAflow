# Observational dependency review

[Issue #342](https://github.com/ahuelsmann/MOBAflow/issues/342) adds
[dependency-review.yml](../.github/workflows/dependency-review.yml) for normal pull requests,
including drafts and new fix commits. It is not a required merge check. The existing Quality
workflow, resolved transitive NuGet audit, analyzer baselines and branch rules are unchanged.

## Read the diagnostic, not only the job conclusion

The summary names the exact event base/head SHA, observation time, changed manifests,
added/changed/removed versions, and advisory IDs with affected paths. Version updates pair
removed and added versions. Removed vulnerabilities are separate from new vulnerabilities.
Severity alone does not prove exploitability.

| Diagnostic | Meaning |
| --- | --- |
| NO_NEW_FINDINGS | Successful comparison found no new vulnerable dependency in the explicitly named API delta. It does not certify the repository or absence of baseline findings. |
| FINDINGS | Potentially vulnerable additions/changes were returned. Read affected versions, paths and coverage limits. |
| INCOMPLETE | Missing/pending snapshots, a changed dependency input without a returned delta (no dependency change or not parsed), unverified NuGet resolution or bounded data prevents complete coverage. Up to 20 such inputs are listed; the rest are counted. |
| FAILED | API/access/rate/parse failure or a stale event prevented reliable comparison. It is not zero findings. |

The API supplies a delta, not a full baseline vulnerability inventory. Unchanged findings and
their IDs/counts cannot be reconstructed from an empty delta: consult the existing full-graph
audit. Complete dependency chains and scopes may be unavailable; the report does not invent them.

## NuGet coverage

GitHub lists NuGet project files as supported. That does not prove coverage of this repository's
central Directory.Packages.props, MSBuild conditions or restored transitive graph.
Changes to those inputs are reported INCOMPLETE; direct rows alone do not prove resolution.
This workflow performs no restore or dependency submission and preserves the existing Quality
audit with its include-transitive option.

Before declaring a relevant dependency PR checked, inspect its exact head/base and returned
manifests. An unrelated documentation PR or an unavailable API is not proof of audited dependency
changes. The initial historical NuGet probe (7d9e62a2...e19706a8, package update #285) returned
HTTP 404 through local public API access. Graph/access/revision availability was not established,
and no clean result is claimed. The normal workflow token provides separate runtime evidence.

## Findings and exceptions

GitHub Manager owns technical reporting, fixtures and SHA provenance. Head of Quality triages
relevance, production/development/transitive scope, prerequisites and evidence. Senior PM assigns
priority and a named fix owner in the existing queue. The delivery owner proves a correction;
the existing independent Claude process reviews the resulting PR.

Material risk acceptance remains with the human, prepared by HQ and Senior PM. HQ may explain
a technically demonstrated false positive; a fixed finding is not an exception. There is no
automatic suppression, exception file, follow-up task, Ready transition or merge.

## Forks, cost and execution limits

- Only ordinary pull-request events run (opened, synchronize, reopened, ready_for_review, and
  edited only when the base branch changed; title/body edits do not run), including drafts and
  fix heads. Permissions are read-only
  repository contents and PR metadata. No application secrets, saved checkout credential,
  PR-comment write permission or privileged target/follow-up event is used.
- The exact PR head supplies reporter and fixtures in an unprivileged disposable runner.
  No write token or application/environment secret is supplied to that code.
- The official GitHub action is pinned to its upstream v5.0.0 commit in warning-only mode,
  covering runtime, development and unknown scopes. License policy and external scorecards
  are outside this vulnerability task. Official API failures remain visible even when their
  failed step outcome is tolerated for observational delivery.
- The job only runs for public MOBAflow on a standard hosted Ubuntu runner, with a five-minute
  timeout. No paid security add-on, larger runner or billing-limit change is required.
- No cache or artifact upload is configured. Summaries/logs do not consume artifact storage.
  The official action has a one-day fallback artifact for summaries over 1 MiB; comparisons
  are bounded here to 200 changes / 32 KiB before invoking it. Oversized input is INCOMPLETE.
  Verify actual incremental artifact usage in delivery evidence; no storage upgrade is allowed.
- A fork awaiting approval or a GitHub-skipped job is not performed, not NO_NEW_FINDINGS.
  No automatic retry/follow-up agent is created.

Run focused fixtures without installing packages:

~~~text
node --test scripts/dependency-review.test.cjs
~~~

Only developer/CI behavior changes. Application UI, runtime contracts and user workflows are
unchanged; the canonical user wiki requires no correction for this integration.

## Primary references

- [Dependency review availability and semantics](https://docs.github.com/en/code-security/concepts/supply-chain-security/dependency-review)
- [Official pinned action and warning-only configuration](https://github.com/actions/dependency-review-action/tree/a1d282b36b6f3519aa1f3fc636f609c47dddb294)
- [Dependency-graph ecosystems](https://docs.github.com/en/code-security/reference/supply-chain-security/dependency-graph-supported-package-ecosystems)
- [Dependency-review API and read permission](https://docs.github.com/en/rest/dependency-graph/dependency-review)
- [Standard public runner and storage billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [Workflow security and untrusted PR code](https://docs.github.com/en/actions/reference/security/secure-use)
