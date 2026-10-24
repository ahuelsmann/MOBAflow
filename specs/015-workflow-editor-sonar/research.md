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

## WF388-LIGHT-01: effective element theme

- Evidence: the 2026-10-24 human f5 Light screenshot shows light descriptions, parameters, step
  numbers and lines on a light graph surface. Direct Application.Current.Resources brush lookup
  in the canvas does not preserve a ThemeResource expression on each generated element.
- Decision: move brush assignments into control-local XAML Style setters and keep the generated
  visuals and existing ActualThemeChanged refresh. A selected border changes style, not a cached
  application brush. Existing WinUI theme keys and text styles supply the palette.
- Rationale: ThemeResource is re-evaluated on theme changes at the target element. Microsoft
  explicitly supports ThemeResource in Style setters: [XAML theme resources](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/xaml-theme-resources).
- Rejected: handcoded Light/Dark colors, forcing Application.RequestedTheme, manual traversal of
  internal WinUI dictionaries, a new resource-resolver abstraction or global theme/DI changes.
- Package evidence: the self-contained SDK payload explicitly copies native/**/*.mui preserving
  relative language paths. The initial f5 candidate omitted all 172 files; matching-source MUI
  supplementation resolved its startup failure in Root's controlled normal launch. Carry that
  payload into every new candidate; do not infer completeness from deps.json runtime DLLs.
- No unresolved design choice remains. This source phase does not grant app-launch authority.
