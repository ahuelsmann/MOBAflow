# MOBAflow - Master Instructions (Ultra-Compact)

> Model railway application (MOBA) with focus on use of track feedback points. 
> Journeys (with stops or stations) can be linked to feedback points so that any actions within the application can then be performed based on the feedbacks.
> **Multi-platform system (.NET 9 / .NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)
> 
> **Last Updated:** 2025-02-04 | **Version:** 3.15

---

## 📋 SESSION START CHECKLIST (ALWAYS DO THIS FIRST!)

**At the beginning of EVERY session:**
1. ✅ Read `.github/instructions/.copilot-todos.md` to see what's been done in previous sessions
2. ✅ Check for any "Noch offen ⏳" (open tasks) that need continuation
3. ✅ Don't repeat recommendations that are already in the "Umgesetzt ✅" list
4. ✅ Use this file as the **Cross-Session Knowledge Bridge**

**Workspace Info:**
- **Solution Format:** SLNX (Modern Visual Studio format) - File: `Moba.slnx`
- **No legacy .sln file** - SLNX handles project loading automatically
- **14 Projects in workspace** (see Moba.slnx for complete list)
- **WinUI Debug Logs:** `WinUI\\bin\\Debug\\logs` (relative to repo root)
- **TODO-Liste:** `.github/instructions/.copilot-todos.md`

**When recommending something new:**
- Ask: **"Soll ich das umsetzen?"** (Should I implement this?)
- Wait for explicit "Ja" or "Nein"
- Update `.github/instructions/.copilot-todos.md` accordingly
- Move tasks through: ⏳ → ✅ (with date)

---

## 🎯 CORE PRINCIPLES (Always Follow!)

### **1. Fluent Design First**
- **Always** follow Microsoft Fluent Design 2 principles and best practices
- Use native WinUI 3 controls and patterns (no custom implementations unless absolutely necessary)
- Consistent spacing: `Padding="8"` or `Padding="16"`, `Spacing="8"` or `Spacing="16"`
- Theme-aware colors: `{ThemeResource TextFillColorSecondaryBrush}`, `{ThemeResource DividerStrokeColorDefaultBrush}`
- Typography: `{StaticResource SubtitleTextBlockStyle}`, `{StaticResource BodyTextBlockStyle}`
- **List Controls:** Use `ItemsControl` for lists (NOT `ListView`) - see `winui.instructions.md`

### **2. Holistic Thinking - Never Implement in Isolation**
- **When changing ONE Page, check ALL Pages** for consistency
- **When adding a feature (e.g., Add/Delete buttons), check if it applies to other entity types**
- **When fixing a pattern, fix it everywhere** - not just the current file
- **Think "Application-wide"** - never "just this one page"

**Checklist before ANY UI change:**
1. Does this pattern exist on other Pages? → Apply consistently
2. Does this feature make sense for other entities? → Implement everywhere
3. Am I following the same layout as sibling Pages? → Match exactly
4. Have I checked EntityTemplates.xaml for similar templates? → Reuse patterns

### **3. Pattern Consistency is Non-Negotiable**
- If JourneysPage has Add/Delete buttons → WorkflowsPage, SolutionPage, FeedbackPointsPage MUST have them too
- If one ListView has a header layout → ALL ListViews follow the same layout
- Deviation from established patterns = bugs + extra work + user frustration

### **4. Copy Existing Code - Don't Invent**
- Before implementing anything new: **Search for existing implementations**
- Copy working patterns exactly, then adapt for the new entity
- If it works on JourneysPage, it should work the same way on WorkflowsPage

### **5. Warning-Free Code**
- **NEVER introduce new warnings** when implementing features
- **Fix warnings immediately** - don't defer to "later"
- **Partial method signatures MUST match** the generated code exactly:
  - ✅ `partial void OnXxxChanged(Type value)` → Use `_ = value;` to suppress if unused
  - ❌ `partial void OnXxxChanged(Type _)` → Parameter name mismatch warning!
- **Event handlers must suppress unused parameters**: `_ = e;` or `_ = sender;`
- **IValueConverter parameters are nullable at runtime**: Use `object? value` not `object value`
- **Run build validation** before declaring any task complete

**Warning Patterns to Avoid:**
```csharp
// ❌ WRONG: Parameter name mismatch (CS8826)
partial void OnSelectedItemChanged(ItemViewModel? _) { }

// ✅ CORRECT: Match generated signature, suppress unused
partial void OnSelectedItemChanged(ItemViewModel? value)
{
    _ = value; // Suppress unused parameter warning
    UpdateRelatedState();
}

// ❌ WRONG: Nullable annotation mismatch in converters
public object Convert(object value, ...) // CS8602 at runtime

// ✅ CORRECT: Runtime nullable
public object Convert(object? value, ...)
{
    return value != null ? Visibility.Visible : Visibility.Collapsed;
}
```

### **6. Always Create a Plan (MANDATORY!)**
- **EVERY user request MUST start with a plan** using the `plan` tool
- **No exceptions** - even for "simple" tasks
- Plans ensure systematic approach and prevent oversights
- Use `update_plan_progress` to track completion
- Call `finish_plan` when all steps are done

**Plan Structure:**
```markdown
# Task Title

## Steps
1. Analyze current state
2. Identify required changes
3. Implement changes
4. Verify with build
5. Update documentation
```

### **6.5. Async-Everywhere Pattern (CRITICAL!)**
- **ALWAYS prefer async/await** - it should be the default, not the exception
- **Use async for ALL I/O operations** - file access, network calls, database queries
- **Use async for ALL UI operations** - navigation, page instantiation, dispatching
- **NEVER use `Task.Run` in Service/Manager classes** - only in UI event handlers
- **Use `Task.FromResult` for simple returns** - not `Task.Run(() => value)`
- **Use `Task.CompletedTask` for void async methods** - not empty `return Task.FromResult(0)`

**Async Best Practices:**
```csharp
// ✅ CORRECT: Async for I/O operations
public async Task<AppSettings> LoadSettingsAsync()
{
    var json = await File.ReadAllTextAsync(_filePath);
    return JsonConvert.DeserializeObject<AppSettings>(json);
}

// ✅ CORRECT: Async for UI operations
public async Task NavigateToPageAsync(string tag)
{
    var page = _serviceProvider.GetRequiredService<Page>();
    _contentFrame.Content = page;
    await Task.CompletedTask; // Explicit async completion
}

// ✅ CORRECT: Task.FromResult for simple returns
public Task<int> GetCountAsync()
{
    return Task.FromResult(_items.Count);
}

// ❌ WRONG: Task.Run in service methods (wastes thread pool)
public async Task<int> GetCountAsync()
{
    return await Task.Run(() => _items.Count); // DON'T DO THIS!
}

// ❌ WRONG: Synchronous I/O (blocks thread)
public AppSettings LoadSettings()
{
    var json = File.ReadAllText(_filePath); // DON'T DO THIS!
}
```

**When to use `Task.Run`:**
- ✅ **UI event handlers:** `await Task.Run(() => HeavyComputation())` - offload from UI thread
- ✅ **CPU-intensive work:** Image processing, complex calculations
- ❌ **Service methods:** NEVER - use real async I/O or stay synchronous
- ❌ **Fake async:** NEVER wrap synchronous code just to make it "async"

**Naming Conventions:**
- All async methods MUST end with `Async` suffix
- Example: `LoadAsync()`, `SaveAsync()`, `NavigateToPageAsync()`
- Exception: `async void` event handlers (no suffix needed)

### **7. File Encoding & Line Endings (CRITICAL!)**

**Standards (enforced by `.editorconfig` and `.gitattributes`):**
- **Encoding:** UTF-8 without BOM (default for modern .NET)
- **Line Endings:** CRLF (Windows standard, enforced via `end_of_line = crlf`)
- **Final Newline:** No (`insert_final_newline = false`)

**Configuration Files:**
- `.editorconfig` - IDE-level enforcement (`charset = utf-8`, `end_of_line = crlf`)
- `.gitattributes` - Git-level enforcement (`* text=auto eol=crlf`)

**Checking for Mixed Line Endings (PowerShell):**
```powershell
# Check single file
$content = [System.IO.File]::ReadAllText("path\to\file.cs")
$crlf = ([regex]::Matches($content, "`r`n")).Count
$lf = ([regex]::Matches($content, "(?<!\r)`n")).Count
if ($lf -gt 0) { "MIXED: CRLF=$crlf, LF=$lf" } else { "OK: CRLF=$crlf" }
```

**Fixing Mixed Line Endings:**
```powershell
# Fix single file (normalize to CRLF)
(Get-Content "path\to\file.cs" -Raw).Replace("`r`n","`n").Replace("`n","`r`n") | Set-Content "path\to\file.cs" -NoNewline
```

**When encountering line ending issues:**
1. Check file with the pattern above
2. Fix if mixed (LF > 0)
3. Verify with build
4. Commit with descriptive message

### **8. PowerShell Terminal Optimization (IMPORTANT!)**

**When running many file operations in PowerShell:**

1. **Avoid complex multi-line strings** - PowerShell has issues with special characters like `$`, `<`, `>` in here-strings
2. **Use `[System.IO.File]` methods** for reliable file operations
3. **Keep commands short** - Split complex operations into multiple simple commands
4. **Avoid parallelization via terminal** - Use sequential operations to prevent race conditions

**Best Practices for File Content Creation:**
```powershell
# ❌ WRONG: Complex here-strings with special chars
$content = @"
<Project>
  $(SomeVariable)
</Project>
"@

# ✅ CORRECT: Use create_file tool or line-by-line approach
# Or use [System.IO.File]::WriteAllText with escaped content
```

**For batch file checks (optimized approach):**
```powershell
# Check directory for mixed line endings (efficient)
Get-ChildItem "path\to\dir" -Filter "*.cs" -Recurse | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName)
    $lf = ([regex]::Matches($c, "(?<!\r)`n")).Count
    if ($lf -gt 0) { $_.Name + ": LF=$lf" }
}
```

**Preference Order for File Operations:**
1. **Use `create_file` tool** - Most reliable for new files
2. **Use `replace_string_in_file` tool** - Best for modifications
3. **Use `run_command_in_terminal`** - Only for simple operations or checks
4. **Avoid complex PowerShell scripts** - Terminal has character limits and escaping issues

---

## 📚 Instruction Set Index
- [a11y.instructions.md](./a11y.instructions.md)
- [azure-devops-pipelines.instructions.md](./azure-devops-pipelines.instructions.md)
- [backend.instructions.md](./backend.instructions.md)
- [blazor.instructions.md](./blazor.instructions.md)
- [code-review-generic.instructions.md](./code-review-generic.instructions.md)
- [collections.instructions.md](./collections.instructions.md)
- [copilot-thought-logging.instructions.md](./copilot-thought-logging.instructions.md)
- [csharp.instructions.md](./csharp.instructions.md)
- [devops-core-principles.instructions.md](./devops-core-principles.instructions.md)
- [di-pattern-consistency.instructions.md](./di-pattern-consistency.instructions.md)
- [dotnet-architecture-good-practices.instructions.md](./dotnet-architecture-good-practices.instructions.md)
- [dotnet-framework.instructions.md](./dotnet-framework.instructions.md)
- [dotnet-maui.instructions.md](./dotnet-maui.instructions.md)
- [dotnet-wpf.instructions.md](./dotnet-wpf.instructions.md)
- [genaiscript.instructions.md](./genaiscript.instructions.md)
- [github-actions-ci-cd-best-practices.instructions.md](./github-actions-ci-cd-best-practices.instructions.md)
- [hasunsavedchanges-patterns.instructions.md](./hasunsavedchanges-patterns.instructions.md)
- [instructions.instructions.md](./instructions.instructions.md)
- [localization.instructions.md](./localization.instructions.md)
- [markdown.instructions.md](./markdown.instructions.md)
- [maui.instructions.md](./maui.instructions.md)
- [memory-bank.instructions.md](./memory-bank.instructions.md)
- [pcf-canvas-apps.instructions.md](./pcf-canvas-apps.instructions.md)
- [pcf-code-components.instructions.md](./pcf-code-components.instructions.md)
- [pcf-fluent-modern-theming.instructions.md](./pcf-fluent-modern-theming.instructions.md)
- [powershell.instructions.md](./powershell.instructions.md)
- [prompt.instructions.md](./prompt.instructions.md)
- [self-explanatory-code-commenting.instructions.md](./self-explanatory-code-commenting.instructions.md)
- [test.instructions.md](./test.instructions.md)
- [update-docs-on-code-change.instructions.md](./update-docs-on-code-change.instructions.md)
- [winui.instructions.md](./winui.instructions.md)

---

## 🎯 Current Session Status (Feb 4, 2025)

### ✅ Latest Completed: Plugin System Architecture (Feb 4, 2025) 🔌✨

**Major Achievement:** Complete **Plugin Framework** implementiert für extensible Architecture!

**What Was Implemented:**

1. **Plugin Framework Architecture:**
   - `IPlugin` Interface - Plugin contract
   - `PluginBase` Abstract class - Simplifies plugin development
   - `PluginMetadata` - Version tracking, author info, dependencies
   - `PluginPageDescriptor` - Page registration

2. **Plugin Management Services:**
   - `PluginDiscoveryService` - Auto-discover DLLs in Plugins folder
   - `PluginValidator` - Validate plugins for errors (duplicate tags, missing names, etc.)
   - `PluginLoader` - Lifecycle management (init/unload hooks)

3. **Minimal Plugin Template:**
   - Complete example plugin in `Plugins/MinimalPlugin/`
   - Demonstrates MVVM with CommunityToolkit.Mvvm
   - Shows MainWindowViewModel injection
   - Includes lifecycle hooks, metadata, page registration

4. **Documentation:**
   - ✅ **README.md** - Comprehensive plugin development section
   - ✅ **docs/ARCHITECTURE.md** - Full system architecture with plugin design
   - ✅ **Plugins/MinimalPlugin/README.md** - Template reference
   - ✅ **Updated Instructions** - Plugin best practices

**Key Features:**
- 🎯 **Easy Discovery** - Plugins in `WinUI/bin/Debug/Plugins/` auto-discovered
- ✅ **Validation** - Automatic plugin validation on startup
- 🔄 **Lifecycle Hooks** - OnInitializedAsync(), OnUnloadingAsync()
- 💉 **Full DI Support** - Inject any host service (MainWindowViewModel, IZ21, etc.)
- 📦 **Metadata** - Version, author, dependencies declared
- 🛡️ **Robustness** - App always runs, even if plugins fail
- 🚀 **Copy-Paste Template** - Minimal Plugin ready to duplicate

**Architecture Highlights:**
```
IPlugin Interface (Contract)
    ↓
PluginBase (Abstract Base)
    ↓
MyPlugin : PluginBase
    ↓
PluginDiscoveryService (Auto-Discovery)
    ↓
PluginValidator (Validation)
    ↓
PluginLoader (Registration & Lifecycle)
    ↓
DI Container (Service Resolution)
```

**Benefits:**
- ✅ Developers can add features **without modifying core**
- ✅ **Isolated plugins** with full DI access
- ✅ **Automatic loading** - just drop DLL in folder
- ✅ **Version tracking** via metadata
- ✅ **Error isolation** - broken plugins don't crash app

**Navigation Integration:**
- Plugins appear in NavigationView between last core page and Settings
- Separator automatically added to distinguish plugin pages
- Dynamic menu item creation with icons and titles

**Files Created/Modified:**
- ✅ `Common/Plugins/IPlugin.cs` - Plugin interface
- ✅ `Common/Plugins/PluginBase.cs` - Abstract base class
- ✅ `Common/Plugins/PluginDiscoveryService.cs` - Discovery
- ✅ `Common/Plugins/PluginValidator.cs` - Validation
- ✅ `WinUI/Service/PluginLoader.cs` - Lifecycle management
- ✅ `WinUI/Service/PluginRegistry.cs` - Page registry
- ✅ `WinUI/View/MainWindow.xaml.cs` - Dynamic plugin menu
- ✅ `Plugins/MinimalPlugin/*` - Template plugin
- ✅ `README.md` - Plugin development guide
- ✅ `docs/ARCHITECTURE.md` - Full architecture documentation

**Build Status:** ✅ Zero errors, zero warnings

---

### ✅ Previous Completed: Photo Upload WinUI → MAUI (Feb 4, 2025) 📸🚀

**Problem:** MAUI konnte Fotos aufnehmen, aber nicht zu WinUI hochladen → PhotoPath blieb leer

**Solution:** Vollständige Real-Time Photo Upload Pipeline implementiert

---
