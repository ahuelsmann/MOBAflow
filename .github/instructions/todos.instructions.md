---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: 2026-02-13 (End of Session 6)

---

## ✅ SESSION 6 COMPLETED (2026-02-13)

### Investigation
1. **DockPanelGroup RebuildTabs Exception** ✅
   - Confirmed `TabViewItem` creation can reparent an existing `UIElement` (`DockPanel` or header element)
   - Add fix in `WinUI/Controls/DockingManager/DockPanelGroup.xaml.cs` to avoid reusing visual elements

---

## ✅ SESSION 5 COMPLETED (2026-02-13)

### Reliability Fixes
1. **WinUI Navigation DI** ✅
   - Registered PageMetadata list for NavigationService resolution

2. **WinUI Docking Converters** ✅
   - Added PinTooltipConverter and PinForegroundConverter
   - Registered converters in App.xaml resources

3. **Statistics Rebuild Reentrancy** ✅
   - CountOfFeedbackPoints now enqueues Statistics rebuild on UI dispatcher

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

## ✅ SESSION 4 COMPLETED (2026-02-13)

### Event-Driven Backend → ViewModel Architecture - Phase 1

**Implemented Infrastructure:**
1. **IEvent Interface** ✅
   - Base marker interface for all events
   - EventBase abstract record for common timestamp

2. **IEventBus & EventBus Implementation** ✅
   - Thread-safe event publishing/subscribing
   - Error isolation (handler errors don't stop other handlers)
   - Guid-based subscription management for unsubscription
   - GetSubscriberCount for diagnostics

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

### Architecture Benefits
- ✅ Decouples Z21 backend from ViewModels
- ✅ Enables plugin subscriptions without core knowledge
- ✅ Single EventBus dependency instead of many service dependencies
- ✅ Foundation for reducing MainWindowViewModel "God Object"

---

## 🚀 NEXT SESSION: Event-Driven Phase 2 (Z21 Refactoring)

### Priority 0: Remove Plugin System (1-2 hours)
**Scope:** Remove plugin loader, discovery, and related tests since plugins are no longer needed.

- Remove plugin discovery/loading code
- Remove plugin page registration paths
- Remove plugin tests and docs references

### Priority 1: Refactor Z21.cs (3-4 hours)
**Location:** `Backend/Z21.cs` - `OnUdpReceived` method

**Tasks:**
1. Replace legacy event invocations with EventBus.Publish()
   - `OnXBusStatusChanged?.Invoke()` → `_eventBus.Publish(new XBusStatusChangedEvent(...))`
   - `OnLocoInfoChanged?.Invoke()` → `_eventBus.Publish(new LocomotiveSpeedChangedEvent(...))`
   - `OnSystemStateChanged?.Invoke()` → `_eventBus.Publish(new Z21TrackPowerChangedEvent(...))`
   - ~20+ event invocations to convert

2. Inject IEventBus into Z21.cs constructor
3. Update Z21Monitor.cs if needed
4. Test: UDP → EventBus → ViewModel flow

### Priority 2: Refactor MainWindowViewModel (2-3 hours)
**Locations:** 
- `SharedUI/ViewModel/MainWindowViewModel.Z21.cs`
- `SharedUI/ViewModel/MainWindowViewModel.cs`

**Tasks:**
1. Inject IEventBus into MainWindowViewModel
2. Replace event handlers with EventBus subscriptions
   - Old: `z21Service.OnXBusStatusChanged += OnXBusStatusChanged`
   - New: `_eventBus.Subscribe<XBusStatusChangedEvent>(OnXBusStatusChanged)`
3. Move Z21-related properties into separate ViewModel or remove if redundant
4. Reduce God Object responsibilities

### Priority 3: Refactor MonitorPageViewModel (1-2 hours)
**Location:** `SharedUI/ViewModel/MonitorPageViewModel.cs`

**Tasks:**
1. Subscribe to FeedbackPointTriggeredEvent
2. Update track occupancy visualization
3. Remove tight coupling to IZ21Service

### Priority 4: Integration Testing (1 hour)
- Test full flow: UDP → EventBus → ViewModel → UI
- Verify no missing updates
- Check performance impact
- Monitor for event leaks

---

## 📊 Implementation Status

| Feature | Status | Completion |
|---------|--------|-----------|
| DI & Navigation Modernization | ✅ 100% | DONE (Session 3) |
| Plugin [NavigationItem] System | ✅ 100% | DONE (Session 3) |
| StatisticsPlugin Migration | ✅ 100% | DONE (Session 3) |
| CmdPlugin Migration | ✅ 100% | DONE (Session 3) |
| **EventBus Infrastructure** | ✅ **100%** | **DONE (Session 4)** |
| **Z21 Backend Refactoring** | ⏳ 0% | **PENDING (Session 5)** |
| **MainWindowViewModel Refactoring** | ⏳ 0% | **PENDING (Session 5)** |
| **MonitorPageViewModel Refactoring** | ⏳ 0% | **PENDING (Session 5)** |
| Layout Persistierung | ✅ 100% | DONE |
| CollapsibleColumn Control | ✅ 100% | DONE |

---

## 🎓 Event-Driven Pattern Summary

### Architecture Flow (Post-Phase 2)

```
Z21 UDP Data (Port 21105)
    ↓ (OnUdpReceived parses)
Backend/Z21.cs
    ↓ publishes via EventBus.Publish()
IEventBus (Singleton)
    ↑ subscribes
┌─────────────────────┬──────────────────┬─────────────────────┐
│ MainWindowViewModel │ MonitorPageVM    │ Other ViewModels    │
│ (Z21 status)        │ (Occupancy)      │ (Plugins can also)  │
└─────────────────────┴──────────────────┴─────────────────────┘
    ↓ update @property
┌─────────────────────────────────────────────────────────────┐
│ XAML UI (Bindings to ViewModel Properties)                  │
└─────────────────────────────────────────────────────────────┘
```

### Key Benefits
- ✅ **Loose Coupling:** Services don't know about ViewModels
- ✅ **Single Responsibility:** Each ViewModel only handles its domain
- ✅ **Testability:** Mock EventBus instead of complex service mocks
- ✅ **Plugin-Friendly:** Plugins can subscribe to any event
- ✅ **Scalability:** Add new events without changing existing code

---

## 📝 Technical Debt Addressed

### Session 3
- ✅ Eliminated circular dependency (WinUI → Plugins)
- ✅ Standardized plugin page registration pattern
- ✅ Documented deprecation path for legacy GetPages()

### Session 4
- ✅ Created EventBus infrastructure for backend decoupling
- ✅ Defined complete Z21 event set
- ✅ Prepared DI integration

### Remaining (Session 5+)
- ⏳ Fix `DockPanelGroup.RebuildTabs` to prevent reparenting `UIElement` instances
- ⏳ Refactor Z21.cs to use EventBus
- ⏳ Refactor MainWindowViewModel to reduce God Object
- ⏳ Extract MainWindowViewModel responsibilities into domain services
- ⏳ Review DockingManager collapsed tab sizing behavior after recent changes

---

## 🔧 Files Created (Session 4)

| File | Purpose |
|------|---------|
| `Common/Events/IEvent.cs` | Base event interface |
| `Common/Events/IEventBus.cs` | EventBus interface + implementation |
| `Common/Events/Z21Events.cs` | Z21 domain events |
| `Common/Events/EventBusServiceCollectionExtensions.cs` | DI registration |

---

## ⚠️ Important Notes for Session 5

### Z21.cs Refactoring Checklist
- [ ] Inject IEventBus into Z21.cs constructor
- [ ] Find all `OnXBusStatusChanged?.Invoke()` calls
- [ ] Find all `OnLocoInfoChanged?.Invoke()` calls
- [ ] Find all `OnSystemStateChanged?.Invoke()` calls
- [ ] Find all `OnVersionInfoChanged?.Invoke()` calls
- [ ] Create mapping: old event → new EventBus event
- [ ] Replace event invocations
- [ ] Keep old event delegates for backward compatibility (optional)
- [ ] Test Z21 connection flow
- [ ] Verify no missed event invocations

### MainWindowViewModel.Z21.cs Refactoring Checklist
- [ ] Inject IEventBus into MainWindowViewModel
- [ ] Replace `z21Service.OnXBusStatusChanged += handler` with `_eventBus.Subscribe<XBusStatusChangedEvent>()`
- [ ] Update all Z21-related event handlers
- [ ] Remove now-unused direct service subscriptions
- [ ] Test UI updates still work
- [ ] Verify all properties are still populated

---

## 📚 Further Information

- **Event System Entry Point:** `Common/Events/IEventBus.cs`
- **Z21 Events:** `Common/Events/Z21Events.cs`
- **DI Registration:** `WinUI/App.xaml.cs` line ~155 (`services.AddEventBus()`)
- **Next Refactoring:** `Backend/Z21.cs` OnUdpReceived method

---

## 🎯 Session 4 Accomplishments

- **Time:** ~1 hour
- **Files Created:** 4
- **Lines of Code:** ~400
- **Infrastructure:** Complete
- **Build Success:** 100%
- **Technical Depth:** Foundation for major MainWindowViewModel refactoring

---

**Status:** ✅ **Ready for Session 6+ (DockingManager fix + Z21 refactoring)**
