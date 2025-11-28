# 🔄 Undo/Redo + HasUnsavedChanges Integration Analysis

**Datum**: 2025-11-27  
**Thema**: Integration von Undo/Redo-Mechanismus mit HasUnsavedChanges Tracking

---

## 🎯 Ihre Frage

> "Können das 'Unsaved Changes' Tracking und der Undo/Redo Mechanismus voneinander profitieren?"

**Antwort: JA! Absolut!** ✅

---

## 🔍 Aktuelle Situation

### UndoRedoManager

```csharp
// SharedUI/Service/UndoRedoManager.cs
public class UndoRedoManager
{
    // Speichert Solution-States als JSON-Dateien
    private readonly List<string> _history = [];
    private int _currentIndex = -1;
    
    // Auto-Save mit Throttling
    public void SaveStateThrottled(Solution solution)
    {
        // Wartet 2 Sekunden nach letzter Änderung
    }
    
    // Immediate Save
    public async Task SaveStateImmediateAsync(Solution solution)
    {
        // Speichert sofort
    }
    
    public bool CanUndo => _currentIndex > 0;
    public bool CanRedo => _currentIndex < _history.Count - 1;
}
```

### HasUnsavedChanges (Neu hinzugefügt)

```csharp
// SharedUI/ViewModel/MainWindowViewModel.cs
[ObservableProperty]
private bool hasUnsavedChanges = false;

// Wird gesetzt bei:
- NewSolutionAsync()  → true
- LoadSolutionAsync() → false
- SaveSolutionAsync() → false
```

---

## 💡 Integration-Möglichkeiten

### 1️⃣ **Undo/Redo sollte HasUnsavedChanges setzen**

#### Problem (Aktuell)

```csharp
[RelayCommand]
private async Task UndoAsync()
{
    var previousSolution = await _undoRedoManager.UndoAsync();
    if (previousSolution != null)
    {
        Solution.UpdateFrom(previousSolution);
        BuildTreeView();
        UpdateUndoRedoState();
        
        // ❌ FEHLT: HasUnsavedChanges wird NICHT gesetzt!
    }
}
```

**Konsequenz**: Nach Undo hat User Änderungen, aber `HasUnsavedChanges = false`

#### Lösung

```csharp
[RelayCommand]
private async Task UndoAsync()
{
    var previousSolution = await _undoRedoManager.UndoAsync();
    if (previousSolution != null)
    {
        Solution.UpdateFrom(previousSolution);
        BuildTreeView();
        UpdateUndoRedoState();
        
        // ✅ Setze HasUnsavedChanges nach Undo
        HasUnsavedChanges = true;
    }
}

[RelayCommand]
private async Task RedoAsync()
{
    var nextSolution = await _undoRedoManager.RedoAsync();
    if (nextSolution != null)
    {
        Solution.UpdateFrom(nextSolution);
        BuildTreeView();
        UpdateUndoRedoState();
        
        // ✅ Setze HasUnsavedChanges nach Redo
        HasUnsavedChanges = true;
    }
}
```

---

### 2️⃣ **SaveStateThrottled sollte HasUnsavedChanges setzen**

#### Problem (Aktuell)

```csharp
private void OnPropertyValueChanged()
{
    // Property wurde geändert
    _undoRedoManager.SaveStateThrottled(Solution);
    
    // ❌ FEHLT: HasUnsavedChanges wird NICHT gesetzt!
}
```

#### Lösung

```csharp
private void OnPropertyValueChanged()
{
    // Property wurde geändert
    _undoRedoManager.SaveStateThrottled(Solution);
    
    // ✅ Markiere als ungespeichert
    HasUnsavedChanges = true;
}
```

---

### 3️⃣ **"Saved Point" im Undo/Redo Stack**

#### Konzept: Letzte gespeicherte Position merken

```csharp
public class UndoRedoManager
{
    private int _savedStateIndex = -1;  // Index der letzten Save-Operation
    
    public void MarkCurrentAsSaved()
    {
        lock (_lock)
        {
            _savedStateIndex = _currentIndex;
        }
    }
    
    public bool IsCurrentStateSaved()
    {
        lock (_lock)
        {
            return _currentIndex == _savedStateIndex;
        }
    }
}
```

#### Verwendung

```csharp
// Nach SaveSolutionAsync
await _ioService.SaveAsync(Solution, path);
_undoRedoManager.MarkCurrentAsSaved();
HasUnsavedChanges = false;

// Bei Undo/Redo: Check ob saved state
private async Task UndoAsync()
{
    var previous = await _undoRedoManager.UndoAsync();
    if (previous != null)
    {
        Solution.UpdateFrom(previous);
        
        // ✅ Check: Sind wir zurück am saved state?
        HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();
    }
}
```

**Vorteil**: 
- Wenn User Save macht, dann Undo → `HasUnsavedChanges = false` (zurück zu saved)
- Wenn User Save macht, dann Edit → `HasUnsavedChanges = true`

---

### 4️⃣ **New Solution sollte Undo/Redo History clearen**

#### Problem (Aktuell)

```csharp
[RelayCommand]
private async Task NewSolutionAsync()
{
    // ... Dialog, etc.
    
    Solution.UpdateFrom(newSolution);
    
    // ❌ FEHLT: Alte History bleibt bestehen!
    // User könnte Undo machen und alte Solution zurückbekommen
}
```

#### Lösung

```csharp
[RelayCommand]
private async Task NewSolutionAsync()
{
    // ... Dialog, etc.
    
    Solution.UpdateFrom(newSolution);
    
    // ✅ Clear alte History
    _undoRedoManager.ClearHistory();
    
    // ✅ Initialer State für neue Solution
    await _undoRedoManager.SaveStateImmediateAsync(Solution);
    UpdateUndoRedoState();
    
    HasUnsavedChanges = true;
}
```

---

### 5️⃣ **Load Solution sollte Undo/Redo History clearen**

#### Gleiche Logik wie New Solution

```csharp
[RelayCommand]
private async Task LoadSolutionAsync()
{
    var (loadedSolution, path, error) = await _ioService.LoadAsync();
    
    if (loadedSolution != null)
    {
        Solution.UpdateFrom(loadedSolution);
        
        // ✅ Clear alte History
        _undoRedoManager.ClearHistory();
        
        // ✅ Initialer State für geladene Solution
        await _undoRedoManager.SaveStateImmediateAsync(Solution);
        UpdateUndoRedoState();
        
        CurrentSolutionPath = path;
        HasUnsavedChanges = false;  // Gerade geladen = saved
    }
}
```

---

## 📊 Verbesserte Integration

### Vollständiger Flow

```
User Action         | Undo/Redo         | HasUnsavedChanges
--------------------|-------------------|-------------------
New Solution        | ClearHistory()    | true
                    | SaveState()       |
Load Solution       | ClearHistory()    | false
                    | SaveState()       |
Save Solution       | MarkAsSaved()     | false
Edit Property       | SaveThrottled()   | true
Undo                | UndoAsync()       | Check saved point
Redo                | RedoAsync()       | Check saved point
```

---

## 🎯 Implementierungs-Empfehlungen

### Priority 1: Essentiell

1. **Undo/Redo setzt HasUnsavedChanges**
   ```csharp
   HasUnsavedChanges = !_undoRedoManager.IsCurrentStateSaved();
   ```

2. **New/Load cleared History**
   ```csharp
   _undoRedoManager.ClearHistory();
   await _undoRedoManager.SaveStateImmediateAsync(Solution);
   ```

3. **SaveStateThrottled setzt HasUnsavedChanges**
   ```csharp
   HasUnsavedChanges = true;
   ```

### Priority 2: Nice to Have

4. **Saved State Tracking**
   ```csharp
   _undoRedoManager.MarkCurrentAsSaved();
   ```

5. **Dialog vor Clear History**
   - Wenn HasUnsavedChanges && History nicht leer
   - Frage: "Discard undo/redo history?"

---

## 🔄 Vorher/Nachher

### Vorher (Aktuell)

```
User:
1. Load Solution → HasUnsavedChanges = false ✅
2. Edit Property → HasUnsavedChanges = ?     ❌ (nicht gesetzt)
3. Undo          → HasUnsavedChanges = false ❌ (sollte true sein)
4. New Solution  → History bleibt           ❌ (alte Undo-States)
```

### Nachher (Verbessert)

```
User:
1. Load Solution → HasUnsavedChanges = false ✅
                   History cleared          ✅
2. Edit Property → HasUnsavedChanges = true  ✅
3. Undo          → HasUnsavedChanges = Check ✅
4. New Solution  → History cleared          ✅
                   HasUnsavedChanges = true  ✅
```

---

## 💾 State Diagram

```
┌─────────────────────────────────────────────────────────┐
│  Solution State Machine                                 │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  [Loaded/Saved] (HasUnsavedChanges = false)            │
│        │                                                 │
│        ├─ Edit → [Modified] (HasUnsavedChanges = true) │
│        │           │                                     │
│        │           ├─ Save → [Loaded/Saved]            │
│        │           ├─ Undo → Check saved point          │
│        │           └─ New  → Clear history → [Modified]│
│        │                                                 │
│        └─ New → Clear history → [Modified]             │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## ✅ Vorteile der Integration

1. **Konsistentes Tracking**
   - Undo/Redo und HasUnsavedChanges arbeiten zusammen
   - Keine Diskrepanzen mehr

2. **Intelligentes Saved State Tracking**
   - System weiß, ob aktuelle Version = gespeicherte Version
   - Ermöglicht präzises "Discard Changes?"

3. **Clean History bei New/Load**
   - Alte Undo-States werden nicht versehentlich wiederhergestellt
   - Jede Session startet sauber

4. **User Experience**
   - User sieht konsistenten Status
   - Dialog "Unsaved Changes" ist akkurat
   - Keine falschen Positive/Negative

---

## 📝 Zusammenfassung

**Ja, Undo/Redo und HasUnsavedChanges können voneinander profitieren!**

### Was zu tun ist:

1. ✅ **Undo/Redo** → Setzt `HasUnsavedChanges`
2. ✅ **New/Load** → Cleared Undo/Redo History
3. ✅ **Save** → Markiert Saved Point im Stack
4. ✅ **Edit** → Setzt `HasUnsavedChanges = true`

### Resultat:

- ✅ Konsistentes State Management
- ✅ Akkurate "Unsaved Changes" Detection
- ✅ Intelligente Undo/Redo Integration
- ✅ Bessere User Experience

**Die Integration macht beide Features robuster!** 🚀
