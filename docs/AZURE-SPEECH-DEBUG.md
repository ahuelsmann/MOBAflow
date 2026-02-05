# 🛠️ Azure Speech Debug Configuration for Developers

**Für Entwickler:** So konfigurierst du Azure Speech Service Credentials im Debug-Modus.

---

## Problem

Die Sprachausgabe funktioniert nicht im Debug-Modus, obwohl Azure Speech Service im Settings-Menü ausgewählt ist.

## Ursache

Die Azure Speech API-Key und Region sind nicht konfiguriert. `appsettings.json` sollte NIEMALS echte Credentials enthalten (wird zu Git committed!).

---

## ✅ Lösung: appsettings.Development.json (Empfohlen)

Diese Datei wird **NUR** im Debug-Modus geladen und ist in `.gitignore` ausgeschlossen.

### Schritt 1: Öffne die Datei

```
WinUI\appsettings.Development.json
```

### Schritt 2: Setze deine Azure Speech Credentials

Ersetze `"your-azure-speech-key-here"` mit deinem echten API-Key:

```json
{
  "Speech": {
    "Key": "abc123def456ghi789jklmno",
    "Region": "germanywestcentral",
    "SpeakerEngineName": "Azure Cognitive Services",
    "VoiceName": "de-DE-KatjaNeural",
    "Rate": -1,
    "Volume": 90
  }
}
```

### Schritt 3: Starte die App neu

1. Stoppe die laufende Debug-Session
2. Starte die App neu (F5)
3. Gehe zu **Settings > Speech Synthesis**
4. Wähle **Azure Cognitive Services**
5. Teste eine Ansage

---

## Alternative: User Secrets (Noch sicherer)

.NET User Secrets speichern Credentials AUSSERHALB des Projekts.

### Schritt 1: Navigiere zum WinUI-Projekt

```powershell
cd WinUI
```

### Schritt 2: Setze Secrets

```powershell
dotnet user-secrets set "Speech:Key" "abc123def456ghi789jklmno"
dotnet user-secrets set "Speech:Region" "germanywestcentral"
```

### Schritt 3: Verifiziere

```powershell
dotnet user-secrets list
```

Ausgabe:
```
Speech:Key = abc123def456ghi789jklmno
Speech:Region = germanywestcentral
```

---

## Alternative: Umgebungsvariablen

Setze permanente Umgebungsvariablen (Windows):

```powershell
setx SPEECH_KEY "abc123def456ghi789jklmno"
setx SPEECH_REGION "germanywestcentral"
```

**⚠️ Wichtig:** Starte Visual Studio nach dem Setzen von Umgebungsvariablen NEU!

---

## 🔍 Debugging

### 1. Prüfe die Konfiguration

Füge einen Breakpoint in `App.xaml.cs` hinzu:

```csharp
services.Configure<SpeechOptions>(configuration.GetSection("Speech"));
```

Prüfe in der Watch-Ansicht:
- `configuration["Speech:Key"]` → Sollte dein API-Key sein
- `configuration["Speech:Region"]` → Sollte deine Region sein

### 2. Prüfe die Logs

Öffne die Log-Datei:
```
%TEMP%\MOBAflow\logs\mobaflow-{date}.log
```

Suche nach:
```
Azure Speech credentials missing
```

### 3. Prüfe den SpeechHealthCheck

Nach App-Start sollte in den Logs stehen:
```
Speech Service health check: Healthy
```

Falls nicht:
```
Speech Service health check: Unhealthy - Azure Speech credentials missing
```

---

## 📚 API-Key bekommen

Falls du noch keinen Azure Speech API-Key hast:

1. Öffne die Wiki-Seite: **[AZURE-SPEECH-SETUP.md](./AZURE-SPEECH-SETUP.md)**
2. Folge der **Step-by-Step Anleitung**
3. Kostenlos bis 500.000 Zeichen/Monat! (Free Tier F0)

---

## ⚠️ NIEMALS committen!

Diese Dateien/Secrets **NIE** zu Git committen:

❌ `appsettings.Development.json` (mit echtem Key)  
❌ `appsettings.json` (mit echtem Key)  
✅ `appsettings.json` (mit `"Key": ""` leer!)

Die `.gitignore` schützt bereits `appsettings.Development.json`.

---

*Zuletzt aktualisiert: 2026-02-05*
