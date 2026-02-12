# Copilot Tips & Tricks für MOBAflow

> Best Practices und Patterns für effektive Nutzung von GitHub Copilot & Copilot Chat in VS

## 🎯 Effektive Prompts für MOBAflow

### 1. **Service/Validator Implementation**
```
"Implement IProjectValidator service following SOLID principles. 
- Use Constructor Injection
- Add comprehensive logging
- Include unit test examples
- Follow MOBAflow naming: Moba.Backend.Service.ProjectValidator"
```

### 2. **ViewModel mit Async Commands**
```
"Create MVVM ViewModel using CommunityToolkit.Mvvm:
- Use [ObservableProperty] for bindable properties
- Use [RelayCommand] for async commands with error handling
- Include proper null checking and logging
- No .Result or .Wait() - only await"
```

### 3. **Unit Tests schreiben**
```
"Write xUnit tests for [ServiceName]:
- Use Moq for mocking dependencies
- Use Enumerable.Range instead of for loops
- Include Theory tests with multiple data points
- Test both success and error cases"
```

### 4. **Refactoring für Performance**
```
"Refactor this code for performance:
- Use LINQ instead of foreach where applicable
- Use string.IsNullOrEmpty() instead of null checks
- Use pattern matching (C# 9+)
- Extract magic numbers to named constants"
```

### 5. **Domain Model Design**
```
"Design a domain model for [Feature]:
- Keep it a pure POCO without business logic
- Add XML documentation
- Use meaningful property names
- Include interfaces for extensibility"
```

---

## 🔄 Slash Commands in Copilot Chat

### `/explain` - Code verstehen
```
/explain
// Erklär mir, was diese Validierungslogik macht
var result = projects.Where(p => p.Locomotives.Count > 0)
                     .Select(p => new { p.Name, LocCount = p.Locomotives.Count })
                     .ToList();
```

### `@workspace` - Repository-wide Suche
```
@workspace "Find all usages of IProjectValidator"
@workspace "Show me all async commands in ViewModels"
@workspace "Where is SignalBoxPlan used?"
```

### `@solutions` - Mehrere Lösungsansätze
```
"Generate 3 different approaches to implement signal state validation:
1. Fluent Builder pattern
2. State Machine pattern  
3. Validator service pattern"
```

### `/doc` - Dokumentation generieren
```
/doc
// Generate XML documentation comments for this method
public ProjectValidationResult ValidateCompleteness(Solution solution)
```

---

## 💡 Häufige Patterns - Copilot hilft besser mit Kontext

### Pattern 1: MVVM Property mit Command
**Schlecht:**
```
"Create a ViewModel property for SignalAspect with a command to change it"
```

**Besser:**
```
"Create a MVVM ViewModel using CommunityToolkit.Mvvm:
- Property: [ObservableProperty] private SignalAspect? _selectedSignal;
- Command: [RelayCommand] async Task ChangeSignalAspectAsync(SignalAspect aspect)
- Include validation and error handling
- Log all state changes
- No .Result or .Wait() - use await
- Follow Moba.SharedUI.ViewModel naming"
```

### Pattern 2: Service Implementation
**Schlecht:**
```
"Create a validation service"
```

**Besser:**
```
"Create IProjectValidator service:
- Interface in Backend/Interface/IProjectValidator.cs
- Implementation in Backend/Service/ProjectValidator.cs
- Constructor inject ILogger<ProjectValidator>
- Use LINQ for all collection operations
- Return ProjectValidationResult with Info/Warning/Error messages
- Add XML documentation for public methods
- Include unit test examples"
```

### Pattern 3: Error Handling
**Schlecht:**
```
"Add error handling to this method"
```

**Besser:**
```
"Add production-ready error handling:
- Catch specific exceptions (JsonException, InvalidOperationException)
- Log errors with context (file path, JSON content length)
- Return meaningful error messages for UI (no technical details)
- Include retry logic if applicable (with Polly pattern)
- Never throw unhandled exceptions - return Result<T> instead"
```

---

## ⚠️ Fallstricke & Workarounds

### Fallstrick 1: Copilot vergisst MVVM Toolkit
**Problem:** Copilot schlägt `INotifyPropertyChanged` vor statt `[ObservableProperty]`

**Workaround:**
```
"Use ONLY CommunityToolkit.Mvvm:
- NOT INotifyPropertyChanged
- Use [ObservableProperty] for all properties
- Use [RelayCommand] for all commands
- Import: using CommunityToolkit.Mvvm.ComponentModel;"
```

### Fallstrick 2: Copilot nutzt `.Result`
**Problem:** Copilot schlägt `.Result` vor, verursacht Deadlocks

**Workaround:**
```
"NEVER use .Result or .Wait()
- Always use async/await
- If sync required: use .GetAwaiter().GetResult()
- Better: refactor to async throughout
- Sign async patterns as: async Task<T>, not Task<T>"
```

### Fallstrick 3: Copilot erstellt `new Service()`
**Problem:** Service wird mit `new` erstellt statt Dependency Injection

**Workaround:**
```
"Use Constructor Injection ONLY:
- Inject dependencies in constructor
- Store in private readonly fields
- NEVER use new Service()
- Register in MobaServiceCollectionExtensions via AddSingleton"
```

### Fallstrick 4: Copilot schreibt zu viel auf einmal
**Problem:** Eine Methode > 50 Zeilen (Hard to maintain)

**Workaround:**
```
"Keep methods small and focused:
- Maximum 20-25 lines per method
- Extract complex logic to separate methods
- Use guard clauses for early returns
- Follow Single Responsibility Principle
- Break up this method into smaller helpers"
```

---

## 🚀 Pro-Tipps für schnellere Entwicklung

### Tip 1: Context Clues geben
```
"In MOBAflow project, we use:
- MVVM Toolkit with [ObservableProperty]
- Constructor Injection for DI
- Custom naming: Moba.Layer.Feature
- No .Result/.Wait() - use await everywhere
Now implement [Feature]..."
```

### Tip 2: Existing Code als Template
```
// Reference existing pattern
"Follow the pattern of Backend/Service/WorkflowService.cs
for this new Backend/Service/SignalValidator.cs:
[Describe what differs]"
```

### Tip 3: Code Review mit Copilot
```
@symbols "Review this code for:
- SOLID principle violations
- Missing null checks
- Performance issues (LINQ vs loops)
- Logging opportunities"
[Paste code]
```

### Tip 4: Multi-Stage Refinement
```
Stage 1: "Generate skeleton with interfaces"
Stage 2: "Add implementation for Create method"
Stage 3: "Add error handling"
Stage 4: "Add logging and unit tests"
```

---

## 📊 Quality Metrics - Was Copilot gut macht

| Aufgabe | Qualität | Tipps |
|---------|----------|-------|
| **MVVM ViewModels** | ⭐⭐⭐⭐ | Immer MVVM Toolkit erwähnen |
| **Unit Tests** | ⭐⭐⭐⭐ | Pattern mit Moq + xUnit zeigen |
| **Service Layer** | ⭐⭐⭐⭐ | DI Pattern + Logging erwähnen |
| **Validation** | ⭐⭐⭐⭐ | Result<T> Pattern + Enums |
| **XAML/WinUI** | ⭐⭐⭐ | Oft zu viel Code-Behind |
| **SQL/Queries** | ⭐⭐ | Besser manuell schreiben |
| **Regex** | ⭐⭐ | Lieber nicht verwenden |
| **Security** | ⭐⭐ | Immer Review + Sicherheits-Audit |

---

## 🎓 Learning Resources für Copilot Prompting

1. **GitHub Copilot Best Practices**
   - https://github.com/features/copilot/best-practices

2. **Prompt Engineering Guide**
   - https://github.com/brexhq/prompt-engineering

3. **MOBAflow-spezifisch**
   - `.github/copilot-instructions.md` (Main rules)
   - `.github/instructions/` (Architecture docs)
   - `.editorconfig` (Code style)

---

## 📝 Checkliste für Copilot-Generated Code

Vor Commit, immer checken:

- [ ] Folgt `.editorconfig` Regeln
- [ ] Nutzt MVVM Toolkit (`[ObservableProperty]`, `[RelayCommand]`)
- [ ] Constructor Injection, kein `new Service()`
- [ ] Kein `.Result` oder `.Wait()` - nur `await`
- [ ] LINQ statt for-Loops
- [ ] Null-Checks und Error Handling
- [ ] XML Documentation für public APIs
- [ ] Unit Tests mit Moq + xUnit
- [ ] Keine Magic Numbers - Named Constants
- [ ] Keine TODOs - in `todos.instructions.md`
- [ ] Build: `dotnet build` erfolgreich
- [ ] Tests: `dotnet test` alle green
- [ ] No ReSharper Warnings

---

**Letzte Aktualisierung:** 2026-02-XX  
**Autor:** MOBAflow Development Team
