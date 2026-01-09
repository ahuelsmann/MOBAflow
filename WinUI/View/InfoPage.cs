// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Windows.UI;

/// <summary>
/// Info Page - Displays README, License, and About information.
/// Renders Markdown-like content using RichTextBlock.
/// Shows MOBA* product family and trademark notices.
/// </summary>
public sealed partial class InfoPage : Page
{
    private readonly RichTextBlock _contentBlock;
    private readonly TreeView _navigationTree;
    private readonly TextBlock _headerText;
    private readonly Dictionary<TreeViewNode, string> _nodeToSection = [];

    public InfoPage()
    {
        _navigationTree = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        _navigationTree.ItemInvoked += OnTreeItemInvoked;

        _contentBlock = new RichTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

        _headerText = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        };

        InitializeNavigation();
        Content = BuildLayout();
        LoadSection("about");
    }

    private Grid BuildLayout()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Panel - Navigation
        var leftPanel = new Grid { Padding = new Thickness(16) };
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new TextBlock
        {
            Text = "MOBAflow",
            FontSize = 20,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Information & Legal",
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.Gray)
        });
        Grid.SetRow(header, 0);
        leftPanel.Children.Add(header);

        Grid.SetRow(_navigationTree, 1);
        leftPanel.Children.Add(_navigationTree);

        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Divider
        var divider = new Border
        {
            Width = 1,
            Background = new SolidColorBrush(Colors.Gray),
            Opacity = 0.3
        };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        // Right Panel - Content
        var rightScroll = new ScrollViewer
        {
            Padding = new Thickness(32, 24, 32, 24),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var contentPanel = new StackPanel 
        { 
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        contentPanel.Children.Add(_headerText);
        contentPanel.Children.Add(_contentBlock);
        rightScroll.Content = contentPanel;

        Grid.SetColumn(rightScroll, 2);
        grid.Children.Add(rightScroll);

        return grid;
    }

    private void InitializeNavigation()
    {
        var rootNode = new TreeViewNode { Content = "Information", IsExpanded = true };

        var aboutNode = new TreeViewNode { Content = "About MOBAflow" };
        _nodeToSection[aboutNode] = "about";
        rootNode.Children.Add(aboutNode);

        var productsNode = new TreeViewNode { Content = "MOBA* Products", IsExpanded = true };

        var flowNode = new TreeViewNode { Content = "MOBAflow" };
        _nodeToSection[flowNode] = "mobaflow";
        productsNode.Children.Add(flowNode);

        var smartNode = new TreeViewNode { Content = "MOBAsmart" };
        _nodeToSection[smartNode] = "mobasmart";
        productsNode.Children.Add(smartNode);

        var dashNode = new TreeViewNode { Content = "MOBAdash" };
        _nodeToSection[dashNode] = "mobadash";
        productsNode.Children.Add(dashNode);

        var erpNode = new TreeViewNode { Content = "MOBAerp" };
        _nodeToSection[erpNode] = "mobaerp";
        productsNode.Children.Add(erpNode);

        var ixlNode = new TreeViewNode { Content = "MOBAixl" };
        _nodeToSection[ixlNode] = "mobaIxl";
        productsNode.Children.Add(ixlNode);

        var tpsNode = new TreeViewNode { Content = "MOBAtps" };
        _nodeToSection[tpsNode] = "mobatps";
        productsNode.Children.Add(tpsNode);

        rootNode.Children.Add(productsNode);

        var legalNode = new TreeViewNode { Content = "Legal", IsExpanded = true };

        var licenseNode = new TreeViewNode { Content = "License (MIT)" };
        _nodeToSection[licenseNode] = "license";
        legalNode.Children.Add(licenseNode);

        var trademarkNode = new TreeViewNode { Content = "Trademarks" };
        _nodeToSection[trademarkNode] = "trademarks";
        legalNode.Children.Add(trademarkNode);

        var thirdPartyNode = new TreeViewNode { Content = "Third-Party Notices" };
        _nodeToSection[thirdPartyNode] = "thirdparty";
        legalNode.Children.Add(thirdPartyNode);

        rootNode.Children.Add(legalNode);

                var readmeNode = new TreeViewNode { Content = "README (Full)" };
                _nodeToSection[readmeNode] = "readme";
                rootNode.Children.Add(readmeNode);

                var creditsNode = new TreeViewNode { Content = "Credits & Thanks" };
                _nodeToSection[creditsNode] = "credits";
                rootNode.Children.Add(creditsNode);

                _navigationTree.RootNodes.Add(rootNode);
            }

    private void OnTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _nodeToSection.TryGetValue(node, out var section))
        {
            LoadSection(section);
        }
    }

    private void LoadSection(string sectionId)
    {
        _contentBlock.Blocks.Clear();

        switch (sectionId)
        {
            case "about":
                _headerText.Text = "About MOBAflow";
                AddParagraph("MOBAflow is a comprehensive model railway control platform that integrates with Roco/Fleischmann Z21 digital command stations.");
                AddHeading("What does MOBA mean?");
                AddParagraph("MOBA is short for Modellbahn (German for 'model railway'). The MOBA* product family represents our suite of tools for model railway enthusiasts.");
                AddHeading("Features");
                AddBulletList([
                    "Z21 Integration - Full control via LAN protocol",
                    "Journey Management - Automate train movements",
                    "Workflow Automation - Trigger actions on events",
                    "Track Planning - Design your layout",
                    "Multi-Platform - Windows, Mobile, and Web"
                ]);
                AddHeading("Version");
                AddParagraph($"Version: 3.15.0 (January 2026)");
                AddParagraph($"Runtime: .NET {Environment.Version}");
                break;

            case "mobaflow":
                _headerText.Text = "MOBAflow Desktop";
                AddParagraph("The main Windows desktop application built with WinUI 3 and .NET.");
                AddHeading("Key Features");
                AddBulletList([
                    "Modern Fluent Design interface",
                    "Full Z21 control (locomotives, turnouts, signals)",
                    "Journey and workflow management",
                    "Track plan editor (MOBAtps)",
                    "Interlocking system (MOBAixl)",
                    "Plugin system for extensibility",
                    "REST API for external integrations"
                ]);
                break;

            case "mobasmart":
                _headerText.Text = "MOBAsmart Mobile";
                AddParagraph("Mobile companion app for iOS and Android, built with .NET MAUI.");
                AddHeading("Features");
                AddBulletList([
                    "Remote locomotive control",
                    "Speed and function buttons",
                    "Connection to MOBAflow via REST API",
                    "Automatic server discovery",
                    "Optimized for touch input"
                ]);
                break;

            case "mobadash":
                _headerText.Text = "MOBAdash Web";
                AddParagraph("Browser-based dashboard built with Blazor WebAssembly.");
                AddHeading("Features");
                AddBulletList([
                    "Accessible from any device with a browser",
                    "Real-time status monitoring",
                    "SignalR for live updates",
                    "No installation required"
                ]);
                break;

            case "mobaerp":
                _headerText.Text = "MOBAerp Transaction System";
                AddParagraph("ERP-style transaction code navigation inspired by enterprise systems like SAP.");
                AddHeading("Transaction Codes");
                AddBulletList([
                    "TC - Train Control",
                    "JR - Journeys",
                    "WF - Workflows",
                    "TR - Trains",
                    "TP / TPS - Track Planner (MOBAtps)",
                    "SB / IXL - Signal Box (MOBAixl)",
                    "SET - Settings",
                    "HELP - Help / Wiki"
                ]);
                AddParagraph("Enter a code and press Enter for instant navigation.");
                break;

            case "mobaIxl":
                _headerText.Text = "MOBAixl Interlocking System";
                AddParagraph("Electronic interlocking (Elektronisches Stellwerk - ESTW) for realistic railway operations.");
                AddHeading("Signal Systems");
                AddBulletList([
                    "Ks (Kombinationssignal) - Modern unified signals",
                    "H/V (Haupt-/Vorsignal) - Classic light signals",
                    "Formsignale - Historical semaphore signals"
                ]);
                AddHeading("Features");
                AddBulletList([
                    "Visual track diagram (Gleisbild)",
                    "Drag & Drop element placement",
                    "Real-time feedback display",
                    "Signal and turnout control",
                    "Route setting (Fahrstrassen)"
                ]);
                break;

            case "mobatps":
                _headerText.Text = "MOBAtps Track Planner System";
                AddParagraph("Comprehensive track planning tool for designing your model railway layout.");
                AddHeading("TPS = Track Planner System");
                AddParagraph("Design, visualize, and plan your model railway track layout with an intuitive drag-and-drop interface.");
                AddHeading("Features");
                AddBulletList([
                    "Track library with common rail systems",
                    "Drag & Drop placement",
                    "Zoom and pan navigation",
                    "Feedback point assignment",
                    "AnyRail import support",
                    "Integration with MOBAixl"
                ]);
                break;

            case "license":
                _headerText.Text = "MIT License";
                AddParagraph("MOBAflow is open source software licensed under the MIT License.");
                AddHeading("License Text");
                AddCodeBlock(@"MIT License

Copyright (c) 2026 Andreas Huelsmann

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.");
                break;

            case "trademarks":
                _headerText.Text = "Trademark Notice";
                AddParagraph("The following are trademarks of Andreas Huelsmann:");
                AddBulletList([
                    "MOBAflow™ - Desktop application",
                    "MOBAsmart™ - Mobile application",
                    "MOBAdash™ - Web dashboard",
                    "MOBAerp™ - Transaction navigation system",
                    "MOBAixl™ - Interlocking system",
                    "MOBAtps™ - Track planner system"
                ]);
                AddHeading("Usage Guidelines");
                AddParagraph("These trademarks may not be used in connection with any product or service without prior written consent, except as required for reasonable and customary use in describing the origin of the software.");
                AddHeading("Third-Party Trademarks");
                AddBulletList([
                    "Z21® and z21® are registered trademarks of Modelleisenbahn GmbH",
                    "Roco® is a registered trademark of Modelleisenbahn GmbH",
                    "Fleischmann® is a registered trademark of Modelleisenbahn GmbH",
                    "Microsoft®, Windows®, .NET®, and Azure® are trademarks of Microsoft Corporation"
                ]);
                break;

            case "thirdparty":
                _headerText.Text = "Third-Party Notices";
                AddParagraph("MOBAflow uses the following open-source libraries and components:");
                AddHeading("NuGet Packages");
                AddBulletList([
                    "CommunityToolkit.Mvvm - MIT License",
                    "Microsoft.Extensions.* - MIT License",
                    "Serilog - Apache 2.0 License",
                    "Azure.AI.Speech - Microsoft License"
                ]);
                AddHeading("Icons");
                AddParagraph("UI icons from Microsoft Fluent UI System Icons (MIT License).");
                AddParagraph("See THIRD-PARTY-NOTICES.md for complete details.");
                break;

            case "credits":
                _headerText.Text = "Credits & Thanks";
                AddParagraph("MOBAflow is developed by Andreas Huelsmann.");
                AddHeading("Special Thanks");
                AddBulletList([
                    "The model railway community",
                    "Roco/Fleischmann for the Z21 system",
                    "Microsoft for .NET and WinUI",
                    "All contributors and testers"
                ]);
                AddHeading("Contact");
                AddParagraph("For questions, suggestions, or contributions, please visit the project repository or open an issue.");
                break;

            case "readme":
                _headerText.Text = "README";
                LoadReadmeContent();
                break;
        }
    }

    private void LoadReadmeContent()
    {
        try
        {
            var readmePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "README.md");

            // Try multiple possible locations
            if (!File.Exists(readmePath))
            {
                readmePath = Path.Combine(AppContext.BaseDirectory, "README.md");
            }
            if (!File.Exists(readmePath))
            {
                // Development path
                var devPath = Path.GetDirectoryName(AppContext.BaseDirectory);
                for (int i = 0; i < 5 && devPath != null; i++)
                {
                    var testPath = Path.Combine(devPath, "README.md");
                    if (File.Exists(testPath))
                    {
                        readmePath = testPath;
                        break;
                    }
                    devPath = Path.GetDirectoryName(devPath);
                }
            }

            if (File.Exists(readmePath))
            {
                var content = File.ReadAllText(readmePath);
                RenderMarkdown(content);
            }
            else
            {
                AddParagraph("README.md file not found.");
                AddParagraph($"Searched in: {AppContext.BaseDirectory}");

                // Show embedded README content instead
                AddHeading("MOBAflow - Model Railway Control Platform");
                AddParagraph("A comprehensive model railway control application for Windows, Mobile, and Web.");

                AddHeading("Features");
                AddBulletList([
                    "Z21 Integration - Connect to Roco/Fleischmann Z21 digital command stations",
                    "Train Control - Control locomotives with speed, direction, and functions",
                    "Journey Management - Create and execute automated train journeys",
                    "Workflow Automation - Trigger actions based on events",
                    "Track Planning (MOBAtps) - Design your layout",
                    "Interlocking (MOBAixl) - Electronic signal box",
                    "Mobile App (MOBAsmart) - Control from your phone",
                    "Web Dashboard (MOBAdash) - Browser-based monitoring"
                ]);

                AddHeading("Getting Started");
                AddBulletList([
                    "1. Connect your Z21 to your network",
                    "2. Open MOBAflow and go to Settings",
                    "3. Enter your Z21's IP address",
                    "4. Click Connect",
                    "5. Start controlling your trains!"
                ]);

                AddHeading("Requirements");
                AddBulletList([
                    "Windows 10/11 (64-bit)",
                    "Roco/Fleischmann Z21 or z21",
                    ".NET 9.0 Runtime"
                ]);

                AddHeading("License");
                AddParagraph("MIT License - See LICENSE file for details.");

                AddHeading("Trademarks");
                AddParagraph("MOBAflow™, MOBAsmart™, MOBAdash™, MOBAerp™, MOBAixl™, and MOBAtps™ are trademarks of Andreas Huelsmann.");
            }
        }
        catch (Exception ex)
        {
            AddParagraph($"Error loading README: {ex.Message}");
        }
    }

    private void RenderMarkdown(string markdown)
    {
        var lines = markdown.Split('\n');
        var inCodeBlock = false;
        var codeBlockContent = new List<string>();
        var inList = false;
        var listItems = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Code blocks
            if (line.StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(string.Join("\n", codeBlockContent));
                    codeBlockContent.Clear();
                }
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockContent.Add(line);
                continue;
            }

            // Flush list if we're leaving list context
            if (inList && !line.StartsWith("- ") && !line.StartsWith("* ") && !string.IsNullOrWhiteSpace(line))
            {
                if (listItems.Count > 0)
                {
                    AddBulletList([.. listItems]);
                    listItems.Clear();
                }
                inList = false;
            }

            // Headers
            if (line.StartsWith("# "))
            {
                // Skip H1 (title) or add as main heading
                var text = line[2..].Trim();
                _headerText.Text = text;
            }
            else if (line.StartsWith("## "))
            {
                AddHeading(line[3..].Trim());
            }
            else if (line.StartsWith("### "))
            {
                AddSubHeading(line[4..].Trim());
            }
            // List items
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                inList = true;
                listItems.Add(line[2..].Trim());
            }
            // Regular paragraph
            else if (!string.IsNullOrWhiteSpace(line))
            {
                // Skip badges and images
                if (line.StartsWith("![") || line.StartsWith("[!["))
                    continue;

                // Clean up markdown formatting
                var cleanLine = line
                    .Replace("**", "")
                    .Replace("*", "")
                    .Replace("`", "");

                AddParagraph(cleanLine);
            }
        }

        // Flush remaining list
        if (listItems.Count > 0)
        {
            AddBulletList([.. listItems]);
        }

        // Flush remaining code block
        if (codeBlockContent.Count > 0)
        {
            AddCodeBlock(string.Join("\n", codeBlockContent));
        }
    }

    private void AddSubHeading(string text)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 14, 0, 6) };
        paragraph.Inlines.Add(new Run
        {
            Text = text,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 150, 220))
        });
        _contentBlock.Blocks.Add(paragraph);
    }

    private void AddHeading(string text)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 20, 0, 8) };
        paragraph.Inlines.Add(new Run
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))
        });
        _contentBlock.Blocks.Add(paragraph);
    }

    private void AddParagraph(string text)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
        paragraph.Inlines.Add(new Run { Text = text });
        _contentBlock.Blocks.Add(paragraph);
    }

    private void AddBulletList(string[] items)
    {
        foreach (var item in items)
        {
            var paragraph = new Paragraph { Margin = new Thickness(16, 4, 0, 4) };
            paragraph.Inlines.Add(new Run { Text = "• " + item });
            _contentBlock.Blocks.Add(paragraph);
        }
    }

    private void AddCodeBlock(string code)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 8, 0, 8),
            FontFamily = new FontFamily("Consolas")
        };
        paragraph.Inlines.Add(new Run
        {
            Text = code,
            FontSize = 12
        });
        _contentBlock.Blocks.Add(paragraph);
    }
}
