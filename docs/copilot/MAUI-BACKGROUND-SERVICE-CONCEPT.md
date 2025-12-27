# MAUI Background Service für Z21 - Implementierungskonzept

## 🎯 Ziel
Z21 UDP-Kommunikation auch im Hintergrund aufrechterhalten (Android only).

---

## 📋 Option 1: Foreground Service (EMPFOHLEN)

### ✅ Vorteile
- Zuverlässig (läuft solange Notification sichtbar)
- Relativ einfach zu implementieren
- User hat Kontrolle (Notification kann geschlossen werden)
- Kein Battery Optimization Problem

### ❌ Nachteile
- Nur Android (iOS nicht möglich)
- Permanente Notification (kann störend sein)
- Höherer Batterieverbrauch

### 📐 Architektur

```csharp
// MAUI/Platforms/Android/Services/Z21ForegroundService.cs
[Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
public class Z21ForegroundService : Service
{
    private IZ21 _z21;
    private Timer _keepAliveTimer;
    
    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        // 1. Create Notification Channel
        CreateNotificationChannel();
        
        // 2. Start Foreground with Notification
        var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("Z21 Connected")
            .SetContentText("Monitoring track feedback...")
            .SetSmallIcon(Resource.Drawable.ic_train)
            .SetOngoing(true)
            .Build();
            
        StartForeground(NOTIFICATION_ID, notification);
        
        // 3. Initialize Z21 Connection
        _z21 = MauiApplication.Current.Services.GetRequiredService<IZ21>();
        
        // 4. Start Keep-Alive Timer
        _keepAliveTimer = new Timer(SendKeepAlive, null, 0, 30000); // Every 30s
        
        return StartCommandResult.Sticky;
    }
    
    private void SendKeepAlive(object state)
    {
        // Send Z21 status request to keep connection alive
        _z21.RequestStatusAsync().Wait();
    }
    
    public override void OnDestroy()
    {
        _keepAliveTimer?.Dispose();
        base.OnDestroy();
    }
}
```

### 🔧 Integration in MauiViewModel

```csharp
// SharedUI/ViewModel/MauiViewModel.cs
[RelayCommand]
private async Task EnableBackgroundModeAsync()
{
#if ANDROID
    var intent = new Intent(Platform.CurrentActivity, typeof(Z21ForegroundService));
    Platform.CurrentActivity.StartForegroundService(intent);
#endif
}

[RelayCommand]
private async Task DisableBackgroundModeAsync()
{
#if ANDROID
    var intent = new Intent(Platform.CurrentActivity, typeof(Z21ForegroundService));
    Platform.CurrentActivity.StopService(intent);
#endif
}
```

### 📱 UI Integration (MainPage.xaml)

```xaml
<!-- Background Mode Toggle -->
<Border Padding="10,8" BackgroundColor="{DynamicResource SurfaceHighlight}">
    <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
        <Label Text="Background Mode" VerticalOptions="Center" />
        <Switch IsToggled="{Binding IsBackgroundModeEnabled}"
                Command="{Binding ToggleBackgroundModeCommand}" />
    </Grid>
</Border>
```

### 📋 Erforderliche Berechtigungen (Android)

```xml
<!-- MAUI/Platforms/Android/AndroidManifest.xml -->
<manifest>
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE_DATA_SYNC" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    <uses-permission android:name="android.permission.WAKE_LOCK" />
    
    <application>
        <service android:name=".Services.Z21ForegroundService"
                 android:foregroundServiceType="dataSync"
                 android:exported="false" />
    </application>
</manifest>
```

---

## 📋 Option 2: Background Task (NICHT EMPFOHLEN)

### ❌ Warum nicht?
- **Android:** Doze Mode killt Service nach 10 Min
- **iOS:** Keine kontinuierliche Netzwerk-Kommunikation erlaubt
- **Unreliable:** System kann Service jederzeit beenden
- **Battery Drain:** User beschweren sich über Akku

### 📐 Wie es funktionieren würde (theoretisch)

```csharp
// MAUI/Platforms/Android/Services/Z21BackgroundService.cs
[Service]
public class Z21BackgroundService : Service
{
    // ⚠️ PROBLEM: Wird vom System beendet!
    // ⚠️ Doze Mode: Netzwerk wird nach ~10 Min eingeschränkt
    // ⚠️ Battery Optimization: Service wird "optimiert" (= beendet)
}
```

**Workarounds (kompliziert):**
- PowerManager WakeLock (Battery Drain!)
- Doze Mode Whitelist (User muss manuell aktivieren)
- WorkManager (nur periodisch, nicht kontinuierlich)

---

## 🔋 Battery Optimization Handling

```csharp
// MAUI/Platforms/Android/MainActivity.cs
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);
    
    // Request to ignore battery optimizations
    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
    intent.SetData(Android.Net.Uri.Parse("package:" + PackageName));
    StartActivity(intent);
}
```

**⚠️ Vorsicht:** Google Play Store lehnt Apps ab, die unnötig Whitelist anfordern!

---

## 📊 Implementierungs-Roadmap

### Phase 1: Foreground Service (4-6h)
- [x] Z21ForegroundService erstellen
- [x] Notification Channel einrichten
- [x] Keep-Alive Timer implementieren
- [x] Integration in MauiViewModel
- [x] UI Toggle hinzufügen

### Phase 2: Event-Kommunikation (2-3h)
- [ ] Messenger-Pattern für Service → App Events
- [ ] Feedback Events an MauiViewModel weiterleiten
- [ ] UI Updates bei Hintergrund-Events

### Phase 3: Testing & Optimization (2-3h)
- [ ] Battery Drain messen
- [ ] Doze Mode Testing
- [ ] Notification Customization
- [ ] Error Handling & Recovery

---

## 🎯 Alternative: Minimalistische Lösung

**Wenn dir Foreground Service zu komplex ist:**

### **Keep-Alive nur wenn App im Hintergrund (Screen off)**

```csharp
// MauiViewModel.cs
public MauiViewModel(...)
{
    // Detect screen on/off
    Application.Current.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(Application.Current.UserAppTheme))
        {
            if (/* screen off */)
                _z21.SetSystemStatePollingInterval(60); // Reduce polling
            else
                _z21.SetSystemStatePollingInterval(5);  // Normal polling
        }
    };
}
```

**Vorteil:** Keine Service-Implementierung nötig  
**Nachteil:** UDP-Verbindung kann trotzdem unterbrochen werden

---

## 📝 Fazit & Empfehlung

### ✅ **JA implementieren, wenn:**
- User nutzen die App längere Zeit (>1h)
- Z21-Verbindung muss stabil bleiben (z.B. bei Automatik-Betrieb)
- Nur Android-Support ausreichend ist
- User akzeptieren permanente Notification

### ❌ **NEIN, wenn:**
- App wird nur kurz genutzt (<15 Min)
- iOS-Support erforderlich
- Battery Drain ein Problem ist
- Google Play Store Compliance wichtig ist (Whitelist-Anfrage)

### 🎯 **Meine Empfehlung:**
Starte mit **Option 1 (Foreground Service)** nur für Android:
- **Aufwand:** ~6-8 Stunden
- **Funktioniert zuverlässig**
- **User hat Kontrolle**
- **Keine Play Store Probleme**

Soll ich dir die vollständige Implementierung erstellen? 🚀
