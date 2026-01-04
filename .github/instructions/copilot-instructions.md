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
1. ✅ Read `.copilot-todos.md` to see what's been done in previous sessions
2. ✅ Check for any "Noch offen ⏳" (open tasks) that need continuation
3. ✅ Don't repeat recommendations that are already in the "Umgesetzt ✅" list
4. ✅ Use this file as the **Cross-Session Knowledge Bridge**

**Workspace Info:**
- **Solution Format:** SLNX (Modern Visual Studio format) - File: `Moba.slnx`
- **No legacy .sln file** - SLNX handles project loading automatically
- **14 Projects in workspace** (see Moba.slnx for complete list)

**When recommending something new:**
- Ask: **"Soll ich das umsetzen?"** (Should I implement this?)
- Wait for explicit "Ja" or "Nein"
- Update `.copilot-todos.md` accordingly
- Move tasks through: ⏳ → ✅ (with date)

---

## 🎯 CORE PRINCIPLES (Always Follow!)

### **1. Fluent Design First**
- **Always** follow Microsoft Fluent Design 2 principles and best practices
- Use native WinUI 3 controls and patterns (no custom implementations unless absolutely necessary)
- Consistent spacing: `Padding="8"` or `Padding="16"`, `Spacing="8"` or `Spacing="16"`
- Theme-aware colors: `{ThemeResource TextFillColorSecondaryBrush}`, `{ThemeResource DividerStrokeColorDefaultBrush}`
- Typography: `{StaticResource SubtitleTextBlockStyle}`, `{StaticResource BodyTextBlockStyle}`

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

### ✅ Latest Completed: Photo Upload WinUI → MAUI (Feb 4, 2025) 📸🚀

**Problem:** MAUI konnte Fotos aufnehmen, aber nicht zu WinUI hochladen → PhotoPath blieb leer

**Solution:** Vollständige Real-Time Photo Upload Pipeline implementiert

**Architecture:**

1. **MAUI Camera Integration:**
   - `MediaPicker.Default.CapturePhotoAsync()` → Foto aufnehmen
   - `PhotoUploadService` → Upload zu WinUI REST-API
   - `RestApiDiscoveryService` → Automatische Server-IP-Erkennung (Broadcasting + Config)

2. **WinUI REST-API (ASP.NET Core):**
   - `PhotoUploadController.UploadPhoto()` → HTTP POST Endpoint
   - `PhotoStorageService` → Speicherung in `AppData\Local\MOBAflow\photos\temp\`
   - `SignalR PhotoHub` → Real-Time Notification an WinUI

3. **Real-Time Photo Assignment:**
   - SignalR `OnPhotoUploaded` → Event in WinUI
   - `MainWindowViewModel.AssignLatestPhoto()` → Automatische Zuweisung zu Lok/Wagon
   - `MovePhotoToCategory()` → Verschiebung von `temp/` → `locomotives/` oder `wagons/`

**Fixes Applied:**
- ✅ **Path Bug Fix:** `baseDir = MOBAflow` (NICHT `MOBAflow\photos`) → Verhindert doppelten `photos\photos\` Pfad
- ✅ **MAUI REST-API Discovery:** Broadcasting + Config-Fallback
- ✅ **SignalR Hub:** WinUI verbindet automatisch zu `localhost:5001/photos-hub`
- ✅ **File.Move with overwrite:** Vermeidet IOException bei existierenden Dateien

**Impact:**
- ✅ **MAUI → WinUI Photo Upload:** Funktioniert vollständig
- ✅ **Automatic Assignment:** Foto wird automatisch zu ausgewählter Lok/Wagon zugewiesen
- ✅ **Real-Time:** SignalR-Benachrichtigung innerhalb von Millisekunden
- ✅ **Photo Storage:** `photos/locomotives/{guid}.jpg` oder `photos/wagons/{guid}.jpg`
- ✅ **Build Status:** Zero errors, zero warnings

**Files Modified:**
- `SharedUI/ViewModel/MainWindowViewModel.Train.cs`: `MovePhotoToCategory` Path-Fix
- `MAUI/Service/PhotoUploadService.cs`: Upload-Implementierung
- `MAUI/Service/RestApiDiscoveryService.cs`: Server-Discovery
- `WinUI/Controllers/PhotoUploadController.cs`: REST-API Endpoint
- `WinUI/Service/PhotoHubClient.cs`: SignalR Client
- `SharedUI/Service/PhotoStorageService.cs`: File Storage Logic

**Key Pattern:**
```csharp
// ✅ CORRECT: Base directory WITHOUT "photos" subfolder
var baseDir = Path.Combine(..., "MOBAflow");
var tempPath = Path.Combine(baseDir, tempPhotoPath); // tempPhotoPath = "photos/temp/xyz.jpg"
// → C:\...\MOBAflow\photos\temp\xyz.jpg ✅

// ❌ WRONG: Would create double "photos" path
var photoDir = Path.Combine(..., "MOBAflow", "photos");
var tempPath = Path.Combine(photoDir, tempPhotoPath);
// → C:\...\MOBAflow\photos\photos\temp\xyz.jpg ❌
```

---

### 🔧 Session Summary (Feb 4, 2025)

**Focus:** Photo Upload Pipeline + Real-Time Communication

**Key Achievements:**
- ✅ **MAUI Camera → WinUI:** End-to-End Photo Upload funktioniert
- ✅ **SignalR Real-Time:** Sofortige Foto-Benachrichtigung
- ✅ **Automatic Assignment:** Foto wird automatisch zu Lok/Wagon zugewiesen
- ✅ **Path Bug Fix:** Verhindert doppelten `photos\photos\` Pfad

**Next Steps (Future Sessions):**
- 📸 **Photo Preview in WinUI:** Image Control in Properties Panel
- 📸 **Photo Delete:** Button zum Entfernen von Fotos
- 📸 **Photo Gallery:** Mehrere Fotos pro Lok/Wagon (Array statt String)
- 🌐 **Cloud Sync:** Optional Azure Blob Storage für Foto-Backup
---

## 📚 Session History

**Detailed session logs moved to:**
- [Session Archive - January 2025](./session-archive-jan-2025.md)

**Key Milestones:**
- ✅ **Feb 4, 2025:** Photo Upload WinUI → MAUI (Real-Time with SignalR)
- ✅ **Feb 3, 2025:** Async-Everywhere Pattern Implementation
- ✅ **Jan 31, 2025:** TrainsPage (Locomotives/Wagons Inventory)
- ✅ **Jan 31, 2025:** AnyRail Import Fix (96/96 connections)
- ✅ **Jan 31, 2025:** Track-Graph Architecture (Pure Topology-First)
- ✅ **Jan 31, 2025:** Piko A-Gleis Geometry Catalog (23+ track types)

---
