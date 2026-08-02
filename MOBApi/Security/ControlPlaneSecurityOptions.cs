// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

/// <summary>
/// Configures the local control-plane security foundation.
/// </summary>
public sealed class ControlPlaneSecurityOptions
{
    public const string SectionName = "ControlPlaneSecurity";

    public string? StorageDirectory { get; set; }

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan RefreshInactivityLifetime { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan RefreshAbsoluteLifetime { get; set; } = TimeSpan.FromDays(365);

    public TimeSpan PairingWindowLifetime { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan PairingCooldown { get; set; } = TimeSpan.FromMinutes(10);

    public int PairingMaximumFailedAttempts { get; set; } = 5;

    public TimeSpan HostBootstrapLifetime { get; set; } = TimeSpan.FromSeconds(30);

    public int HostBootstrapMaximumFailedAttempts { get; set; } = 5;

    public TimeSpan HostDisconnectGrace { get; set; } = TimeSpan.FromSeconds(30);

    internal string ResolveStorageDirectory()
    {
        if (!string.IsNullOrWhiteSpace(StorageDirectory))
            return Path.GetFullPath(StorageDirectory);

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "MOBAflow", "security");
    }
}
