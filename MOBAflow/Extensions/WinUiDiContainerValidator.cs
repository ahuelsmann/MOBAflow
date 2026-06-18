// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Extensions;

using Backend.Interface;

using Common.Events;

using Microsoft.Extensions.DependencyInjection;

using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Validates that critical MOBAflow services resolve from the DI container at startup.
/// </summary>
public static class WinUiDiContainerValidator
{
    /// <summary>
    /// Resolves all critical singletons; throws on the first resolution failure.
    /// Does not construct XAML pages or MainWindow.
    /// </summary>
    public static void Validate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        try
        {
            ValidateCoreServices(services);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("[MOBAflow DI] Container validation failed.", ex);
        }
    }

    /// <summary>
    /// Resolves critical singleton services without constructing XAML pages or MainWindow.
    /// </summary>
    public static void ValidateCoreServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.GetRequiredService<IMobaRuntime>();
        _ = services.GetRequiredService<IEventBus>();
        _ = services.GetRequiredService<ISettingsService>();
        _ = services.GetRequiredService<IIoService>();
        _ = services.GetRequiredService<IRuntimeCommandGateway>();
        _ = services.GetRequiredService<MainWindowViewModel>();
        _ = services.GetRequiredService<TrainControlViewModel>();
    }
}
