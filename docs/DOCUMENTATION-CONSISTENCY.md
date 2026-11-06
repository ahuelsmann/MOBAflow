# Documentation consistency

Documentation and the canonical user wiki under `docs/wiki/` are part of each
assigned issue's delivery. Codex and the existing independent Claude review
process maintain them in the same leading PR as the implementation. A passing
structural check alone does not establish semantic accuracy.

## Source-derived facts

The [Quality workflow](../.github/workflows/quality.yml) continues to check
package references, central versions and active mutation configuration facts.
Run from the task worktree root:

```powershell
python scripts/documentation_consistency.py
python scripts/documentation_consistency.py --write
python scripts/Test-DocumentationConsistency.py
```

`--write` updates only marked source-derived tables and repeated native-package
version references. Package purposes and license prose remain hand-authored;
new packages are explicitly flagged for purpose/license review. The offline
regressions retain generator idempotency, reference/version checks, license and
surrounding-prose preservation, malformed-input and path protections.

## Documentation impact in the existing delivery

1. The assigned delivery owner compares the issue's actual base-to-head changes
   with affected implementations, callers, host composition, developer guidance
   and the canonical user wiki. Record affected pages and corrections, or a
   concrete source-grounded reason that no change is necessary, in the PR.
2. Make necessary corrections in that issue's existing leading PR and assigned
   worktree. Documentation Lead contributes when documentation expertise is
   needed; one writer retains ownership until an explicit handover. Do not open
   a separate documentation slice PR for the same issue.
3. Give Chief Project Manager the current head/base, documentation/wiki impact,
   relevant checks, actual acceptance, remaining limits and confirmed writer end.
   Chief Project Manager alone qualifies the draft and marks it Ready.
4. The existing Claude plan independently reviews the current comparison,
   including affected documentation and wiki claims. Reviewers remain separate
   from implementers/fixers. Source-grounded contradictions are findings; missing
   evidence is a stated limitation, not a consistency pass. Review is not a Ready
   prerequisite and no additional review automation is introduced.
5. After behavior-changing review fixes, reassess documentation/wiki impact and
   refresh affected evidence. A changed comparison needs fresh independent review
   through the existing plan. Current CI/Sonar and all actual acceptance still apply.
6. After the verified merge, Chief Project Manager and the delivery owner confirm
   the affected documentation against the actual integrated `main`. Record the
   exact merged commit and result in the issue/PR handover. Any remaining drift
   stays visible with its owner in the same central queue; do not claim overall
   currency or completion while an accepted requirement remains open.

The [PR template](../.github/PULL_REQUEST_TEMPLATE.md) captures the documentation
impact. [AGENTS.md](../AGENTS.md#completion) and
[master coordination](../.github/instructions/master-coordination.instructions.md)
define the existing ownership, review and integration boundaries.

## Subscription usage and removed activation

The human decision on 2026-10-21 replaced the separately activated API/GitHub-App
bot with this existing Codex/Claude delivery process. The API request runner,
model prompts, privileged publication workflow and their obsolete tests are
removed. There is no documentation-bot activation, App auto-commit, additional
API budget or new subscription-to-CI integration to configure.

Use the existing native clients under their approved subscription login and
usage limits. Do not silently switch to usage-based API billing, buy extra usage,
export login tokens to CI or introduce a replacement automation. At an allowance
limit, coordinate waiting through Chief Project Manager. Existing credentials
used by other work are not read, changed or deleted by this change.

The former custom bot check is not a required check. The existing Quality,
Sonar, instruction and documentation-link gates remain in place. They provide
structural evidence; semantic acceptance comes from the actual delivery and
independent review. No branch-protection change is part of this delivery.

## Acceptance and evidence

T007 now uses a real assigned leading PR with documented developer/wiki impact,
independent review through the existing Claude plan and an actual integrated-main
comparison. A synthetic repository test is not that acceptance. T008 confirms
that the applicable existing deterministic checks remain effective; it does not
activate the retired API-bot check. Required-rule administration remains outside
this delivery.

The original offline/API implementation is historical evidence, not proof of
live API acceptance. The former API/App activation and bot-loop acceptance were
superseded, not performed successfully. Historical private-review audit items
remain separately identified; they are not silently treated as closed.

See [the feature specification](../specs/011-documentation-consistency/spec.md)
and [Issue #266](https://github.com/ahuelsmann/MOBAflow/issues/266) for the
requirements, current acceptance evidence and overall completion status.
