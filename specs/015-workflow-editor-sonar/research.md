# Research: bounded Issue #378 corrections

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

- Decision: preserve the exact existing output and command contracts. The 13 remote findings
  concern readability and analyzability, not a request for new behavior.
- Rationale: current d6aa332c Linux and Windows checks pass; suppressions or larger refactors
  would obscure the narrow correction and invalidate unrelated evidence.
- Alternatives rejected: dropping event parameters from a WinUI delegate, making a control-bound
  zoom operation static, changing resource values, loosening assertions or increasing baselines.
- Callback design: a discard-parameter event adapter invokes a parameterless refresh. Zoom reset
  resolves the same x:Name through the control's instance namescope; no alternate control/state.
- Coverage: the existing node-presentation test covers notifications and common summaries, but
  announcement truncation and unset/explicit-target branches need boundary assertions.
- Quality evidence: the remote issue query returned total 13 OPEN/CONFIRMED. Rule-description
  API reads returned HTTP 400; fixes follow the actual issue messages and inspected source.
- Startup safety: inspect only source/configuration that passes secrets scanning. Never infer
  that a separate project file alone disables host startup, network discovery or autoload.
  Any unproven startup condition remains a named blocker in the acceptance recipe.

No unresolved product decision remains for the code correction scope.
