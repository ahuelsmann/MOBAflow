# 🌐 Dual-Repository Setup: GitHub + Azure DevOps (Option D)

**Status:** 🎯 Implementation Guide  
**Scenario:** Public GitHub + Private Azure DevOps  
**Last Updated:** February 2026

---

## 📊 Overview: Option D

Your strategy for MOBAflow:

```
┌─────────────────────────────────────────────────────┐
│  GitHub (Public)                                    │
│  └─ MOBAflow Open Source (WinUI, WebApp, MAUI)      │
│     └─ Open for Community & External Contributions │
└─────────────┬───────────────────────────────────────┘
              │ Master is synchronized
              │ (daily or manual)
              ↓
┌─────────────────────────────────────────────────────┐
│  Azure DevOps (Private)                             │
│  ├─ Open Source Mirror (synchronized)               │
│  └─ Commercial Features & Plugins (private)         │
│     └─ Closed for Team-only Development            │
└─────────────────────────────────────────────────────┘
```

### Advantages of Option D

✅ **Clarity:** GitHub = Open Source, AzDo = Private Commercial  
✅ **Security:** Commercial features remain protected  
✅ **Control:** You decide what synchronizes to GitHub  
✅ **Backup:** Both repos as redundancy  
✅ **Workflow:** Can develop in both, selectively merge  

---

## 🔧 Step 1: Create GitHub Repository

### 1.1 Prepare GitHub Repository

```bash
# On GitHub.com:
# 1. "Create a new repository"
# 2. Name: MOBAflow
# 3. Description: Event-driven automation for model railroads
# 4. Visibility: Public
# 5. ❌ Do NOT "Initialize with README" (we push existing code)
# 6. Create Repository
```

**Copy GitHub Repository URL:** `https://github.com/ahuelsmann/MOBAflow.git`

### 1.2 Locally: Add GitHub Remote

```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Check current origin
git remote -v
# origin: https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow (currently)

# Add GitHub as new remote
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# Rename: origin → azure (for clarity)
git remote rename origin azure

# Verify
git remote -v
# azure:  https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow (Private)
# github: https://github.com/ahuelsmann/MOBAflow.git (Public)
```

### 1.3 Push to GitHub

```bash
# All branches
git push github main

# All tags
git push github --tags

# Or everything at once:
git push github --all --tags
```

✅ **Done!** MOBAflow is now on GitHub!

---

## 🔗 Step 2: Visual Studio Setup (Dual Repos)

### 2.1 Prepare Solution

**Visual Studio 2026:**
```
1. Open Solution (existing solution remains)
2. Open Team Explorer (View → Team Explorer)
3. Should already be connected to AzDo (azure)
4. Manually add GitHub (github)
```

### 2.2 Configure Git Remotes in Visual Studio

**Team Explorer → Repositories → Settings:**

```
Local Git Repositories
├─ Path: C:\Repos\ahuelsmann\MOBAflow
   
Remotes
├─ azure: https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
├─ github: https://github.com/ahuelsmann/MOBAflow.git
└─ origin: [remove - not needed]
```

**In Visual Studio:**
```
1. Team Explorer → Settings → Repository Settings
2. Remotes:
   - azure: https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
   - github: https://github.com/ahuelsmann/MOBAflow.git
3. Save
```

**Or via Command Line:**
```bash
git remote remove origin  # If still present
git remote add azure https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
git remote add github https://github.com/ahuelsmann/MOBAflow.git
```

### 2.3 Verify Setup

```bash
git remote -v

# Expected output:
# azure    https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow (fetch)
# azure    https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow (push)
# github   https://github.com/ahuelsmann/MOBAflow.git (fetch)
# github   https://github.com/ahuelsmann/MOBAflow.git (push)
```

---

## 🔄 Step 3: Synchronization Workflow

### Workflow 1: GitHub → Azure DevOps (Automatic Daily)

**GitHub Actions Workflow** (`.github/workflows/sync-to-azdo.yml`):

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

      - name: Sync main to AzDo
        run: |
          git remote add azure https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
          git push azure main:main --force
        env:
          GIT_AUTHOR_NAME: github-actions
          GIT_AUTHOR_EMAIL: github-actions@github.com
          
      - name: Sync develop to AzDo
        run: |
          git push azure develop:develop --force 2>/dev/null || echo "develop branch not found"
```

### Workflow 2: Work Locally & Synchronize

**You work locally and push to both repos:**

```bash
# Create feature branch & work
git checkout -b feature/new-feature
git add .
git commit -m "feat: Add new feature"

# Push to BOTH repos
git push azure feature/new-feature
git push github feature/new-feature

# Or later: Merge main and push to GitHub
git checkout main
git merge feature/new-feature
git push github main      # To GitHub
git push azure main       # To AzDo
```

### Workflow 3: Commercial Features (AzDo only)

```bash
# Separate branch for commercial features
git checkout -b feature/commercial-plugin

# Develop code
git add .
git commit -m "feat: Add commercial plugin"

# ONLY push to AzDo (not to GitHub!)
git push azure feature/commercial-plugin

# Later: Never merge to GitHub (stays private)
```

---

## 📋 Practical Scenarios

### Scenario 1: Normal Development (Open Source)

```bash
# 1. New feature for Track Plan
git checkout -b feature/track-library-improvements

# 2. Write code & test
echo "New code" > TrackLibrary.PikoA/NewFeature.cs
git add .
git commit -m "feat(track): Add new track geometries"

# 3. Push to GitHub (Main development)
git push github feature/track-library-improvements

# 4. Create Pull Request on GitHub
# → Community can review & contribute

# 5. After merge: Automatic sync to AzDo
# (GitHub Actions handles this)
```

**Result:**
- ✅ Code is on GitHub for Community
- ✅ Code also exists on AzDo
- ✅ No double management

### Scenario 2: Commercial Features (Private)

```bash
# 1. Private branch for commercial feature
git checkout -b feature/premium-analytics

# 2. Write code (analytics engine)
git add .
git commit -m "feat(commercial): Premium analytics dashboard"

# 3. ONLY push to Azure DevOps!
git push azure feature/premium-analytics

# 4. Merge in private AzDo branch (not main!)
git checkout commercial-branch
git merge feature/premium-analytics
git push azure commercial-branch

# 5. NEVER push to GitHub!
# ❌ git push github feature/premium-analytics  ← NEVER!
```

**Result:**
- ✅ Feature stays private on AzDo
- ✅ Available later as plugin
- ✅ GitHub doesn't see this code

### Scenario 3: AzDo main → GitHub main (Manual Sync)

```bash
# Sometimes AzDo is more advanced
# Then: Manually sync from AzDo to GitHub

# 1. Fetch from AzDo
git fetch azure
git checkout main
git merge azure/main

# 2. Push to GitHub
git push github main

# Done! GitHub is now current.
```

---

## 🛠️ Visual Studio: Practical Management

### Multiple Remotes in Team Explorer

**Visual Studio 2026 → Team Explorer:**

```
Home
├─ Settings
│  └─ Repository Settings
│     └─ Remotes
│        ├─ azure: [configured]
│        └─ github: [configured]
├─ Branches
│  ├─ Local
│  │  ├─ main
│  │  ├─ develop
│  │  └─ feature/...
│  └─ Remote
│     ├─ azure/main
│     ├─ azure/develop
│     ├─ github/main
│     └─ github/develop
└─ Sync
   ├─ Pull
   ├─ Push
   └─ Fetch
```

### Push to Multiple Repos at Once

**Option A: Script for Quick Syncs**

**File:** `sync-all.ps1` (in repo root)

```powershell
# Script: Push to all remotes
param(
    [string]$Branch = "main"
)

Write-Host "Pushing $Branch to all remotes..." -ForegroundColor Green

git push azure $Branch
Write-Host "✓ Pushed to Azure DevOps" -ForegroundColor Green

git push github $Branch
Write-Host "✓ Pushed to GitHub" -ForegroundColor Green

git push azure --tags
git push github --tags
Write-Host "✓ All tags pushed" -ForegroundColor Green

Write-Host "Done!" -ForegroundColor Green
```

**Usage:**
```bash
# From PowerShell in repo directory:
.\sync-all.ps1 -Branch main
```

**Option B: Git Hooks for Automatic Syncing**

**File:** `.git/hooks/post-push` (Auto-sync after push)

```bash
#!/bin/bash
BRANCH=$(git rev-parse --abbrev-ref HEAD)
echo "Pushed $BRANCH - syncing to other remotes..."

# Push to GitHub (if not from GitHub)
if [ "$BRANCH" = "main" ] || [ "$BRANCH" = "develop" ]; then
  git push github $BRANCH
  echo "✓ Synced to GitHub"
fi
```

---

## 🔐 Authentication & Credentials

### GitHub Personal Access Token (PAT)

```
GitHub.com → Settings → Developer Settings → Personal Access Tokens

Create token with scopes:
✓ repo (Full control of private repositories)
✓ read:org
```

**Store in Git:**
```bash
git credential approve
protocol=https
host=github.com
username=<your-github-username>
password=<your-personal-access-token>
[Ctrl+D to finish]
```

### Azure DevOps Personal Access Token (PAT)

```
dev.azure.com → User Settings → Personal Access Tokens

Create token with scopes:
✓ Code (Read & Write)
✓ Identity (Read)
```

**Store in Git:**
```bash
git credential approve
protocol=https
host=dev.azure.com
username=<your-username>
password=<your-pat-token>
[Ctrl+D to finish]
```

---

## 📋 Checklist: Dual-Repo Setup

### Preparation
- [ ] GitHub Account & Repo created
- [ ] Azure DevOps Repo (already exists)
- [ ] Git SSH Keys or PAT configured

### GitHub Setup
- [ ] `git remote add github https://github.com/ahuelsmann/MOBAflow.git`
- [ ] `git remote rename origin azure` (for clarity)
- [ ] `git push github --all --tags`
- [ ] GitHub Repo verified (code is present)

### Visual Studio
- [ ] Team Explorer opened
- [ ] Both remotes configured (azure + github)
- [ ] Branches visible (remote-tracking branches)

### Sync Automation
- [ ] GitHub Actions Workflow created (`.github/workflows/sync-to-azdo.yml`)
- [ ] Sync Script created (`sync-all.ps1`)

### Plan Branches
- [ ] `main`: Public Open Source (on GitHub & AzDo)
- [ ] `develop`: Development (optional)
- [ ] `feature/*`: Feature branches (both repos)
- [ ] `commercial/*`: Private Features (AzDo only)

---

## 🚨 Important Rules for Dual-Repo

| Action | ✅ Allowed | ❌ Forbidden |
|--------|---------|-----------|
| Push code to github/main | Yes | - |
| Push code to azure/main | Yes | - |
| Sync github/main → azure/main | Yes | - |
| Sync azure/main → github/main | Yes (only Open Source) | Never for commercial code! |
| commercial/* branch on GitHub | - | ❌ Never! |
| commercial/* branch on AzDo | Yes | - |
| Push tags to both remotes | Yes | - |
| Force push to main | Only if error | ⚠️ Very carefully |

---

## 🔄 Automatic GitHub ↔ AzDo Synchronization

### Option 1: GitHub Actions (Recommended)

**`.github/workflows/sync-to-azdo.yml`:**

```yaml
name: Auto-Sync to Azure DevOps

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

      - name: Configure Git
        run: |
          git config user.email "sync-bot@github.com"
          git config user.name "GitHub Sync Bot"

      - name: Push to Azure DevOps
        run: |
          git remote add azure "https://${{ secrets.AZURE_DEVOPS_USERNAME }}:${{ secrets.AZURE_DEVOPS_PAT }}@dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow"
          git push azure main:main --force
          git push azure --tags
        env:
          AZURE_DEVOPS_USERNAME: ${{ secrets.AZURE_DEVOPS_USERNAME }}
          AZURE_DEVOPS_PAT: ${{ secrets.AZURE_DEVOPS_PAT }}

      - name: Notify
        if: success()
        run: echo "✓ Synced main to Azure DevOps"
```

**Set up GitHub Secrets:**
```
Settings → Secrets → New Repository Secret

AZURE_DEVOPS_USERNAME: <your-azdo-username>
AZURE_DEVOPS_PAT: <your-azdo-personal-access-token>
```

### Option 2: Azure DevOps → GitHub (Webhook-based)

Create a webhook in Azure DevOps that pushes to GitHub (more complex, not recommended).

---

## 📞 Troubleshooting Dual-Repo

### Problem: "Could not resolve host"

```bash
# Firewall/Proxy blocking?
git remote -v  # Check that both URLs are correct
ping github.com
ping dev.azure.com
```

### Problem: "Authentication failed"

```bash
# Reset credentials:
git credential reject
protocol=https
host=github.com
[Enter]

# Then try again, Git will ask for credentials
```

### Problem: "Diverged branches"

```bash
# If GitHub & AzDo have different histories:
git fetch azure
git fetch github
git merge --allow-unrelated-histories azure/main github/main
# (Not recommended - better to sync in time)
```

### Problem: Commits on AzDo but not on GitHub

```bash
# Manual sync:
git fetch azure
git push github azure/main:main
```

---

## ✅ Go-Live Checklist

```
[ ] GitHub Repo created and public
[ ] Code pushed to GitHub
[ ] Azure DevOps remains as private mirror
[ ] Both remotes configured locally
[ ] GitHub Actions Sync workflow runs
[ ] Branching strategy documented
[ ] Team informed about Dual-Repo workflow
[ ] Credentials in GitHub Secrets stored
[ ] Tests on GitHub Actions configured
```

---

## 📚 Summary

**Option D is ideal for you because:**

1. **GitHub = Open Source for Community**
   - Public, for contributors
   - Transparency, community-driven

2. **Azure DevOps = Private for Team + Commercial**
   - Secure for commercial features
   - Private CI/CD, private branches
   - Team collaboration

3. **Dual-Sync is simple**
   - Automatic synchronization via GitHub Actions
   - Manual control where needed
   - No conflicts with good discipline

4. **Scales well**
   - Later commercial plugins can be monetized on GitHub
   - Open source remains open
   - Team maintains full control

---

*With this strategy, you get the best of both worlds! 🚀*
