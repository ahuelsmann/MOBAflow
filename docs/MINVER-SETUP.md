# 🔢 MinVer: Versioning for MOBAflow

**Status:** ℹ️ Implementation in Progress  
**Version:** 0.1.0 (Preview)  
**Last Updated:** February 2026

---

## 📖 What is MinVer?

**MinVer** is a .NET tool for **automatic Semantic Versioning** based on Git tags.

### 🎯 The Problem (without MinVer)

```xml
<!-- Directory.Build.props - HARDCODED! -->
<Version>0.1.0</Version>
<AssemblyVersion>0.1.0.0</AssemblyVersion>
<FileVersion>0.1.0.0</FileVersion>
```

**Disadvantages:**
- ❌ Version must be manually updated with each release
- ❌ Easy to forget → wrong version numbers
- ❌ No automatic preview versions (0.1.0-preview.123)
- ❌ No Git integration

### ✅ The Solution (with MinVer)

```powershell
git tag -a v0.1.0 -m "Release 0.1.0"
# MinVer automatically reads from this tag!
# → Version = 0.1.0 ✅
```

With each commit after the tag:
```
0.1.0-preview.124+g1a2b3c4d  (automatic!)
```

---

## 🔧 How MinVer Works

### Versioning Logic

```
Git Structure:
─────────────────────────────────────────────────────────

Tag: v0.1.0
│
├─ Commit 1 (after tag)  → Version: 0.1.0-preview.1
├─ Commit 2 (after tag)  → Version: 0.1.0-preview.2
│
Tag: v0.2.0
│
├─ Commit 1 (after tag)  → Version: 0.2.0-preview.1
```

### Semantic Versioning (SemVer)

MinVer follows **Semantic Versioning 2.0.0**:

```
MAJOR.MINOR.PATCH
│      │      └─ Bugfixes (0.1.0 → 0.1.1)
│      └──────── Features (0.1.0 → 0.2.0)
└─────────────── Breaking Changes (0.1.0 → 1.0.0)
```

### Version Format

| Scenario | Version Number | Example |
|----------|---|---|
| Create tag | `v1.0.0` | Releases |
| Commits after tag | `1.0.0-preview.N` | Development |
| Build metadata | `+g<commit>` | Git commit |

---

## 🚀 Installation & Setup

### Step 1: Add MinVer NuGet Package

```bash
# Navigate to project directory
cd C:\Repos\ahuelsmann\MOBAflow

# Add MinVer as NuGet Package
# Or: Manually write to project file
```

**Manually in `.csproj` or `Directory.Build.props`:**

```xml
<ItemGroup>
  <PackageReference Include="MinVer" Version="5.0.0" />
</ItemGroup>
```

### Step 2: Create version.json

**File:** `version.json` (in repo root)

```json
{
  "$schema": "https://raw.githubusercontent.com/adamralph/minver/main/schema.json",
  "minimum": "0.1.0",
  "prerelease": "preview",
  "buildMetadata": "build"
}
```

**Meaning:**
- `minimum`: First version number (before first tag)
- `prerelease`: Prefix for preview versions
- `buildMetadata`: Add Git commit hash

### Step 3: Adjust Directory.Build.props

**Replace:**
```xml
<!-- OLD - HARDCODED -->
<Version>0.1.0</Version>
<AssemblyVersion>0.1.0.0</AssemblyVersion>
<FileVersion>0.1.0.0</FileVersion>
```

**With:**
```xml
<!-- NEW - AUTOMATIC VIA MINVER -->
<!-- MinVer will automatically inject the version! -->
```

### Step 4: Create First Tag

```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Create tag (local)
git tag -a v0.1.0 -m "Release version 0.1.0"

# Push to remote (GitHub/AzDo)
git push origin v0.1.0

# Push all tags
git push origin --tags
```

### Step 5: Test Build

```bash
# Build
dotnet build -c Release

# Check version (in DLL properties)
```

**Verification with PowerShell:**
```powershell
# View compiled DLL version
[System.Reflection.Assembly]::LoadFrom(".\WinUI\bin\Release\net10-windows\WinUI.exe").GetName().Version

# Output should be: 0.1.0.0
```

---

## 📊 Practical Examples

### Scenario 1: Normal Development Flow

```bash
# 1. Check current tag
git tag
# Output: v0.1.0

# 2. Work on new feature
git add .
git commit -m "feat: Add lap counter"
# → Version is now: 0.1.0-preview.1

# 3. Another commit
git add .
git commit -m "feat: Add statistics"
# → Version is now: 0.1.0-preview.2

# 4. Plan release - create new tag
git tag -a v0.2.0 -m "Release v0.2.0: New Statistics Feature"
# → Version is now: 0.2.0

# 5. After release, more commits
git commit -m "chore: Update deps"
# → Version is now: 0.2.0-preview.1
```

### Scenario 2: Bugfix Release

```bash
# Current: Tag v0.2.0 exists
# Bugfix needed:

git tag -a v0.2.1 -m "Release v0.2.1: Critical Z21 Fix"
git push origin v0.2.1

# New version: 0.2.1 ✅
```

### Scenario 3: Major Release (Breaking Changes)

```bash
# Current: v0.5.0
# API completely redesigned:

git tag -a v1.0.0 -m "Release v1.0.0: Major API Redesign"

# New version: 1.0.0 ✅
# Signals: Breaking changes!
```

---

## 🔗 GitHub & Automation

### GitHub Actions with MinVer

**`.github/workflows/release.yml`:**

```yaml
name: Create Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10'
      
      - name: Build
        run: dotnet build -c Release
      
      - name: Get Version
        id: version
        run: |
          VERSION=$(git describe --tags --abbrev=0)
          echo "version=${VERSION}" >> $GITHUB_OUTPUT
      
      - name: Create GitHub Release
        uses: actions/create-release@v1
        with:
          tag_name: ${{ steps.version.outputs.version }}
          release_name: Release ${{ steps.version.outputs.version }}
```

---

## 🐛 MinVer Troubleshooting

### Problem: "MinVer: No tags found"

```bash
# Check: Are there any tags?
git tag

# If empty: Create first tag
git tag -a v0.1.0 -m "Initial release"
git push origin v0.1.0
```

### Problem: Version is always "0.0.0"

```bash
# 1. Ensure fetch with history:
git fetch --tags

# 2. Rebuild:
dotnet clean
dotnet build -c Release
```

### Problem: "Invalid tag format"

MinVer expects tags in format `v<MAJOR>.<MINOR>.<PATCH>`:

```bash
# ✅ Correct
git tag v0.1.0
git tag v1.0.0
git tag v0.2.1

# ❌ Wrong
git tag 0.1.0          # Missing 'v' prefix
git tag release-1.0.0  # Wrong format
git tag v0.1           # Missing PATCH
```

### Problem: Tag already exists

```bash
# Delete and recreate
git tag -d v0.1.0           # Delete locally
git push origin --delete v0.1.0  # Delete remotely
git tag -a v0.1.0 -m "..."  # Recreate
git push origin v0.1.0
```

---

## 📚 Additional Resources

- **MinVer GitHub:** https://github.com/adamralph/minver
- **Semantic Versioning:** https://semver.org/
- **SemVer for Git:** https://git-scm.com/book/en/v2/Git-Basics-Tagging

---

## ✅ Checklist: MinVer Implementation

```
[ ] 1. Add MinVer NuGet package to Directory.Build.props
[ ] 2. Create version.json in root
[ ] 3. Clean up Directory.Build.props (remove hardcoded versions)
[ ] 4. Create first Git tag: git tag -a v0.1.0
[ ] 5. Test build: dotnet build -c Release
[ ] 6. Verify version in compiled DLL
[ ] 7. Configure GitHub Actions for Auto-Release (optional)
[ ] 8. Update documentation (README.md / docs)
```

---

*MinVer makes versioning automatic & error-free - configure once, forget forever! 🚀*
