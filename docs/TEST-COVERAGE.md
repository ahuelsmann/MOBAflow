# MOBAflow Test Coverage Analysis

> **Generated:** 2025-01-21  
> **Scope:** Backend, Common, Domain, SharedUI, Sound

---

## 📊 Executive Summary

This document tracks unit test coverage for core MOBAflow projects. The goal is **100% coverage** for all public APIs.

### Current Status

| Project | Classes | Tested | Coverage | Priority |
|---------|---------|--------|----------|----------|
| **Domain** | ~30 | 3 | ~10% | 🔴 **HIGH** |
| **Backend** | ~15 | 8 | ~53% | 🟡 **MEDIUM** |
| **Common** | ~10 | 0 | 0% | 🔴 **HIGH** |
| **SharedUI** | ~20 | 3 | ~15% | 🟡 **MEDIUM** |
| **Sound** | ~5 | 2 | ~40% | 🟢 **LOW** |

---

## 🎯 Domain Project (Priority: HIGH)

### ✅ Tested Classes
- `Solution` (SolutionTests.cs, SolutionInstanceTests.cs, NewSolutionTests.cs)

### ❌ Missing Tests
| Class | Public Members | Priority | Complexity |
|-------|----------------|----------|------------|
| `Journey` | 10 properties | HIGH | Low |
| `Station` | 5 properties | HIGH | Low |
| `Workflow` | 8 properties | HIGH | Medium |
| `WorkflowAction` | 6 properties + logic | HIGH | Medium |
| `Train` | 9 properties | MEDIUM | Low |
| `Locomotive` | 12 properties | MEDIUM | Low |
| `Wagon` | 8 properties | LOW | Low |
| `PassengerWagon` | 3 properties | LOW | Low |
| `GoodsWagon` | 2 properties | LOW | Low |
| `Project` | 10 properties | HIGH | Low |
| `City` | 4 properties | LOW | Low |
| `Voice` | 3 properties | LOW | Low |
| `SpeakerEngineConfiguration` | 4 properties | MEDIUM | Low |
| `SignalBoxPlan` | TBD | LOW | Low |
| `Details` | TBD | LOW | Low |
| `LocomotiveSeries` | TBD | LOW | Low |
| `LocomotiveCategory` | TBD | LOW | Low |
| `ConnectingService` | TBD | LOW | Low |

### Domain Enums (Validation Tests Needed)
- `ActionType` - ✅ Test enum values
- `BehaviorOnLastStop` - ✅ Test enum values
- `TrainType` - ✅ Test enum values
- `ServiceType` - ✅ Test enum values
- `DigitalSystem` - ✅ Test enum values
- `Epoch` - ✅ Test enum values
- `PowerSystem` - ✅ Test enum values
- `CargoType` - ✅ Test enum values
- `PassengerClass` - ✅ Test enum values
- `WorkflowExecutionMode` - ✅ Test enum values
- `ColorScheme` - ✅ Test enum values

---

## 🎯 Backend Project (Priority: MEDIUM)

### ✅ Tested Classes
- `Z21` (Z21UnitTests.cs, Z21WrapperTests.cs, Z21IntegrationTests.cs)
- `ActionExecutor` (ActionExecutorTests.cs)
- `WorkflowService` (WorkflowServiceTests.cs)
- `AnnouncementService` (AnnouncementServiceTests.cs)
- `DataManager` (DataManagerTests.cs)
- `Z21MessageParser` (Z21MessageParserTests.cs)
- `DccCommandDecoder` (Z21DccCommandDecoderTests.cs)
- `Result<T>` (ResultTests.cs)

### ❌ Missing Tests
| Class | Public Members | Priority | Complexity |
|-------|----------------|----------|------------|
| `Z21Wrapper` | 8 methods | MEDIUM | Medium |
| `UdpClientWrapper` | 4 methods | HIGH | Medium |
| `IoService` | 5 methods | HIGH | High |
| `SettingsService` | 10 properties | MEDIUM | Low |
| `CityService` | 4 methods | LOW | Low |
| `AudioPlayer` | 3 methods | MEDIUM | Medium |

---

## 🎯 Common Project (Priority: HIGH)

### ✅ Tested Classes
- **NONE** ❌

### ❌ Missing Tests (All Classes Need Tests!)
| Class | Public Members | Priority | Complexity |
|-------|----------------|----------|------------|
| `PluginBase` | 6 virtual methods | HIGH | Medium |
| `PluginLoader` | 5 methods | HIGH | High |
| `PluginDiscoveryService` | 3 methods | HIGH | Medium |
| `PluginValidator` | 4 methods | HIGH | Medium |
| `IPlugin` interface | 6 members | HIGH | N/A |
| `PluginMetadata` record | 6 properties | MEDIUM | Low |
| `PluginPageDescriptor` record | 4 properties | MEDIUM | Low |
| Configuration classes | TBD | MEDIUM | Low |
| Logger utilities | TBD | LOW | Low |

---

## 🎯 SharedUI Project (Priority: MEDIUM)

### ✅ Tested Classes
- `MainWindowViewModel` (WinuiJourneyViewModelTests.cs, PageViewModelTests.cs)
- `MauiAdapter` (MauiAdapterDispatchTests.cs)

### ❌ Missing Tests
| Class | Public Members | Priority | Complexity |
|-------|----------------|----------|------------|
| `JourneyViewModel` | 15 commands + properties | HIGH | High |
| `WorkflowViewModel` | 12 commands + properties | HIGH | High |
| `TrainViewModel` | 10 commands + properties | MEDIUM | Medium |
| `StationViewModel` | 8 commands + properties | MEDIUM | Medium |
| `WorkflowActionViewModel` | 10 commands + properties | HIGH | High |
| `TrainControlViewModel` | 12 commands + properties | HIGH | Medium |
| `SignalBoxPlanViewModel` | TBD | MEDIUM | Medium |
| `OverviewViewModel` | TBD | LOW | Low |
| `SettingsViewModel` | TBD | MEDIUM | Low |
| Base ViewModels | TBD | HIGH | Medium |

---

## 🎯 Sound Project (Priority: LOW)

### ✅ Tested Classes
- `CognitiveSpeechEngine` (CognitiveSpeechEngineTest.cs)
- `SystemSpeechEngine` (SystemSpeechEngineTest.cs)

### ❌ Missing Tests
| Class | Public Members | Priority | Complexity |
|-------|----------------|----------|------------|
| `SpeakerEngineFactory` | 1 factory method | MEDIUM | Low |
| `AudioFilePlayer` | 3 methods | MEDIUM | Medium |
| Audio resource helpers | TBD | LOW | Low |

---

## 📋 Test Strategy

### Priorities

1. **Domain Tests** (Week 1)
   - Journey, Station, Workflow, WorkflowAction
   - Project, Train
   - Enum validations

2. **Common Tests** (Week 2)
   - Plugin infrastructure (critical for app stability)
   - Configuration
   - Utilities

3. **Backend Tests** (Week 3)
   - Complete Z21 coverage
   - IoService
   - SettingsService

4. **SharedUI Tests** (Week 4)
   - ViewModels with commands
   - Property change notifications
   - Command validations

5. **Sound Tests** (Week 5)
   - Complete speaker engine coverage
   - Audio playback edge cases

### Test Patterns

#### Domain Entity Tests
```csharp
[Test]
public void Constructor_InitializesWithDefaults()
{
    var journey = new Journey();
    
    Assert.That(journey.Id, Is.Not.EqualTo(Guid.Empty));
    Assert.That(journey.Name, Is.EqualTo("New Journey"));
    Assert.That(journey.Stations, Is.Empty);
}

[Test]
public void Property_CanSetAndGet()
{
    var journey = new Journey { Name = "Test Journey" };
    Assert.That(journey.Name, Is.EqualTo("Test Journey"));
}
```

#### ViewModel Tests
```csharp
[Test]
public void Command_WhenExecuted_UpdatesProperty()
{
    var viewModel = new JourneyViewModel();
    var raised = false;
    viewModel.PropertyChanged += (s, e) => {
        if (e.PropertyName == nameof(JourneyViewModel.Name))
            raised = true;
    };
    
    viewModel.Name = "New Name";
    
    Assert.That(raised, Is.True);
}
```

#### Service Tests (with Mocks)
```csharp
[Test]
public async Task LoadAsync_WhenFileExists_LoadsSolution()
{
    var mockIo = new Mock<IIoService>();
    mockIo.Setup(x => x.LoadAsync()).ReturnsAsync((solution, path, null));
    
    var service = new DataManager(mockIo.Object);
    var result = await service.LoadAsync();
    
    Assert.That(result.IsSuccess, Is.True);
}
```

---

## 🛠️ Tools & Infrastructure

### Test Frameworks
- **NUnit** 4.4.0
- **Moq** 4.20.72 (for mocking)
- **coverlet.collector** 6.0.4 (for coverage)

### Running Tests
```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet-coverage collect -f cobertura -o coverage.xml dotnet test

# Run specific project
dotnet test Test/Test.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~JourneyTests"
```

### Coverage Reports
```powershell
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html

# Open report
start coverage-report/index.html
```

---

## 📈 Coverage Goals

| Milestone | Target | Deadline |
|-----------|--------|----------|
| **Domain 100%** | All entities + enums | Week 1 |
| **Common 100%** | Plugin infrastructure | Week 2 |
| **Backend 80%** | Core services | Week 3 |
| **SharedUI 70%** | Key ViewModels | Week 4 |
| **Sound 100%** | All engines | Week 5 |
| **Overall 85%** | Full solution | Week 6 |

---

## 🚀 Next Steps

1. **Create test files** for missing Domain classes
2. **Implement Common project tests** (zero coverage currently!)
3. **Complete Backend test gaps** (IoService, SettingsService)
4. **Add ViewModel tests** for critical workflows
5. **Set up CI/CD pipeline** for automated coverage tracking

---

## 📚 Resources

- **NUnit Docs:** https://docs.nunit.org/
- **Moq Quickstart:** https://github.com/moq/moq4/wiki/Quickstart
- **Coverage Tools:** https://github.com/coverlet-coverage/coverlet
- **MOBAflow Architecture:** [ARCHITECTURE.md](../ARCHITECTURE.md)

---

**Last Updated:** 2025-01-21  
**Next Review:** After Domain tests complete
