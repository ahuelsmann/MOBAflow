---
description: Choose a practical Windsurf model strategy for MOBAflow work without overusing Arena Mode.
---

# Choose Model

Use this workflow when the user is unsure which model to use for the next task.

1. Classify the task into one of these buckets:
   - routine implementation
   - broad refactoring or architecture
   - difficult debugging or root-cause analysis
   - high-risk decision that benefits from comparison
2. Recommend one default model first, not Arena Mode by default.
3. Use this practical strategy unless the user asks otherwise:
   - routine implementation: prefer the user's standard coding model
   - broad refactoring or architecture: prefer a stronger reasoning or large-context model
   - difficult debugging or root-cause analysis: prefer a stronger reasoning model
   - high-risk decision: consider Arena Mode
4. Recommend Arena Mode only when comparison is worth the extra time and cost, for example:
   - architecture trade-offs
   - uncertain refactoring direction
   - conflicting candidate solutions
   - expensive mistakes
5. If Arena Mode is recommended, suggest comparing only two focused candidates instead of many models.
6. If the user mentions models such as `o1` or `DeepSeek-R1`, distinguish clearly between:
   - models available directly in the Windsurf selector
   - models available only through external tooling or MCP integrations
7. End with a short recommendation in this format:
   - default model
   - whether Arena Mode is recommended
   - why
   - optional fallback model
