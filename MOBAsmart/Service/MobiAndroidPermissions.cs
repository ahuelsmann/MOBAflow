// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if ANDROID

namespace Moba.MAUI.Service;

using global::Android.Content;
using global::Android.OS;
using global::Android.Provider;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

/// <summary>
/// Requests Android runtime permissions required for foreground-service notifications.
/// </summary>
internal static class MobiAndroidPermissions
{
    private const string BatteryOptimizationPromptShownKey = "battery_opt_prompt_shown";

    public static async Task EnsureForegroundServicePermissionsAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>().ConfigureAwait(false);
            }
        }

        await TryRequestBatteryOptimizationExemptionOnceAsync().ConfigureAwait(false);
    }

    private static Task TryRequestBatteryOptimizationExemptionOnceAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return Task.CompletedTask;
        }

        if (Preferences.Get(BatteryOptimizationPromptShownKey, false))
        {
            return Task.CompletedTask;
        }

        var context = Platform.AppContext;
        var powerManager = context.GetSystemService(Context.PowerService) as PowerManager;
        if (powerManager == null || powerManager.IsIgnoringBatteryOptimizations(context.PackageName))
        {
            return Task.CompletedTask;
        }

        Preferences.Set(BatteryOptimizationPromptShownKey, true);

        try
        {
            var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(global::Android.Net.Uri.Parse("package:" + context.PackageName));
            intent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch (Exception)
        {
            // Some OEM builds block the battery-optimization intent.
        }

        return Task.CompletedTask;
    }
}

#endif
