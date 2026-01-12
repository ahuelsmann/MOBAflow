// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Plugin.Cmd;

/// <summary>
/// Maps MOBAcmd transaction codes to navigation tags.
/// Transaction codes follow command-style conventions (e.g., TC, JR, WF).
/// Only simple codes are supported - no numeric suffixes.
/// </summary>
public static class TransactionCodeMapper
{
    private static readonly Dictionary<string, string> TransactionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Train Operations
        { "TC", "traincontrol" },
        { "TR", "trains" },

        // Configuration
        { "JR", "journeys" },
        { "WF", "workflows" },

        // Layout Design
        { "TP", "trackplaneditor" },
        { "JM", "journeymap" },
        { "SB", "signalbox" },

        // System
        { "OV", "overview" },
        { "SOL", "solution" },
        { "SET", "settings" },
        { "MON", "monitor" },

        // Help
        { "HELP", "help" },

        // Info
        { "INFO", "info" },

        // CMD
        { "CMD", "cmd" },

        // Statistics
        { "STAT", "statistics" },
    };

    /// <summary>
    /// Maps a transaction code to its corresponding navigation tag.
    /// Supports short codes (TC, JR) only - no numeric suffixes.
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
            ("TR", "Trains - Locomotive and wagon management"),
            ("JR", "Journeys - Configure train journeys"),
            ("WF", "Workflows - Automation workflows"),
            ("TP", "Track Plan - Layout editor"),
            ("JM", "Journey Map - Visual journey display"),
            ("SB", "Signal Box - Electronic interlocking (ESTW)"),
            ("OV", "Overview - Dashboard"),
            ("SOL", "Solution - Project management"),
            ("SET", "Settings - Application configuration"),
            ("MON", "Monitor - System log viewer"),
            ("HELP", "Help - Documentation"),
            ("INFO", "Info - About MOBAflow"),
            ("STAT", "Statistics - Project statistics"),
            ("CMD", "CMD - Command navigation (this page)"),
        ];
    }
}
