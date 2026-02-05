# Setup appsettings.Development.json

## Für Entwickler

Die `appsettings.Development.json` ist **NICHT** im Repository und enthält sensible Daten (Azure Speech Keys).

### Ersteinrichtung:

1. **Kopiere die Template-Datei:**
   ```sh
   copy WinUI\appsettings.Development.json.template WinUI\appsettings.Development.json
   ```

2. **Trage deinen Azure Speech Key ein:**
   - Öffne `WinUI\appsettings.Development.json`
   - Ersetze `YOUR_AZURE_SPEECH_KEY_HERE` mit deinem echten Key
   - **WICHTIG:** Diese Datei wird automatisch von `.gitignore` ignoriert!

### Azure Speech Key erhalten:

Siehe: `docs/wiki/AZURE-SPEECH-SETUP.md`

### Hinweise:

- **appsettings.json** → Basis-Konfiguration (im Git, **KEINE Secrets**)
- **appsettings.Development.json** → Entwickler-spezifisch (lokal, **MIT Secrets**, nicht im Git)
- **appsettings.Development.json.template** → Vorlage für neue Entwickler (im Git)

### Was wird wann geladen?

```
DEBUG Build:
  1. appsettings.json (Basis)
  2. appsettings.Development.json (überschreibt #1)
  
RELEASE Build:
  1. appsettings.json (nur Basis)
```
