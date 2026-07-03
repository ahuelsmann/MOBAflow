// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Characterization tests for high-risk SharedUI ViewModel behavior.
/// </summary>
[TestFixture]
internal class ViewModelCharacterizationTests
{
    [Test]
    public void TrainControlViewModel_Constructor_ProjectsConnectionStateFromCurrentSnapshot()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public async Task TrainControlViewModel_SpeedChange_SendsDriveCommand()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        viewModel.Speed = 12;
        await Task.Delay(100);

        mobaRuntimeMock.Verify(client => client.SetLocomotiveDriveAsync(3, 12, true, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public void TrainControlViewModel_SpeedIncrease_WhenDisconnected_IsRejected()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        viewModel.Speed = 12;

        Assert.That(viewModel.Speed, Is.Zero);
        Assert.That(viewModel.IsSpeedControlEnabled, Is.False);
        mobaRuntimeMock.Verify(client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionAsync_UpdatesStateAndCallsRuntime()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        await viewModel.ToggleFunctionAsync(1);

        Assert.That(viewModel.Functions[1].IsOn, Is.True);
        mobaRuntimeMock.Verify(client => client.SetLocomotiveFunctionAsync(3, 1, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionCommand_UpdatesUiWhenDisconnected()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        Assert.That(viewModel.ToggleFunctionCommand.CanExecute(0), Is.True);
        await viewModel.ToggleFunctionCommand.ExecuteAsync(0);

        Assert.That(viewModel.Functions[0].IsOn, Is.True);
    }

    [Test]
    public async Task TrainControlViewModel_EditFunctionAppearanceCommand_AppliesPickerResult()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive
                {
                    Name = "BR 218",
                    DigitalAddress = 3,
                    FunctionSymbols = ["none"],
                    FunctionColors = ["#FFD700"]
                }
            ]
        };
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        var picker = new FakeFunctionAppearancePicker(new FunctionAppearancePickerResult(
            IsConfirmed: true,
            IsSelectionCleared: false,
            Glyph: "headlight.png",
            ColorHex: "#E81123"));
        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            settingsServiceMock.Object,
            mainViewModel,
            eventBus: CreateEventBus(),
            functionAppearancePicker: picker);

        await viewModel.EditFunctionAppearanceCommand.ExecuteAsync(0);

        Assert.That(picker.LastRequest?.InitialColorHex, Is.EqualTo("#FFD700"));
        Assert.That(viewModel.Functions[0].IconAsset, Is.EqualTo("headlight.png"));
        Assert.That(viewModel.Functions[0].BacklightColorHex, Is.EqualTo("#E81123"));
        Assert.That(project.Locomotives[0].FunctionSymbols![0], Is.EqualTo("headlight.png"));
        Assert.That(project.Locomotives[0].FunctionColors![0], Is.EqualTo("#E81123"));
    }

    [Test]
    public async Task TrainControlViewModel_EditFunctionAppearanceCommand_ClearsAppearanceWhenPickerClearsSelection()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive
                {
                    Name = "BR 218",
                    DigitalAddress = 3,
                    FunctionSymbols = ["headlight.png"],
                    FunctionColors = ["#E81123"]
                }
            ]
        };
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        var picker = new FakeFunctionAppearancePicker(new FunctionAppearancePickerResult(
            IsConfirmed: true,
            IsSelectionCleared: true,
            Glyph: null,
            ColorHex: null));
        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            settingsServiceMock.Object,
            mainViewModel,
            eventBus: CreateEventBus(),
            functionAppearancePicker: picker);

        await viewModel.EditFunctionAppearanceCommand.ExecuteAsync(0);

        Assert.That(project.Locomotives[0].FunctionSymbols![0], Is.EqualTo("none"));
        Assert.That(project.Locomotives[0].FunctionColors![0], Is.EqualTo("none"));
    }

    [Test]
    public async Task TrainControlViewModel_EditFunctionAppearanceCommand_NoPicker_IsNoOp()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());
        var iconBefore = viewModel.Functions[0].IconAsset;

        await viewModel.EditFunctionAppearanceCommand.ExecuteAsync(0);

        Assert.That(viewModel.Functions[0].IconAsset, Is.EqualTo(iconBefore));
    }

    [Test]
    public async Task TrainControlViewModel_SuppressedSnapshotFunctionState_DoesNotClearManualFunctionToggles()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 0,
                    IsForward = true,
                    Functions = 0
                }
            }
        });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: eventBus);

        await viewModel.ToggleFunctionAsync(2);
        Assert.That(viewModel.Functions[2].IsOn, Is.True);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 10,
                    IsForward = true,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.Functions[2].IsOn, Is.True, "Manual function state must survive snapshots while suppressed");
    }

    [Test]
    public void TrainControlViewModel_HybridSnapshots_DoNotApplyDirectionFromSnapshots()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot
        {
            IsConnected = false,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 0,
                    IsForward = true,
                    Functions = 0
                }
            }
        });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        viewModel.IsForward = false;

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = false,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 10,
                    IsForward = true,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.IsForward, Is.False, "Manual direction must survive snapshots on MOBAsmart without connection");
        Assert.That(viewModel.Speed, Is.Zero, "Manual speed must survive snapshots on MOBAsmart without connection");
    }

    [Test]
    public void TrainControlViewModel_LocalSnapshots_ApplyDirectionWhenNotHybrid()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 0,
                    IsForward = true,
                    Functions = 0
                }
            }
        });

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus);

        viewModel.IsForward = false;

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 5,
                    IsForward = true,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.IsForward, Is.True, "MOBAflow desktop should reflect Z21 snapshot direction");
        Assert.That(viewModel.Speed, Is.EqualTo(5));
    }

    [Test]
    public void TrainControlViewModel_HybridRemoteSnapshots_ApplyFunctionBitsFromLocalZ21WhenConnected()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 0,
                    IsForward = true,
                    Functions = 0b1010
                }
            }
        }));

        Assert.That(viewModel.Functions[1].IsOn, Is.True, "Local Z21 snapshot should drive F1 during MOBAflow session");
        Assert.That(viewModel.Functions[3].IsOn, Is.True, "Local Z21 snapshot should drive F3 during MOBAflow session");
        Assert.That(viewModel.Functions[0].IsOn, Is.False);
    }

    [Test]
    public void TrainControlViewModel_HybridRemoteSnapshots_IgnoreSlimRemoteLocomotiveStatesWhenConnected()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 12,
                    IsForward = false,
                    Functions = 0
                }
            }
        }));

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 80,
                    IsForward = true,
                    Functions = 0b1111
                }
            }
        }));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Speed, Is.EqualTo(12), "Slim remote snapshots must not override local Z21 drive state");
            Assert.That(viewModel.IsForward, Is.False);
            Assert.That(viewModel.Functions[0].IsOn, Is.False, "Remote locomotive states are omitted from MOBAsmart broadcasts");
        });
    }

    [Test]
    public void TrainControlViewModel_ProjectLocomotiveSwitch_PreservesFunctionStatesPerAddress()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [110] = new LocomotiveRuntimeSnapshot { Address = 110, Functions = 0b1 },
                [211] = new LocomotiveRuntimeSnapshot { Address = 211, Functions = 0b100 }
            }
        });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            settingsServiceMock.Object,
            eventBus: eventBus);

        viewModel.LocoAddress = 110;
        Assert.That(viewModel.Functions[0].IsOn, Is.True);

        viewModel.LocoAddress = 211;
        Assert.That(viewModel.Functions[0].IsOn, Is.False);
        Assert.That(viewModel.Functions[2].IsOn, Is.True);

        viewModel.LocoAddress = 110;
        Assert.That(viewModel.Functions[0].IsOn, Is.True, "Function state for loco 110 should be restored after switching back");
        Assert.That(viewModel.Functions[2].IsOn, Is.False);
    }

    [Test]
    public async Task TrainControlViewModel_HybridRemoteSnapshot_UpdatesOtherFunctionsDuringLocalGrace()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        coordinator.SetLocalZ21Connected(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        viewModel.LocoAddress = 211;
        await viewModel.ToggleFunctionAsync(0);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [211] = new LocomotiveRuntimeSnapshot
                {
                    Address = 211,
                    Functions = 0b100
                }
            }
        }));

        Assert.That(viewModel.Functions[0].IsOn, Is.True, "Locally toggled F0 stays protected during grace");
        Assert.That(viewModel.Functions[2].IsOn, Is.True, "Local Z21 F2 update must apply while another function is in grace");
    }

    [Test]
    public void TrainControlViewModel_HybridRemoteSnapshots_DoNotOverwriteManualStatusMessage()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 42,
                    IsForward = false,
                    Functions = 0
                }
            }
        }));

        viewModel.StatusMessage = "F0: ON";

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 42,
                    IsForward = false,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.StatusMessage, Is.EqualTo("F0: ON"));
        Assert.That(viewModel.Speed, Is.EqualTo(42), "Remote drive state should sync when local Z21 is offline");
        Assert.That(viewModel.IsForward, Is.False, "Remote direction should sync when local Z21 is offline");
    }

    [Test]
    public void TrainControlViewModel_HybridRemoteSnapshots_ApplyDriveStateFromMobaflowWhenZ21Offline()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        viewModel.Speed = 0;
        viewModel.IsForward = true;

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 18,
                    IsForward = false,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.Speed, Is.EqualTo(18));
        Assert.That(viewModel.IsForward, Is.False);
    }

    [Test]
    public async Task TrainControlViewModel_HybridRemoteSnapshots_PreserveLocalDriveDuringGrace()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        viewModel.Speed = 5;
        viewModel.IsForward = true;
        await viewModel.SetSpeedCommand.ExecuteAsync(null);

        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            IsConnected = true,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new LocomotiveRuntimeSnapshot
                {
                    Address = 3,
                    Speed = 40,
                    IsForward = false,
                    Functions = 0
                }
            }
        }));

        Assert.That(viewModel.Speed, Is.EqualTo(5), "Local throttle change must win during grace period");
        Assert.That(viewModel.IsForward, Is.True, "Local direction must win during grace period");
    }

    [Test]
    public async Task TrainControlViewModel_CanExecuteFunctions_WhenLocalZ21CoordinatorReadyWithoutSnapshotConnection()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var coordinator = new MobileRuntimeCoordinator(mobaRuntimeMock.Object, new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetLocalZ21Connected(true);

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });

        eventBus.Publish(new RuntimeCommandAvailabilityChangedEvent());

        await viewModel.ToggleFunctionAsync(2);

        Assert.That(viewModel.Functions[2].IsOn, Is.True);
        mobaRuntimeMock.Verify(
            client => client.SetLocomotiveFunctionAsync(3, 2, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionAsync_CancelsInFlightAllFunctionsOff()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        mobaRuntimeMock
            .Setup(client => client.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async (int _, CancellationToken token) =>
            {
                await Task.Delay(500, token);
            });

        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: eventBus);

        var allOffTask = viewModel.TurnOffAllFunctionsAsync();
        await viewModel.ToggleFunctionAsync(3);
        await allOffTask;

        Assert.That(viewModel.Functions[3].IsOn, Is.True);
        mobaRuntimeMock.Verify(
            client => client.SetLocomotiveFunctionAsync(3, 3, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionAsync_WinsOverQueuedDecoderAllOff()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        mobaRuntimeMock
            .Setup(client => client.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async (int _, CancellationToken token) =>
            {
                await Task.Delay(500, token);
            });

        var viewModel = new TrainControlViewModel(
            mobaRuntimeMock.Object,
            CreateSettingsServiceMock().Object,
            eventBus: CreateEventBus());

        var decoderAllOffTask = viewModel.TurnOffAllFunctionsAsync(resetUi: false);
        await viewModel.ToggleFunctionAsync(0);
        await decoderAllOffTask;

        Assert.That(viewModel.Functions[0].IsOn, Is.True);
        mobaRuntimeMock.Verify(
            client => client.SetLocomotiveFunctionAsync(3, 0, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TrainControlViewModel_TurnOffAllFunctionsAsync_ResetsUiAndCallsRuntime()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        await viewModel.ToggleFunctionAsync(0);
        await viewModel.ToggleFunctionAsync(5);
        Assert.That(viewModel.Functions[0].IsOn, Is.True);

        await viewModel.TurnOffAllFunctionsAsync();

        Assert.That(viewModel.Functions.All(f => !f.IsOn), Is.True, "All function buttons should be off");
        mobaRuntimeMock.Verify(client => client.SetAllLocomotiveFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TrainControlViewModel_Timetable_ShowsVirtualEventWithSignalAspect()
    {
        var workflowId = Guid.NewGuid();
        var journey = new Journey
        {
            FirstPos = 1,
            Stations =
            [
                new Station { Name = "Bielefeld", Arrival = new DateTime(2026, 5, 25, 12, 0, 0), Departure = new DateTime(2026, 5, 25, 12, 1, 0) },
                new Station { Name = "Signal Event", IsVirtual = true, WorkflowId = workflowId },
                new Station { Name = "Herford" }
            ]
        };
        var workflow = new Workflow
        {
            Id = workflowId,
            Actions =
            [
                new WorkflowAction
                {
                    Number = 1,
                    Type = ActionType.SelectSignalAspect,
                    SelectSignalAspect = new SelectSignalAspectActionPayload { SignalAspect = SignalAspect.Ks1 }
                }
            ]
        };
        var project = new Project { Journeys = [journey], Workflows = [workflow] };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        mainViewModel.SelectedJourney = mainViewModel.SelectedProject.Journeys.Single();
        mainViewModel.SelectedJourney.UpdateFromSessionState(new JourneySessionState { JourneyId = journey.Id, CurrentPos = 1 });

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, mainViewModel, eventBus: CreateEventBus());

        Assert.That(viewModel.CurrentStationName, Is.EqualTo("Signal Event"));
        Assert.That(viewModel.CurrentStationIsEvent, Is.True);
        Assert.That(viewModel.CurrentStationArrival, Is.EqualTo("\u2014"));
        Assert.That(viewModel.CurrentStationDeparture, Is.EqualTo("\u2014"));
        Assert.That(viewModel.CurrentStationTrack, Is.EqualTo("Signal: Ks1"));
        Assert.That(viewModel.CurrentStationShowsExitDirection, Is.False);
        Assert.That(viewModel.PreviousStationName, Is.EqualTo("Bielefeld"));
        Assert.That(viewModel.NextStationName, Is.EqualTo("Herford"));
    }

    [Test]
    public void TrainControlViewModel_Timetable_ResolvesSignalAspectFromSolutionWhenSelectedProjectDiffers()
    {
        var workflowId = Guid.NewGuid();
        var journey = new Journey
        {
            FirstPos = 0,
            Stations =
            [
                new Station { Name = "Signal Event", IsVirtual = true, WorkflowId = workflowId }
            ]
        };
        var workflow = new Workflow
        {
            Id = workflowId,
            Actions =
            [
                new WorkflowAction
                {
                    Number = 1,
                    Type = ActionType.SelectSignalAspect,
                    SelectSignalAspect = new SelectSignalAspectActionPayload { SignalAspect = SignalAspect.Ks2 }
                }
            ]
        };
        var project = new Project { Name = "Runtime Project", Journeys = [journey], Workflows = [workflow] };
        var otherProject = new Project { Name = "Other Project" };
        var solution = new Solution { Projects = [project, otherProject] };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings(), solution);
        mainViewModel.SelectedProject = mainViewModel.SolutionViewModel?.Projects.Single(viewModel => viewModel.Model == otherProject);
        mainViewModel.SelectedJourney = mainViewModel.SolutionViewModel?.Projects.Single(viewModel => viewModel.Model == project).Journeys.Single();

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, mainViewModel, eventBus: CreateEventBus());

        Assert.That(viewModel.CurrentStationTrack, Is.EqualTo("Signal: Ks2"));
    }

    [Test]
    public void MainWindowViewModel_AssignWorkflowToStation_UsesExplicitTargetStation()
    {
        var firstStation = new Station { Name = "A" };
        var secondStation = new Station { Name = "B" };
        var workflow = new Workflow { Name = "Target workflow" };
        var project = new Project
        {
            Journeys =
            [
                new Journey { Stations = [firstStation, secondStation] }
            ],
            Workflows = [workflow]
        };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        mainViewModel.SelectedJourney = mainViewModel.SelectedProject.Journeys.Single();
        mainViewModel.SelectedStation = mainViewModel.SelectedJourney.Stations[0];
        var targetStation = mainViewModel.SelectedJourney.Stations[1];
        var workflowViewModel = mainViewModel.SelectedProject.Workflows.Single();

        mainViewModel.AssignWorkflowToStation(workflowViewModel, targetStation);

        Assert.That(firstStation.WorkflowId, Is.Null);
        Assert.That(secondStation.WorkflowId, Is.EqualTo(workflow.Id));
        Assert.That(mainViewModel.SelectedStation, Is.SameAs(targetStation));
    }

    [Test]
    public void MainWindowViewModel_AutoStartWebApp_SetterPersistsSettings()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());

        var settings = new AppSettings();
        var settingsServiceMock = CreateSettingsServiceMock(settings);

        var viewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, settings);
        viewModel.AutoStartWebApp = false;

        Assert.That(settings.Application.AutoStartWebApp, Is.False);
        settingsServiceMock.Verify(service => service.SaveSettingsAsync(settings), Times.AtLeastOnce);
    }

    private static Mock<IMobaRuntime> CreateMobaRuntimeMock(MobaRuntimeSnapshot snapshot)
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(snapshot);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mobaRuntimeMock;
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock(AppSettings? settings = null)
    {
        var currentSettings = settings ?? new AppSettings();
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(currentSettings);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.ResetToDefaultsAsync()).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }

    private static IEventBus CreateEventBus() => new Mock<IEventBus>().Object;

    private static MainWindowViewModel CreateMainWindowViewModel(
        IMobaRuntime mobaRuntime,
        ISettingsService settingsService,
        AppSettings settings,
        Solution? solution = null)
    {
        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntime,
            eventBusMock.Object,
            uiDispatcherMock.Object,
            settings,
            solution ?? new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object,
            settingsService: settingsService);
    }

    private sealed class FakeFunctionAppearancePicker(FunctionAppearancePickerResult? result) : IFunctionAppearancePicker
    {
        public FunctionAppearancePickerRequest? LastRequest { get; private set; }

        public Task<FunctionAppearancePickerResult?> PickAsync(
            FunctionAppearancePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }
}