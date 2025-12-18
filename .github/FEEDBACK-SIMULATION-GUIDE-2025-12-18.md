# Feedback Simulation für Messenger-Pattern Tests

**Purpose:** How to properly simulate Z21 feedbacks in unit tests with Messenger  
**Status:** Documentation for test implementation  
**Date:** 2025-12-18

---

## 🎯 Overview

Feedback-Simulation ist der **Kern des Test-Patterns** für die neue Messenger-Architektur.

**Flow:**
```
FakeUdpClientWrapper.RaiseReceived(bytes)
    ↓
UdpWrapper.Received Event
    ↓
Z21.OnUdpReceived()
    ↓
WeakReferenceMessenger.Default.Send(FeedbackReceivedMessage)
    ↓
Manager/ViewModel receives & processes
```

---

## 📋 How Feedback Simulation Works

### Step 1: Create Test Feedback Bytes
```csharp
// Valid Z21 RBus feedback packet format:
// [Length(2)] [CRC(2)] [0xF0] [0xA1] [InPort(1)] [Value(1)]
// Example: InPort = 5, Value = 0x03 (bit 0 on)

var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
```

**Byte Breakdown:**
- `0x04, 0x00` = Length (4 bytes of payload)
- `0xF0, 0xA1` = RBus feedback marker
- `0x05` = InPort (which feedback point, 1-128)
- `0x03` = Value (which bits are active)

### Step 2: Simulate Reception
```csharp
// FakeUdpClientWrapper has this method:
public void RaiseReceived(byte[] payload, IPEndPoint? remote = null)
{
    Received?.Invoke(this, new UdpReceivedEventArgs(payload, remote ?? ...));
}

// Call it in test:
_fakeUdp.RaiseReceived(testFeedback);
```

### Step 3: Wait for Async Processing
```csharp
// Feedback processing is async (Task.Run)
// Give it time to execute:
System.Threading.Thread.Sleep(100);  // 100ms is safe for unit tests

// Or use proper async/await:
await Task.Delay(100);
```

### Step 4: Assert Results
```csharp
// Verify message was published and received
Assert.That(messageReceived, Is.True);
Assert.That(capturedMessage.Value, Is.EqualTo(5u));  // InPort
```

---

## 🧪 Complete Test Example

### Basic Pattern
```csharp
[TestFixture]
public class FeedbackSimulationTests
{
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;

    [SetUp]
    public void Setup()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp);
    }

    [TearDown]
    public void Cleanup()
    {
        _z21.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [Test]
    public void Feedback_Published_AsMessage()
    {
        // Arrange
        FeedbackReceivedMessage? capturedMessage = null;
        
        WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
            this,
            (r, m) => capturedMessage = m
        );

        // Act
        var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
        _fakeUdp.RaiseReceived(testFeedback);
        
        System.Threading.Thread.Sleep(100);  // ⚠️ IMPORTANT!

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage.Value, Is.EqualTo(5u));  // InPort
    }
}
```

---

## 🔑 Key Points for Feedback Simulation

### 1. **Valid Feedback Packet Format**
```csharp
// Template: [Length] [CRC] [0xF0] [0xA1] [InPort] [Value]

// InPort 5, bit 0 active
var feedback5 = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };

// InPort 10, bit 1 active
var feedback10 = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x0A, 0x02 };

// InPort 128, all bits active
var feedback128 = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x80, 0xFF };
```

### 2. **Always Wait After RaiseReceived()**
```csharp
// ❌ WRONG: No wait
_fakeUdp.RaiseReceived(feedback);
Assert.That(received, Is.True);  // May fail! (Still processing)

// ✅ CORRECT: Wait for async
_fakeUdp.RaiseReceived(feedback);
System.Threading.Thread.Sleep(100);  // Process time
Assert.That(received, Is.True);  // Now it's ready
```

### 3. **Proper Async/Await Pattern (Preferred)**
```csharp
[Test]
public async Task Feedback_Processed_Correctly()
{
    // Arrange
    bool processed = false;
    WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
        this,
        (r, m) => processed = true
    );

    // Act
    _fakeUdp.RaiseReceived(testFeedback);
    
    // Wait for async processing
    await Task.Delay(100);

    // Assert
    Assert.That(processed, Is.True);
}
```

### 4. **Test Multiple Feedbacks**
```csharp
[Test]
public void MultipleFeedbacks_AllProcessed()
{
    // Arrange
    int feedbackCount = 0;
    WeakReferenceMessenger.Default.Register<FeedbackReceivedMessage>(
        this,
        (r, m) => feedbackCount++
    );

    // Act
    _fakeUdp.RaiseReceived(new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 });
    System.Threading.Thread.Sleep(50);
    
    _fakeUdp.RaiseReceived(new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x0A, 0x02 });
    System.Threading.Thread.Sleep(50);
    
    _fakeUdp.RaiseReceived(new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x0F, 0x01 });
    System.Threading.Thread.Sleep(50);

    // Assert
    Assert.That(feedbackCount, Is.EqualTo(3));
}
```

---

## 🎯 Integration Test with Manager

### Full Stack Test
```csharp
[Test]
public void Feedback_FlowsThrough_Manager()
{
    // Arrange
    var fakeUdp = new FakeUdpClientWrapper();
    var z21 = new Z21(fakeUdp);
    
    var project = new Project
    {
        Journeys = new List<Journey>
        {
            new Journey { Id = Guid.NewGuid(), Name = "TestJourney", InPort = 5, FirstPos = 1 }
        },
        Workflows = new List<Workflow>()
    };
    
    var workflowService = new WorkflowService(null);
    var journeyMgr = new JourneyManager(z21, project, workflowService);
    
    bool journeyFeedbackFired = false;
    journeyMgr.FeedbackReceived += (s, e) => journeyFeedbackFired = true;

    // Act
    var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
    fakeUdp.RaiseReceived(testFeedback);
    
    System.Threading.Thread.Sleep(100);  // ⚠️ WAIT!

    // Assert
    Assert.That(journeyFeedbackFired, Is.True);
}
```

---

## ⚠️ Common Mistakes

### ❌ Mistake 1: No Wait After RaiseReceived
```csharp
// WRONG - Race condition!
_fakeUdp.RaiseReceived(feedback);
Assert.That(received, Is.True);  // May fail sometimes
```

**Fix:** Always wait
```csharp
_fakeUdp.RaiseReceived(feedback);
System.Threading.Thread.Sleep(100);
Assert.That(received, Is.True);  // Reliable
```

---

### ❌ Mistake 2: Invalid Feedback Packet
```csharp
// WRONG - Invalid format
var badFeedback = new byte[] { 0x05, 0x03 };  // Too short!
_fakeUdp.RaiseReceived(badFeedback);  // Will be ignored
```

**Fix:** Use valid format
```csharp
var goodFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
_fakeUdp.RaiseReceived(goodFeedback);  // Works!
```

---

### ❌ Mistake 3: Forgetting TearDown Cleanup
```csharp
// WRONG - Test pollution!
[Test]
public void Test1()
{
    WeakReferenceMessenger.Default.Register<MyMessage>(this, ...);
    // No cleanup!
}

[Test]
public void Test2()
{
    // Old subscription still active! ⚠️
}
```

**Fix:** Always cleanup
```csharp
[TearDown]
public void Cleanup()
{
    WeakReferenceMessenger.Default.UnregisterAll(this);  // ✅
}
```

---

## 📊 Feedback Packet Generator Helper

Lass mich einen Helper erstellen für Test-Feedback-Generierung:

```csharp
// Test/Helpers/FeedbackPacketGenerator.cs
namespace Moba.Test.Helpers;

/// <summary>
/// Helper to generate valid Z21 RBus feedback packets for testing
/// </summary>
public static class FeedbackPacketGenerator
{
    /// <summary>
    /// Creates a valid RBus feedback packet
    /// </summary>
    /// <param name="inPort">Feedback point number (1-128)</param>
    /// <param name="value">Bit value (0x00 = all off, 0xFF = all on)</param>
    /// <returns>Valid Z21 RBus feedback packet bytes</returns>
    public static byte[] CreateFeedbackPacket(byte inPort, byte value = 0x03)
    {
        // [Length] [CRC] [0xF0] [0xA1] [InPort] [Value]
        return new byte[] { 0x04, 0x00, 0xF0, 0xA1, inPort, value };
    }

    /// <summary>
    /// Creates feedback packet with bit 0 active (typical)
    /// </summary>
    public static byte[] CreateFeedbackPacket(byte inPort)
        => CreateFeedbackPacket(inPort, 0x03);

    /// <summary>
    /// Creates multiple feedback packets for sequence testing
    /// </summary>
    public static IEnumerable<byte[]> CreateSequence(int startPort, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateFeedbackPacket((byte)(startPort + i));
        }
    }
}

// Usage in tests:
[Test]
public void TestWithHelper()
{
    // Single feedback
    var feedback = FeedbackPacketGenerator.CreateFeedbackPacket(5);
    _fakeUdp.RaiseReceived(feedback);
    
    // Or sequence
    foreach (var packet in FeedbackPacketGenerator.CreateSequence(1, 5))
    {
        _fakeUdp.RaiseReceived(packet);
        System.Threading.Thread.Sleep(50);
    }
}
```

---

## 🔗 Integration with Existing Tests

### Current Implementation (FeedbackMessengerTests.cs)
```csharp
// Already using RaiseReceived correctly:
var testFeedback = new byte[] { 0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03 };
_fakeUdp.RaiseReceived(testFeedback);
System.Threading.Thread.Sleep(100);

// ✅ This pattern is CORRECT and should be maintained
```

---

## 📋 Checklist for Feedback Simulation Tests

- [x] Valid packet format used (6 bytes: Length, CRC, 0xF0, 0xA1, InPort, Value)
- [x] RaiseReceived() called on FakeUdpClientWrapper
- [x] Wait for async processing (Thread.Sleep or Task.Delay)
- [x] Messenger subscription setup before simulation
- [x] TearDown cleanup with UnregisterAll()
- [x] Multiple subscribers tested
- [x] Edge cases covered (InPort 1, 5, 128)
- [x] Integration with Manager tested

---

## 🎯 Best Practices Summary

| Practice | Why | Example |
|----------|-----|---------|
| **Valid packet** | Z21 ignores invalid | `[0x04,0x00,0xF0,0xA1,InPort,Value]` |
| **Wait after raise** | Async processing | `Thread.Sleep(100)` |
| **Use helper** | Readable tests | `FeedbackPacketGenerator.Create...` |
| **Cleanup in TearDown** | Prevent pollution | `UnregisterAll(this)` |
| **Multiple subscribers** | Test real scenarios | Register 2-3 handlers |
| **Integration tests** | Validate end-to-end | Test with actual Manager |

---

## ✅ Current Implementation Status

**FeedbackMessengerTests.cs is already CORRECT:**
- ✅ Uses valid packet format
- ✅ Waits for processing
- ✅ Cleans up properly
- ✅ Tests multiple scenarios
- ✅ Integration tested with JourneyManager

**No changes needed** - current implementation follows all best practices!

---

**Status:** Documentation Complete 📚  
**Reference:** `Test/FeedbackMessengerTests.cs` for examples  
**Date:** 2025-12-18
