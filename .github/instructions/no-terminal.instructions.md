---
description: TEMPORARY restriction - Do not use terminal commands
applyTo: "**"
---

# TEMPORARY RESTRICTIONS (ACTIVE!)

## Terminal Usage FORBIDDEN

**DO NOT use `run_command_in_terminal` tool!**

### Reason
Terminal commands frequently fail, corrupt files (encoding issues), and cause cascading problems.
Examples of past issues:
- Encoding corruption in markdown files (UTF-8 emoji characters turned into garbage)
- Multi-line strings failing in PowerShell
- Commands timing out or being cancelled
- File content being duplicated or corrupted

### Instead use these tools:

| Task | Tool to Use |
|------|-------------|
| Create new files | `create_file` |
| Edit existing files | `replace_string_in_file` or `multi_replace_string_in_file` |
| Read files | `get_file` |
| Find files | `file_search` |
| Search code | `code_search` |
| Build solution | `run_build` (this is allowed - it's not terminal) |
| Get build errors | `get_errors` |

### Exceptions (only if explicitly requested by user)

- Git commands (when user specifically asks)
- dotnet CLI commands (when user specifically asks, prefer `run_build` otherwise)

### Duration

**This restriction is TEMPORARY** and will be removed when terminal reliability improves.

---

**Added:** 2025-01-07
**Reason:** Multiple terminal failures causing file corruption and wasted time
