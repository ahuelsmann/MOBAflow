# Data model impact

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

No entity or persisted field is introduced or changed. Workflow.Actions remains the authoritative
ordered sequence. Graph node position and selection remain existing ViewModel presentation state.
ParameterSummary remains computed from the existing typed action payload and existing notification
paths. The correction adds only private resource-key constants and clearer control flow.

JSON schemas, configuration defaults, public interfaces, Z21 traffic, runtime ownership and
persisted column proportions remain unchanged. No migration or compatibility code is required.
