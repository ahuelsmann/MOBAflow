---
description: 'Comment guidelines - explain WHY not WHAT'
applyTo: '**/*.cs'
---

# Code Commenting

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
```

### Divider Comments
```javascript
// Bad: Don't use decorative comments
//=====================================
// UTILITY FUNCTIONS
//=====================================
```

## Quality Checklist

Before committing, ensure your comments:
- [ ] Explain WHY, not WHAT
- [ ] Are grammatically correct and clear
- [ ] Will remain accurate as code evolves
- [ ] Add genuine value to code understanding
- [ ] Are placed appropriately (above the code they describe)
- [ ] Use proper spelling and professional language

## Summary

Remember: **The best comment is the one you don't need to write because the code is self-documenting.**
