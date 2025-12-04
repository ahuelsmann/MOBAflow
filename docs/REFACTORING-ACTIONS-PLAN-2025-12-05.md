# Action-Refactoring: IAction/ActionBase Pattern

**Status**: 🔄 In Progress (90% - Build-Fehler wegen Architektur-Constraints)  
**Erstellt**: 2025-12-04  
**Priorität**: Mittel

---

## 🎯 Ziel

Refaktorierung von `WorkflowAction + Dictionary Parameters` zu `IAction + Polymorphie`

---

## 📊 Aktueller Stand

### ❌ Build-Fehler:
**Problem**: Actions in `Domain/Actions` → referenzieren `Backend.Services.ActionExecutionContext`

**Root Cause**: Clean Architecture Regel verletzt (Domain darf Backend nicht referenzieren)

---

## 📋 Plan für Morgen (2025-12-05)

### Phase 1: Revert & Bug-Fix (10 Min) ✅ ERLEDIGT
```bash
git reset --hard HEAD
git clean -fd
```

**Dann**: Nur Announcement-Bug fixen in `Backend/Services/ActionExecutor.cs`:

```csharp
private async Task ExecuteAnnouncementAsync(WorkflowAction action, ActionExecutionContext context)
{
    // ✅ Graceful handling statt Exception
    if (action.Parameters == null || !action.Parameters.ContainsKey("Message"))
    {
        Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: Missing Message");
        return;
    }
    
    if (context.SpeakerEngine == null)
    {
        Debug.WriteLine($"    ⚠ Announcement skipped: No SpeakerEngine");
        return;
    }
    // ... rest bleibt gleich
}
```

### Phase 2: Actions Refactoring (60 Min)

#### Architektur-Entscheidung:
**Actions gehören nach `Backend/Actions/`**, da sie:
1. Execution-Logik enthalten
2. `ActionExecutionContext` benötigen (Backend.Services)
3. Dependencies wie IZ21, ISpeakerEngine verwenden

#### Implementierung:
1. **Backend/Actions/IAction.cs** - Interface
2. **Backend/Actions/ActionBase.cs** - Basis-Klasse
3. **Backend/Actions/AnnouncementAction.cs** - Mit Message, VoiceName Properties
4. **Backend/Actions/CommandAction.cs** - Mit Bytes Property
5. **Backend/Actions/AudioAction.cs** - Mit FilePath Property

#### Lazy Loading Pattern:
```csharp
// Domain/WorkflowAction.cs (bleibt für JSON)
public class WorkflowAction
{
    public Dictionary<string, object>? Parameters { get; set; }  // ✅ Bleibt
    
    [JsonIgnore]
    public Backend.Actions.IAction? RuntimeAction { get; set; }  // ✅ Runtime
}

// Backend/Factory/ActionFactory.cs
public static IAction Create(WorkflowAction data)
{
    return data.Type switch
    {
        ActionType.Announcement => new AnnouncementAction
        {
            Message = data.Parameters?["Message"]?.ToString() ?? ""
        },
        // ...
    };
}
```

---

## 🚨 Lessons Learned

1. **Clean Architecture ist strikt**: Domain darf Backend nie referenzieren
2. **Actions mit Logic gehören nach Backend**, nicht Domain
3. **Lazy Loading**: WorkflowAction (Domain) → RuntimeAction (Backend)

---

## 🔗 Files Changed Today

- ✅ `WinUI/View/EditorPage.xaml` - UI-Optimierungen
- ✅ `SharedUI/ViewModel/StationViewModel.cs` - Grüne Farbe #60A060
- ✅ `SharedUI/ViewModel/MainWindowViewModel.Z21.cs` - Debug-Logs
- ✅ `Backend/Services/ActionExecutor.cs` - Message-Parameter Check

**Nächste Session**: Actions nach Backend verschieben + Lazy Loading implementieren

---

**Review**: Pending (nach erfolgreichem Build morgen)
