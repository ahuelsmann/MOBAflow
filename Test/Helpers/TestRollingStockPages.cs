// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

internal sealed partial class TestRollingStockPages : IDisposable
{
    private readonly List<IDisposable> _pages = [];

    // The test fixture owns this collection; returned pages stay alive until its teardown.
    public LocomotivesPageViewModel GetLocomotivesPage(
        ISolutionSession session, ISolutionIoService? files = null, ILocomotiveTrainRuntimeUsageReader? usageReader = null)
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(service => service.Detect(It.IsAny<Moba.Domain.Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var management = new LocomotiveManagementViewModel(detector.Object, new LocomotiveLibraryService(), projectContext: session);
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var usage = CreateUsage(session, usageReader);
        _pages.Add(usage);
        var page = new LocomotivesPageViewModel(session, TestFileServices.Photos(files),
            persistence.Object, new LayoutColumnWidthsViewModel(), new LocomotivePageAnalysis(management, usage, CreateControl()),
            NullLogger<LocomotivesPageViewModel>.Instance, _ => { });
        _pages.Add(page);
        return page;
    }
    private RuntimeLocomotiveControl CreateControl()
    {
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        snapshots.SetupGet(value => value.Snapshots).Returns([]);
        var control = new RuntimeLocomotiveControl(snapshots.Object, new Mock<IUiDispatcher>().Object);
        _pages.Add(control);
        return control;
    }

    internal static LocomotiveTrainUsageViewModel CreateUsage(
        ISolutionSession session, ILocomotiveTrainRuntimeUsageReader? usageReader = null)
    {
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable));
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(value => value.InvokeOnUiAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());
        return new LocomotiveTrainUsageViewModel(session, new LocomotiveTrainReferenceAnalyzer(), usageReader ?? reader.Object,
            new Mock<IProjectRuntimeSnapshots>().Object, dispatcher.Object,
            NullLogger<LocomotiveTrainUsageViewModel>.Instance);
    }

    public WagonsPageViewModel GetWagonsPage(ISolutionSession session, ISolutionIoService? files = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new WagonsPageViewModel(session, TestFileServices.Photos(files),
            persistence.Object, new LayoutColumnWidthsViewModel(),
            NullLogger<WagonsPageViewModel>.Instance, _ => { });
        _pages.Add(page);
        return page;
    }
    public TrainsPageViewModel GetTrainsPage(
        ISolutionSession session, IConnectionRuntime runtime, ISolutionIoService? files = null,
        ILocomotiveTrainRuntimeUsageReader? usageReader = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new TrainsPageViewModel(session, runtime, persistence.Object, new LayoutColumnWidthsViewModel(),
            GetLocomotivesPage(session, files, usageReader), GetWagonsPage(session, files),
            NullLogger<TrainsPageViewModel>.Instance);
        _pages.Add(page);
        return page;
    }
    public StationsPageViewModel GetStationsPage(ISolutionSession session, IConnectionRuntime runtime,
        ActionExecutionContext executionContext, IEventBus eventBus, IDialogService? dialogs = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var library = new WorkflowLibraryViewModel(session, dialogs, new WorkflowValidator(),
            runtimeServices: new WorkflowLibraryRuntimeServices { ExecutionContext = executionContext, EventBus = eventBus });
        _pages.Add(library);
        var page = new StationsPageViewModel(session, runtime, persistence.Object,
            new LayoutColumnWidthsViewModel(), library, NullLogger<StationsPageViewModel>.Instance);
        _pages.Add(page);
        return page;
    }
    public JourneysPageViewModel GetJourneysPage(ISolutionSession session,
        IRuntimeCommandGateway gateway, IUiDispatcher dispatcher)
    {
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(new AppSettings());
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new JourneysPageViewModel(session, gateway, dispatcher, Mock.Of<ICityService>(),
            persistence.Object, new LayoutColumnWidthsViewModel(), NullLogger<JourneysPageViewModel>.Instance);
        _pages.Add(page);
        return page;
    }

    public void Dispose()
    {
        foreach (var page in _pages)
            page.Dispose();
        _pages.Clear();
    }
}
