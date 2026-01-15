---
description: 'Security checklist for GitHub release - API keys, secrets, history cleanup'
applyTo: '**'
---

# 🚨 Security Checklist (vor GitHub-Release)

> **Status:** ⚠️ **BLOCKIEREND** - Muss vor Public Release erledigt werden!
> **Priorität:** 🔴🔴🔴 HÖCHSTE PRIORITÄT
> **Letztes Update:** 2026-01-18

---

## Gefundene Secrets im Code

| Datei | Zeile | Problem | Status |
|-------|-------|---------|--------|
| `WinUI/appsettings.json` | 21 | Azure Cognitive Services Key hardcoded | ⚠️ OFFEN |
| `WinUI/appsettings.Development.json` | 10 | Placeholder `your-test-key-here` | ✅ OK |

### Konkrete Fundstelle

```json
// WinUI/appsettings.json - Zeile 21
"Speech": {
    "Key": "b29427debf254c88bef939dbab94f162",  // ← ECHTE API KEY!
    "Region": "germanywestcentral",
    ...
}
```

---

## Erforderliche Aktionen

- [ ] **1. Azure Key rotieren** - Der aktuelle Key könnte bereits kompromittiert sein
- [ ] **2. Key aus appsettings.json entfernen** - Ersetzen durch Placeholder oder Umgebungsvariable
- [ ] **3. User Secrets oder Environment Variables nutzen**
  - Option A: `dotnet user-secrets` für lokale Entwicklung
  - Option B: Umgebungsvariable `MOBA_SPEECH_KEY`
  - Option C: Azure Key Vault für Production
- [ ] **4. .gitignore prüfen** - Sicherstellen dass keine Secrets committed werden
- [ ] **5. Git History bereinigen** - BFG Repo-Cleaner oder `git filter-branch`
- [ ] **6. appsettings.json.template erstellen** - Vorlage ohne echte Keys

---

## Empfohlene Lösung

**appsettings.json (öffentlich):**
```json
"Speech": {
    "Key": "",  // Set via environment variable MOBA_SPEECH_KEY
    "Region": "germanywestcentral",
    ...
}
```

**Umgebungsvariable setzen:**
```powershell
$env:MOBA_SPEECH_KEY = "your-actual-key"
```

**Code-Anpassung (App.xaml.cs oder SpeechOptions):**
```csharp
var key = Environment.GetEnvironmentVariable("MOBA_SPEECH_KEY") 
       ?? configuration["Speech:Key"];
```

---

## Weitere Security-Checks

| Check | Status |
|-------|--------|
| Keine anderen API Keys | ✅ Überprüft |
| Keine Passwörter | ✅ Überprüft |
| Keine Connection Strings | ✅ Überprüft |
| Z21 IP-Adresse | ✅ Nur lokale Netzwerk-IP |

---

*Teil von: [.copilot-todos.md](../.copilot-todos.md)*
