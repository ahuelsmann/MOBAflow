// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using System.Diagnostics;

/// <summary>
/// Helper class for managing Windows Firewall rules for WebApp UDP Discovery and HTTP REST-API.
/// Automatically creates firewall exceptions when WebApp starts.
/// </summary>
public static class FirewallHelper
{
    private const string RULE_NAME_UDP = "MOBAflow WebApp UDP Discovery";
    private const string RULE_NAME_HTTP_PREFIX = "MOBAflow WebApp REST-API";
    private const int UDP_PORT = 21106;

    /// <summary>
    /// Ensures Windows Firewall rules exist for WebApp UDP Discovery and REST-API.
    /// Creates rules if they don't exist, using netsh advfirewall command.
    /// </summary>
    /// <returns>True if rules were created/verified successfully, false otherwise</returns>
    public static bool EnsureFirewallRulesExist(int httpPort)
    {
        try
        {
            Debug.WriteLine("🔥 Checking Windows Firewall rules for WebApp...");

            var httpRuleName = $"{RULE_NAME_HTTP_PREFIX} (Port {httpPort})";

            // Check and create UDP Discovery rule (Port 21106 Inbound)
            // Delete and recreate to ensure correct profile=any is applied
            if (FirewallRuleExists(RULE_NAME_UDP))
            {
                Debug.WriteLine($"   Updating UDP Discovery firewall rule (Port {UDP_PORT})...");
                DeleteFirewallRule(RULE_NAME_UDP);
            }
            else
            {
                Debug.WriteLine($"   Creating UDP Discovery firewall rule (Port {UDP_PORT})...");
            }
            CreateUdpFirewallRule();

            // Check and create HTTP REST-API rule (Port httpPort Inbound)
            // Delete and recreate to ensure correct profile=any is applied
            if (FirewallRuleExists(httpRuleName))
            {
                Debug.WriteLine($"   Updating HTTP REST-API firewall rule (Port {httpPort})...");
                DeleteFirewallRule(httpRuleName);
            }
            else
            {
                Debug.WriteLine($"   Creating HTTP REST-API firewall rule (Port {httpPort})...");
            }
            CreateHttpFirewallRule(httpRuleName, httpPort);

            Debug.WriteLine("✅ Windows Firewall rules verified");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Failed to create firewall rules: {ex.Message}");
            Debug.WriteLine("   This is normal if running without admin rights.");
            Debug.WriteLine("   Manually create firewall rules or run WinUI as Administrator once.");
            return false;
        }
    }

    /// <summary>
    /// Deletes a firewall rule by name.
    /// </summary>
    private static void DeleteFirewallRule(string ruleName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
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
                     $"name=\"{RULE_NAME_UDP}\" " +
                     $"dir=in " +
                     $"action=allow " +
                     $"protocol=UDP " +
                     $"localport={UDP_PORT} " +
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
    /// </summary>
    private static void ExecuteNetshCommand(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            Verb = "runas", // Request admin elevation
            UseShellExecute = true, // Required for runas
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();

        if (process?.ExitCode == 0)
        {
            Debug.WriteLine("   ✅ Firewall rule created successfully");
        }
        else
        {
            Debug.WriteLine($"   ⚠️ Firewall rule creation failed (Exit Code: {process?.ExitCode})");
            Debug.WriteLine("      Run WinUI as Administrator to create firewall rules automatically");
        }
    }

    /// <summary>
    /// Removes the WebApp firewall rules (cleanup on uninstall).
    /// </summary>
    public static void RemoveFirewallRules()
    {
        try
        {
            Debug.WriteLine("🔥 Removing WebApp firewall rules...");

            ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{RULE_NAME_UDP}\"");
            ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{RULE_NAME_HTTP_PREFIX} (Port 5000)\"");

            Debug.WriteLine("✅ Firewall rules removed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Failed to remove firewall rules: {ex.Message}");
        }
    }
}
