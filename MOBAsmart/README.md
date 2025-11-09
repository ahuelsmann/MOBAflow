# MOBAsmart - Setup und Verwendung

## Port-Konfiguration

**Wichtig:** Port 5000 wird bereits von der Z21-UDP-Verbindung verwendet!

| Port | Protokoll | Dienst | Beschreibung |
|------|-----------|--------|--------------|
| **5000** | UDP | Z21 | Digitale Modelleisenbahn-Steuerung |
| **5001** | TCP/HTTP | FeedbackApi | SignalR-Hub für Feedback-Monitoring |

## Multi-Project Startup Konfiguration

### Visual Studio 2022 Setup

1. **Solution öffnen**: `Moba.slnx` in Visual Studio öffnen

2. **Multi-Project Startup konfigurieren**:
   - Rechtsklick auf die Solution im Solution Explorer
   - Wählen Sie **"Set Startup Projects..."** oder **"Configure Startup Projects..."**
   - Wählen Sie **"Multiple startup projects"**
   - Setzen Sie folgende Projekte auf **"Start"**:
     - ✅ **FeedbackApi** - Action: **Start** (Profil: `FeedbackApi`)
     - ✅ **MOBAsmart** - Action: **Start**
   - Reihenfolge (wichtig!):
     1. FeedbackApi (wird zuerst gestartet auf Port **5001**)
     2. MOBAsmart (startet nach FeedbackApi)

3. **FeedbackApi Profil auswählen**:
   - Stellen Sie sicher, dass FeedbackApi das Profil **"FeedbackApi"** verwendet (Port **5001**)

### Netzwerk-Konfiguration

#### Aktuelle Konfiguration
- **PC IP-Adresse**: `192.168.0.22`
- **FeedbackApi Port**: `5001` (HTTP/TCP)
- **Z21 Port**: `5000` (UDP - nicht verwenden für HTTP!)
- **FeedbackApi URL**: `http://192.168.0.22:5001`

#### Für physische Android-Geräte
Die App ist bereits auf `http://192.168.0.22:5001` konfiguriert.

#### Für Android-Emulator
Falls Sie einen Emulator verwenden, ändern Sie in `appsettings.json`:
```json
{
  "ServerUrl": "http://10.0.2.2:5001"
}
```

### Firewall-Konfiguration

**Windows Defender Firewall öffnen für Port 5001:**

```powershell
# PowerShell als Administrator ausführen
New-NetFirewallRule -DisplayName "FeedbackApi - Port 5001" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow
```

Oder manuell:
1. Windows Defender Firewall → Erweiterte Einstellungen
2. Eingehende Regeln → Neue Regel
3. Port → TCP → 5001
4. Verbindung zulassen
5. Alle Profile (Domäne, Privat, Öffentlich)
6. Name: "FeedbackApi - Port 5001"

### Debugging-Ablauf

1. **F5 drücken** oder **"Start Debugging"** klicken
2. Visual Studio startet automatisch:
   - ✅ FeedbackApi läuft auf Port 5001
   - ✅ MOBAsmart wird auf das Android-Gerät deployed
3. In der App:
   - Status zeigt: "Disconnected (http://192.168.0.22:5001)"
   - **"Connect"**-Button drücken
   - Status ändert sich zu: "Connected (http://192.168.0.22:5001)" (grün)

### Fehlerbehandlung

#### Problem: "Server not reachable"

**Lösungen:**
1. Prüfen Sie, ob FeedbackApi läuft:
   - Output-Fenster in Visual Studio → Debug
   - Sollte zeigen: `Now listening on: http://192.168.0.22:5001`

2. Firewall-Test:
   ```powershell
   Test-NetConnection -ComputerName 192.168.0.22 -Port 5001
   ```

3. Browser-Test vom PC:
   - Öffnen: `http://192.168.0.22:5001` oder `http://localhost:5001`
   - Sollte eine Antwort zeigen (404 oder JSON ist OK)

4. IP-Adresse prüfen:
   ```cmd
   ipconfig
   ```
   - Vergleichen Sie mit der IP in `appsettings.json`

#### Problem: "Port 5001 already in use"

```powershell
# Port 5001 Prozess finden
Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue | ForEach-Object {
    Get-Process -Id $_.OwningProcess
}

# Prozess beenden (VORSICHT!)
Get-Process -Name "dotnet" | Stop-Process -Force
```

#### Problem: "Connection lost" während der Nutzung

Die App versucht **automatisch zu reconnecten**:
- Bis zu 5 Versuche
- Exponentieller Backoff: 0s, 2s, 5s, 10s
- Status zeigt: "Reconnecting... (Attempt X/5)"

Manueller Reconnect:
- **"Connect"**-Button drücken

### Features der App

#### Robustheit
- ✅ Automatische Reconnection bei Verbindungsverlust
- ✅ Exponentieller Backoff (verhindert Server-Überlastung)
- ✅ Connection State Events (UI wird automatisch aktualisiert)
- ✅ Detaillierte Fehlermeldungen mit Lösungsvorschlägen
- ✅ Manuelle Reconnect-Option
- ✅ Sortierte Feedback-Liste (nach InPort)

#### UI-Elemente
- **Status-Label**: Zeigt Verbindungsstatus + Server-URL
- **Connect/Disconnect-Button**: Manuell verbinden/trennen
- **Feedback-Liste**: Echtzeit-Updates der Feedback-Statistiken
- **Fehlerdialoge**: Hilfreiche Meldungen bei Problemen

### Entwickler-Tipps

#### Debug-Logs anzeigen
Alle wichtigen Events werden geloggt:
```
System.Diagnostics.Debug.WriteLine(...)
```

In Visual Studio:
- **Output-Fenster** → **Debug**
- Suchen Sie nach: "Connected", "Reconnecting", "Error"

#### appsettings.json ändern
```json
{
  "ServerUrl": "http://192.168.0.22:5001",
  "ServerUrlEmulator": "http://10.0.2.2:5001"
}
```

Nach Änderungen:
1. Projekt neu builden
2. App erneut deployen

#### Neue IP-Adresse verwenden

1. Neue IP ermitteln:
   ```cmd
   ipconfig
   ```

2. Dateien aktualisieren:
   - `MOBAsmart/appsettings.json`
   - `FeedbackApi/Properties/launchSettings.json`

3. Firewall-Regel anpassen (falls nötig)

### Bekannte Einschränkungen

- SignalR funktioniert nicht ohne laufenden Server
- Android-Emulator benötigt `10.0.2.2` statt `localhost`
- Physische Geräte brauchen PC und Gerät im selben Netzwerk
- **Port 5000 ist für Z21-UDP-Verbindung reserviert!**

### Support

Bei Problemen prüfen Sie:
1. ✅ FeedbackApi läuft auf Port **5001**
2. ✅ Firewall erlaubt Port **5001**
3. ✅ IP-Adresse ist korrekt
4. ✅ PC und Android-Gerät im selben WLAN
5. ✅ Port 5000 ist NICHT für FeedbackApi verwendet (Z21!)

---

## Schnellstart

```bash
# 1. Solution öffnen
# 2. Multi-Project Startup konfigurieren (siehe oben)
# 3. F5 drücken
# 4. In MOBAsmart App: "Connect" drücken
# 5. Feedback-Updates in Echtzeit sehen
```

Viel Erfolg! 🚀
