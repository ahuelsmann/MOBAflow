# Data model impact

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

No entity or persisted field is introduced or changed. Workflow.Actions remains the authoritative
ordered sequence. Graph node position and selection remain existing ViewModel presentation state.
ParameterSummary remains computed from the existing typed action payload and existing notification
paths. The correction adds only private resource-key constants and clearer control flow.
The WF388-LIGHT-01 follow-up adds local XAML styles and applies them to the existing generated
visuals. These styles are not model fields or serialized state and do not alter layout values.

The correction and theme phases change no JSON schema, configuration default, public interface,
Z21 traffic, runtime ownership or persisted column proportion. The feature as a whole, compared
with `main`, does change `WorkflowsPageLayoutSettings` in `Common/Configuration/AppSettings.Sections.cs`:
`ActionsColumnWidth` (pixels, default 300) is removed, `ActionsColumnStarValue` (default 2) and
`IsGraphViewActive` (default `true`) are added. These are configuration defaults and persisted
column proportions of the Workflows page; removed JSON fields are ignored on load, so no migration
or compatibility code is required.

The review follow-up adds `WorkflowViewModel.GetCanvasExtent()` (computed from node positions,
never smaller than 1600 x 1200) plus the node-size constants it needs. These are presentation
values, not persisted state.
