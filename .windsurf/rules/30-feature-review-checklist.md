description: >
  Persistent feature-review checklist for MOBAflow to validate C#/.NET
  best practices after implementation.
trigger: always_on
---

<!-- markdownlint-disable MD003 MD041 -->

# Feature Review Checklist

Use this checklist when implementing or reviewing a feature in MOBAflow.

## Core Design Questions

- Check whether the change still follows single responsibility at the
  method, class, and service level.
- Check whether the abstraction is broader than necessary for the requested task.
- Prefer extending an existing pattern only when that keeps responsibilities clear.
- If the change introduces a new abstraction, verify that it removes
  complexity instead of hiding it.

## Service and Static Code Questions

- Check whether a small injected service would be clearer than a new
  static helper or utility type.
- Avoid generic helper buckets that mix unrelated behavior.
- If a static type is used, verify that it is narrowly scoped, pure, and justified.

## Async, Cancellation, and Concurrency Questions

- Check whether async and await boundaries remain explicit and correct.
- Check whether cancellation should be accepted, propagated, or
  respected by the changed code.
- Check whether the change introduces fire-and-forget behavior, hidden
  blocking, or thread-affinity risks.
- For UI-related work, check whether the existing EventBus and
  UI-dispatch boundaries are still respected.

## Coupling and Layering Questions

- Check whether the feature increases coupling between layers,
  services, ViewModels, or infrastructure concerns.
- Check whether the implementation belongs in the current layer for the
  MOBAflow architecture.
- Avoid leaking platform-specific concerns into shared or domain layers.
- Check whether the feature reuses existing patterns instead of
  introducing a one-off design.

## Validation Questions

- Check whether the implementation is still easy to test and reason
  about.
- Check whether relevant automated tests or targeted validation steps
  were added or suggested.
- End feature work with a short summary of the main design risks, if any remain.
