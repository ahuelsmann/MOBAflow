// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Extensions;

using Backend.Interface;
using Backend.Service.Recording;

using Common.Events;

using Interface;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Service;

/// <summary>
/// Extensions for registering the EventBus with UI-thread marshalling.
/// For WinUI/MAUI: Call after AddUiDispatcher() so all EventBus handlers run on the UI thread.
/// </summary>
public static class EventBusUiExtensions
{
    /// <summary>
    /// Registers the EventBus so that Publish executes all handlers on the UI thread.
    /// Must be called after AddUiDispatcher(). The registered <see cref="IEventBus"/> records published events,
    /// so the host also registers the backend services (recording session and event mapper registry).
    /// </summary>
    public static IServiceCollection AddEventBusWithUiDispatch(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<EventBus>();
        services.AddSingleton<UiThreadEventBusDecorator>(sp =>
            new UiThreadEventBusDecorator(
                sp.GetRequiredService<EventBus>(),
                sp.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IEventBus>(sp => new RecordingEventBusDecorator(
            sp.GetRequiredService<UiThreadEventBusDecorator>(),
            sp.GetRequiredService<IRecordingSessionService>(),
            sp.GetRequiredService<RecordingEventMapperRegistry>(),
            sp.GetRequiredService<ILogger<RecordingEventBusDecorator>>()));

        return services;
    }
}