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
    public LocomotivesPageViewModel GetLocomotivesPage(ISolutionSession session, IIoService? ioService = null)
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(service => service.Detect(It.IsAny<Moba.Domain.Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var management = new LocomotiveManagementViewModel(detector.Object, new LocomotiveLibraryService(), projectContext: session);
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new LocomotivesPageViewModel(session, ioService ?? new NullIoService(),
            persistence.Object, new LayoutColumnWidthsViewModel(), management,
            NullLogger<LocomotivesPageViewModel>.Instance, _ => { });
        _pages.Add(page);
        return page;
    }
    public WagonsPageViewModel GetWagonsPage(ISolutionSession session, IIoService? ioService = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new WagonsPageViewModel(session, ioService ?? new NullIoService(),
            persistence.Object, new LayoutColumnWidthsViewModel(),
            NullLogger<WagonsPageViewModel>.Instance, _ => { });
        _pages.Add(page);
        return page;
    }
    public TrainsPageViewModel GetTrainsPage(ISolutionSession session, IConnectionRuntime runtime, IIoService? ioService = null)
    {
        var settings = new AppSettings();
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        var page = new TrainsPageViewModel(session, runtime, persistence.Object, new LayoutColumnWidthsViewModel(),
            GetLocomotivesPage(session, ioService), GetWagonsPage(session, ioService),
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
