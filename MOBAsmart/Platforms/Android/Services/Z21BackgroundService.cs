// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if ANDROID

namespace Moba.MAUI.Platforms.Android.Services;

using AndroidX.Core.App;

using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.OS;

/// <summary>
/// Android Foreground Service to keep MOBAsmart network connections alive in background.
/// Shows persistent notification while running.
/// </summary>
[Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
public class Z21BackgroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "mobasmart_connection_channel";
    private const string ChannelName = "MOBAsmart Connection";

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
        var message = intent?.GetStringExtra("message") ?? "Connection maintained";

        CreateNotificationChannel();
        var notification = BuildNotification(title, message);
        StartForeground(NotificationId, notification);

        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
            {
                Description = "Keeps MOBAsmart connections active in background"
            };

            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private Notification? BuildNotification(string title, string message)
    {
        var intent = new Intent(this, typeof(MainActivity));
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

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(title)
            ?.SetContentText(message)
            ?.SetSmallIcon(global::Android.Resource.Drawable.IcMenuInfoDetails)
            ?.SetOngoing(true)
            ?.SetContentIntent(pendingIntent)
            ?.AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Stop", stopPendingIntent)
            ?.SetPriority(NotificationCompat.PriorityLow);

        return builder?.Build();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopForeground(true);
    }
}
#endif