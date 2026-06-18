// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Runtime hub method names shared between MOBApi, MOBAflow host, and MOBAsmart remote clients.
/// </summary>
public static class RuntimeHubMethods
{
    public const string RegisterHost = "RegisterHost";
    public const string RegisterRemote = "RegisterRemote";
    public const string PushSnapshot = "PushSnapshot";
    public const string SetSignalAspect = "SetSignalAspect";
    public const string SetLocomotiveDrive = "SetLocomotiveDrive";
    public const string SetLocomotiveFunction = "SetLocomotiveFunction";
    public const string SnapshotUpdated = "SnapshotUpdated";
    public const string SessionStateChanged = "SessionStateChanged";
    public const string ExecuteSetSignalAspect = "ExecuteSetSignalAspect";
    public const string ExecuteSetLocomotiveDrive = "ExecuteSetLocomotiveDrive";
    public const string ExecuteSetLocomotiveFunction = "ExecuteSetLocomotiveFunction";
}
