// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Extensions;

using Common.Events;
using Interface;
using Microsoft.Extensions.DependencyInjection;
using Service;

/// <summary>
/// Erweiterungen zur Registrierung des EventBus mit UI-Thread-Marshalling.
/// Für WinUI/MAUI: Nach AddUiDispatcher() aufrufen, damit alle EventBus-Handler auf dem UI-Thread laufen.
/// </summary>
public static class EventBusUiExtensions
{
    /// <summary>
    /// Registriert den EventBus so, dass Publish alle Handler auf dem UI-Thread ausführt.
    /// Muss nach AddUiDispatcher() aufgerufen werden.
    /// </summary>
    public static IServiceCollection AddEventBusWithUiDispatch(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<EventBus>();
        services.AddSingleton<IEventBus>(sp =>
            new UiThreadEventBusDecorator(
                sp.GetRequiredService<EventBus>(),
                sp.GetRequiredService<IUiDispatcher>()));

        return services;
    }
}
