---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-11 (Post-startup indicator visibility tuned)

---

## ⚡ PERFORMANCE & STARTUP OPTIMIZATION

### Background
**Performance Trace Analysis (2026-02-10):**
- `Application.Start(ApplicationInitializationCallback)` is the primary bottleneck in startup
- The slow path originates from heavy initialization in `App()` constructor and early ViewModel setup
- Current startup time: Needs baseline measurement
- Target: Reduce startup time by 500ms-1s through lazy-loading and deferred initialization

---

## ✅ PHASE 1: APP-STARTUP OPTIMIZATION - COMPLETED

### Completed Implementations

#### ✅ 1.1: PostStartupInitializationService Created
- **File:** `WinUI/Service/PostStartupInitializationService.cs` (NEW)
- **Status:** ✅ DONE
- **What it does:**
  - Defers non-critical initialization until after MainWindow is visible
  - Runs HealthCheckService asynchronously
  - Starts WebApp in background if enabled
  - 30-second safety timeout
  - Comprehensive error logging

#### ✅ 1.2: WinUI/App.xaml.cs Optimized
- **File:** `WinUI/App.xaml.cs` (MODIFIED)
- **Status:** ✅ DONE
- **Changes:**
  - Removed HealthCheckService from synchronous startup
  - Added `InitializePostStartupServicesAsync()` method
  - Triggered from `OnLaunched()` after MainWindow activation
  - Services now registered as deferred
  - MainWindow health checks are wired in post-startup initialization
  - Status bar shows post-startup progress with minimum display time and log entries in Monitor

#### ✅ 1.3: MAUI/MauiProgram.cs Optimized
- **File:** `MAUI/MauiProgram.cs` (MODIFIED)
- **Status:** ✅ DONE
- **Changes:**
  - RestApiDiscoveryService marked as lazy-loaded
  - Prevents network I/O during startup
  - Performance comments added

#### ✅ 1.4: Documentation Created
- **Files Created:**
  - `docs/STARTUP-OPTIMIZATION-IMPLEMENTATION.md` - Deployment guide
  - `docs/STARTUP-OPTIMIZATION.md` - Measurement baseline guide
  - `docs/Z21PROTOCOL-NAMING-FIX.md` - Fix guide for build errors
  - `.github/instructions/todos.instructions.md` - This file (updated)

---

## ✅ RESOLVED: Z21Protocol Naming Inconsistency

### Issue
Build failures caused by naming mismatch in Z21Protocol constants and a few related refactors.

### Resolution
- `Backend/Protocol/Z21Command.cs` updated to use `Z21Protocol.*` `UPPER_SNAKE_CASE` constants
- `Backend/Protocol/Z21MessageParser.cs` updated to use `UPPER_SNAKE_CASE` constants
- `WinUI/Service/PostStartupInitializationService.cs` fixed DI using and aligned to current APIs
- Test fixes applied for renamed members (WR -> Wr, Z21Packets member names, Sound namespace)

Reference: `docs/Z21PROTOCOL-NAMING-FIX.md`

---

### Phase 1: App-Startup entschlacken (Immediate - 3-4 hours) ✅ DONE
**Goal:** Reduce synchronous initialization in App constructor ✅ ACHIEVED

#### 1.1: Analyze WinUI/MAUI App Constructors ✅
- [x] `WinUI/App.xaml.cs` → Analyzed and optimized
- [x] `MAUI/MauiProgram.cs` → Analyzed and optimized
- [x] Resource loading analyzed
- [x] Identified heavy singleton registrations

#### 1.2: Extract Non-Essential Initialization from App Constructor ✅
- [x] Moved HealthCheckService to deferred initialization
- [x] Moved SpeechHealthCheck to deferred phase
- [x] Moved WebApp startup to deferred async
- [x] Kept only: DI container, window creation, basic UI init

#### 1.3: Convert Heavy Singletons to Lazy-Initialized Services ✅
- [x] RestApiDiscoveryService → Lazy-loaded
- [x] HealthCheckService → Deferred async
- [x] PostStartupInitializationService → Created for deferred work

---

### Phase 2: Lazy-Loading Implementation (1-2 hours) ⏳ PENDING
**Goal:** Delay expensive initialization until needed

#### 2.1: TrackLibrary.PikoA Lazy-Loading
- [ ] Check `TrackLibrary.PikoA/WR.cs` usage in TrackPlanSvgRenderer
- [ ] If loaded at startup: Create lazy provider
- [ ] Load only when TrackPlanPage first accessed
- [ ] Register as `Lazy<TrackLibraryProvider>`

#### 2.2: Z21 Connection Lazy-Initialization
- [ ] Check `MAUI/Platforms/Android/Services/Z21BackgroundService.cs`
- [ ] Defer connection until user navigates to train control
- [ ] Keep service registration, but delay actual connection
- [ ] Pattern: `IsInitialized` flag, lazy first-use

#### 2.3: REST API Discovery Lazy-Initialization
- [ ] `MAUI/Service/RestApiDiscoveryService.cs` - Check if runs at startup
- [ ] Move to background task after UI loads
- [ ] Use `.ContinueWith()` with error handling
- [ ] Implement 5s timeout to not block app

---

### Phase 3: Profiling & Validation (30-45 min) ⏳ PENDING
**Goal:** Measure actual improvement in startup time

#### 3.1: Baseline Measurement (BEFORE OPTIMIZATION)
- [ ] Run app and measure with VS Profiler
- [ ] Record: Time to UI visible, time to interactive, total startup
- [ ] Document in `StartupOptimization-Baseline-2026-02-10.md`

#### 3.2: Post-Optimization Measurement (AFTER FIXES)
- [ ] Build after Z21Protocol naming fix
- [ ] Run app and measure same metrics
- [ ] Compare: Target = 500ms-1s reduction
- [ ] Document results

#### 3.3: Functional Validation
- [ ] Verify no runtime errors during startup
- [ ] Check Z21 connection works (delayed but functional)
- [ ] Ensure all pages load on first access
- [ ] Test on both WinUI and MAUI platforms

---

### Phase 4: Optimization Checklist ⏳ PENDING

**Before Deployment:**
- [ ] Baseline measurements documented
- [ ] Phase 1 changes: App constructor streamlined ✅ DONE
- [ ] Phase 2 changes: Lazy-loading implemented
- [ ] Phase 3 validation: Profiling complete
- [ ] No functional regressions
- [ ] Code review: Async patterns follow guidelines
- [ ] All services properly disposed in cleanup

**Files Affected:**
- ✅ `WinUI/App.xaml.cs` (modified)
- ✅ `WinUI/App.xaml` (not modified)
- ✅ `MAUI/MauiProgram.cs` (modified)
- ✅ `WinUI/Service/PostStartupInitializationService.cs` (NEW)
- ⏳ `MAUI/Platforms/Android/Services/Z21BackgroundService.cs` (future)
- ⏳ `MAUI/Service/RestApiDiscoveryService.cs` (future)
- ⏳ `TrackLibrary.PikoA/WR.cs` (future)

---

## 📋 QUALITY IMPROVEMENTS & AUDITS COMPLETED

### UiDispatcher Best Practices ✅
- ✅ **DI Pattern:** Always use `AddUiDispatcher()` extension method
- ✅ **InvokeOnUi()** vs **EnqueueOnUi():** Rules documented
- ✅ **Fire-and-Forget:** Must use `.ContinueWith()` for error handling
- ✅ **ConfigureAwait:** Use `false` in library code, NOT in UI code
- ✅ **CancellationToken:** Always accept and propagate

### Async/Void & Fire-and-Forget Audit ✅
**Status:** 🟢 GOOD - All issues fixed
- ✅ 50+ files audited
- ✅ 15+ event handlers checked
- ✅ 1 async void violation FIXED
- ✅ 1 fire-and-forget issue FIXED

### Solution Quality Analysis Framework ✅
**8 Analysis Dimensions:** Documented and tracked

---

## 🏗️ ARCHITECTURE ISSUES & REFACTORING ROADMAP

### Critical Architecture Issues Identified

#### Issue #1: MainWindowViewModel - God Object ⚠️
**Current State:**
- 9 partial files, ~800 LOC
- Too many responsibilities mixed

**Solution:** Extract into 3 dedicated services (Phase 2-4)

#### Issue #2: Domain Model - Missing Aggregates ⚠️
**Current State:**
- Collections directly exposed
- No encapsulation, no validation

**Solution:** ✅ Started - GridConfig, GridPosition created

#### Issue #3: Backend Service Coupling ⚠️
**Current State:**
- WorkflowService depends on multiple services

**Solution (Phase 2):** Event-driven or Command Pattern

#### Issue #4: Frontend-Backend Coupling ⚠️
**Current State:**
- View directly depends on Backend Services

**Solution (Phase 1):** ✅ PostStartupInitializationService created

#### Issue #5: Z21 Integration - Not Isolated ⚠️
**Current State:**
- Z21 event handling scattered in MainWindowViewModel
- Auto-connect logic mixed with ViewModel state

**Solution (Phase 1):** ✅ Deferred initialization through PostStartupInitializationService

---

## 📊 PROGRESS TRACKING

| Phase | Status | Completion | Target |
|-------|--------|-----------|--------|
| Quality Audits | ✅ Complete | 100% | - |
| Phase 1 (App Startup) | ✅ Complete | 100% | 1-2 days |
| Phase 1 Blocker (Protocol Naming) | ✅ Resolved | 100% | 1-2 hours |
| Phase 2 (Lazy-Loading) | ⏳ Pending | 0% | 1-2 weeks |
| Phase 3 (Profiling) | ⏳ Pending | 0% | 30-45 min |
| Phase 4 (Checklist) | ⏳ Pending | 0% | On-demand |
| Core Features | 🚧 In Progress | 40% | 2-3 weeks |

---

## 🚀 NEXT STEPS (IMMEDIATE)

1. **Build & Verify** (30 min)
   - `dotnet clean && dotnet build -c Release`
   - Should compile without errors

3. **Test Startup** (30 min)
   - Run app in VS Profiler
   - Measure baseline times (BEFORE optimization)
   - Record in measurements file

4. **Measure Improvement** (30 min)
   - Compare with Phase 1 optimization
   - Target: 500ms-1s reduction in startup

5. **Continue Phase 2** (After baseline)
   - TrackLibrary lazy-loading
   - Z21 connection deferral
   - REST API discovery async

---

## 📚 DOCUMENTATION FILES

**New Documentation Created:**
- ✅ `docs/STARTUP-OPTIMIZATION-IMPLEMENTATION.md` - Full implementation guide
- ✅ `docs/STARTUP-OPTIMIZATION.md` - Measurement baseline guide
- ✅ `docs/Z21PROTOCOL-NAMING-FIX.md` - Fix guide with PowerShell script
- ✅ `.github/instructions/todos.instructions.md` - This file (updated 2026-02-10)

**Related Instructions:**
- `.github/copilot-instructions.md` - Team standards and patterns
- `.github/instructions/z21-backend.instructions.md` - Z21-specific rules
- `.github/instructions/backend.instructions.md` - Backend platform independence
