# Documentation consistency

The [Quality workflow](../.github/workflows/quality.yml) checks generated package
and mutation facts for every PR. Run from the task worktree root:

```powershell
python scripts/documentation_consistency.py
python scripts/documentation_consistency.py --write
python scripts/Test-DocumentationConsistency.py
```

`--write` updates only marked source-derived tables and repeated native-package
version references. Package purposes and license prose remain hand-authored;
new packages are explicitly flagged for purpose/license review.

## Semantic maintenance and activation

The separate [documentation workflow](../.github/workflows/documentation-consistency.yml)
is **not activated by default**. It reports that state explicitly. Structural
checks do not establish semantic correctness.

Before activating it, configure the following repository values:

| Name | Type | Purpose |
| --- | --- | --- |
| `OPENAI_API_KEY` | Actions secret | API access for two independent, bounded assessments. API usage is billed separately from ChatGPT subscriptions. |
| `DOCS_AI_MODEL` | Actions variable | A model available to your API project that supports strict structured Responses output. No model is silently selected. |
| `DOCS_APP_ID` | Actions variable | ID of a GitHub App installed on this repository. |
| `DOCS_APP_PRIVATE_KEY` | Actions secret | Private key for that App. Grant repository Contents read/write; no administration permission is needed. |
| `DOCS_AI_ENABLED` | Actions variable | Set to `true` only for the live acceptance test after configuring the other values and API budget. |

Use an API project with a suitable budget/limits. No paid API request or GitHub
App setup was performed during offline implementation. Run a small same-repository
acceptance PR after merging the workflow, since trusted controls are loaded from
the base branch. Confirm the following before requiring the check:

1. A source change receives the necessary documentation-only commit on the same branch.
2. Quality and Sonar run again on that new commit; prior-head results do not suffice.
3. The new head passes the custom **Documentation consistency** check without another
   automatic commit. Confirm unchanged-documentation and uncertainty cases too.
4. Add **Documentation consistency** to required checks and require branches to be up
   to date. Select the check emitted by GitHub Actions, not a target-branch job result.

## Boundaries and failure handling

The workflow executes scripts/prompts from the base commit. PR files are only
data and are checked out only for an enabled, trusted same-repository PR; fork
and untrusted heads never reach the privileged runner. The model has no execution
tools. A separate publishing job receives the repository-scoped App token after independent review. That token causes normal
PR workflows to rerun, unlike an ordinary `GITHUB_TOKEN` push.

An update must replace a unique existing passage and quote supplied source
evidence. Automatic writes cover existing Markdown under `docs/` plus README and
CONTRIBUTING. Generated facts, licenses, specifications, agent policies, workflows
and source code cannot be rewritten by AI. The publisher rechecks head/base,
document hashes, paths, secrets, links and source-derived facts; a push lease
rejects an intervening human commit.

A documentation-only last commit disables further auto-updates, including after
the bot's commit. If the independent review still finds a problem, correct it
manually. Forks and untrusted authors need maintainer review and receive no
automatic writes or API calls. Missing enabled configuration, an oversized context,
unavailable source evidence, API/scanner errors and incomplete/refused model output
produce a failed or action-required result rather than a semantic pass.

The context limit is one megabyte; each of the two requests has an 8,000-token
output cap and a timeout. No automatic API retries multiply charges. These limits
do not guarantee factual correctness. Review documentation alongside code.

See [the feature specification](../specs/011-documentation-consistency/spec.md)
and [Issue #266](https://github.com/ahuelsmann/MOBAflow/issues/266) for acceptance status.
