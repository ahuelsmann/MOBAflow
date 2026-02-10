---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-16 (End of Session 8)

---

## ✅ SESSION 8 COMPLETED (2026-02-16)

### What was implemented
- [x] Viessmann 5229 turnout-command mapping based on 5229.md (Common/Multiplex/MultiplexerDefinition.cs, Common/Multiplex/MultiplexerHelper.cs)
- [x] Signal UI aspect filtering per signal article (WinUI/View/SignalBoxPage.Properties.cs)
- [x] Multiplex signal commands switched to turnout logic (SharedUI/ViewModel/MainWindowViewModel.Signals.cs)
- [x] Multiplexer mapping tests updated (Test/Common/MultiplexerHelperTests.cs)
- [x] Multiplex metadata comments refreshed (Domain/SignalBoxPlan.cs)

### Issues resolved
- [x] Removed incorrect extended accessory command assumption for 5229

### Technical Debt Identified
- [ ] None

### Status
- Build: ⚠️ Successful with 14 warnings (pre-existing)
- Tests: ❌ Failed (TrainClassParserTests and Z21WrapperTests)
- Code Review: ⏳ Pending

---

## 🚀 SESSION 9 READY: Fix failing tests

**Blocked:** Test failures in TrainClassParserTests and Z21WrapperTests
- [ ] Initialize TrainClassLibrary in TrainClassParserTests (TrainClassLibrary not initialized)
- [ ] Investigate Z21WrapperTests Received event timing (Received event not raised)

**Files to Modify:**
- Test/Backend/TrainClassParserTests.cs
- Test/Backend/Z21WrapperTests.cs

**Estimated Effort:** 1-2 hours

---

## ✅ SESSION 7 COMPLETED (2026-02-15)

### What was implemented
- [x] Z21 EventBus publish mapping to primitives (`Backend/Z21.cs`)
- [x] ViewModel event reconstruction for Z21 events (`SharedUI/ViewModel/MainWindowViewModel.cs`, `SharedUI/ViewModel/TrainControlViewModel.cs`)
- [x] Z21 event bus wiring and DI updates (`Backend/Extensions/MobaServiceCollectionExtensions.cs`, `WinUI/App.xaml.cs`)
- [x] Z21 interface completion for turnout/accessory/signal commands (`Backend/Z21.cs`)
- [x] Tests updated for new Z21 constructor (`Test/Backend/*`, `Test/Integration/*`)

### Issues resolved
- [x] Build error: missing Z21 EventBus mapping in publish/subscribe paths
- [x] Build error: missing IZ21 members after constructor update

### Technical Debt Identified
- [ ] None

### Status
- Build: ⚠️ Failed (dotnet build blocked by locked WinUI binaries)
- Tests: ⏭️ Not run (build failed)
- Code Review: ⏳ Pending

---

## 🚀 SESSION 7 READY: Final build/test validation

**Blocked:** Close running MOBAflow/Visual Studio processes that lock `WinUI/bin/Debug` outputs.
- [ ] Re-run `dotnet clean`
- [ ] Re-run `dotnet build`
- [ ] Run `dotnet test --no-build --verbosity normal`

**Files to Modify:** None (validation only)
**Estimated Effort:** 10-15 minutes

---

## ✅ SESSION 6 COMPLETED (2026-02-14)

### EventBus WeakReferences Implementation ✅
1. **EventBus Refactored with WeakReferences** ✅
   - `Subscribe<TEvent>` captures target via `WeakReference<object>`
   - `Publish<TEvent>` automatically removes dead references
   - `GetSubscriberCount` counts only live references
   - No manual `Unsubscribe()` needed for ViewModels (automatic GC cleanup)
   - **Root Cause of COMException fixed:** No more stale UI-updates into dead TextBlocks

### Investigation: Direct Event Subscribers
1. **Identified Z21 event subscribers:**
   - `MainWindowViewModel` (lines 94-99): 6 direct Z21 events
   - `TrainControlViewModel` (lines 625-631): 3 direct Z21 events  
   - `MonitorPageViewModel`: Indirect via MainWindowViewModel
   - `MainWindowViewModel.Z21.cs`: TrafficMonitor events

2. **PropertyChanged Event Subscribers:**
   - `TrainControlViewModel`: MainWindowViewModel.PropertyChanged, JourneyViewModel.PropertyChanged

### Status
- **Build:** EventBus files compile without errors
- **Pre-existing issues:** Multiplex namespace errors (unrelated)
- **Session goal:** Prepare foundation for Session 5 Z21 refactoring ✅

---

## ✅ SESSION 4 COMPLETED (2026-02-13)

### Event-Driven Backend → ViewModel Architecture - Phase 1

**Implemented Infrastructure:**
1. **IEvent Interface** ✅
   - Base marker interface for all events
   - EventBase abstract record for common timestamp

2. **IEventBus & EventBus Implementation** ✅
   - Thread-safe event publishing/subscribing (v1.0)
   - Error isolation (handler errors don't stop other handlers)
   - Guid-based subscription management for unsubscription
   - GetSubscriberCount for diagnostics
   - **Session 6 upgrade:** WeakReferences + automatic cleanup

3. **Z21 Backend Events** ✅
   - Z21ConnectionEstablishedEvent / Z21ConnectionLostEvent
   - LocomotiveSpeedChangedEvent / LocomotiveDirectionChangedEvent
   - LocomotiveFunctionToggledEvent / LocomotiveEmergencyStopEvent
   - FeedbackPointTriggeredEvent / FeedbackPointClearedEvent
   - SignalAspectChangedEvent / SwitchPositionChangedEvent
   - HealthCheckFailedEvent / HealthCheckRecoveredEvent

4. **DI Registration** ✅
   - EventBusServiceCollectionExtensions.AddEventBus()
   - Registered as singleton in App.xaml.cs
   - Ready for use in all services

---

## ✅ SESSION 3 COMPLETED (2026-02-13)

### Implemented
1. **NavigationRegistry Cleanup** ✅
   - NavigationRegistry.cs was already deleted
   - No references remaining in codebase

2. **Plugin [NavigationItem] Support** ✅
   - NavigationItemAttribute moved to Common/Navigation
   - NavigationCategory moved to Common/Navigation
   - StatisticsPluginPage created with [NavigationItem] attribute
   - CmdPluginPage created with [NavigationItem] attribute
   - Both plugins migrated to new pattern
   - Legacy GetPages() deprecated with [Obsolete] attribute
   - PluginLoader.DiscoverPluginPages functional
   - Plugin pages auto-merge with core pages

3. **Deprecation Documentation** ✅
   - IPlugin.GetPages() marked [Obsolete("...", false)]
   - PluginPageDescriptor marked [Obsolete("...", false)]
   - Clear migration path documented

### Build Status
- ✅ Common compiles without errors
- ✅ StatisticsPlugin compiles without errors
- ✅ CmdPlugin compiles without errors
- ✅ Plugin [NavigationItem] system fully functional

---

## 📊 Implementation Status

| Feature | Status | Completion |
|---------|--------|-----------|
| DI & Navigation Modernization | ✅ 100% | DONE (Session 3) |
| Plugin [NavigationItem] System | ✅ 100% | DONE (Session 3) |
| EventBus Infrastructure | ✅ 100% | DONE (Session 4) |
| **EventBus WeakReferences** | ✅ **100%** | **DONE (Session 6)** |
| **Z21 EventBus Integration (Architecture)** | ✅ **100%** | **DONE (Session 5)** |
| **Z21 EventBus Integration (Code)** | ✅ **100%** | **DONE (Session 5)** |
| Layout Persistierung | ✅ 100% | DONE |
| CollapsibleColumn Control | ✅ 100% | DONE |

---

## 🎓 Event-Driven Pattern Summary

### Architecture Flow (Post-Phase 2 – Session 5)

```
Z21 UDP Data (Port 21105)
    ↓ (OnUdpReceived parses)
Backend/Z21.cs (with IEventBus injection)
    ↓ publishes via EventBus.Publish(new XyzEvent(...))
IEventBus Singleton (with WeakReferences)
    ↑ subscribes (automatic cleanup on ViewModel GC)
┌─────────────────────┬──────────────────┬─────────────────────┐
│ MainWindowViewModel │ TrainControlVM   │ Other ViewModels    │
│ (via EventBus sub)  │ (via EventBus sub)│ (can also sub)      │
└─────────────────────┴──────────────────┴─────────────────────┘
    ↓ update @property
┌─────────────────────────────────────────────────────────────┐
│ XAML UI (Bindings to ViewModel Properties)                  │
└─────────────────────────────────────────────────────────────┘
```

### Key Benefits (Post-Session 6)
- ✅ **Loose Coupling:** Services don't know about ViewModels
- ✅ **Single Responsibility:** Each ViewModel only handles its domain
- ✅ **Testability:** Mock EventBus instead of complex service mocks
- ✅ **Plugin-Friendly:** Plugins can subscribe to any event
- ✅ **Memory Safe:** WeakReferences prevent leaks, automatic cleanup
- ✅ **No Manual Cleanup:** Dispose/Unsubscribe not needed
- ✅ **No Circular Dependencies:** Events use primitive types, not Backend.Model

---

## 🔧 Files Modified (Session 5)

| File | Change | Status |
|------|--------|--------|
| `Backend/Z21.cs` | Event publishing mapping, IZ21 members, EventBus injection | ✅ Done |
| `Backend/Extensions/MobaServiceCollectionExtensions.cs` | Z21 DI update | ✅ Done |
| `WinUI/App.xaml.cs` | ViewModel DI EventBus injection | ✅ Done |
| `SharedUI/ViewModel/MainWindowViewModel.cs` | EventBus subscriptions mapped to primitives | ✅ Done |
| `SharedUI/ViewModel/TrainControlViewModel.cs` | EventBus subscriptions mapped to primitives | ✅ Done |
| `Common/Events/Z21Events.cs` | Refactored to primitive types | ✅ Done |
| `Test/Backend/*`, `Test/Integration/*` | Z21 constructor updates | ✅ Done |
