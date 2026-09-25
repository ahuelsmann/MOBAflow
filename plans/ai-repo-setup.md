# Implementierungsplan: KI-Repository-Setup

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Spec Kit**: Required
**Status**: Implementing
**Datum**: 2026-09-25
**Planungsbasis**: `github/main` bei `2b65379fb4f4c6cb23394ea9d44467c6801f027f`
**Planungsbranch**: `codex/issue-145-ai-repo-setup`

## Ziel und Umfang

Ein frischer Klon und jeder getrennte Git-Worktree sollen der KI dieselben verständlichen Projektregeln,
passenden Skills und verlässlichen Prüfabläufe bereitstellen. Die Einrichtung soll ohne Andreas' persönliche
Skillinstallation nachvollziehbar sein. Dateizugriffe müssen zum aktiven Arbeitsverzeichnis passen.

Dieser Plan verfolgt die Umsetzung. Er ersetzt weder die
Spec-Kit-Spezifikation noch deren Aufgabenliste. Der aktuelle Fortschritt ist unten und in der Spec-Kit-Aufgabenliste vermerkt.
Der ursprüngliche Plan wurde mit PR #150 veröffentlicht. Am 25.09.2026 wurde auf Benutzerwunsch die Prüfung
der GitHub-Pipelines als P7 ergänzt und P2 verständlicher beschrieben. Die Umsetzung wurde am 25.09.2026 freigegeben;
Issue #145 bleibt bis zur Abnahme offen. Spezifikation, Aufgaben und Nachweise liegen unter `specs/005-ai-repo-setup/`.

Im Umfang liegen `AGENTS.md`, README und Contributor-Einstieg, Fachanweisungen, Spec-Kit-Integration,
Repository-Skills, MCP-Konfiguration und deren strukturelle Validierung sowie die Prüfung der GitHub-Pipelines.

Nicht im Umfang liegen Produktfunktionen, Änderungen an Journey-/Workflow-Verhalten, App-Starts,
Hardwareaktionen, ein Austausch der bestehenden CI-Plattform oder eine pauschale Aktualisierung aller
Community-Skills. Persönliche Codex-Einstellungen und globale Skills werden nicht automatisch verändert.
Die optionale Einführung von `domain-modeling` bleibt eine spätere Entscheidung.

## Verifizierter Ausgangspunkt

- Das Audit vom 24.09.2026 untersuchte zunächst den lokalen Hauptcheckout bei `698c4c8b`; der ursprüngliche
  Plan beruhte auf `57b96c1f`. Am 25.09.2026 wurde er gegen GitHub `main` bei `d0ffddc4` abgeglichen.
  Die Planergänzung zu GitHub-Pipelines basiert auf dem anschließend aktualisierten Hauptstand `010caeb5`.
  Die folgenden Auditbefunde wurden dadurch nicht erneut vollständig geprüft.
- Bereits umgesetzt durch PR #138: Sonar-Codeanalyse läuft ausschließlich in GitHub CI. Lokale
  Sonar-/Vortex-Codeanalyse und deren Analysehooks sind ausgeschlossen; lokale Geheimnisscans bleiben erforderlich.
  Diese Regel gilt bereits in `AGENTS.md`, der Sonar-Anweisung, der Constitution und den lokalen Spec-Kit-Vorlagen.
- Bereits umgesetzt durch PR #134: Keine neuen Legacy-Pfade, Kompatibilitätsschichten oder Migrationen für
  abgelöste Produktmodelle; keine nicht angeforderten Zusatzmechanismen. Diese Vorgaben bleiben erhalten.
  P1 führt beide bereits geltenden Regeln nicht erneut ein, sondern prüft ihre konsistente Anwendung in weiteren
  Leitfäden und Skills. Das Aktualisieren von Entwicklungswerkzeugen ist keine Produktdatenmigration.
- Elf Skills sind unter `.agents/skills/` eingebunden: zehn Spec-Kit-Skills und `audit-code-quality`.
  Weitere 42 `SKILL.md` liegen unter `skills/`; dieses Verzeichnis ist kein standardmäßiger Codex-Skillpfad.
- Im Audit vom 24.09.2026 standen CLI und verwaltete Spec-Kit-Integration auf 1.0.4; der Integrationsstatus
  meldete keine fehlenden oder veränderten verwalteten Dateien. Die Repository-Integration ist auch bei `d0ffddc4`
  auf 1.0.4 festgelegt. Release 1.0.11 bleibt das im Audit ermittelte, noch nicht installierte Updateziel.
- Bei 41 Matt-Pocock-Skills wurden nach Normalisierung der Zeilenenden 30 Abweichungen, drei identische
  Dateien und acht im aktuellen Original nicht mehr vorhandene Skills gefunden. Vergleichsstand:
  `c55ee46073ed923f86ce59a5eb3b6d895095d1b7`. Abweichungen sind keine automatische Updatefreigabe.
- Für `windows-app-developer` ließ sich die ermittelte externe Originalquelle nicht öffentlich abrufen.
- Der Filesystem-MCP ist auf den Hauptcheckout festgelegt. `speckit-taskstoissues` erwartet `origin`,
  während dieses Repository `github` verwendet. Diese Remoteannahme besteht auch in der Vorlage von 1.0.11.
- Constitution und lokale Plan-/Aufgabenvorlagen enthalten weiterhin pauschale vollständige Testbefehle,
  obwohl `AGENTS.md` nach Änderungsumfang und Plattform unterscheidet.
- Beim Audit vom 24.09.2026 gab es fremde Änderungen an `.codex/config.toml` (`web_search = "live"`) und
  `MOBAflow/solution.json`. Sie wurden nicht in den Planungsworktree übernommen. Beim Abgleich am 25.09.2026
  war der Hauptcheckout sauber. Vor der späteren Umsetzung den dann aktuellen Zustand erneut prüfen;
  historische lokale Änderungen nicht aus diesem Plan rekonstruieren oder überschreiben.

## Vorarbeiten vor der Umsetzung

- [x] V1: Issue #145, aktuellen Hauptstand und parallele Änderungen an denselben Dateien abgleichen.
  Auf einer eigenen `codex/`-Branch in einem sauberen Worktree arbeiten; keine persönlichen Konfigurationen kopieren.
- [x] V2: Mit dem bestehenden Spec-Kit-Ablauf eine kollisionsfrei nummerierte Feature-Spezifikation unter
  `specs/NNN-ai-repo-setup/` erstellen. Issue #145 referenzieren. Den tatsächlichen Pfad anschließend hier eintragen.
  Anforderungen: Skill-Erkennung, Worktree-Isolation, Anweisungskonsistenz, reproduzierbare Updates,
  Offline-Prüfbarkeit und nachvollziehbare GitHub-Prüfabläufe.
- [x] V3: Spezifikation, technischer Plan, Aufgaben und Konsistenzanalyse abschließen. Offene technische Fragen
  durch Dokumentation und kleine lokale Proben klären; nur echte Umfangsentscheidungen an den Benutzer geben.
- [x] V4: Aktuelle offizielle Codex-/Copilot-Konfigurationsschemata und die Herkunft der zu übernehmenden Skills
  erneut prüfen. Die im Audit ermittelte Zielversion 1.0.11 festhalten; neuere Releases nicht stillschweigend übernehmen.

## Reihenfolge und Abhängigkeiten

| Paket | Priorität | Voraussetzung | Ergebnis |
| --- | --- | --- | --- |
| P1 Anweisungen vereinheitlichen | Hoch | V1-V4 | Eine eindeutige Projekt- und Prüfpolitik |
| P2 KI-Werkzeuge im richtigen Arbeitsordner einrichten | Hoch | P1 | Benötigte Werkzeuge greifen auf den aktiven Worktree zu |
| P3 Spec Kit aktualisieren | Hoch | P1 | Gepinnte Integration mit funktionierender Remoteauflösung |
| P4 Projektskills einbinden | Mittel | P1, P3 | Zwei gezielte Skills ohne Namenskollisionen |
| P5 Einstieg und Herkunft dokumentieren | Mittel | P2-P4 | Reproduzierbares Onboarding und nachvollziehbare Quellen |
| P7 GitHub-Pipelines prüfen | Hoch | P1 | Belegte Bewertung von Auslösern, Pflichtprüfungen und Laufzeiten |
| P6 Gesamtvalidierung abschließen | Hoch | P1-P5, P7 | Nachweise aus frischem Klon, Worktree und CI |

Prüfungen entstehen mit dem jeweiligen Paket. P6 führt die Nachweise zusammen und wiederholt bereits
bestandene Prüfungen nur bei zwischenzeitlichen Änderungen oder offenen Risiken.
P7 behält als nachträglich ergänztes Paket seine Nummer und wird vor dem Abschluss von P6 ausgewertet.

## P1: Anweisungen vereinheitlichen

**Dateien:** `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`,
`.specify/memory/constitution.md`, `.specify/templates/overrides/`, `CONTRIBUTING.md`.

- [x] P1.1: `AGENTS.md` als gemeinsame Quelle für Arbeitsablauf, Architekturgrenzen und Prüfauswahl erhalten.
  Copilot-Einstieg und Instruction-Index bleiben kurze Verweise. Kein vollständiges Regelwerk duplizieren.
- [x] P1.2: Constitution, Plan- und Aufgabenvorlagen an die bestehende Prüftabelle anpassen:
  Dokumentationsprüfungen für Dokumentation, betroffene Tests und Builds für Verhalten, breitere Prüfungen für
  gemeinsame Laufzeitgrenzen. Die Pflicht zu aussagekräftigen Regressionstests bleibt erhalten.
  Constitution-Version und Auswirkungsnotiz nach den vorhandenen Regeln aktualisieren.
- [x] P1.3: Die bereits umgesetzten Regeln aus PR #138 (Sonar ausschließlich über GitHub CI) und PR #134
  (keine neuen Legacy-/Migrationspfade oder nicht angeforderten Zusatzmechanismen) beibehalten.
  Offen bleibt die Prüfung des Spec-Kit-Leitfadens und importierter Skills auf entgegenstehende Anweisungen.
- [x] P1.4: Große Fluent-/Naming-Anweisungen auf MOBAflow-spezifische Entscheidungen konzentrieren.
  Wiederholte Beispiele bei Bedarf in verlinkte Referenzdokumente verschieben, vorhandene Referenzorte bevorzugen.
  Keine willkürliche Zeilenobergrenze einführen; relevante Architektur- und Sicherheitsregeln erhalten.
- [x] P1.5: Das Auto-Save-Beispiel mit dem tatsächlichen Abonnement-/Abmeldeverhalten abgleichen und durch einen
  korrekten Quellverweis oder ein vollständiges Muster ersetzen. Markdown-Codeblöcke der Kommentaranweisung korrigieren.
- [x] P1.6: Den Unterschied zwischen der mitgegebenen globalen Scanregel und der Repository-Regel dokumentieren.
  Eine lokale Projektdokumentation kann globale Vorgaben nicht abschalten. Eine Änderung außerhalb des Repositorys
  gegebenenfalls separat benennen; die Umsetzung hängt nicht davon ab, globale Einstellungen zu verändern.

**Abnahme:** Für Dokumentation, Backend-Fehlerbehebung und UI-Änderung lässt sich jeweils eine widerspruchsfreie
Prüfliste ableiten. Fachdateien haben passende Geltungsbereiche und gültige lokale Links. Historische Inhalte
werden nicht als aktuelle Pflichten dargestellt. Es werden keine Tests erstellt, die lediglich Dokumenttext nachbilden.

## P2: KI-Werkzeuge im richtigen Arbeitsordner einrichten

Gemeint sind zusätzliche Zugänge, über die ein KI-Assistent zum Beispiel Dateien lesen, suchen oder auf
GitHub zugreifen kann. MCP ist die technische Schnittstelle für solche Werkzeuge. Geprüft wird, welche
Zugänge tatsächlich gebraucht werden, wie sie starten und auf welchen Projektordner sie zugreifen.

Konkretes Beispiel aus dem Audit: Ein Dateizugriff ist fest auf den Hauptcheckout eingestellt. Arbeitet die
KI in einem getrennten Worktree, könnte sie dadurch Dateien aus dem falschen Arbeitsstand lesen oder ändern.
Benötigte Dateizugriffe sollen deshalb dem aktiven Worktree folgen. Doppelte oder ungenutzte Anbindungen
entfallen erst nach Prüfung ihres Einsatzes; erforderliche Zugänge bleiben erhalten. Dazu gehört auch die
Prüfung, ob die Azure-DevOps-Anbindung angesichts des GitHub-Umzugs noch gebraucht wird.

**Dateien:** `.codex/config.toml`, `.mcp.json`, `.codex/hooks.json`, vorhandenes Secrets-Hook-Skript;
ein Startskript nur, falls für einen tatsächlich benötigten MCP-Client erforderlich.

- [x] P2.1: Pro Client erfassen, welche Repository-Konfiguration er tatsächlich lädt und welche Werkzeuge bereits
  nativ verfügbar sind. Repository-Einrichtung von persönlichen Plugin-/Connector-Installationen unterscheiden.
- [x] P2.2: Azure DevOps aus der standardmäßigen Repository-MCP-Einrichtung entfernen, soweit kein aktueller
  Repository-Ablauf darauf angewiesen ist. Vorhandene Legacy-Builddefinitionen bleiben außerhalb dieses Pakets.
- [x] P2.3: Für Codex vorhandene native Datei-, Such- und Webwerkzeuge bevorzugen; redundante Filesystem-/Ripgrep-/Fetch-MCPs
  nur behalten, wenn ein konkreter Client sie benötigt. Keine neuen Server ohne nachgewiesenen Bedarf hinzufügen.
- [x] P2.4: Für verbleibende Dateiserver den erlaubten Wurzelpfad aus dem aktiven Worktree bestimmen.
  Nur vom Client unterstützte Variablen verwenden; andernfalls einen kleinen geprüften Starter einsetzen.
  Fehlender Projektkontext muss sichtbar scheitern, statt auf den Hauptcheckout zurückzufallen.
- [x] P2.5: Behaltene externe Server auf geprüfte Versionen festlegen und Voraussetzungen dokumentieren.
  Unbelegte Umgebungsvariablen nicht als Sicherheitskontrolle darstellen. Keine Tokens in Repository-Dateien.
- [x] P2.6: Den Secrets-Hook auf Pfadauflösung, unterstützte Betriebssysteme, fehlende Werkzeuge und sichtbare
  Fehler prüfen. Die bestehende Regel für nicht verfügbare Scanner erhalten. Hook-Vertrauen ist ein lokaler
  Einrichtungszustand und wird nicht allein aus dem Vorhandensein von `hooks.json` abgeleitet.

**Abnahme:** In zwei isolierten Test-Worktrees zeigt jeder konfigurierte Dateizugriff auf den jeweiligen
Worktree. Die Probe verwendet künstliche Markerdateien in temporären Repositories. Der Hauptcheckout wird
nicht als Schreibziel getestet. JSON/TOML sind gültig; erforderliche Startbefehle funktionieren auf den
ausdrücklich unterstützten Plattformen. Nicht verfügbare Plattformtests werden als offen ausgewiesen.

## P3: Spec Kit kontrolliert aktualisieren

**Dateien:** `.specify/`, `.agents/skills/speckit-*/`, `docs/SPEC-KIT.md`, gegebenenfalls gezielte Hilfsskripte.

- [x] P3.1: Vorherige CLI-/Integrationsversion, verwaltete Dateien, lokale Overrides und das dokumentierte
  `common.ps1`-Refactoring erfassen. Zunächst mit einer isolierten Installation der gepinnten Zielversion arbeiten.
  Eine persönliche CLI-Installation nicht als versteckte Voraussetzung verändern.
- [x] P3.2: Integration mit den unterstützten Aktualisierungsbefehlen auf 1.0.11 bringen. Constitution,
  MOBAflow-Vorlagen und bestehende Quellenanpassungen gezielt erhalten beziehungsweise neu anwenden.
  Änderungen an verwalteten Dateien und Manifesten mit dem vorgesehenen Verfahren nachvollziehbar halten.
  Keine Prüfsummen manipulieren, nur um einen sauberen Status vorzutäuschen.
- [x] P3.3: Für `speckit-taskstoissues` die tatsächliche GitHub-Remoteadresse verwenden. Zuerst den
  konfigurierten Tracking-Remote bevorzugen, andernfalls genau einen passenden GitHub-Remote auswählen.
  Bei Mehrdeutigkeit oder fehlendem GitHub-Remote verständlich abbrechen. Weder Name `origin` noch Host/Repository
  dürfen stillschweigend angenommen werden. Eine unterstützte lokale Vorlage/Anpassung samt Herkunft dokumentieren.
- [x] P3.4: Remoteauflösung mit `github`, `origin`, SSH-/HTTPS-Adressen, fehlendem Remote und mehreren Remotes
  gegen temporäre lokale Git-Repositories prüfen. Keine echten GitHub-Issues für Tests erzeugen.
- [x] P3.5: Integrationsstatus, PowerShell-Syntax, Vorlagenauflösung und Governance prüfen. Den eigenen
  Spec-Kit-Ablauf durch eine isolierte Probe testen; Feature-Metadaten der laufenden Aufgabe nicht überschreiben.

**Abnahme:** Zielversionen und lokale Anpassungen sind dokumentiert; keine unerklärten fehlenden, veränderten
oder ungeprüften verwalteten Dateien. Eine im vorgesehenen Verfahren registrierte lokale Anpassung ist zulässig.
Die Remote-Proben bestehen und die Vorlagen verwenden die in P1 beschlossene Prüfauswahl.

## P4: Zwei gezielte MOBAflow-Skills einbinden

**Neue Verzeichnisse:** `.agents/skills/mobaflow-code-review/`, `.agents/skills/mobaflow-diagnosing-bugs/`.
**Bestehender Skill:** `.agents/skills/audit-code-quality/`.

- [x] P4.1: Die geprüften Community-Vorlagen für Review und Diagnose als Ausgangspunkt auswählen und notwendige
  Begleitdateien mitnehmen. Herkunft, Lizenz und lokale Änderungen erfassen. Neue eindeutige Namen vermeiden
  Kollisionen mit persönlich installierten `code-review`- und `diagnosing-bugs`-Skills.
- [x] P4.2: `mobaflow-code-review` prüft einen festgelegten Diff gegen Anforderungen und relevante Projektregeln.
  Der normale Review verändert keine Dateien. Die Ausgabe nennt belegte Fehler mit Datei, Auswirkung und
  Prüfnachweis; eine fehlerfreie Prüfung erfindet keine Beanstandungen. Ein Review erfordert nicht automatisch
  mehrere Agenten, Installationen oder einen vollständigen Repository-Audit.
- [x] P4.3: `mobaflow-diagnosing-bugs` führt über Reproduktion, Eingrenzung, Hypothese und gezielte Validierung.
  Es nutzt vorhandene Fakes für Z21-/Runtime-Probleme und startet die App oder Hardware nicht ohne Freigabe.
  Die Anweisung berücksichtigt .NET 10, NUnit, PowerShell und die lokalen Architekturgrenzen.
- [x] P4.4: Kurze Beschreibungen mit klaren Auslösern und Abgrenzung verfassen. Routinemäßige Bestätigungen und
  fremde Werkzeugannahmen entfernen. Eine echte ungeklärte fachliche Entscheidung bleibt ein Rückfragegrund.
- [x] P4.5: `audit-code-quality` auf aktuelle CI-only-Sonar-Regeln und engere Auslöser prüfen. Seine aufwendige
  Komplettprüfung darf nicht durch eine gewöhnliche kleine Review-Anfrage ausgelöst werden.
  Eine vorhandene globale Namensdublette im Leitfaden kenntlich machen, ohne die globale Installation zu verändern.

**Abnahme:** Ein frischer Klon findet die beiden neuen Skills unter `.agents/skills/`, ohne persönliche
Installationen zu benötigen. Alle verwendeten Begleitdateien existieren. Je Skill wird eine typische Aufgabe
und eine Nicht-Auslöser-Aufgabe geprüft; sichere Diagnosen funktionieren mit künstlichen Fehlerfällen.

## P5: Einstieg und Skill-Herkunft dokumentieren

**Dateien:** `README.md`, `CONTRIBUTING.md`, `docs/AI-DEVELOPMENT.md` (neu), `docs/SPEC-KIT.md`,
`.agents/skills/sources.json` (neu), `skills/README.md` (neu).

- [x] P5.1: In README einen kurzen Einstieg zur KI-Entwicklung mit Links auf `AGENTS.md` und den Leitfaden ergänzen.
  Voraussetzungen und Builddetails nicht aus mehreren Dokumenten duplizieren.
- [x] P5.2: Der Leitfaden erklärt Ersteinrichtung, gezielte Skillaufrufe, persönliche gegenüber eingebundenen
  Skills, Worktrees, Prüfungen und den Aktualisierungsablauf. Beispiele müssen mit den tatsächlich verfügbaren
  Werkzeugen funktionieren. Die Hardwarefreigabe bleibt eindeutig.
- [x] P5.3: Ein kleines maschinenlesbares Quellenverzeichnis für aktive Projektskills anlegen: Name, Pfad,
  Quellen-URL oder `local`, feste Revision/Version, Prüfdatum und Verweis auf lokale Anpassungen/Lizenz.
  Vorhandene Spec-Kit-Manifeste referenzieren, statt deren Dateihashes in einem zweiten System zu duplizieren.
- [x] P5.4: Die übrige Sammlung unter `skills/` als Referenzbestand kennzeichnen und ihren Updatezustand erläutern.
  Persönliche, experimentelle und veraltete Skills werden nicht automatisch aktiviert oder ungeprüft gelöscht.
  Ungeklärte Quellen wie `windows-app-developer` werden als ungeklärt ausgewiesen.

**Abnahme:** Ein neuer Mitwirkender kann allein anhand der verlinkten Dokumentation die benötigten Skills
finden, ihre Herkunft erkennen und den passenden Prüfablauf wählen. Keine maschinenspezifischen Benutzerpfade
werden als allgemeine Installationsanleitung ausgegeben.

## P6: Validierung und Auslieferung

**Dateien:** vorhandene Prüfskripte in `scripts/`, `.github/workflows/quality.yml`, bei Bedarf ein kleiner
`Test-AiRepositorySetup.ps1`-Prüfer und zugehörige isolierte Funktionstests.

- [x] P6.1: Den vorhandenen Instruction-Check um strukturelle Prüfungen ergänzen: lokale Linkziele,
  erforderliche Skill-Metadaten, eindeutige aktive Skillnamen, vorhandene Quellenregistrierung und parsebare Konfiguration.
  Den Geltungsbereich auf aktive Anweisungen und Skills begrenzen. Keine bloßen Dokumenttext-Tests hinzufügen
  und keine automatische inhaltliche Widerspruchsfreiheit versprechen.
- [x] P6.2: Kleine Negativ-Fixtures für die neue Prüflogik verwenden: fehlender Link, doppelte Skillnamen,
  ungültige Konfiguration und falsche Worktreeauflösung müssen reproduzierbar scheitern.
  Bestehende Prüfer erweitern, bevor eine neue Infrastruktur oder Abhängigkeit eingeführt wird.
- [x] P6.3: Automatisierbare Prüfungen in den bestehenden Instruction-Consistency-CI-Job integrieren.
  Sie benötigen weder persönliche Zugangsdaten noch lokale Sonar-Codeanalyse oder einen laufenden MCP-Server.
- [ ] P6.4: In einem frischen Klon mit isoliertem Benutzerkontext und einem zweiten Worktree eine Agentenprobe
  durchführen: Dokumentationsaufgabe, begrenztes Review und simulierte Fehlerdiagnose. Skill-Erkennung, geladene
  Regeln, Arbeitsverzeichnis und ausgewählte Prüfungen protokollieren. Fehlende Anmeldung als offenen Nachweis melden.
- [ ] P6.5: Änderungen und Quellenanpassungen abschließend prüfen, Geheimnisscan und Zeilenendenkontrolle ausführen.
  Umsetzung als Draft-PR gegen den tatsächlichen aktuellen GitHub-Hauptstand veröffentlichen.
  Erst nach grünem SonarCloud-Ergebnis für den aktuellen PR-Commit und null OPEN/CONFIRMED-PR-Issues bereitstellen.

**Abnahme:** Alle anwendbaren lokalen Struktur- und Funktionstests bestehen. CI- und manuelle Agentenproben
werden getrennt ausgewiesen. Eine lediglich statisch gültige Hook-/MCP-Datei gilt nicht als erfolgreich aktivierter Dienst.

## P7: GitHub-Pipelines prüfen

**Quellen:** `.github/workflows/`, dort aufgerufene Skripte und Buildkonfigurationen, tatsächliche
GitHub-Actions-Läufe sowie Branchschutz/Rulesets und erforderliche Statusprüfungen auf GitHub.
Fehlende Leserechte werden als offene Prüfung ausgewiesen.

- [x] P7.1: Aktive Workflows und Jobs erfassen: Aufgabe, Auslöser (`pull_request`, `push`, manuell),
  Branch-/Pfadfilter, Abhängigkeiten und verwendete Runner. Dokumentieren, welche Prüfungen bei reinen
  Dokumentationsänderungen, gemeinsamem .NET-Code, WinUI und Android Release/AAB tatsächlich starten sollen.
- [x] P7.2: Workflow-Ergebnisse mit den auf GitHub verlangten Statusprüfungen abgleichen. Prüfen, ob Filter,
  bedingte Jobs oder geänderte Jobnamen Pflichtprüfungen dauerhaft ausstehend lassen oder unbeabsichtigt umgehen.
  SonarCloud und bestehende Qualitäts-/Analyzer-Grenzen erhalten; Prüfpflichten nicht zur Beschleunigung abschwächen.
- [x] P7.3: Repräsentative aktuelle und abgeschlossene Läufe anhand von Commit, Zeitstempeln und Logs untersuchen.
  Wartezeit auf Runner, laufende Arbeit und fehlenden Fortschritt unterscheiden; Job-Zeitlimits,
  Parallelitäts-/Abbruchregeln und Wiederholungen prüfen. Den vom Benutzer verlinkten
  [Android-Release-AAB-Job zu PR #154](https://github.com/ahuelsmann/MOBAflow/actions/runs/36130988570/job/108057952187?pr=154)
  als konkreten Untersuchungsfall einbeziehen. Der Link allein belegt weder einen Hänger noch den aktuellen Status.
- [x] P7.4: SDK-/Workload-Versionen, Restore-/Build-/Publish-Schritte, Cache-Schlüssel, Berechtigungen,
  Secret-Verwendung und Action-Versionen auf Reproduzierbarkeit und unnötige Arbeit prüfen.
  Für Android sicherstellen, dass Release/AAB und die Bundle-Prüfung tatsächlich ausgeführt werden;
  ein FastDebug-Build ersetzt diesen Nachweis nicht. Zugangsdaten nicht ausgeben.
- [x] P7.5: Ergebnisse mit Workflow-/Job-Verweis, beobachtetem Verhalten und konkreter Empfehlung festhalten.
  Den KI-Leitfaden um das Lesen der Pipeline-Ergebnisse und den Umgang mit ausstehenden oder fehlgeschlagenen
  Prüfungen ergänzen. Notwendige Pipelineänderungen als eigene überprüfbare Aufgaben festhalten;
  dieser Prüfauftrag löst keine pauschalen Workflowänderungen oder Jobabbrüche aus.

**Abnahme:** Eine Übersicht ordnet jedem Änderungstyp die erwarteten Prüfungen zu. Relevante Befunde sind
durch Konfiguration oder konkrete Läufe belegt; Laufzeitprobleme werden nicht allein aus dem Status
„läuft noch“ abgeleitet. Erfolgreiche, fehlgeschlagene, übersprungene und noch ausstehende Prüfungen werden
getrennt für den aktuellen PR-Commit ausgewiesen. Notwendige Folgearbeiten haben klare Abschlusskriterien.

## Prüfkommandos und Nachweise

Bestehende Einstiegspunkte, jeweils aus dem eigenen Worktree:

```powershell
pwsh -NoProfile -File scripts/Test-InstructionConsistency.ps1
pwsh -NoProfile -File scripts/Test-SpecKitGovernance.Tests.ps1
pwsh -NoProfile -File scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -ChangedFiles plans/ai-repo-setup.md
specify version
specify integration status
specify check
```

Bei der Umsetzung dem Governance-Prüfer alle betroffenen Plan-/Spec-Dateien übergeben oder den tatsächlichen
PR-Basisref verwenden. Neue Prüfer und Tests erst aufrufen, nachdem sie implementiert sind. Für alle geänderten
Textdateien den Zeilenendenprüfer mit `-Path` ausführen, nötigenfalls mit `-Fix`, anschließend erneut prüfen.
Vor einem angeforderten Commit zusätzlich `-Staged` prüfen; geänderte Dateien mit `sonar analyze secrets` scannen.

Die planmäßigen Änderungen betreffen keine Produktlogik. Dafür werden keine .NET-Builds oder App-Starts als
zusätzliche Pflicht eingeführt. Falls der tatsächliche Diff Produkt- oder Buildverhalten berührt, gilt die
Prüftabelle aus `AGENTS.md`; die Umfangserweiterung muss vorab sichtbar gemacht werden.

## Reviewbare Umsetzung und Rücknahme

Die Pakete werden in kleinen, zusammenhängenden Commits umgesetzt: gemeinsame Regeln, portable Konfiguration,
Spec-Kit-Aktualisierung, Projektskills, Einstieg/Quellen und abschließende CI-Anbindung. Zugehörige Prüfungen
gehören jeweils zum auslösenden Paket. Ein Draft-PR für Issue #145 hält die Abhängigkeiten sichtbar.

Bei fehlgeschlagener Spec-Kit-Aktualisierung werden ausschließlich die aufgabenbezogenen Änderungen am
eigenen Worktree auf den dokumentierten Ausgangspunkt zurückgeführt. Persönliche Installationen und fremde
Änderungen werden dabei nicht zurückgesetzt. Ist eine externe Quelle nicht erreichbar, bleibt der bisherige
Stand mit dokumentierter Einschränkung erhalten; es wird keine Ersatzversion aus unbekannter Herkunft installiert.

Abgeschlossen ist die Umsetzung erst nach Erfüllung aller Paketkriterien und dokumentierten offenen
Plattformgrenzen. Anschließend Issue #145 abschließen und diesen erledigten Plan entfernen. Die dauerhaft
gültige Anleitung bleibt in `AGENTS.md`, dem KI-Leitfaden und den Skills; Git-Historie und Issue bewahren den Verlauf.

## Quellen

- [Codex-Projektanweisungen](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Codex-Skills und Erkennung](https://learn.chatgpt.com/docs/build-skills)
- [Codex-Hooks und lokales Vertrauen](https://learn.chatgpt.com/docs/hooks)
- [Aktuelle Hinweise zu schlanken Skills und Anweisungen](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra)
- [GitHub-Copilot-Konfiguration](https://docs.github.com/en/copilot/reference/customization-cheat-sheet)
- [Spec Kit 1.0.11](https://github.com/github/spec-kit/releases/tag/v1.0.11)
- [Verglichener Community-Skillstand](https://github.com/mattpocock/skills/tree/c55ee46073ed923f86ce59a5eb3b6d895095d1b7)
- [Bereits umgesetzte Legacy- und Umfangsregeln aus PR #134](https://github.com/ahuelsmann/MOBAflow/pull/134)
- [Bereits umgesetzte Sonar-Regel aus PR #138](https://github.com/ahuelsmann/MOBAflow/pull/138)
