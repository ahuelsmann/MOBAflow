# MOBAflow - Schnellreferenz für Fragen

> Antworten auf häufige Fragen - kurz & prägnant

---

## Git Hooks (planned)

Git hook scripts are **not checked into this repository**. Until they are added:

```powershell
dotnet test Test/Test.csproj
dotnet build MOBAflow/MOBAflow.csproj -c Release
```

JSON validation runs via the MSBuild `ValidateJsonConfiguration` target (Release builds;
skipped in FastDebug). Planned git hooks are documented in
[`future-enhancements.instructions.md`](./future-enhancements.instructions.md).

---

## ❓ "Welche NuGet Pakete sollen wir nehmen?"

**Priority:**

```text
🔴 JETZT SOFORT:
   - Polly (Resilience for Z21)
   - Serilog.Async (Non-blocking logging)
   - FluentValidation (Better validation)

🟡 BALD (Session 33):
   - MediatR (CQRS Pattern)
   - Mapster (Fast object mapping)

🟢 SPÄTER (Optional):
   - FluentAssertions (Readable tests)
   - BenchmarkDotNet (Performance)
```

Installation:

```powershell
dotnet add Backend package Polly
dotnet add WinUI package Serilog.Sinks.Async
dotnet add Common package FluentValidation
```

---

## ❓ "Was ist SonarQube? Kostenlos?"

**TL;DR:**

```text
SonarQube = Automatischer Code-Scanner
- Findet: Bugs, Security Issues, Code Smells
- Kostet: 💚 KOSTENLOS (Community Edition)
- Nutzen: Fängt 99% der Probleme VOR Production
```

**Beispiele was erkannt wird:**

```text
✅ Null-Reference Exception Risk
✅ SQL Injection Vulnerability
✅ Komplexität zu hoch (> 15)
✅ Code Duplication (sollte < 3%)
✅ Performance-Probleme
```

**Kostenmodelle:**

```text
Community Edition (FREE) ← ← ← MOBAflow nutzt DAS
Developer Edition ($150/Jahr)
Enterprise Edition ($$)
```

---

## ❓ "Was sind ADRs? Brauchen wir die?"

**TL;DR:**

```text
ADR = "Warum entschieden wir MVVM statt Prism?"

Dokumentiert:
- WAS wir entschieden
- WARUM wir es entschieden
- WELCHE Alternativen wir erwogen

Nutzen:
- 6 Monate später: Neuer Dev versteht Design
- Verhindert: Unnötige Diskussionen ("Warum MVVM?")
- Klarheit: Jede Architektur-Entscheidung begründet
```

**Beispiel MOBAflow ADRs:**

```text
ADR-001: Use MVVM Toolkit (80% Boilerplate-Reduktion)
ADR-002: JSON Schema Validation (Fehlerprävention)
ADR-003: Constructor Injection (Testability)
ADR-004: Z21 as Singleton (Connection Pooling)
```

---

## ❓ "Ist das alles nötig? Oder Overkill?"

**Ehrliche Antwort:**

- **Ja, nötig wenn:**
  - Projekt > 5 KLOC
  - Team > 2 Person
  - Production-Code
  - > 6 Monate Lebensdauer
- **Nein, vielleicht overkill wenn:**
  - Hobby-Projekt (< 1000 Zeilen)
  - Nur dich selbst
  - Wegwerf-Prototyp
  - One-time Script

**MOBAflow:** ✅ JA, alle Hooks & Tools nötig!

```text
- Production WinUI Desktop App
- Team wird wachsen
- Langfristige Wartung
- Hohe Qualitäts-Anforderungen
```

---

## ❓ "Wie lange dauert Session 35 (SonarQube)?"

**Geschätzt:**

```text
🕐 Installation + Setup: 30 Min
🕐 GitHub Actions Integration: 45 Min
🕐 Quality Gate Configuration: 30 Min
🕐 Testing + Documentation: 1 Stunde
──────────────────────────────────
💰 TOTAL: ~2.5-3 Stunden
```

**Ergebnis:**

```text
- Automatischer Security Scan auf jedem Push
- Fehler-Report vor PR-Merge
- Dashboard mit Code Quality Trends
- Zero-Kosten für MOBAflow (Community Edition)
```

---

## ❓ "Wo finde ich alles?"

**Ordner-Struktur:**

```text
.github/
├── copilot-instructions.md              ← Primary agent rules (repo root .github/)
└── instructions/
    ├── instructions-index.md              ← Index of instruction files
    ├── quick-reference.md                 ← This file
    ├── copilot-tips.instructions.md
    ├── future-enhancements.instructions.md
    ├── vs-setup.instructions.md
    └── … (layer-specific *.instructions.md)

.azure-pipelines/
├── quality.yml                            ← PR CI (build, test, SonarCloud)
└── release.yml                            ← Manual release (MinVer, git-cliff)
```

---

## Pre-commit validation (current)

Before committing, run manually:

```powershell
dotnet test Test/Test.csproj
dotnet build MOBAflow/MOBAflow.csproj -c Release
```

Use Conventional Commits (`feat:`, `fix:`, `docs:`, etc.) for git-cliff changelog generation.

---

## ❓ "Was ist das ROI dieser Investition?"

**Kosten:**

```text
Development: 5 Tage (Sessions 30-34)
Maintenance: ~4 Stunden/Monat
Training: 2 Stunden
──────────────
GESAMT: ~6-7 Tage + Wartung
```

**Nutzen (pro Monat):**

```text
Fehlersuche vermieden:    3-4 Stunden
Production-Bugs:         10-15 Stunden (verhindert!)
Remote broken prevent:   1-2 Stunden
Git-Historie Klarheit:   1 Stunde
────────────────────────────────────
TOTAL: 15-22 Stunden + Major Quality
```

**ROI:**

```text
6 Tage Setup / 20 Stunden Nutzen/Monat = 300% ROI im ersten Monat 🚀
```

---

## Quality checklist (current)

```text
Implemented:
├─ JSON validation (MSBuild ValidateJsonConfiguration.targets)
├─ Azure DevOps quality.yml (PR build, tests, SonarCloud)
├─ coverlet / dotnet-coverage runsettings
└─ Instruction docs (.github/copilot-instructions.md)

Planned:
├─ Git hooks (pre-commit, commit-msg, pre-push)
└─ Optional GitHub Actions workflows
```

Deprecated detail docs (historical): `visual-summary.md`, `summary-hooks-packages-sonarqube.md`

---

## Next improvements

See [`future-enhancements.instructions.md`](./future-enhancements.instructions.md). SonarCloud already runs in
`.azure-pipelines/quality.yml`; optional GitHub Actions workflows remain planned.

---

**F: Kann ich Hooks für alle Devs erzwingen?**  
A: Ja: `git config core.hooksPath .git/hooks` im Setup-Script.

---

Viel Erfolg! 🚀
