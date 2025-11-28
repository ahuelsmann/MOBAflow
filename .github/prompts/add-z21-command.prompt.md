---
description: Template for adding new Z21 protocol commands with proper event handling and testing
---

# Add Z21 Protocol Command

I need to add a new Z21 protocol command. Please help me:

## 1. Define Command in Backend

```csharp
// Backend/Z21.cs - Add method
public async Task Send[CommandName]Async([Parameters])
{
    var header = new byte[] { /* packet size */, 0x00, /* header */ };
    var data = new byte[] { /* X-Bus header */, /* command bytes */ };
    
    var packet = header.Concat(data).ToArray();
    
    await _udpClient.SendAsync(packet);
    _logger?.LogDebug("Z21: Sent [CommandName]");
}
```

## 2. Add Response Handler (if applicable)

```csharp
// Backend/Z21.cs - In HandleReceivedData method
private void HandleReceivedData(byte[] data)
{
    if (data.Length < 4) return;
    
    var header = BitConverter.ToUInt16(data, 2);
    
    switch (header)
    {
        case 0xXXXX: // Your command response header
            Handle[CommandName]Response(data);
            break;
        // ... existing cases
    }
}

private void Handle[CommandName]Response(byte[] data)
{
    // Parse response
    var value = data[4]; // Example
    
    // Raise event
    On[CommandName]Changed?.Invoke(this, new [CommandName]EventArgs(value));
}
```

## 3. Define Event and EventArgs

```csharp
// Backend/Interface/IZ21.cs
public interface IZ21
{
    // ... existing members
    event EventHandler<[CommandName]EventArgs> On[CommandName]Changed;
    Task Send[CommandName]Async([Parameters]);
}

// Backend/Model/Event/[CommandName]EventArgs.cs
namespace Moba.Backend.Model.Event;

public class [CommandName]EventArgs : EventArgs
{
    public [PropertyType] [PropertyName] { get; }
    
    public [CommandName]EventArgs([PropertyType] value)
    {
        [PropertyName] = value;
    }
}
```

## 4. Add Test

```csharp
// Test/Backend/Z21Tests.cs
[Test]
public async Task Send[CommandName]_Should_SendCorrectPacket()
{
    // Arrange
    var fakeUdp = new FakeUdpClientWrapper();
    var z21 = new Z21(fakeUdp, null);
    
    // Act
    await z21.Send[CommandName]Async([testParameters]);
    
    // Assert
    var sentData = fakeUdp.SentData;
    Assert.That(sentData, Is.Not.Null);
    Assert.That(sentData.Length, Is.EqualTo([expectedLength]));
    Assert.That(sentData[2], Is.EqualTo([expectedHeader]));
}

[Test]
public void Z21_Should_Raise[CommandName]Event_OnResponse()
{
    // Arrange
    var fakeUdp = new FakeUdpClientWrapper();
    var z21 = new Z21(fakeUdp, null);
    
    [CommandName]EventArgs? receivedEvent = null;
    z21.On[CommandName]Changed += (sender, e) => receivedEvent = e;
    
    // Act
    var responseData = new byte[] { /* mock response */ };
    fakeUdp.SimulateReceive(responseData);
    
    // Assert
    Assert.That(receivedEvent, Is.Not.Null);
    Assert.That(receivedEvent.[PropertyName], Is.EqualTo([expectedValue]));
}
```

## 5. Update Documentation

```markdown
// docs/Z21-PROTOCOL.md - Add command documentation

### [CommandName]

**Command**: `0xXXXX`  
**Direction**: Client → Z21  
**Purpose**: [Description]

**Packet Structure**:
| Byte | Value | Description |
|------|-------|-------------|
| 0-1  | Length | Packet size |
| 2-3  | 0xXXXX | Header |
| 4    | [Data] | [Description] |

**Response**:
| Byte | Value | Description |
|------|-------|-------------|
| 0-1  | Length | Packet size |
| 2-3  | 0xYYYY | Response header |
| 4+   | [Data] | [Description] |

**Event**: `On[CommandName]Changed`
```

## 6. Usage Example

```csharp
// In ViewModel or Manager
public async Task Execute[Action]Async()
{
    await _z21.Send[CommandName]Async([parameters]);
}

// Subscribe to event
_z21.On[CommandName]Changed += (sender, e) =>
{
    _logger.LogInformation($"[CommandName] changed: {e.[PropertyName]}");
    // Update UI or model
};
```

---

## Command Details

**Command Name**: [YOUR_COMMAND_NAME]  
**Z21 Header**: `0x[XXXX]`  
**X-Bus Byte**: `0x[XX]`  
**Parameters**: [LIST_PARAMETERS]  
**Response Header**: `0x[YYYY]` (if applicable)  
**Response Data**: [DESCRIBE_RESPONSE]

## Protocol Reference

- [ ] Check Z21 LAN Protocol V1.13 documentation
- [ ] Verify header values
- [ ] Test with real Z21 hardware (if available)
- [ ] Add FakeUdpClientWrapper test

## Checklist

- [ ] Command method added to `IZ21` interface
- [ ] Implementation in `Z21.cs`
- [ ] Event and EventArgs defined
- [ ] Response handler implemented (if needed)
- [ ] Unit tests added
- [ ] Documentation updated
- [ ] Tested with FakeUdpClientWrapper
- [ ] Backend stays platform-independent

#file:Backend/Z21.cs
#file:Backend/Interface/IZ21.cs
#file:docs/Z21-PROTOCOL.md
#file:Test/Backend/Z21Tests.cs
