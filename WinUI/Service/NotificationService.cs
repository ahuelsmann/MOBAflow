// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Moba.SharedUI.Interface;

/// <summary>
/// WinUI implementation of INotificationService.
/// Uses Windows System.Media.SoundPlayer for notification sounds.
/// </summary>
public class NotificationService : INotificationService
{
    public void PlayNotificationSound()
    {
        try
        {
            // Play Windows system notification sound
            System.Media.SystemSounds.Asterisk.Play();
            System.Diagnostics.Debug.WriteLine(" Notification sound played");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Failed to play notification sound: {ex.Message}");
        }
    }
}