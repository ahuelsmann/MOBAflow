// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Moba.SharedUI.Service;

/// <summary>
/// MAUI/Android implementation of INotificationService.
/// Plays the default system notification sound.
/// </summary>
public class NotificationService : INotificationService
{
    public void PlayNotificationSound()
    {
        try
        {
#if ANDROID
            var notification = Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Notification);
            var ringtone = Android.Media.RingtoneManager.GetRingtone(Android.App.Application.Context, notification);
            ringtone?.Play();
            System.Diagnostics.Debug.WriteLine("🔔 Notification sound played");
#else
            System.Diagnostics.Debug.WriteLine("⚠️ Notification sound not implemented for this platform");
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to play notification sound: {ex.Message}");
        }
    }
}
