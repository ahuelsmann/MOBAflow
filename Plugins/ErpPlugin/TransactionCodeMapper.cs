// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Plugin.Erp;

/// <summary>
/// Maps MOBAerp transaction codes to navigation tags.
/// Transaction codes follow ERP-style conventions (e.g., TC, JR, WF).
/// </summary>
public static class TransactionCodeMapper
{
    private static readonly Dictionary<string, string> TransactionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Train Operations
        { "TC", "traincontrol" },
        { "TC01", "traincontrol" },
        { "TRAINCONTROL", "traincontrol" },
        { "TR", "trains" },
        { "TR01", "trains" },
        { "TRAINS", "trains" },

        // Configuration
        { "JR", "journeys" },
        { "JR01", "journeys" },
        { "JOURNEYS", "journeys" },
        { "WF", "workflows" },
        { "WF01", "workflows" },
        { "WORKFLOWS", "workflows" },

        // Layout Design
        { "TP", "trackplaneditor" },
        { "TP01", "trackplaneditor" },
        { "TRACKPLAN", "trackplaneditor" },
        { "JM", "journeymap" },
        { "JM01", "journeymap" },
        { "JOURNEYMAP", "journeymap" },
        { "SB", "signalbox" },
        { "SB01", "signalbox" },
        { "SIGNALBOX", "signalbox" },

        // System
        { "OV", "overview" },
        { "OV01", "overview" },
        { "OVERVIEW", "overview" },
        { "SOL", "solution" },
        { "SOL01", "solution" },
        { "SOLUTION", "solution" },
        { "SET", "settings" },
        { "SET01", "settings" },
        { "SETTINGS", "settings" },
        { "MON", "monitor" },
        { "MON01", "monitor" },
        { "MONITOR", "monitor" },

        // Help
        { "H1", "help1" },
        { "HELP1", "help1" },
        { "H2", "help2" },
        { "HELP2", "help2" },
        { "H3", "help3" },
        { "HELP3", "help3" },

        // ERP
        { "ERP", "erp" },
        { "MOBAERP", "erp" },
    };

    /// <summary>
    /// Maps a transaction code to its corresponding navigation tag.
    /// Supports both short codes (TC, JR) and full names (TRAINCONTROL, JOURNEYS).
    /// Also supports /N prefix (e.g., /NTC).
    /// </summary>
    /// <param name="command">The transaction code to map.</param>
    /// <returns>The navigation tag, or null if not found.</returns>
    public static string? MapToNavigationTag(string command)
    {
        var cleanCommand = command.StartsWith("/N", StringComparison.OrdinalIgnoreCase)
            ? command[2..]
            : command;

        return TransactionMappings.TryGetValue(cleanCommand, out var tag) ? tag : null;
    }

    /// <summary>
    /// Gets all available transaction codes with their descriptions.
    /// Used for displaying the transaction code reference.
    /// </summary>
    public static IReadOnlyList<(string Code, string Description)> GetAllTransactionCodes()
    {
        return
        [
            ("TC", "Train Control - Locomotive driving interface"),
            ("TC01", "Train Control with Speed Ramp"),
            ("JR", "Journeys - Configure train journeys"),
            ("JR01", "Journey Editor"),
            ("WF", "Workflows - Automation workflows"),
            ("WF01", "Workflow Editor"),
            ("TR", "Trains - Locomotive management"),
            ("TR01", "Train Details"),
            ("TP", "Track Plan - Layout editor"),
            ("TP01", "Track Plan Import"),
            ("JM", "Journey Map - Visual journey display"),
            ("JM01", "Map Settings"),
            ("SB", "Signal Box - Electronic interlocking"),
            ("SB01", "Track Diagram"),
            ("OV", "Overview - Dashboard"),
            ("OV01", "Quick Stats"),
            ("SOL", "Solution - Project management"),
            ("SOL01", "Solution Settings"),
            ("SET", "Settings - Application configuration"),
            ("SET01", "Connection Settings"),
            ("MON", "Monitor - System log viewer"),
            ("MON01", "Debug Log"),
            ("H1", "Help 1 - WebView2 Wiki"),
            ("H2", "Help 2 - TreeView Help"),
            ("H3", "Help 3 - TabView Help"),
        ];
    }
}
