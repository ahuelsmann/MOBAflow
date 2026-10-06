// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>
/// Signal-box editor changes reach the runtime through the project refresh and the command gateway.
/// </summary>
[TestFixture]
internal sealed class MainWindowViewModelSignalBoxTests
{
    private static readonly string[] UpdateThenAspect = ["update", "aspect"];

    [Test]
    public async Task ApplySignalBoxElementChange_WithPersistence_UpdatesRuntimeSignalBoxBeforeSendingAspect()
    {
        var project = new Project();
        var signal = new SbSignal { SignalAspect = Enum.GetValues<SignalAspect>()[^1] };
        var calls = new List<string>();
        var runtime = CreateRuntime();
        runtime.Setup(value => value.UpdateSignalBoxAsync(project, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("update"))
            .Returns(Task.CompletedTask);
        var gateway = new Mock<IRuntimeCommandGateway>();
        gateway.Setup(value => value.SetSignalAspectAsync(signal.Id, signal.SignalAspect, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("aspect"))
            .Returns(Task.CompletedTask);
        var viewModel = CreateViewModel(project, runtime, gateway);
        runtime.Invocations.Clear();

        await viewModel.ApplySignalBoxElementChangeAsync(signal, requiresPersistence: true, requiresSignalCommand: true)
            .ConfigureAwait(false);

        Assert.That(calls, Is.EqualTo(UpdateThenAspect));
        runtime.Verify(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never,
            "A signal-box change must not re-activate the project.");
    }

    [Test]
    public async Task ApplySignalBoxElementChange_AspectOnly_SendsAspectWithoutUpdatingRuntime()
    {
        var project = new Project();
        var signal = new SbSignal { SignalAspect = Enum.GetValues<SignalAspect>()[0] };
        var runtime = CreateRuntime();
        var gateway = new Mock<IRuntimeCommandGateway>();
        var viewModel = CreateViewModel(project, runtime, gateway);
        runtime.Invocations.Clear();

        await viewModel.ApplySignalBoxElementChangeAsync(signal, requiresPersistence: false, requiresSignalCommand: true)
            .ConfigureAwait(false);

        gateway.Verify(value => value.SetSignalAspectAsync(signal.Id, signal.SignalAspect, It.IsAny<CancellationToken>()), Times.Once);
        runtime.Verify(value => value.UpdateSignalBoxAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ApplySignalBoxElementChange_AspectWaitsForPendingConfigurationUpdate()
    {
        var project = new Project();
        var signal = new SbSignal { SignalAspect = Enum.GetValues<SignalAspect>()[0] };
        var update = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = CreateRuntime();
        runtime.Setup(value => value.UpdateSignalBoxAsync(project, It.IsAny<CancellationToken>())).Returns(update.Task);
        var gateway = new Mock<IRuntimeCommandGateway>();
        var viewModel = CreateViewModel(project, runtime, gateway);

        var configurationChange = viewModel.ApplySignalBoxElementChangeAsync(signal, requiresPersistence: true, requiresSignalCommand: false);
        var aspectChange = viewModel.ApplySignalBoxElementChangeAsync(signal, requiresPersistence: false, requiresSignalCommand: true);
        gateway.Verify(value => value.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()), Times.Never);

        update.SetResult();
        await Task.WhenAll(configurationChange, aspectChange).ConfigureAwait(false);

        gateway.Verify(value => value.SetSignalAspectAsync(signal.Id, signal.SignalAspect, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IMobaRuntime> CreateRuntime()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        return runtime;
    }

    private static MainWindowViewModel CreateViewModel(
        Project project,
        Mock<IMobaRuntime> runtime,
        Mock<IRuntimeCommandGateway> gateway)
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            runtime.Object,
            runtime.Object,
            gateway.Object,
            new Mock<IEventBus>().Object,
            dispatcher.Object,
            new AppSettings(),
            new Solution { Projects = [project] },
            new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
            NullLogger<MainWindowViewModel>.Instance)
        {
            SelectedProject = new ProjectViewModel(project)
        };
    }
}
