---
description: 'Maintain concise, evidence-based repository agent guidance.'
applyTo: 'AGENTS.md,.github/copilot-instructions.md,.github/instructions/**/*.md'
---

# Maintaining agent instructions

- Keep repository-wide behavior, constraints and validation in [AGENTS.md](../../AGENTS.md).
  Use specialized files for durable technical knowledge and link them from the index; avoid duplicate workflows.
- Include `description` and a narrow `applyTo` glob in specialized `.instructions.md` files.
  Root AGENTS.md and the Copilot entry point do not need frontmatter.
- Verify paths, interfaces, commands and platform assumptions against current source/build files.
  Prefer links to real implementations and tests over long, incomplete code examples or fixed line numbers.
- State the trigger, intended behavior and relevant exception. Avoid absolute slogans, arbitrary size limits,
  mandatory tool names, routine approval gates and blanket testing requirements.
- Separate enforced constraints from optional advice and historical plans. A reference to a tool, skill, tracker
  or deployment procedure does not require installing it, contacting it or performing a deployment.
- For model-specific revisions, consult current official documentation and record source links with the review date.
  Keep model selection and permission configuration outside these documents unless explicitly requested.
- Validate Markdown structure, relative links, scoped globs and consistency across entry points.
  For an instruction-only edit, .NET tests and app builds add no evidence; use the AGENTS.md documentation checks.