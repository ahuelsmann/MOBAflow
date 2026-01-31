namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System.Reflection;

public sealed partial class InfoPage : Page
{
    private readonly Dictionary<TreeViewNode, string> _sections = [];

    public InfoPage()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializePage();
    }

    private void InitializePage()
    {
        // MOBAflow Info
        var mobaflow = new TreeViewNode { Content = "MOBAflow", IsExpanded = true };
        AddSection(mobaflow, "Ueber MOBAflow");
        AddSection(mobaflow, "Features");
        AddSection(mobaflow, "Systemanforderungen");
        NavigationTreeView.RootNodes.Add(mobaflow);

        // Version & Updates
        var version = new TreeViewNode { Content = "Version", IsExpanded = false };
        AddSection(version, "Aktuelle Version");
        AddSection(version, "Changelog");
        AddSection(version, "Roadmap");
        NavigationTreeView.RootNodes.Add(version);

        // Legal
        var legal = new TreeViewNode { Content = "Rechtliches", IsExpanded = false };
        AddSection(legal, "Lizenz");
        AddSection(legal, "Datenschutz");
        AddSection(legal, "Haftungsausschluss");
        NavigationTreeView.RootNodes.Add(legal);

        // Credits
        var credits = new TreeViewNode { Content = "Credits", IsExpanded = false };
        AddSection(credits, "Entwickler");
        AddSection(credits, "Open Source");
        AddSection(credits, "Danksagungen");
        NavigationTreeView.RootNodes.Add(credits);

        // Contact
        var contact = new TreeViewNode { Content = "Kontakt", IsExpanded = false };
        AddSection(contact, "Support");
        AddSection(contact, "Feedback");
        AddSection(contact, "Community");
        NavigationTreeView.RootNodes.Add(contact);

        ShowContent("Ueber MOBAflow");
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

        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        var versionString = $"{version.Major}.{version.Minor}.{version.Build}";

        var content = section switch
        {
            // MOBAflow
            "Ueber MOBAflow" => $"""
                MOBAflow - Model Railway Control & Automation
                Version {versionString}

                MOBAflow ist eine umfassende Steuerungsloesung fuer digitale Modelleisenbahnen 
                mit Roco/Fleischmann Z21 Digitalzentralen.

                Die Software ermoeglicht:
                - Direkte Loksteuerung ueber UDP
                - Automatisierte Fahrten mit Stationsansagen
                - Workflow-basierte Automatisierung
                - Elektronisches Stellwerk (ESTW)
                - Visueller Gleisplan-Editor

                MOBAflow ist Open Source und wird aktiv weiterentwickelt.
                """,

            "Features" => """
                Hauptfunktionen:

                Loksteuerung (MOBAflow)
                - Geschwindigkeitsregelung mit Tachometer-Anzeige
                - Funktionstasten F0-F28
                - Mehrere Loks gleichzeitig steuern

                Automatisierung
                - Fahrten mit mehreren Stationen
                - Text-to-Speech Ansagen (Windows/Azure)
                - Workflow-Engine fuer komplexe Ablauefe
                - Rueckmelder-basierte Trigger

                Stellwerk (ESTW)
                - Grafischer Gleisplan-Editor
                - Signal- und Weichensteuerung
                - Ks-Signalsystem

                Multi-Plattform
                - Windows Desktop (WinUI 3)
                - Android App (MAUI) - MOBAsmart
                - Web Dashboard (Blazor) - MOBAdash
                """,

            "Systemanforderungen" => """
                Minimale Anforderungen:

                Betriebssystem:
                - Windows 10 Version 1809 oder hoeher
                - Windows 11

                Hardware:
                - 64-bit Prozessor
                - 4 GB RAM
                - 100 MB Festplattenspeicher
                - Netzwerkverbindung (Ethernet oder WLAN)

                Software:
                - .NET 10 Runtime (wird bei Bedarf installiert)

                Z21 Digitalzentrale:
                - Roco Z21 (schwarz oder weiss)
                - Fleischmann Z21
                - Firmware 1.42 oder hoeher empfohlen
                """,

            // Version
            "Aktuelle Version" => $"""
                MOBAflow Version {versionString}

                Build-Datum: {DateTime.Now:yyyy-MM-dd}
                Framework: .NET 10
                UI: WinUI 3

                Komponenten:
                - MOBAflow (WinUI Desktop)
                - MOBAsmart (MAUI Android)
                - MOBAdash (Blazor Web)
                - MOBAtps (Track Plan System)
                """,

            "Changelog" => """
                Aenderungsprotokoll:

                Version 1.0.0 (aktuell)
                - Initiale Veroeffentlichung
                - Z21 UDP-Kommunikation
                - Loksteuerung mit Tachometer
                - Fahrten-Management
                - Workflow-Automatisierung
                - Stellwerk-Editor
                - Gleisplan-Editor mit Piko A-Gleis

                Geplante Features:
                - Weitere Gleisbibliotheken
                - Erweiterte Fahrstrassen
                - 3D-Ansicht
                """,

            "Roadmap" => """
                Geplante Entwicklung:

                Kurzfristig (Q1 2026):
                - Zusaetzliche Gleisbibliotheken (Maerklin C, Roco Line)
                - Verbesserte Rueckmelder-Visualisierung
                - Export/Import von Anlagen

                Mittelfristig (Q2-Q3 2026):
                - Fahrstrassen-Automatik
                - Zugverfolgung auf dem Gleisplan
                - Mehrzugsteuerung

                Langfristig (2027+):
                - 3D-Visualisierung
                - KI-gestuetzte Fahrplanoptimierung
                - Integration mit anderen Zentralen
                """,

            // Legal
            "Lizenz" => """
                MIT License

                Copyright (c) 2026 Andreas Huelsmann

                Permission is hereby granted, free of charge, to any person obtaining a copy
                of this software and associated documentation files (the "Software"), to deal
                in the Software without restriction, including without limitation the rights
                to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                copies of the Software, and to permit persons to whom the Software is
                furnished to do so, subject to the following conditions:

                The above copyright notice and this permission notice shall be included in all
                copies or substantial portions of the Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
                """,

            "Datenschutz" => """
                Datenschutzerklaerung:

                MOBAflow speichert folgende Daten lokal:
                - Anlagenkonfiguration (.mobaflow Dateien)
                - Benutzereinstellungen (appsettings.json)
                - Protokolldateien (logs/)

                Netzwerkkommunikation:
                - Nur direkte UDP-Verbindung zur Z21
                - Keine Daten an externe Server
                - Keine Telemetrie oder Tracking

                Azure Speech (optional):
                - Bei Nutzung von Azure TTS werden Texte an Microsoft gesendet
                - Kann durch Windows Speech ersetzt werden (offline)
                """,

            "Haftungsausschluss" => """
                Haftungsausschluss:

                Die Software wird "wie besehen" bereitgestellt, ohne jegliche Garantie.

                Der Autor haftet nicht fuer:
                - Schaeden an der Modelleisenbahn
                - Fehlfunktionen der Z21
                - Datenverlust
                - Indirekte oder Folgeschaeden

                Empfehlung:
                - Testen Sie neue Funktionen zunaechst im sicheren Umfeld
                - Erstellen Sie regelmaessig Backups Ihrer Konfiguration
                - Bei Problemen: Notaus-Funktion nutzen

                Die Nutzung erfolgt auf eigene Gefahr.
                """,

            // Credits
            "Entwickler" => """
                Entwicklung:

                Andreas Huelsmann
                - Konzept und Architektur
                - Hauptentwicklung
                - UI/UX Design

                Kontakt:
                - GitHub: github.com/ahuelsmann
                - Azure DevOps: dev.azure.com/ahuelsmann/MOBAflow
                """,

            "Open Source" => """
                Open Source Komponenten:

                .NET Foundation:
                - .NET 10 Runtime
                - WinUI 3
                - .NET MAUI
                - ASP.NET Core Blazor

                Community Toolkit:
                - CommunityToolkit.Mvvm
                - CommunityToolkit.WinUI

                Logging:
                - Serilog

                Testing:
                - NUnit

                Alle Lizenzen sind MIT oder Apache 2.0 kompatibel.
                """,

            "Danksagungen" => """
                Danke an:

                - Die .NET Community fuer exzellente Frameworks
                - Roco/Fleischmann fuer das Z21-System
                - Die Modelleisenbahn-Community fuer Feedback
                - Alle Beta-Tester

                Besonderer Dank:
                - Microsoft fuer WinUI 3 und .NET
                - Die Open Source Entwickler aller verwendeten Bibliotheken
                """,

            // Contact
            "Support" => """
                Support erhalten:

                GitHub Issues:
                Melden Sie Bugs oder Feature-Requests auf GitHub:
                github.com/ahuelsmann/MOBAflow/issues

                Wiki:
                Dokumentation und Anleitungen:
                github.com/ahuelsmann/MOBAflow/wiki

                FAQ:
                Haeufig gestellte Fragen finden Sie im Wiki.

                Bitte beschreiben Sie bei Problemen:
                - MOBAflow Version
                - Windows Version
                - Z21 Firmware Version
                - Schritte zur Reproduktion
                """,

            "Feedback" => """
                Feedback geben:

                Wir freuen uns ueber Ihr Feedback!

                Feature-Wuensche:
                Erstellen Sie ein Issue mit dem Label "enhancement"

                Verbesserungsvorschlaege:
                Pull Requests sind willkommen!

                Allgemeines Feedback:
                - GitHub Discussions
                - E-Mail (siehe GitHub Profil)

                Jedes Feedback hilft, MOBAflow besser zu machen.
                """,

            "Community" => """
                MOBAflow Community:

                GitHub:
                github.com/ahuelsmann/MOBAflow
                - Quellcode
                - Issues
                - Discussions
                - Wiki

                Foren:
                - 1zu160.net (Spur N Forum)
                - Stummis-Modellbahnforum
                - Bahn.de/modellbahn

                Soziale Medien:
                Folgen Sie dem Projekt auf GitHub fuer Updates.

                Beitragen:
                Contributions sind willkommen! Siehe CONTRIBUTING.md
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
