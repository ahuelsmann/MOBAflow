// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;

using Moq;

/// <summary>
/// Creates the MOBApi endpoint locator for mobile ViewModel tests; it shares the settings with the ViewModel.
/// </summary>
internal static class TestEndpointLocators
{
    public static MobaflowEndpointLocator Create(
        AppSettings settings,
        ISettingsService settingsService,
        IRestDiscoveryService? discovery = null,
        TimeProvider? timeProvider = null) =>
        new(
            settings,
            settingsService,
            discovery ?? new Mock<IRestDiscoveryService>().Object,
            timeProvider ?? TimeProvider.System,
            NullLogger<MobaflowEndpointLocator>.Instance);
}
