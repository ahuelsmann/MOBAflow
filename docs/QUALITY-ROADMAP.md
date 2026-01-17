# MOBAflow Quality Improvement Roadmap

> **Created:** 2025-01-21  
> **Goal:** 100% XML Documentation + 85% Test Coverage

---

## 📋 Overview

This roadmap coordinates **documentation** and **test coverage** improvements across all MOBAflow projects.

### Current State

| Metric | Domain | Backend | Common | SharedUI | Sound |
|--------|--------|---------|--------|----------|-------|
| **Documentation** | 83% | 67% | 30%  | 60% | 80% |
| **Test Coverage** | 10% | 53% | 0% 🔴 | 15% | 40% |

### Targets

| Metric | Domain | Backend | Common | SharedUI | Sound |
|--------|--------|---------|--------|----------|-------|
| **Documentation** | 100% | 90% | 100% | 80% | 100% |
| **Test Coverage** | 100% | 80% | 100% | 70% | 100% |

---

## 🎯 6-Week Sprint Plan

### Week 1: Foundation (Common Project)

**Priority:** 🔴 **CRITICAL** - Common has 0% test coverage and 30% documentation!

#### Documentation
- [ ] `PluginLoader.cs` - Complete XML docs
- [ ] `PluginDiscoveryService.cs` - Complete XML docs
- [ ] `PluginValidator.cs` - Complete XML docs
- [ ] `PluginMetadata.cs` - Complete XML docs
- [ ] `PluginPageDescriptor.cs` - Complete XML docs
- [ ] Configuration classes - Add XML docs

#### Tests
- [ ] `PluginLoaderTests.cs` - Discovery, validation, lifecycle
- [ ] `PluginDiscoveryServiceTests.cs` - DLL scanning, reflection
- [ ] `PluginValidatorTests.cs` - Name checks, tag validation, reserved words
- [ ] `PluginBaseTests.cs` - Default implementations, virtual method behavior
- [ ] `PluginMetadataTests.cs` - Record equality, serialization
- [ ] `PluginPageDescriptorTests.cs` - Record equality, validation

**Deliverable:** Common project at 100% documentation + 100% test coverage

---

### Week 2: Domain Entities

#### Documentation
- [ ] All 11 Domain enums with member documentation
- [ ] `WorkflowAction.cs` - Add `<param>` tags
- [ ] `Project.cs` - Complete method documentation
- [ ] `City.cs`, `Voice.cs`, `Details.cs` - Add full documentation
- [ ] Add `<value>` tags to all properties

#### Tests
- [ ] `JourneyTests.cs` - Constructor, properties, validation
- [ ] `StationTests.cs` - Constructor, properties
- [ ] `WorkflowTests.cs` - Constructor, properties, execution mode
- [ ] `WorkflowActionTests.cs` - Constructor, properties, action types
- [ ] `TrainTests.cs` - Constructor, properties, double traction
- [ ] `ProjectTests.cs` - Constructor, collections, methods
- [ ] Enum validation tests for all 11 enums

**Deliverable:** Domain at 100% documentation + 100% test coverage

---

### Week 3: Backend Services

#### Documentation
- [ ] `IoService.cs` - Platform-specific notes
- [ ] `UdpClientWrapper.cs` - Complete documentation
- [ ] `ActionExecutor.cs` - Add exception documentation
- [ ] `SettingsService.cs` - Property descriptions
- [ ] `CityService.cs`, `AudioPlayer.cs` - Add documentation

#### Tests
- [ ] `IoServiceTests.cs` - Load, save, browse (with mocks)
- [ ] `UdpClientWrapperTests.cs` - Send, receive, error handling
- [ ] `SettingsServiceTests.cs` - Get/set properties, persistence
- [ ] `CityServiceTests.cs` - CRUD operations
- [ ] `AudioPlayerTests.cs` - Play, stop, error cases
- [ ] Complete Z21 edge case tests

**Deliverable:** Backend at 90% documentation + 80% test coverage

---

### Week 4: SharedUI ViewModels

#### Documentation
- [ ] All `[RelayCommand]` methods with XML docs
- [ ] `JourneyViewModel.cs` - Commands and properties
- [ ] `WorkflowViewModel.cs` - Commands and properties
- [ ] `TrainControlViewModel.cs` - Commands and properties
- [ ] Base ViewModels - Critical for inheritance

#### Tests
- [ ] `JourneyViewModelTests.cs` - Commands, property changes, validation
- [ ] `WorkflowViewModelTests.cs` - Commands, property changes
- [ ] `TrainViewModelTests.cs` - Commands, property changes
- [ ] `StationViewModelTests.cs` - Commands, property changes
- [ ] `WorkflowActionViewModelTests.cs` - Commands, validation
- [ ] `TrainControlViewModelTests.cs` - Z21 integration (with mocks)

**Deliverable:** SharedUI at 80% documentation + 70% test coverage

---

### Week 5: Sound & Remaining Gaps

#### Documentation
- [ ] `AudioFilePlayer.cs` - Complete documentation
- [ ] Resource helper classes - Add documentation
- [ ] Review all projects for missing `<value>` tags

#### Tests
- [ ] `SpeakerEngineFactoryTests.cs` - Factory pattern, engine selection
- [ ] `AudioFilePlayerTests.cs` - File playback, error handling
- [ ] Integration tests for speech engines

**Deliverable:** Sound at 100% documentation + 100% test coverage

---

### Week 6: Integration & CI/CD

#### Tasks
- [ ] Run Doxygen and fix all warnings
- [ ] Generate coverage report (dotnet-coverage)
- [ ] Set up Azure DevOps pipeline for automated testing
- [ ] Set up coverage reporting in pipeline
- [ ] Add quality gates (80% coverage minimum)
- [ ] Final documentation review

**Deliverable:** Automated quality checks in CI/CD

---

## 📊 Success Metrics

### Documentation
- ✅ All public classes have `<summary>`
- ✅ All public properties have `<value>`
- ✅ All public methods have `<param>` and `<returns>`
- ✅ All enums and enum members documented
- ✅ Doxygen generates without warnings
- ✅ 85%+ overall documentation coverage

### Testing
- ✅ All Domain entities have tests
- ✅ All Backend services have tests (80%+)
- ✅ Common project has 100% coverage
- ✅ Key ViewModels have command tests
- ✅ Sound engines have 100% coverage
- ✅ 85%+ overall test coverage

---

## 🛠️ Tools & Automation

### Documentation
```powershell
# Generate Doxygen docs
doxygen Doxyfile

# Check for warnings
doxygen Doxyfile 2>&1 | findstr /i "warning"

# Open documentation
start docs/api/html/index.html
```

### Testing
```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet-coverage collect -f cobertura -o coverage.xml dotnet test

# Generate HTML report
reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html

# Open report
start coverage-report/index.html
```

### CI/CD Pipeline (Azure DevOps)
```yaml
steps:
  - task: DotNetCoreCLI@2
    displayName: 'Run Tests with Coverage'
    inputs:
      command: 'test'
      arguments: '--collect:"XPlat Code Coverage"'
  
  - task: PublishCodeCoverageResults@1
    inputs:
      codeCoverageTool: 'Cobertura'
      summaryFileLocation: '$(Agent.TempDirectory)/**/*coverage.cobertura.xml'
  
  - task: PowerShell@2
    displayName: 'Generate Doxygen Docs'
    inputs:
      targetType: 'inline'
      script: 'doxygen Doxyfile'
```

---

## 📚 Reference Documents

- [`docs/TEST-COVERAGE.md`](TEST-COVERAGE.md) - Detailed test status
- [`docs/DOCUMENTATION-STATUS.md`](DOCUMENTATION-STATUS.md) - Documentation checklist
- [`docs/DOXYGEN.md`](DOXYGEN.md) - Doxygen setup guide
- [`ARCHITECTURE.md`](../ARCHITECTURE.md) - System architecture

---

## 👥 Team Responsibilities

| Area | Owner | Reviewer |
|------|-------|----------|
| **Common** (Week 1) | TBD | TBD |
| **Domain** (Week 2) | TBD | TBD |
| **Backend** (Week 3) | TBD | TBD |
| **SharedUI** (Week 4) | TBD | TBD |
| **Sound** (Week 5) | TBD | TBD |
| **CI/CD** (Week 6) | TBD | TBD |

---

## 🚦 Status Dashboard

| Week | Documentation | Tests | Status |
|------|--------------|-------|--------|
| Week 1 | Common | Common | 🔴 Not Started |
| Week 2 | Domain | Domain | 🔴 Not Started |
| Week 3 | Backend | Backend | 🔴 Not Started |
| Week 4 | SharedUI | SharedUI | 🔴 Not Started |
| Week 5 | Sound | Sound | 🔴 Not Started |
| Week 6 | Review | CI/CD | 🔴 Not Started |

---

**Next Update:** End of Week 1  
**Last Updated:** 2025-01-21
