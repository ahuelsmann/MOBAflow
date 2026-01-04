# MOBAflow REST-API & WebApp Refactoring TODO

**Erstellt:** 2025-02-03  
**Status:** Phase A - In Arbeit  
**Priorität:** Hoch

---

## 📋 **Phase A: Firewall-Fix & Verbesserungen (Diese Session)** 🔥

### ✅ **Erledigt:**

1. ✅ **FirewallHelper erstellt** (`WinUI/Service/FirewallHelper.cs`)
   - Automatische Firewall-Regel-Erstellung für UDP Port 21106
   - Automatische Firewall-Regel-Erstellung für HTTP Port 5000
   - Prüfung, ob Regeln bereits existieren
   - Admin-Rechte-Elevation (UAC-Prompt)
   - Cleanup-Methode zum Entfernen der Regeln

2. ✅ **WebApp Auto-Start verbessert** (`WinUI/App.xaml.cs`)
   - Firewall-Check beim Start integriert
   - Bessere Suchpfade für WebApp.dll (4 Locations)
   - Detailliertes Logging mit Diagnose
   - WebApp-Output-Redirection für Debugging
   - Benutzerfreundliche Hinweise bei Fehlern

3. ✅ **Discovery-Fehlermeldungen verbessert** (`MAUI/Service/RestApiDiscoveryService.cs`)
   - Timeout-Details (3000ms)
   - Troubleshooting-Tipps (4 Punkte)
   - Socket-Error-Diagnostik
   - Freundliche Emoji-basierte Logs

4. ✅ **MAUI Build-Fehler behoben**
   - Problem: MainPage.xaml hatte 31.000 Zeilen (Duplikation durch edit_file Bug)
   - Lösung: Git checkout HEAD, Datei wiederhergestellt
   - Status: Build erfolgreich ✅

5. ✅ **TrainsPage Properties-Spalte korrigiert** (`WinUI/View/TrainsPage.xaml`)
   - Problem: Properties-Spalte hatte MaxWidth="650", war zu schmal
   - Lösung: `Width="*" MinWidth="520"` - füllt verfügbaren Platz
   - Vorher: Teile der Wagon Properties waren rechts am Fensterrand abgeschnitten
   - Nachher: Wagon Properties vollständig sichtbar

### ⏳ **Noch zu tun:**

6. ⏳ **MAUI Akzentfarben komplett korrigieren** 🎨
   - **Lap Time:** `LastLapTimeFormatted` (Zeile 461) → `RailwayWarning` (orange) 🟠
   - **Counter Badges:** Background → `RailwayAccent` (blau), TextColor → `White` 🔵
     - Tracks Counter (Zeile 234-247)
     - Target Counter (Zeile 285-298)
   - **Counter-Labels:** OBEN statt LINKS (VerticalStackLayout statt Grid)
     - Tracks Layout (Zeile 211-260)
     - Target Layout (Zeile 262-311)
   - ✅ **Switches bereits korrekt:** Connection (grün), Track Power (orange)
   - **Aktion:** Siehe detaillierte Anleitung in `docs/MAUI-AKZENTFARBEN-KORREKTUR.md`
   - **Warum nicht erledigt:** edit_file funktioniert nicht bei langen Dateien (Bug)

7. ⏳ **Firewall-Helper testen**
   - Erste Ausführung: Admin-Rechte anfordern
   - Prüfen ob Regeln korrekt erstellt wurden
   - Testen ob MAUI den Server findet
   - **Aktion:** WinUI als Admin starten, Debug-Logs prüfen

8. ⏳ **Windows Firewall Regeln verifizieren**
   - Manuelle Prüfung: `netsh advfirewall firewall show rule name="MOBAflow WebApp UDP Discovery"`
   - Manuelle Prüfung: `netsh advfirewall firewall show rule name="MOBAflow WebApp REST-API"`
   - **Aktion:** PowerShell als Admin öffnen, Befehle ausführen

9. ⏳ **End-to-End Test**
   - WinUI starten (WebApp sollte auto-starten)
   - MAUI auf Android-Emulator starten
   - Discovery-Broadcast senden
   - Verbindung zum REST-API testen
   - **Erwartetes Ergebnis:** MAUI findet WebApp automatisch

### 📝 **Offene Fragen:**

- ❓ Soll der FirewallHelper auch für andere Ports konfigurierbar sein?
- ❓ Sollen die Firewall-Regeln beim App-Uninstall automatisch entfernt werden?
- ❓ Brauchen wir eine UI-Benachrichtigung, wenn Firewall-Regeln nicht erstellt werden konnten?

---

## 🏗️ **Phase B: SharedUI.Web Klassenbibliothek (Neue Session)** 🎯

### **Ziel:**
REST-API in eine wiederverwendbare Klassenbibliothek auslagern, die sowohl von WinUI (in-process) als auch von WebApp (standalone) genutzt werden kann.

### **Architektur-Änderungen:**

```
Solution/
├── SharedUI.Web/              # ✨ NEU: REST-API Klassenbibliothek
│   ├── Controllers/           # Photo, System, Discovery Controllers
│   ├── Services/              # PhotoStorageService, UdpDiscoveryService
│   ├── Extensions/            # DI-Extensions (AddWebServices)
│   └── WebAppConfiguration.cs # Kestrel/ASP.NET Core Setup
│
├── WinUI/                     # Hostet SharedUI.Web in-process
│   └── App.xaml.cs            # IHost statt Process.Start
│
└── WebApp/                    # Standalone Blazor Server (optional)
    └── Program.cs             # Nutzt SharedUI.Web
```

### **Schritt-für-Schritt Plan:**

#### **1. Neue Klassenbibliothek erstellen**
```bash
cd C:\Repo\ahuelsmann\MOBAflow
dotnet new classlib -n SharedUI.Web -f net10.0
dotnet sln add SharedUI.Web/SharedUI.Web.csproj
```

**Packages:**
```xml
<PackageReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.Extensions.Hosting" />
```

#### **2. Controller aus WebApp migrieren**
- [ ] `WebApp/Controllers/PhotoController.cs` → `SharedUI.Web/Controllers/PhotoController.cs`
- [ ] Weitere REST-API Controller identifizieren und migrieren

#### **3. Services auslagern**
- [ ] `WebApp/Service/PhotoStorageService.cs` → `SharedUI.Web/Services/PhotoStorageService.cs`
- [ ] `WebApp/Service/UdpDiscoveryService.cs` → `SharedUI.Web/Services/UdpDiscoveryService.cs`

#### **4. DI-Extension erstellen**
```csharp
// SharedUI.Web/Extensions/WebServicesExtensions.cs
public static class WebServicesExtensions
{
    public static IServiceCollection AddMobaWebServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<PhotoStorageService>();
        services.AddHostedService<UdpDiscoveryService>();
        return services;
    }
}
```

#### **5. WinUI in-process Hosting implementieren**
```csharp
// WinUI/App.xaml.cs
private IHost? _webAppHost;

private async Task StartWebAppIfEnabledAsync()
{
    var builder = WebApplication.CreateBuilder();
    
    builder.WebHost.UseKestrel();
    builder.WebHost.UseUrls("http://localhost:5000");
    
    builder.Services.AddMobaWebServices(); // ← Aus SharedUI.Web
    
    var app = builder.Build();
    app.MapControllers();
    
    _webAppHost = app;
    await app.StartAsync();
}
```

#### **6. WebApp anpassen (optional standalone)**
```csharp
// WebApp/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMobaWebServices(); // ← Aus SharedUI.Web

var app = builder.Build();
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
```

### **Vorteile dieser Lösung:**

✅ **Kein separater Prozess** - WebApp läuft in WinUI  
✅ **Shared Memory** - Gleicher DI-Container, gemeinsame Services  
✅ **Keine Firewall-Probleme** - Nur localhost binding  
✅ **Einfaches Debugging** - Ein Prozess, ein Debugger  
✅ **Wiederverwendbar** - Gleiche Library für WinUI + WebApp standalone  
✅ **Saubere Architektur** - REST-API klar separiert  

### **Nachteile / Trade-offs:**

⚠️ **WinUI-Abhängigkeit** - WebApp kann nicht mehr standalone ohne WinUI-Prozess  
⚠️ **Speicher-Overhead** - Kestrel + ASP.NET Core im WinUI-Prozess  
⚠️ **Breaking Change** - Große Architektur-Änderung, viel Testing nötig  

### **Aufwandsschätzung:**

- **SharedUI.Web erstellen:** ~30 Minuten
- **Controller migrieren:** ~20 Minuten
- **WinUI in-process Hosting:** ~40 Minuten
- **WebApp anpassen:** ~15 Minuten
- **Testing & Debugging:** ~60 Minuten
- **GESAMT:** ~2-3 Stunden

### **Risiken:**

🔴 **HOCH:** Kestrel-Lifetime-Management im WinUI-Prozess  
🟡 **MITTEL:** DI-Container-Konflikte zwischen WinUI und WebApp  
🟢 **NIEDRIG:** Port-Binding-Konflikte (kann konfiguriert werden)  

### **Entscheidungskriterien:**

**JETZT MACHEN, wenn:**
- ✅ Sie wollen, dass WinUI und MAUI nahtlos zusammenarbeiten
- ✅ Sie Firewall-Probleme komplett eliminieren wollen
- ✅ Sie eine sauberere Architektur bevorzugen

**SPÄTER MACHEN, wenn:**
- ⏳ Die aktuelle Lösung (separater Prozess) funktioniert
- ⏳ Sie zuerst andere Features implementieren wollen
- ⏳ Das Team noch nicht bereit für große Architektur-Änderungen ist

---

## 📋 **Nächste Schritte (Entscheidung nötig):**

### **Option 1: Phase A abschließen (Empfohlen für jetzt)**
1. ✅ MAUI Build-Fehler beheben → **ERLEDIGT**
2. ✅ TrainsPage Properties-Spalte korrigieren → **ERLEDIGT**
3. ⏳ MAUI Counter-Labels Layout manuell anpassen (edit_file Bug)
4. ⏳ Firewall-Helper testen
5. ⏳ End-to-End Test durchführen
6. ⏳ Dokumentation aktualisieren

### **Option 2: Phase B starten (Neue Session)**
1. 🔄 SharedUI.Web Klassenbibliothek erstellen
2. 🔄 Controller & Services migrieren
3. 🔄 WinUI in-process Hosting implementieren
4. 🔄 Ausführlich testen

**Empfehlung:** Phase A jetzt abschließen (Punkte 3-6), Phase B in neuer Session mit frischem Mindset angehen.

---

## 📚 **Referenzen:**

- **ASP.NET Core In-Process Hosting:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host
- **Kestrel Configuration:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel
- **Windows Firewall via netsh:** https://learn.microsoft.com/en-us/windows-server/networking/technologies/netsh/netsh-contexts

---

**Letzte Aktualisierung:** 2025-02-03 01:15 UTC  
**Erstellt von:** GitHub Copilot  
**Status Phase A:** 5/9 erledigt (56%)
