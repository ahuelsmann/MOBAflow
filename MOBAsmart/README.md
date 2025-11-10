# MOBAsmart - Z21 Feedback Monitor

> **Teil von [MOBAflow](../README.md)** - Android-App für Echtzeit-Gleis-Feedback-Überwachung

Für eine Gesamtübersicht siehe: **[MOBAflow Haupt-README](../README.md)**

---

## Überblick

MOBAsmart ist eine .NET MAUI Android-App zur **direkten Überwachung** von Gleis-Feedback-Ereignissen (InPorts) einer Z21 Digital-Zentrale. Die App zeigt die Anzahl der Zugdurchfahrten pro Gleis in Echtzeit an.

**NEU**: Die App kommuniziert jetzt **direkt mit der Z21** ohne FeedbackApi!

## Architektur

```
┌─────────────────┐         ┌──────────────┐
│ MOBAsmart       │   UDP   │ Z21 Digital  │
│ (Android MAUI)  │◄───────►│ Station      │
│                 │  21105  │              │
└─────────────────┘         └──────────────┘
```

### Komponenten

- **Z21FeedbackService**: Direkte UDP-Verbindung zur Z21 (Port 21105)
- **FeedbackStatisticsManager**: Thread-sichere Zählung der Feedback-Ereignisse
- **MainPage**: UI mit ListView für Statistiken pro InPort

## Unterschied zu WinUI

| Feature | WinUI | MOBAsmart |
|---------|-------|-----------|
| Backend-Integration | ✅ Voll | ✅ Gleicher Z21-Client |
| Automation (JourneyManager) | ✅ | ❌ (nicht benötigt) |
| UI | TreeView, PropertyGrid | ListView (Statistiken) |
| Hintergrund-Betrieb | ✅ | ⚠️ Nur Vordergrund |

## Konfiguration

### appsettings.json

```json
{
  "Z21IpAddress": "192.168.0.111"
}
```

**Wichtig**: Passe die IP-Adresse an deine Z21-Station an!

### IP-Adresse der Z21 herausfinden

1. **Z21 App (Roco)**:
   - App öffnen → Einstellungen → "Z21 IP" anzeigen

2. **Router-Admin-Panel**:
   - Router-Webinterface öffnen
   - DHCP-Leases anzeigen
   - Gerät "Z21" suchen

3. **Netzwerk-Scanner** (z.B. Fing App für Android)

## Android-Permissions

Die App benötigt folgende Permissions (bereits konfiguriert):

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
<uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
```

## Z21-Protokoll

Die App nutzt das Z21 LAN-Protokoll (Version 1.13):

- **Port**: 21105 (UDP)
- **Ping-Intervall**: 60 Sekunden (verhindert Timeout)
- **Broadcast-Flags**: 0xFFFFFFFF (alle Events)
- **Feedback-Format**: 0x0F-00-80-00 (R-BUS Feedback)

### Wichtige Z21-Befehle

| Befehl | Hex | Beschreibung |
|--------|-----|--------------|
| Handshake | 04 00 85 00 | Initiale Verbindung |
| Set Broadcast Flags | 08 00 50 00 FF FF FF FF | Alle Events empfangen |
| Ping (Keep-Alive) | 04 00 1A 00 | Alle 60s gesendet |
| Feedback Event | 0F 00 80 00 | InPort-Ereignis |

## Android Hintergrund-Einschränkungen

⚠️ **Wichtig**: Die App funktioniert zuverlässig nur im **Vordergrund**!

Android 16+ schränkt Hintergrund-Aktivitäten stark ein:
- Nach 5-10 Minuten im Hintergrund → Doze Mode
- UDP-Sockets werden geschlossen
- Keine Garantie für Event-Empfang

### Warum die Roco Z21 App funktioniert

Die offizielle Roco Z21 App nutzt:
1. **Foreground Service** mit Notification
2. **Wake Lock** zur Verhinderung von Doze Mode
3. Regelmäßige Pings alle 60 Sekunden

## Verwendung

### Erstmalige Einrichtung

1. **appsettings.json bearbeiten**:
   ```json
   {
     "Z21IpAddress": "192.168.0.111"  // Deine Z21-IP hier eintragen
   }
   ```

2. **App deployen**:
   ```bash
   dotnet build MOBAsmart/MOBAsmart.csproj
   ```

### Normale Nutzung

1. **App öffnen**
   - App startet und zeigt: "Disconnected (192.168.0.111)"

2. **Verbinden**
   - "Connect"-Button drücken
   - Warten auf "Connected to Z21" (grün)
   - Bei Fehler: Retry-Option wird angeboten

3. **Feedback beobachten**
   - Liste zeigt InPort-Nummern
   - Zähler erhöht sich bei jedem Feedback-Event
   - Letzte Uhrzeit wird angezeigt

4. **Trennen**
   - "Disconnect"-Button drücken

## Troubleshooting

### "Connection Failed"

**Mögliche Ursachen:**
- ❌ Z21 nicht eingeschaltet
- ❌ Falsche IP-Adresse in `appsettings.json`
- ❌ Android-Gerät nicht im gleichen WLAN wie Z21
- ❌ Firewall blockiert UDP Port 21105

**Lösung:**
1. Z21 IP-Adresse im Netzwerk prüfen
2. `appsettings.json` anpassen
3. App neu starten
4. Prüfen: PC und Android im gleichen WLAN

**Test vom PC**:
```powershell
# Test UDP-Verbindung zur Z21
Test-NetConnection -ComputerName 192.168.0.111 -Port 21105
```

### "No Feedback Events"

**Mögliche Ursachen:**
- Kein Zug fährt über Feedback-Stellen
- Broadcast Flags nicht korrekt gesetzt
- Z21 Verbindung verloren

**Lösung:**
1. Trennen und neu verbinden
2. Prüfen ob Z21 Status "Connected" ist (grün)
3. Test-Feedback durch Zugdurchfahrt auslösen

### "App verliert Verbindung im Hintergrund"

**Erklärung:**
Das ist **normales Android-Verhalten**! Die App ist für **Vordergrund-Nutzung** konzipiert.

**Workaround:**
- App im Vordergrund lassen
- Bildschirm-Timeout verlängern in Android-Einstellungen
- **Für Hintergrund-Betrieb**: FeedbackApi auf PC/Server nutzen

## Entwicklung

### Build

```bash
dotnet build MOBAsmart/MOBAsmart.csproj
```

### Deploy auf Android-Gerät

```bash
dotnet publish MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release
```

### Deploy auf Android-Emulator

**Wichtig**: Im Emulator ist die Z21 nicht direkt erreichbar!

Für Entwicklung im Emulator:
1. FeedbackApi auf PC laufen lassen
2. Z21 mit PC verbinden
3. FeedbackApi als Bridge nutzen

### Debugging

Die App schreibt ausführliche Debug-Logs:

```csharp
System.Diagnostics.Debug.WriteLine("📥 Feedback received for InPort {inPort}");
```

**Logs ansehen**:
- Visual Studio: **Output → Debug**
- Wichtige Meldungen:
  - 🔌 "Connecting to Z21..."
  - ✅ "Connected to Z21..."
  - 📥 "Feedback received for InPort X"
  - ❌ "Z21 connection failed..."

## Architektur-Entscheidungen

### Warum kein FeedbackApi mehr?

**Vorher**: 
```
MOBAsmart → HTTP/SignalR → FeedbackApi → UDP → Z21
```

**Jetzt**: 
```
MOBAsmart → UDP → Z21 (direkt)
```

**Vorteile:**
- ✅ Keine Server-Abhängigkeit
- ✅ Geringere Latenz (~10-20ms statt ~50-100ms)
- ✅ Einfachere Architektur
- ✅ Gleicher Code wie WinUI (Backend.Z21)
- ✅ Unabhängig von Netzwerkinfrastruktur

**Nachteile:**
- ❌ Kein zuverlässiger Hintergrund-Betrieb
- ❌ Nur eine App kann gleichzeitig mit Z21 verbunden sein
- ❌ Keine Persistierung der Statistiken

### Backend-Projekt-Referenz

MOBAsmart nutzt **Backend.csproj** als Projektabhängigkeit:

```xml
<ProjectReference Include="..\Backend\Backend.csproj" />
```

**Vorteile:**
- ✅ Shared Code mit WinUI
- ✅ Gleiche Z21-Klasse (Backend.Z21)
- ✅ Wartbarkeit (eine Implementierung, mehrere Clients)
- ✅ Konsistentes Verhalten

**Klassen aus Backend:**
- `Z21`: UDP-Client mit Ping
- `FeedbackResult`: Parser für Z21-Feedback-Messages

## Vergleich: MOBAsmart vs. WinUI

### Gemeinsamkeiten
- ✅ Nutzen beide `Backend.Z21`
- ✅ Gleiche Z21-Protokoll-Implementierung
- ✅ Gleiche Ping-Logik (60s Keep-Alive)
- ✅ Gleiche Feedback-Verarbeitung

### Unterschiede
| Aspekt | WinUI | MOBAsmart |
|--------|-------|-----------|
| Ziel-Plattform | Windows 10+ | Android 16+ |
| UI-Framework | WinUI 3 | .NET MAUI |
| Hauptfunktion | Workflow-Management | Feedback-Monitoring |
| Automatisierung | JourneyManager | - |
| Hintergrund | Ja (Windows Service) | Nein (Foreground) |
| Mehrere Z21 | Ja | Nein |

## Zukünftige Erweiterungen

Mögliche Features:
- [ ] Export der Statistiken (CSV, JSON)
- [ ] Konfigurierbare InPort-Namen
- [ ] Grafische Anzeige (Diagramme, Charts)
- [ ] Mehrere Z21-Stationen gleichzeitig
- [ ] Benachrichtigungen bei bestimmten Ereignissen
- [ ] Persistierung (SQLite)
- [ ] Foreground Service (echter Hintergrund-Betrieb)

## Multi-Project Setup (für Entwickler)

Falls du **FeedbackApi zusätzlich** nutzen möchtest (Hybrid-Ansatz):

### Visual Studio 2022 Setup

1. **Multi-Project Startup konfigurieren**:
   - Solution → Eigenschaften
   - Mehrere Startprojekte
   - ✅ FeedbackApi (Start)
   - ✅ MOBAsmart (Start)

2. **Netzwerk-Konfiguration**:
   - FeedbackApi: Port 5001 (HTTP/TCP)
   - Z21: Port 21105 (UDP)
   - PC IP: z.B. 192.168.0.22

3. **Firewall öffnen**:
   ```powershell
   New-NetFirewallRule -DisplayName "FeedbackApi" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow
   ```

**Hinweis**: FeedbackApi wird für MOBAsmart **nicht mehr benötigt**, kann aber parallel für andere Clients laufen.

## Lizenz

Teil des MOBAflow-Projekts.

## Support

Bei Fragen oder Problemen:
- GitHub Issues im MOBAflow-Repository
- Debug-Logs in Visual Studio Output-Fenster prüfen

---

## Schnellstart

```bash
# 1. appsettings.json mit Z21-IP anpassen
# 2. F5 in Visual Studio drücken
# 3. App auf Android deployen lassen
# 4. In MOBAsmart: "Connect" drücken
# 5. Feedback-Updates in Echtzeit beobachten
```

Viel Erfolg! 🚀
