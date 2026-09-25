---
description: 'MOBAflow C# identifiers and protocol naming exceptions.'
applyTo: '**/*.cs'
---

# Naming conventions
Use ASCII identifiers. Preserve unrelated existing names during scoped work.

| Identifier | Convention |
| --- | --- |
| Types, members, constants | PascalCase |
| Interfaces | IPascalCase |
| Parameters and locals | camelCase |
| Private fields | _camelCase |
| Async methods returning tasks | Async suffix |

Use meaningful railway/domain names and existing protocol abbreviations.
[Z21 protocol constants](../../Backend/Protocol/Z21Protocol.cs) retain UPPER_SNAKE_CASE where they
map directly to protocol names; do not rename them to apply a generic style rule.

[.editorconfig](../../.editorconfig) owns configured formatting/analyzer rules;
[Moba.sln.DotSettings](../../Moba.sln.DotSettings) supplies IDE settings.
These conventions do not imply that every naming rule is enforced by the compiler.
