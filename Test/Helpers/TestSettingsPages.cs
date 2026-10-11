// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

internal static class TestSettingsPages
{
    public static SettingsPageViewModel Create(AppSettings settings, IUiDispatcher dispatcher)
    {
        var persistence = new Mock<ISettingsService>();
        persistence.Setup(service => service.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        return new SettingsPageViewModel(settings, persistence.Object, dispatcher,
            NullLogger<SettingsPageViewModel>.Instance);
    }
}
