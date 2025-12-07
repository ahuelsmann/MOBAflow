// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.Interface;

namespace Moba.Test.SharedUI;

[TestFixture]
public class CounterViewModelTests
{
    private sealed class StubZ21 : IZ21
    {
        public event Feedback? Received;
        public event SystemStateChanged? OnSystemStateChanged;
        public event XBusStatusChanged? OnXBusStatusChanged;
        public event Action? OnConnectionLost;
        public bool IsConnected { get; private set; }
        public Task ConnectAsync(System.Net.IPAddress address, int port = 21105, CancellationToken cancellationToken = default) { IsConnected = true; return Task.CompletedTask; }
        public Task DisconnectAsync() { IsConnected = false; return Task.CompletedTask; }
        public Task SendCommandAsync(byte[] sendBytes) => Task.CompletedTask;
        public Task SetTrackPowerOnAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetTrackPowerOffAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetEmergencyStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GetStatusAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void SimulateFeedback(int inPort) => Received?.Invoke(new FeedbackResult([0x0F,0x00,0x80,0x00, 0x00, (byte)inPort, 0x01]));
        public void Dispose() { }
    }

    /// <summary>
    /// Test dispatcher that executes actions immediately on the current thread (no marshalling).
    /// </summary>
    private sealed class TestUiDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();
    }

    [Test]
    public void CounterViewModel_InitializesStatistics()
    {
        var solution = new Solution();
        var settings = new Common.Configuration.AppSettings();
        var vm = new CounterViewModel(new StubZ21(), new TestUiDispatcher(), settings, solution, notificationService: null);
        Assert.That(vm.Statistics, Is.Not.Null);
        Assert.That(vm.Statistics.Count, Is.EqualTo(3));
        Assert.That(vm.Statistics[0].InPort, Is.EqualTo(1));
    }

    [Test]
    public void ResetCounters_ClearsCounts()
    {
        var solution = new Solution();
        var settings = new Common.Configuration.AppSettings();
        var vm = new CounterViewModel(new StubZ21(), new TestUiDispatcher(), settings, solution, notificationService: null);
        vm.Statistics[0].Count = 5;
        vm.Statistics[1].Count = 3;

        // Execute command
        vm.ResetCountersCommand.Execute(null);

        Assert.That(vm.Statistics[0].Count, Is.EqualTo(0));
        Assert.That(vm.Statistics[1].Count, Is.EqualTo(0));
    }

    [Test]
    public void Ctor_InitializesDefaults()
    {
        var solution = new Solution();
        var settings = new Common.Configuration.AppSettings();
        var vm = new CounterViewModel(new StubZ21(), new TestUiDispatcher(), settings, solution, notificationService: null);
        Assert.That(vm.IsNotConnected, Is.True);
    }
}
