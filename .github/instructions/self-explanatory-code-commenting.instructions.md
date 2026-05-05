---

description: 'Comment guidelines - explain WHY not WHAT'
applyTo: '**/*.cs'
---

# Code Commenting

## File Header (Mandatory)

Every new `.cs` file must start with the following copyright header as the very first line (before `using` or `namespace`):

```csharp
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
```

This applies to all hand-written `.cs` files. Exclude generated files (`*.g.cs`, `*.g.i.cs`, `*.designer.cs`) and build artifacts (`obj/`, `bin/`).

## Core Rule

**Comment only when explaining WHY, not WHAT. Code should be self-explanatory.**

## Good Comments

- Complex business logic: `// Progressive tax: 10% up to 10k, 20% above`
- Non-obvious algorithms: `// Floyd-Warshall for all-pairs shortest paths`
- API constraints: `// Z21 rate limit: max 20 commands/second`
- Regex patterns: `// Match: username@domain.ext`

## Annotations (use sparingly)

`TODO:`, `FIXME:`, `HACK:`, `NOTE:`, `WARNING:`, `SECURITY:`

## Avoid

- Obvious: `counter++;  // Increment counter`
- Redundant: `return user.Name;  // Return user name`
- Dead code comments
- Changelog in comments (use Git)

```text

### Divider Comments
```javascript
// Bad: Don't use decorative comments
//=====================================
// UTILITY FUNCTIONS
//=====================================
```

## Quality Checklist

Before committing, ensure your comments:

- [ ] Every new `.cs` file has the mandatory copyright header on line 1
- [ ] Explain WHY, not WHAT
- [ ] Are grammatically correct and clear
- [ ] Will remain accurate as code evolves
- [ ] Add genuine value to code understanding
- [ ] Are placed appropriately (above the code they describe)
- [ ] Use proper spelling and professional language

## Summary

Remember: **The best comment is the one you don't need to write because
the code is self-documenting.**
