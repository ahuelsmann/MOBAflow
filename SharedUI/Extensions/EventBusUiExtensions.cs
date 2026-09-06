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
    /// Must be called after AddUiDispatcher().
    /// </summary>
    public static IServiceCollection AddEventBusWithUiDispatch(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<EventBus>();
        services.AddSingleton<UiThreadEventBusDecorator>(sp =>
            new UiThreadEventBusDecorator(
                sp.GetRequiredService<EventBus>(),
                sp.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IEventBus>(sp =>
        {
            var uiEventBus = sp.GetRequiredService<UiThreadEventBusDecorator>();
            var recordingSession = sp.GetService<IRecordingSessionService>();
            var mapperRegistry = sp.GetService<RecordingEventMapperRegistry>();
            return recordingSession is null || mapperRegistry is null
                ? uiEventBus
                : new RecordingEventBusDecorator(
                    uiEventBus,
                    recordingSession,
                    mapperRegistry,
                    sp.GetRequiredService<ILogger<RecordingEventBusDecorator>>());
        });

        return services;
    }
}