# MOBAflow - Master Instructions (Ultra-Compact)

> Model railway application (MOBA) with focus on use of track feedback points. 
> Journeys (with stops or stations) can be linked to feedback points so that any actions within the application can then be performed based on the feedbacks.
> **Multi-platform system (.NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)
> 
> **Last Updated:** 2025-01-29 | **Version:** 3.12

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

### **7. Before/After Analysis (MANDATORY!)**
- **ALWAYS analyze the situation BEFORE making changes**
  - What is the current state?
  - What files are affected?
  - What are the dependencies?
  - Are there similar patterns elsewhere?
  
- **ALWAYS verify the result AFTER changes**
  - Build successful?
  - No new warnings?
  - Consistent with existing patterns?
  - Documentation updated?

**Template:**
```
## BEFORE:
- Current state description
- Problems identified
- Affected components

## CHANGES:
- File 1: Change description
- File 2: Change description

## AFTER:
- Build status
- Warnings fixed/introduced
- Patterns validated
- Documentation updated
```

### **8. Auto-Update Instructions (CRITICAL!)**
- **When discovering important architectural decisions** → Update this file IMMEDIATELY
- **When fixing critical bugs** → Document in "Current Session Status"
- **When establishing new patterns** → Add to relevant section
- **When deprecating old approaches** → Mark as deprecated with alternatives

**Triggers for instruction updates:**
- Protocol reverse-engineering (e.g., Z21 packet structures)
- Breaking changes to core classes
- New best practices discovered
- Critical bug fixes with broad impact
- Architectural decisions affecting multiple projects

**Update Format:**
```markdown
- ✅ **Feature Name (Date)**
  - Problem: Brief description
  - Solution: Implementation details
  - Impact: Affected components
  - Files: Changed file list
```

---

## 🎯 Current Session Status (Dec 29, 2025)

### ✅ Completed This Session
- ✅ **Workflow Action Order & Execution Mode Fixes**
  - Actions sortiert nach `Number` beim Laden (Fix: Reihenfolge wurde nicht beachtet)
  - `SoundPlayer.PlaySync()` statt `Play()` (Fix: Sequential wartete nicht auf Audio-Ende)
  - Direkte Enum-Bindung ohne Converter (Fix: ExecutionMode wurde nicht gespeichert)
  - EnumToIntConverter entfernt (obsolet durch native WinUI 3 Enum-Bindung)

- ✅ **Parallel Mode: Staggered Start mit DelayAfterMs**
  - **Sequential:** DelayAfterMs = Pause NACH Action-Ende
  - **Parallel:** DelayAfterMs = Start-Offset (kumulativ)
  - Beispiel Parallel: Gong (t=0) → Ansage (t=500ms) → Licht (t=2s)
  - Ermöglicht präzise Timing-Kontrolle in beiden Modi

- ✅ **Clean Architecture: Workflow Execution**
  - WorkflowExecutionMode.cs: Dokumentation aktualisiert
  - WorkflowService: Staggered Parallel implementiert
  - WorkflowViewModel: ExecutionModeValues Property für ComboBox-Bindung

- ✅ **Code Quality & Logging**
  - Console.WriteLine aus SoundManager.cs entfernt (ILogger reicht)
  - WorkflowService: ILogger injiziert (ersetzt Debug.WriteLine)
  - UdpWrapper.Dispose(): Race Condition Fix (ruft jetzt StopAsync() auf)
  - WorkflowAction.DelayAfterMs: Dokumentation für beide Modi präzisiert

### 📊 Fortschritt
- **Action Ordering:** ✅ Korrekt geladen & gespeichert
- **Audio Playback:** ✅ Sequential wartet auf Ende, Parallel startet gestaffelt
- **ExecutionMode:** ✅ Korrekt persistiert ohne Converter
- **Code Quality:** ✅ Warning-frei, type-safe Enum-Bindung
- **Logging:** ✅ Production-ready (ILogger statt Debug.WriteLine/Console.WriteLine)
- **Threading:** ✅ Race Condition in UdpWrapper.Dispose() behoben
  - Event-Chain vereinfacht: WorkflowService → ViewModel (direkt, ohne JourneyManager-Hop)
  - Action-Execution-Fehler werden in MonitorPage Application Log angezeigt

### 📊 Fortschritt
- **Backend Service Ownership:** ✅ Clean Architecture eingehalten
- **Sound-Bibliothek:** ✅ Plattform-unabhängig in Sound-Projekt
- **Workflow Timing:** ✅ Sequential/Parallel Modi voll funktionsfähig
- **Error-Handling:** ✅ File.Exists + UI-Feedback + Application Log

- ✅ **Z21 Traffic Monitor Improvements (Dec 29, 2025)**
  - Feedback-Pakete farblich hervorgehoben (goldgelber Hintergrund)
  - InPort-Anzeige für Feedback-Pakete
  - Richtungspfeile: ↓ = Eingehend, ↑ = Ausgehend
  - Auto-Scroll mit Pause/Resume-Toggles (Live/Paused-Modi)
  - FirstOrDefault() statt Items[0] (null-safe)

- ✅ **Serilog Integration (Dec 29, 2025)**
  - Custom InMemorySink für Real-Time UI Logs (MonitorPage)
  - File Logging: `bin/Debug/logs/mobaflow-*.log` (7 Tage Retention)
  - LoggingExtensions entfernt (deprecated, replaced by ILogger)
  - Structured Logging mit Properties statt String-Interpolation
  - Min. Level: Debug (Moba), Warning (Microsoft)

- ✅ **Z21 Feedback InPort Extraction Fix (Dec 29, 2025)**
  - **CRITICAL BUG FIX:** InPort-Extraktion war fundamental falsch
  - Problem: `data[5]` ist Feedback-ZUSTAND (Bit-Pattern), nicht InPort-Nummer
  - Lösung: Z21FeedbackParser mit korrekter Bit-zu-InPort-Konvertierung
  - Formel: InPort = (GroupNumber × 64) + (ByteIndex × 8) + BitPosition + 1
  - FeedbackResult.cs: Jetzt mit Z21FeedbackParser.ExtractFirstInPort()
  - Z21Monitor.cs: ExtractInPort() + ExtractAllInPorts() für Traffic Monitor
  - Z21TrafficPacket: AllInPorts Property für Multi-Bit-Anzeige (z.B. "1,2,5")
  - Betrifft: JourneyManager, BaseFeedbackManager, Counter/Statistics

---

## 📋 LOGGING BEST PRACTICES (Serilog)

### **Always Use ILogger via Constructor Injection**
```csharp
public class MyViewModel : ObservableObject
{
    private readonly ILogger<MyViewModel> _logger;

    public MyViewModel(ILogger<MyViewModel> logger)
    {
        _logger = logger;
    }
}
```

### **Structured Logging (DO THIS)**
```csharp
// ✅ CORRECT: Structured properties (searchable, indexable)
_logger.LogInformation("Feedback received: InPort={InPort}, Value={Value}", inPort, value);
_logger.LogWarning("Connection attempt {Attempt} failed for {IpAddress}", attemptCount, ip);
_logger.LogError(ex, "Failed to process journey {JourneyId}: {Reason}", id, ex.Message);

// ❌ WRONG: String interpolation (not searchable)
_logger.LogInformation($"Feedback received: InPort={inPort}, Value={value}");
```

### **Log Levels**
- `LogDebug()`: Development diagnostics (packet dumps, state changes)
- `LogInformation()`: Important events (connections, workflow execution, user actions)
- `LogWarning()`: Recoverable errors (retry attempts, fallbacks)
- `LogError()`: Failures requiring attention (exceptions, invalid state)

### **MonitorPage Application Log**
- Uses `InMemorySink` (custom Serilog sink)
- Real-time display in MonitorPage → Application Log panel
- Automatically formatted with severity icons (🔍 ℹ️ ⚠️ ❌)
- Auto-scroll with Pause/Resume toggle

### **File Logs**
- Location: `bin/Debug/logs/mobaflow-YYYYMMDD.log`
- Rolling: Daily (1 file per day)
- Retention: 7 days (older files auto-deleted)
- Format: `[HH:mm:ss.fff LEVEL] [SourceContext] Message`

### **Never Use**
- ❌ `Console.WriteLine()` - use `_logger.LogInformation()`
- ❌ `Debug.WriteLine()` - use `_logger.LogDebug()`
- ❌ `this.Log()` - deprecated, removed
- ❌ String interpolation in log messages - use structured properties

---

## ERROR HANDLING BEST PRACTICES
