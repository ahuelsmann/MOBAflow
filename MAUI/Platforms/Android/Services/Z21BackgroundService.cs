// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if ANDROID



// ← Add MainActivity reference

namespace Moba.MAUI.Platforms.Android.Services;

using AndroidX.Core.App;
using global::Android;
using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.OS;

/// <summary>
/// Android Foreground Service to keep Z21 UDP connection alive in background.
/// Shows persistent notification while running.
/// </summary>
[Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
public class Z21BackgroundService : Service
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

        var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle(title)
            ?.SetContentText(message)
            ?.SetSmallIcon(Resource.Drawable.IcMenuInfoDetails)
            ?.SetOngoing(true)
            ?.SetContentIntent(pendingIntent)
            ?.AddAction(Resource.Drawable.IcMenuCloseClearCancel, "Stop", stopPendingIntent)
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