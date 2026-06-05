---
description: Generate docs/CHANGELOG.md using git-cliff and Conventional Commits
---

# Changelog generieren (git-cliff)

## Voraussetzungen

- **git-cliff** installieren: `choco install git-cliff` (Win) oder `cargo install git-cliff`
- MinVer-Tag `v*.*.*` oder `version.json` als Fallback

## Lokale Befehle

```powershell
# Preview aller Versionen
git cliff

# Unreleased-Änderungen previewen
git cliff --unreleased

# CHANGELOG.md aktualisieren (prepended)
git cliff --unreleased -o docs/CHANGELOG.md

# Vollstaendige Neugenerierung
git cliff -o docs/CHANGELOG.md
```

## CI

Die Release-Pipeline `.azure-pipelines/release.yml` fuehrt `git-cliff -o docs/CHANGELOG.md` automatisch aus.
