// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

/// <summary>
/// Helper class for managing Windows Firewall rules for WebApp UDP Discovery and HTTP REST-API.
/// Automatically creates firewall exceptions when WebApp starts.
/// </summary>
internal static class FirewallHelper
{
    private const string RuleNameUdp = "MOBAflow WebApp UDP Discovery";
    private const string RuleNameHttpPrefix = "MOBAflow WebApp REST-API";
    private const int UdpPort = 21106;

    /// <summary>
    /// Ensures Windows Firewall rules exist for WebApp UDP Discovery and REST-API.
    /// Creates rules if they don't exist, using netsh advfirewall command.
    /// </summary>
    /// <param name="httpPort">TCP port for the REST API inbound rule.</param>
    /// <param name="logger">Optional logger; failures are logged at warning level.</param>
    /// <returns>True if rules were created or verified successfully; false if an error occurred.</returns>
    public static bool EnsureFirewallRulesExist(int httpPort, ILogger? logger = null)
    {
        try
        {
            var httpRuleName = $"{RuleNameHttpPrefix} (Port {httpPort})";

            // Check and create UDP Discovery rule (Port 21106 Inbound)
            // Delete and recreate to ensure correct profile=any is applied
            if (FirewallRuleExists(RuleNameUdp))
            {
                DeleteFirewallRule(RuleNameUdp);
            }
            CreateUdpFirewallRule();

            // Check and create HTTP REST-API rule (Port httpPort Inbound)
            // Delete and recreate to ensure correct profile=any is applied
            if (FirewallRuleExists(httpRuleName))
            {
                DeleteFirewallRule(httpRuleName);
            }
            CreateHttpFirewallRule(httpRuleName, httpPort);

            return true;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Failed to create or verify Windows Firewall rules for REST API (port {HttpPort}). " +
                "This is normal without admin rights; create rules manually or run once as Administrator.",
                httpPort);
            return false;
        }
    }

    /// <summary>
    /// Deletes a firewall rule by name. Does not request elevation (no UAC prompt).
    /// Succeeds only when the process already has sufficient rights.
    /// </summary>
    private static void DeleteFirewallRule(string ruleName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    /// <summary>
    /// Checks if a firewall rule exists by name.
    /// </summary>
    private static bool FirewallRuleExists(string ruleName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // If rule exists, output contains "Rule Name:"
            return output.Contains("Rule Name:", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a Windows Firewall rule for UDP Discovery (Port 21106 Inbound).
    /// Requires Administrator privileges.
    /// </summary>
    private static void CreateUdpFirewallRule()
    {
        // Use profile=any to allow connections on all network types (private, domain, AND public)
        // Many home Wi-Fi networks are classified as Public by default
        var command = $"advfirewall firewall add rule " +
                     $"name=\"{RuleNameUdp}\" " +
                     $"dir=in " +
                     $"action=allow " +
                     $"protocol=UDP " +
                     $"localport={UdpPort} " +
                     $"profile=any " +
                     $"description=\"Allows MAUI clients to discover MOBAflow WebApp REST-API server via UDP broadcast\"";

        ExecuteNetshCommand(command);
    }

    /// <summary>
    /// Creates a Windows Firewall rule for HTTP REST-API (Port 5000 Inbound).
    /// Requires Administrator privileges.
    /// </summary>
    private static void CreateHttpFirewallRule(string ruleName, int httpPort)
    {
        // Use profile=any to allow connections on all network types (private, domain, AND public)
        // Many home Wi-Fi networks are classified as Public by default
        var command = $"advfirewall firewall add rule " +
                     $"name=\"{ruleName}\" " +
                     $"dir=in " +
                     $"action=allow " +
                     $"protocol=TCP " +
                     $"localport={httpPort} " +
                     $"profile=any " +
                     $"description=\"Allows MAUI clients to connect to MOBAflow WebApp REST-API\"";

        ExecuteNetshCommand(command);
    }

    /// <summary>
    /// Executes a netsh command to modify Windows Firewall rules.
    /// Does not request elevation (no UAC prompt). Succeeds only when the process already has sufficient rights.
    /// </summary>
    private static void ExecuteNetshCommand(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    /// <summary>
    /// Removes the WebApp firewall rules (cleanup on uninstall).
    /// </summary>
    public static void RemoveFirewallRules()
    {
        try
        {
            ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{RuleNameUdp}\"");
            ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{RuleNameHttpPrefix} (Port 5000)\"");
        }
        catch
        {
            // Best-effort cleanup; ignore failures (often insufficient rights).
        }
    }
}
