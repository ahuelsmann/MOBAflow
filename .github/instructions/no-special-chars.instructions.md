---

applyTo: '**/*.cs'
description: 'Avoid special characters in source code to prevent encoding issues'
---

# No Special Characters in Source Code

## CRITICAL: Encoding Corruption Prevention

**DO NOT use the following in C# source files:**

### Forbidden Characters

- **Emojis**
  - **Examples:** `🎯 ✅ 🖐️ 🔵 🛑 🚂`
  - **Problem:** Multi-byte UTF-8, corrupts easily
- **Box-drawing**
  - **Examples:** `├ └ ─ │ ┌ ┐`
  - **Problem:** Not ASCII, encoding-sensitive
- **Arrows**
  - **Examples:** `→ ← ↑ ↓ ➡`
  - **Problem:** Unicode, causes issues
- **German Umlaute**
  - **Examples:** `ä ö ü ß Ä Ö Ü`
  - **Problem:** Use ASCII alternatives in code

### Allowed Alternatives

- **`Debug.WriteLine("✅ Success")`**
  - **Use:** `Debug.WriteLine("Success")`
- **`Debug.WriteLine("🎯 Target")`**
  - **Use:** `Debug.WriteLine("[TARGET] ...")`
- **`StatusMessage = "Loko fährt ↑"`**
  - **Use:** `StatusMessage = "Loko forward"`
- **`"Bogen 45°"`**
  - **Use:** `"Bogen 45 Grad"` or `$"Bogen 45{'\u00B0'}"`
- **`"Löschen"`**
  - **Use:** Use resource files for UI strings

### Where Special Characters ARE Allowed

1. **Resource files (.resx)** - For localized UI strings
2. **Documentation (.md)** - But avoid in code blocks
3. **Comments** - Only ASCII, no emojis

### Code Examples

```csharp
// BAD - Will corrupt
Debug.WriteLine("🎯 Dragging segment");
Debug.WriteLine("✅ Connected successfully");
var text = "Größe: 45°";

// GOOD - Safe ASCII
Debug.WriteLine("[DRAG] Dragging segment");
Debug.WriteLine("[OK] Connected successfully");
var text = $"Groesse: 45{'\u00B0'}";  // Unicode escape for degree
```

### Why This Matters

When tools read/write files with different encoding assumptions:

1. UTF-8 emoji `🎯` (4 bytes: F0 9F 8E AF)
2. Read as Latin-1: `ðŸŽ¯` (4 separate characters)
3. Written back as UTF-8: Corruption multiplies

Each edit cycle makes it worse, eventually producing unreadable garbage like:
`ÃƒÆ'Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ'Ã¢â‚¬Å¡`

### Enforcement

Before committing, run:

```powershell
# Find files with problematic characters
Get-ChildItem -Include "*.cs" -Recurse | 
  Select-String -Pattern "[\x{1F300}-\x{1F9FF}]|[äöüßÄÖÜ]" |
  Select-Object Path, LineNumber
```
