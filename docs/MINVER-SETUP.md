# 🔢 MinVer: Versioning for MOBAflow

**Status:** ✅ Enabled for all .NET projects  
**Baseline Version:** 0.1.x  
**Last Updated:** February 2026

---

## 📖 What is MinVer?

**MinVer** is a .NET tool for **automatic Semantic Versioning** based on Git tags.

### 🎯 The Problem (without MinVer)

Früher war die Version in `Directory.Build.props` hart codiert und musste bei jedem Release manuell hochgezogen werden. Das war fehleranfällig und bot keine sinnvollen Preview‑Versionen.

### ✅ The Solution (with MinVer)

```powershell
git tag -s 0.1.0 -m "Release 0.1.0"
# MinVer automatically reads from this tag!
# → Version = 0.1.0 ✅
```

Mit jedem Commit nach dem Tag:
```
0.1.1-alpha.0.5+g1a2b3c4d  (automatisch berechnet)
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
|----------|----------------|---------|
| Create tag | `1.0.0` | Releases |
| Commits after tag | `1.0.1-alpha.0.N` | Development |
| Build metadata | `+g<commit>` | Git commit |

---

## 🚀 Installation & Setup

### Step 1: Central NuGet Package Management

MOBAflow verwendet **Central Package Management (CPM)**:

- `Directory.Packages.props` definiert die Version:

```xml
<ItemGroup>
  …
  <PackageVersion Include="MinVer" Version="7.0.0" />
</ItemGroup>
```

- `Directory.Build.props` referenziert MinVer für alle Projekte:

```xml
<ItemGroup>
  <PackageReference Include="MinVer" PrivateAssets="all" />
</ItemGroup>
```

### Step 2: Configure MinVer in Directory.Build.props

In `Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Use tags like 0.1.0 (no 'v' prefix) -->
  <MinVerTagPrefix></MinVerTagPrefix>
  <!-- Baseline for tag-less builds -->
  <MinVerMajorMinor>0.1</MinVerMajorMinor>
  <!-- Pre-release identifier for commits after the last tag -->
  <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

### Step 3: Create First Tag

```bash
cd C:\Repos\ahuelsmann\MOBAflow

# Create tag (local, signed)
git tag -s 0.1.0 -m "Release 0.1.0"

# Push to remote (GitHub/AzDo)
git push origin 0.1.0
```

### Step 4: Test Build

```bash
# Build
dotnet build -c Release

# Check version (in DLL properties)
```

**Verification with PowerShell:**
```powershell
# View compiled DLL version
[System.Reflection.Assembly]::LoadFrom(".\MOBAflow\bin\Release\net10.0-windows10.0.22621.0\win-x64\MOBAflow.exe").GetName().Version

# Output should be: 0.1.0.0
```

---

## 📊 Practical Examples

### Scenario 1: Normal Development Flow (0.1.x)

```bash
# 1. Check current tag
git tag
# Output: 0.1.0

# 2. Work on new feature (0.1.1-alpha.0.1, …)
git add .
git commit -m "feat: Add lap counter"
# → Version is now: 0.1.1-alpha.0.1

# 3. Another commit
git add .
git commit -m "feat: Add statistics"
# → Version is now: 0.1.1-alpha.0.2

# 4. Plan release - create new tag
git tag -s 0.1.1 -m "Release 0.1.1: New Statistics Feature"
# → Version is now: 0.1.1

# 5. After release, more commits
git commit -m "chore: Update deps"
# → Version is now: 0.2.0-preview.1
```

### Scenario 2: Bugfix Release

```bash
# Current: Tag 0.2.0 exists
# Bugfix needed:

git tag -s 0.2.1 -m "Release 0.2.1: Critical Z21 Fix"
git push origin 0.2.1

# New version: 0.2.1 ✅
```

### Scenario 3: Major Release (Breaking Changes)

```bash
# Current: 0.5.0
# API completely redesigned:

git tag -s 1.0.0 -m "Release 1.0.0: Major API Redesign"

# New version: 1.0.0 ✅
# Signals: Breaking changes!
```

---

## Release automation

MOBAflow provides two deliberate release paths while the GitHub-native workflow is being evaluated:

- [`.azure-pipelines/quality.yml`](../.azure-pipelines/quality.yml) – PR quality gate (build, tests, SonarCloud)
- [`.azure-pipelines/release.yml`](../.azure-pipelines/release.yml) – manual release: MinVer version detection and `CHANGELOG.md` update via git-cliff
- [`.github/workflows/release-studio.yml`](../.github/workflows/release-studio.yml) – validates an existing signed tag, builds and tests MOBAflow, and creates a draft GitHub Release

See [`RELEASE-STUDIO.md`](RELEASE-STUDIO.md) for the maintainer checklist and safety model. The GitHub workflow does not create tags or publish releases automatically.

Git tags use **plain SemVer without `v` prefix** (e.g. `0.1.0`). The release pipeline installs `minver-cli` 7.x to match the NuGet package version.

### version.json vs Directory.Build.props

- [`version.json`](../version.json) sets MinVer defaults: `minimum` `0.1.0`, `prerelease` `preview`
- [`Directory.Build.props`](../Directory.Build.props) sets `MinVerDefaultPreReleaseIdentifiers` to `alpha.0` for builds without tags

Both are intentional; tagged releases use the tag version, commits after a tag use the alpha preview suffix from `Directory.Build.props`.

## 🐛 MinVer Troubleshooting

### Problem: "MinVer: No tags found"

```bash
# Check: Are there any tags?
git tag

# If empty: Create first tag
git tag -s 0.1.0 -m "Initial release"
git push origin 0.1.0
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

MOBAflow uses **plain SemVer tags without `v`‑Prefix**:

```bash
# ✅ Correct
git tag 0.1.0
git tag 1.0.0
git tag 0.2.1

# ❌ Wrong
git tag v0.1.0          # We don't use 'v' prefix
git tag release-1.0.0   # Wrong format
git tag 0.1             # Missing PATCH
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

## ✅ Checklist: MinVer Implementation (MOBAflow)

```
[x] 1. Add MinVer NuGet package via Directory.Packages.props
[x] 2. Reference MinVer in Directory.Build.props (PrivateAssets=all)
[x] 3. Remove hardcoded versions from Directory.Build.props
[x] 4. Configure MinVerTagPrefix / MinVerMajorMinor / MinVerDefaultPreReleaseIdentifiers
[x] 5. Create first Git tag: git tag -s 0.1.0
[x] 6. Test build: dotnet build -c Release
[x] 7. Verify version in compiled DLL / EXE
[ ] 8. Configure CI release automation (optional)
```

---

*MinVer makes versioning automatic & error-free - configure once, forget forever! 🚀*
