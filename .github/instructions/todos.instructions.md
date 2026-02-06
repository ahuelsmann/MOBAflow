---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-10 (Architecture Refactoring Phase Started)

---

## 📋 QUALITY IMPROVEMENTS & AUDITS COMPLETED

### UiDispatcher Best Practices ✅
- ✅ **DI Pattern:** Always use `AddUiDispatcher()` extension method (not direct `AddSingleton`)
- ✅ **InvokeOnUi()** vs **EnqueueOnUi():**
  - `InvokeOnUi()` - Direct execution if on UI thread, else enqueue
  - `EnqueueOnUi()` - ALWAYS enqueue, breaks out of PropertyChanged chains
- ✅ **Fire-and-Forget:** Must use `.ContinueWith()` for error handling
- ✅ **ConfigureAwait:** Use `false` in library code, NOT in UI code
- ✅ **CancellationToken:** Always accept and propagate
- ✅ **Reference:** See `.github/instructions/uidispatcher-best-practices.md`

### Async/Void & Fire-and-Forget Audit ✅
**Status:** 🟢 GOOD - All issues fixed

**Violations Found & Fixed:**
- ✅ `TrainControlPage.OnLoaded()` - Converted async void → sync void
- ✅ `SaveSpeedStepsSettingAsync()` - Added `.ContinueWith()` error handling
- ✅ `HealthCheckService` - Fire-and-forget with proper logging

**Results:**
- 50+ files audited
- 15+ event handlers checked
- 1 async void violation FIXED
- 1 fire-and-forget issue FIXED

**Code Review Checklist:**
- [ ] Event Handlers: All are `sync void` (not `async void`)
- [ ] Async Work in Events: Extracted to separate methods
- [ ] Fire-and-Forget: Has error handling (ContinueWith/logging)
- [ ] Async All The Way: `await` used throughout chain
- [ ] ConfigureAwait: `false` in library code
- [ ] CancellationToken: Accepted and propagated
- [ ] Exception Handling: All async operations wrapped try/catch
- [ ] No Deadlocks: No `.Result` or `.Wait()` on async methods

**Reference:** See `.github/instructions/async-void-and-fire-and-forget-audit.md`

### Solution Quality Analysis Framework ✅
**8 Analysis Dimensions Identified:**

1. **Code Quality & Maintainability**
   - Complexity, duplication, naming, magic numbers
   - Tools: NDepend, SonarQube, Roslyn Analyzers

2. **Architecture Analysis**
   - Dependency graph, circular dependencies, layer violations
   - Tools: Visual Studio Architecture Explorer, NDepend

3. **Test Coverage & Testing Strategy**
   - Branch/line coverage, testing pyramid, flaky tests
   - Tools: Coverlet, OpenCover, ReportGenerator

4. **Performance & Scalability**
   - Memory usage, CPU hot spots, disk I/O, network latency
   - Tools: Visual Studio Profiler, dotTrace, PerfView

5. **Security & Dependency Management**
   - Vulnerability scanning, outdated packages, CVEs
   - Tools: NuGet Audit, WhiteSource, Snyk

6. **Design Patterns & Consistency**
   - MVVM, DI, Factory patterns, anti-patterns, code style
   - Tools: EditorConfig, StyleCop, Roslyn Analyzers

7. **Documentation & Knowledge Management**
   - Code comments, README, ADRs, technical debt tracking
   - Tools: Docfx, GitHub Wiki, Markdown

8. **CI/CD & Monitoring**
   - Build times, test execution, deployment frequency
   - Tools: Azure DevOps Pipelines, SonarQube, Grafana

**Quick Wins (Phase 1 - Immediate):**
- [ ] Enable Roslyn Analyzers in project
- [ ] Setup Coverlet for code coverage baseline
- [ ] Run `dotnet list package --outdated`
- [ ] Document critical paths for testing

**Reference:** See `.github/instructions/solution-analysis-and-quality-framework.md`

---

## 🏗️ ARCHITECTURE ISSUES & REFACTORING ROADMAP

### Critical Architecture Issues Identified

#### Issue #1: MainWindowViewModel - God Object ⚠️
**Current State:**
- 9 partial files (Counter, Z21, Health, Journey, Workflow, Settings, Solution, Train, Commands)
- ~800 LOC across all partials
- Too many responsibilities mixed

**Problems:**
- Traffic Monitoring ≠ UI State Management
- Z21 Connection ≠ Train Selection
- Health Status ≠ Solution Navigation

**Solution (Phase 1):** Extract into 3 dedicated services

---

#### Issue #2: Domain Model - Missing Aggregates & Value Objects ⚠️
**Current State:**
```csharp
project.Locomotives.Add(loco);  // Direct collection access
```

**Problems:**
- Collections directly exposed (no encapsulation)
- No aggregation boundaries
- No validation on add/remove
- Difficult to track changes

**Solution (Phase 2):** Proper Aggregates with bounded contexts
- ✅ Already Started: GridConfig, GridPosition, ConnectionPointDirection created

---

#### Issue #3: Backend Service Coupling ⚠️
**Current State:**
- WorkflowService depends on: ActionExecutionContext, IZ21, ISpeakerEngine, AnnouncementService, ILogger
- ActionExecutionContext depends on: ISpeakerEngine, IZ21, IUiDispatcher, ILogger

**Problem:** Too many direct dependencies (God Object Pattern)

**Solution (Phase 2):** Event-driven or Command Pattern

---

#### Issue #4: Frontend-Backend Coupling ⚠️
**Current State:**
```csharp
// TrainControlPage.xaml.cs
private ILocomotiveService _service;  // ← Direct backend call!
```

**Problem:** View directly depends on Backend Services (breaks MVVM)

**Solution (Phase 1):** IDataFacade - Single entry point

---

#### Issue #5: Z21 Integration - Not Isolated ⚠️
**Current State:**
- Z21 event handling scattered in MainWindowViewModel.Z21.cs
- Auto-connect logic mixed with ViewModel state
- No abstraction layer
- Difficult to test

**Solution (Phase 1):** Z21ConnectionService

---

### Phase 1: Immediate Wins (Low Risk - 1 Week)

**Goal:** Extract 3 feature-specific services from MainWindowViewModel (~50% size reduction)

#### 1.1: Z21ConnectionService
- [ ] Create `Backend/Z21/Z21ConnectionService.cs`
- [ ] Extract all Z21 event handling from `MainWindowViewModel.Z21.cs`
- [ ] Implement: `ConnectAsync()`, `DisconnectAsync()`, `Status` property
- [ ] Add events: `OnConnected`, `OnDisconnected`, `OnConnectionFailed`
- [ ] Register in DI: `services.AddSingleton<Z21ConnectionService>()`
- [ ] Update MainWindowViewModel to use service instead of direct IZ21
- [ ] Write unit tests: Z21ConnectionServiceTests.cs
- [ ] Verify: Build passes ✅

**Key Pattern:**
```csharp
public class Z21ConnectionService
{
    public async Task ConnectAsync() { ... }
    public bool IsConnected { get; private set; }
    public event Action? OnConnected;
}
```

---

#### 1.2: TrafficMonitoringService
- [ ] Create `Backend/Traffic/TrafficMonitoringService.cs`
- [ ] Extract counter logic from `MainWindowViewModel.Counter.cs`
- [ ] Implement: `RecordFeedback()`, `CurrentCount` property, `TargetLapCount`
- [ ] Add events: `CountChanged`, `OnTargetReached`
- [ ] Register in DI: `services.AddSingleton<TrafficMonitoringService>()`
- [ ] Update MainWindowViewModel to delegate to service
- [ ] Write unit tests: TrafficMonitoringServiceTests.cs
- [ ] Verify: Build passes ✅

**Key Pattern:**
```csharp
public class TrafficMonitoringService
{
    public int CurrentCount { get; private set; }
    public void RecordFeedback(int inPort) { ... }
    public event Action? OnTargetReached;
}
```

---

#### 1.3: HealthStatusService
- [ ] Create `Backend/Health/HealthStatusService.cs`
- [ ] Extract health check logic from `MainWindowViewModel.HealthStatus.cs`
- [ ] Implement: `PerformHealthCheckAsync()`, `StartPeriodicChecks()`
- [ ] Add properties: `SpeechServiceStatus`, `IsSpeechServiceHealthy`
- [ ] Add events: `StatusChanged`
- [ ] Register in DI: `services.AddSingleton<HealthStatusService>()`
- [ ] Update MainWindowViewModel to use service
- [ ] Write unit tests: HealthStatusServiceTests.cs
- [ ] Verify: Build passes ✅

**Key Pattern:**
```csharp
public class HealthStatusService
{
    public string SpeechServiceStatus { get; set; }
    public async Task PerformHealthCheckAsync() { ... }
    public event Action<string>? StatusChanged;
}
```

---

#### 1.4: IDataFacade (Single Entry Point for Data Access)
- [ ] Create `SharedUI/Service/IDataFacade.cs` interface
- [ ] Create `SharedUI/Service/DataFacade.cs` implementation
- [ ] Methods: `GetAllLocomotiveSeriesAsync()`, `GetAllCitiesAsync()`, `SaveTrainControlSettingsAsync()`
- [ ] Add error handling and logging
- [ ] Register in DI: `services.AddScoped<IDataFacade, DataFacade>()`
- [ ] Update ViewModels to use IDataFacade instead of direct service calls
- [ ] Update TrainControlPage to use facade
- [ ] Write unit tests: DataFacadeTests.cs
- [ ] Verify: Build passes ✅

**Benefits:**
- ViewModels need only 1 interface instead of 5
- Error handling at single point
- Easy to mock for tests
- Easy to add caching/logging later

---

#### 1.5: Update MainWindowViewModel
- [ ] Inject Z21ConnectionService, TrafficMonitoringService, HealthStatusService
- [ ] Remove Z21.cs partial - logic moved to Z21ConnectionService
- [ ] Remove Counter.cs partial - logic moved to TrafficMonitoringService
- [ ] Remove HealthStatus.cs partial - logic moved to HealthStatusService
- [ ] Connect service events to ViewModel properties
- [ ] Verify: Build passes ✅, All tests pass ✅
- [ ] Code review: MainWindowViewModel ~400 LOC (from ~800)

---

#### 1.6: Verify & Test
- [ ] Full solution build: `dotnet build` ✅
- [ ] All tests pass: `dotnet test` ✅
- [ ] App functionality unchanged (manual smoke test)
- [ ] No runtime errors during Z21 connection
- [ ] No runtime errors during health checks

---

### Phase 2: Medium Priority (1-2 Weeks)

**Goal:** CQRS, Z21 Abstraction, Repository Pattern, Event-Driven

#### 2.1: CQRS (Command Query Responsibility Segregation)
- [ ] Create `Backend/CQRS/Commands/ICommand.cs` interface
- [ ] Create `Backend/CQRS/Commands/ICommandHandler.cs` interface
- [ ] Create `Backend/CQRS/Queries/IQuery.cs` interface
- [ ] Create `Backend/CQRS/Queries/IQueryHandler.cs` interface
- [ ] Create `Backend/Workflow/Commands/ExecuteWorkflowCommand.cs`
- [ ] Create `Backend/Workflow/Commands/ExecuteWorkflowCommandHandler.cs`
- [ ] Create `Backend/Workflow/Queries/GetAllWorkflowsQuery.cs`
- [ ] Create `Backend/Workflow/Queries/GetAllWorkflowsQueryHandler.cs`
- [ ] Register in DI: Command/Query handlers
- [ ] Update WorkflowService to use CQRS pattern
- [ ] Write tests for command/query handlers

**Concept:**
- **Commands:** Change state (ExecuteWorkflow, CreateWorkflow) - complex with side effects
- **Queries:** Read data (GetAllWorkflows) - simple, can be cached

---

#### 2.2: Z21 Subdomain Isolation
- [ ] Create `Backend/Subdomain/Z21/` folder structure
- [ ] Move Z21ConnectionService to subdomain
- [ ] Create Z21 Domain Events: `Z21ConnectedEvent`, `FeedbackReceivedEvent`
- [ ] Create Z21 event publisher integration
- [ ] Create `Backend/Subdomain/Z21/Z21CommandService.cs`
- [ ] Create `Backend/Subdomain/Z21/Z21FeedbackService.cs`
- [ ] Extract protocol handling into separate layer
- [ ] Write integration tests for Z21 subdomain

---

#### 2.3: Repository Pattern
- [ ] Create `Backend/Repository/IRepository<T>.cs` interface
- [ ] Create `Backend/Repository/LocomotiveRepository.cs`
- [ ] Create `Backend/Repository/JourneyRepository.cs`
- [ ] Create `Backend/Repository/WorkflowRepository.cs`
- [ ] Add validation in repositories
- [ ] Add audit logging in repositories
- [ ] Register repositories in DI
- [ ] Update services to use repositories
- [ ] Write repository tests

**Benefits:**
- Abstract data source (JSON → Database later)
- Add caching/auditing easily
- Type-safe data access

---

#### 2.4: Event-Driven Architecture
- [ ] Create `Backend/Events/IDomainEvent.cs` interface
- [ ] Create `Backend/Events/TrainDetectedAtStationEvent.cs`
- [ ] Create `Backend/Events/WorkflowExecutionStartedEvent.cs`
- [ ] Create `Backend/Events/IEventPublisher.cs` interface
- [ ] Create `Backend/Events/IEventHandler<T>.cs` interface
- [ ] Create event publisher implementation
- [ ] Create traffic counting event handler
- [ ] Create workflow execution event handler
- [ ] Create UI update event handler
- [ ] Register event handlers in DI
- [ ] Write event handler tests

**Benefit:** Loose coupling - Services don't know about each other, only events

---

### Phase 3: Long Term (3-4 Weeks)

**Goal:** Bounded Contexts, Event Sourcing, Full DDD

#### 3.1: Bounded Contexts
- [ ] Create `Domain/Journeys/` (Journey BoundedContext)
- [ ] Create `Domain/Workflows/` (Workflow BoundedContext)
- [ ] Create `Domain/Trains/` (Train BoundedContext)
- [ ] Create `Domain/SignalBox/` (Track BoundedContext)
- [ ] Move domain models to respective contexts
- [ ] Create context-specific aggregates
- [ ] Define aggregate roots
- [ ] Create anti-corruption layers (adapters) between contexts
- [ ] Write context integration tests

**Benefit:** Each context can evolve independently

---

#### 3.2: Event Sourcing (Optional - Future)
- [ ] Design event store structure
- [ ] Create `Backend/EventSourcing/IEventStore.cs`
- [ ] Implement event serialization
- [ ] Create projection handlers
- [ ] Migrate existing state to event sourcing
- [ ] Add event replay capability

---

#### 3.3: Complete DDD (Domain-Driven Design)
- [ ] Create value objects (GridPosition, GridConfig, etc.) ✅ Already started
- [ ] Define invariants and business rules
- [ ] Create domain services
- [ ] Create application services
- [ ] Create specification pattern for queries
- [ ] Add rich domain events
- [ ] Write comprehensive domain tests

---

#### 3.4: Performance & Monitoring
- [ ] Add performance metrics
- [ ] Add distributed tracing
- [ ] Create health check endpoints
- [ ] Add caching layer (IMemoryCache)
- [ ] Optimize database queries (if applicable)
- [ ] Write performance benchmarks

---

## ⚠️ PRE-GITHUB FEATURE COMPLETION (CRITICAL)

**Status:** 🚧 In Progress - Must complete before GitHub launch

### Core Features (Must-Have for v0.1.0)

#### 1. TrackPlanPage (Track Plan Editor)
- [ ] **Basic track placement** (drag & drop from toolbox)
- [ ] **Snap-to-connect functionality**
- [ ] **Grid alignment and rotation**
- [ ] **Piko A-Gleis track library** (basic templates & complete article codes from catalog)
- [ ] **Track plan editing UI** (move, rotate, delete elements via mouse/touch)
- [ ] **Route definition** (define routes between signals)
- [ ] **Validation constraints** (detect invalid track connections)
- [ ] **Copy/Paste track elements**
- [ ] **Track plan export** (PNG/SVG for documentation)

#### 2. SignalBoxPage (Signal Box Configuration)
- [ ] **Feedback point assignment UI** (assign InPort to detector elements)
  - Can be offered on SignalBoxPage as alternative to TrackPlanPage
  - Link detectors to journey stations

**Priority:** 🔥 HIGH - Core feature for GitHub showcase

**Note:** 
- **SignalBoxPlan** (TrackPlan) wird bereits vollständig in `example-solution.json` gespeichert/geladen
- JSON-Struktur: `Project.signalBoxPlan.elements[]` enthält alle Track-, Signal- und Detector-Elemente
- Serialisierung erfolgt via `System.Text.Json` mit Polymorphie-Support (`$type` discriminator)
- Siehe `WinUI/example-solution.json` für aktuelle Struktur

---

## 📊 PROGRESS TRACKING

| Phase | Status | Target | Actual |
|-------|--------|--------|--------|
| Quality Audits | ✅ Complete | - | Done |
| Phase 1 (Immediate Wins) | 🚧 Not Started | 1 week | - |
| Phase 2 (Medium Priority) | ⏳ Pending | 1-2 weeks | - |
| Phase 3 (Long Term) | ⏳ Pending | 3-4 weeks | - |
| Core Features | 🚧 In Progress | 2-3 weeks | - |

---

## 🔗 RELATED DOCUMENTATION

All detailed documentation is consolidated in this file:
- **UiDispatcher Best Practices** (section above)
- **Async/Void Audit Results** (section above)
- **Quality Analysis Framework** (section above)
- **Architecture Deep Dive** (section above)
- **Refactoring Roadmap** (section above)

Original files (for reference only, consolidated here):
- `.github/instructions/uidispatcher-best-practices.md`
- `.github/instructions/async-void-and-fire-and-forget-audit.md`
- `.github/instructions/solution-analysis-and-quality-framework.md`
- `.github/instructions/architecture-deep-dive-and-refactoring.md`
- `.github/copilot-instructions.md` - Team standards and patterns
