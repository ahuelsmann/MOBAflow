// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;

using Moq;

[TestFixture]
internal sealed class MobaRuntimeServiceVehicleUsageTests
{
    [Test]
    public async Task AuthoritativeZ21State_ProjectsStableVehicleUsageAndStopsOnConnectionLoss()
    {
        var z21Mock = CreateZ21Mock();
        var time = new ManualTimeProvider();
        var checkpointStore = new MemoryCheckpointStore();
        using var runtime = CreateRuntime(z21Mock.Object, time, checkpointStore);
        var locomotive = new Locomotive { DigitalAddress = 3, Usage = new VehicleUsageData() };
        var wagon = new PassengerWagon { Usage = new VehicleUsageData() };
        var train = new Train
        {
            Vehicles =
            [
                new Vehicle { VehicleId = locomotive.Id, VehicleKind = TrainVehicleKind.Locomotive },
                new Vehicle { VehicleId = wagon.Id, VehicleKind = TrainVehicleKind.PassengerWagon }
            ]
        };
        var editorProject = new Project
        {
            Locomotives = [locomotive],
            PassengerWagons = [wagon],
            Trains = [train]
        };

        await runtime.ActivateProjectAsync(editorProject);
        z21Mock.Raise(z21 => z21.OnConnectedChanged += null, true);
        z21Mock.Raise(
            z21 => z21.OnSystemStateChanged += null,
            new SystemState { CentralState = 0x00 });
        z21Mock.Raise(
            z21 => z21.OnLocoInfoChanged += null,
            new LocoInfo { Address = 3, Speed = 18 });

        time.Advance(TimeSpan.FromSeconds(8));
        await runtime.CheckpointUsageAsync();

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Current.ActiveTrainId, Is.EqualTo(train.Id));
            Assert.That(runtime.Current.VehicleUsage[locomotive.Id].TrackedOperatingSeconds, Is.EqualTo(8));
            Assert.That(runtime.Current.VehicleUsage[wagon.Id].TrackedOperatingSeconds, Is.EqualTo(8));
            Assert.That(editorProject.Locomotives.Single().Usage!.TrackedOperatingSeconds, Is.Zero,
                "Runtime checkpointing must not mutate the editor model from a backend timer thread.");
        });

        z21Mock.Raise(z21 => z21.OnConnectionLost += null);
        time.Advance(TimeSpan.FromSeconds(12));
        await runtime.CheckpointUsageAsync();

        Assert.That(runtime.Current.VehicleUsage[locomotive.Id].TrackedOperatingSeconds, Is.EqualTo(8));
        Assert.That(runtime.Current.VehicleUsage[wagon.Id].TrackedOperatingSeconds, Is.EqualTo(8));
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z21 => z21.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z21 => z21.IsConnected).Returns(false);
        z21Mock.Setup(z21 => z21.DisconnectAsync()).Returns(Task.CompletedTask);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(
        IZ21 z21,
        TimeProvider timeProvider,
        IVehicleUsageCheckpointStore checkpointStore)
    {
        var workflowServiceMock = new Mock<IWorkflowService>();
        var loggerMock = new Mock<ILogger<MobaRuntimeService>>();
        return new MobaRuntimeService(
            z21,
            workflowServiceMock.Object,
            new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 }),
            new AppSettings { Z21 = new Z21Settings { CurrentIpAddress = string.Empty } },
            loggerMock.Object,
            vehicleUsageCheckpointStore: checkpointStore,
            timeProvider: timeProvider);
    }

    private sealed class MemoryCheckpointStore : IVehicleUsageCheckpointStore
    {
        private readonly Dictionary<Guid, VehicleUsageCheckpointState> _states = [];

        public VehicleUsageCheckpointState? Load(Guid projectId) => _states.GetValueOrDefault(projectId);

        public void Save(VehicleUsageCheckpointState state) => _states[state.ProjectId] = state;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;
        private DateTimeOffset _utcNow = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed)
        {
            _timestamp += elapsed.Ticks;
            _utcNow += elapsed;
        }
    }
}
