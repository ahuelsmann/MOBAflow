# Z21 Maintenance Tool

# Bedienungsanleitung


## Rechtliches, Haftungsausschluss

Die Firma Modelleisenbahn GmbH erklärt ausdrücklich, in keinem Fall für den Inhalt in diesem Dokument
oder für in diesem Dokument angegebene weiterführende Informationen rechtlich haftbar zu sein.

Die Rechtsverantwortung liegt ausschließlich beim Verwender der angegebenen Daten oder beim
Herausgeber der jeweiligen weiterführenden Information.

Für sämtliche Schäden die durch die Verwendung der angegebenen Informationen oder durch die Nicht-
Verwendung der angegebenen Informationen entstehen, übernimmt die Modelleisenbahn GmbH,
Plainbachstraße 4, A-5101 Bergheim, Austria, ausdrücklich keinerlei Haftung.

Die Modelleisenbahn GmbH, Plainbachstraße 4, A- 51 01 Bergheim, Austria, übernimmt keinerlei Gewähr
für die Aktualität, Korrektheit, Vollständigkeit oder Qualität der bereitgestellten Informationen.
Haftungsansprüche, welche sich auf Schäden materieller, immaterieller oder ideeller Art beziehen, die
durch die Nutzung oder Nichtnutzung der dargebotenen Informationen verursacht wurden, sind
grundsätzlich ausgeschlossen.

Die Modelleisenbahn GmbH, Plainbachstraße 4, A-5101 Bergheim, Austria, behält es sich vor, die bereit
gestellten Informationen ohne gesonderte Ankündigung zu verändern, zu ergänzen oder zu löschen.

Alle innerhalb des Dokuments genannten und gegebenenfalls durch Dritte geschützten Marken- und
Warenzeichen unterliegen uneingeschränkt den Bestimmungen des jeweils gültigen Kennzeichenrechts
und den Besitzrechten der jeweiligen eingetragenen Eigentümer.

Das Copyright für veröffentlichte, von der Modelleisenbahn GmbH, Plainbachstraße 4, A-5101 Bergheim,
Austria, erstellte Informationen, bleibt in jedem Fall allein bei der Modelleisenbahn GmbH,
Plainbachstraße 4, A-5101 Bergheim, Austria.

Eine Vervielfältigung oder Verwendung der bereit gestellten Informationen in anderen elektronischen oder
gedruckten Publikationen ist ohne ausdrückliche Zustimmung nicht gestattet.

Sollten Teile oder einzelne Formulierungen des Haftungsausschlusses der geltenden Rechtslage nicht,
nicht mehr oder nicht vollständig entsprechen, bleiben die übrigen Teile des Haftungsausschlusses in
ihrem Inhalt und ihrer Gültigkeit davon unberührt.

## Impressum

Apple, iPad, iPhone, iOS are trademarks of Apple Inc., registered in the U.S. and other countries.
App Store is a service mark of Apple Inc.
Android is a trademark of Google Inc.
Google Play is a service mark of Google Inc.
Motorola is a registered trademark of Motorola Inc., Tempe-Phoenix, USA
LocoNet is a registered trademark of Digitrax, Inc.
RailCom ist eingetragenes Warenzeichen der Firma Lenz Elektronik GmbH.
XpressNet ist eingetragenes Warenzeichen der Firma Lenz Elektronik GmbH.

Alle Rechte, Änderungen, Irrtümer und Liefermöglichkeiten vorbehalten.
Spezifikationen und Abbildungen ohne Gewähr. Änderung vorbehalten.

Herausgeber: Modelleisenbahn GmbH, Plainbachstraße 4, A-5101 Bergheim, Austria


## Änderungshistorie

Datum Dokumentenversion Änderung
07.02.2013 1.00 Beschreibung für Programmversion V1.
20.03.2013 1.01 Beschreibung für Programmversion V1.
19.04.2013 1.02 Anpassungen für Programmversion V1.02 Z21 FW V1.21;
neues Kapitel „R-BUS“ erstellt.
12.07.2013 1.03 Anpassungen für Programmversion V1.03 mit smartRail FW V1.
27.08.2013 1.04 Anpassungen für Programmversion V1.
multiMAUS-Update, LNCV
07.11.2013 1.05 Anpassungen für Programmversion V1.05 mit Z21 FW V1.22 und
smartRail FW V1.
Sniffer, LocoNet, CV Programmieren
12.02.2014 1.06 Anpassungen für Programmversion V1.06 mit Z21 FW V1.23 und
smartRail FW V1.
Einstellungen, MM Programmieren
21.05.2014 1.07 Anpassungen für Programmversion V1.07 mit Z21 FW V1.24 und
smartRail FW V1.
Einstellungen für Kurzschlusserkennung
05 .0 8 .2014 1.07.1 Fehlerkorrekturen
11 .1 1 .2014 1.08 Anpassungen für Programmversion V1.08 mit Z 2 1 FW V1.
LocoNet, R-BUS, CV Programmieren
26.02.2015 1.09 Z21 Firmwareversion V1.
21.04.2015 1.10 Z21 Firmwareversion V1.
Zurücksetzen der IP-Adresse bei Reset auf Werkszustand
14 .0 7 .201 6 1. 11 Z21 Firmwareversion V1.2 8
z21start Unlock-Code
Stop-Start-Einstellung
Decoder Update
21 .0 8 .2017 1.12 Z21 Firmwareversion V1.
CAN Belegtmelder
13.04.2018 1.13 Z21 Firmwareversion V1.
multiMAUS Firmware V1.05 (F0 bis F28)
CAN Belegtmelder Firmware Version V3.1.
Einstellungen: DCC Weichenadressierung konform mit RCN-213,
DCC Weichen geradeaus/abzweigend invertieren
19. 12 .2018 1.1 4 Z21 Firmwareversion V1.
CAN Booster
01. 02 .20 21 1.1 5 Z21 Firmwareversion V1. 40
Optionen, DCCext
zLink Booster und Decoder
11. 08 .20 21 1.1 6 Z21 Firmwareversion V1. 41
MS Decoder Update
06. 04 .20 22 1.1 7 Z21 Firmwareversion V1. 42 und smartRail FW V1.1 6
Erweiterte Einstellungen, DCC CV Rechner,
CAN Booster Management, multiMAUS Firmware Update Infos
02.06.2 022 1.17.2 multiMAUS Firmware V2.00 (F0 bis F31, 2048 Weichen)
22 .06.2023 1.18 Z21 Firmwareversion V1. 43
Modellzeit, DCC Zentralenkennung, Z21 XL BOOSTER


## Inhaltsverzeichnis


- 1 EINLEITUNG
- 2 VERBINDUNG VOM PC ZUR Z21
- 2.1 Verbindungsmöglichkeiten
   - 2.1.1 Verbindung mittels Ethernet-Kabel über den im Lieferumfang enthaltenen Router
   - 2.1.2 Verbindung mittels WLAN über den im Lieferumfang enthaltenen Router
   - 2.1.3 Direkte Verbindung über Ethernet-Kabel ohne Router
- 2.2 Firewall
- 2.3 Verbindung aufbauen und testen.................................................................................................................
   - 2.3.1 Erfolgreicher Verbindungsaufbau zur Zentrale
   - 2.3.2 Verbindungsprobleme
- 3 Z21 MAINTENANCE TOOL FEATURES
- 3.1 Status
   - 3.1.1 z21start Unlock-Code
- 3.2 Einstellungen
   - 3.2.1 Allgemeine Einstellungen
   - 3.2.2 Programmiereinstellungen
   - 3.2.3 Erweiterte Einstellungen
- 3.3 IP-Einstellungen
- 3.4 LocoNet
- 3.5 CAN
   - 3.5.1 Z21 CAN Belegtmelder
   - 3.5.2 Z21 CAN Booster
- 3.6 R-BUS
   - 3.6.1 Rückmeldebus
   - 3.6.2 XpressNet
- 3.7 multiMAUS Update
   - 3.7.1 multiMAUS Fehlermeldungen
- 3.8 Firmware Update
- 3.9 Decoder Update
   - 3.9.1 Hinweise zum MX Decoder Update
   - 3.9.2 Hinweise zum MS Decoder Update
- 3.10 CV Programmieren
   - 3.10.1 Programmieren von DCC-Decodern
   - 3.10.2 Programmieren von MM-Decodern
   - 3.10.3 Programmieren von CV-Sets
   - 3.10.4 DCC CV Rechner
- 3.11 Optionen
   - 3.11.1 Detector Monitor
   - 3.11.2 RailCom Monitor
   - 3.11.3 Lok Liste
   - 3.11.4 Lok Fahrpult
   - 3.11.5 Stellwerk
   - 3.11.6 Modellzeit
- 3.12 smartRail........................................................................................................................................................
   - 3.12.1 smartRail Status
   - 3.12.2 smartRail Einstellungen
- 4 ZLINK
- 4.1 Z21 BOOSTER
   - 4.1.1 Status
   - 4.1.2 Einstellungen
   - 4.1.3 Firmware Update
- 4.2 Z21 switch DECODER
   - 4.2.1 Status
   - 4.2.2 Schalten
   - 4.2.3 Einstellungen
   - 4.2.4 Firmware Update
   - 4.2.5 CV Programmieren
- 4.3 Z21 signal DECODER
   - 4.3.1 Status
   - 4.3.2 Schalten
   - 4.3.3 Einstellungen
   - 4.3.4 Firmware Update
   - 4.3.5 CV Programmieren


## 1 EINLEITUNG

Auf Wunsch der Z21-Community ist die Z2 1 - Service-Applikation „Z21_Maintenance.exe“ erstellt worden.
Mit dieser Anwendung können Sie Ihre Geräte der Z2 1 - Systemfamilie (weiße und schwarze Z21,
smartRail) über Ethernet oder WLAN konfigurieren und warten.

Neben den eventuell aus der App bekannten Einstellungen können fortgeschrittene Anwender auch die
Netzwerkeinstellungen ändern (auf eigene Verantwortung!) oder auch die Z21-Firmware updaten.

Für smartRail werden außerdem noch weitere Einstellungsmöglichkeiten angeboten.

Weiters wird für die Z21 auch ein Zimo-Decoder-Update angeboten. Da es aber gerade hier zahlreiche
mögliche Problemquellen geben kann – angefangen von unzureichendem Kontakt am Gleis bis zu
Umbauten mit Pufferkondensatoren am Decoder – geschieht die Nutzung der bereitgestellten
Informationen ebenfalls auf eigene Verantwortung.

Bitte berücksichtigen Sie in den Einstellungen Ihrer Windows-Firewall und gegebenenfalls auch bei Ihrem
Virenscanner, dass „Z21_Maintenance.exe“ per UDP auf die Ports 21105, 21106 und 34472 zugreifen
können muss, um mit Ihrer Z21 zu kommunizieren. Sehr scharfe Einstellungen am Virenscanner und
Firewall können dazu führen, dass die Kommunikation auf diesen Ports als „verdächtiger
Netzwerkverkehr“ gemeldet und blockiert wird.

Die Applikation kann nach dem Herunterladen direkt gestartet werden. Eine Installation ist nicht
notwendig. Es müssen jedoch bestimmte Netzwerkeinstellungen vorgenommen werden; sie werden in
den folgenden Abschnitten beschrieben.


## 2 VERBINDUNG VOM PC ZUR Z21

## 2.1 Verbindungsmöglichkeiten

Es gibt mehrere Möglichkeiten, den PC mit der Z21 zu verbinden:

- Kabelverbindung vom PC über den im Lieferumfang enthaltenen Router.
    Diese Verbindung lässt sich am einfachsten herstellen. Sowohl der PC als auch die Z21 werden
    per Ethernet-Kabel mit dem WLAN-Router verbunden.
- Verbindung mittels WLAN über den im Lieferumfang enthaltenen Router.
    Dafür muss der PC am WLAN-Router angemeldet werden und die Z21 per Ethernet-Kabel mit
    dem WLAN-Router verbunden sein. Bei smartRail ist nur eine Verbindung über WLAN möglich.
- Direkte Verbindung ohne Router mittels Patchkabel von der Ethernet-Buchse des PC
    direkt bis zur LAN-Buchse an der Z21. Dies ist die stabilste Verbindung. Durch das Weglassen
    des Routers und manueller Konfiguration der PC-Schnittstelle werden zahlreiche potenzielle
    Problemquellen ausgeschlossen. Diese Art der Verbindung wird für Firmware- und Decoder-
    Update empfohlen, falls beim Update unerwartete Probleme auftreten sollten.

In allen in dieser Anleitung gezeigten Beispielen wird davon ausgegangen, dass die IP-Einstellungen in
der Z21 nicht verstellt worden sind und ggf. der im Lieferumfang enthaltene WLAN-Router verwendet
wird.


### 2.1.1 Verbindung mittels Ethernet-Kabel über den im Lieferumfang enthaltenen Router

Diese Verbindung lässt sich am einfachsten herstellen. Entfernen Sie ggf. zuerst ein bereits
angeschlossenes Ethernet-Kabel von der Ethernet-Schnittstelle (100 MBit/s) Ihres PCs.

Stecken Sie nun ein Ethernet-Kabel (Patchkabel) in eine freie, gelbe Ethernet-Buchse des im
Lieferumfang enthaltenen Routers und verbinden Sie das andere Ende mit der Ethernet-Schnittstelle
Ihres PC. Die Z21 wird ebenfalls mit einer gelben Buchse des Routers verbunden.

Jetzt sollte der PC vom Router eine IP-Adresse zugeteilt bekommen. Falls das nicht so ist, sind die
Netzwerkeinstellungen des PC verstellt. Bitte kontrollieren Sie im Problemfall folgende Einstellung:

Gehen Sie nun zu „Start“ / „Einstellungen“ / „Netzwerkverbindungen“

Wählen Sie die Schnittstelle aus, an der nun die Z21 hängt.
Wählen Sie „Eigenschaften“ (rechten Maustaste).


Zeile „Internetprotokoll (TCP/IP)“ markieren und auf „Eigenschaften“ klicken.

Die Standard-Einstellung ist „IP-Adresse automatisch beziehen“.

Falls auf Ihrem PC die Firewall aktiviert ist können Sie jetzt mit Schritt 2.2 **_„_** Firewall **_“_** fortfahren,
ansonsten gehen Sie zu Schritt 2.3 **_„_** Verbindung aufbauen und testen **_“_**.


### 2.1.2 Verbindung mittels WLAN über den im Lieferumfang enthaltenen Router

Z21 und Router einschalten.

Mausklick auf das WLAN-Symbol (grün eingekreist) in der Taskleiste ganz unten.

„Drahtlosnetzwerke anzeigen“ betätigen.


Ggf. Netzwerkliste aktualisieren, bis das WLAN-Netzwerk des WLAN-Routers erscheint.

Der Name des Netzwerks ist „Z21_wxyz“, wobei wxyz die Endziffern der Router-Seriennummer (s. Feld
„S/N“ auf der Router-Unterseite) sind.

Doppelklick auf das entsprechende Z21-Netzwerk.

Geben Sie nun das Passwort für die Netzwerkanmeldung ein.
Sie finden es im Feld „PIN“ an der Unterseite des Routers.
Dann auf „Verbinden“ klicken.


Sie sollten nun dieses Bild sehen.

#### TIPP!

Wahrscheinlich möchten Sie nicht, dass sich Ihr PC in Zukunft automatisch mit dem WLAN-
Router der Z21 verbindet. Dies könnte sehr störend sein, wenn Sie z. B. die WLAN-Schnittstelle
Ihres PC normalerweise für die Internetverbindung verwenden. Gehen Sie deshalb
folgendermaßen vor:
Wählen Sie im oben gezeigten Dialog „Erweiterte Einstellungen ändern“ (siehe im Screenshot oben die
orange Markierung).

Gehen Sie in den Reiter „Drahtlosnetzwerke“.


Wählen Sie den WLAN-Router aus, an dem die Z21 hängt, und wählen Sie „Eigenschaften“.

Im Reiter „Verbindung“ entfernen Sie das Häkchen bei _„Verb_ indung herstellen, wenn das Netzwerk in
Reichweite ist“. Beenden Sie die Dialoge dann jeweils mit „OK“. Damit wird die Verbindung mit dem Z21-
WLAN nur mehr manuell hergestellt, wenn Sie dies auch wirklich wollen.

Falls auf Ihrem PC die Firewall aktiviert ist, können Sie jetzt mit Schritt 2.2 **_„_** Firewall **_“_** fortfahren,
ansonsten gehen Sie zu Schritt 2.3 **_„_** Verbindung aufbauen und testen **_“_**.


### 2.1.3 Direkte Verbindung über Ethernet-Kabel ohne Router

Die stabilste Verbindung zu Ihrer Z21 kann durch eine direkte Verbindung mittels eines Netzwerk-
Patchkabels hergestellt werden. Das Weglassen des Routers entfernt dabei eine weitere mögliche
Problemquelle.

Entfernen Sie ggf. zuerst ein bereits angeschlossenes Ethernet Kabel von der Ethernet-Schnittstelle ( 100
MBit/s) Ihres PC. Stecken Sie nun ein Patchkabel in die LAN-Buchse der Z21 und geben das andere
Ende in die Ethernet-Buchse Ihres PC.

Gehen Sie nun zu „Start“ / „Einstellungen“ / „Netzwerkverbindungen“.

Wählen Sie die Schnittstelle aus, an der nun die Z21 hängt.
Wählen Sie „Eigenschaften“ (rechten Maustaste).


Zeile „Internetprotokoll (TCP/IP)“ markieren und auf „Eigenschaften“ klicken.

Achtung! Notieren Sie sich jetzt vor dem Ändern genauestens Ihre bisherigen Einstellungen, um
nach der Wartung der Z21 diese Schnittstelle wieder zurückkonfigurieren zu können.

Konfigurieren Sie dann in diesem Dialog die Eigenschaften exakt so, wie es dieser Screenshot zeigt und
bestätigen Sie mit „OK“.

Achtung! Vergessen Sie nicht, nach dem Arbeiten mit dem Z21 Maintenance Tool diese
Netzwerkschnittstelle mit Ihren vorher notierten Einstellungen zurückzukonfigurieren.

Falls auf Ihrem PC die Firewall aktiviert ist, können Sie jetzt mit Schritt 2.2 **_„_** Firewall **_“_** fortfahren,
ansonsten gehen Sie zu Schritt 2.3 **_„_** Verbindung aufbauen und testen **_“_**.


## 2.2 Firewall

Wenn Sie die Windows Firewall aktiviert haben bzw. beim ersten Start von „Z21_Maintenance.exe“
folgende Meldung kommt, müssen die Einstellungen der Firewall für das Z21 Maintenance Tool
angepasst werden:

Hintergrund dieser Meldung ist, dass „Z21_Maintenance.exe“ per UDP über die IP-Ports 21105 , 21106
und 34472 mit der Z21 kommuniziert. Eventuell kann sogar ein sehr scharf eingestellter Virenscanner
aufgrund von „verdächtigen Netzwerkverkehr“ Alarm schlagen. Die Kommunikation über diese Ports ist
im Vergleich zu einer „normalen“ PC-Anwendung zwar etwas ungewöhnlich, aber Sie wissen ja bereits:
Die Z21 ist in jeder Hinsicht außergewöhnlich!

Am einfachsten ist es im oben gezeigten Dialog auf „Nicht mehr blockieren“ zu klicken. Sollten Sie eine
andere Firewall verwenden, wird sehr wahrscheinlich eine ähnliche Meldung kommen, bei der Sie eine
sinngemäß ähnliche Ausnahmeregel erstellen können.

Ausnahmeregel für Z21_Maintenance.exe in den Firewall-Einstellungen (Windows XP).


## 2.3 Verbindung aufbauen und testen.................................................................................................................

Starten Sie Z21_Maintenance.exe und drücken Sie auf „Verbinden“.

### 2.3.1 Erfolgreicher Verbindungsaufbau zur Zentrale

Im Idealfall erscheint dieses Bild: Die Z21 wird erkannt und die Verbindung ist aufgebaut.


### 2.3.2 Verbindungsprobleme

Dieses Bild zeigt Ihnen, dass die Verbindung nicht aufgebaut werden kann.

Verifizieren Sie die Verbindung mittels „ping“:

Starten Sie dafür die Windows Eingabeaufforderung.
Geben Sie in der Eingabeaufforderung das Kommando „ping 192.168.0.111“ ein und drücken Sie die
Enter-Taste.


In diesem Fall funktioniert die Verbindung im Prinzip bereits. Überprüfen Sie die Einstellungen der
Firewall (siehe vorheriger Abschnitt) und des Virenscanners (ggf. Log).
Versuchen Sie noch einmal in der Datei Z21_Maintenance.exe die Verbindung aufzubauen. Oftmals
benötigt Windows vom Anstecken des Kabels bis zur vollständigen Herstellung der Verbindung etwas
länger. Den Status des Netzwerks kann man normalerweise in der Windows-Task-Leiste sehen (unten
rechts, ggf. Klick auf das entsprechende Netzwerksymbol).

Antwortet die Zentrale auch nicht auf ein ping, liegt die Ursache für das Problem sicherlich nicht mehr in
der Datei Z21_Maintenance.exe, sondern im Netzwerk bzw. der Netzwerkkonfiguration.

Prüfen Sie erneut die Verbindung, achten Sie dabei auch auf unwichtig und selbstverständlich
erscheinende Details.
− Ist das Kabel in die richtigen Buchsen gesteckt und die Z21 mit Spannung versorgt?
− Funktioniert dieses Netzwerk-Kabel generell?
− Ist der Router, falls Sie ein WLAN eingerichtet haben, erreichbar (z. B. ping auf 192.168.0. 1 )?
− Wird die Schnittstelle von der Firewall oder dem Virenscanner blockiert?
− Wiederholen und überprüfen Sie die Einstellungen der Netzwerkschnittstelle (wie in den
vorherigen Abschnitten beschrieben) bis der ping funktioniert.


## 3 Z21 MAINTENANCE TOOL FEATURES

## 3.1 Status

Bei erfolgreichem Verbindungsaufbau können Sie hier unter anderem die FW Version und andere
Betriebsparameter Ihrer Zentrale überprüfen. Die „Interne Spannung“ entspricht der aktuellen
Gleisspannung.
Das Z21 Maintenance Tool erkennt automatisch, mit welchem Hardware-Typ es verbunden ist.


### 3.1.1 z21start Unlock-Code

Falls die z21start noch nicht entsperrt ist, dann erscheint nach dem Verbinden ein Button
„Entsperre **n...** “. Wenn der Button dagegen ausgegraut ist, dann ist die z21start bereits entsperrt.

Über diesen Button können Sie das Dialogfenster öffnen, wo Sie den Freischalt-Code (Artikelnummer
10814) eingeben können.


Das Entsperren kann bis zu 10 Sekunden dauern.

Damit nimmt die z21start über die LAN-Schnittstelle Lok- und Weichenbefehle an und verarbeitet sie.

Wenn die z21start erfolgreich entsperrt ist, dann bleibt der Button „En **tsperren...** “ ausgegraut.


## 3.2 Einstellungen

Die verfügbaren Einstellmöglichkeiten werden je nach erkanntem Hardware-Typ automatisch angepasst.

Einstellungen für die „Z21 Version für Experten“ (schwarze Box)

Einstellungen für die „z21 Version für Einsteiger“ und „z21 start“ (weiße Box)


Einstellungen für die „Z21 XL“ (für große Spuren)

### 3.2.1 Allgemeine Einstellungen

Hauptgleis-Spannung: hier kann bei der schwarzen Z21 die Schienenspannung am Hauptgleis
eingestellt werden. Die Hautgleisspannung kann allerdings nicht höher als die Spannung vom Netzteil
werden. Bei der weißen z21 und z21start kann die Schienenspannung von der Hardware nicht eingestellt
werden, sondern wird durch die Spannung vom Netzteil vorgegeben.

Zentralen-Stop-Taster: Mit dieser Option wird die Funktion des STOP-Tasters an der Zentrale
festgelegt. Es kann dabei entweder die Gleisspannung ausgeschaltet werden, oder es kann bei
aufrechter Spannungsversorgung ein Nothalt-Befehl an die Lokomotiven gesendet werden.

Ausgabeformat am Gleis: Mit dieser Option können Sie das Befehls-Format am Gleis auswählen.

- DCC und MM: Die Z21 arbeitet als Multiprotokollzentrale und gibt die Datenpakete des jeweiligen
    Fahrzeugs/der jeweiligen Weiche im DCC- oder Motorola-Format aus. Das Format der einzelnen
    Fahrzeuge/Weichen kann über die App konfiguriert werden. Diese Einstellung bleibt in der Z21
    persistent gespeichert.
- nur DCC: Unabhängig von der Einstellung des Fahrzeugs/der Weiche werden die Datenpakete
    nur im DCC-Format ausgegeben.
- nur MM: Unabhängig von der Einstellung des Fahrzeugs/der Weiche werden die Datenpakete
    nur im Motorola-Format ausgegeben.
Diese Option gibt es ab Z21 Firmware V1.23.

Kurze DCC Lokadressen: Mit dieser Option können Sie den Adressbereich festlegen, in welchem für
DCC-Loks am Gleisausgang „kurze Adressen“ generiert werden. Dies ist im Lok-Decoder zu
berücksichtigen (siehe Decoderbeschreibung CV1, CV17, CV18, CV29).

- von 1 bis 99: Dies ist die rückwärts kompatible Standardeinstellung der Z21. Für Loks mit den
    Adressen von 1 bis 99 werden DCC-Pakete mit kurzen Adressen generiert, ab der Adresse 100
    werden DCC-Pakete mit langen Adressen generiert.
- von 1 bis 127: Für Loks mit den Adressen von 1 bis 127 werden DCC-Pakete mit kurzen
    Adressen generiert, ab der Adresse 128 werden DCC-Pakete mit langen Adressen generiert.
Diese Option gibt es ab Z21 Firmware V1.23.

Kurzschluss Hauptgleis und Kurzschluss B-BUS: Mit diesen Optionen können Sie die
Ansprechgeschwindigkeit der Kurzschlusserkennung am Hauptgleis (DCC Main) und am B-BUS
(Booster-Bus) erhöhen. Dies kann vor allem für Anlagen in Spur N sinnvoll sein.


RailCom aktivieren: RailCom, also die Kommunikation vom Decoder zur Zentrale, wird zum Beispiel
benötigt, wenn Sie einen RailCom-fähigen Decoder bei der Hauptgleisprogrammierung (POM) auch
auslesen möchten oder wenn der Belegtmelder 10808 die Lokadressen melden soll. Mit wenigen, meist
sehr alten Decodern, welche vor der Einführung von RailCom entwickelt worden sind, kann es allerdings
im Betrieb mit RailCom zu Inkompatibilitäten kommen. Aus diesem Grund kann hier RailCom generell
deaktiviert werden.

CV29 Adresse automatisch: Diese Option ist nur beim Betrieb mit der multiMAUS von Bedeutung.
Wenn Sie mit der multiMAUS eine kurze oder lange Adresse programmieren, wird durch diese Option im
Decoder das Bit 5 in CV29 („lange Adresse“) automatisch gesetzt oder gelöscht.

DCC Weichenadressierung konform mit RCN- 213 (identisch mit früherer Option **„** DCC-
Weichenadressverschiebung +4 **“** ): Mit dieser Option können Inkompatibilitäten bezüglich der
Nummerierung von Weichen- oder Signaladressen behoben werden. Roco nummeriert die Weichen ab
Modul 0 (mit jeweils 4 Weichen), andere DCC-Zentralenhersteller erst ab Modul 1. Diese unterschiedliche
Zählweise ist historisch aus einer Schwäche der Spezifikation NMRA S-9.2. 1 gewachsen, wo keine der
beiden Zählweisen grundsätzlich als „falsch“ bezeichnet werden konnte. Erst mit der neueren
Spezifikation von der RailCommunity RCN- 213 (2015) ist die Nummerierung der Weichenadressen
eindeutig definiert worden.
Die Standardeinstellung für diese Option ist „deaktiviert“, um mit älteren Roco-Geräten rückwärts
kompatibel zu bleiben. Beim Umstieg von einem bestehenden Fremd-System auf die Z21 kann es daher
vorkommen, dass nun die bisher gewohnten Weichenadressen aufgrund der unterschiedlichen Zählweise
der Modulgruppen um den Wert 4 verschoben erscheinen. Wurde eine Weiche auf dem bisherigen
Fremdsystem z. B. unter 1 gesteuert, wird sie auf der Z21 unter 1+4=5 angesprochen. Um unseren
Kunden den Umstieg zu erleichtern, wurde nun diese Option eingeführt, mit deren Hilfe die Weichen und
Signale mit den bisher gewohnten Weichenadressen weiterverwendet werden bzw. gemäß RCN- 213
adressiert werden können.
Diese Option gibt es ab Z21 Firmware V1.21.
Ab Z21 Firmware V1.22 wirkt sich die Einstellung nicht nur auf die DCC-Pakete am Gleisausgang
aus, sondern legt auch fest, wie Weichen-Schaltbefehle am Sniffer BUS-Eingang zu interpretieren
sind.

Gleisausgang bei Start der Z21 auf STOP-Stellung: Mit dieser Option bleibt der Gleisausgang der Z21
bzw. z21 nach dem Starten der Zentrale abgeschaltet.
Diese Option gibt es ab Z21 Firmware V1.28.

### 3.2.2 Programmiereinstellungen

Programmier-Spannung: hier kann bei der schwarzen Z21 die Schienenspannung für den CV-
Programmiervorgang am Programmiergleis eingestellt werden. Diese Spannung kann allerdings nicht
höher als die Spannung vom Netzteil werden. Bei der weißen z21 und z21start gibt es dagegen keinen
separaten Programmierausgang, dessen Spannung eingestellt werden könnte.

Auslese-Modus: Eine CV eines Decoders kann am Programmiergleis entweder bit- oder byteweise
ausgelesen werden. Während für bitweises Lesen nur einige wenige Zugriffe nötig sind, müssen bei
byteweisen Lesen alle möglichen Werte so lange durchprobiert werden, bis der korrekte Wert gefunden
ist. Dies kann je nach Wert sehr lange dauern, denn im schlimmsten Fall sind bis zu 256 Versuche
notwendig. Sehr alte Decoder können nur byteweise ausgelesen werden.
Die Einstellung „Bit- und Byteweise“ geht folgendermaßen vor: Zuerst wird versucht, die CV bitweise
auszulesen. Nur falls dies nicht gelingt, wird als zweiter Versuch das byteweise Lesen gestartet.


### 3.2.3 Erweiterte Einstellungen

Der Button Erweiterte Einstellungen öffnet ein Popup-Fenster mit weiteren Einstellungsmöglichkeiten,
die nur in speziellen Fällen tatsächlich benötigt werden.

Erweiterte Einstellungen

Vergessen Sie nach dem Schließen des Fensters nicht, eventuell veränderte Einstellungen mit dem
Button Schreiben im Hauptformular in die Z21 abzuspeichern.

XBUS-Protokoll Version
Mit dieser Option kann die X-BUS Protokoll Version ausgewählt werden. Diese Einstellung kann sich als
nützlich erweisen, falls sich irgend ein X-BUS Teilnehmer wider Erwarten als inkompatibel mit Protokoll
Version 4.0 erweisen sollte.

- Version 4.0: Am X-BUS werden Fahr- und Schaltbefehle verwendet, welche kompatibel mit
    XpressNet V4.0 sind (z.B. LH101 FW V2.10). Das ist die Default-Einstellung ab Z21 Firmware
    V1.42.
- Version 3.6: Am X-BUS werden Fahr- und Schaltbefehle verwendet, welche kompatibel mit
    XpressNet V 3. 6 sind. Das war die Protokollversion, welche in Z21 Firmware V1.24 bis V1.41
    verwendet worden ist.
Diese Option gibt es ab Z21 Firmware V1. 42.

Ausgabe Zubhörbefehle
Mit dieser auf Kundenwunsch veranlassten Option kann die Ausgabe von Befehlen für Zubehördecoder
(z.B. Weichendecoder, Signaldecoder, ...) am Hauptgleis und Booster unterbunden werden. Das kann
nützlich sein, wenn man ausschließlich Funktionsdecoder am CAN oder LocoNet verwendet, und die
verfügbare Bandbreite am Gleis für Fahrzeugdecoder-Befehle reservieren möchte.

- Gleis und Bus: Befehle für Zubehördecoder werden sowohl am Gleis ausgegeben als auch an
    X-BUS, LocoNet, CAN und LAN weitergeleitet.
- Nur BUS: Befehle für Zubehördecoder werden nur an X-BUS, LocoNet, CAN und LAN
    weitergeleitet. Es erfolgt keine Ausgabe am Gleis.
Diese Option gibt es ab Z21 Firmware V1. 42.

DCC Loks hohe Funktionen zyklisch wiederholen
Funktionsbefehle für Fahrzeugdecoder von F0 bis inklusive F12 werden am Gleis - ähnlich wie die
aktuelle Fahrstufe und Fahrtrichtung – regelmäßig (prioritätsgesteuert) ausgegeben. Funktionsbefehle ab
F13 werden dagegen nur nach einer Änderung des Zustands drei Mal am Gleis ausgegeben, und danach


aber aus Rücksicht auf die verfügbare Bandbreite am Gleis und gemäß RCN- 212 bis zur nächsten
Änderung nicht mehr regelmäßig wiederholt. Das kann aber zu Problemen mit Dauerfunktionen ab F13
führen. Beispiel: die Innenbeleuchtung eines Triebwagens sei mit F1 9 aktiviert, und es kommt zufällig zu
einer etwas längeren Unterbrechung der Spannungsversorgung des Fahrzeugs. Die Innenbeleuchtung
geht aus ... und bleibt aus. Alle Dauerfunktionen ab F13 sind vom Decoder „vergessen“, weil ja
Funktionen ab F13 normalerweise nicht mehr wiederholt ausgegeben werden.
Durch Aktivieren dieser Option kann erzwungen werden, dass von der Z21 auch Funktionen ab F1 3
regelmäßig ausgegeben werden, wenn sie eingeschaltet sind. D.h. es wird dafür nur dann Bandbreite am
Gleis verbraucht, wenn eine hohe Funktion auch tatsächlich eingeschaltet ist. Dank der
prioritätsgesteuerten Ausgabe von Lokbefehlen am Gleisausgang sollte das bei kleineren Anlagen mit bis
zu 10 Fahrzeugen ohne merkbare Effekte möglich sein. Für größere Clubanlagen mit vielen Fahrzeugen
und vielen Dauerfunktionen ab F13 ist diese Option eventuell aber weniger empfehlenswert.
Diese Option gibt es ab Z21 Firmware V1. 42.

DCC Weichen geradeaus/abzweigend invertieren: Mit dieser auf Kundenwunsch veranlassten Option
kann die Logik der DCC-Schaltbefehle für Weichen (geradeaus/abzweigend) und Signale (grün/rot)
invertiert werden. Diese Option kann für Anwender nützlich sein, die mit ihrer bestehenden Anlage von
einem älteren Fremdsystem auf die Z21 umsteigen.
Diese Option gibt es ab Z21 Firmware V1. 3 1.

Funktionsdecoder Ausgänge überwachen: Mit dieser Option werden die Ausgänge von
Funktionsdecodern mittels eines Timers überwacht und ggf. nach einigen Sekunden automatisch
abgeschaltet, falls ein fehlerhafter Controller (Handregler, App, PC-Steuerung, ...) dies nicht selbst
vorgenommen hat. Damit wird eine Überhitzung der Antriebsspulen verhindert.
Diese Option gibt es ab Z21 Firmware V1.20.

DCC 28 statt 14 Fahrstufen am Sniffer BUS auswerten: Mit Z21 Firmware Version V1.22 wurde der
Sniffer BUS implementiert. Das heißt, dass Sie zusätzlich mit einer älteren DCC-Zentrale die Loks der
Z21 steuern können, indem Sie einfach den Gleisausgang der alten Zentrale mit dem Sniffer BUS der
Z21 verbinden. Die Z21 versucht das Format der alten Zentrale so gut wie möglich auf das in der Z21
eingestellte Format der Lok abzubilden. Auf diese Weise ist es möglich, mit einer sehr alten Zentrale,
welche z. B. nur das DCC-Format „14 Fahrstufen“ beherrscht, eine moderne Lok mit 128 Fahrstufen oder
sogar eine Lok mit MM-Decoder (Motorola) zu steuern.
Allerdings ist es technisch nicht immer möglich, das Format „14 Fahrstufen“ und „28 Fahrstufen“ am
Sniffer-Eingang zuverlässig zu erkennen (Vergleich: Lok-Decoder CV29 Bit 1) und auf das eingestellte
Ausgabe-Format abzubilden. Im Zweifelsfall geht die Z21 von „14 Fahrstufen“ aus:

Einstellung Fahrstufen der Lok n in der Z21: 14 28 128

Fahrstufen der Lok n am Sniffer-Eingang: 14 Ok (a) Ok
Fahrstufen der Lok n am Sniffer-Eingang: 28 (b) OK (b)

Fahrstufen der Lok n am Sniffer-Eingang: 128 Ok OK OK
(a) Das Licht kann nicht eingeschaltet werden, weil die Z21 bei der Lok-Einstellung „28 Fahrstufen“ für diese Lok am Sniffer
Eingang ebenfalls das Format „28 Fahrstufen“ annimmt, und deswegen die Licht-Info vergeblich in der DCC-Functiongroup1
erwartet.
(b) Stirnlicht flackert mit Fahrstufe. Abhilfe verschafft man sich, indem man

- entweder das Format in der Fremdzentrale und in der Z21 möglichst passend konfiguriert (am besten mit 128 Fahrstufen),
- oder in der Fremdzentrale für diese Lok das Licht (F0) einmal einschaltet, um die automatische Formaterkennung zu unterstützen.
- oder im Maintenance.exe die Einstellung "DCC 28 statt 14 Fahrstufen am Sniffer BUS auswerten" aktiviert,
Diese Option gibt es ab Z21 Firmware V1.22.

R-BUS: XpressNet Rückmeldungen emulieren
Mit dieser auf Kundenwunsch veranlassten Option kann die Weiterleitung von R-BUS Belegtmeldungen
an X-BUS Teilnehmer aktiviert werden. Belegtmeldungen vom R-BUS werden dann zusätzlich als
XpressNet Meldungen („BC Rückmeldung Rückmeldebaustein“) an den X-BUS weitergeleitet, und
können z.B. auf einem LH 101 betrachtet werden.
Dazu werden allerdings für die R-BUS Rückmeldemodule 1 bis 20 die XpressNet Rückmelderadressen
65 bis 84 verwendet, wodurch dann allerdings die Weichen 257 bis 3 36 nicht mehr sauber per X-BUS
geschaltet werden können, falls diese Option aktiviert ist. Diese Option sollte daher nur dann aktiviert
werden, wenn diese XpressNet Meldungen tatsächlich benötigt werden.
Diese Option gibt es ab Z21 Firmware V1. 42.


DCC Zentralen-Eigenschaftenkennung am Gleis senden
Mit dieser Option werden die Eigenschaften der Zentrale regelmäßig mit einem neuen, speziell für
diesem Zweck geschaffenen DCC-Befehl (RailCommunity Norm RCN- 211 ) auf das Gleis ausgegeben.
Dadurch können sich dann in Zukunft die Decoder automatisch auf die verschiedenen Optionen und
Eigenheiten der DCC Zentrale einstellen, wie z.B. RailCom und POM-Lesen, die maximal verfügbare
Anzahl der Lok-Funktionen oder die genaue Art und Weise der Lok- und Weichenadressierung.
Diese Option gibt es ab Z21 Firmware V1. 43.

Erweiterte Programmier-Einstellungen

Reset Pakete (starten): Bestimmt die Anzahl der DCC-Reset-Pakete ganz am Anfang der CV-
Programmiersequenz (CV-Lesen/schreiben). Je höher dieser Wert ist, desto mehr Zeit bekommt der
Decoder zum Hochfahren.

Programmier Pakete: Bestimmt die Anzahl der DCC-Lese/Schreib-Kommandos in der CV-
Programmiersequenz (RP-9.2.3).

Reset Pakete (fortsetzen): Bestimmt die Anzahl der DCC-Reset-Pakete innerhalb der
Programmiersequenz.

Sollten Sie einmal Probleme beim CV-Lesen/-Schreiben bei einem bestimmten Decoder haben, können
Sie diese Paket-Anzahl ändern, z. B. 25/10/ 10 oder 30/15/15.

ACK Schwelle: Ein Lokdecoder beantwortet CV-Lese- und CV-Schreib-Anfragen mit einem leicht
erhöhten Stromverbrauch gemäß „Basic Acknowledgment“ aus RP-9.2.3: „... increased load (positive-
delta) on the programming track of at least 60 mA for 6 ms +/- 1 ms“.
Obwohl die Z21 ab der FW V1.20 bereits etwas empfindlicher eingestellt ist, kann es in der Praxis leider
noch immer vorkommen, dass einige Decoder von der Norm abweichen und daher nicht gelesen werden
können. Dies kann aber auch bei Verwendung eines Pufferkondensators passieren. Um auch solche
Decoder erfolgreich auslesen zu können, können Sie hier die Schwelle für das Erkennen des „Basic
Acknowledement“ niedriger oder ggf. auch höher einstellen.
Diese Option gibt es ab Z21 Firmware V1.20.


## 3.3 IP-Einstellungen

Wie von einigen Kunden gewünscht, kann die IP-Adresse der Z21 geändert werden.

Der Normalverbraucher benötigt das nicht und es wird vom Hersteller auch dringend davon
abgeraten. Beachten Sie die entsprechenden Hinweise in den Screenshots.

Bekannte Probleme: Bei einigen Routern von Internetprovidern können bestimmte LAN-Buchsen für IP-
TV reserviert sein. Bei alten HUBs kann es ebenfalls zu Kommunikationsproblemen kommen.

Bevor der Anwender eine neue IP-Adresse eingeben kann, muss erst einmal der obligatorische
Disclaimer ausdrücklich akzeptiert werden:

Erst nach dem Klick auf „Ja“ werden die Eingabefelder aktiv.


Erst jetzt kann man die Adresse z. B. auf „192.168.0. 123 “ ändern und – nach einer weiteren Warnung –
in die Z21 schreiben:

und nach dem „Ja“:

Wichtig: Die neue IP-Adresse wird in der Z21 erst nach einem Kaltstart (d. h. Versorgungs-
spannung entfernen) der Zentrale aktiv!


Ihre letzte Änderung wird auch im Programm als „Ihre letzte Änderung war... _“_ angezeigt.

Beachten Sie bitte, dass jede Änderung der IP-Adresse in der Z21 auch zum Rest des Netzwerks
passen muss, angefangen von den Subnetz-Masken, Router, eigene Ethernet-Schnittstelle am PC
bis zu den App-Einstellungen im Smartphone oder Tablet etc.

Bis Firmware V1.26 gilt: Auch beim Zurückstellen auf Werkseinstellung (Stopp-Taste halten bis LED
violett blinkt) bleibt diese geänderte IP Adresse erhalten.
Ab V1.27 werden die IP-Einstellungen auf die Werksvorgabe zurückgesetzt.


## 3.4 LocoNet

LocoNet ist ab Z21 Firmware V1.20 aktiviert.

Mit der Firmware V1.20 wurde auf der Z21 der erste Schritt für die LocoNet Integration vollzogen.

Über LNCV Lesen und LNCV Schreiben können Sie die sogenannten „LocoNet Konfigurationsvariablen“
programmieren, welche in einigen LocoNet-Zubehörartikeln verwendet werden (z. B. Uhlenbrock 63410
„LocoNet-Schaltmodul“). Details und Bedeutung der einzelnen LNCVs können Sie der jeweiligen
Bedienungsanleitung des Herstellers entnehmen.

Mittels LocoNet Dispatch können Sie eine Lokadresse vorbereiten, welche Sie danach im LocoNet
Dispatch-Verfahren auf Ihrem LocoNet-Handregler (z. B. FRED) übernehmen können.

Ab Z21 Firmware V1.25 bekommen Sie in der Lok Liste eine Übersicht aller Fahrzeuge, welche seit dem
letzten Einschalten der Z21 verwendet worden sind. Neben der Fahrzeugadresse werden auch das
verwendete Schienenformat, die Geschwindigkeit und die aktiven Funktionen dargestellt. Mit _„_ Edit _...“_
oder der rechten Maustaste kann das Schienenformat der ausgewählten Lok verändert werden. Das
LocoNet-Dispatch kann in der Lok-Liste ebenfalls mit der rechten Maustaste eingeleitet werden.
Die Spalten Status und Timer beziehen sich auf das sogenannte „Purging“, siehe weiter unten.


Unter LocoNet Einstellungen können Sie über den Button FastClock die LocoNet Modellzeituhr
aktivieren und die Rate vorgeben. Beschreibung siehe Abschnitt 3.11.6 Modellzeit.

Ab Z21 Firmware V1.2 2 können Sie mit LocoNet Modus die Z21 als „LocoNet Master“ (Standard-
Einstellung) oder als „LocoNet Slave an Intellibox“ konfigurieren.

Entgegen ursprünglichen Planungen haben wir aufgrund von Kundenwünschen für die Z21 einen
LocoNet-Slave Modus implementiert. Damit können Sie die Z21 (Slave, L-BUS Schnittstelle) über
LocoNet mit einem TWIN-CENTER bzw. Intellibox (Master, LocoNet T-Buchse) verbinden. Die Z21 stellt
dann die Schnittstelle zwischen App und Intellibox her. So ist es möglich, Ihre Anlage gleichzeitig sowohl
mit dem TWIN-CENTER und eventuell vorhandenen LocoNet Bediengeräten, als auch mit der Z21 App
am Smartphone bzw. Tablet und multiMAUS zu steuern!

Wichtige Information: Da, wie bereits erwähnt, für die Z21 ursprünglich kein LocoNet Slave geplant war,
gibt es unbedingt Folgendes zu beachten: Damit nicht die Z21 und der vorhandene Master gleichzeitig
die Versorgungsspannung am LocoNet-Bus einspeisen, was zu beträchtlichen elektrischen Problemen in
den Zentralen führen kann, muss die Versorgungsspannung an der L-BUS Schnittstelle an der Z21 vom
Rest des LocoNet isoliert werden. Dazu verwenden Sie bitte das extra für diesen Zweck angefertigte
weiße „Z21 LocoNet Slave Kabel“ mit der Art.Nr. 136100.
Dieses Kabel erhalten Sie bei Ihrem Händler oder online im Ersatzteilshop von Roco.


Ab Z21 Firmware V1.2 5 können Sie mit der Option Lok Freigabezeit das sogenannte Purging
konfigurieren. „Purging n Minuten“ bedeutet, dass für eine stehende Lok keine Fahrbefehle mehr über
das Gleis an den Decoder geschickt werden, wenn sie n Minuten lange nicht vom Benutzer oder einem
Steuerungsprogramm verwendet worden ist. Sobald die Lok wieder verwendet wird, wird der
Wiederholzyklus der Fahrbefehle für dieses Fahrzeug wieder aktiviert und der Purging-Timer startet neu.

(^)
Purging erhöht den Datendurchsatz für die tatsächlich verwendeten Fahrzeuge. Purging kann für
besonders große Anlagen oder Modultreffen mit vielen Teilnehmern interessant sein, wo sich deutlich
mehr als 50 Fahrzeuge gleichzeitig auf der Anlage befinden. Für den normalen Betrieb kleinerer und
mittlerer Anlagen mit weniger als 50 Fahrzeuge wird normalerweise der prioritätsgesteurte
Wiederholzyklus der Fahrbefehle in der Z21 auch ohne Purging völlig ausreichen.
Den Status der Lok und der aktuellen Wert des Purging-Timers können Sie in der Lok-Liste sehen, siehe
oben. Dort kann das Purging auch für einzelne Loks gezielt deaktiviert werden.


## 3.5 CAN

Wenn Z21 Belegtmelder und Z21 Booster über CAN mit der Z21 verbunden sind, dann erscheinen sie
automatisch im Z21-Maintenance-Tool ggf. untereinander im Reiter „CAN“ auf.

Für Belegtmelder, die über CAN mit der Zentrale verbunden sind, bietet die Z21 eine R-BUS- und
LocoNet-Emulation an. Dadurch kann der Belegtmelder sofort in bestehenden PC-Steuerungen
verwendet werden, ohne dass diese zuerst erweitert werden müssen. Der Belegtmelder kann so in der
PC-Steuerung bzw. in der Z21-App entweder wie ein Roco 10 787 oder wie ein LocoNet-Belegtmelder
eingebunden werden.

- R-BUS Rückmelder emulieren
    Diese Option aktiviert die Emulation des R-BUS. Der 1 08 08 kann in der PC-Steuerung wie ein
    Roco 10787 verwendet werden.
    Die Werkseinstellung ist „ein“.
- LocoNet Rückmelder emulieren
    Diese Option aktiviert die Emulation von LocoNet-Belegtmeldern. Es werden entsprechende
    LocoNet-Belegtmeldungen mit Lokadressen-Informationen generiert.
    Die Werkseinstellung ist „ein“.
- Lissy/Marco emulieren
    Es werden entsprechende Lissy/Marco-Meldungen mit Lokadressen-Informationen generiert.
    Die Werkseinstellung ist „aus“.

Sie können diese Emulationseinstellungen, die in der Z21 gespeichert sind, mit den Buttons Lesen und
Schreiben von und zur Z21 übertragen.


### 3.5.1 Z21 CAN Belegtmelder

Der Belegtmelder Roco 10808 wird ab Z21 Firmware V1.30 am CAN-BUS unterstützt.

Der Belegtmelder Roco 10808 kann entweder über den R-BUS oder den CAN-BUS betrieben werden.
Der CAN-BUS bietet gegenüber dem R-BUS weitere Möglichkeiten:

- Übertragung der erkannten RailCom-Lokadressen
- Konfiguration des Belegtmelders (Modul-Adresse, ...)
- Firmware-Update des Belegtmelders

Im Bereich CAN Module werden die angeschlossenen Roco 10808-Belegtmelder automatisch angezeigt.

In der Liste der Belegtmelder sehen Sie jeweils die fest eingestellte NetID, die programmierbare Modul-
Adresse sowie den Status der Eingänge mit den erkannten RailCom-Lokadressen. Ein kleiner Pfeil
neben der Lokadresse zeigt die Richtungsinformation zur Lok an:

- Rot bedeutet „belegt“.
- Grün bedeutet „frei“.
- Grau bedeutet „noch keine aktuelle Belegt-Information erhalten“ (z.B., wenn der Belegtmelder
    nicht mit Spannung versorgt wird).

Mit dem Button Status anfordern können Sie den Status aller angeschlossenen Belegtmelder anfordern.

Mit dem Button Off/On Coldstart werden die Belegtmelder durch kurzes Aus- und Einschalten der
Gleisspannung neu gestartet. Dadurch wird ebenfalls der Status erneut aktualisiert.

Mit dem Button Setup gelangen Sie zum jeweiligen Belegtmelder-Setup-Dialog, in welchem die
Einstellungen verändert und das Firmware Update durchgeführt werden können.


3.5.1.1 Roco 10808 Belegtmelder-Setup
Im Belegtmelder-Setup-Dialog können Sie den ausgewählten Roco 10808 Belegtmelder konfigurieren
oder ein Update der Belegtmelder-Firmware durchführen.

Unter Moduladresse können Sie eine neue Adresse vergeben. Achten Sie bitte darauf, dass alle
Belegtmelder in Ihrem System jeweils eine unterschiedliche Moduladresse bekommen. Sie können die
Moduladresse alternativ auch über den Hardware-Taster am Belegtmelder und einem kurz
darauffolgenden Weichenbefehl verändern.

Mit RailCom Kanal 2 Daten an Zentrale weiterleiten können Sie die Weiterleitung der RailCom-Daten
vom Kanal 2 (Geschwindigkeit, POM-Read-Result, QoS, etc.) an die Z21 aktivieren. Normalerweise sollte
dies aber nicht notwendig sein, da diese Aufgabe bereits vom RailCom-Global-Detector in der Z2 1
übernommen wird. Die Weiterleitung der RailCom-Lokadresse ist übrigens von dieser Option
unabhängig. Ab Belegtmelder-Firmware V3.1.20 werden POM-Read-Results immer an die Zentrale
weitergeleitet.

Die Besetztschwelle: Strom definiert die Empfindlichkeit der Eingänge des Belegtmelders in Schritten
von 0.1 mA (= 100 μA). Die Werkseinstellung bei der Auslieferung des Belegtmelders beträgt „10“ (d.h.
1 mA).

Die Verzögerung Frei **–** Besetzt bestimmt das Zeitverhalten des Belegtmelders beim Übergang von
„Frei“ auf „Besetzt“ in Millisekunden. Die Werkseinstellung beträgt 0 ms.

Die Verzögerung Besetzt **–** Frei bestimmt das Zeitverhalten des Belegtmelders beim Übergang von
„Besetzt“ auf „Frei“ in Millisekunden. Die Werkseinstellung beträgt 1000 ms.

Mit Default Werte können Sie die Werkseinstellungen des Belegtmelders in den Eingabefeldern des
Dialogfensters wiederherstellen.

Die Werte der Eingabefelder werden mit dem Button Schreiben in den Belegtmelder übertragen.

Mit Lesen werden die Einstellungen aus dem Belegtmelder erneut ausgelesen und angezeigt.

Mit FW Update können Sie die Firmware im Belegtmelder aktualisieren. Im Feld Update-Info werden die
Zielversion bzw. Informationen zum Update-Verlauf angezeigt.


### 3.5.2 Z21 CAN Booster

Die Booster Roco 10806 und 10807 werden ab Z21 Firmware V1.33 am CAN-BUS unterstützt.

Die Z21 Single Booster 10806 und Z21 Dual Booster 10807 können entweder über den B-BUS oder den
CAN-BUS betrieben werden.
Der CAN-BUS bietet gegenüber dem B-BUS mehr Möglichkeiten:

- Die Gleisspannung und die RailCom-Einstellung können automatisch von der Zentrale
    übernommen werden (Auto-Settings)
- Über CAN werden mittels ZCAN20-Protokoll die empfangenen RailCom-Kanal2-Daten vom
    Booster zur Z21 übertragen. Dadurch ist zum Beispiel das Auslesen eines Fahrzeug-Decoders
    über die POM-Lesebefehle nicht nur am Hauptgleis der Zentrale, sondern auch im Booster-
    Abschnitt möglich.
- Konfiguration des Boosters
- Firmware-Update des Boosters
- Booster Management (ab Booster Firmware V1.11)

Je nach Booster-Typ, Single oder Dual Booster, erscheinen ein oder zwei Panels mit der aktuellen
Spannung und dem Stromverbrauch des jeweiligen Gleisausgangs.

Mit einem Klick auf die Panels lassen sich ab Booster Firmware V1.11 und Z21 Firmware V1.42 die
dazugehörende Gleisausgänge einzeln deaktivieren und wieder reaktivieren (Booster Management).
Ein deaktivierter Gleisausgang wird dabei grau dargestellt.

Die Icons unter dem Panel zeigen den Status des jeweiligen Gleisausgangs an:
Gleisausgang ist ausgeschaltet
Kurzschluss erkannt
Bremsgenerator-Modus aktiv
RailCom aktiv (d.h. die RailCom-Lücke wird vom Booster im Gleissignal erzeugt)

Über den Button Setup gelangen Sie in den Booster-Setup-Dialog, in welchem die Einstellungen
verändert und das Firmware Update durchgeführt werden können.


3.5.2.1 Roco 10806 und 10807 Booster-Setup
Im Booster-Setup-Dialog können Sie den ausgewählten Z21 Single Booster 10806 sowie Z21 Dual
Booster 10807 konfigurieren oder ein Update der Booster-Firmware durchführen.

Beim Z21 Dual Booster 10807 lassen sich die beiden Gleisausgänge unabhängig voneinander
konfigurieren:


Gleisspannung & RailCom: Vorgaben automatisch über CAN von der Zentrale übernehmen ( = „Auto-
Settings“)
Falls der Z21-Booster mit der Zentrale über den CAN-BUS verbunden ist, dann kann der Booster die
Einstellungen für die Gleisspannung und RailCom automatisch von der Zentrale übernehmen („Auto-
Settings“). Falls die Einstellungen aus der Zentrale aber nicht ermittelt werden können, z.B. weil der
Booster über den B-BUS verbunden ist, dann werden die im Booster gespeicherten Vorgaben für die
Gleisspannung und RailCom verwendet.
Sie können diese „Auto-Settings“ bei Bedarf deaktivieren, indem Sie diese Option deaktivieren. Auf diese
Weise können Sie eine von der Zentrale abweichende Einstellung des Boosters erzwingen (nicht
empfohlen).

Gleisspannung
Hier kann der Soll-Wert für die Gleisspannung eingestellt werden. Dieses Eingabefeld ist ausgegraut,
solange die „Auto-Settings“ (siehe oben) in den Einstellungen aktiviert sind. Der
Gleisspannungsvorgabewert kommt aber immer dann zu tragen, wenn entweder die Einstellungen der
Zentrale nicht ermittelt werden können (B-BUS), oder die „Auto-Settings“ deaktiviert sind.

RailCom aktivieren
Mit dieser Option kann die Erzeugung einer RailCom-Lücke aktiviert/deaktiviert werden. Diese Checkbox
ist ausgegraut so lange die „Auto-Settings“ (siehe oben) in den Einstellungen aktiviert sind. Der RailCom-
Vorgabewert kommt aber immer dann zu tragen, wenn entweder die Einstellungen der Zentrale nicht
ermittelt werden können (B-BUS), oder die „Auto-Settings“ deaktiviert sind.
WICHTIG: Wenn angrenzende Booster-Abschnitte keine RailCom-Lücke erzeugen, dann muss diese
Option deaktiviert werden. Siehe Booster-Anleitung.

RailCom-Kanal 2 an Zentrale weiterleiten
Mit dieser Option wird die Weiterleitung der vom Booster empfangenen RailCom-Daten (RailCom-Kanal
2, d.h. Geschwindigkeit, POM-Read-Result, QoS, etc.) an die Z21 aktiviert. Der Z21-Booster 10 806
verfügt über einen, der 10807 pro Gleisausgang über je einen RailCom-Empfänger und kann die
empfangenen Daten über den CAN-Bus an die Z21 weiterleiten. Dadurch ist zum Beispiel das Auslesen
eines Fahrzeug-Decoders mittels POM-Lesebefehle nicht nur am Hauptgleis der Zentrale, sondern auch
im Booster-Abschnitt möglich.

DCC-Bremsgenerator aktivieren
Mit dieser Option kann der Gleisausgang des Z21-Boosters als Ersatz für den Artikel 10779
„Bremsgenerator“ verwendet werden. Siehe auch Booster-Anleitung.

Kurzschluss an Zentrale melden
Wenn diese Option deaktiviert wird, erfolgt keine Weiterleitung von Kurzschlussmeldungen an die
Zentrale. Der Betrieb kann in den nicht betroffenen Booster-Abschnitten bzw. am Hauptgleis der Zentrale
weitergeführt werden.
Der betroffene Z 21 - Booster schaltet dennoch bei Kurzschlüssen ab und versucht automatisch alle 3
Sekunden den Gleisausgang wieder zu aktivieren.

Auto-Invertierung aktivieren
Diese Option aktiviert die Auto-Invertierung, die ein automatisches Umpolen des Gleissignals bewirkt,
wenn der Booster z.B. als Kehrschleifenmodul verwendet wird. Es ist aber auch praktisch, um nicht
immer auf die korrekte Polung des Gleissignals achten zu müssen.
WICHTIG: Bei angrenzenden Booster-Abschnitten darf nur bei einem der beiden Booster diese Option
aktiviert sein, da sonst beide gleichzeitig umpolen würden, was zu einem Kurzschluss führen würde.
Siehe auch Booster-Anleitung.

Kurzschlusserkennung (standardmäßig normal)
Mit dieser Option können Sie die Ansprechgeschwindigkeit der Kurzschlusserkennung am Booster-
Gleisausgang erhöhen. Dies kann vor allem für Anlagen in Spur N sinnvoll sein.

Auto-Invertierung (standardmäßig schnell)
Mit dieser Option können Sie die Ansprechgeschwindigkeit der automatischen Umpolung einstellen.
Unsere Langzeittests haben gezeigt, dass es bei Auto-Invertierungs-Werten unter 15 (sehr schnelles


Umpolverhalten) sowie über 200 (sehr langsames Umpolverhalten) zu Problemen kommen kann. Wir
empfehlen daher die Standardeinstellung 20 beizubehalten und diesen Wert nur im Falle von Konflikten
zu verändern.

Mit Default Werte können Sie die Werkseinstellungen des Z21-Boosters in den Eingabefeldern des
Dialogfensters wiederherstellen.

Die Werte der Eingabefelder werden mit dem Button Schreiben in den Z21-Booster übertragen.

Mit Lesen werden die Einstellungen aus dem Z21-Booster erneut ausgelesen und angezeigt.

Mit FW Update können Sie die Firmware im Z21-Booster aktualisieren. Im Feld Update-Info werden die
Zielversion bzw. Informationen zum Update-Verlauf angezeigt.


3.5.2.2 Roco 10869 XL Booster-Setup
Im Booster-Setup-Dialog können Sie den ausgewählten Z21 XL Booster 10869 konfigurieren oder ein
Update der Booster-Firmware durchführen.

RailCom: Vorgaben automatisch über CAN von der Zentrale übernehmen ( = „Auto-Settings“)
Falls der Z21-Booster mit der Zentrale über den CAN-BUS verbunden ist, dann kann der Booster die
Einstellung für RailCom automatisch von der Zentrale übernehmen („Auto-Settings“). Falls die
Einstellungen aus der Zentrale aber nicht ermittelt werden können, z.B. weil der Booster über den B-BUS
verbunden ist, dann werden die im Booster gespeicherten Vorgaben für RailCom verwendet.
Sie können diese „Auto-Settings“ bei Bedarf deaktivieren, indem Sie diese Option deaktivieren. Auf diese
Weise können Sie eine von der Zentrale abweichende Einstellung des Boosters erzwingen (nicht
empfohlen).

RailCom aktivieren
Mit dieser Option kann die Erzeugung einer RailCom-Lücke aktiviert/deaktiviert werden. Diese Checkbox
ist ausgegraut so lange die „Auto-Settings“ (siehe oben) in den Einstellungen aktiviert sind. Der RailCom-
Vorgabewert kommt aber immer dann zu tragen, wenn entweder die Einstellungen der Zentrale nicht
ermittelt werden können (B-BUS), oder die „Auto-Settings“ deaktiviert sind.
WICHTIG: Wenn angrenzende Booster-Abschnitte keine RailCom-Lücke erzeugen, dann muss diese
Option deaktiviert werden. Siehe Booster-Anleitung.

RailCom-Kanal 2 an Zentrale weiterleiten
Mit dieser Option wird die Weiterleitung der vom Booster empfangenen RailCom-Daten (RailCom-Kanal
2, d.h. Geschwindigkeit, POM-Read-Result, QoS, etc.) an die Z21 aktiviert. Der Z21 XL Booster 10 869
verfügt über einen RailCom-Empfänger und kann die empfangenen Daten über den CAN-Bus an die Z2 1
weiterleiten. Dadurch ist zum Beispiel das Auslesen eines Fahrzeug-Decoders mittels POM-Lesebefehle
nicht nur am Hauptgleis der Zentrale, sondern auch im Booster-Abschnitt möglich.

Kurzschluss an Zentrale melden
Wenn diese Option deaktiviert wird, erfolgt keine Weiterleitung von Kurzschlussmeldungen an die
Zentrale. Der Betrieb kann in den nicht betroffenen Booster-Abschnitten bzw. am Hauptgleis der Zentrale
weitergeführt werden.
Der betroffene Z21-Booster schaltet dennoch bei Kurzschlüssen ab und versucht automatisch alle 3
Sekunden den Gleisausgang wieder zu aktivieren.


Kurzschlusserkennung (standardmäßig normal)
Mit dieser Option können Sie die Ansprechgeschwindigkeit der Kurzschlusserkennung am Booster-
Gleisausgang erhöhen.

PowerOn-Retries
Diese Option kann bei Problemen mit Kurzschlussmeldungen beim (Wieder-)Einschalten des Booster-
Gleisausgangs helfen, falls die Personenwagen oder Anlagenbeleuchtungen noch mit Glühlampen
ausgestattet sind. Beim Einschalten kann in einer kalten Metalldraht-Glühlampe der Einschaltstrom für
eine kurze Zeit bis zum Fünfzehnfachen des Normalwerts betragen. Durch Erhöhen der PowerOn-Retries
wird der Z21 XL BOOSTER fehlertoleranter gegenüber zu hohen Einschaltstromspitzen, ohne dabei die
elektronischen Bauteile unnötig zu überlasten.
Ausführliche Beschreibung siehe Betriebsanleitung 10869 Z21 XL BOOSTER.

Mit Default Werte können Sie die Werkseinstellungen des Z21-Boosters in den Eingabefeldern des
Dialogfensters wiederherstellen.

Die Werte der Eingabefelder werden mit dem Button Schreiben in den Z21-Booster übertragen.

Mit Lesen werden die Einstellungen aus dem Z21-Booster erneut ausgelesen und angezeigt.

Mit FW Update können Sie die Firmware im Z21-Booster aktualisieren. Im Feld Update-Info werden die
Zielversion bzw. Informationen zum Update-Verlauf angezeigt.


## 3.6 R-BUS

### 3.6.1 Rückmeldebus

Ab Maintenance Tool V1.02 können Sie hier den aktuellen Status der Eingänge Ihrer Rückmeldemodule
(Roco 107 87 , 10808 und 10819) sehen sowie einzelnen Rückmeldemodulen eine neue Modul-Adresse
zuweisen.

Ein aktiver Rückmeldeeingang wird rot eingefärbt (belegt).
Ein inaktiver Rückmeldeeingang wird grün eingefärbt (frei).

Beispiel mit aktivem Eingang 5 am Modul 1.

Siehe auch Abschnitt 3.11.1 Detector Monitor.


Wie bereits erwähnt, kann hier auch die Moduladresse eines Rückmeldemoduls (Roco 10787 , 10808 und
10819 ) geändert werden. Wichtig ist dabei, dass vor diesem Vorgang alle anderen Rückmeldemodule
vom R-BUS der Z21 getrennt werden, um zu verhindern, dass diese ebenfalls auf die neue Adresse
umprogrammiert werden.

Geben Sie die gewünschte neue Moduladresse ein; der erlaubte Bereich ist dabei 1 bis 20. Beachten Sie
außerdem, dass die verwendeten Rückmeldemodule aufsteigend von 1 durchgehend zu nummerieren
sind. Siehe auch die Bedienungsanleitung des Moduls. Die Werkseinstellung ist 1.

Betätigen Sie nun „Programmieren...“ und folgen Sie den Dialogen.

Wenn alle Module von der R-BUS Buchse der Z21 getrennt sind, betätigen Sie mit „OK“.

Die Z21 schaltet nun den R-BUS in den Programmiermodus um und der nächste Dialog erscheint.

Stecken Sie den zu programmierenden Belegtmelder als einziges Modul an den R-BUS an. Die acht
Status-LEDs des Modul leuchten zuerst hintereinander auf. Danach wird die alte Moduladresse auf den
LEDs angezeigt und kurz darauf die neue Adresse zur Bestätigung der erfolgreichen Programmierung.
Die Adress-Codierung über die LEDs finden Sie in der Bedienungsanleitung des Rückmeldemoduls.
Drücken Sie erst danach auf „OK“ des oben gezeigten Dialogs.

Es erscheint folgende abschließende Meldung:

Das Modul ist jetzt fertig programmiert und kann abgesteckt werden.

Nach dem Beenden dieses letzten Dialogs wird der Programmiermodus des R-BUS verlassen und wieder
auf Normalbetrieb umgestellt. Sie können jetzt alle Rückmelder wieder mit der Z21 verbinden.


### 3.6.2 XpressNet

Falls Sie am R-BUS keine Rückmelder verwenden und gleichzeitig der bestehende X-BUS für Ihre
Anforderungen nicht ausreichen sollte, können Sie ab Z21 Firmware V1.25 den R-BUS alternativ als
weiteren X-BUS für zusätzliche Handregler verwenden. Dies könnte z.B. bei Modultreffen mit mehr als 31
X-BUS-Handregler nützlich sein.

Beachten Sie bitte sorgfältig die Informationen im Dialogfenster bezüglich Pin1 und Pin6 sowie den
Hinweis zum Firmwareupdate der multiMAUS.


## 3.7 multiMAUS Update

Hier können Sie Ihre multiMAUS (Roco 10810 , Roco 10835, Fleischmann 686810 ) auf den aktuellen
Firmware-Stand V2.0 0 bringen. Mit dieser neuen Firmware können Sie nun auch über die multiMAUS mit
der Z 2 1 (ab Z21 FW V1.42) nicht nur die Lokfunktionen F0 bis F 31 schalten und auf CV1 bis CV10 24
zugreifen (früher nur F0 bis F20 sowie CV1 bis CV255), sondern auch 2048 Weichen (früher nur 1024 )
schalten sowie die neue „Fangfunktion“ des Drehreglers nutzen.

ACHTUNG: Um die neuen Leistungsmerkmale im knappen Programmspeicher der multiMAUS
unterbringen zu können, musste allerdings der X-BUS-Master sowie DCC-Master aus der multiMAUS
entfernt werden. Das bedeutet, dass die multiMAUS ab Version V2.00 nur mehr an der Slave-Buchse
der inzwischen nicht mehr lieferbaren Digitalverstärker (Roco 10761 und 10764 ) verwendet werden kann.
Für den Betrieb mit vollwertigen Zentralen bringt das jedoch keine Nachteile. Ein Update auf die Version
V1.05 empfehlen wir nur mehr, wenn Sie die multiMAUS unbedingt an der Master-Buchse eines solchen
Digitalverstärkers verwenden müssen. Auch diese ältere Firmwareversion ist ebenfalls im Z21
Maintenance Tool enthalten und kann für das Update ausgewählt werden. Ein Downgrade von
multiMAUS V2.00 auf V1.05 ist natürlich jederzeit möglich.

Beim Firmware-Update können Sie die Sprachen auf Ihrer multiMAUS entweder unverändert lassen oder
alternativ eines von sechs Sprachpaketen zu je vier Sprachen auswählen. Dann schließen Sie die
multiMAUS als einziges Gerät am Rückmelder-Bus (R-BUS-Buchse) an und betätigen Sie „Aktualisieren“.
Stecken Sie die multiMAUS nicht während des Updates ab. Warten Sie, bis das Update abgeschlossen
ist.


### 3.7.1 multiMAUS Fehlermeldungen

Sollte während des Firmware-Updates einmal ein Verbindungsproblem auftreten oder es aus
irgendwelchen Gründen zu einem Abbruch kommen, dann stellt auch das normalerweise kein
großes Problem dar. Die multiMAUS zeigt dann beim Neustart in diesem Fall entweder „ERR98“
(keine Anzeigetexte vorhanden) oder „ERR99“ (keine Firmware vorhanden) an.

Der für den Update-Vorgang zuständige Bootloader in der multiMAUS bleibt aber nach wie vor
funktionsfähig. Sie müssen dann die Aktualisierung – gegebenenfalls auch mehrmals - erneut
starten, bis der Update-Vorgang komplett durchläuft, damit die multiMAUS wieder voll
funktionstüchtig wird.

Für das Durchführen des multiMAUS Firmware Updates wird die Z21 Firmware Version V1.42 oder höher
empfohlen.


## 3.8 Firmware Update

Hier kann die Firmware in der Z21 bzw. smartRail aktualisiert werden (je nachdem, mit welchem Gerät
der PC verbunden ist; automatische Erkennung)

Die Information, welche Firmware Versionen enthalten sind, finden Sie in Menü Hilfe – Info:

Zukünftige Firmware Updates werden in neuen Versionen des Maintenance Tools enthalten sein.


Nach dem FW-Update der Zentrale.

Sollte es während des Updates mal ein Verbindungsproblem geben oder es aus irgendeinem anderen
Grund schief gehen, dann stellt das normalerweise kein Problem dar. Der Bootloader in der Zentrale, der
für die Durchführung des Updates verantwortlich ist, kann nicht zerstört werden. Sie können das Update
also immer wieder erneut starten. (Anm.: Die Zentrale kann sich nach einen fehlgeschlagenen Update
ggf. mit FW V1.0x des Bootloaders der Z21 melden).

Am besten machen Sie nach einem Fehlschlag einen Kaltstart der Z21, geben dann Windows genug Zeit,
bis die Netzwerkschnittstelle wieder bereit ist (Icon in der Taskleiste rechts unten, ggf. ping) und führen
dann den Firmware Update erneut durch.

Bei wiederholten Problemen empfehlen wir möglichst eine direkte Kabelverbindung anstelle von WLAN
zu verwenden.


## 3.9 Decoder Update

Das Feature Decoder Update ist ausschließlich für von Roco unterstützte Zimo MX und MS Decoder
vorgesehen.

Mit dem Feature Decoder Update können sowohl Decoder-Firmware-Updates (Dateiendung „zsu“) als
auch Sound-Updates (Dateiendung „zpp“) eingespielt werden. Achten Sie bitte auf exzellenten
elektrischen Kontakt von der Z21 bis zum Decoder. Ggf. wird auch eine LAN-Kabelverbindung vom PC
zur Zentrale empfohlen, wenn Sie WLAN-Netzwerkprobleme jeglicher Art ausschließen möchten.

Beachten Sie außerdem auch die Hinweise von Zimo in den entsprechenden Decoder-Anleitungen.

Die Zimo-Decoder-Firmware muss bei Zimo (www.zimo.at – Update & Sound – Update MX-Decoder,
bzw. Update MS-Decoder) vorher selbst runtergeladen und auf dem PC entpackt worden sein. Gleiches
gilt auch für Soundprojekte, welche ebenfalls bei Zimo (www.zimo.at – Update & Sound – ZIMO Sound
Database) heruntergeladen werden können.

Wählen Sie nun die bereits heruntergeladene und entpackte Zimo-Firmware-Datei aus.

Nach „Aktualisieren...“ kommt ein obligatorischer Disclaimer.


Vor dem eigentlichen Update-Vorgang werden automatisch der Hersteller des Decoders (CV 8) und ggf.
auch die Programmier- und Update-Sperre (CV 144) sowie Decoder-Typ (CV 250) überprüft.

Sollte es bereits beim Überprüfen der CVs irgendwelche Leseprobleme geben, dann kann man das durch
Deaktivieren der nun automatisch angezeigten Checkbox „CV-Prüfung“ umgehen und den Update-
Vorgang erneut starten.

Beachten Sie bitte auch die Hinweise beim Button „Info...“.

#### TIPP:

Sollte das Decoder Update fehlschlagen, überprüfen Sie bitte den Kontakt bis zum Decoder und
probieren Sie es erneut. Bei anhaltenden Problemen versuchen Sie, falls möglich, den Decoder in eine
andere Lok zu stecken. Es ist durchaus möglich, dass es mit einer anderen Lokplatine tatsächlich besser
funktioniert (aufgrund von anderen Kondensatoren etc.).


#### TIPP:

Falls Sie ein sogenanntes „Coded Sound-Projekt“ einspielen möchten, dann schrieben Sie den
Ladecode unbedingt vorher in den Decoder hinein. Siehe auch die entsprechende Decoder-Anleitung.

Ein „Coded Sound-Projekt“ wird vom Z21 Maintenance Tool automatisch erkannt, und vor dem Sound-
Update wird dann noch ein entsprechender Hinweis angezeigt.

#### TIPP:

Verwenden Sie zum Updaten von Lok-Decodern für Großbahnen die Z21XL, wo die Programmier-
endstufe besser auf den erhöhten Strombedarf der großen Decoder abgestimmt ist.
Schalten Sie Decoder mit großen Energiespeichern (z.B. MX6 99 , MS990) nach dem Update für eine
Minute stromlos, indem Sie zum Beispiel den Decoder samt Lok von Gleis nehmen, um so alle
Pufferkondensatoren für den nächsten Neustart zu entleeren.
Falls am Anfang des Updates die Fehlermeldung „Fehler beim Senden der Blockanzahl“ kommen sollte,
dann kann die Ursache an einem noch immer zu hohen ACK-Stromimpuls des Decoders liegen. In dem
Fall kann man den Strom mit einem Widerstand mit 4,7 Ohm ( 5 Watt) begrenzen, welchen man mit dem
Decoder in Reihe schaltet.

### 3.9.1 Hinweise zum MX Decoder Update

Damit ein Update mit einem Zimo MX Decoder möglich ist, muss eventuell zuerst über CV 144 die
sogenannte „Programmier- und Update-Sperre“ im Decoder aufgehoben werden:

- CV 144 = 0 Keine Programmier- und Update-Sperre.
- CV 144 Bit 6 = 1 Der Decoder kann im 'Service-Modus' nicht programmiert werden.
    (Schutzmaßnahme gegen versehentliches Umprogrammieren und Löschen)
- CV 144 Bit 7= 1 Sperre des Software-Updates.

In einigen Sound-Projekten ist diese Sperre gegen versehentliche Änderungen gesetzt.

Die von Roco unterstützten MX Decoder beherrschen das sogenannte „sichere Update“. Das bedeutet,
dass selbst nach einem Abbruch des Update-Vorgangs der Decoder weiter ansprechbar bleibt. Das
Update kann daher vom Anwender so oft wiederholt werden, bis es erfolgreich durchläuft. Einige ältere,
längst abgekündigte MX Decoder (z.B. MX 620 , ...) beherrschen dieses „sichere Update“ jedoch noch
nicht und bleiben deshalb für das Update mit der Z21 explizit gesperrt.


### 3.9.2 Hinweise zum MS Decoder Update

Das MS Decoder Update-Verfahren wird ab Z21 Firmware V1. 41 unterstützt.

ACHTUNG! Das Updaten von MS Decodern, die vor März 2021 produziert worden sind, ist mit der
Z 21 nicht erlaubt, denn bei diesen älteren MS Decoder laufen teilweise Bootloader, Firmware und
Hardware nicht stabil genug für einen sicheren und zuverlässigen Update-Vorgang! Solche älteren
MS Decoder sind auch aus diesem Grund nicht von Roco vertrieben worden. Aufgrund der Instabilitäten
kann von Roco ein erfolgreiches Decoder-Update mit diesen älteren MS Decodern nicht gewährleistet
werden.

- MS Decoder Firmware-Update wird erst ab MS Decoder-Bootloader V2.2 vollständig unterstützt.
    Die Bootloader-Version Ihres MS Decoders können Sie über CV 248 und CV 249 kontrollieren.
    Alle seit März 2021 von Zimo ausgeliefert MS Decoder enthalten die Bootloader Version 2.2 oder
    höher, problematisch sind dagegen speziell die älteren MS450.
- MS Decoder Sound-Update wird erst ab MS Decoder-Firmware V4.97 unterstützt.
    Die Firmware-Version Ihres MS Decoders können Sie über CV 7 und CV 65 kontrollieren.
    Aktualisieren Sie also gegebenenfalls die MS Decoder-Firmware vor dem Sound-Update.

3.9.2.1 MS Decoder Firmware Update

ACHTUNG! Der Firmware-Update-Vorgang für MS Decoder wird erst ab MS Decoder-Bootloader-
Version V2.2 (ausgeliefert ab März 20 21) vollständig unterstützt.

Ein Update von älteren MS Decodern über die Z2 1 ist daher ausdrücklich nicht erlaubt! Wenden Sie
sich ggf. an den Decoder-Hersteller oder besorgen Sie sich das entsprechende Update-Gerät des
Decoder-Herstellers, falls Sie so alte MS Decoder unbedingt updaten müssen.


3.9.2.2 MS Decoder Sound Update

ACHTUNG! Der Sound-Update-Vorgang für MS Decoder wird erst ab MS Decoder-Firmware-Version
V 4. 97 (veröffentlicht ab Juni 2021 ) unterstützt, denn ältere Decoder-Firmware-Versionen reagieren nicht
zuverlässig **„** fail-safe **“** auf Verbindungsabbrüche! Ein Sound-Update über die Z21 mit älteren MS
Decoder-Firmware-Versionen ist daher ausdrücklich nicht erlaubt! Führen Sie daher ggf. vor dem
Sound-Update zuerst ein Decoder Firmware-Update auf mindestens V4.9 7 oder höher durch.

„Fail-Safe“ bedeutet beim Zimo MS Decoder: falls es beim Sound-Update mit Decoder-Firmware-Version
V4. 97 (oder höher) zu einem Verbindungsabbruch kommen sollte, z.B. aufgrund von schlechtem
Gleiskontakt, dann wird das automatisch erkannt und der Decoder geht beim nächsten Neustart in einen
sogenannten „Fail-Safe“-Zustand, wo der Decoder wenigstens für einen neuen Update Versuch
ansprechbar bleibt. Bei älteren MS Decoder-Firmware-Versionen könnte es dagegen passieren, dass der
Decoder nach einer Unterbrechung des Sound-Updates nicht mehr ansprechbar ist.

Damit der MS Decoder in den „Fail-Safe“-Zustand gehen kann, ist es allerdings wichtig, dass der MS
Decoder beim Neustart während seiner Fail-Safe-Initialisierung für mindestens 30 Sekunden lang mit
Spannung versorgt bleibt. Während dieser Zeitspanne wird im Z21 Maintenance Tool ein
Fortschrittsbalken angezeigt. Ab Decoder Firmware V4.102 blinken auch während der Fail-Safe-
Initialisierung die Stirnlampen der Lok. Danach können Sie das Update erneut starten.


Hinweis: Es liegt in der Natur der Sache, dass ein 16-bit Soundprojekt für den MS Decoder deutlich
größer ist als ein vergleichbares 8 - bit Soundprojekt für einen MX Decoder, und dass daher auch der
Sound-Update-Vorgang über das Gleis ebenfalls wesentlich länger dauert (circa 7 min pro Megabyte).
Das auf ausdrücklichen Kundenwunsch eingebaute Sound-Update über das Programmiergleis der Z21 ist
daher nur für die gelegentliche Anwendung im privaten Bereich zu empfehlen. Wenn Sie aber häufig
Soundprojekte in MS Decoder laden möchten, dann sollten Sie sich eventuell die Anschaffung eines
entsprechenden Update-Geräts des Decoder-Herstellers überlegen, mit welchem ein Sound-Update über
die SUSI-Schnittstelle des MS Decoders sehr viel schneller durchgeführt werden kann.

Im Z21 Maintenance Tool wird während des Update-Vorgangs im Fortschrittsbalken die geschätzte
Gesamtzeit und Restzeit angezeigt.

In der Statuszeile gibt es außerdem Informationen zur Übertragung wie etwa die Geschwindigkeit und die
Anzahl der Datenpakete, welche aufgrund von erkannten Übertragungsfehlern wiederholt werden
mussten. Wiederholungen im Promillebereich sind übrigens normal und völlig problemlos.

TIPP:
Geben Sie dem MS Decoder nach dem erfolgreichen Sound-Update noch einige Sekunden Zeit für die
Initialisierung seiner CVs.


## 3.10 CV Programmieren

Der **Dialog „CV Programmieren“ i** st ab Z21 Firmware V1.22 sichtbar.

### 3.10.1 Programmieren von DCC-Decodern

In diesem Dialog können Sie die CV von Lok- und Schaltartikel-Decodern programmieren – sowohl am
Programmiergleis im DCC Direct CV Modus als auch am Hauptgleis (POM „Programming on Main“).

Durch Auswählen der Option DCC POM Lok-Decoder ist es auch möglich, die CV am Hauptgleis (POM)
zu schreiben und sogar auszulesen, wenn RailCom sowohl in der Z21 als auch im RailCom-fähigen
Decoder aktiviert ist (üblicherweise CV28=3 und CV29 Bit 3=1, siehe auch Decoderanleitung). Bei der
Hauptgleis-Programmierung muss die Adresse des zu programmierenden Decoders angegeben werden.

Einige moderne Schaltartikel-Decoder (Weichendecoder, Signaldecoder) lassen auch eine CV-
Programmierung am Hauptgleis zu (siehe auch Decoder-Anleitung). Die neuesten Weichendecoder
erlauben sogar das Auslesen der CVs am Hauptgleis mittels RailCom. Dazu muss RailCom sowohl in der


Z21 als auch im RailCom-fähigen Decoder aktiviert sein (z. B. Z21 switch DECODER CV28=6 und CV29
Bit 3=1). Wählen Sie zum Programmieren des Weichendecoders die Optionen DCC POM Schaltartikel-
Decoder aus.
Jetzt ist es notwendig, die Adresse des Decoders zu kennen. Die Decoderadresse ist nicht mit der
Weichennummer zu verwechseln. Unter einer Decoderadresse werden nämlich bis zu vier verschiedene
Weichen angesprochen. Um für Sie die Zuordnung zu visualisieren, werden im Dialog unter der
gewählten Decoderadresse die entsprechenden Weichennummern automatisch angezeigt.

Sehr alte DCC Lok-Decoder beherrschen noch keine CV-Programmierung im „DCC Direct CV Modus“.
Damit Sie auch diese Decoder programmieren können, gibt es ab Z21 Firmware V1.25 die
Programmierung im sogenannten DCC Register Modus.


### 3.10.2 Programmieren von MM-Decodern

Ab Z21 Firmware Version V1.23 können Sie durch Auswählen von MM ́6021 Programmiermodus ́
sogar modernere Motorola-Decoder programmieren.

Das Programmieren von Motorola-Decodern war allerdings im ursprünglichen Motorola-Format nicht
vorgesehen. Daher gibt es zum Programmieren von Motorola-Decodern kein genormtes und
verbindliches Verfahren.

Für die Programmierung von Motorola-Decodern wurde in der Z21 der später eingeführte, sogenannte
„6 021 - Programmiermodus“ implementiert. Dieser erlaubt das Schreiben von Werten, jedoch nicht das
Auslesen. Ebenso kann der Erfolg der Schreibeoperation nicht automatisch überprüft werden.

Dieses Programmierverfahren funktioniert für viele Decoder von ESU, Zimo und Märklin, jedoch nicht
zwingend für alle Decoder. Beispielsweise können Motorola-Decoder mit DIP-Schaltern nicht
programmiert werden. Leider liegen uns keine Angaben seitens der Decoder-Hersteller vor, welche ihrer
Decoder dieses Programmierverfahren unterstützen und welche nicht.

Der erlaubte Bereich für CV-Nummer ist 1 bis 79. Manche Decoder akzeptieren nur CV-Werte von 0 bis
80, andere Werte von 0 bis 255. Siehe dazu die jeweilige Decoder-Beschreibung.

Für Multiprotokoll-Decoder wird aufgrund der vielfältigeren Möglichkeiten unbedingt die DCC CV-
Programmierung empfohlen!


### 3.10.3 Programmieren von CV-Sets

Mit der Option CV-Set Programmieren können Sie einfache, oft verwendete Programmiersequenzen
automatisieren. So funktioniert es: Mit jedem einfachen Text-Editor können Sie eine sogenannte CSV-
Datei erstellen. Jede Zeile entspricht dann einem CV-Schreibkommando und hat folgendes Format:

CV-Nummer **;** Wert **;** Beschreibung

Geben Sie CV-Nummer und Wert als dezimale Zahlen an. Die Beschreibung ist ein beliebiger Text.
Trennen Sie die Einträge jeweils durch einen Strichpunkt (Semikolon). Leerzeichen sind erlaubt.
Fortgeschrittene Anwender können den gewünschten Wert auch gerne in hexadezimaler Notation
angeben (z. B. 0x1C).

Beispiel zum Aktivieren von RailCom in einem Zimo Decoder:

29 ; 14 ; CV29 bit 3=1: enable RailCom
28 ; 3 ; enable RailCom channel 1 and 2 (data)

Speichern Sie im Editor die Datei mit der Endung *.csv ab. Wählen Sie danach eben diese Datei im Z21
Maintenance Tool aus.

Durch Betätigen von „CV-Set Schreiben“ wird diese Programmiersequenz in den Lok-Decoder
geschrieben.


### 3.10.4 DCC CV Rechner

Im Hauptmenü können Sie unter Hilfe den DCC CV Rechner finden. Er dient als Rechen- und Merkhilfe
für die oft benötigten Konfigurationsvariablen CV#1, CV#17 und CV#18 für die Berechnung der
sogenannten langen Lokadresse bis 9999 , sowie für die vielfältigen Einstellmöglichkeiten in CV#29.

Der DCC CV Rechner richtet sich dabei nach der Norm RCN- 225 für DCC Konfigurationsvariablen. Es
kann allerdings vorkommen, dass ein älterer Fahrzeugdecoder diese Norm nicht zur Gänze erfüllt.
Beachten Sie deswegen auch die Betriebsanleitung Ihres Decoder, bevor sie die hier berechneten Werte
in Ihren Decoder schreiben.


## 3.11 Optionen

Im Hauptmenü können Sie unter Optionen die Sprachumschaltung und eine Auswahl unterschiedlicher
Skins für die Benutzeroberfläche finden.

Daneben gibt es unter Optionen noch weitere Menüpunkte, die für das Einrichten und Testen von
Hardwarekomponenten rund um die Z21 nützlich sein können und in den folgenden Abschnitten
beschrieben werden.

### 3.11.1 Detector Monitor

In diesem Fenster werden alle R-BUS Rückmelder angezeigt.

Ein rotes Feld bedeutet „belegt“ und ein grünes Feld steht für „frei“.


In den Reitern CAN und LocoNet werden alle im System gefundenen Belegtmelder automatisch
angezeigt.

Falls der verwendetet Belegtmelder und die Lok es zulassen, wird im belegten Feld (rot) auch die per
RailCom ermittelte Lokadresse angzeigt.

Mit dem Button „Poll“ kann der Status der Belegtmelder auch explizit angefordert werden. Bei der „Report
Address 1017 “ handelt es sich um jene Report-Adresse, die zum Beispiel in den Uhlenbrock 63320
Belegtmeldern standardmäßig voreingestellt ist und zur Abfrage dieser Belegtmelder dient. Siehe
Benutzerhandbuch Uhlenbrock 63320.

### 3.11.2 RailCom Monitor

Im RailCom Monitor kann man eine Übersicht über die Rückmeldungen von RailCom-fähigen
Fahrzeugdecodern sehen.

Neben der Adresse und der Anzahl der empfangenen Pakete werden auch die vom Decoder
gemeldete aktuelle Geschwindigkeit (Speed) sowie seine Empfangsstatistik (QoS, Quality of Service)
angzeigt, falls der Decoder diese Daten auch tatsächlich an die Zentrale sendet.

Die angezeigte Geschwindigkeit sollte theoretisch „km/h“ entsprechen und kann im Decoder
üblicherweise mit einem Faktor nachjustiert werden.


Mit QoS kann ein Decoder seine Zahl fehlerhaft empfangener DCC Pakete pro Gesamtzahl melden. Je
niedriger der Zahlenwert dieser Empfangsstatistik ist, desto besser ist der Empfang im Decoder. Höhere
Werte können dagegen auf einen schlechten Kontakt hinweisen, z.B. durch verschmutzte Gleise.

Beachten Sie bitte, dass sich die Lokdecoder bezüglich der rückgemeldeten Daten noch immer recht
unterschiedlich verhalten, und einige melden auch gar keine Daten. Teilweise muss man die
Rückmeldung im Decoder auch erst per CV aktivieren (CV28, CV29, ...). Mehr Information dazu finden
Sie in der Betriebsanleitung zum jeweiligen Decoder oder beim Decoder-Hersteller. Der RailCom Monitor
kann daher nur zu Informations- und Testzwecken dienen. Inwieweit die gemeldeten Werte auch sinnvoll
sind, hängt dagegen vom einzelnen Decoder ab.

### 3.11.3 Lok Liste

Beschreibung siehe Abschnitt 3.4 LocoNet.

### 3.11.4 Lok Fahrpult

Wenn Sie Ihre Änderungen an den Decoder-CVs gleich ausprobieren oder einfach nur probeweise eine
Lok über den PC steuern möchten, können Sie das über das Lok-Fahrpult machen. Es können sogar
mehrere Fahrpulte mit unterschiedlichen Lokadressen gleichzeitig verwendet werden.

Das Lok-Fahrpult wird im Z21 Maintenance Tool über das Hauptmenü → Optionen → Lok Fahrpult oder
über die Tastenkombination Strg+L geöffnet.

Im Fahrpult können Sie dann die gewünschte Lokadresse eingeben. Es besteht außerdem die
Möglichkeit, die eingestellten Fahrstufen (Speedsteps) der Lok zu kontrollieren und zu verändern. Sobald
Sie einen Fahrbefehl oder eine Funktion an die Lok schicken, werden die eingestellten Speedsteps in der
Z2 1 gespeichert.

Die Lok kann mit der PC-Maus und mit den Tasten R, F, S, G und 0 bis 9 bedient werden. Die
Geschwindigkeit kann über die Cursortasten geändert werden.

Ab Z21 Firmware V1.42 sind hier auch F29 bis F31 verfügbar. Beachten Sie aber, dass aktuell nur sehr
wenige Fahrzeugdecoder tatsächlich die DCC Befehle für F29 bis F31 verstehen und ausführen.


### 3.11.5 Stellwerk

Das Fenster Stellwerk dient zum Testen von Magnetartikeln und Signalen, welche an Zubehördecoder
angeschlossen sind. Dieses Fenster wird im Z21 Maintenance Tool über das Hauptmenü → Optionen →
Stellwerk oder über die Tastenkombination Strg+S geöffnet.

Im Reiter „Basic“ können Weichen bzw sehr einfache Lichtsignale mit den sogenannten „Schaltbefehlen
für einfache Zubehördecoder“ bedient werden, wie es sie seit den Anfangszeiten von DCC gibt, und so
wie man sie auch mit einer multiMAUS schalten kann. „Rot“ steht dabei auch für „abzweigende Weiche“
und „Grün“ für „gerade Weiche“.

Ab Z21 FW Version 1.40 kann im Reiter DCCext Switch der Z 21 switch DECODER ( 108 36) auch über
die Schaltbefehle im neuen „erweiterten Zubehördecoder Paketformat“ angesteuert werden. DCCext
steht dabei für „extended DCC“. Dabei kann man nicht nur die Weichenstellung, sondern auch
gleichzeitig die Zeit angeben, wie lange der Magnetantrieb (Spule) eingeschaltet werden soll. Der Wert 0
steht für „ausschalten“, und der Wert 12 7 für „dauer-ein“, und beide Werte können zum Beispiel für
Beleuchtungen verwendet werden, aber niemals für nicht-endabgeschaltete Magnetantriebe! „Rot“ steht
auch hier wieder für „abzweigende Weiche“ und „Grün“ für „gerade Weiche“.

ACHTUNG: Achten Sie beim Testen unbedingt darauf, die Spulen Ihrer Weichenantriebe nicht durch eine
zu lange Einschaltdauer zu überlasten, wenn diese Antriebe über keine Endabschaltung verfügen!
Wählen Sie die kürzestmögliche Zeit, mit welcher ein zuverlässiger Schaltbetrieb möglich ist, und
beginnen Sie im Zweifelsfall mit dem Wert 1 , welcher der kürzest möglichen Einschaltdauer von 100ms
entspricht.


Ab Z21 FW Version 1. 40 kann im Reiter DCCext Signal der Z21 signal DECODER ( 10837 ) auch über
die Schaltbefehle im neuen „erweiterten Zubehördecoder Paketformat“ angesteuert werden. Dabei wird in
nur einem Schaltbefehl der gewünschte Signalbegriff eindeutig übertragen. Der dabei gültige
Wertebereich hängt stark vom konkreten Signal und den verfügbaren Signalbegriffen ab, übliche Werte
sind aber zum Beispiel:

- 0 ... absoluter Haltebegriff
- 4 ... Fahrt mit Geschwindigkeitsbegrenzung 40 km/h
- 6 ... Fahrt mit Geschwindigkeitsbegrenzung 60 km/h
- 16 ... freie Fahrt
- 65 (0x41) ... Rangieren erlaubt
- 66 (0x42) ... Dunkelschaltung (z.B. Lichtvorsignale)
- 69 (0x45) ... Ersatzsignal (erlaubt die Vorbeifahrt)

Den konkreten Wert zum gewünschten Signalbegriff Ihres Signals finden Sie für den Z21 signal
DECODER unter https://www.z21.eu/de/produkte/z21-signal-decoder/signaltypen jeweils unter „DCCext“.

DCCext-Schaltbefehle sind ab Z21 Firmware V1. 40 verfügbar.


### 3.11.6 Modellzeit

Die Modellzeituhr ist ab Z21 Firmware V1. 43 verfügbar (Default-Einstellung: deaktiviert).

Das Fenster „Modellzeit“ dient zur Anzeige und Konfiguration der in der Z21 eingebauten Modellzeituhr.
Eine Modellzeituhr erlaubt auch bei verkürzten Abständen zwischen den Modellbahnhöfen durch ihren
einstellbaren Zeit-Beschleunigungsfaktor einen vorbildgerechten Betrieb nach Fahrplan. Die aktuelle
Modellzeit kann auch auf das Gleis (DCC), Netzwerk und Busse (X-BUS und LocoNet) gesendet werden.

Hinweis: Nachdem die Z21 intern über keine Echtzeituhr-Hardware verfügt, beginnt die Modellzeit bei
jedem Neustart mit der voreingestellten Startzeit. Falls die Modellzeituhr beim Ausschalten der Z21
aktiviert war, dann wird die Modellzeituhr beim Einschalten automatisch wieder aktiviert und gestartet.

Die Modellzeituhr kann am Bildschirm in drei verschiedenen Farben dargestellt werden:

- Grau: die Modellzeituhr ist angehalten (deaktiviert).
- Schwarz: die Modellzeituhr ist aktiviert und läuft mit dem voreingestellten Beschleunigungsfaktor.
- Blau: die Modellzeituhr ist aufgrund eines Not-Stopps (z.B. Gleisspannung-aus, Kurzschluss, ...)
    eingefroren und wird dann bei der Rückkehr in den Normalbetrieb automatisch wieder fortgesetzt.

Mit dem Button Start wird die Modellzeit gestartet, bzw. fortgesetzt.

Mit dem Button Stop wird die Modellzeit angehalten (dekativiert).

Mit dem Button Setzen werden die Einstellungen in die Z21 geschrieben und dort übernommen. Die
Einstellungen können nur bei angehaltener Modellzeit verändert und geschrieben werden.

Mit dem Button Mehr bzw. Weniger werden die erweiterten Einstellungen eingeblendet bzw.
ausgeblendet.


Einstellungen

Der **„** Beschleunigungsfaktor **“** bestimmt die Rate der Modellzeit. Faktor 1 bedeutet normale
Geschwindigkeit, Faktor 4 bedeutet vierfache Geschwindigkeit der Modellzeituhr, u.s.w.

Setzen Sie ein Häkchen bei **„** Modellzeit ändern **“** , wenn die eingegebene Uhrzeit als neue aktuelle
Modellzeit übernommen werden soll. Setzen Sie dieses Häkchen nicht, wenn Sie zum Beispiel nur den
Beschleunigungsfaktor bei ansonst unveränderter Modelluhrzeit anpassen möchten.

Setzen Sie ein Häkchen bei **„** Modellzeit als Default-Startzeit speichern **“** nur dann, wenn die
eingegebene Uhrzeit beim jedem Einschalten der Zentrale als neue Startzeit verwendet werden soll.
Setzen Sie dieses Häkchen nicht, wenn Sie zum Beispiel nur den Beschleunigungsfaktor bei ansonst
unveränderter Startzeit anpassen möchten.

Erweiterte Einstellungen

Setzen Sie ein Häkchen bei „Modellzeit bei Notaus anhalten“, wenn die Modellzeit während einem
Notstopp oder Kurzschluss eingefroren werden soll. Ein eventuelles Problem kann dann vom Benutzer
behoben werden, ohne dass dabei der Fahrplan durcheinander kommt.

Setzen Sie ein Häkchen bei „Modellzeit am LocoNet bereitstellen“, wenn die Modellzeit ggf. auch auf
einem LocoNet-Handregler angezeigt werden soll (z.B. DT 402 ; beachten Sie die Bedienungsanleitung
des jeweiligen Herstellers).

Setzen Sie ein Häkchen bei „Modellzeit am X-BUS ausgeben“, wenn die Modellzeit ggf. auch auf einem
X-BUS-Handregler angezeigt werden soll (z.B. LH101; beachten Sie die Bedienungsanleitung des
jeweiligen Herstellers). Hinweis: eine multiMAUS zeigt keine Modellzeit an.


Setzen Sie ein Häkchen bei „Modellzeit am Gleis ausgeben (DCC)“, wenn die Modellzeit regelmäßig
mit einem neuen, speziell zu diesem Zweck geschaffenen DCC-Befehl (RailCommunity Norm RCN- 211 )
auf das Gleis ausgegeben werden soll. Für zukünftige Decoder-Entwicklungen.

Setzen Sie ein Häkchen bei „Modellzeit an MRclock-Clients senden“, wenn die Modellzeit regelmäßig
auf der LAN Schnittstelle auch per „MRclock“-Multicast gesendet werden soll. Das erlaubt die
Verwendung von MRclock-Clients wie zum Beispiel die Android MRclock App zur Anzeige der Modellzeit.
Falls aktiviert, wird der MRclock Multicast dann einmal pro Modellminute (aber mindestens dreimal pro
echter Minute) an die Adresse 239.50.50.20, Port 2000 versendet.
ACHTUNG: Aufgrund von sehr durchwachsenen Integrationstestergebnissen mit MRclock-Clients auf
verschiedenen Geräten und Betriebssystemen kann von Roco ein reibungsloser Betrieb mit MRclock-
Clients nicht gewährleistet werden, weil nicht alle MRclock-Clients auf jedem System zuverlässig laufen.
Dieses Feature ist daher als „experimentell“ zu betrachten.

Modelluhr Darstellung

Durch einen Klick mit der rechten Maustaste im Ziffernblatt kann die Darstellung der analogen
Bildschirmuhr angepasst werden:

Bei einem hohen Beschleunigungsfaktor für die Modellzeit kann der Sprung des Zeigers als störend
empfunden werden. Setzen Sie das Häkchen bei „kontinuierliche Bewegung“, um eine fließende
Bewegung des Zeigers zu erreichen.

Entfernen Sie das Häkchen bei „Wochentag anzeigen“, um ggf. den Wochentag im Ziffernblatt
auszublenden.

Bei einem sehr hohen Beschleunigungsfaktor für die Modellzeit kann der schnell kreisende
Sekundenzeiger als störend empfunden werden. Entfernen Sie das Häkchen bei „Sekundenzeiger
anzeigen“, um den Sekundenzeiger auszublenden.

Ein Häkchen bei „Nebenuhr simulieren“ zeigt dann den typischen optischen Effekt, dass der voreilende
Sekundenzeiger bei der vollen Minute kurz anhält, während er auf den nächsten Minutenimpuls der
Hauptuhr wartet. Dieses Verhalten ist von diversen Bahhofsuhren bekannt. Für einen bestmöglichen
visuellen Effekt wählen Sie zusätzlich einen eher niedrigen Beschleunigungsfaktor und aktivieren Sie die
Option „kontinuierliche Bewegung.“


## 3.12 smartRail........................................................................................................................................................

Das Z 2 1 Maintenance Tool erkennt automatisch, mit welcher Hardware sie verbunden ist.

Neben den bereits vorgestellten Reitern Status, Einstellungen, IP Einstellungen und CV Programmieren
erscheinen im Fall von smartRail noch zwei zusätzliche Reiter, die nun beschrieben werden.

### 3.12.1 smartRail Status

Hier kann der aktuelle Betriebszustand Ihrer smartRail eingelesen werden. Des Weiteren kann der
Touch-Bedienoberfläche eine bestimmte Lokadresse zugewiesen werden (nur im Status „GO“, detto CV
Programmieren).


### 3.12.2 smartRail Einstellungen

Hier kann der Anwender verschiedene Feinjustierungen für smartRail durchführen, z. B. die Touch-
Empfindlichkeit an seine Bedürfnisse anpassen.

Sollten sich die Laufeigenschaften ungewollt verschlechtern, können alle Änderungen durch Drücken von
„Werkseinstellungen“ gefolgt von „Schreiben“ rückgängig gemacht werden.

Neu ab smartRail Firmware V1.14 sind die Optionen, mit welchen die Belegung des Silders für die
Fahrtrichtung „Vorwärts“ und „Rückwärts“ umgedreht sowie der Lok-Scanmodus für die eigene
Sammlung eingeschränkt werden kann. Dadurch wird der Scan-Vorgang in der Regel etwas schneller,
und es gibt weniger mögliche Fehlerquellen. Es gibt folgende Einstellungsmöglichkeiten:

- 2 - und 3-Leiter (auto): Es wird versucht, sowohl digitale 3 - Leiter-Loks (MM II, DCC) als auch
    digitale (DCC) und analoge (PWM) 2-Leiter-Loks automatisch zu erkennen.
    Dies ist die Default-Einstellung von smartRail.
- nur 3-Leiter: Der Scan-Vorgang für 2-Leiter-Loks wird ausgelassen.
- nur 2-Leiter (DCC, analog): Der Scan-Vorgang für 3-Leiter-Loks wird übersprungen. Es wird
    versucht automatisch zu erkennen, ob sich eine digitale oder analoge 2 - Leiter-Lok auf dem
    smartRail befindet.
- nur 2 - Leiter DCC: Der Scan-Vorgang für 3 - Leiter-Loks und analoge 2 - Leiter-Loks wird
    übersprungen.
- nur 2-Leiter analog: Nach dem Vermessen der Lok-Länge wird sofort in den analogen 2-Leiter-
    Modus (PWM) gewechselt.

Analoge Loks werden auf dem smartRail mittels niederfrequenter Pulsweitenmodulation (PWM)
gesteuert. Die niedrige Frequenz ist aufgrund von in den analogen Loks verbauten Entstörkondensatoren
zwingend notwendig. Je nach Modell und Hersteller sind diese Kondensatoren unterschiedlich
dimensioniert und können bei hoher Frequenz zu unnötig hohem Stromverbrauch und starker Erwärmung
führen. Niederfrequente PWM ist allerdings für Glockenankermotoren (z. B. von Faulhaber, Maxon, ...)
ungeeignet. Damit Sie Ihre hochwertigen, mit Glockenankermotoren ausgerüsteten Modelle ebenfalls auf
dem smartRail betreiben können, gibt es hier die neue Option „PWM Hochfrequenz“. Hinweis: Dieses
Feature befindet sich in der Testphase und wird für analoge Loks mit konventionellen
Gleichstrommotoren ausdrücklich nicht empfohlen.


Neu ab smartRail Firmware V1.15 ist die Option „Schneeschleuder Spezialfunktionen“. Wenn diese
Option aktiviert ist, erfolgt ein erweiterter Lok-Scan zum Erkennen der Beilhack Schneeschleuder. Wird
eine Schneeschleuder erkannt, wird eine spezielle Behandlung der Touch-Funktionstasten F0 und F4
aktiviert.

Durch kurzes Drücken von F0 wird wie gewohnt die normale Stirnbeleuchtung aktiviert. Durch langes
Drücken von F0 wird nach circa zwei Sekunden die Treppenbeleuchtung eingeschaltet. Nach weiteren
zwei Sekunden werden die hinteren roten Warnlichter aktiviert.

Über F4 wird das Drehen des Aufbaus gestartet. Dies ist nur bei nur bei Fahrstufe 0 möglich. Während
des Drehvorgangs bleibt das Laufband gebremst, um eine unkontrollierte Bewegung aufgrund der
Geometrieänderung des überwachten Modells zu unterbinden.


## 4 ZLINK

Die erstmals mit dem Z 21 single BOOSTER eingeführte zLink-Schnittstelle erlaubt es unter anderem,
sogar Geräte mit sehr kleinen Microcontrollern wie Weichen- oder Signaldecoder in sein eigenes
Netzwerk zu integrieren. Endgeräte mit zLink Schnittstelle sind mit Stand 0 6 /202 3 :

- 1080 6 Z21 single BOOSTER, 10807 Z21 dual BOOSTER, 10869 Z21 XL BOOSTER
- 10836 Z21 switch DECODER
- 10837 Z21 signal DECODER

An diese zLink Schnittstelle des Endgeräts kann der 10838 Z21 pro LINK angeschlossen werden, der als
Gateway zwischen WLAN und zLink für folgende Zwecke dient:

1. Konfiguration des Endgeräts
2. Firmware Update des Endgeräts
3. Steuerung des Endgeräts durch WLAN Clients, z.B. zwecks Tests während der Inbetriebnahme

Alle diese Aufgaben können auch mit dem Z21 Maintenance Tool durchgeführt werden, indem Sie den
Z21 pro LINK im Client Mode mit demselben WLAN Netzwerk verbinden, in welchem auch Ihr PC oder
Laptop mit dem Z21 Maintenance Tool angemeldet ist. Siehe dazu auch die Bedienungsanleitung des
108 38 Z21 pro LINK.

Wenn nun alle Geräte im WLAN Netzwerk angemeldet sind und der Z21 pro LINK am gewünschten
Endgerät wie Booster oder Decoder angeschlossen ist, dann gehen Sie am Z21 pro Link über die Tasten
in das Menü „Wireless“ und scrollen Sie runter bis zur Zeile „IP Address“. Die am Display angezeigte IP-
Adresse hat der Z 21 pro LINK vom Router erhalten, und sie kann sich natürlich in Ihrem Netzwerk auch
vom hier gezeigten Beispiel unterscheiden, z.B. 192.168.0.103 oder anders.

Geben Sie nun die auf Ihrem Z21 pro LINK angezeigte IP Adresse in das Eingabefeld vom Z 21
Maintenance Tool ein, und drücken Sie danach auf den Button „Verbinden“.

Das Z21 Maintenance Tool versucht nun das am 10838 Z2 1 pro LINK angeschlossene Gerät zu
erkennen. Das alles funktioniert ganz ohne DCC Zentrale. Es ist hier also keine Z2 1 zwingend
notwendig, denn der PC kommuniziert direkt über WLAN und den Z21 pro LINK mit dem Endgerät.

Für die Arbeit mit dem Z21 Maintenance Tool wird empfohlen, dass keine weiteren WLAN Clients wie
z.B. die Z21 App mit dem Z21 pro LINK verbunden sind.


## 4.1 Z21 BOOSTER

### 4.1.1 Status

Wenn der Z21 pro LINK an einem 10806 Z21 single BOOSTER, 10807 Z 21 dual BOOSTER oder 108 69
Z21 XL BOOSTER angesteckt ist, dann wird das erkannte Gerät grafisch dargestellt sowie der Status mit
der Firmware Version des Boosters angezeigt.

Im Reiter Booster Status werden noch mehr Informationen sichtbar.

Je nach Typ, single oder dual Booster, erscheinen ein oder zwei Panels mit der aktuellen Spannung und
dem Stromverbrauch am jeweiligen Gleisausgang. Die Icons unter den Panels zeigen den Status des
jeweiligen Gleisausgangs an:
Gleisausgang ist ausgeschaltet
Kurzschluss erkannt


Auto Settings Ok: die Vorgaben der Zentrale für Gleisspannung und RailCom wurden automatisch
über CAN vom Booster übernommen
Bremsgenerator-Modus aktiv
RailCom-Cutout aktiv: die RailCom-Lücke wird vom Booster im Gleissignal erzeugt
RailCom-Cutout aktiv + RailCom-Daten eines Lokdecoders werden im Booster empfangen

Außerdem werden noch weitere Status-Informationen zum DCC-Eingangssignal, CAN-Bus, Modus sowie
die Betriebstemperaturen im Booster angezeigt.

Beim Z21 single BOOSTER und Z21 XL BOOSTER entfallen natürlich jeweils die Anzeigen zum zweiten
Gleisausgang.

Mit einem Klick auf die Panels lassen sich ab Booster Firmware V1.11 die dazugehörende
Gleisausgänge einzeln aus- und wieder einschalten (Booster Management). Mit den Buttons STOP und
GO können Sie alle Gleisausgänge des Boosters gemeinsam aus- und wieder einschalten. Das
Einschalten kann selbstverständlich nur dann erfolgreich sein, wenn am Booster auch ein gültiges DCC-
Eingangssignal von der Zentrale anliegt.

### 4.1.2 Einstellungen

Im Reiter Booster Einstellungen finden Sie alle Optionen, die Sie an Ihrem Booster konfigurieren
können.

Die einzelnen Einstellungsmöglichkeiten wurden bereits in den Abschnitten 3.5.2.1 Roco 10806 und
10807 Booster-Setup und 3.5.2.2 Roco 10869 XL Booster-Setup erklärt, siehe ebenda. Mit dem Button
„Default Werte“ werden die Eingabefelder mit der Werkseinstellung ausgefüllt, die sie dann mit dem
Button „Schreiben“ in den Booster übertragen können.

Im Eingabefeld „Name“ können Sie in Ihrem Booster einen Namen (Freitext mit bis zu 1 6 Zeichen)
speichern, um später das Gerät wieder einmal identifizieren zu können.

### 4.1.3 Firmware Update

Im Reiter Firmware Update können Sie ggf. die Firmware Ihres Boosters aktualisieren.


## 4.2 Z21 switch DECODER

### 4.2.1 Status

Wenn der Z21 pro LINK an einem 10836 Z21 switch DECODER angesteckt ist, dann wird das erkannte
Gerät grafisch dargestellt sowie der Status mit der Firmware Version des Decoders angezeigt.

Im Reiter Decoder Status werden noch mehr Details sichtbar.

Neben Kontrollanzeigen für das Eingangssignal, Konfigurationsmodus, Kurzschluss und Strom werden
der Betriebsmodus und Status von jedem der 8 Ausgangspaare angezeigt. Rot bzw. Grün stehen dabei


jeweils für Ausgang A bzw. B. Eine helle Farbe bedeutet „ein“, eine dunkle Farbe „aus“. Ausgänge, die
seit dem Einschalten des Decoders noch nie geschaltet worden sind, werden grau dargestellt.

### 4.2.2 Schalten

Sie können die Ausgänge für Testzwecke sogar ohne DCC Zentrale schalten. Verwenden Sie dazu das
„Stellwerk“, welches im Abschnitt 3.11.5 Stellwerk beschrieben wird, und das über Menü Optionen –
Stellwerk oder über die Tastenkombination Strg+S geöffnet werden kann.

### 4.2.3 Einstellungen

Im Reiter Decoder Einstellungen kann der Decoder konfiguriert werden.


Hier können ganz ohne komplizierte CV-Tabellen die erste und zweite Decoder-Adresse, sowie der
Adressierungs-Modus, RailCom und die Zeiten für das sanfte Ein- und Ausblenden eingestellt werden.

Neben den beiden Decoder-Adressen werden die dazu gehörenden Weichennummern angezeigt, mit
denen die einzelnen Ausgangspaare z.B. mit der multiMAUS umgeschaltet werden können.

Für jedes Ausgangspaar kann der Betriebsmodus, ggf. die Einschaltdauer, ein Dimm-Wert für
Beleuchtungen und die Einschalt-Initialisierung individuell konfiguriert werden. Nähere Beschreibungen
zu den möglichen Werten finden Sie in der Betriebsanleitung zum 10836 Z21 switch DECODER.
Verwenden Sie das Dimmen der Ausgänge nur für Beleuchtungen und nicht bei Magnetantrieben.

Im Eingabefeld „Name“ können Sie in Ihrem Decoder einen Namen (Freitext mit bis zu 16 Zeichen) als
Merkhilfe hinterlegen. Damit können Sie später einmal den Decoder leichter identifizieren.

Mit dem Button „Decoder Reset“ können Sie den Decoder auf seine Werkseinstellung zurücksetzen.

### 4.2.4 Firmware Update

Im Reiter Firmware Update können Sie ggf. die Firmware Ihres Decoders aktualisieren.

### 4.2.5 CV Programmieren

Falls Sie den Decoder lieber über CV konfigurieren möchten, dann können Sie das im Reiter CV
Programmieren machen.

Auch diese CV-Programmierbefehle werden vom Z21 pro LINK über die zLink Schnittstelle direkt an den
Decoder weitergeleitet. Das heißt, sogar hier ist ebenfalls keine DCC Zentrale notwendig. Die
entsprechende CV-Tabelle finden Sie in der Betriebsanleitung zum 10836 Z21 switch DECODER.


## 4.3 Z21 signal DECODER

### 4.3.1 Status

Wenn der Z21 pro LINK an einem 10837 Z 21 signal DECODER angesteckt ist, dann wird das erkannte
Gerät wie üblich grafisch dargestellt sowie der Status mit der Firmware Version des Decoders angezeigt.

Im Reiter Decoder Status werden noch mehr Details sichtbar.

Neben Kontrollanzeigen zum Eingangssignal, Konfigurationsmodus, Kurzschluss und zur Spannung
werden jeweils der eingestellte Signaltyp und der aktuelle Signalbegriff angezeigt. Jeder aktive Ausgang


wird zusätzlich auch noch in der Grafik des Signaldecoder farblich markiert, wobei ein „Stern“ für einen
blinkenden Ausgang steht.

Vergrößern Sie das Fenster der Anwendung, um noch mehr Details auf den Signalschirmen erkennen zu
können.

Eine kleine Ziffer bei den „Lampen“ im Signalschirm zeigt die Reihenfolge an, an welcher Klemme des
Signaldecoders die LEDs jeweils anzuschließen sind.


### 4.3.2 Schalten

Sie können die Signalbegriffe für Testzwecke sogar ohne DCC Zentrale schalten. Verwenden Sie dazu
das „Stellwerk“, welches im Abschnitt 3.11.5 Stellwerk beschrieben wird und über Menü Optionen –
Stellwerk oder über die Tastenkombination Strg+S geöffnet werden kann.

Den konkreten DCCext-Wert zum gewünschten Signalbild Ihres Signals finden Sie für den Z21 signal
DECODER unter https://www.z21.eu/de/produkte/z2 1 - signal-decoder/signaltypen.

Noch komfortabler kann man ein Signal über einem Klick mit der rechten Maustaste auf die Signalgrafik
schalten.


Klicken Sie mit der rechten Maustaste in das Feld mit den Signalgrafiken. Dadurch wird ein Popup-Menü
sichtbar, in welchem Sie einen neuen Signalbegriff einfach auswählen und schalten können. Neben dem
Namen jedes Signalbegriffs ist zusätzlich jeweils links auch jener Wert zu sehen, mit welchem dieser
Signalbegriff mittels „„erweiterten Zubehördecoder Paketformat“ geschaltet werden könnte. DCCext steht
dabei für „extended DCC“. Mehr Informationen zum Thema „DCCext“ finden Sie in der
Betriebsanleitung zum 10 837 Z21 signal DECODER. Die Angabe erfolgt sowohl im dezimalen Format,
als auch in Klammer im hexadezimalen Format für den fortgeschrittenen Anwender. Mit diesem Popup-
Menü können Sie auch ohne „Nachschlagewerk“ die verschiedenen Signale auf sehr einfache Art und
Weise kennenlernen und ausprobieren, oder Ihr neues Signal auf korrekte Funktion testen.

### 4.3.3 Einstellungen

Im Reiter Decoder Einstellungen kann der Decoder konfiguriert werden.

Hier können ganz ohne CV-Tabellen die erste Decoder-Adresse, die Anzahl der Signale, der
Adressierungs-Modus und RailCom konfiguriert werden. Neben der Decoder-Adresse wird die dazu
gehörenden erste Signaladresse angezeigt, ab welcher die Signale zum Beispiel mit einer multiMAUS
oder mit der Z21 App geschaltet werden können.

Für jedes Signal kann eine von zahlreichen vorgefertigten Signalkonfigurationen sowie eine Einschalt-
Initialisierung individuell eingestellt werden. Standardmäßig ist es so konfiguriert, dass beim Einschalten
des Decoders jener Signalbegriff wiederhergestellt wird, der zuletzt vor dem Ausschalten gezeigt worden
ist. Sie können hier aber auch einen ganz bestimmten Signalbegriff auswählen, der beim Einschalten des
Decoders angezeigt werden soll.

Neben jeder aktiven Signalkonfiguration wird - abhängig von der eingestellten Anzahl der Signale -
angezeigt, an welchen Klemmenblock des Decoders das jeweilige Signal angeschlossen werden kann.

Mehr Informationen zu Decoder- und Signaladressen, Signalkonfiguration sowie zum Thema „DCCext“
finden Sie in der Betriebsanleitung zum 10 837 Z21 signal DECODER. Noch mehr Details über die
vorgefertigten Signalkonfigurationen finden Sie online unter https://www.z21.eu/de/produkte/z21-signal-
decoder/signaltypen.

Im Eingabefeld „Name“ können Sie in Ihrem Decoder einen Namen (Freitext mit bis zu 16 Zeichen) als
Merkhilfe hinterlegen. Damit können Sie später einmal das Gerät und die dazu gehörenden Signale
leichter identifizieren.

Mit dem Button „Decoder Reset“ können Sie den Decoder auf seine Werkseinstellung zurücksetzen.


### 4.3.4 Firmware Update

Im Reiter Firmware Update können Sie ggf. die Firmware und die Ressourcen Ihres Decoders
aktualisieren. Bei den Ressourcen handelt es sich um die vorgefertigten Signalkonfigurationen, die Ihnen
zur Auswahl bereitstehen.

### 4.3.5 CV Programmieren

Falls Sie den Decoder lieber über CV konfigurieren möchten, dann können Sie das im Reiter CV
Programmieren machen.

Alle CV-Programmierbefehle werden vom Z21 pro LINK ebenfalls über die zLink Schnittstelle direkt an
den Decoder weitergeleitet. Das heißt, sogar hier ist keine DCC Zentrale notwendig. Die entsprechende
CV-Tabelle finden Sie in der Betriebsanleitung zum 10837 Z21 signal DECODER.


