// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

/// <summary>
/// Defines stable authorization capabilities shared by REST and SignalR policies.
/// </summary>
public static class ControlPlaneCapabilities
{
    public const string Read = "controlplane.read";
    public const string ClientPresence = "client.presence";
    public const string RuntimeControl = "runtime.control";
    public const string PhotoWrite = "photo.write";
    public const string HostPublish = "host.publish";
    public const string HostConsume = "host.consume";
    public const string SecurityManage = "security.manage";

    public const string ClaimType = "mobaflow:capability";
    public const string AccessTokenExpiresAtClaimType = "mobaflow:access_token_expires_at";

    public static IReadOnlySet<string> ForRole(ControlPlaneRole role) => role switch
    {
        ControlPlaneRole.Host => new HashSet<string>(StringComparer.Ordinal)
        {
            Read,
            ClientPresence,
            HostPublish,
            HostConsume,
            SecurityManage
        },
        ControlPlaneRole.RemoteControl => new HashSet<string>(StringComparer.Ordinal)
        {
            Read,
            ClientPresence,
            RuntimeControl,
            PhotoWrite
        },
        ControlPlaneRole.ReadOnly => new HashSet<string>(StringComparer.Ordinal)
        {
            Read,
            ClientPresence
        },
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown control-plane role.")
    };

    public static IReadOnlyList<string> All { get; } =
    [
        Read,
        ClientPresence,
        RuntimeControl,
        PhotoWrite,
        HostPublish,
        HostConsume,
        SecurityManage
    ];
}

/// <summary>
/// Defines the fixed least-privilege role templates used by issued credentials.
/// </summary>
public enum ControlPlaneRole
{
    Host,
    RemoteControl,
    ReadOnly
}
