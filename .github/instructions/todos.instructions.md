---
description: 'MOBAflow open tasks and roadmap'
applyTo: '**'
---

# MOBAflow TODOs & Roadmap

> Last Updated: February 2026 (GitHub Preparation Session)

---

## 🎯 GITHUB MIGRATION (In Progress)

### Legal Preparation
- [x] Hardware-Disclaimer.md created
- [x] README.md updated (with security notices)
- [x] Wiki reviewed & Installation.md added
- [ ] Create GitHub Repository and make public
- [ ] Configure both remotes locally (azure + github)
- [ ] Create first Git tags (v0.1.0) and push to GitHub

### Versioning with MinVer
- [x] MinVer Documentation (docs/MINVER-SETUP.md) created
- [x] Directory.Build.props configured for MinVer
- [x] version.json created
- [x] First Git tag v0.1.0 created (locally)
- [ ] Push Git tag to Azure DevOps: `git push azure v0.1.0`
- [ ] Push Git tag to GitHub: `git push github v0.1.0`
- [ ] Integrate MinVer into CI/CD pipelines

### Dual-Repository Setup (Option D)
- [x] Dual-repo strategy documented (docs/DUAL-REPO-STRATEGY.md)
- [ ] Create GitHub Repository on GitHub.com
- [ ] Add GitHub remote locally
- [ ] Push code initially to GitHub
- [ ] Create GitHub Actions sync workflow (`.github/workflows/sync-to-azdo.yml`)
- [ ] Create sync-all.ps1 script for manual syncs
- [ ] Store GitHub credentials/PAT in GitHub Secrets
- [ ] Store Azure DevOps PAT in GitHub Secrets
- [ ] Document branching strategy (main, develop, feature/*, commercial/*)

### GitHub Actions & CI/CD
- [ ] Create build workflow (`.github/workflows/build.yml`)
- [ ] Create test workflow (`.github/workflows/test.yml`)
- [ ] Create release workflow (automatic on Git tags)
- [ ] Add code quality checks (ReSharper, StyleCop)
- [ ] Add security scanning (Dependabot, GHSA)

### GitHub Configuration
- [ ] Set up branch protection rules for main
- [ ] Create CODEOWNERS file (`.github/CODEOWNERS`)
- [ ] Create pull request template (`.github/pull_request_template.md`)
- [ ] Create issue templates:
  - [ ] Bug Report Template
  - [ ] Feature Request Template
  - [ ] Community Question Template
- [ ] Enable GitHub Discussions (for community support)
- [ ] Set up GitHub Pages / documentation site (optional)

### Documentation Update
- [ ] Verify README.md quickstart links
- [ ] Update wiki INDEX.md completely
- [ ] Update all internal links (AzDo → GitHub)
- [ ] Adapt CONTRIBUTING.md for GitHub

---

## 📋 BACKLOG (Existing Tasks)

### 0. UI Consistency & Refactoring

- [ ] Validate SignalBox refactor (SbElement direct binding, canvas interaction validation)
- [ ] Regularly align DI registrations across all UI projects (WinUI/WebApp/ReactApp/MAUI)
- [ ] Replace remaining hardcoded colors in UI components (use ThemeResources/SkinPalette)

### 1. Add Missing Track Types by Developers

- [ ] All track types from Piko A track assortment (codes) must be implemented
- [ ] Track libraries for additional manufacturers planned:
  - [ ] TrackLibrary.RocoLine
  - [ ] TrackLibrary.Tillig
  - [ ] TrackLibrary.Marklin

### 2. Automated Setup Scripts (v0.2.0+)

- [ ] PowerShell setup scripts for Windows
- [ ] Docker support for web apps
- [ ] Windows MSI installer
- [ ] Azure Speech auto-setup

### 3. Commercial Features (after GitHub launch)

- [ ] Finalize plugin system
- [ ] Prepare commercial plugin marketplace
- [ ] Implement license management
- [ ] Develop premium features in private AzDo branch
- [ ] Test private branch strategy in action

### 4. Testing & Quality (v0.2.0)

- [ ] Increase unit test coverage to 80%+
- [ ] Write integration tests for Z21 communication
- [ ] Add performance tests for track plan editor
- [ ] Conduct security audit

---

## 🎬 GO-LIVE CHECKLIST (End of This Session)

```
PRE-LAUNCH:
[ ] MinVer fully configured
[ ] Git tags (v0.1.0) created
[ ] Hardware Disclaimer online
[ ] Wiki updated
[ ] GitHub remote prepared

LAUNCH DAY:
[ ] GitHub Repo public
[ ] Push all branches to GitHub
[ ] Enable GitHub Actions workflows
[ ] Configure Azure DevOps as mirror
[ ] Update team documentation
[ ] Prepare community announcement

POST-LAUNCH:
[ ] Enable GitHub Issues & Discussions
[ ] Configure Dependabot
[ ] Automate GitHub Releases
[ ] Set up monitoring & analytics
```

---

## 🔄 SYNC STRATEGY (After GitHub Launch)

**Daily:**
- [ ] GitHub → Azure DevOps auto-sync (via GitHub Actions)

**Weekly:**
- [ ] Manual review of commercial features (stay private)
- [ ] Review tag versions

**After Each Release:**
- [ ] Create release notes on GitHub
- [ ] Upload release assets (builds, docs)
- [ ] Notify community of new version

---

## 📝 Session Summary

**What was done in this session:**
1. ✅ Legal assessment completed
2. ✅ Hardware-Disclaimer.md created
3. ✅ README.md & Wiki updated (installation, security)
4. ✅ MinVer set up (automatic versioning)
5. ✅ Dual-repo strategy documented (Option D)
6. ✅ Documentation for all steps created

**Next Sessions:**
1. Create GitHub repo & public launch
2. GitHub Actions & CI/CD pipelines
3. Community management & release process

---

## 🚀 NEXT MILESTONE: GitHub Go-Live

**When:** This week  
**What:** Push MOBAflow open-source to GitHub  
**Status:** ✅ Ready!

**Commands Ready to Execute:**
```bash
# 1. Create GitHub repo (manual)
# 2. Add remote
git remote add github https://github.com/ahuelsmann/MOBAflow.git

# 3. Push all
git push github --all --tags

# 4. Configure branch protection (manual in UI)
# 5. Launch! 🚀
```

---

*Status: Ready for GitHub Migration! 🚀*
