# CommunityToolkit.Mvvm Messenger - Quick Reference Card

**Quick Access:** Print this page or bookmark for daily reference

---

## 🚀 One-Minute Quick Start

### Publish a Message
```csharp
using CommunityToolkit.Mvvm.Messaging;
using Domain.Message;

WeakReferenceMessenger.Default.Send(
    new FeedbackReceivedMessage((uint)5, rawData)
);
```

### Subscribe to Message
```csharp
WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
    this,
    (r, m) => HandleFeedback(m.Value, m.RawData)
);
```

### Unsubscribe
```csharp
WeakReferenceMessenger.Default.Unregister<FeedbackReceivedMessage>(this);
```

---

## 📍 Where to Find What

| Need | Location | Example |
|------|----------|---------|
| **Existing Message** | `Domain/Message/*.cs` | `FeedbackReceivedMessage.cs` |
| **How to Publish** | `Backend/Z21.cs` line ~370 | See OnUdpReceived() |
| **How to Subscribe** | `Backend/Manager/BaseFeedbackManager.cs` line ~40 | See Constructor |
| **Tests** | `Test/FeedbackMessengerTests.cs` | 6 examples |
| **Best Practices** | `.github/MESSENGER-BEST-PRACTICES-2025-12-18.md` | Full guide |
| **Future Messages** | `.github/FUTURE-MESSAGE-TYPES-2025-12-18.md` | Templates |

---

## 🎯 Architecture at a Glance

```
┌─ Backend (Background Thread)
│  ├─ Z21: WeakReferenceMessenger.Default.Send(Message)
│  └─ Managers: WeakReferenceMessenger.Default.Register<Message>(...)
│
├─ EventBus: WeakReferenceMessenger.Default
│  └─ Decouples publishers from subscribers
│
└─ UI Layer (Optional)
   └─ ViewModels: Can also register for real-time updates
```

---

## 📋 Common Patterns

### Pattern A: Backend → Manager (Already Implemented ✅)
```csharp
// Backend/Z21.cs - Publishes
WeakReferenceMessenger.Default.Send(new FeedbackReceivedMessage(...));

// Backend/Manager/BaseFeedbackManager.cs - Subscribes (auto in constructor)
WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
    this, (r, m) => r.OnMessageReceived(m)
);
```

### Pattern B: Manager → UI (To Implement)
```csharp
// Backend: Publish event
WeakReferenceMessenger.Default.Send(new StationChangedMessage(...));

// UI: Subscribe for real-time updates
WeakReferenceMessenger.Default.Register<StationChangedMessage>(
    this, (r, m) => r.CurrentStation = m.Value
);
```

### Pattern C: Test Pattern
```csharp
[SetUp] public void Setup() { }

[Test]
public void Feature_Works()
{
    bool received = false;
    WeakReferenceMessenger.Default.Register<MyMessage>(
        this, (r, m) => received = true
    );
    
    // Trigger message...
    
    Assert.That(received, Is.True);
}

[TearDown]
public void Cleanup()
{
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

---

## 🔥 Pro Tips

| Tip | Code | Why |
|-----|------|-----|
| **Use recipient param** | `(recipient, msg) =>` | Avoids capturing 'this' |
| **Always cleanup tests** | `UnregisterAll(this)` | Prevents test pollution |
| **Timestamp included** | `message.ReceivedAt` | Free timestamp in message |
| **Value is InPort** | `message.Value` | First generic param |
| **Extra data** | `message.RawData` | Additional properties |

---

## ⚠️ Common Mistakes

| Mistake | ❌ Wrong | ✅ Correct |
|---------|---------|-----------|
| **Forgetting cleanup** | `WeakReferenceMessenger.Default.Register(...)` | Always `UnregisterAll()` in teardown |
| **Blocking in handler** | `Thread.Sleep(1000)` | Wrap in `Task.Run(async ...)` |
| **Capturing 'this'** | `(r,m) => { Field = ... }` | `(r,m) => { r.Field = ... }` |
| **Wrong namespace** | `using Moba.CommunityToolkit...` | `using CommunityToolkit.Mvvm.Messaging` |
| **Not awaiting** | `Task.Run(...)` without await | Use `_ = Task.Run(...)` or `ConfigureAwait(false)` |

---

## 🧪 Testing Checklist

```csharp
[TestFixture]
public class MyTests
{
    [TearDown]
    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);  // ✅ ALWAYS!
    }

    [Test]
    public void Message_Publishes()
    {
        // ✅ Register test subscriber
        bool received = false;
        WeakReferenceMessenger.Default.Register<MyMessage>(
            this, (r, m) => received = true
        );

        // ✅ Trigger message
        SomeService.TriggerMessage();

        // ✅ Assert
        Assert.That(received, Is.True);
    }
}
```

---

## 📦 What's Included (Status Dec 18, 2025)

| Item | Status | File |
|------|--------|------|
| FeedbackReceivedMessage | ✅ DONE | `Domain/Message/FeedbackReceivedMessage.cs` |
| Z21 Publisher | ✅ DONE | `Backend/Z21.cs` line ~370 |
| BaseFeedbackManager Subscriber | ✅ DONE | `Backend/Manager/BaseFeedbackManager.cs` line ~40 |
| Unit Tests | ✅ DONE | `Test/FeedbackMessengerTests.cs` (6 tests) |
| Documentation | ✅ DONE | This file + 2 other guides |
| Package Dependencies | ✅ DONE | CommunityToolkit.Mvvm 8.4.0 |

---

## 🚀 Next Steps (Optional)

### Short Term (Q1 2026)
- [ ] Create `StationChangedMessage`
- [ ] Create `WorkflowExecutedMessage`
- [ ] Add ViewModel subscription examples

### Medium Term (Q2 2026)
- [ ] Remove legacy `Z21.Received` event (after full migration)
- [ ] Create additional message types
- [ ] Add performance monitoring

### Long Term (Q3+ 2026)
- [ ] Multi-directional messaging (UI → Backend)
- [ ] Message filtering/channels
- [ ] Message history logging

---

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| **Message not received** | 1. Check subscription is in code 2. Unregister in TearDown 3. Verify message type matches |
| **Test fails randomly** | UnregisterAll() test subscribers (they persist between tests!) |
| **Memory leak suspected** | Use WeakReferenceMessenger (default), never StrongReferenceMessenger |
| **Build errors** | Add `using CommunityToolkit.Mvvm.Messaging;` |
| **Namespace not found** | Run `dotnet restore` or rebuild solution |

---

## 📞 Quick Links

- **Implementation Details:** `.github/IMPLEMENTATION-NOTES-MESSENGER-2025-12-18.md`
- **Best Practices:** `.github/MESSENGER-BEST-PRACTICES-2025-12-18.md`
- **Future Messages:** `.github/FUTURE-MESSAGE-TYPES-2025-12-18.md`
- **Official Docs:** https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger
- **CommunityToolkit NuGet:** https://www.nuget.org/packages/CommunityToolkit.Mvvm/

---

## 🎓 Key Concepts (2 Minutes)

**Messenger** = Event Bus (like CAN-Bus in cars)

**Use When:**
- ✅ Multiple subscribers
- ✅ Need decoupling
- ✅ Cross-layer communication
- ✅ Backend → UI updates

**Architecture:**
```
Publisher → Messenger ← Subscriber 1
                    ├─ Subscriber 2
                    └─ Subscriber 3
(All independent, no direct coupling)
```

**Performance:**
- ~0.1ms per message with 5 subscribers
- WeakReferences: Auto cleanup
- Type-safe at compile time

---

**Version:** 1.0 | **Date:** 2025-12-18 | **Status:** Production Ready 🚀
