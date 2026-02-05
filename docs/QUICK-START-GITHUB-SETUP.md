# 🎯 QUICK REFERENCE: GitHub Setup für Sie

**Für:** Andreas (Projekt-Owner)  
**Format:** Schnelle Befehle zum Kopieren & Ausführen  
**Letzte Aktualisierung:** Februar 2026

---

## ⚡ Die wichtigsten Befehle

### 1️⃣ Git-Remotes einrichten (JETZT)

```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Remote rename & add
git remote rename origin azure 2>/dev/null || echo "origin not found"
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# Verify
git remote -v
```

### 2️⃣ Zu GitHub pushen (Nach GitHub Repo erstellen)

```bash
# Alles pushen
git push github --all --tags

# Oder einzeln:
git push github main
git push github develop 2>/dev/null || echo "develop not found"
git push github --tags
```

### 3️⃣ MinVer testen

```bash
# Clean & Rebuild
dotnet clean
dotnet build -c Release

# Version anschauen
[System.Reflection.Assembly]::LoadFrom(".\WinUI\bin\Release\net10-windows\WinUI.exe").GetName().Version
```

### 4️⃣ Tags zu beiden Repos pushen

```bash
# Lokal Tag erstellen (falls noch nicht)
git tag -a v0.1.0 -m "Release 0.1.0" 2>/dev/null || echo "Tag exists"

# Zu beiden pushen
git push azure v0.1.0
git push github v0.1.0

# Alle Tags
git push azure --tags
git push github --tags
```

---

## 📋 Nächste Woche: GitHub Setup Schritte

### Schritt-für-Schritt (Kopiere diese Befehle)

```powershell
# 1. GitHub Repo erstellen
#    https://github.com/new
#    Name: MOBAflow, Description: Event-driven automation...
#    Public ✓, Initialize: ✗

# 2. Lokal Remotes
cd C:\Repos\ahuelsmann\MOBAflow
git remote rename origin azure 2>/dev/null || echo "Already done"
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# 3. Zu GitHub pushen
git push github --all --tags

# 4. Verify
git remote -v
curl -I https://github.com/ahuelsmann/MOBAflow  # Sollte 200 OK sein
```

---

## 🎨 Kommerzielle Features: Privat halten

```bash
# Neue Feature für Premium
git checkout -b feature/premium-analytics

# Entwickeln & committen
echo "Premium Code" > premium.cs
git add .
git commit -m "feat(commercial): Add analytics"

# NUR zu AzDo pushen (NICHT zu GitHub!)
git push azure feature/premium-analytics

# ❌ NICHT MACHEN:
# git push github feature/premium-analytics
```

---

## 🚀 Automatische Sync prüfen

```bash
# Nach GitHub Push:
git push github main

# Warten 1-2 Minuten, dann überprüfen:
git fetch azure
git log --oneline -1 azure/main

# Sollte gleich wie github/main sein ✓
```

---

## 📁 Wichtige Dateien (Schneller Zugriff)

```
Rechtliches:
├─ HARDWARE-DISCLAIMER.md     ← Immer prominent verlinken!
├─ LICENSE                     ← MIT ✓
└─ THIRD-PARTY-NOTICES.md

Setup & Installation:
├─ docs/wiki/INSTALLATION.md   ← Für neue User
└─ docs/wiki/INDEX.md          ← Wiki-Übersicht

Versionierung:
├─ version.json                ← MinVer Config
├─ Directory.Build.props       ← MinVer Build
└─ docs/MINVER-SETUP.md        ← Erklärung

Dual-Repo:
├─ docs/DUAL-REPO-STRATEGY.md  ← Strategie
├─ docs/VISUAL-STUDIO-DUAL-REPO.md ← Team Guide
└─ .github/workflows/sync-to-azdo.yml ← Auto-Sync
```

---

## 🔐 GitHub Credentials speichern

```bash
# Einmalig für GitHub
git credential approve
protocol=https
host=github.com
username=ahuelsmann
password=<your-personal-access-token>
[Ctrl+D]

# Einmalig für Azure DevOps
git credential approve
protocol=https
host=dev.azure.com
username=ahuelsmann
password=<your-azdo-pat>
[Ctrl+D]
```

---

## 📊 Visual Studio Team Explorer Checklist

```
Team Explorer (Ctrl+0, C)
├─ Home
│  ├─ Settings → Repository Settings
│  │  └─ Remotes
│  │     ├─ ☑ azure: https://dev.azure.com/...
│  │     └─ ☑ github: https://github.com/...
│  ├─ Branches
│  │  ├─ Local: main, develop, feature/*
│  │  └─ Remote: azure/*, github/*
│  └─ Sync
│     ├─ Pull
│     ├─ Fetch
│     └─ Push → wähle: github oder azure
```

---

## 🚨 Häufige Fehler (NICHT machen)

```bash
# ❌ FALSCH: Kommerzielle zu GitHub
git push github feature/premium-analytics

# ❌ FALSCH: Force-Push zu main
git push azure main --force

# ❌ FALSCH: PAT im .gitconfig speichern
# → Verwende: git credential approve

# ❌ FALSCH: Unterschiedliche Versionen pushen
# → Nutze: git push git/azure + github immer beide

# ❌ FALSCH: Tags nur zu einem Repo
git push github v0.1.0  # Immer auch:
git push azure v0.1.0
```

---

## ✅ Sync-Workflow (Täglich)

```bash
# Morgens: Fetch aus beiden Repos
git fetch azure
git fetch github

# Tagsüber: Normale Arbeit
git checkout -b feature/xyz
git commit -m "..."

# Abends: Zu GitHub pushen (automatisch zu AzDo synced)
git push github feature/xyz

# Oder für Hotfixes: Zu beiden
git push azure feature/xyz && git push github feature/xyz
```

---

## 📞 Support

**Frage:** MinVer funktioniert nicht?
```bash
git clean -fdx  # Clean everything
dotnet restore
dotnet build -c Release

# Falls immer noch "0.0.0":
git tag  # Sollte v0.1.0 anzeigen
git fetch --tags
```

**Frage:** Welcher Remote wird verwendet?
```bash
git push -u origin main  # Zeigt: "fatal: 'origin' does not exist"
# → Richtig! origin existiert nicht mehr (renamed zu azure)
```

**Frage:** Sind GitHub & AzDo synchron?
```bash
git log --oneline azure/main ^github/main  # Sollte leer sein
git log --oneline github/main ^azure/main  # Sollte leer sein
```

---

## 🎯 GO-LIVE CHECKLIST (Morgen/Nächste Woche)

```
Vor GitHub:
[ ] GitHub Repo erstellt (https://github.com/new)
[ ] local: git remote add github https://...
[ ] Credentials gespeichert (git credential approve)

Während GitHub:
[ ] git push github --all --tags
[ ] Verify: https://github.com/ahuelsmann/MOBAflow
[ ] Branch Protection konfigurieren

Nach GitHub:
[ ] Sync-Workflow testen: Push zu main → Auto-Sync zu AzDo
[ ] MinVer testen: dotnet build -c Release
[ ] Erste v0.1.0 Release auf GitHub
```

---

## 📈 Später (Nächste Sessions)

```
[ ] GitHub Actions: .github/workflows/build.yml
[ ] GitHub Actions: .github/workflows/test.yml
[ ] GitHub Actions: .github/workflows/sync-to-azdo.yml
[ ] Branch Protection: main branch
[ ] Dependabot aktivieren
[ ] CONTRIBUTING.md für Community
[ ] Release Management automatisieren
```

---

*Alles klar? Let's go! 🚀*
