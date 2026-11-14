// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.SharedUI.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// Verifies how the throttle follows runtime feedback without echoing it to the Z21, which speed changes it refuses
/// for safety (no control, brake applied, doors open) and when it asks the Z21 for a locomotive's state.
/// </summary>
[TestFixture]
internal sealed partial class TrainControlViewModelRuntimeStateTests
{
    private const int Address = 7;
    private static readonly string[] IndicatorProperties = ["Z21StatusText", "Z21StatusSemanticDescription", "Z21IndicatorResourceKey"];

    [Test]
    public void LocomotiveFeedback_ForTheSelectedAddress_UpdatesTheThrottleWithoutSendingADriveCommand()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;

        fixture.Publish(Connected(new LocomotiveRuntimeSnapshot { Address = Address, Speed = 40, IsForward = false }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.Speed, Is.EqualTo(40));
            Assert.That(fixture.ViewModel.IsForward, Is.False);
            Assert.That(fixture.ViewModel.DirectionStatusText, Is.EqualTo("Backward"));
            Assert.That(fixture.ViewModel.StatusMessage, Is.EqualTo("Loco 7: 40 REV"));
            fixture.Runtime.Verify(
                runtime => runtime.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Test]
    public void LocomotiveFeedback_ThenALowerSpeedFromTheOperator_IsAccepted()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;
        fixture.Publish(Connected(new LocomotiveRuntimeSnapshot { Address = Address, Speed = 40, IsForward = true }));
        fixture.Disconnect();

        // Feedback set the reference speed: without control only a reduction below it is allowed.
        fixture.ViewModel.Speed = 50;
        var increased = fixture.ViewModel.Speed;
        fixture.ViewModel.Speed = 30;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(increased, Is.EqualTo(40));
            Assert.That(fixture.ViewModel.Speed, Is.EqualTo(30));
        }
    }

    [Test]
    public void LocomotiveFeedback_ForAnotherAddress_IsIgnored()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;
        var statusBefore = fixture.ViewModel.StatusMessage;

        fixture.Publish(Connected(new LocomotiveRuntimeSnapshot { Address = 5, Speed = 40, IsForward = false }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.Speed, Is.Zero);
            Assert.That(fixture.ViewModel.IsForward, Is.True);
            Assert.That(fixture.ViewModel.StatusMessage, Is.EqualTo(statusBefore));
        }
    }

    [Test]
    public void SpeedIncrease_WithoutControl_IsRefused()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.LocoAddress = Address;

        fixture.ViewModel.Speed = 10;

        Assert.That(fixture.ViewModel.Speed, Is.Zero);
    }

    [Test]
    public void SpeedIncrease_WithTheParkingBrakeApplied_IsRefused()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;
        fixture.ViewModel.IsParkingBrakeEnabled = true;

        fixture.ViewModel.Speed = 10;

        Assert.That(fixture.ViewModel.Speed, Is.Zero);
    }

    [Test]
    public void SpeedIncrease_WithOpenDoors_IsRefused()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;
        fixture.ViewModel.IsDoorReleaseLocked = false;

        fixture.ViewModel.Speed = 10;

        Assert.That(fixture.ViewModel.Speed, Is.Zero);
    }

    [Test]
    public void SpeedIncrease_WithControlAndClosedDoors_IsAccepted()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;

        fixture.ViewModel.Speed = 10;

        Assert.That(fixture.ViewModel.Speed, Is.EqualTo(10));
    }

    [Test]
    public void DirectionChange_AtStandstillWithControl_IsSentImmediately()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.ViewModel.LocoAddress = Address;

        fixture.ViewModel.IsForward = false;

        using (Assert.EnterMultipleScope())
        {
            fixture.Runtime.Verify(runtime => runtime.SetLocomotiveDriveAsync(Address, 0, false, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(fixture.ViewModel.StatusMessage, Is.EqualTo("Loco 7: 0 REV"));
            Assert.That(fixture.ViewModel.DirectionStatusText, Is.EqualTo("Backward"));
        }
    }

    [Test]
    public void DirectionChange_WithoutControl_SendsNothing()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.LocoAddress = Address;

        fixture.ViewModel.IsForward = false;

        fixture.Runtime.Verify(
            runtime => runtime.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(9999, true)]
    [TestCase(10000, false)]
    public void AddressChange_WhileConnected_RequestsOnlyValidDccAddresses(int address, bool requested)
    {
        using var fixture = new Fixture();
        fixture.Connect();

        fixture.ViewModel.LocoAddress = address;

        fixture.Runtime.Verify(
            runtime => runtime.RequestLocomotiveInfoAsync(address, It.IsAny<CancellationToken>()),
            requested ? Times.Once() : Times.Never());
    }

    [Test]
    public void AddressChange_WhenTheRequestFails_ShowsTheError()
    {
        using var fixture = new Fixture();
        fixture.Connect();
        fixture.Runtime.Setup(runtime => runtime.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Z21 busy"));

        fixture.ViewModel.LocoAddress = Address;

        Assert.That(fixture.ViewModel.StatusMessage, Is.EqualTo("Error: Z21 busy"));
    }

    [Test]
    public void ConnectionChanges_AreAnnouncedAndShownOnTheIndicator()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.LocoAddress = 0;
        var changed = new HashSet<string?>(StringComparer.Ordinal);
        fixture.ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        fixture.Connect();
        var connected = (fixture.ViewModel.StatusMessage, fixture.ViewModel.Z21StatusText,
            fixture.ViewModel.Z21StatusSemanticDescription, fixture.ViewModel.Z21IndicatorResourceKey);
        fixture.Disconnect();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(connected, Is.EqualTo(("Z21 Connected", "ON", "Z21 connected", "RailwayAccent")));
            Assert.That(fixture.ViewModel.StatusMessage, Is.EqualTo("Z21 Disconnected"));
            Assert.That(fixture.ViewModel.Z21StatusText, Is.EqualTo("OFF"));
            Assert.That(fixture.ViewModel.Z21StatusSemanticDescription, Is.EqualTo("Z21 disconnected"));
            Assert.That(fixture.ViewModel.Z21IndicatorResourceKey, Is.EqualTo("RailwayDanger"));
            Assert.That(changed, Is.SupersetOf(IndicatorProperties));
        }
    }

    [Test]
    public void Dispose_StopsFollowingRuntimeSnapshots()
    {
        var fixture = new Fixture();
        fixture.Dispose();

        fixture.Connect();

        Assert.That(fixture.ViewModel.IsConnected, Is.False);
    }

    private static MobaRuntimeSnapshot Connected(LocomotiveRuntimeSnapshot locomotive) => new()
    {
        IsConnected = true,
        LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot> { [locomotive.Address] = locomotive }
    };

    private sealed partial class Fixture : IDisposable
    {
        private readonly EventBus _eventBus = new(NullLogger<EventBus>.Instance);

        public Fixture()
        {
            Runtime.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(service => service.GetSettings()).Returns(new UiSettings());
            settingsService.Setup(service => service.SaveSettingsAsync(It.IsAny<UiSettings>())).Returns(Task.CompletedTask);
            ViewModel = new TrainControlViewModel(
                Runtime.Object,
                new LocalRuntimeCommandGateway(Runtime.Object),
                settingsService.Object,
                new MobileSolutionContext(TestViewModelServices.Default),
                NullLogger<TrainControlViewModel>.Instance,
                ImmediateUiDispatcher.Instance,
                _eventBus,
                new NullFunctionAppearancePicker(),
                new NullRuntimeRoutingState());
        }

        public Mock<IMobaRuntime> Runtime { get; } = new();

        public TrainControlViewModel ViewModel { get; }

        public void Publish(MobaRuntimeSnapshot snapshot) => _eventBus.Publish(new RuntimeSnapshotChangedEvent(snapshot));

        public void Connect() => Publish(new MobaRuntimeSnapshot { IsConnected = true });

        public void Disconnect() => Publish(new MobaRuntimeSnapshot { IsConnected = false });

        public void Dispose() => ViewModel.Dispose();
    }
}
