namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

using System.Collections.Generic;

public sealed partial class HelpPage : Page
{
    private readonly Dictionary<TreeViewNode, string> _sections = [];

    public HelpPage()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializePage();
    }

    private void InitializePage()
    {
        // Getting Started
        var gettingStarted = new TreeViewNode { Content = "Erste Schritte", IsExpanded = true };
        AddSection(gettingStarted, "Willkommen");
        AddSection(gettingStarted, "Installation");
        AddSection(gettingStarted, "Z21 Verbindung");
        AddSection(gettingStarted, "Erste Fahrt");
        NavigationTreeView.RootNodes.Add(gettingStarted);

        // Train Control
        var trainControl = new TreeViewNode { Content = "Loksteuerung", IsExpanded = false };
        AddSection(trainControl, "Geschwindigkeit");
        AddSection(trainControl, "Funktionen F0-F28");
        AddSection(trainControl, "Fahrtrichtung");
        AddSection(trainControl, "Notaus");
        NavigationTreeView.RootNodes.Add(trainControl);

        // Journeys & Automation
        var automation = new TreeViewNode { Content = "Automatisierung", IsExpanded = false };
        AddSection(automation, "Fahrten erstellen");
        AddSection(automation, "Stationen definieren");
        AddSection(automation, "Workflows");
        AddSection(automation, "Durchsagen");
        NavigationTreeView.RootNodes.Add(automation);

        // Signal Box
        var signalBox = new TreeViewNode { Content = "Stellwerk", IsExpanded = false };
        AddSection(signalBox, "Gleisplan Editor");
        AddSection(signalBox, "Signale");
        AddSection(signalBox, "Weichen");
        AddSection(signalBox, "Rueckmelder");
        NavigationTreeView.RootNodes.Add(signalBox);

        // Track Plan
        var trackPlan = new TreeViewNode { Content = "Gleisplan", IsExpanded = false };
        AddSection(trackPlan, "Gleisbibliotheken");
        AddSection(trackPlan, "Gleise platzieren");
        AddSection(trackPlan, "Verbindungen");
        NavigationTreeView.RootNodes.Add(trackPlan);

        // Troubleshooting
        var troubleshooting = new TreeViewNode { Content = "Problemloesung", IsExpanded = false };
        AddSection(troubleshooting, "Keine Verbindung");
        AddSection(troubleshooting, "Lok reagiert nicht");
        AddSection(troubleshooting, "Rueckmelder fehlen");
        NavigationTreeView.RootNodes.Add(troubleshooting);

        ShowContent("Willkommen");
    }

    private void AddSection(TreeViewNode parent, string topic)
    {
        var node = new TreeViewNode { Content = topic };
        _sections[node] = topic;
        parent.Children.Add(node);
    }

    private void OnNavigationItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _sections.TryGetValue(node, out var section))
        {
            ShowContent(section);
        }
    }

    private void ShowContent(string section)
    {
        HeaderTextBlock.Text = section;
        ContentRichTextBlock.Blocks.Clear();

        var content = section switch
        {
            // Getting Started
            "Willkommen" => """
                Willkommen bei MOBAflow - Ihrer Steuerungszentrale fuer Modelleisenbahnen!

                MOBAflow verbindet sich direkt mit Ihrer Roco/Fleischmann Z21 Digitalzentrale und bietet:

                - Loksteuerung mit Geschwindigkeit und Funktionen
                - Automatisierte Fahrten mit Stationsansagen
                - Workflow-Automatisierung fuer komplexe Ablauefe
                - Elektronisches Stellwerk (ESTW) fuer Signale und Weichen
                - Gleisplan-Editor mit verschiedenen Gleisbibliotheken

                Waehlen Sie links ein Thema, um mehr zu erfahren.
                """,

            "Installation" => """
                Systemanforderungen:
                - Windows 10/11 (64-bit)
                - .NET 10 Runtime
                - Netzwerkverbindung zur Z21

                Installation:
                1. Laden Sie MOBAflow von GitHub herunter
                2. Entpacken Sie das Archiv
                3. Starten Sie MOBAflow.exe
                4. Bei erster Ausfuehrung wird .NET Runtime automatisch installiert

                Updates:
                MOBAflow prueft automatisch auf Updates beim Start.
                """,

            "Z21 Verbindung" => """
                Verbindung zur Z21 herstellen:

                1. Stellen Sie sicher, dass Ihre Z21 eingeschaltet ist
                2. Verbinden Sie Ihren PC mit dem gleichen Netzwerk
                3. Geben Sie die IP-Adresse der Z21 ein (Standard: 192.168.0.111)
                4. Klicken Sie auf "Verbinden"

                Die Z21 wird per UDP auf Port 21105 angesprochen.

                Troubleshooting:
                - Pruefen Sie die Netzwerkverbindung
                - Stellen Sie sicher, dass keine Firewall blockiert
                - Z21 App auf dem Smartphone vorher beenden
                """,

            "Erste Fahrt" => """
                Ihre erste Lokfahrt:

                1. Verbinden Sie sich mit der Z21
                2. Gehen Sie zur Loksteuerung
                3. Waehlen Sie eine Lok aus oder geben Sie die DCC-Adresse ein
                4. Schalten Sie die Gleisspannung ein (Track Power)
                5. Bewegen Sie den Geschwindigkeitsregler

                Tipps:
                - F0 schaltet meist das Licht
                - Nutzen Sie die Fahrtrichtungs-Umschaltung
                - Bei Notfall: STOP-Taste druecken
                """,

            // Train Control
            "Geschwindigkeit" => """
                Geschwindigkeitssteuerung:

                Der Tachometer zeigt die aktuelle Fahrstufe (0-126).
                
                Bedienung:
                - Schieberegler fuer stufenlose Regelung
                - Voreinstellungen: Stop, Langsam, Normal, Schnell
                - Mausrad zum Feinjustieren

                DCC Fahrstufen:
                - 14 Fahrstufen (aeltere Decoder)
                - 28 Fahrstufen (Standard)
                - 126 Fahrstufen (empfohlen)
                """,

            "Funktionen F0-F28" => """
                Lokfunktionen:

                F0: Spitzensignal/Licht (fahrtrichtungsabhaengig)
                F1-F4: Haeufig Sound (Horn, Pfeife, etc.)
                F5-F8: Weitere Sounds oder Beleuchtung
                F9-F28: Zusatzfunktionen je nach Decoder

                Bedienung:
                - Klick auf Funktionstaste schaltet um
                - Leuchtende Taste = Funktion aktiv
                - Doppelklick fuer Momentfunktionen
                """,

            "Fahrtrichtung" => """
                Fahrtrichtung aendern:

                - Klicken Sie auf den Richtungspfeil
                - Oder druecken Sie die Leertaste
                - Bei Geschwindigkeit > 0 wird erst gebremst

                Hinweis:
                Die Richtung bezieht sich auf die Decoder-Einstellung.
                Bei falsch eingebauten Decodern kann die Richtung vertauscht sein.
                """,

            "Notaus" => """
                Notaus-Funktion:

                STOP-Taste: Sofortiger Halt aller Loks
                - Gleisspannung wird abgeschaltet
                - Alle Geschwindigkeiten auf 0

                Tastenkuerzel: Escape oder F12

                Nach Notaus:
                1. Ursache beheben
                2. Track Power wieder einschalten
                3. Loks fahren nicht automatisch weiter
                """,

            // Automation
            "Fahrten erstellen" => """
                Eine Fahrt (Journey) definieren:

                1. Neues Projekt erstellen oder oeffnen
                2. Gehen Sie zu "Fahrten"
                3. Klicken Sie auf "Neue Fahrt"
                4. Geben Sie einen Namen ein
                5. Fuegen Sie Stationen hinzu

                Eine Fahrt besteht aus:
                - Zugeordneter Lok (DCC-Adresse)
                - Liste von Stationen
                - Ansagetext-Vorlage
                """,

            "Stationen definieren" => """
                Stationen einer Fahrt:

                Jede Station hat:
                - Name (z.B. "Hamburg Hbf")
                - Rueckmelder-ID (Gleiskontakt)
                - Ausfahrt links/rechts (fuer Ansagen)

                Workflow-Trigger:
                Wenn ein Zug den Rueckmelder erreicht:
                1. Geschwindigkeit wird reduziert
                2. Ansage wird abgespielt
                3. Optionaler Workflow wird gestartet
                """,

            "Workflows" => """
                Workflow-Automatisierung:

                Ein Workflow ist eine Abfolge von Aktionen:
                - Lok steuern (Geschwindigkeit, Funktionen)
                - Warten (Zeit oder Rueckmelder)
                - Ansage abspielen
                - Weichen/Signale schalten

                Ausfuehrungsmodi:
                - Sequentiell: Aktionen nacheinander
                - Parallel: Aktionen gleichzeitig
                - Loop: Wiederholung
                """,

            "Durchsagen" => """
                Stationsansagen:

                MOBAflow unterstuetzt Text-to-Speech (TTS):
                - Windows Speech API (offline)
                - Azure Cognitive Services (online, natuerlicher)

                Ansagevorlagen:
                Verwenden Sie Platzhalter:
                {StationName} - Name der aktuellen Station
                {NextStation} - Naechste Station
                {TrainName} - Name des Zuges
                {Time} - Aktuelle Uhrzeit

                Beispiel: "Naechster Halt: {NextStation}"
                """,

            // Signal Box
            "Gleisplan Editor" => """
                ESTW - Elektronisches Stellwerk:

                Der Gleisplan-Editor ermoeglicht:
                - Grafische Darstellung Ihrer Anlage
                - Platzieren von Gleisen, Signalen, Weichen
                - Steuerung per Mausklick

                Bedienung:
                - Drag & Drop aus der Werkzeugleiste
                - Rechtsklick fuer Kontextmenue
                - Doppelklick zum Schalten
                - Rechte Maustaste zum Verschieben des Plans
                """,

            "Signale" => """
                Signalsteuerung:

                Unterstuetzte Signalsysteme:
                - Ks-Signale (Kombinationssignale)
                - H/V-Signale (Haupt-/Vorsignale)
                - Rangiersignale (Sh/Ra)

                Signalbegriffe:
                - Hp0: Halt
                - Ks1: Fahrt (gruen)
                - Ks2: Fahrt mit Geschwindigkeitsbegrenzung
                - Kennlicht: weisses Licht

                Doppelklick auf Signal wechselt den Begriff.
                """,

            "Weichen" => """
                Weichensteuerung:

                Weichentypen:
                - Einfache Weiche (links/rechts)
                - Dreiwegweiche
                - Kreuzungsweiche (DKW)

                Bedienung:
                - Doppelklick wechselt die Stellung
                - Gruen = Geradeaus
                - Gelb = Abzweig

                Die Weichenadresse wird in den Eigenschaften eingestellt.
                """,

            "Rueckmelder" => """
                Rueckmelder / Gleisbelegtmelder:

                Rueckmelder erkennen, wo sich Zuege befinden.
                Die Z21 sendet automatisch Meldungen bei Belegungsaenderungen.

                Typen:
                - Stromfuehler (Achszaehler)
                - Reed-Kontakte
                - Lichtschranken

                Konfiguration:
                Jeder Rueckmelder hat eine Adresse (1-1024).
                Diese wird in MOBAflow den Stationen zugeordnet.
                """,

            // Track Plan
            "Gleisbibliotheken" => """
                Verfuegbare Gleisbibliotheken:

                - Piko A-Gleis (aktiv)
                - Weitere in Planung...

                Die Bibliotheken enthalten:
                - Gerade Gleise (verschiedene Laengen)
                - Kurven (verschiedene Radien/Winkel)
                - Weichen
                - Kreuzungen
                - Prellboecke

                Gleise werden massstaeblich dargestellt.
                """,

            "Gleise platzieren" => """
                Gleise im Editor platzieren:

                1. Waehlen Sie ein Gleis aus der Bibliothek
                2. Ziehen Sie es auf den Gleisplan
                3. Drehen Sie mit R-Taste oder Rechtsklick
                4. Verbinden Sie mit anderen Gleisen

                Tipps:
                - Raster-Einrastung kann deaktiviert werden
                - Mehrfachauswahl mit Strg+Klick
                - Kopieren mit Strg+C, Einfuegen mit Strg+V
                """,

            "Verbindungen" => """
                Gleisverbindungen:

                Gleise verbinden sich automatisch, wenn:
                - Die Enden nah genug sind
                - Die Winkel kompatibel sind

                Anzeige:
                - Gruen: Verbunden
                - Rot: Nicht verbunden
                - Gelb: Warnung (Winkelabweichung)

                Nicht verbundene Enden sollten mit Prellboecken abgeschlossen werden.
                """,

            // Troubleshooting
            "Keine Verbindung" => """
                Problem: Keine Verbindung zur Z21

                Checkliste:
                1. Z21 eingeschaltet? (LED leuchtet)
                2. PC im gleichen Netzwerk?
                3. IP-Adresse korrekt? (Standard: 192.168.0.111)
                4. Z21 Maintenance Tool erreichbar?
                5. Firewall blockiert UDP Port 21105?

                Test:
                - Ping 192.168.0.111 in der Eingabeaufforderung
                - Z21 App auf Smartphone testen

                Loesung oft: Z21 kurz aus- und einschalten
                """,

            "Lok reagiert nicht" => """
                Problem: Lok reagiert nicht auf Befehle

                Checkliste:
                1. Gleisspannung eingeschaltet? (Track Power)
                2. Korrekte DCC-Adresse eingestellt?
                3. Lok auf dem Gleis aufgesetzt?
                4. Decoder-Typ kompatibel?

                Test:
                - Andere Lok testen
                - Lok auf Programmiergleis testen
                - CV1 (Adresse) pruefen

                Haeufige Ursache: Falsche Adresse oder Kontaktprobleme
                """,

            "Rueckmelder fehlen" => """
                Problem: Rueckmelder werden nicht erkannt

                Checkliste:
                1. Z21 erkennt Rueckmeldermodul?
                2. Richtige Adresse konfiguriert?
                3. Belegtmelder korrekt angeschlossen?
                4. Lok hat Stromabnahme auf diesem Abschnitt?

                Test:
                - Im Z21 Maintenance Tool pruefen
                - Manuell Kontakt ausloesen

                Hinweis: Rueckmeldungen kommen per UDP-Broadcast
                """,

            _ => "Waehlen Sie ein Thema aus dem Menue."
        };

        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var line in content.Split('\n'))
        {
            para.Inlines.Add(new Run { Text = line });
            para.Inlines.Add(new LineBreak());
        }

        ContentRichTextBlock.Blocks.Add(para);
    }
}
