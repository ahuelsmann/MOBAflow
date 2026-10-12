// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

internal sealed partial class TestLocomotivePages : IDisposable
{
    private readonly List<LocomotivesPageViewModel> _pages = [];

    // The test fixture owns this collection; returned pages stay alive until its teardown.
    public LocomotivesPageViewModel GetPage(ISolutionSession session, IIoService? ioService = null)
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
    public void Dispose()
    {
        foreach (var page in _pages)
            page.Dispose();
        _pages.Clear();
    }
}
