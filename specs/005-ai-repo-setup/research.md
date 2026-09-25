# Research and decisions
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
Reviewed 2026-09-25 against main 2b65379fb4f4c6cb23394ea9d44467c6801f027f.

- Decision: remove redundant filesystem/ripgrep/fetch and obsolete Azure DevOps defaults.
  Rationale: native tools serve these tasks; fixed-checkout filesystem access is wrong in worktrees.
  Alternative: an extra portable launcher adds dependencies without demonstrated need.
- Decision: retain remote Sonar MCP using the cross-platform command name; document CLI prerequisites.
  No local analysis hooks. Secrets hook uses pwsh and its own worktree, warns if CLI is missing,
  propagates the scanner's hook result without separately logging the prompt payload.
- Decision: isolated Specify 1.0.11 via uv tool run; leave personal 1.0.4 unchanged.
  Preserve overrides and compare common.ps1 before retaining local adaptations.
- Decision: registry references managed manifests; document modified managed files honestly.
  Never rewrite hashes just to hide modifications.
- Decision: read-only remote resolver with tracking preference, otherwise one GitHub remote.
  Ambiguous or non-GitHub tracking fails visibly; temporary local Git fixtures test destinations.
- Decision: Python standard-library tomllib/json for offline config/skill/link validation.
  Limit checks to active guidance; exclude fenced examples and template placeholder paths.
- Decision: independent read-only research covers pipelines and source-linked instruction examples.

## Official sources
- [Codex configuration](https://learn.chatgpt.com/docs/config-file/config-basic)
- [MCP](https://learn.chatgpt.com/docs/extend/mcp)
- [Skills](https://learn.chatgpt.com/docs/build-skills)
- [Hooks](https://learn.chatgpt.com/docs/hooks)
- [Spec Kit 1.0.11](https://github.com/github/spec-kit/releases/tag/v1.0.11)
