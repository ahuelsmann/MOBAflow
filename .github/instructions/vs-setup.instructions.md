# MOBAflow - Visual Studio Recommended Settings & Extensions

> Best Practices für optimale Copilot + Code Quality Integration in VS 2026

## 🎯 Essential VS Extensions

### Code Quality & Analysis
- ✅ **ReSharper** (or Rider) - Static analysis, refactoring
- ✅ **SonarLint** - Real-time code quality feedback
- ✅ **StyleCop Analyzers** - Code style enforcement
- ✅ **Roslynator** - Additional C# analyzers

### Copilot & AI
- ✅ **GitHub Copilot** - AI-assisted code generation
- ✅ **GitHub Copilot Chat** - AI chat in IDE
- ✅ **IntelliCode** - Context-aware suggestions

### Productivity
- ✅ **Prettier** - Code formatter
- ✅ **Rainbow Braces** - Brace highlighting
- ✅ **GitLens** - Git integration
- ✅ **REST Client** - API testing (REST)

### Performance
- ✅ **Productivity Power Tools** - Enhanced IDE features
- ✅ **Thunder** - Performance monitoring

---

## ⚙️ Keyboard Shortcuts (Recommended)

| Action | Shortcut | Use Case |
|--------|----------|----------|
| Copilot Suggest | `Ctrl+Alt+\` | Get Copilot suggestion |
| Copilot Chat | `Ctrl+Alt+'` | Open Copilot Chat |
| Quick Fix | `Ctrl+.` | ReSharper quick fixes |
| Refactor | `Ctrl+R, Ctrl+R` | Refactoring menu |
| Format Doc | `Ctrl+K, Ctrl+D` | Format entire document |
| Go to Definition | `F12` | Jump to class/method definition |
| Find Usages | `Shift+F12` | Find all references |
| Run Tests | `Ctrl+R, Ctrl+A` | Run all tests |
| Debug | `F5` | Start debugging |

---

## 🎨 Recommended Visual Studio Theme

**Theme:** Dark (Microsoft)  
**Color Scheme:** Visual Studio Dark  
**Font:** Consolas / Cascadia Code (better for coding)  
**Font Size:** 11-12pt (readability)

---

## 🔍 Editor Configuration

### In `Tools → Options → Text Editor → C# → Code Style`

```
[Recommended Settings]
Tabs:
  - Use spaces: YES
  - Tab size: 4
  - Indent size: 4

Line Numbers: ON
Word Wrap: OFF (keep code visible)
Whitespace Guides: ON (see indentation)

Highlighting:
  - Highlight matching braces: ON
  - Highlight references: ON
  - Highlight related keywords: ON
```

---

## 🐛 Debugging Tips

### Debug Launch Profiles (.vs/launch.json)

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "WinUI (Debug)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/WinUI/bin/Debug/MOBAflow.exe",
      "args": [],
      "stopAtEntry": false,
      "console": "integratedTerminal",
      "cwd": "${workspaceFolder}/WinUI"
    },
    {
      "name": "Tests",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "test",
      "program": "dotnet",
      "args": ["test", "Test/Test.csproj"],
      "stopAtEntry": false
    }
  ]
}
```

---

## 🧪 Test Explorer Settings

### In `Test Explorer`
- ✅ Show test details pane
- ✅ Group by project
- ✅ Auto-run tests on build (optional)
- ✅ Show only failed tests (filter)

### Test Keyboard Shortcuts
```
Run All Tests: Ctrl+R, Ctrl+A
Run Test at Cursor: Ctrl+R, Ctrl+T
Debug Test at Cursor: Ctrl+R, Ctrl+D
```

---

## 📊 Performance Profiling

### Memory Profiler (.NET Memory Allocation Profiler)
```
Debug → Performance Profiler → .NET Memory Allocation
- Trace memory allocations
- Find memory leaks
- Identify GC pressure
```

### CPU Profiler
```
Debug → Performance Profiler → CPU Usage
- Find performance bottlenecks
- Identify hot paths
- Analyze call stacks
```

---

## 🔧 Useful VS Commands (Ctrl+Shift+P)

| Command | Function |
|---------|----------|
| `format document` | Auto-format file |
| `rename symbol` | Refactor symbol name |
| `extract method` | Extract code to new method |
| `organize usings` | Sort & remove unused usings |
| `analyze performance` | Open performance profiler |
| `run tests` | Run unit tests |
| `build solution` | Build entire solution |
| `clean solution` | Clean build artifacts |

---

## 🎯 Copilot Chat Commands in VS

```powershell
# In Copilot Chat, use @-mentions and slash commands:

@workspace /explain
// Explain this complex method

@solution /generate tests
// Generate unit tests for method

@project /refactor
// Suggest refactoring improvements

@git /explain changes
// Explain recent git changes

/clear
// Clear chat history
```

---

## 📋 Pre-Commit Checklist (in VS)

Before committing, run:

```powershell
# Terminal in VS (Ctrl+`)

# 1. Run pre-commit hook manually
.git/hooks/pre-commit.cmd

# 2. Run tests
dotnet test

# 3. Check code style
dotnet format --verify-no-changes

# 4. Check for warnings
dotnet build /p:EnforceCodeStyleInBuild=true

# If all OK → Commit
git commit -m "feat(feature): description"
```

---

## 🚀 Setup Script for New Developers

```powershell
# Run this after cloning MOBAflow

# 1. Install recommended extensions
code --install-extension GitHub.Copilot
code --install-extension GitHub.Copilot-Chat
code --install-extension SonarSource.sonarlint-vscode
code --install-extension microsoft.vscode-roslynator

# 2. Restore NuGet packages
dotnet restore

# 3. Install local tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-outdated

# 4. Run initial build
dotnet build

# 5. Run tests
dotnet test

# 6. Configure git hooks
git config core.hooksPath .git/hooks

# You're ready to go! 🚀
```

---

## 💡 Pro Tips for Maximum Productivity

### Tip 1: Use Copilot Chat for Architecture Decisions
```
@workspace "Explain how SignalBoxViewModel works and where would be the best place 
to add signal state change logging"
```

### Tip 2: Quick Fixes with ReSharper
```
Position cursor on error → Ctrl+. → Select fix
Often faster than manual refactoring!
```

### Tip 3: Search Symbol Definitions
```
Ctrl+F12 in file → Go to Type (opens symbol search)
Shows inheritance hierarchy
```

### Tip 4: Git Blame for Context
```
Right-click line → Git → Blame
See who changed this and why (commit message!)
```

### Tip 5: Test-Driven with Test Explorer
```
1. Write failing test (Red)
2. Impl implementation (Green)
3. Refactor (Refactor)
All in Test Explorer UI
```

---

## ⚠️ Common Mistakes (and Fixes)

| Problem | Cause | Fix |
|---------|-------|-----|
| "NuGet not found" | Dependencies not restored | `dotnet restore` |
| Copilot suggests `.Result` | Copilot doesn't know async pattern | Remind: "Use await, never .Result" |
| ReSharper warnings ignored | Warnings not shown | Enable: Tools → Options → Code Analysis |
| Tests not discovered | Project not built | `dotnet build` first |
| Formatting conflicts | EditorConfig not loaded | Reload solution (Ctrl+Shift+F5) |

---

**VS Version Used:** Visual Studio 2026 Enterprise  
**Recommended:** Update to latest insider build monthly  
**Performance:** Should run smoothly on 8GB+ RAM systems
