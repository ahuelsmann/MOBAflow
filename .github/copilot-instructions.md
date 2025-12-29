# MOBAflow - Master Instructions (Ultra-Compact)

> Model railway application (MOBA) with focus on use of track feedback points. 
> Journeys (with stops or stations) can be linked to feedback points so that any actions within the application can then be performed based on the feedbacks.
> **Multi-platform system (.NET 10)**  
> MOBAflow (WinUI) | MOBAsmart (MAUI) | MOBAdash (Blazor)
> 
> **Last Updated:** 2025-12-29 | **Version:** 3.10

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

### **5. Warning-Free Code (NEW!)**
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

### 📊 Fortschritt
- **Action Ordering:** ✅ Korrekt geladen & gespeichert
- **Audio Playback:** ✅ Sequential wartet auf Ende, Parallel startet gestaffelt
- **ExecutionMode:** ✅ Korrekt persistiert ohne Converter
- **Code Quality:** ✅ Warning-frei, type-safe Enum-Bindung
  - Event-Chain vereinfacht: WorkflowService → ViewModel (direkt, ohne JourneyManager-Hop)
  - Action-Execution-Fehler werden in MonitorPage Application Log angezeigt

### 📊 Fortschritt
- **Backend Service Ownership:** ✅ Clean Architecture eingehalten
- **Sound-Bibliothek:** ✅ Plattform-unabhängig in Sound-Projekt
- **Workflow Timing:** ✅ Sequential/Parallel Modi voll funktionsfähig
- **Error-Handling:** ✅ File.Exists + UI-Feedback + Application Log
---

## ERROR HANDLING BEST PRACTICES
