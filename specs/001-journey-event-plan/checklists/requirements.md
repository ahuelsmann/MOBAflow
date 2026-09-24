# Specification quality checklist: Journey event plans

> **Superseded in part (2026-09-24):** Legacy feedback sequences, follow-up journeys, runtime start/stop and
> per-run count baselines were removed. Journeys now carry a persisted active flag and events match absolute InPort
> session counts. [spec.md](../spec.md), [data-model.md](../data-model.md) and [contracts/event-plan.md](../contracts/event-plan.md)
> describe the current behavior; this file records the earlier increment.

**Created**: 2026-09-10

**Feature**: [spec.md](../spec.md)

- [x] User value, platform scope and exclusions are explicit.
- [x] Continuous counts, per-port baselines, independent events and idle-only reset reflect the accepted conversation.
- [x] Acceptance scenarios and outcomes are measurable.
- [x] Edge cases cover packet repetition, unseen ports, failure, restart and reset/start concurrency.
- [x] Legacy per-step counting is distinguished from new start-relative thresholds.
- [x] Drag and drop has concrete useful operations and keyboard alternatives.
- [x] Requirements map to stories, design and validation tasks.
- [x] Authoritative source issue #124 exists and actual issue-body/PR metadata governance passes.
- [x] Local domain/runtime/editor implementation and regression test code are present.
- [x] Runtime baseline full suite, 18 focused tests after UI refinement and final Windows FastDebug compile passed; exact counts are recorded in the analysis.
- [ ] Remaining platform/manual UI acceptance and publication gates have produced completion evidence.

Checked specification-quality items do not establish successful feature execution.
