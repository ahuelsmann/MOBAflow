# Tasks: Documentation consistency

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/266
**Revision**: 2026-10-21 human decision to use existing Codex/Claude delivery

## Delivery and acceptance

The implementation/handover tasks below replace the earlier API activation
plan. Final exact-commit CI, independent review, integrated-main acceptance and
overall closure are recorded in [Issue #266](https://github.com/ahuelsmann/MOBAflow/issues/266)
and the leading PR; these checkboxes are the pre-integration handover snapshot.

- [x] T001 Specify the subscription-based requirements, ownership and excluded activation.
- [x] T002 Preserve deterministic package/mutation facts and file protections.
- [x] T003 Remove the obsolete API request, model prompts and App auto-publication code.
- [x] T004 Retire the privileged bot workflow; preserve existing Quality entry points and gates.
- [x] T005 Validate the retained offline fact regressions, generator, instructions, links and line endings.
- [ ] T006 Complete current-head CI and Sonar with zero OPEN/CONFIRMED PR findings.
- [ ] T007 Demonstrate real same-PR developer/wiki delivery, existing independent Claude review and actual-main verification.
- [ ] T008 Confirm existing deterministic gates remain effective; complete the owned acceptance/closure handover without activating the retired bot check.

## Superseded work and limits

The former T007 API/model/App credential configuration and API live acceptance,
and T008 bot-loop/race acceptance and required custom-check activation, were
superseded by the human decision, not executed successfully. Do not provision
replacement credentials, transfer subscription tokens to CI, buy API usage or
change branch protection. Preserve historical evidence and separately owned
private-review audit items without silently closing them.
