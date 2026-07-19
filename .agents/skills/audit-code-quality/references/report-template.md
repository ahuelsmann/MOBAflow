# Code-quality audit report template

## Executive conclusion

- Overall assessment in two to four sentences
- Highest credible risks
- Audit date, commit, branch, dirty state, and confidence
- Clear statement that this is an engineering assessment, not ISO certification

## Scope and method

| Item | Evidence |
| --- | --- |
| Production source | File count, languages, projects |
| Test source | File count, frameworks |
| Excluded | Generated, vendored, caches, build output, reasons |
| Inventory coverage | Classified / discovered |
| Automated coverage | Projects successfully analyzed / total |
| Semantic review coverage | Components and hotspots reviewed |
| Platforms | Executed and unverified platforms |

List the authoritative standards and repository rules applied. Summarize commands with exit codes; move lengthy output to an appendix or artifact.

## Quality assessment

| Quality area | Rating | Confidence | Key evidence |
| --- | ---: | --- | --- |
| Functional suitability / correctness | 0-4 or NV | High/Medium/Low | |
| Reliability | 0-4 or NV | | |
| Security | 0-4 or NV | | |
| Maintainability | 0-4 or NV | | |
| Performance efficiency | 0-4 or NV | | |
| Compatibility / portability | 0-4 or NV | | |
| Flexibility / architecture | 0-4 or NV | | |
| Interaction capability / accessibility | 0-4 or NV | | |
| Safety, if applicable | 0-4 or NV | | |
| Test and verification maturity | 0-4 or NV | | |

Explain each rating immediately below the table. Omit non-applicable rows only with a reason.

## Findings

Order by severity and then affected scope.

### `[Severity] Concise finding title`

- **Confidence:** High / Medium / Low
- **Evidence type:** Executed / Static / Configuration / Documented / Inference
- **Scope:** project, component, or repository-wide
- **Evidence:** clickable `file:line`, diagnostic ID, command output, or metric
- **Standard/local rule:** exact rule name and direct source link
- **Impact:** credible consequence
- **Recommendation:** smallest durable remediation
- **Verification:** exact test, analyzer, build, or review needed

## Verified strengths

Include only controls confirmed by execution or direct configuration plus implementation evidence. Explain their scope and avoid converting configuration presence into proof that a gate passes.

## Prioritized remediation

| Order | Action | Risk reduced | Effort | Verification |
| ---: | --- | --- | --- | --- |

Separate immediate defects, systemic quality-gate improvements, and longer-term investments.

## Limitations and unverified evidence

List unavailable SDKs, workloads, external services, hardware, UI runtime checks, failed restores, skipped projects, stale CI-only evidence, and any dirty-worktree constraints. Never score an unverified area as passing.

## Reproduction appendix

Record:

- exact commands and exit codes;
- tool and SDK versions;
- file/project inventory;
- coverage and mutation summaries;
- analyzer suppressions and accepted baselines;
- all Medium/Low findings omitted from the executive section;
- official source links close to supported claims.
