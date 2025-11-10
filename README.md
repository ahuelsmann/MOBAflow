# MOBAflow

> **Modellbahn-Automatisierung**

Eine Anwendung zur Steuerung und Automatisierung von Modelleisenbahnanlagen mit Z21 Digital-Zentrale.

---

## 🎯 Überblick

MOBAflow ist eine ereignisgesteuerte Automatisierungslösung für Modelleisenbahnen. Das System ermöglicht komplexe Workflow-Abläufe, Zugsteuerung mit Bahnhofsdurchsagen und Echtzeit-Feedback-Monitoring - alles über eine direkte UDP-Verbindung zur Z21 Digital-Zentrale.

### Hauptmerkmale

- ✅ **Workflow-Automatisierung**: Ereignisgesteuerte Aktionssequenzen basierend auf Gleis-Feedback
- ✅ **Zugmanagement**: Journey-basierte Steuerung mit Stationen und Plattformen
- ✅ **Audio-Integration**: Bahnhofsdurchsagen und Sound-Effekte
- ✅ **Direkte Z21-Kommunikation**: UDP-basierte Echtzeitsteuerung ohne Middleware
- ✅ **Multi-Platform**: Windows (WinUI) und Android (MAUI)
- ✅ **Echtzeit-Monitoring**: Live-Feedback-Statistiken pro Gleis

---

## 🏗️ Architektur

```
┌─────────────────────────────────────────────────────────────┐
│                      MOBAflow Solution                       │
├─────────────────┬───────────────┬──────────────┬────────────┤
│   WinUI App     │  MOBAsmart    │   Backend    │   Sound    │
│  (Workflows &   │  (Feedback    │  (Core       │  (Audio    │
│   Management)   │   Monitor)    │   Logic)     │   Engine)  │
└────────┬────────┴───────┬───────┴──────┬───────┴─────┬──────┘
         │                │              │             │
         └────────────────┴──────────────┴─────────────┘
                          │
                    ┌─────▼─────┐
                    │    Z21    │ UDP Port 21105
                    │  Digital  │
                    │  Station  │
                    └───────────┘
                          │
                    ══════╪══════  DCC/MM
                      Modelleisenbahn
```

---

## 📦 Projekte

### 1. **Backend** (Core)
Zentrale Geschäftslogik und Steuerungskomponenten.

**Hauptklassen:**
- `Z21`: UDP-Client für direkte Z21-Kommunikation (Port 21105)
- `WorkflowManager`: Führt Workflows basierend auf Feedback-Events aus
- `JourneyManager`: Verwaltet Zugfahrten zwischen Stationen
- `PlatformManager`: Steuert Bahnsteig-spezifische Aktionen
- `FeedbackResult`: Parser für Z21 R-BUS Feedback-Messages

**Manager-System:**
```csharp
// Alle Manager arbeiten unabhängig mit derselben Z21-Instanz
var z21 = new Z21();
await z21.ConnectAsync(IPAddress.Parse("192.168.0.111"));

// Workflow-Automatisierung
var workflowManager = new WorkflowManager(z21, workflows);

// Zug-Management
var journeyManager = new JourneyManager(z21, journeys, context);

// Plattform-Steuerung  
var platformManager = new PlatformManager(z21, platforms);
```
**Neuer Architekturansatz: Trennung von Feedback und Steuerung**
- Feedback-Receiver separiert von Command-Handlern
- Erleichtert zukünftige Protokollanpassungen und Fehlerbehebung

```csharp
// Neues Feedback-System
public class FeedbackService
{
    public event Action<FeedbackData> OnFeedbackReceived;
    
    private async Task ReceiveLoop()
    {
        while (_running)
        {
            var result = await _udpClient.ReceiveAsync();
            var feedback = ParseFeedback(result.Buffer);
            OnFeedbackReceived?.Invoke(feedback);
        }
    }
}

// Verwendung im Workflow
_feedbackService.OnFeedbackReceived += (feedback) =>
{
    // Reagiere auf Feedback, z.B. Trigger für Workflows
};
```

### 2. **WinUI** (Windows Desktop App)
Vollwertige Management-Applikation für Windows 10/11.

**Features:**
- 🎨 TreeView für Solution-/Projekt-Hierarchie
- 📝 PropertyGrid für Entity-Eigenschaften
- 🔌 Z21-Verbindungsmanagement
- ▶️ Workflow-Ausführung und Debugging
- 🧪 Feedback-Simulation für Tests

**Technologie:**
- WinUI 3 / Windows App SDK
- MVVM mit CommunityToolkit
- .NET 10
- 📦 NuGet-Pakete: Microsoft.Extensions.Hosting, Newtonsoft.Json

**Wichtige Klassen:**
- `App`: Starte und konfiguriere die Anwendung
- `MainWindow`: Hauptbenutzeroberfläche
- `SolutionExplorer`: Baumansicht für Lösungen und Projekte
- `PropertiesPanel`: Anzeige und Bearbeitung von Projekteigenschaften
- `WorkflowDesigner`: Visueller Designer für Workflows

```csharp
// Beispiel: WorkflowDesigner.xaml.cs
public partial class WorkflowDesigner : UserControl
{
    public WorkflowDesigner()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Speichere den aktuellen Workflow
        _workflowService.SaveCurrentWorkflow();
    }
}
```

### 3. **MOBAsmart** (Android Feedback Monitor)
Leichtgewichtige Android-App für Live-Feedback-Überwachung.

**Features:**
- 📊 Echtzeit-Zähler pro InPort (Gleisabschnitt)
- 🔗 Direkte Z21 UDP-Verbindung
- 📱 Foreground-Betrieb (Android 16+)
- 🎯 Fokus auf Rundenstatistik

**Verwendung:**
```json
// appsettings.json
{
  "Z21IpAddress": "192.168.0.111"
}
```

**Hinweis:** Die App funktioniert zuverlässig nur im Vordergrund (Android-Hintergrund-Einschränkungen).

**Wichtige Klassen:**
- `MainActivity`: Einstiegspunkt der Anwendung
- `FeedbackReceiver`: Empfang von UDP-Feedback-Nachrichten
- `StatisticsViewModel`: Berechnung und Bereitstellung von Statistiken

```kotlin
// Beispiel: FeedbackReceiver.kt
class FeedbackReceiver(private val z21IpAddress: String) {
    fun startReceiving() {
        // Starte den Empfang von UDP-Nachrichten
    }

    private fun onMessageReceived(message: String) {
        // Verarbeite die empfangene Nachricht
    }
}
```

### 4. **SharedUI**
Gemeinsame UI-Komponenten und ViewModels.

- `MainWindowViewModel`: Zentrale WinUI-Logik
- `TreeViewBuilder`: Solution-Explorer-Funktionalität
- `PropertyViewModel`: Generisches Property-Editing

### 5. **Sound**
Audio-Engine für Bahnhofsdurchsagen und Sound-Effekte.

**Features:**
- 🔊 Text-to-Speech Integration
- 📻 Audio-Datei-Wiedergabe
- 🎛️ Lautstärkeregelung

**Wichtige Klassen:**
- `AudioPlayer`: Abspielen von Audio-Dateien und TTS
- `SoundEffect`: Repräsentation eines Soundeffekts (z.B. als Datei oder TTS)

```csharp
// Beispiel: AudioPlayer.cs
public class AudioPlayer
{
    public void PlaySound(string filePath)
    {
        // Spiele den Sound von der angegebenen Datei ab
    }

    public void PlayTTS(string text)
    {
        // Konvertiere Text in Sprache und spiele ihn ab
    }
}
```

### 6. **Test**
Unit- und Integrationstests.

---

## 🚀 Getting Started

### Voraussetzungen

- **.NET 10 SDK** (oder höher)
- **Visual Studio 2022** (Version 17.13+)
- **Z21 Digital-Zentrale** (Modelleisenbahn GmbH)
- **Windows 10/11** (für WinUI)
- **Android 16+** (für MOBAsmart)

### Installation

1. **Repository klonen:**
   ```bash
   git clone https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
   cd MOBAflow
   ```

2. **Solution öffnen:**
   ```bash
   # Visual Studio
   Moba.slnx

   # Oder via CLI
   dotnet restore
   ```

3. **Z21 IP-Adresse konfigurieren:**
   - WinUI: In der App → Projekt → IP-Adressen-Liste
   - MOBAsmart: `appsettings.json` → `Z21IpAddress`

4. **Build:**
   ```bash
   dotnet build
   ```

5. **Starten:**
   - **WinUI**: F5 in Visual Studio oder `dotnet run --project WinUI`
   - **MOBAsmart**: Deploy auf Android-Gerät

### Erste Schritte mit WinUI

1. App starten
2. Solution laden (z.B. `example-solution.json`)
3. Z21-IP konfigurieren (TreeView → Project → Properties)
4. "Connect to Z21" klicken
5. Workflows ausführen oder Feedback beobachten

### Erste Schritte mit MOBAsmart

1. `appsettings.json` mit Z21-IP bearbeiten
2. App auf Android deployen
3. "Connect"-Button drücken
4. Live-Feedback-Statistiken beobachten

---

## 🎮 Z21 LAN-Protokoll

MOBAflow kommuniziert direkt mit der Z21 über UDP (Port 21105) gemäß **Z21 LAN Protokoll Spezifikation V1.13**.

### Wichtige Befehle

| Hex | Befehl | Beschreibung |
|-----|--------|--------------|
| `04 00 85 00` | HANDSHAKE | Initiale Verbindung |
| `08 00 50 00 FF FF FF FF` | SET_BROADCAST_FLAGS | Alle Events empfangen |
| `04 00 1A 00` | PING | Keep-Alive (alle 60s) |
| `0F 00 80 00` | FEEDBACK_EVENT | R-BUS InPort-Ereignis |

### Implementierung

```csharp
// Backend/Z21.cs
public class Z21 : IDisposable
{
    public event Feedback? Received;

    public async Task ConnectAsync(IPAddress address)
    {
        _client = new UdpClient();
        _client.Connect(address, 21105);
        
        await SendHandshakeAsync();
        await SetBroadcastFlagsAsync();
        
        // Ping-Task (Keep-Alive alle 60s)
        _pingTask = Task.Run(() => SendPingAsync(_cancellationToken));
    }

    public async Task SendCommandAsync(byte[] sendBytes)
    {
        await _client.SendAsync(sendBytes, sendBytes.Length);
    }
}
```

### Ping-Mechanismus

Die Z21 erwartet mindestens alle 60 Sekunden eine Nachricht, sonst wird der Client aus der aktiven Teilnehmerliste entfernt. MOBAflow sendet automatisch Pings (LAN_GET_HWINFO).

---

## 📱 Projekt-Übersicht

### Backend-Projektstruktur

```
Backend/
├── Z21.cs                     # UDP-Client für Z21-Kommunikation
├── FeedbackResult.cs          # Parser für Feedback-Messages
├── Manager/
│   ├── WorkflowManager.cs     # Workflow-Ausführung
│   ├── JourneyManager.cs      # Zugsteuerung
│   ├── PlatformManager.cs     # Bahnsteig-Logik
│   └── FeedbackMonitorManager.cs  # Statistik (optional)
├── Model/
│   ├── Solution.cs            # Lösung/Projekt-Datenmodell
│   ├── Workflow.cs            # Workflow-Definition
│   ├── Journey.cs             # Zug-Fahrt
│   └── Action/                # Action-Implementierungen
└── Hub/
    └── FeedbackHub.cs         # SignalR Hub (veraltet)
```

### WinUI-Projektstruktur

```
WinUI/
├── App.xaml.cs                # Application Entry Point
├── View/
│   └── MainWindow.xaml        # Hauptfenster
└── Service/
    └── IoService.cs           # File I/O (Save/Load Solution)
```

### MOBAsmart-Projektstruktur

```
MOBAsmart/
├── MainPage.xaml              # Haupt-UI
├── MainPage.xaml.cs           # UI-Logik
├── Services/
│   ├── Z21FeedbackService.cs  # Z21-Verbindungsservice
│   ├── FeedbackStatisticsManager.cs  # Statistik-Manager
│   └── FeedbackStatistic.cs   # Datenmodell
└── appsettings.json           # Z21-IP-Konfiguration
```

---

## 🔧 Erweiterte Konfiguration

### Solution-Datei Format

```json
{
  "Projects": [
    {
      "Name": "Meine Anlage",
      "IpAddresses": ["192.168.0.111"],
      "Workflows": [...],
      "Journeys": [...],
      "Trains": [...],
      "Locomotives": [...]
    }
  ]
}
```

### Workflow-Definition

```json
{
  "Name": "Bahnhof Einfahrt",
  "InPort": 5,
  "Actions": [
    {
      "$type": "AnnouncementAction",
      "Text": "Der ICE aus München fährt ein",
      "VoiceName": "Microsoft Hedda Desktop"
    },
    {
      "$type": "CommandAction",
      "Commands": ["0A 00 40 00 ..."]
    }
  ]
}
```

---

## 🧪 Testing & Debugging

### Feedback-Simulation (WinUI)

```csharp
// Simuliert ein Feedback-Event ohne echte Z21-Hardware
_z21.SimulateFeedback(inPort: 5);
```

### Debug-Logs

Alle wichtigen Events werden geloggt:
```csharp
System.Diagnostics.Debug.WriteLine("📥 Feedback received for InPort {inPort}");
```

**Logs ansehen:**
- Visual Studio: **Output → Debug**

---

## 🤝 Entwicklung & Zusammenarbeit

### AI-Assistierte Entwicklung

Dieses Projekt wurde mit Unterstützung moderner KI-Entwicklungstools erstellt:

- 🤖 **GitHub Copilot** (Code-Completion, Refactoring)
- 🧠 **Claude Sonnet 4.5** (Architektur-Design, Code-Review)
- 💡 **GPT-4o** (Dokumentation, Problemlösung)

Die Kombination von traditioneller Softwareentwicklung und KI-gestützten Werkzeugen ermöglichte eine schnellere Iteration und höhere Code-Qualität.

### Mitwirken

Contributions sind willkommen! Bitte beachte:

1. **Fork** das Repository
2. **Branch** erstellen (`feature/mein-feature`)
3. **Commit** mit aussagekräftigen Nachrichten
4. **Pull Request** erstellen

### Code-Konventionen

- ✅ C# Coding Conventions (Microsoft)
- ✅ MVVM-Pattern für UI-Projekte
- ✅ Async/Await für I/O-Operationen
- ✅ Dependency Injection wo sinnvoll
- ✅ XML-Dokumentation für öffentliche APIs

---

## 📋 Bekannte Einschränkungen

### MOBAsmart (Android)
- ⚠️ **Nur Vordergrund-Betrieb**: Android 16+ Doze Mode schließt Hintergrund-UDP-Sockets
- ⚠️ **Ein Client gleichzeitig**: Z21 unterstützt mehrere Clients, aber praktische Limitierung durch UDP

### WinUI
- ⚠️ **Windows-exklusiv**: WinUI 3 läuft nur auf Windows 10/11

---

## 🛠️ Troubleshooting

### "Z21 Connection Failed"

**Ursachen:**
- Z21 nicht eingeschaltet
- Falsche IP-Adresse
- Firewall blockiert Port 21105
- Gerät nicht im gleichen Netzwerk

**Lösung:**
```powershell
# IP-Adresse prüfen
Test-NetConnection -ComputerName 192.168.0.111 -Port 21105

# Firewall-Regel hinzufügen
New-NetFirewallRule -DisplayName "Z21" -Direction Inbound -LocalPort 21105 -Protocol UDP -Action Allow
```

### "No Feedback Events"

**Lösung:**
1. Z21-Verbindung prüfen (grüner Status)
2. Broadcast Flags überprüfen (sollte 0xFFFFFFFF sein)
3. Testfahrt durchführen über Feedback-Stelle

---

## 📚 Ressourcen

- [Z21 LAN Protokoll Spezifikation V1.13](https://www.z21.eu)
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
- [WinUI 3 Documentation](https://learn.microsoft.com/windows/apps/winui/)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)

---

## ⚖️ Lizenz

Dieses Projekt ist unter der **MIT-Lizenz** lizenziert - siehe [LICENSE](LICENSE) für Details.

### Trademark Notice

**Z21** und **Roco** sind eingetragene Marken der Modelleisenbahn GmbH, Plainbachstraße 4, A-5101 Bergheim, Austria. Die Verwendung dieser Markennamen dient ausschließlich der Produktidentifikation und erfolgt ohne werbliche Absicht.

---

## 👤 Autor

**Andreas Huelsmann**

- 📧 Email: [Ihre E-Mail]
- 💼 LinkedIn: [Ihr LinkedIn]
- 🐙 GitHub: [ahuelsmann](https://github.com/ahuelsmann)

---

## 🙏 Danksagungen

- **Modelleisenbahn GmbH** für die Z21 Digital-Zentrale und Protokoll-Dokumentation
- **Microsoft** für .NET, WinUI und MAUI
- **Community** für Feedback und Beiträge

---

## 📅 Versionshistorie

### v2.0.0 (2025-11)
- 🔄 Umstellung auf direkte Z21-Kommunikation
- ❌ FeedbackApi entfernt (nicht mehr benötigt)
- ✅ MOBAsmart mit direktem Z21-Zugriff
- ✅ Verbesserte Stabilität und Latenz

### v1.0.0 (2024)
- 🎉 Initiales Release
- ✅ Workflow-System
- ✅ Journey-Manager
- ✅ FeedbackApi (veraltet)

---

<div align="center">

**Gebaut mit ❤️ für die Modellbahn-Community**

[Report Bug](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems) · [Request Feature](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems) · [Documentation](https://dev.azure.com/ahuelsmann/MOBAflow/_wiki)

</div>