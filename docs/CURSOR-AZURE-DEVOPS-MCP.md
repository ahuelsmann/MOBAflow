# Azure DevOps MCP in Cursor

Die Anbindung an **Azure DevOps** (dev.azure.com/ahuelsmann) erfolgt über den offiziellen **Azure DevOps MCP Server** von Microsoft. So ist der Cursor-Assistent in der Lage, Work Items, Pull Requests, Builds und Projekte abzufragen.

**Projekt-Regel:** Offene Arbeit (Features, Tasks, Backlog) gilt **Azure DevOps** als maßgebliche Quelle; bei Fragen wie „was ist offen?“ wird zuerst das Azure-DevOps-MCP genutzt. Siehe auch `.github/copilot-instructions.md`.

## Voraussetzungen

- **Node.js 18+** (empfohlen 20+)
- **Azure-DevOps-Organisation:** ahuelsmann (Zugriff mit deinem Microsoft-Konto)
- In Cursor muss der Assistent im **Agent-Modus** laufen, damit MCP-Tools genutzt werden können.

## Konfiguration (bereits eingerichtet)

In `.cursor/mcp.json` ist der Server **azure-devops** eingetragen:

- **Paket:** `@azure-devops/mcp@latest`
- **Organisation:** `ahuelsmann` (als Argument)
- **Env:** `AZURE_DEVOPS_ORG_URL=https://dev.azure.com/ahuelsmann`

## Aktivierung in Cursor

1. Cursor öffnen und dieses Repo laden.
2. Wenn Cursor den neuen/geänderten MCP-Server erkennt: MCP-Server **aktivieren** (z. B. in den Einstellungen oder beim ersten Tool-Aufruf).
3. Im Chat den **Agent-Modus** verwenden und ggf. die Azure-DevOps-Tools auswählen.

Beim **ersten Aufruf** eines Azure-DevOps-Tools öffnet sich in der Regel der **Browser zur Anmeldung** mit deinem Microsoft-Konto. Einmal anmelden reicht für die weitere Nutzung.

## Token-Auth (optional, ohne Browser)

Für Skripte, CI oder wenn du ohne Browser-Login arbeiten willst:

1. **Personal Access Token (PAT)** in Azure DevOps anlegen:
   - dev.azure.com/ahuelsmann → Benutzereinstellungen (Profil) → **Personal access tokens**
   - Neuen Token erstellen, Mindestbereich z. B. **Work Items (Read)**, **Build (Read)** je nach Bedarf

2. **Umgebungsvariable** setzen (niemals den Token in `mcp.json` eintragen):
   - Windows (PowerShell, Benutzer):  
     `[System.Environment]::SetEnvironmentVariable('ADO_MCP_AUTH_TOKEN', 'dein-pat', 'User')`
   - Windows (cmd):  
     `setx ADO_MCP_AUTH_TOKEN "dein-pat"`

3. **MCP-Config anpassen:** In `.cursor/mcp.json` beim Server **azure-devops** in `args` ergänzen:  
   `"--authentication", "envvar"`  
   (also z. B. `"args": ["-y", "@azure-devops/mcp@latest", "ahuelsmann", "--authentication", "envvar"]`)

4. Cursor vollständig neu starten.

## Beispiel-Prompts

- „Liste die Azure-DevOps-Projekte der Organisation ahuelsmann.“
- „Zeige meine Work Items für das Projekt MOBAflow.“
- „Welche Pull Requests sind offen?“

## Weitere Infos

- [Azure DevOps MCP Server (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/devops/mcp-server/mcp-server-overview)
- [Quellcode & Cursor-Setup (GitHub)](https://github.com/microsoft/azure-devops-mcp)
- [Troubleshooting](https://github.com/microsoft/azure-devops-mcp/blob/main/docs/TROUBLESHOOTING.md) (z. B. OAuth vs. PAT, Multi-Tenant)
