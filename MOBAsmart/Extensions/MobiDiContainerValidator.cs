// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Extensions;

using Backend.Interface;

using Microsoft.Extensions.DependencyInjection;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using View;

/// <summary>
/// Validates that critical MOBAsmart services and pages resolve from the DI container at startup.
/// </summary>
public static class MobiDiContainerValidator
{
    /// <summary>
    /// Resolves all critical singletons and transient pages; throws on the first resolution failure.
    /// </summary>
    public static void Validate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        try
        {
            ValidateCoreServices(services);
            ValidatePages(services);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("[MOBAsmart DI] Container validation failed.", ex);
        }
    }

    /// <summary>
    /// Resolves critical singleton services without constructing XAML pages.
    /// </summary>
    public static void ValidateCoreServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.GetRequiredService<MobiStartupService>();
        _ = services.GetRequiredService<MauiViewModel>();
        _ = services.GetRequiredService<TrainControlViewModel>();
        _ = services.GetRequiredService<RemotePairingViewModel>();
        _ = services.GetRequiredService<RemoteRuntimeBridge>();
        _ = services.GetRequiredService<IMobaRuntime>();
        _ = services.GetRequiredService<IRuntimeCommandGateway>();
    }

    private static void ValidatePages(IServiceProvider services)
    {
        _ = services.GetRequiredService<SplashPage>();
        _ = services.GetRequiredService<AppTabHostPage>();
        _ = services.GetRequiredService<CounterPage>();
        _ = services.GetRequiredService<SignalBoxPage>();
        _ = services.GetRequiredService<EnginePage>();
        _ = services.GetRequiredService<ControlPage>();
        _ = services.GetRequiredService<PairingPage>();
        _ = services.GetRequiredService<AppShell>();
    }
}