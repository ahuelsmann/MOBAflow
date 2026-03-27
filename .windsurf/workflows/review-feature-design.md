description: >
  Review a MOBAflow feature implementation against C#/.NET best
  practices and design quality questions.
---

<!-- markdownlint-disable MD003 MD026 MD041 -->

# Review Feature Design

Use this workflow after implementing a feature or when reviewing a
proposed feature approach.

1. Identify the affected layers, projects, and main responsibilities
   touched by the feature.
2. Review the implementation against these questions:
   - Does the change preserve single responsibility for methods,
     classes, and services?
   - Is any new abstraction broader or more generic than necessary?
   - Would a small injected service be clearer than a new static helper?
   - Are async, await, and cancellation boundaries clean and explicit?
   - Did the change introduce hidden coupling across layers, UI,
     infrastructure, or lifecycle concerns?
3. If the feature touches WinUI, XAML, EventBus delivery, or UI
   dispatch, explicitly review those boundaries as part of the design
   check.
4. If the feature introduces a new service, verify that constructor
   injection, interface usage, and lifetime expectations remain clear.
5. If the feature extends an existing pattern, confirm that the
   extension is consistent with the surrounding code rather than
   creating a parallel pattern.
6. Suggest the most relevant targeted validation commands and tests
   for the changed area.
7. End with a concise review summary using this structure:
   - what is sound
   - what should be simplified
   - any hidden risks
   - recommended validation
