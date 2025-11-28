---
description: Testing patterns and best practices for MOBAflow unit and integration tests
applyTo: "Test/**/*.cs"
---

# Testing Guidelines

## 🎯 Testing Philosophy

**Core Principle**: Tests are **living documentation** of how the system works.

---

## 🏗️ AAA Pattern (Arrange-Act-Assert)

```csharp
// ✅ CORRECT: Clear AAA structure
[Test]
public async Task SendLocoSpeedAsync_Should_SendCorrectPacket()
{
    // Arrange
    var fakeUdp = new FakeUdpClientWrapper();
    var z21 = new Z21(fakeUdp, null);
    const int address = 3;
    const byte speed = 50;
    
    // Act
    await z21.SendLocoSpeedAsync(address, speed);
    
    // Assert
    var sentData = fakeUdp.SentData;
    Assert.That(sentData, Is.Not.Null);
    Assert.That(sentData.Length, Is.EqualTo(10));
    Assert.That(sentData[4], Is.EqualTo(0xE4)); // X-Bus header
}

// ❌ WRONG: Mixed concerns, unclear structure
[Test]
public void Test1()
{
    var z21 = new Z21(new FakeUdpClientWrapper(), null);
    z21.SendLocoSpeedAsync(3, 50).Wait();
    Assert.That(true); // What are we testing?
}
```

---

## 🔧 FakeUdpClientWrapper Usage

```csharp
// ✅ CORRECT: Use FakeUdpClientWrapper to avoid real UDP
[Test]
public void Z21_Should_RaiseEvent_OnFeedbackResponse()
{
    // Arrange
    var fakeUdp = new FakeUdpClientWrapper();
    var z21 = new Z21(fakeUdp, null);
    
    bool eventRaised = false;
    z21.OnXBusStatusChanged += (sender, e) => eventRaised = true;
    
    // Act
    var responsePacket = Z21Packets.StatusResponse;
    fakeUdp.SimulateReceive(responsePacket);
    
    // Assert
    Assert.That(eventRaised, Is.True);
}

// ❌ WRONG: Real UDP client (network dependency)
[Test]
public void Test_RealUdp()
{
    var udp = new UdpWrapper(); // ❌ Real network I/O!
    var z21 = new Z21(udp, null);
    // Tests become flaky, require network setup
}
```

---

## 📦 Test Data Management

```csharp
// ✅ CORRECT: Centralized test data
// Test/TestData/Z21Packets.cs
public static class Z21Packets
{
    public static byte[] StatusResponse => new byte[]
    {
        0x08, 0x00, 0x40, 0x00, 0x62, 0x22, 0x00, 0x00
    };
    
    public static byte[] PowerOn => new byte[]
    {
        0x07, 0x00, 0x40, 0x00, 0x61, 0x00, 0x61
    };
}

// Use in tests:
var packet = Z21Packets.StatusResponse;
fakeUdp.SimulateReceive(packet);
```

---

## 🎭 Mocking vs Faking

### When to Fake (Custom Test Double)

```csharp
// ✅ Use FakeUdpClientWrapper for I/O
public class FakeUdpClientWrapper : IUdpClientWrapper
{
    public byte[]? SentData { get; private set; }
    
    public Task SendAsync(byte[] data)
    {
        SentData = data;
        return Task.CompletedTask;
    }
    
    public void SimulateReceive(byte[] data)
    {
        DataReceived?.Invoke(this, new UdpReceivedEventArgs(data));
    }
    
    public event EventHandler<UdpReceivedEventArgs>? DataReceived;
}
```

### When to Mock (NSubstitute/Moq)

```csharp
// ✅ Mock for simple interfaces
[Test]
public void JourneyManager_Should_CallZ21_OnStart()
{
    // Arrange
    var mockZ21 = Substitute.For<IZ21>();
    var manager = new JourneyManager(mockZ21);
    
    // Act
    await manager.StartJourneyAsync(journey);
    
    // Assert
    await mockZ21.Received(1).SendLocoSpeedAsync(Arg.Any<int>(), Arg.Any<byte>());
}
```

---

## 🧪 ViewModel Testing with Factories

```csharp
// ✅ CORRECT: Test factory creation
[Test]
public void WinUIJourneyViewModelFactory_Should_CreateViewModel()
{
    // Arrange
    var dispatcher = CreateTestDispatcher();
    var factory = new WinUIJourneyViewModelFactory(dispatcher);
    var model = new Journey { Name = "Test Journey" };
    
    // Act
    var viewModel = factory.Create(model);
    
    // Assert
    Assert.That(viewModel, Is.Not.Null);
    Assert.That(viewModel.Name, Is.EqualTo("Test Journey"));
}

// ✅ CORRECT: Test UI thread dispatching
[Test]
public void WinUIJourneyViewModel_Should_DispatchPropertyChanges()
{
    // Arrange
    var dispatcher = CreateTestDispatcher();
    var model = new Journey { Name = "Original" };
    var viewModel = new WinUIJourneyViewModel(model, dispatcher);
    
    bool propertyChanged = false;
    viewModel.PropertyChanged += (s, e) => propertyChanged = true;
    
    // Act
    model.Name = "Updated";
    
    // Assert (wait for dispatcher)
    await Task.Delay(50);
    Assert.That(propertyChanged, Is.True);
}
```

---

## ⚡ Async Testing Patterns

```csharp
// ✅ CORRECT: Async test method
[Test]
public async Task LoadDataAsync_Should_LoadJourneys()
{
    // Arrange
    var service = new JourneyService();
    
    // Act
    var journeys = await service.LoadJourneysAsync();
    
    // Assert
    Assert.That(journeys, Is.Not.Empty);
}

// ❌ WRONG: .Result or .Wait() (causes deadlocks)
[Test]
public void LoadData_Blocking()
{
    var service = new JourneyService();
    var journeys = service.LoadJourneysAsync().Result; // ❌ Deadlock risk!
}

// ❌ WRONG: Async void test
[Test]
public async void LoadData() // ❌ NUnit won't wait for completion
{
    await service.LoadAsync();
}
```

---

## 🔄 Event Testing

```csharp
// ✅ CORRECT: Test event raising
[Test]
public void Journey_Should_RaisePropertyChanged_OnNameChange()
{
    // Arrange
    var journey = new Journey { Name = "Original" };
    string? changedProperty = null;
    journey.PropertyChanged += (s, e) => changedProperty = e.PropertyName;
    
    // Act
    journey.Name = "Updated";
    
    // Assert
    Assert.That(changedProperty, Is.EqualTo(nameof(Journey.Name)));
}

// ✅ CORRECT: Test event subscription/unsubscription
[Test]
public void ViewModel_Should_UnsubscribeOnDispose()
{
    // Arrange
    var model = new Journey();
    var viewModel = new JourneyViewModel(model);
    
    // Act
    viewModel.Dispose();
    model.Name = "Changed";
    
    // Assert (no event should be raised after dispose)
    // Verify no memory leak by weak reference or debugger
}
```

---

## 🎯 Integration Testing

```csharp
// ✅ CORRECT: Z21 integration test
[TestFixture]
[Category("Integration")]
public class Z21IntegrationTests
{
    private Z21 _z21 = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    
    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp, null);
    }
    
    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
    }
    
    [Test]
    public async Task CompleteWorkflow_Should_ExecuteAllCommands()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var manager = new WorkflowManager(_z21);
        
        // Act
        await manager.ExecuteAsync(workflow);
        
        // Assert
        Assert.That(_fakeUdp.SendCount, Is.EqualTo(3)); // 3 commands
    }
}
```

---

## 📋 DI Testing

```csharp
// ✅ CORRECT: Test all DI registrations
[TestFixture]
public class WinUiDiTests
{
    private IServiceProvider _serviceProvider = null!;
    
    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        
        // Register all services as in App.xaml.cs
        services.AddSingleton<IZ21, Z21>();
        services.AddSingleton<IJourneyViewModelFactory, WinUIJourneyViewModelFactory>();
        // ... all other registrations
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Test]
    public void JourneyViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<IJourneyViewModelFactory>();
        
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<WinUIJourneyViewModelFactory>());
    }
    
    [Test]
    public void AllFactories_ShouldBeRegistered()
    {
        Assert.DoesNotThrow(() =>
        {
            _serviceProvider.GetRequiredService<IJourneyViewModelFactory>();
            _serviceProvider.GetRequiredService<IStationViewModelFactory>();
            _serviceProvider.GetRequiredService<IWorkflowViewModelFactory>();
            // ... all other factories
        });
    }
}
```

---

## 🚫 Common Anti-Patterns

```csharp
// ❌ WRONG: Testing implementation details
[Test]
public void CheckPrivateField()
{
    var obj = new MyClass();
    var field = typeof(MyClass).GetField("_privateField", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.That(field.GetValue(obj), Is.Not.Null); // ❌ Fragile!
}

// ❌ WRONG: Multiple assertions without clear focus
[Test]
public void TestEverything()
{
    var journey = new Journey();
    Assert.That(journey.Name, Is.Not.Null);
    Assert.That(journey.Stations, Is.Empty);
    Assert.That(journey.IsValid, Is.False);
    // Too many concerns in one test
}

// ❌ WRONG: Tests depending on external state
[Test]
public void TestWithFileSystem()
{
    File.WriteAllText("test.json", "{}"); // ❌ External dependency
    var data = LoadFromFile("test.json");
    Assert.That(data, Is.Not.Null);
}
```

---

## ✅ Test Naming Convention

```csharp
// ✅ PATTERN: [Method]_Should_[ExpectedBehavior]_When_[Condition]

[Test]
public void SendLocoSpeedAsync_Should_SendCorrectPacket()

[Test]
public void CreateJourney_Should_ThrowException_When_NameIsNull()

[Test]
public void ViewModel_Should_UpdateUI_When_ModelChanges()
```

---

## 📊 Test Categories

```csharp
// ✅ Use categories for filtering
[TestFixture]
[Category("Unit")]
public class JourneyTests { }

[TestFixture]
[Category("Integration")]
public class Z21IntegrationTests { }

[TestFixture]
[Category("UI")]
public class ViewModelTests { }

// Run specific category:
// dotnet test --filter "Category=Unit"
```

---

## 🎯 Checklist

When writing tests:

- [ ] Use AAA pattern (Arrange-Act-Assert)
- [ ] Test method name describes behavior
- [ ] Use `FakeUdpClientWrapper` instead of real UDP
- [ ] Async tests return `Task`, not `async void`
- [ ] No `.Result` or `.Wait()` on async code
- [ ] One logical assertion per test (or related group)
- [ ] Tests are isolated (no shared state)
- [ ] Use `[SetUp]` and `[TearDown]` for initialization
- [ ] Test public API, not implementation details
- [ ] Add `[Category]` for test filtering

---

## 📂 Test Organization

```
Test/
├── Backend/                # Backend logic tests
│   ├── Z21Tests.cs
│   ├── JourneyManagerTests.cs
│   └── WorkflowTests.cs
├── SharedUI/               # ViewModel tests
│   ├── JourneyViewModelTests.cs
│   └── MainWindowViewModelTests.cs
├── WinUI/                  # Platform-specific tests
│   └── WinUiDiTests.cs
├── MAUI/                   # MAUI-specific tests
│   └── MauiDiTests.cs
├── WebApp/                 # Blazor tests
│   └── WebAppDiTests.cs
├── Mocks/                  # Test doubles
│   └── FakeUdpClientWrapper.cs
├── TestData/               # Shared test data
│   └── Z21Packets.cs
└── TestBase/               # Base classes
    └── ViewModelTestBase.cs
```
