# Changelog

All notable changes to MOBAflow will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- GitHub migration, docs, MVVM & skin refactor
- add JSON solution file validation & schema
- Entferne appsettings.Development.json
- Verbesserungen an Sprachsynthese und Konfiguration
- Einführung von SpeakerEngineFactory für dynamische TTS
- UI-Thread-Sicherheit & SpeakerEngine-Umschaltung
- remove dev config template and README
- add session summary, remove PDF/docx and PDF tool
- Refactoring Grid, UiDispatcher & Audits
- Deferred Init & Z21-Protokoll vereinheitlicht
- DockingManager mit Tab-Grouping & .NET 10
- Visual Studio Docking & PropertyGrid
- Event-Handler bei Seitenladen registrieren
- Fahrbefehl-Bedingung angepasst & Terminal-Profil
- Auto-Hide & Collapsible Tabs für DockPanels
- Multiplexer- und Zugklassen-Integration
- Viessmann 5229/52292 Mapping auf turnout-Logik
- Multiplex, Docking-UI und DCC-Tests verbessert
- Z21 UDP-Receiver asynchronisiert
- JSON-Validierung & Projektprüfung integriert
- Kernregeln für MOBAflow in Cursor ergänzt
- Async-Initialisierung & Cleanup, Paketupdates
- Missing track codes added.
- Missing article codes added.
- [**breaking**] Editierbarer Gleisplan-Editor mit Drag&Drop
- Geometrie, Snapping & UI grundlegend überarbeitet
- GPU-Rendering für Track Plan integriert
- Fahrtenbuch-Service, LocomotivesPage & EventBus-UI
- zentrale Stammdaten & Funktionssymbol-Picker
- UI- und Konfigurationsanpassungen, Lokdaten aktualisiert
- Grid-System überarbeitet & Doku ergänzt
- Viessmann Multiplex-Signale zentral verwalten
- Projekt-Lokauswahl per ComboBox & Preset
- Azure DevOps als Hauptquelle für TODOs
- Bremse & Türfreigabe mit UI/Icons
- Multiplex-Signale konfigurierbar
- interne Sichtbarkeit, Docking-Refactoring, Konverter
- unify PR validation with coverage & SonarQube
- .mcp.json vereinfacht und hinzugefügt
- Azure DevOps MCP-Server erweitert, .mcp.json entfernt
- Signaladresse, Aspekt und Wert angepasst
- Signaladresse, Aspekt und Wert aktualisiert
- Versionierung & Doku-UX verbessert, Release-Pipeline
- Ports und Segment-IDs hinzugefügt
- XML-Dokumentation für Datenobjekte ergänzt
- Feature-Toggles jetzt dynamisch & InfoPage mit Markdown
- Dokumentation auf Englisch & SignalBoxPlan Update
- Events erweitert & Doku verbessert
- XML-Dokumentation für TTS-Engines ergänzt
- XML-Dokumentation für ViewModels & Services
- Async-Methoden zu TestDispatcher hinzugefügt
- Paketversionen aktualisiert für Kompatibilität
- AGENTS.md zum Projekt hinzugefügt
- Automatische Discovery & Client-Status
- zentrale Foto-Pfad-Logik & Discovery-Protokoll
- Fahrtenbuch entfernt, neue Wagen-Seiten & Splitter
- Grid-Spaltenbreiten persistent & flexibel
- Panel-State-Persistenz für Splitter-Seiten
- CommunityToolkit.WinUI/Labs hinzugefügt & Updates
- InjectedLayers-Konfiguration ergänzt
- Plattform-Umbenennung & Architektur-Update
- GridSplitter-Layout mit MVVM eingeführt
- GridSplitter-Layout modernisiert
- Enhance MCP server configuration with new services
- Update MCP server configuration with new commands and services
- Konsolidierung und Erweiterung der MCP-Server-Konfiguration
- CollapsibleColumn-Refactoring & statische Navigation
- JourneysPage-Layout & Suche vereinheitlicht
- JourneysPage-Layout auf weitere Seiten ausgerollt
- Docking-System & Panel-Layout für WinUI eingeführt
- Runtime-Boundary & Snapshot-Architektur
- Flexibles Fahrzeugmodell für Züge eingeführt
- Asynchrones Shutdown-Handling & UI-Verbesserungen
- Segment-Ports refaktoriert & Fahrzeugrichtung UI
- Docking-Layout-Architektur erneuert
- SonarQube-Integration in quality.yml aktiviert
- Auto-Hide, neue SignalBox-Controls, Snap/Validierung
- SonarCloud-Integration für statische Analyse
- Enterprise PR Reviewer Agent hinzugefügt
- SubnetCandidateBuilder für Netzwerkscan
- Zuverlässigere REST-API-Discovery im LAN
- Viessmann-Multiplex-Signale & Docking-API überarbeitet
- SignalBox-Layout überarbeitet, Refactoring
- Properties-Panel bindet direkt an SelectedProject
- [**breaking**] Runtime-Architektur, Logging & Payloads modernisiert
- using-Direktiven vereinheitlicht & Formatierung
- Namensräume vereinheitlicht & Converter public gemacht
- Update UI text to English and enhance settings for Copilot integration
- Enhance Track Plan functionality and UI interactions
- Add Z21 R-Bus feedback pulse visualization in TrackPlanPage
- Add Azure AI Vision integration with health monitoring and UI
- Update solution.json and UI components for improved layout and functionality
- Enhance track planning features and UI elements
- Add MOBAdisplay project with bitmap processing and UDP frame transmission
- Integrate SkiaSharp for bitmap processing and enhance frame transmission
- Add screenshots section to README for enhanced visual documentation
- Enhance asset management and update station data structure
- Add support for serial communication and enhance frame transmission options
- Add train destination display support and enhance layout persistence
- Add mandatory copyright header to all C# source files
- Add MatrixImage collection to Project and enhance 5x5 matrix editor UI
- Add Matrix and Display page feature toggles and reorganize navigation
- Add display configuration views and reorganize matrix page layout

### Fixed

- FAdr-Encoding & Signalsteuerung Z21-konform
- FAdr-Berechnung & Port korrigiert, Tests entfernt
- Korrektur von & zu &amp; in XML-Kommentaren
- Add default cases for DockPosition switch statements and improve null checks
- Testausführung auf spezifisches Projekt beschränkt
- Entferne --no-build aus dotnet test
- improve z21 subnet discovery on android startup
- Klammer ergänzt & DccSpeedSteps-Handling verbessert
- Ks-Signale: Zs3-Anzeige, Settings immer sichtbar, UI-Update

### Documentation

- Entferne NEURO-UI- und Azure Speech Debug-Doku
- Bedienungsanleitung für Multiplexer 5229 hinzugefügt
- Copilot-Doku, Quality-Checklisten & Roadmap erweitert
- Add quality framework documentation
- Projektübersicht und lokale Einstellungen hinzugefügt
- Session 34 abgeschlossen & Ausblick ergänzt
- major rewrite and restructure for clarity
- Doku-Update & MinVer-Versionierung
- MinVer-Doku & Tag-Konvention überarbeitet
- XML-Kommentare von Deutsch auf Englisch übersetzt
- XML-Kommentare und Dokumentation im Backend ergänzt
- Kommentare sprachlich vereinheitlicht
- Update quality framework documentation with new guidelines and best practices
- Enhance XML documentation for ProjectViewModel and SignalBoxRouteViewModel
- XML-Kommentare für DockingDropBehavior ergänzt
- Copilot-Instruktionen komplett überarbeitet

### Changed

- Plugin-System entfernt, Navigation modernisiert
- XAML vereinfacht, Layout & SVG verbessert
- Formatierung für bessere Lesbarkeit
- Details-Klasse konkret, JSON restrukturiert
- Solution Items umstrukturiert und bereinigt
- Events als record-Klassen mit Properties
- Entferne SpeakerEngines/Voices aus Project
- Directory and project structure reworked. Web applications removed. Projects renamed according to product names.
- MaxWidth entfernt, Layout vereinheitlicht
- Vereinheitlichung und Code-Cleanup
- consolidate UI/runtime cleanup and add characterization tests
- backend wave2/3, UI splits, JourneyManager guard + tests
- Ordnerstruktur & Deploy für MOBAsmart
- DI-Registrierung in App.xaml.cs zentralisiert
- Remove unused stack-mcp-server configuration from mcp.json
- Remove Azure AI Vision integration and related components
- Remove skin provider and related components
- Simplify namespace usage across multiple files
- Restructure station and platform management with master data support
- Remove serial transport support and add Wi-Fi provisioning to ESP32 display
- SVG handling revised
- Reorganize domain classes into subdirectories and enhance journey map UI
- Remove unused OverviewPage2.xaml view
- Move commands to ViewModel and enhance logging infrastructure
- Standardize layout persistence to use star values and enhance LED matrix interaction

### Tests

- MAUI Adapter-Test vorübergehend deaktiviert
- OutputSpeech-MinimalTest ignoriert fehlendes Audiogerät
- Fehlerbehandlung für Audio-Geräte verbessert
- Unit-Tests für Kernkomponenten hinzugefügt

### Chore

- reformat germany-stations.json for consistency
- plan-completion.instructions.md entfernt
- update comments to YAML-style in quality.yml
- comment out SonarQube steps in pipeline
- update .NET SDK to stable 10.0.103 in global.json
- reformat settings and clear completed todos
- Remove temp/ from repo and add temp/,tmp/ to gitignore
- Entferne JSON-Schema und Dev-Konfiguration
- Analysis/Program.cs von Kompilierung ausschließen
- Update MOBAdisplay project configuration and remove unused Program.cs
- Update package versions and clean up using statements
- Normalize whitespace and formatting in project files
- Add VS Code workspace exclusions and Windows WinUI tooling documentation

## [0.1.0] - 2026-02-04

### Added

- Topology-First-Architektur & SVG-Rendering
- Topology-First-Architektur & WorldTransform
- Topologie-Architektur modularisiert
- Projektstruktur bereinigt & Build-Probleme behoben
- C#-UI für Plugin & UDP Discovery für REST-API
- [**breaking**] Modernes Plugin-System & Splash für WinUI/MAUI
- Digitale Train Control Seite & Throttle
- Lokomotiv-Presets & UI-Redesign
- BacklightToggleButtonStyle & Speed-Presets-Redesign
- HelpPage (Wiki) & SAP-Transaktionsseite hinzugefügt
- MOBAflow Architektur, ERP & Stellwerk
- MOBAerp als Plugin, neues Statistics Plugin, TrackPlan-Verbesserungen
- Plugin-Navigation & Code-Mapping vereinfacht
- Modernisierung auf C# 12, Collection Expressions & UI
- Mehrere Stellwerk-Stile & UI-Redesign
- Neue TrackPlan-Architektur & SignalBox-Stile
- add WinUI track plan editor & geometry
- MOBAerp durch MOBAcmd ersetzt, Signalbox modernisiert
- implement auto-save for settings and solution
- App-Shell & Responsive Layout für MOBAflow
- AdaptivePanel entfernt, VSM empfohlen
- VisualStateManager responsive layout & docs
- enhance F0–F20 function button support
- .mcp.json mit Server-Konfiguration hinzugefügt
- VSM best practices, Ks signals, UX fixes
- Lokomotiv-Masterdaten & UI/Architektur-Refactoring
- Theme-System & neue TrainControl-Seiten
- Theme/Layout-Selector & VSM integriert
- Neue TrackPlanPage & modernes SignalBox-ESTW
- Skin-System eingeführt und UI modernisiert
- add Azure App Config & User Secrets support
- add XML docs to enums & default value tests
- Geometrie, Tests, Docs & CI Coverage
- SVG-Exporter für Gleisgeometrie hinzugefügt
- Y-Koordinaten-Fix & WL/WR-Templates ergänzt
- Geometrie-Validierung & SVG für WR-Oval
- Geometrie-Berechnung & SVG-Export verbessert
- Topology-First Rendering-Architektur
- Modularisierung & Ghost Placement für Gleise
- Entfernen der Datei TrackPlan.csproj
- Modularisierung und Refactoring der Codebasis
- TrackPlanPage fixes - build stability and UI handlers
- Implementierung von Designsystem & Effekten
- Phase 9 Neuro-UI, Snap, Animation, Docs
- neue Gleise, Code-Labels & flexibles Snapping
- Ruler-Feature (feste & verschiebbare Lineale)
- extract TrackLibrary.Base for reusable models
- migrate to POCO graph & service-based DI
- Dual-Port Hover-Feedback & DI-Refactoring
- Mauszeiger-Priorisierung & Topologie-Renderer
- Fluent-API für Topologie & WR-Fix
- Fluent Builder & SVG-Renderer für Piko A
- Verbindungen für WR explizit definiert
- Port-basierte Verkettung & SVG-Rendering
- Port-Striche & neue Gleistypen-Visualisierung
- Piko A Gleistypen & Quelle dokumentiert
- TrackPlan DI-Registrierung & JSON Strukturupdate
- DCC Speed Steps konfigurierbar & dynamische Skalen
- add tacho-indicator line band UI
- Violet entfernt, Orange/DarkOrange vereinheitlicht
- refactor TrainControlPage & add journey info
- improve accessibility & persist loco series
- optimize port lines & color schemes
- Z21-Backend & Amperemeter-Integration überarbeitet
- LocomotiveInfoCardControl & Exit-Icons ergänzt

### Fixed

- Convert all source files to UTF-8 with BOM for Visual Studio compatibility

### Documentation

- update instructions, lift terminal restriction
- expand and restructure copilot instructions
- VSM-Migration und Architektur-Doku aktualisiert
- Update README with current TrackPlan status and recent fixes
- Add Geometrie-Breakthrough analysis documentation
- Session 12 Changelog & Port C Fragen ergänzt
- unify code style & naming conventions repo-wide
- DI-Pattern-Guide ergänzt & Konsistenz verbessert

### Changed

- unify UI, remove old pages, add VM
- reformat CheckBox elements to single line
- Einheitliches Auto-Save-Pattern für ViewModels
- remove page persistence, improve UI & docs
- SignalBox-Elemente typisiert, Enum entfernt
- using-Direktiven und Namensräume bereinigt
- remove LocoInfoCard, improve logs & docs

### Tests

- Unit-Tests & WinUI.Controls-Projekt hinzugefügt

### Dependencies

- Update Maui.Controls und Swashbuckle

### Chore

- JSON- und XML-Dateien neu formatiert
- Changelog für Session 11/12 strukturiert

### Documentation

- Phase 9 Neuro-UI Design improvements

### TrackPlan.Import

- AnyRailLayout zu AnyRail refaktoriert

### TrainsPage

- Inventarverwaltung & Foto-Upload integriert

### UI

- Puls-Animation & Haptik, Audio-README zentralisiert

## [0.3-topology] - 2025-12-30

### Added

- introduce action system and enhance documentation
- add DataManager and improve station handling
- Echtzeit-Feedback-Monitoring (FeedbackApi + MOBAsmart)
- enable nullable types, refactor factories, add tests
- add Z21 system state handling with IUiDispatcher
- Hinzufügen von ProjectConfigurationPage
- Einführung der neuen OverviewPage und Navigation
- Einführung eines Tab-basierten CRUD-Editors
- Editor-Ansicht und Validierungslogik hinzugefügt
- add copyright headers and refactor settings
- Keepalive-Mechanismus für Z21 hinzugefügt
- Verbesserungen an UX, Barrierefreiheit und Leistung
- Verbesserungen bei UI und Auto-Load-Funktionalität
- Projekte und Editoren verbessert
- Gleisspannungssteuerung und Dokumentation erweitert
- Hintergrunddienste und neue Tests hinzugefügt
- Einführung neuer ViewModels und Refactoring
- [**breaking**] Refactor to Clean Architecture Phase 2
- [**breaking**] Refactor and enhance templates and CI pipeline
- Clean Architecture 99% - Architecture violation fixed and test migration 70% complete
- Implement Clean Architecture migration
- Refactor settings and enhance journey UI
- Verbesserte Workflow-Referenzen und JSON-Handling
- enforce Clean Architecture and centralize settings
- Überarbeitung der Architektur und UI-Verbesserungen
- UI-Optimierungen und Actions-Refactoring-Plan
- UI optimizations - layout improvements and bug fixes
- Add collapsible Workflows and Cities helpers to Properties panel
- PropertyGrid moved to MainWindow as fixed right panel
- Add FOSS documentation and improve ViewModels
- add new EditorPage with navigation support
- refactor EditorPage with new tab layout
- Verbesserungen für Aktionen, Drag & Drop, Layout
- [**breaking**] Migration von SelectorBar zu TabView
- Z21TrafficMonitor und UI-Filter hinzugefügt
- Activate v3.0 Ultra-Compact Instructions with Auto Context Loading- Replace copilot-instructions.md with ultra-compact version- Add Red Flags checklist (10 critical checks)- Add Context-Aware Loading table (automatic trigger on keywords)- Add Past Mistakes summary (PropertyGrid, Nested Objects, etc.)- Add 5-Step Analysis methodology with PowerShell commands- Backup old version as copilot-instructions-v2-OLD.md- Keep v3-BACKUP for referenceBenefits:- Red Flags always visible (not buried in 1500 lines)- Automatic loading of layer-specific instructions- Token-efficient (~20KB vs ~50KB)- Past mistakes summary prevents repeating errors- Quick architecture reference (5 lines per layer)See: docs/INSTRUCTIONS-CONSOLIDATION-SUMMARY.md
- modernize property panel & enable DI for pages
- Cleanup beim Beenden & UI-Redesign
- add IsConnected to UDP, improve station UX, update JSON
- add track visualization & journey map pages
- add FeedbackPointOnTrack entity to Project
- event-driven track plan sync & improved PS docs
- add FeedbackPointsPage & DI pattern refactor
- Monitor-Seite für Debug- und Traffic-Analyse
- Systemstatus-Polling konfigurierbar gemacht
- DCC-Decoder & UI für Lokbefehle integriert
- Feedback Points Verwaltung entfernt
- Topologie-Modell & SVG-Renderer eingeführt
- Migration auf .NET 9, neues Fluent-Design & ViewModel
- Zähler-Einstellungen plattformübergreifend vereinheitlicht
- Neue Doku-Struktur & Icon-Assets eingeführt
- AssignedInPort-Werte aktualisiert
- Interfaces für ActionExecutor & JourneyManager
- MAUI aus PR-Validierungspipeline entfernt
- ReSharper-Suppressions dokumentiert, Tests erweitert
- Neues App-Icon-Design & Icon-Workflow
- Monitor-Toolbar überarbeitet, MAUI Deploy & Settings
- AnyRail-Import, Doku-Update & Dateien entfernt
- Fehlerhandling, Timing & UI verbessert
- Serilog, Feedback-Analyse, UI-Upgrade
- [**breaking**] Topologie-Renderer & Konverter integriert
- AnyRail Topologie-Import & Drag/Snap
- Topologie-Rendering und PDF-Import überarbeitet

### Fixed

- WinUI build errors - IoService and MainWindow namespace migration
- Binding-Probleme und Refactoring behoben

### Documentation

- Clean architecture phase 3 - documentation cleanup and guides
- overhaul documentation and third-party notices
- update instructions and organize manifest output
- add AnyRail legal compliance & type mapping
- update metadata and remove redundant examples

### Changed

- Vereinfachung der Array-Initialisierung
- [**breaking**] Migration von Einstellungen auf Solution-Ebene
- Entferne Migration und Projekt-Initialisierung
- Build-Zeit von 115s auf 1,6s reduziert
- Migration zu hierarchischen ViewModels
- Entferne Factories und verbessere Logging
- migrate to clean architecture
- remove App.xaml.cs and related logic
- update Solution handling and test fixes
- simplify architecture and add EditorPage2
- Konsolidierung von Dokumentation und Code
- Modularisierung und PropertyGrid-Integration
- Move CurrentSelectedObject update logic to ViewModel setters (Best Practice)
- Konsistenz, UI und Codebasis optimiert
- GUID-basierte Architektur eingeführt
- centralize entity management in Project
- replace SimplePropertyGrid with ContentControl
- transition to reference-based model
- Entferne JourneyStation-Entität
- simplify selection & property mapping
- simplify entity selection logic
- simplify namespaces and selection logic
- DI-Architektur optimiert & Quick Wins umgesetzt
- DotSettings-Datei umbenannt und vereinheitlicht

### Dependencies

- Aktualisiere UraniumUI-Pakete auf v2.14.0
- remove unused NuGet package versions

### AnyRail-Import

- Gleispläne aus XML laden & anzeigen

### Cleanup

- Remove JourneyManager.cs.backup

### Docs

- Add SessionState Pattern to copilot-instructions.md
- Add session summary for SessionState Pattern refactoring
- Update Manager Architecture with multi-perspective concept (JourneyManager=Train, WorkflowManager=Independent, StationManager=Platform). Cleanup temp files.

### Erweiterung

- Journey- und Feedback-Verarbeitung

### Event-Bus

- Messenger für Feedback & Ansagen integriert

### Gleisplan

- Interaktive Auswahl & AnyRail-Import

### MAUI

- register SharedUI MAUI JourneyViewModel in DI; Add Azure Pipelines CI with build, test, and test-data copy check
- switch MainPage to constructor injection and resolve from DI

### Projektüberarbeitung

- Tests, Lizenz, Bereinigungen

### Refactor

- Einführung von DI und Factory-Pattern
- Complete SessionState Pattern - JourneyManager + JourneyViewModel
- Remove CurrentProjectViewModel, use SelectedProject

### Refaktorierung

- Direkte Z21-Kommunikation implementiert
- Einführung von BlazorUiDispatcher

### SharedUI

- introduce platform-specific adapters for WinUI and MAUI ViewModels; remove duplicate WinUI ViewModels; wire WinUI app to SharedUI adapters; add basic unit tests for MAUI/WinUI JourneyViewModel dispatch; update Copilot instructions with SharedUI subfolders

### Track-Plan-Editor

- Neue Architektur & PDF-Entfernung

### TrackPlan

- Interaktive Segmentauswahl & Domain-Modell

### UI

- Restructure EditorPage to 4-column layout (Projects | CityLibrary | Workflows | PropertyGrid)
- Add SelectorBar navigation + Compact Sizing to EditorPage
- Add TitleBar to MainWindow
- TitleBar and StatusBar fixes

### WIP

- Clean Architecture Refactoring (70%)
- SessionState Pattern Phase 1 - Created SessionState classes and cleaned Domain/Journey
- Domain refactoring to reference-based architecture (72 percent complete) - Added Guid Id to Station, Locomotive, Wagon, Train - Journey.Stations to Journey.StationIds - Journey.NextJourney to Journey.NextJourneyId - Train.Locomotives/Wagons to LocomotiveIds/WagonIds - Station.Flow removed (only WorkflowId remains) - Project.Stations list added (aggregate root) - Deleted StationConverter (no longer needed) - JourneyManager refactored (uses Project for reference resolution) - Removed RestoreWorkflowReferences (obsolete) - Updated copilot-instructions.md (Aggregat-Design guidelines) - Created REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md - Cleaned up obsolete docs - Domain and Backend 100 percent complete - ViewModels Pending (64 errors to fix) - Tests Pending - See docs/REFACTORING-PLAN-REFERENCE-BASED-ARCHITECTURE.md
- JourneyStation refactoring - Backend + StationViewModel complete- JourneyManager: Uses JourneyStations instead of StationIds  - Resolves Station from City library  - WorkflowId from JourneyStation (Journey-specific)  - NumberOfLapsToStop from JourneyStation- StationManager: Marked as Obsolete  - Station.WorkflowId no longer exists  - Use JourneyManager for station workflows- StationViewModel: Refactored to Junction Entity pattern  - Constructor: Station + JourneyStation + Project  - NumberOfLapsToStop/WorkflowId/IsExitOnLeft from JourneyStation  - Platforms resolved via Project.Platforms lookup  - Track/Arrival/Departure from JourneyStationArchitecture:- City.Stations = List<Station> (Library pattern)- Journey.JourneyStations = List<JourneyStation> (Junction Entity)- Station has only: Id, Name, InPort, PlatformIdsStatus: Backend 100%, StationViewModel 100%, JourneyViewModel pendingNext: Fix JourneyViewModel + MainWindowViewModel + TestsSee: docs/SESSION-SUMMARY-2025-12-08-PART2-ARCHITECTURE-FIX.md

### WinUI

- EditorPage entfernt, neue Seitenstruktur eingeführt

### Z21

- Versionserkennung & Anzeige in UI integriert
- Software-Recovery-Funktion und Journey-Zähler
- Zuverlässige Verbindung, Auto-Reconnect & Feature-Labels

