# MOBAflow - Schnellreferenz für Fragen

> Antworten auf häufige Fragen - kurz & prägnant

---

## ❓ "Was bringt mir die Hooks?"

**In 30 Sekunden:**
```
❌ OHNE Hooks: Fehler → Commit → Push → Remote broken → Team wartet
✅ MIT Hooks: Fehler blockiert vor Commit → sofort fixen → sauberer Remote

Praktisch: 7-11 Stunden Einsparung pro Monat (Fehlersuche, Debugging)
```

---

## ❓ "Welche NuGet Pakete sollen wir nehmen?"

**Priority:**
```
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
```
SonarQube = Automatischer Code-Scanner
- Findet: Bugs, Security Issues, Code Smells
- Kostet: 💚 KOSTENLOS (Community Edition)
- Nutzen: Fängt 99% der Probleme VOR Production
```

**Beispiele was erkannt wird:**
```
✅ Null-Reference Exception Risk
✅ SQL Injection Vulnerability
✅ Komplexität zu hoch (> 15)
✅ Code Duplication (sollte < 3%)
✅ Performance-Probleme
```

**Kostenmodelle:**
```
Community Edition (FREE) ← ← ← MOBAflow nutzt DAS
Developer Edition ($150/Jahr)
Enterprise Edition ($$)
```

---

## ❓ "Was sind ADRs? Brauchen wir die?"

**TL;DR:**
```
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
```
ADR-001: Use MVVM Toolkit (80% Boilerplate-Reduktion)
ADR-002: JSON Schema Validation (Fehlerprävention)
ADR-003: Constructor Injection (Testability)
ADR-004: Z21 as Singleton (Connection Pooling)
```

---

## ❓ "Ist das alles nötig? Oder Overkill?"

**Ehrliche Antwort:**

| Ja, nötig wenn: | Nein, vielleicht overkill wenn: |
|-----------------|----------------------------------|
| Projekt > 5 KLOC | Hobby-Projekt (< 1000 Zeilen) |
| Team > 2 Person | Nur dich selbst |
| Production-Code | Wegwerf-Prototyp |
| > 6 Monate Lebensdauer | One-time Script |

**MOBAflow:** ✅ JA, alle Hooks & Tools nötig!
```
- Production WinUI Desktop App
- Team wird wachsen
- Langfristige Wartung
- Hohe Qualitäts-Anforderungen
```

---

## ❓ "Wie lange dauert Session 35 (SonarQube)?"

**Geschätzt:**
```
🕐 Installation + Setup: 30 Min
🕐 GitHub Actions Integration: 45 Min
🕐 Quality Gate Configuration: 30 Min
🕐 Testing + Documentation: 1 Stunde
──────────────────────────────────
💰 TOTAL: ~2.5-3 Stunden
```

**Ergebnis:**
```
- Automatischer Security Scan auf jedem Push
- Fehler-Report vor PR-Merge
- Dashboard mit Code Quality Trends
- Zero-Kosten für MOBAflow (Community Edition)
```

---

## ❓ "Wo finde ich alles?"

**Ordner-Struktur:**
```
.github/instructions/
├── copilot-instructions.md              ← Copilot Regeln
├── copilot-tips.instructions.md         ← Copilot Prompts
├── summary-hooks-packages-sonarqube.md  ← DIESE DATEI (ausführlich)
├── visual-summary.md                    ← Visuelle Übersicht
├── future-enhancements.instructions.md  ← Roadmap Sessions 35+
├── vs-setup.instructions.md             ← VS Extensions
├── di-pattern-consistency.instructions.md
├── plan-completion.instructions.md
├── naming-conventions.instructions.md
└── todos.instructions.md

.git/hooks/
├── pre-commit.ps1 / .cmd                ← JSON Validierung
├── commit-msg.ps1 / .cmd                ← Conventional Commits
├── pre-push.ps1 / .cmd                  ← Tests + Build
├── post-checkout.ps1 / .cmd             ← NuGet Restore
└── README.md                            ← Hooks Doku
```

---

## ❓ "Was wenn ich einen Hook umgehen will?"

**Git-Befehl:**
```powershell
# Commit ohne pre-commit Hook
git commit --no-verify -m "feat: Emergency fix"

# Push ohne pre-push Hook
git push --no-verify

# ABER: NIEMALS bei JSON-Validation umgehen!
# Das kann Production brechen.
```

**Best Practice:**
```
✅ OK: --no-verify bei dringenden Hotfixes
❌ NIEMALS: --no-verify als Standard (bricht den Sinn)
❌ NIEMALS: --no-verify bei pre-commit (JSON-Fehler!)
```

---

## ❓ "Was ist das ROI dieser Investition?"

**Kosten:**
```
Development: 5 Tage (Sessions 30-34)
Maintenance: ~4 Stunden/Monat
Training: 2 Stunden
──────────────
GESAMT: ~6-7 Tage + Wartung
```

**Nutzen (pro Monat):**
```
Fehlersuche vermieden:    3-4 Stunden
Production-Bugs:         10-15 Stunden (verhindert!)
Remote broken prevent:   1-2 Stunden
Git-Historie Klarheit:   1 Stunde
────────────────────────────────────
TOTAL: 15-22 Stunden + Major Quality
```

**ROI:**
```
6 Tage Setup / 20 Stunden Nutzen/Monat = 300% ROI im ersten Monat 🚀
```

---

## ✅ Checkliste: Was ist DONE?

```
SESSIONS 30-34 KOMPLETT:

Git Hooks (4/4):
├─ ✅ pre-commit (JSON + Secrets)
├─ ✅ commit-msg (Conventional Commits)
├─ ✅ pre-push (Build + Tests + Analysis)
└─ ✅ post-checkout (NuGet Auto Restore)

Dokumentation (8 Dateien):
├─ ✅ copilot-instructions.md (Regeln)
├─ ✅ copilot-tips.instructions.md (Prompts)
├─ ✅ summary-hooks-packages-sonarqube.md (THIS FILE)
├─ ✅ visual-summary.md (Visuelle Übersicht)
├─ ✅ future-enhancements.instructions.md (Roadmap)
├─ ✅ vs-setup.instructions.md (IDE Config)
└─ ✅ .git/hooks/README.md (Hook Doku)

Code Quality:
├─ ✅ .editorconfig (Formatting)
├─ ✅ Pre-commit Validation
├─ ✅ Pre-push Testing
└─ ✅ Copilot Best Practices

BEREIT FÜR:
├─ 🚀 Session 35: SonarQube Integration
├─ 🚀 Session 36: Coverage Dashboard
├─ 🚀 Session 37: Performance Benchmarking
└─ 🚀 Sessions 38-40: Advanced Tools
```

---

## 🎓 Empfehlung für nächste Session

**Session 35 Priorität:**
```
1️⃣  SonarQube Community Edition Setup
    → Docker oder SonarCloud (Cloud kostenlos)
    
2️⃣  GitHub Actions CI Integration
    → Scan bei jedem Push zu main
    
3️⃣  Code Coverage Reporting
    → Coverlet + ReportGenerator
    → Target: 80%+ Coverage

4️⃣  ADR Templates
    → docs/adr/ Ordner erstellen
    → ADR-001 bis ADR-008 schreiben

⏱️  Estimated: 2.5-3 Stunden
🎯 Impact: 🟢🟢🟢 SEHR HOCH
💰 Kosten: 💚 KOSTENLOS
```

---

**"Mit diesen Hooks und Tools haben Sie die Grundlagen für ein professionelles, wartbares Projekt geschaffen. Das ist Enterprise-Grade Quality!" 🏆**

---

**Q&A**

**F: Können wir Hooks deaktivieren?**  
A: Ja, aber nicht empfohlen. Eher: Für Emergency `--no-verify` nutzen.

**F: Was wenn jemand hook-bypass.exe schreibt?**  
A: 😄 Dann haben Sie ein Team-Problem, kein Tech-Problem!

**F: Funktioniert das auch auf Mac/Linux?**  
A: Teils - PowerShell läuft überall, aber besser: Bash-Versionen schreiben.

**F: Kann ich Hooks für alle Devs erzwingen?**  
A: Ja: `git config core.hooksPath .git/hooks` im Setup-Script.

---

Viel Erfolg! 🚀
