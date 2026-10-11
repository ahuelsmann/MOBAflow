// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;

/// <summary>
/// Creates the MOBApi hub session for mobile ViewModel tests; it shares settings, locator and hub client with the ViewModel.
/// </summary>
internal static class TestHubSessions
{
    public static MobaflowHubSession Create(
        AppSettings settings,
        MobaflowEndpointLocator endpointLocator,
        IPhotoUploadService healthService,
        IRuntimeHubRemoteClient? hubClient = null,
        IRestApiClientRegistration? clientRegistration = null,
        TimeProvider? timeProvider = null) =>
        new(
            settings,
            endpointLocator,
            healthService,
            timeProvider ?? TimeProvider.System,
            NullLogger<MobaflowHubSession>.Instance,
            hubClient,
            clientRegistration);
}
