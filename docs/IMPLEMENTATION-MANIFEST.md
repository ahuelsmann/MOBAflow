# 🚀 Implementierungsmanifest: GitHub + MinVer + Dual-Repo

**Für:** MOBAflow Open-Source-Migration  
**Status:** Ready to Execute  
**Nächste Schritte:** Diese Session beenden, GitHub-Repo erstellen  
**Letzte Aktualisierung:** Februar 2026

---

## 📋 Zusammenfassung: Was wurde bereits getan

### ✅ Session-Ergebnisse

| Task | Status | Datei |
|------|--------|-------|
| Hardware-Disclaimer | ✅ Erstellt | `HARDWARE-DISCLAIMER.md` |
| README.md Update | ✅ Updated | `README.md` |
| Wiki Installation | ✅ Erstellt | `docs/wiki/INSTALLATION.md` |
| Wiki INDEX Update | ✅ Updated | `docs/wiki/INDEX.md` |
| MinVer Dokumentation | ✅ Erstellt | `docs/MINVER-SETUP.md` |
| MinVer Konfiguration | ✅ Configured | `Directory.Build.props`, `version.json` |
| Git-Tag v0.1.0 | ✅ Erstellt | `git tag v0.1.0` (lokal) |
| Dual-Repo Dokumentation | ✅ Erstellt | `docs/DUAL-REPO-STRATEGY.md` |
| Visual Studio Guide | ✅ Erstellt | `docs/VISUAL-STUDIO-DUAL-REPO.md` |
| TODOs aktualisiert | ✅ Updated | `.github/instructions/todos.instructions.md` |

---

## 🎯 Nächste Schritte (Konkret)

### Phase 1: GitHub Repo Setup (30 Min)

#### Schritt 1.1: GitHub Repo erstellen
```
1. Gehe zu: https://github.com/new
2. Repository name: MOBAflow
3. Description: "Event-driven automation for model railroads"
4. Visibility: ☑ Public
5. ❌ Nicht "Initialize with README"
6. Create Repository
```

**Output:** GitHub Repo URL `https://github.com/ahuelsmann/MOBAflow.git`

#### Schritt 1.2: Lokal Remotes konfigurieren
```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Überprüfe aktuellen Status
git remote -v

# Remotes anpassen
git remote rename origin azure  # Falls noch "origin"
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# Verify
git remote -v
```

**Ergebnis:**
```
azure   https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
github  https://github.com/ahuelsmann/MOBAflow.git
```

#### Schritt 1.3: Code zu GitHub pushen
```bash
# Alle Branches
git push github main
git push github develop 2>/dev/null || echo "develop nicht vorhanden"

# Alle Tags
git push github --tags

# Oder Combined:
git push github --all --tags
```

**Verify:** https://github.com/ahuelsmann/MOBAflow sollte Code enthalten ✅

---

### Phase 2: MinVer Tests (10 Min)

#### Schritt 2.1: MinVer Build durchführen
```bash
# Clean & Rebuild
dotnet clean
dotnet restore

# Build mit Release-Konfiguration
dotnet build -c Release

# Sollte ohne Fehler durchlaufen ✅
```

#### Schritt 2.2: Version überprüfen
```bash
# Compiled DLL-Version anschauen
[System.Reflection.Assembly]::LoadFrom(".\WinUI\bin\Release\net10-windows\WinUI.exe").GetName().Version

# Sollte etwa: 0.1.0.0 sein ✅
```

#### Schritt 2.3: Tag zu Azure DevOps pushen
```bash
# Tag wurde bereits lokal erstellt (git tag v0.1.0)
# Jetzt zu Azure DevOps pushen:
git push azure v0.1.0

# Und zu GitHub
git push github v0.1.0
```

---

### Phase 3: GitHub Actions Workflow (15 Min)

#### Schritt 3.1: Sync-Workflow erstellen
**Datei:** `.github/workflows/sync-to-azdo.yml`

```yaml
name: Sync to Azure DevOps

on:
  push:
    branches: [main, develop]

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Sync to Azure DevOps
        run: |
          git remote add azure https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
          git push azure main:main --force
          git push azure --tags
        env:
          GIT_AUTHOR_NAME: github-actions
          GIT_AUTHOR_EMAIL: github-actions@github.com
```

#### Schritt 3.2: GitHub Secrets konfigurieren (Falls Auth nötig)
```
GitHub.com → ahuelsmann/MOBAflow → Settings → Secrets & Variables

Falls Azure DevOps Auth erforderlich:
AZURE_DEVOPS_USERNAME: <username>
AZURE_DEVOPS_PAT: <personal-access-token>
```

**Note:** Öffentliche Repos brauchen meist keine Auth für Push (überprüfen)

---

### Phase 4: Branch Protection & CI/CD (20 Min)

#### Schritt 4.1: Branch Protection für main
```
GitHub.com → ahuelsmann/MOBAflow → Settings → Branches

Add branch protection rule:
- Branch name pattern: main
- ☑ Require pull request reviews (1)
- ☑ Require status checks to pass (before merging)
- ☑ Require branches to be up to date
```

#### Schritt 4.2: GitHub Actions für Build
**Datei:** `.github/workflows/build.yml`

```yaml
name: Build & Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release --no-restore
      
      - name: Test
        run: dotnet test --no-build -c Release
```

---

## 📋 Checkliste: Was noch zu tun ist

### Vor Go-Live (1-2 Tage)

```
[ ] GitHub Repo erstellen (öffentlich)
[ ] Code zu GitHub gepusht (mit Tags)
[ ] MinVer Build funktioniert (test: dotnet build)
[ ] Tags zu beiden Repos gepusht
[ ] GitHub Actions Workflows erstellt:
    [ ] sync-to-azdo.yml (GitHub → AzDo)
    [ ] build.yml (Compile & Test)
[ ] GitHub Branch Protection konfiguriert
[ ] Credentials/PAT in GitHub Secrets gespeichert (falls nötig)
[ ] README.md Quickstart funktioniert (manual test)
[ ] Hardware-Disclaimer ist prominent verlinkt
```

### Nach Go-Live (1 Woche)

```
[ ] First Public Release (v0.1.0) auf GitHub Releases
[ ] GitHub Issues & Discussions aktivieren
[ ] Dependabot aktivieren (für Security Updates)
[ ] CONTRIBUTING.md für GitHub Community
[ ] Community-Ankündigung:
    [ ] Blog/Website
    [ ] GitHub Discussions
    [ ] Model-Railroad Forums
    [ ] Twitter/LinkedIn
[ ] Monitor: GitHub Stars, Issues, PRs
```

### Kommerzialisierung (Später)

```
[ ] Kommerzielle Features in private AzDo Branch
[ ] Licensing/Payment System evaluieren
[ ] Plugin Marketplace planen
[ ] v1.0.0 Release Plan erstellen
```

---

## 🔗 Dokumentations-Index (Alle Links)

| Zweck | Datei | Leser |
|-------|-------|-------|
| **Haftung & Sicherheit** | `HARDWARE-DISCLAIMER.md` | Alle User |
| **Installation & Setup** | `docs/wiki/INSTALLATION.md` | User |
| **MinVer Understanding** | `docs/MINVER-SETUP.md` | Developer |
| **Dual-Repo Strategie** | `docs/DUAL-REPO-STRATEGY.md` | Team/Developer |
| **Visual Studio Howto** | `docs/VISUAL-STUDIO-DUAL-REPO.md` | Developer (Team) |
| **TODOs & Roadmap** | `.github/instructions/todos.instructions.md` | Team |
| **Version Info** | `version.json` | MinVer (Auto) |
| **Architektur** | `ARCHITECTURE.md` | Developer |
| **Contributing** | `CONTRIBUTING.md` | Community |

---

## 📞 FAQ: Häufige Fragen

### F: Muss ich beide Repos lokal klonen?
**A:** Nein! Ein lokales Repo mit zwei Git Remotes ist genug (current setup). Sie können auch zwei lokale Clones haben, aber das ist redundant.

### F: Passiert MinVer automatisch?
**A:** Ja! MinVer liest automatisch Git-Tags beim Build. Kein Manual-Update der Version nötig.

### F: Können andere zu GitHub beitragen?
**A:** Ja! Nach Go-Live können Community-Mitglieder PRs zu GitHub erstellen. Diese werden dann zu AzDo synced.

### F: Was ist mit kommerziellen Features?
**A:** Bleiben auf private AzDo Branches (never pushen zu github). Später können diese als Plugins monetarisiert werden.

### F: Wie lange dauert der Sync GitHub→AzDo?
**A:** ~1-2 Minuten (GitHub Actions Workflow). Manueller Sync ist sofort.

### F: Können wir GitHub später wieder ausschalten?
**A:** Theoretisch ja, aber nicht empfohlen. Ein Mal public = für immer public (History). AzDo bleibt für private Module.

---

## 🎓 Learning Resources

### Git & Remote Management
- Git Documentation: https://git-scm.com/docs
- GitHub Docs: https://docs.github.com
- Multiple Remotes: https://git-scm.com/book/en/v2/Git-Basics-Working-with-Remotes

### Versionierung
- MinVer: https://github.com/adamralph/minver
- Semantic Versioning: https://semver.org/
- Git Tags: https://git-scm.com/book/en/v2/Git-Basics-Tagging

### GitHub & Open Source
- GitHub Open Source Guide: https://opensource.guide
- GitHub Best Practices: https://docs.github.com/en/communities

---

## ✨ Zusammenfassung des Setups

**Sie haben jetzt:**

1. ✅ **Rechtliche Absicherung**
   - Hardware-Disclaimer für Z21
   - Klar in README verlinkt

2. ✅ **Automatische Versionierung**
   - MinVer konfiguriert
   - Git-Tags als Single Source of Truth
   - Keine manuellen Versions-Updates mehr nötig

3. ✅ **Dual-Repo Ready**
   - GitHub für Open Source Community
   - Azure DevOps für private Features
   - Automatischer Sync GitHub → AzDo
   - Klare Doku für Development Workflow

4. ✅ **Documentation**
   - Installation Guide für User
   - Entwickler-Guides für Team
   - Troubleshooting & FAQ

5. ✅ **TODOs aktualisiert**
   - Nächste Milestones klar
   - Go-Live Checkliste
   - Roadmap für v0.2.0+

---

## 🚀 Letzter Schritt: Execution Mindset

**Sie sind READY! 🎉**

Die Infrastruktur ist aufgebaut. Nächste Session:

1. GitHub Repo erstellen (30 Min)
2. Code pushen (5 Min)
3. Workflows testen (15 Min)
4. Go-Live ansagen (5 Min)

**Total: ~1 Stunde zum public gehen!**

Danach können Sie:
- Community-Feedback sammeln
- GitHub Issues bearbeiten
- Commercial Plugins in AzDo entwickeln
- MOBAflow als echter Open-Source-Success aufbauen

---

*Status: ALL SYSTEMS GO! 🚀*

**Kontakt:** Jederzeit weitere Fragen → Copilot fragen!
