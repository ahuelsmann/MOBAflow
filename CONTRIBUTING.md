# Contributing to MOBAflow

Vielen Dank für dein Interesse an MOBAflow! 🎉  
Dieses Dokument ist bewusst knapp gehalten und richtet sich primär an Entwickler:innen.  
Benutzer-Dokumentation findest du im Ordner `docs/wiki/`.

## 1. Getting Started

### Repository klonen und bauen

```bash
git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow

dotnet restore MOBAflow/MOBAflow.csproj
dotnet restore MOBApi/MOBApi.csproj
dotnet build MOBAflow/MOBAflow.csproj
```

Für schnelle lokale Compile-Checks der WinUI-App kannst du `FastDebug` verwenden:

```bash
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Diese Variante ist für Edit/Build-Zyklen gedacht. Für vollständige App-Starts und
Release-Validierung bleibt der normale Build maßgeblich. Details stehen in
`docs/BUILD-PERFORMANCE.md`.

### Tests ausführen

```bash
dotnet test Test/Test.csproj
```

## 2. Projektübersicht & wichtige Doku

- Architektur & Schichten: `docs/ARCHITECTURE.md`
- JSON-Validierung & Solution-Format: `docs/JSON-VALIDATION.md`
- Hardware‑ und Haftungshinweise: `docs/HARDWARE-DISCLAIMER.md`
- Drittanbieter-Lizenzen: `docs/THIRD-PARTY-NOTICES.md`
- Benutzer-Wiki: `docs/wiki/INDEX.md`

Bitte lies mindestens `README.md` und `docs/ARCHITECTURE.md`, bevor du größere Änderungen machst.

**Repository:** Code, Issues und Pull Requests auf [GitHub](https://github.com/ahuelsmann/MOBAflow).
GitHub Actions enthält die öffentlichen Qualitäts- und Release-Workflows;
die vorhandenen Azure-DevOps-Pipelines bleiben als zusätzlicher Build- und
Release-Weg erhalten.

## 3. Wie du beitragen kannst

- **Bugs melden / Features vorschlagen**
  - GitHub Issues: `https://github.com/ahuelsmann/MOBAflow/issues`
  - Möglichst mit:
    - Reproduktionsschritten
    - Erwartetem Verhalten
    - Tatsächlichem Verhalten
    - Log-Auszug (falls relevant, ohne Geheimnisse)

- **Pull Requests**
  1. Repository forken
  2. Feature-Branch anlegen (z. B. `feat/...`, `fix/...`)
  3. Änderungen inklusive Tests implementieren
  4. Relevante Projekt-Builds und `dotnet test Test/Test.csproj` lokal ausführen
  5. Relevante Doku aktualisieren (README / Wiki / docs)
  6. Pull Request gegen `main` eröffnen

Bitte beschreibe im PR kurz **Was** du geändert hast und **Warum**.

## 4. Coding-Guidelines (Kurzfassung)

Die vollständigen Regeln stehen in:

- `.github/copilot-instructions.md`
- `.github/instructions/*.instructions.md`

Kurz zusammengefasst:

- **Architektur**
  - Clean Architecture einhalten (`Domain` → `Backend/Common` → `SharedUI` → `MOBAflow/MOBAsmart/MOBApi`)
  - MVVM mit `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`)
  - Nur Konstruktor-Injektion, kein Service Locator

- **Async / Threading**
  - Keine `.Result` / `.Wait()` – immer `async/await`
  - UI-Updates nur über den jeweiligen Dispatcher / EventBus-Mechanismus

- **Stil**
  - `.editorconfig` einhalten (Formatierung über IDE)
  - Sinnvolle Namen statt `data`, `tmp`, `x`
  - Kleine, fokussierte Methoden (20–25 Zeilen, wenn möglich)
  - Öffentliche APIs mit XML-Dokumentation (`/// <summary>`)

- **Tests**
  - Neue Logik nach Möglichkeit mit Unit-Tests abdecken (NUnit)
  - Bestehende Tests nicht brechen

## 5. Dokumentation anpassen

- Benutzerrelevante Änderungen:
  - `README.md` und/oder passende Seite unter `docs/wiki/` aktualisieren
- Technische Änderungen:
  - Gegebenenfalls `docs/ARCHITECTURE.md`, `docs/JSON-VALIDATION.md` usw. ergänzen
- Drittanbieter / neue Pakete:
  - `docs/THIRD-PARTY-NOTICES.md` mit Lizenzinformationen nachziehen

## 6. Contributor License Agreement (CLA)

Für Beiträge an MOBAflow gilt das Contributor License Agreement:

- **Dokument:** `docs/legal/CLA.md`

Indem du einen Pull Request einreichst, bestätigst du, dass du die Bedingungen dieses CLA gelesen und akzeptiert hast.
Bei Fragen zur Lizenz oder zum CLA:

- Siehe `docs/legal/CLA.md`
- Oder erstelle ein Issue mit dem Label `cla-question`

---

Vielen Dank für deinen Beitrag zu MOBAflow! 🚂✨

