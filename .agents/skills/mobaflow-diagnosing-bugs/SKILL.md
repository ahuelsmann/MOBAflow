---
name: mobaflow-diagnosing-bugs
description: Diagnose a reproducible or intermittent MOBAflow defect, including runtime, project switching, UI state and Z21 feedback. Use for reported failures or performance regressions, not ordinary planning or a complete quality audit.
---

# MOBAflow diagnosis
Read [AGENTS.md](../../../AGENTS.md) and task-relevant
[instructions](../../../.github/instructions/instructions-index.md).

1. Capture the expected/actual behavior, scope and smallest available reproduction.
   Inspect code, logs and tests as needed to construct it. Keep hypotheses distinct from observations.
2. Build a bounded repeatable probe using NUnit/Moq and existing fakes when testing product behavior.
   Use temporary repositories/files for tooling faults. Never require a live layout to reproduce an automated test.
   For intermittent failures record frequency, timing and controlled inputs; do not start unbounded stress loops.
3. Rank plausible causes and test discriminating predictions, changing one relevant condition at a time.
   In shared UI inspect project identity, stale asynchronous results, subscriptions and runtime projection.
   For Z21 inspect the real execution context before adding dispatch or changing protocol handling.
4. When a fix is requested, make the smallest cohesive change and add meaningful regression coverage.
   Run the checks selected by AGENTS.md, with actual test discovery and platform limitations reported.
5. Explain the cause supported by evidence, changed behavior, validation and remaining uncertainty.
   If reproduction is unavailable, continue useful static investigation and state what evidence is missing;
   do not claim a suspected cause is proven.

Do not launch MOBAflow, deploy to a device or operate hardware without authorization.
Use GitHub CI for Sonar code analysis; no local Sonar/Vortex analysis or hooks.
No Bash-only loop helper, debugger or agent delegation is mandatory.
Source and adaptations: [sources.json](../sources.json); upstream license: [LICENSE](LICENSE).
