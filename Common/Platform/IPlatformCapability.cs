// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Platform;

/// <summary>
/// Reports platform-specific feature availability without referencing OS APIs in consumers.
/// </summary>
public interface IPlatformCapability
{
    bool SupportsWindowsSystemSpeech { get; }

    bool SupportsWindowsSoundPlayback { get; }
}

/// <summary>
/// Runtime platform capability probe used by cross-platform assemblies.
/// </summary>
public sealed class RuntimePlatformCapability : IPlatformCapability
{
    public bool SupportsWindowsSystemSpeech =>
        OperatingSystem.IsWindows();

    public bool SupportsWindowsSoundPlayback =>
        OperatingSystem.IsWindows();
}
