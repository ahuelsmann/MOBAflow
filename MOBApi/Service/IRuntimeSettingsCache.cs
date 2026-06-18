// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

/// <summary>
/// In-memory cache for runtime settings pushed by MOBAflow WinUI (e.g. Z21 endpoint for MOBAsmart).
/// </summary>
public interface IRuntimeSettingsCache
{
    /// <summary>
    /// Gets the cached Z21 endpoint when MOBAflow has pushed settings.
    /// </summary>
    bool TryGetZ21Endpoint(out string? ipAddress, out int port);

    /// <summary>
    /// Stores the Z21 endpoint from MOBAflow settings.
    /// </summary>
    void SetZ21Endpoint(string ipAddress, int port);
}
