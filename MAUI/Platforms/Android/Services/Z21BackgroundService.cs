// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using Moba.Smart.Platforms.Android; // ← Add MainActivity reference

namespace Moba.MAUI.Platforms.Android.Services;

/// <summary>
/// Android Foreground Service to keep Z21 UDP connection alive in background.
/// Shows persistent notification while running.
/// </summary>
[global::Android.App.Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public class Z21BackgroundService : global::Android.App.Service
{
    private const int NOTIFICATION_ID = 1001;
    private const string CHANNEL_ID = "mobasmart_z21_channel";
    private const string CHANNEL_NAME = "Z21 Connection";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "STOP_SERVICE")
        {
            StopForeground(true);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var title = intent?.GetStringExtra("title") ?? "MOBAsmart Active";
        var message = intent?.GetStringExtra("message") ?? "Z21 connection maintained";

        CreateNotificationChannel();
        var notification = BuildNotification(title, message);
        StartForeground(NOTIFICATION_ID, notification);

        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Low)
            {
                Description = "Keeps Z21 connection active in background"
            };

            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private Notification BuildNotification(string title, string message)
    {
        var intent = new Intent(this, typeof(Moba.Smart.Platforms.Android.MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop);
        
        var pendingIntent = PendingIntent.GetActivity(
            this, 
            0, 
            intent, 
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var stopIntent = new Intent(this, typeof(Z21BackgroundService));
        stopIntent.SetAction("STOP_SERVICE");
        var stopPendingIntent = PendingIntent.GetService(
            this, 
            1, 
            stopIntent, 
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuInfoDetails)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Stop", stopPendingIntent)
            .SetPriority(NotificationCompat.PriorityLow);

        return builder.Build();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopForeground(true);
    }
}
#endif
