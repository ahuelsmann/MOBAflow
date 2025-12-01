# Test Migration Guide - Action Hierarchy → WorkflowAction Pattern

**Status:** Work in Progress
**Created:** 2025-01-20
**Estimated Effort:** 3-4 hours

## Overview

The refactoring from Action hierarchy (`Base`, `Command`, `Audio`, `Announcement`) to `WorkflowAction` + Parameters pattern requires significant test updates.

---

## Affected Files

### Backend Tests (Priority: High)
- `Test\Backend\WorkflowTests.cs` - 4 TestAction classes, 15+ usages
- `Test\Backend\WorkflowManagerTests.cs` - 1 TestAction class, 12+ usages  
- `Test\Backend\StationManagerTests.cs` - 1 TestAction class, 5+ usages
- `Test\Backend\ActionTests.cs` - Complete rewrite needed (tests Action execution)

### SharedUI Tests (Priority: Medium)
- `Test\SharedUI\EditorViewModelTests.cs`
- `Test\SharedUI\ValidationServiceTests.cs`
- `Test\SharedUI\MauiAdapterDispatchTests.cs`
- `Test\SharedUI\WinUIAdapterDispatchTests.cs`
- `Test\SharedUI\*JourneyViewModelTests.cs`

### Unit Tests (Priority: Low)
- `Test\Unit\SolutionTest.cs`
- `Test\Unit\SolutionInstanceTests.cs`
- `Test\Unit\NewSolutionTests.cs`
- `Test\Unit\PlatformTest.cs`

---

## Migration Patterns

### Pattern 1: TestAction Class Replacement

**❌ Old Pattern:**
```csharp
file class TestAction : Moba.Backend.Model.Action.Base
{
    private readonly Action _callback;
    
    public TestAction(Action callback)
    {
        _callback = callback;
    }
    
    public override ActionType Type => ActionType.Command;
    
    public override Task ExecuteAsync(ActionExecutionContext context)
    {
        _callback();
        return Task.CompletedTask;
    }
}
```

**✅ New Pattern:**
```csharp
// No TestAction class needed - use WorkflowAction directly
var testAction = new WorkflowAction
{
    Name = "Test Action",
    Number = 1,
    Type = ActionType.Command,
    Parameters = new Dictionary<string, object>
    {
        ["Address"] = 123,
        ["Speed"] = 80,
        ["Direction"] = "Forward"
    }
};

// For callback tracking, use WorkflowService execution events
var executionLog = new List<WorkflowAction>();
// Subscribe to WorkflowService events to track execution
```

---

### Pattern 2: Workflow Creation

**❌ Old Pattern:**
```csharp
var testAction = new TestAction(() => executed = true);
var workflow = new Workflow
{
    Name = "Test Workflow",
    Actions = new List<Moba.Backend.Model.Action.Base> { testAction }
};
```

**✅ New Pattern:**
```csharp
var testAction = new WorkflowAction
{
    Name = "Test Action",
    Type = ActionType.Command,
    Parameters = new Dictionary<string, object>()
};

var workflow = new Workflow
{
    Name = "Test Workflow",
    Actions = new List<WorkflowAction> { testAction }
};
```

---

### Pattern 3: Action Execution Testing

**❌ Old Pattern:**
```csharp
file class TestActionWithContext : Moba.Backend.Model.Action.Base
{
    private readonly Action<ActionExecutionContext> _callback;
    
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        _callback(context);
        await Task.CompletedTask;
    }
}

// Usage
var action = new TestActionWithContext(ctx =>
{
    Assert.That(ctx.Journey, Is.Not.Null);
    Assert.That(ctx.Station, Is.Not.Null);
});
```

**✅ New Pattern:**
```csharp
// Test WorkflowService execution with context
var action = new WorkflowAction
{
    Name = "Test Action",
    Type = ActionType.Command,
    Parameters = new Dictionary<string, object>()
};

var context = new ActionExecutionContext
{
    Journey = testJourney,
    Station = testStation,
    Solution = testSolution
};

// Test via WorkflowService
await _workflowService.ExecuteAsync(workflow, context);

// Verify context was used (requires WorkflowService instrumentation or mocking)
```

---

### Pattern 4: Async Action Testing

**❌ Old Pattern:**
```csharp
file class AsyncTestAction : Moba.Backend.Model.Action.Base
{
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        await Task.Delay(100);
        executed = true;
    }
}
```

**✅ New Pattern:**
```csharp
// Async behavior is now handled by WorkflowService
// Test async execution through WorkflowService API
var action = new WorkflowAction
{
    Name = "Async Action",
    Type = ActionType.Command,
    Parameters = new Dictionary<string, object>()
};

var workflow = new Workflow { Actions = new List<WorkflowAction> { action } };

// Execute asynchronously
await _workflowService.ExecuteAsync(workflow);

// Verify async completion
Assert.That(executed, Is.True);
```

---

## Namespace Fixes

### GlobalUsings.cs

**❌ Old:**
```csharp
global using Moba.Backend.Model;
global using Moba.Backend.Model.Action;
```

**✅ New:**
```csharp
global using Moba.Domain;
global using Moba.Backend.Services;
```

### Individual Test Files

Replace:
- `using Moba.Backend.Model;` → `using Moba.Domain;`
- `Moba.Backend.Model.Action.Base` → Removed (use `WorkflowAction`)
- `Moba.Backend.Model.Enum.ActionType` → `Moba.Domain.ActionType`
- `Moba.Backend.Model.Action.ActionExecutionContext` → `Moba.Backend.Services.ActionExecutionContext`

---

## Testing Strategy

### Phase 1: Simple Namespace Replacements (~30min)
1. Fix `Test\Unit\*.cs` files (simple namespace replacement)
2. Fix `Test\SharedUI\*.cs` files (namespace + minor ViewModel updates)
3. Run: `dotnet build Test/Test.csproj` to verify

### Phase 2: Backend Test Refactoring (~2-3h)
1. Start with `Test\Backend\WorkflowTests.cs`:
   - Remove TestAction classes
   - Replace with WorkflowAction instances
   - Update assertions to work with new pattern
2. Repeat for `Test\Backend\WorkflowManagerTests.cs`
3. Repeat for `Test\Backend\StationManagerTests.cs`
4. Run: `dotnet test Test/Backend/*.cs` to verify

### Phase 3: Action Execution Tests (~1h)
1. Decide on `Test\Backend\ActionTests.cs` approach:
   - Option A: Delete (Action execution now in WorkflowService)
   - Option B: Rewrite as WorkflowService integration tests
2. Add WorkflowService tests if needed
3. Run full test suite: `dotnet test`

---

## Verification Checklist

- [ ] All `using Moba.Backend.Model` replaced
- [ ] No `Action.Base` references remain
- [ ] All `List<Action.Base>` → `List<WorkflowAction>`
- [ ] Test\Backend builds without errors
- [ ] Test\SharedUI builds without errors
- [ ] Test\Unit builds without errors
- [ ] All tests pass (or are marked as skipped with TODO)
- [ ] No `Moba.Backend.Model.Enum.ActionType` references

---

## Next Steps

1. **Close Visual Studio** (to unlock WinUI files)
2. Complete WinUI namespace migration (Backend.Model → Domain)
3. Start Test migration with Phase 1 (Unit + SharedUI)
4. Continue with Phase 2 (Backend tests)
5. Full solution build + test run

---

## Notes

- **WorkflowService Execution:** Tests should verify workflow execution through `WorkflowService.ExecuteAsync()`, not by calling `Action.ExecuteAsync()` directly
- **Context Verification:** Use mocking or test instrumentation to verify ActionExecutionContext is passed correctly
- **Test Coverage:** Some tests may need complete rewrites if they tested internal Action logic (now in WorkflowService)

---

**Estimated Total Time:** 4 hours
**Priority:** High (blocks solution build)
**Dependencies:** WinUI namespace migration
