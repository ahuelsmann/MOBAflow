// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>Creates the layout persistence boundary over a settings service, as the hosts register it.</summary>
internal static class TestLayouts
{
    /// <summary>Creates the boundary over the given settings service with fresh column widths.</summary>
    public static LayoutPersistenceService Create(
        ISettingsService settingsService,
        ILogger<LayoutPersistenceService>? logger = null) =>
        new(settingsService, new LayoutColumnWidthsViewModel(), logger ?? NullLogger<LayoutPersistenceService>.Instance);

    /// <summary>Creates the boundary over the given settings with a settings service whose writes succeed.</summary>
    public static LayoutPersistenceService Create(AppSettings settings)
    {
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.GetSettings()).Returns(settings);
        persistence.Setup(service => service.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        return Create(persistence.Object);
    }
}
