---
description: 'Documentation and repository configuration checklist'
applyTo: '**'
---

# 📄 Dokumentation & Repository

> **Letztes Update:** 2026-01-19

---

## Dokumentation

| Status | Datei | Details |
|--------|-------|---------|
| ✅ | README.md | Features, Getting Started, Architecture |
| ✅ | LICENSE | MIT License |
| ✅ | THIRD-PARTY-NOTICES.md | Third-party attributions |
| ✅ | docs/ARCHITECTURE.md | Clean Architecture Dokumentation |
| ✅ | CONTRIBUTING.md | Contribution Guidelines (04.02.2025) |
| ✅ | docs/wiki/AZURE-SPEECH-SETUP.md | Azure Speech Setup Guide |
| ✅ | CODE_OF_CONDUCT.md | Community Guidelines ✓ |
| ✅ | SECURITY.md | GitHub Security Policy ✓ |
| ✅ | CHANGELOG.md | Versionshistorie (2026-01-19) ✓ |
| ✅ | CLA.md | Contributor License Agreement ✓ |

---

## Repository-Konfiguration

| Status | Datei | Details |
|--------|-------|---------|
| ✅ | .gitignore | Build-Outputs, User-spezifische Dateien |
| ✅ | .gitattributes | CRLF-Normalisierung (05.02.2025) |
| ✅ | .editorconfig | Code-Style Konsistenz |
| ⏳ | **GitHub Issue Templates** | `.github/ISSUE_TEMPLATE/` erstellen |
| ⏳ | **GitHub Actions CI/CD** | Workflow für Build + Test |
| ⏳ | **Dependabot Config** | Automatische Dependency-Updates |

---

## Code-Qualität

| Status | Aufgabe | Details |
|--------|---------|---------|
| ⏳ | EnableNETAnalyzers | `<EnableNETAnalyzers>true</EnableNETAnalyzers>` |
| ⏳ | Nullable Reference Types | Durchgängig aktiviert prüfen |
| ⏳ | ReSharper 0-Warnings | Alle Warnungen beheben |
| ⏳ | Unused Usings entfernen | IDE-Analyse durchführen |

---

## Potenzielle Duplikate (Code-Bereinigung)

| Status | Problem | Empfehlung |
|--------|---------|------------|
| ⏳ | `WinUI\Configuration\AppSettings.cs` vs `Common\Configuration\AppSettings.cs` | Konsolidieren |
| ⏳ | `SpeakerEngineFactory` + direkte DI-Registration | Factory ODER DI wählen |

---

## Tests

| Status | Aufgabe | Details |
|--------|---------|---------|
| ✅ | Test-Projekt vorhanden | `Test\Test.csproj` mit NUnit |
| ⏳ | Test-Coverage prüfen | Z21, JourneyManager, WorkflowService |
| ⏳ | CI-Integration | Tests in GitHub Actions |

---

*Teil von: [.copilot-todos.md](../.copilot-todos.md)*
