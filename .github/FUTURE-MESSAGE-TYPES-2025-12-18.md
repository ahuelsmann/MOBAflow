# Future Message Types - Template & Checklist

**Purpose:** Reference for creating additional message types beyond FeedbackReceivedMessage  
**Status:** Optional enhancements  
**Target:** Q1 2026 and beyond

---

## 📋 Message Type Template

Use this template when creating new message types:

```csharp
// Domain/Message/[EntityName][Action]Message.cs
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain.Message;

using CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// Message: [What happened, past tense]
/// 
/// Published by: [Which class/service publishes this]
/// Subscribed by: [Who listens - Manager, ViewModel, Logger, etc.]
/// 
/// Use Case: [When would this be used?]
/// 
/// Threading: [Which thread publishes? (Backend = background thread)]
/// 
/// Backward Compat: [Any legacy events this replaces?]
/// </summary>
public sealed class [MessageName] : ValueChangedMessage<[DataType]>
{
    /// <summary>
    /// Additional context if needed (timestamp, IDs, etc.)
    /// </summary>
    public DateTime OccurredAt { get; }
    
    /// <summary>
    /// Creates a new message with current UTC timestamp.
    /// </summary>
    public [MessageName]([DataType] value)
        : this(value, DateTime.UtcNow)
    {
    }
    
    /// <summary>
    /// Creates a new message with explicit timestamp.
    /// </summary>
    public [MessageName]([DataType] value, DateTime occurredAt)
        : base(value)
    {
        OccurredAt = occurredAt;
    }
}
```

---

## 🗂️ Candidate Message Types

### Priority 1: High-Value (Q1 2026)

#### 1️⃣ **StationChangedMessage**
```csharp
// Published by: JourneyManager
// Subscribed by: ViewModels, Logging
// Data: Journey ID, Station Name, Station Index
public sealed class StationChangedMessage : ValueChangedMessage<string> // Station Name
{
    public Guid JourneyId { get; }
    public int StationIndex { get; }
}

// Usage:
// JourneyManager (Backend) → StationChangedMessage → ViewModel (UI)
// Enables: Real-time "train at station X" updates in UI
```

**When to implement:** After verifying real-time UI updates are needed

---

#### 2️⃣ **WorkflowExecutedMessage**
```csharp
// Published by: JourneyManager (after workflow execution)
// Subscribed by: ViewModels, Logger, Statistics
public sealed class WorkflowExecutedMessage : ValueChangedMessage<Guid> // Workflow ID
{
    public Guid JourneyId { get; }
    public DateTime StartedAt { get; }
    public DateTime CompletedAt { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
}

// Usage:
// Provides real-time feedback about workflow execution
```

**When to implement:** After station change tracking works

---

#### 3️⃣ **TrackPowerToggledMessage**
```csharp
// Published by: Z21 (after SetTrackPowerOnAsync/OffAsync)
// Subscribed by: UI, Logging
public sealed class TrackPowerToggledMessage : ValueChangedMessage<bool> // Is On
{
    public DateTime ChangedAt { get; }
}

// Usage:
// UI can disable/enable controls based on track power state
```

**When to implement:** As soon as needed for UI responsiveness

---

#### 4️⃣ **ConnectionLostMessage**
```csharp
// Published by: Z21 (when connection fails)
// Subscribed by: UI, Auto-reconnect logic
public sealed class ConnectionLostMessage : ValueChangedMessage<string> // Reason
{
    public DateTime LostAt { get; }
    public int AttemptCount { get; }
}

// Usage:
// UI shows "Disconnected" status
// Background service attempts reconnect
```

**When to implement:** When graceful disconnect handling is needed

---

### Priority 2: Nice-to-Have (Q2 2026)

#### 5️⃣ **SystemStateChangedMessage**
```csharp
// Published by: Z21 (current SystemState updates)
// Subscribed by: ViewModels (HealthStatus display), Logger
public sealed class SystemStateChangedMessage : ValueChangedMessage<SystemState>
{
    // SystemState already has: MainCurrent, Temperature, SupplyVoltage, etc.
}

// Usage:
// Real-time display of Z21 health metrics in UI
```

---

#### 6️⃣ **VersionInfoReceivedMessage**
```csharp
// Published by: Z21 (after version request)
// Subscribed by: UI (About dialog)
public sealed class VersionInfoReceivedMessage : ValueChangedMessage<Z21VersionInfo>
{
}

// Usage:
// Populate Z21 version info in settings/about page
```

---

#### 7️⃣ **EmergencyStopTriggeredMessage**
```csharp
// Published by: Z21 (when emergency stop activated)
// Subscribed by: UI, Logging, Alerting
public sealed class EmergencyStopTriggeredMessage : ValueChangedMessage<bool> // Is Active
{
}

// Usage:
// Immediate UI feedback when E-stop activated
// Audio alert in MOBAsmart
```

---

### Priority 3: Future (Q3+ 2026)

#### 8️⃣ **LocomotiveCommandMessage**
```csharp
// Published by: ViewModel (user clicked button)
// Subscribed by: Backend command dispatcher
public sealed class LocomotiveCommandMessage : ValueChangedMessage<LocomotiveId> // Which loco
{
    public int Speed { get; }
    public Direction Direction { get; }
}

// Usage:
// Bidirectional messaging (not just Backend → UI)
```

---

#### 9️⃣ **TurnoutSwitchedMessage**
```csharp
// Published by: Workflow executor (after switch command)
// Subscribed by: UI (visual feedback)
public sealed class TurnoutSwitchedMessage : ValueChangedMessage<uint> // Turnout ID
{
    public TurnoutPosition NewPosition { get; }
}

// Usage:
// Show turnout position changes in track plan view
```

---

#### 🔟 **ProjectLoadedMessage**
```csharp
// Published by: Application (after project load)
// Subscribed by: All views/managers
public sealed class ProjectLoadedMessage : ValueChangedMessage<Project>
{
}

// Usage:
// Global notification that project is ready
// Trigger initialization of all managers
```

---

## ✅ Implementation Checklist

When implementing a new message type:

### 1. **Planning Phase**
- [ ] Define: What event is this for?
- [ ] Identify: Who publishes it?
- [ ] Identify: Who needs to know about it?
- [ ] Verify: Is Messenger better than event? (Multi-subscriber = YES)
- [ ] Consider: What data needs to be in the message?

### 2. **Design Phase**
- [ ] Choose data type for `ValueChangedMessage<T>`
- [ ] Decide: Additional properties needed?
- [ ] Add: Timestamp? IDs? Error info?
- [ ] Document: Usage scenarios

### 3. **Implementation Phase**
- [ ] Create: `Domain/Message/[Name]Message.cs`
- [ ] Test: Can be created and accessed?
- [ ] Implement: Publisher code
- [ ] Implement: Subscriber code
- [ ] Add: XML documentation

### 4. **Testing Phase**
- [ ] Unit test: Message publishes correctly
- [ ] Unit test: Subscribers receive message
- [ ] Integration test: End-to-end flow
- [ ] Test: Multiple subscribers simultaneously
- [ ] Test: Unregister works

### 5. **Documentation Phase**
- [ ] Update: `MESSENGER-BEST-PRACTICES.md`
- [ ] Add: Code examples in comments
- [ ] Document: When to use this message
- [ ] Update: This template with learnings

### 6. **Review & Deploy Phase**
- [ ] Code review: Architecture approved?
- [ ] Performance: Any impact measured?
- [ ] Backward compat: Any legacy events?
- [ ] Build: All tests pass?
- [ ] Deploy: Ready for production?

---

## 🎯 Prioritization Matrix

| Message Type | Complexity | Value | Timeline | Priority |
|--------------|-----------|-------|----------|----------|
| StationChanged | Low | High | Q1 | 🔴 1 |
| WorkflowExecuted | Low | High | Q1 | 🔴 1 |
| TrackPowerToggled | Very Low | Medium | Q1 | 🟠 2 |
| ConnectionLost | Medium | High | Q1-Q2 | 🟠 2 |
| SystemStateChanged | Low | Medium | Q2 | 🟡 3 |
| VersionInfoReceived | Very Low | Low | Q2 | 🟡 3 |
| EmergencyStop | Low | High | Q2 | 🟠 2 |
| LocomotiveCommand | High | High | Q3 | 🔵 4 |
| TurnoutSwitched | Medium | High | Q3 | 🔵 4 |
| ProjectLoaded | Low | High | Q3 | 🔵 4 |

---

## 💡 Decision Tree

```
Do I need a new message type?

├─ Is it cross-layer communication? (Backend → UI)
│  └─ YES → Create message type
├─ Are there multiple subscribers?
│  └─ YES → Create message type
├─ Is it a domain event?
│  └─ YES → Consider message type
├─ Is it a simple manager-internal event?
│  └─ NO → Use traditional event or method call
└─ Am I replacing a legacy pattern?
   └─ YES → Check backward compatibility
```

---

## 🚀 Quick Start: Add a New Message

### 1. Create Message File
```csharp
// Domain/Message/StationChangedMessage.cs
public sealed class StationChangedMessage : ValueChangedMessage<string>
{
    public Guid JourneyId { get; }
    public StationChangedMessage(string stationName, Guid journeyId)
        : base(stationName)
    {
        JourneyId = journeyId;
    }
}
```

### 2. Publish from Backend
```csharp
// Backend/Manager/JourneyManager.cs
protected virtual void OnStationChanged(StationChangedEventArgs e)
{
    // Legacy event (backward compat)
    StationChanged?.Invoke(this, e);
    
    // NEW: Messenger
    WeakReferenceMessenger.Default.Send(
        new StationChangedMessage(e.StationName, _journeyId)
    );
}
```

### 3. Subscribe from UI
```csharp
// SharedUI/ViewModel/JourneyViewModel.cs
public JourneyViewModel()
{
    WeakReferenceMessenger.Default.Register<StationChangedMessage>(
        this,
        (r, m) => r.CurrentStation = m.Value
    );
}
```

### 4. Test
```csharp
[Test]
public void StationChanged_PublishesMessage()
{
    var received = false;
    WeakReferenceMessenger.Default.Register<StationChangedMessage>(
        this, (r, m) => received = true
    );
    
    manager.OnStationChanged(new(...));
    
    Assert.That(received, Is.True);
}
```

---

## 📚 Reference

- **Implementation:** `.github/IMPLEMENTATION-NOTES-MESSENGER-2025-12-18.md`
- **Best Practices:** `.github/MESSENGER-BEST-PRACTICES-2025-12-18.md`
- **Example:** `Domain/Message/FeedbackReceivedMessage.cs`
- **Tests:** `Test/FeedbackMessengerTests.cs`

---

## 🔗 Links

- [CommunityToolkit.Mvvm Messenger Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
- [ValueChangedMessage API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.toolkit.mvvm.messaging.messages.valuechangedmessage-1)
- MOBAflow Instructions: `.github/copilot-instructions.md`

---

**Last Updated:** 2025-12-18  
**Status:** Ready for implementation  
**Owner:** Architecture Team
