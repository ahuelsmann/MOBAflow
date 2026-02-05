# 🛠️ Visual Studio: Dual-Repo Verwaltung (Schnellstart)

**Für:** Visual Studio 2026 mit MOBAflow  
**Zielgruppe:** Team-Entwickler  
**Letzte Aktualisierung:** Februar 2026

---

## ⚡ 5 Minuten Setup

### 1️⃣ Git-Remotes konfigurieren

**In Visual Studio:**
```
Team Explorer (Ctrl+0, C)
└─ Home
   └─ Settings
      └─ Repository Settings
         └─ Remotes
            ├─ azure: https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
            └─ github: https://github.com/ahuelsmann/MOBAflow.git
```

**Oder per Command Line:**
```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Alte Origin entfernen
git remote remove origin 2>/dev/null

# Neue Remotes hinzufügen
git remote add azure https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# Verify
git remote -v
```

### 2️⃣ Dafault Remote setzen

**GitHub als Default (für neue Features):**
```bash
git config branch.main.pushRemote github
git config branch.develop.pushRemote github

# Oder für alle: 
git config push.default simple
```

### 3️⃣ Test: Zu beiden Repos pushen

```bash
# Branch erstellen & pushen
git checkout -b test/setup
echo "test" > test.txt
git add .
git commit -m "test: Setup verification"

# Zu Azure DevOps
git push azure test/setup

# Zu GitHub
git push github test/setup

# In Team Explorer beide Branches sehen
# Team Explorer → Branches → Remote
```

---

## 📱 Alltagsarbeit in Visual Studio

### Workflow: Open Source Feature (→ GitHub & AzDo)

**In Visual Studio:**

```
1. Team Explorer → Branches
   └─ Create new branch from main
   └─ Name: feature/improve-track-editor
   
2. Code ändern, committen
   git add .
   git commit -m "feat: Improve track editor snapping"

3. Team Explorer → Sync
   └─ Push
   └─ Wähle: github (default)
   └─ Push button
   
4. Automatic GitHub Actions:
   └─ Workflow: sync-to-azdo.yml läuft automatisch
   └─ Code wird zu Azure DevOps synchronisiert ✓
```

**Ergebnis:** Feature ist auf beiden Repos vorhanden! 🎉

### Workflow: Kommerzielle Feature (→ nur AzDo)

```
1. Team Explorer → Branches
   └─ Create new branch from develop
   └─ Name: feature/premium-dashboard
   
2. Code ändern, committen
   git add .
   git commit -m "feat(commercial): Premium analytics dashboard"

3. Team Explorer → Sync
   └─ Push Dropdown → wähle: azure
   └─ Push button
   
4. ⚠️ WICHTIG: NIE zu GitHub pushen!
   ❌ git push github feature/premium-dashboard
```

**Ergebnis:** Feature bleibt privat auf Azure DevOps! 🔐

---

## 🚀 Praktische Team-Szenarien

### Szenario 1: PR von GitHub annehmen

```
GitHub.com → ahuelsmann/MOBAflow
├─ Pull Requests
└─ Community-PR: "feat: Add new track types"

In Visual Studio:
1. Team Explorer → Branches → Remote → github/PR-123
2. Lokal überprüfen & testen
3. Falls gut:
   - GitHub: Approve & Merge button
   - Visual Studio: Automatisch zu AzDo synchronisiert ✓
```

### Szenario 2: Hotfix parallel entwickeln

```
Situation:
- main ist v0.1.0 (auf GitHub)
- Emergency Bugfix nötig (Z21 Connection)

In Visual Studio:
1. Branch von main erstellen
   └─ feature/hotfix-z21-connection

2. Fix durchführen & testen
   git add .
   git commit -m "fix: Z21 timeout on lost network"

3. Zu GitHub pushen (Priorität)
   Team Explorer → Push → github

4. Zu AzDo auch pushen
   Team Explorer → Push → azure
   
5. GitHub & AzDo haben beide den Fix ✓
```

### Szenario 3: Tags zu beiden Repos

```
Ready für Release!

In Visual Studio oder Command Line:
1. Tag erstellen (lokal)
   git tag -a v0.2.0 -m "Release 0.2.0"

2. Zu BEIDEN Repos pushen
   git push github v0.2.0
   git push azure v0.2.0

3. GitHub Releases-Seite zeigt v0.2.0
   AzDo Releases-Seite zeigt auch v0.2.0 ✓
```

---

## 🔄 Automatische Synchronisierung verstehen

### Was läuft automatisch?

**GitHub Actions:** `.github/workflows/sync-to-azdo.yml`

```
Entwickler pusht Code zu GitHub main
    ↓
GitHub Actions Workflow triggert automatisch
    ↓
Fetcht Code von GitHub
    ↓
Pusht zu Azure DevOps (azure/main)
    ↓
✓ Azure DevOps ist jetzt aktuell!
```

**Zeitverzögerung:** ~1-2 Minuten

### Manuelle Sync (falls nötig)

```bash
# Falls automatische Sync fehlgeschlagen ist:

# GitHub → AzDo
git fetch github
git push azure github/main:main --force

# AzDo → GitHub (selten!)
git fetch azure
git push github azure/main:main --force
```

---

## 🎮 Cheat Sheet: Team Explorer Shortcuts

### Branch-Management

| Aktion | Ort | Shortcut |
|--------|-----|---------|
| Neue Branch | Branches | Right-click main → New Branch from |
| Branch switchen | Branches | Double-click Branch name |
| Branch löschen | Branches | Right-click → Delete |
| Sync | Sync | Pull / Push buttons |
| Fetch | Sync | Fetch button |
| Merge | Branches | Right-click → Merge From |

### Remotes konfigurieren

```powershell
# Alle in Command Line:

# Remote anzeigen
git remote -v

# Remote hinzufügen
git remote add <name> <url>

# Remote umbennen
git remote rename <old> <new>

# Remote löschen
git remote remove <name>

# Push-Default setzen
git config branch.<branch>.pushRemote <remote>
```

---

## ⚙️ Automat konfigurieren (Optim Konfi)

### Auto-Push zum richtigen Remote

**`.gitconfig` (global oder lokal):**

```ini
[branch "main"]
    pushRemote = github

[branch "develop"]
    pushRemote = github

[branch "feature/*"]
    pushRemote = github

[branch "commercial/*"]
    pushRemote = azure

[push]
    default = simple
    followTags = true
```

**Resultat:** `git push` geht automatisch zum richtigen Remote! 🎯

### Auto-Merge nach Pull Request

**GitHub.com → Repository → Settings → Rules:**

```
Branch protection rule for main:
✓ Require pull request reviews before merging
✓ Require status checks to pass
✓ Automatically merge after checks pass (optional)
```

---

## 🚨 Häufige Fehler & Lösungen

### ❌ Fehler: "Permission denied (publickey)"

```bash
# SSH-Key nicht konfiguriert
# Lösung: GitHub/AzDo SSH-Key hinzufügen

# Oder HTTPS verwenden:
git remote set-url azure "https://..."
git remote set-url github "https://..."
```

### ❌ Fehler: "Diverged branches"

```bash
# GitHub & AzDo haben unterschiedliche History
# Lösung: Force Sync (vorsichtig!)

git fetch azure
git push github azure/main:main --force-with-lease
```

### ❌ Fehler: "Commercial code auf GitHub"

```bash
# Oops! Kommerzieller Code nach GitHub gepusht
# Lösung: 

# 1. Branch löschen (überall)
git push github --delete feature/premium
git push azure --delete feature/premium

# 2. Lokal löschen
git branch -d feature/premium

# 3. Commit history bei GitHub entfernen (siehe docs)
```

---

## 📊 Monitoring: Sind beide Repos synchron?

```bash
# Überprüfung: Haben alle Branches den gleichen Code?

# GitHub vs AzDo vergleichen
git fetch github
git fetch azure

# Unterschiede anzeigen
git log --oneline azure/main ^github/main  # In AzDo aber nicht GitHub
git log --oneline github/main ^azure/main  # In GitHub aber nicht AzDo

# Sollte leer sein (kein Output) = synchron ✓
```

---

## 🎯 Best Practices

### ✅ DO

✅ **Use GitHub für public Open Source Features**
```bash
git push github main
```

✅ **Sync regelmäßig**
```bash
# Täglich vor Feierabend
./sync-all.ps1 -Branch main
```

✅ **Tags zu beiden pushen**
```bash
git push github --tags
git push azure --tags
```

✅ **Commercial Features auf separate Branches**
```bash
git checkout -b feature/commercial-module
# Nur zu azure!
git push azure feature/commercial-module
```

### ❌ DON'T

❌ **Commercial Code zu GitHub**
```bash
# ❌ FALSCH:
git push github feature/premium-analytics
```

❌ **Force-Push zu main (ohne Grund)**
```bash
# ❌ FALSCH:
git push azure main --force
# Nur `--force-with-lease` wenn absolut nötig
```

❌ **Unterschiedliche Branches auf GitHub & AzDo**
```bash
# ❌ FALSCH: main auf GitHub != main auf AzDo
# → Verwende automatische Sync!
```

❌ **PAT/SSH-Keys ins Repo**
```bash
# ❌ FALSCH: Credentials in .gitconfig / .git/config
# → Verwende GitHub Secrets!
```

---

## 📞 Schnelle Hilfe

**Frage:** Wo wurde mein Code gepusht?
```bash
git show-ref  # Zeigt alle lokalen & remoten Refs
```

**Frage:** Ist GitHub aktuell?
```bash
git ls-remote github refs/heads/main
# Vergleich mit: git rev-parse main
```

**Frage:** Wann war das letzte Sync?
```bash
git log --oneline -n 5 -- :/sync
# Sucht nach Sync-Commits
```

---

## 🚀 Schnellstart Video (Text-Version)

```
1. Open Visual Studio
2. Open MOBAflow Solution
3. Team Explorer (Ctrl+0, C) → Settings → Remotes → verify both
4. Create new branch: feature/my-feature
5. Make changes, commit
6. Team Explorer → Sync → Push → choose remote
7. Done! (Automatic sync to other repo in ~1-2 min)
```

---

*Happy Dual-Repo Development! 🎉*
