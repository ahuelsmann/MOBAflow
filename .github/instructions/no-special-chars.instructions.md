---
description: 'C# identifier and encoding conventions.'
applyTo: '**/*.cs'
---

# Identifiers and encoding

- Use ASCII identifiers and English names. Avoid decorative emoji, arrows or box drawings in code/comments.
- Read and write files as UTF-8, preserving the repository's line-ending conventions from `.editorconfig`
  and `.gitattributes`. Unicode itself is valid; mismatched encodings cause corruption.
- Keep UI text English as specified in [AGENTS.md](../../AGENTS.md). Do not introduce localization resources
  solely to work around encoding. Preserve user-provided names and configurable announcement text.
- Keep required symbols and test data when they represent actual content or an encoding regression.
  Check touched files for accidental character corruption; do not transliterate unrelated source or data.