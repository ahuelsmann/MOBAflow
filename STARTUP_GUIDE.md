# Multi-Project Startup Setup für Visual Studio 2022

## ✅ Automatisches Startup konfigurieren

### Methode 1: Visual Studio UI (Empfohlen - Einfach)

1. **Schließen Sie alle geöffneten Dateien** in Visual Studio (wichtig!)

2. **Solution Explorer** → Rechtsklick auf **"Moba"** (Solution-Knoten)

3. Wählen Sie: **"Startprojekte festlegen..."** oder **"Set Startup Projects..."**

4. Im Dialog:
   ```
   ⚪ Einzelnes Startprojekt
   ⚫ Mehrere Startprojekte  ← AUSWÄHLEN
   ⚪ Aktuelles Projekt
   ```

5. Konfigurieren Sie die Projekte:
   ```
   Projekt          | Aktion  | Profil (falls verfügbar)
   -----------------|---------|-------------------------
   FeedbackApi      | Start   | FeedbackApi
   MOBAsmart        | Start   | (Standard)
   Backend          | Keine   |
   Test             | Keine   |
   Sound            | Keine   |
   WinUI            | Keine   |
   SharedUI         | Keine   |
   ```

6. **Wichtig**: Prüfen Sie die Reihenfolge!
   - **FeedbackApi** sollte ÜBER **MOBAsmart** stehen
   - Falls nicht: Nutzen Sie die **Pfeiltasten** zum Sortieren

7. **OK** klicken

8. **Testen Sie es**:
   - Drücken Sie **F5**
   - Output-Fenster → "Debug" sollte zeigen:
     ```
     FeedbackApi: Now listening on: http://192.168.0.22:5000
     MOBAsmart: Deployment started...
     ```

---

### Methode 2: Skript-basiert (Parallel-Start)

Falls die UI-Methode nicht funktioniert, verwenden Sie die Skripte:

#### Windows-Benutzer:

1. **Doppelklick auf**: `StartFeedbackApi.bat`
   - Ein Terminal öffnet sich mit der FeedbackApi
   - **Lassen Sie dieses Fenster offen!**

2. **Visual Studio**: Drücken Sie **F5** um MOBAsmart zu starten

#### PowerShell-Benutzer:

```powershell
# Terminal 1: FeedbackApi starten
.\StartFeedbackApi.ps1

# Terminal 2 oder Visual Studio: MOBAsmart starten
# F5 in Visual Studio
```

---

### Methode 3: Visual Studio Solution File direkt bearbeiten (Fortgeschritten)

**Nachdem Sie Visual Studio geschlossen haben:**

1. Schließen Sie **Visual Studio 2022 komplett**

2. Öffnen Sie `Moba.slnx` in einem Text-Editor (z.B. Notepad, VS Code)

3. Fügen Sie vor dem schließenden `</Solution>` Tag hinzu:

```xml
  <Properties Name="Visual Studio">
    <Property Name="StartupProjects" Value="FeedbackApi/FeedbackApi.csproj;MOBAsmart/MOBAsmart.csproj" />
  </Properties>
```

4. Speichern und schließen

5. Öffnen Sie Visual Studio wieder

---

## 🔍 Verifizierung

### Ist die FeedbackApi gestartet?

**Methode 1: Output-Fenster**
- Visual Studio → **View** → **Output**
- Wählen Sie: **"Debug"** im Dropdown
- Suchen Sie nach: `Now listening on: http://192.168.0.22:5000`

**Methode 2: Browser**
```
http://localhost:5000
```
Sollte eine Antwort zurückgeben (404 oder Swagger-UI)

**Methode 3: PowerShell**
```powershell
Test-NetConnection -ComputerName 192.168.0.22 -Port 5000
```
Sollte: `TcpTestSucceeded : True` zeigen

**Methode 4: Curl/Invoke-WebRequest**
```powershell
Invoke-WebRequest -Uri http://192.168.0.22:5000 -Method GET
```

---

## 🐛 Troubleshooting

### Problem: Nur MOBAsmart startet, FeedbackApi nicht

**Lösung A**: Startup-Projekte neu setzen (siehe Methode 1 oben)

**Lösung B**: FeedbackApi manuell in 2. Instanz starten
- Visual Studio → **File** → **New** → **Window**
- Neue Instanz öffnet sich
- Setzen Sie **FeedbackApi** als Startprojekt
- Beide Instanzen: **F5** drücken

**Lösung C**: Skript verwenden (siehe Methode 2)

### Problem: "Port 5000 already in use"

```powershell
# Port 5000 Prozess finden
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess

# Prozess beenden (VORSICHT!)
Stop-Process -Id <PID>
```

Oder:
```cmd
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

### Problem: MOBAsmart kann nicht mit FeedbackApi verbinden

**Checklist**:
1. ✅ FeedbackApi läuft? (siehe Verifizierung oben)
2. ✅ Firewall erlaubt Port 5000? (siehe README.md)
3. ✅ IP-Adresse korrekt? (`ipconfig` vs. `appsettings.json`)
4. ✅ PC und Android-Gerät im selben WLAN?

**Firewall schnell öffnen**:
```powershell
# Als Administrator ausführen
New-NetFirewallRule -DisplayName "FeedbackApi Port 5000" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

---

## 📊 Debug-Workflow

### Empfohlener Workflow:

1. **Pre-Check**:
```powershell
# IP prüfen
ipconfig | findstr IPv4

# Port 5000 frei?
Test-NetConnection -ComputerName localhost -Port 5000
```

2. **Start (Methode 1 - UI)**:
   - F5 in Visual Studio
   - Beide Projekte starten automatisch

3. **Start (Methode 2 - Manuell)**:
   - Terminal: `.\StartFeedbackApi.bat`
   - Visual Studio: F5

4. **Verifizierung**:
   - Output-Fenster: "Now listening on..."
   - MOBAsmart App: "Connected (http://192.168.0.22:5000)" ✅

5. **Debugging**:
   - Breakpoints in beiden Projekten funktionieren
   - SignalR-Traffic im Output-Fenster sichtbar

---

## 🎯 Schnellstart

**Ich will einfach nur loslegen:**

1. ✅ Firewall-Regel erstellt? → `README.md`
2. ✅ IP korrekt? → `appsettings.json` = `ipconfig`
3. ✅ Multi-Startup eingerichtet? → **Methode 1** oben
4. ✅ **F5** drücken
5. ✅ In App auf **"Connect"** drücken
6. ✅ Fertig! 🎉

Bei Problemen: Siehe **Troubleshooting** oben oder die ausführliche `MOBAsmart/README.md`.
