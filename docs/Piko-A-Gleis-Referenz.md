# PIKO A-Gleis – Referenz (aus Prospekt 2019)

Offizielle Spezifikationen aus dem PIKO A-Gleis Prospekt 2019 (99556) – Gegenüberstellung mit der MOBAflow-Implementierung.

## Modul und Basisgeometrie

| Parameter | PIKO-Spez | MOBAflow |
|-----------|-----------|----------|
| Modullänge | 470 mm | 231 + 239 = 470 mm |
| Parallelgleisabstand | 61,88 mm | G62 = 61,88 mm ✓ |

## Gerade Gleise (Straight)

| Code | PIKO (mm) | MOBAflow | Status |
|------|-----------|----------|--------|
| G239 | 239,07 | 239.07 | ✓ |
| G231 | 230,93 | 230.93 | ✓ |
| G119 | 119,54 | 119.54 | ✓ |
| G115 | 115,46 | 115.46 | ✓ |
| G107 | 107,32 | 107.32 | ✓ (Parallelgleis K30) |
| G62 | 61,88 | 61.88 | ✓ (Adapter R3↔R4) |
| G940 | 940 | – | Flexgleis (nicht modelliert) |

## Kurven (Curved)

| Code | Radius (mm) | Bogen (°) | Stück/Kreis | MOBAflow |
|------|-------------|-----------|-------------|----------|
| R1 | 360,00 | 30 | 12 | R1: 360, 30° ✓ |
| R2 | 421,88 | 30 | 12 | R2: 421.88, 30° ✓ |
| R3 | 483,75 | 30 | 12 | R3: 483.75, 30° ✓ |
| R4 | 545,63 | 30 | 12 | R4: 545.63, 30° ✓ |
| R9 | 907,97 | 15 | 24 | R9: 907.97, 15° ✓ |
| R1 7,5° | 360 | 7,5 | 48 | R175 ✓ |
| R2 7,5° | 421,88 | 7,5 | 48 | R275 ✓ |

## Weichen (Switches)

| Code | Gerade | Abzweig | Winkel | MOBAflow |
|------|--------|---------|--------|----------|
| WR | G239 | R9 | 15° | WR: 239.07, 907.97, 15° ✓ |
| WL | G239 | R9 | 15° | WL: 239.07, 907.97, 15° ✓ |
| BWL | R2→R3 | – | – | BWL: R2 421.88, R3 483.75 ✓ |
| BWR | R2→R3 | – | – | BWR: R2 421.88, R3 483.75 ✓ |
| BWL-R3 | R3→R4 | – | – | BWLR3 ✓ |
| BWR-R3 | R3→R4 | – | – | BWRR3 ✓ |
| K15 | G239 | – | 15° | K15 ✓ |
| K30 | G119 | – | 30° | K30 ✓ |

## Wichtige Hinweise aus dem Prospekt

1. **R9** ist der Weichengegenbogen – 15° passen zur WR/WL-Abzweigung.
2. **Gleisabstand 61,88 mm** gewährleistet maßstabsgetreue Begegnung auf R1/R2.
3. **R5** (607,51 mm) – es gibt keine gebogenen Gleise für R5; nur Bogenweichen BWLR3/BWRR3 nutzen R5 als virtuellen Außenradius.
4. **Geometriebeispiele** (Seite 12–15) zeigen Übergänge zwischen Radien und Parallelgleisen – nützlich für Validierung.

## Referenz-PDF

`docs/99556__A-Gleis_Prospekt_2019.pdf` – offizieller PIKO A-Gleis Prospekt.
