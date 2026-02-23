# Viessmann-Multiplex-Signale (5229) – Signalbild-Mapping

Diese Seite beschreibt, wie die **SignalBoxPage** (Stellwerk) die Signalbilder auf das echte Viessmann-Multiplex-Signal (Decoder 5229) abbildet und wie du die passende Konfiguration findest.

## Übersicht

- Am Signal sind **4 DCC-Adressen** konfiguriert (z. B. 201, 202, 203, 204).
- Jede Adresse wird als **Weichenbefehl** (Z21 LAN_X_SET_TURNOUT) angesteuert: **Adresse**, **Ausgang** (0 oder 1), **Activate** (Ja/Nein).
- Das Mapping „Signalbild → (Adresse, Ausgang, Activate)“ ist im Code in `Common/Multiplex/MultiplexerHelper.cs` hinterlegt (5229-Definition).

## Aktuelles Mapping (Basisadresse 201, Ks-Mehrabschnittssignal 4046)

| Signalbild (UI) | DCC-Adresse | Ausgang | Activate | Hinweis |
|-----------------|-------------|---------|----------|---------|
| **Hp0** (Rot)  | 201         | 0       | Nein     | Halt    |
| **Ks1** (Grün) | 201         | 0       | Ja       | Fahrt   |
| **Ks2** (Gelb)  | 203         | 0       | Nein     | Halt erwarten |
| **Ks1 Blink**   | 202         | 0       | Ja       | Fahrt mit Geschw.-Anzeige |
| **Ra12**       | 202         | 0       | Nein     | Rangierfahrt |

Weitere Aspekte (Kennlicht, Dunkel, Zs1, Zs7) sind für 4046 derzeit nicht im Mapping.

**Geschwindigkeitsanzeiger (Varianten mit Ziffern):**  
Das Multiplex-Signal kann zwei zusätzliche Anzeigen haben:
- **Oben (weiße Ziffer):** Höchstgeschwindigkeit in km/h, die **ab sofort** gilt (z. B. 8 = 80 km/h).
- **Unten (gelbe Ziffer):** Höchstgeschwindigkeit in km/h, die **ab nächstem Signal** gilt (z. B. 6 = 60 km/h).

Diese Varianten können ergänzt werden, sobald die DCC-Adressen/Ausgänge für die Geschwindigkeitsanzeiger aus der Viessmann-Dokumentation vorliegen.

## So findest du die passende Konfiguration

1. **Basisadresse prüfen**  
   Am Decoder 5229 ist die Startadresse eingestellt (z. B. 201). In der SignalBoxPage beim Signal unter „MultipLEX-SIGNAL KONFIGURATION“ die **Basis-DCC-Adresse** auf denselben Wert setzen (z. B. 201).

2. **Gesendeten Befehl sichtbar machen**  
   Nach Klick auf ein Signalbild erscheint unten der Status z. B.:  
   `Signal: Ks1` / `DCC-Adresse: 201, Ausgang: 0, Activate: Ja`.  
   In der Hauptzeile der App steht z. B.:  
   `Signal '…' gestellt: DCC-Adresse 201, Ausgang 0, Activate=True`.  
   So siehst du genau, welcher Weichenbefehl gesendet wird.

3. **Polarität pro Adresse umkehren**  
   Wenn am Modell ein Signalbild vertauscht erscheint (z. B. bei 201 Grün/Rot getauscht), die **Polarität** für genau diese Adresse umkehren:
   - **Einstellungen** → Abschnitt **„Stellwerk / Viessmann-Signale“** → für die betroffene Adresse „Adresse X – Polarität umkehren“ aktivieren (4 Checkboxen für die 4 aufeinanderfolgenden Adressen, z. B. 201, 202, 203, 204).
   - Oder in `appsettings.json` unter `signalBox`:
     ```json
     "signalBox": {
       "invertPolarityOffset0": true,
       "invertPolarityOffset1": false,
       "invertPolarityOffset2": false,
       "invertPolarityOffset3": false
     }
     ```
   - Jede der 4 Adressen kann einzeln invertiert werden (Offset 0 = erste Adresse, z. B. 201; Offset 1 = 202; usw.).

4. **Mit Bedienungsanleitung abgleichen**  
   In der Viessmann-Bedienungsanleitung zum 5229 steht, welche DCC-Adresse welches Signalbild steuert. Wenn dein Modell davon abweicht (z. B. anderes Signalmodell 4040/4042/4046), können Adress-Offset oder Activate-Logik abweichen. Die genaue Tabelle entnimm bitte der aktuellen Viessmann-Dokumentation.

5. **Weitere Signalbilder (z. B. mit Geschwindigkeitsanzeiger)**  
   Sobald du die genauen Adressen/Ausgänge für weitere Aspekte kennst, können diese in der 5229-Definition in `MultiplexerHelper.cs` ergänzt werden (und ggf. in der UI).

## Technik (für Entwickler)

- **Weichenbefehl:** Z21 `LAN_X_SET_TURNOUT` (0x53); FAdr = DCC-Adresse − 1; pro Adresse zwei Ausgänge (P=0/1), jeweils Activate An/Aus.
- **Berechnung:** DCC-Adresse = `Basisadresse` + `AddressOffset` aus der Multiplexer-Definition.
- **Invert:** `SignalBox.InvertPolarityOffset0` … `InvertPolarityOffset3` – pro Adresse (Offset 0–3) kann das Activate-Bit einzeln umgekehrt werden.

## Siehe auch

- Stellwerk-Seite (SignalBoxPage) in der MOBAflow-App
- `Common/Multiplex/MultiplexerHelper.cs` – 5229-Definition und Mapping
- `docs/wiki/INDEX.md` – Übersicht der Dokumentation
