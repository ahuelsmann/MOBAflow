# Gleisplanung – Internet-Recherche

Recherche-Ergebnisse zu PIKO A-Gleis, Modellbahn-Gleisplanung, Geometrie und Layout-Algorithmen (Stand: Februar 2025).

---

## 1. PIKO A-Gleis – Technische Specs & Geometrie

### Basisgeometrie
- **Modulraster:** 470 × 61,88 mm (CAD-optimiert)
- **Gleisabstand (Mitte zu Mitte):** 61,88 mm
- **Profilhöhe:** 4,5 mm (Code 100, NEM 120)

### Gerade Gleise
| Code | Länge (mm) | Verwendung |
|------|------------|------------|
| G239 | 239 | Standardweichenlänge |
| G231 | 231 | G239 + G231 = 470 mm Modul |
| G119 | 119,5 | Teilung |
| G115 | 115,5 | Teilung |
| G107 | 107 | Kreuzungen (K30) |
| G62 | 61,9 | Adapter R3↔R4, Modulbreite |
| G940 | 940 | Flexgleis |

### Kurven
- **R9:** Weichengegenbogen für parallele Gleise im 61,88 mm Abstand
- **R1–R4:** Standardradien mit 30°-Bögen

### Quellen
- [PIKO A-Gleis PDF (elektrickevlacky.cz)](https://www.elektrickevlacky.cz/documents/PIKO_A-Gleis_DE.pdf)
- [h0-modellbahnforum: Piko A-Gleis Durchmesser](https://www.h0-modellbahnforum.de/t323612f54878-Piko-A-Gleis-Durchmesser.html)
- [gleisplan24: PIKO A-Gleis Beispiele](https://www.gleisplan24.de/piko-a-gleis-gleisplan-beispiele/)

---

## 2. Modellbahn-Gleisplanung – Open Source & Tools

### XTrkCAD (Open Source)
- **Plattform:** Linux, Windows, Mac, BSD, ChromeOS
- **Repository:** [SourceForge xtrkcad-fork](https://sourceforge.net/projects/xtrkcad-fork/)
- **Parameter-Dateien:** `.xtp` für Gleisbibliotheken verschiedener Hersteller
- **PIKO-Support:** GitHub-Repo für [Piko Hohlprofil XTrkCAD](https://github.com/retro-speccy/hohlprofil) (DDR-Standardgleis)
- **Format:** Parameter-Dateien definieren Geometrie, Radien, Längen

### TRAX / TRAX3D
- Web-basiert, kostenlos
- Unterstützt 100+ Gleisprogramme (Märklin, Fleischmann, Peco, Tillig, H0)
- [trax3d.com](https://trax3d.com/), [traxeditor.com](https://traxeditor.com/)

### SCARM
- Kostenlose Online-Planung
- Große H0-Gleisplan-Datenbank
- [scarm.info](https://scarm.info/layouts/track_plans.php?scale=H0)

### gleisplan24
- Shop mit fertigen PIKO A-Gleis-Plänen (PDF)
- Detaillierte Stücklisten, Beschriftungen
- [gleisplan24.de](https://www.gleisplan24.de/)

---

## 3. H0 Gleisgeometrie – Radien, Winkel, Regeln

### NEM 111 – Minimalradien
- **Alle Wagen:** 495 mm
- **Wagen bis 20 m Vorbildlänge:** 354,75 mm
- **Empfohlen Hauptgleise:** 577,5–742,5 mm

### Hersteller-Standards
- **R1:** typisch 360 mm
- **30°-Winkel:** Standard (12 × 30° = 360°)
- **Bogenwinkel-Beziehung:** 360 mm × sin(30°) = 180 mm (Weichenlänge)

### Parallele Gleise
- Abstand ca. 70 mm für Kollisionsfreiheit (abweichend von PIKO 61,88 mm)

### Quellen
- [Modellbau-Wiki: Gleisradius](https://www.modellbau-wiki.de/wiki/Gleisradius)
- [Familie-Farr: Von der Realität zum Modell](http://www.familie-farr.de/Modellierung_Eisenbahn.htm)

---

## 4. Track Layout – Algorithmen

### Kombinator-Bibliothek (Funktionale Programmierung)
- *"A combinator library for the design of railway track layouts"* (Journal of Functional Programming, Cambridge Core)
- Zusammensetzung von Gleiskomponenten als funktionale Kombinatoren
- Strukturierte Komposition statt freier Platzierung

### Zufällige gültige Layouts (StackExchange)
- [How to devise an algorithm to generate a random but valid train track layout?](https://cs.stackexchange.com/questions/117972/how-to-devise-an-algorithm-to-generate-a-random-but-valid-train-track-layout)
- **Zentrale Regeln:**
  1. Geschlossener Ring (360°)
  2. Keine Überkreuzungen/Kollisionen
  3. Linkskurven − Rechtskurven = 12 (für einen vollen Kreis)
- **Ansätze:** Brute-Force (Base-3-Zählung), Hill-Climbing, Simulated Annealing, genetische Algorithmen
- **Geometrie:** LOGO-Style (Richtungsvektor + 30°-Rotation), Endpunkt-Prüfung

### LiDAR-basierte Automatisierung
- Automatische Rekonstruktion von Gleisgeometrie aus LiDAR-Daten
- Segmentierung, parametrische Assembly
- Etwa 88,9 % Zeitersparnis, ca. 98 % Segmentierungsgenauigkeit

---

## Relevanz für MOBAflow

| Thema | Bezug zu MOBAflow |
|-------|-------------------|
| PIKO-Spez | Bestätigt unsere Werte in PikoACatalog und SegmentLocalPathBuilder |
| XTrkCAD .xtp | Mögliches Austauschformat; Parameter-Struktur als Referenz |
| NEM 111 | Validierung: Minimalradien für Fahrzeuge |
| Kombinatoren | EditableTrackPlan / TrackPlanBuilder als „Kombinatoren“ denkbar |
| Layout-Algorithmen | Port-basierte Traversierung ähnlich „Richtungsvektor + Rotation“ |
| 360°-Regel | Bei uns implizit durch Port-Verkettung; könnte für Auto-Layout genutzt werden |
