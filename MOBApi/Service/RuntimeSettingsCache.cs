// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

/// <summary>
/// Thread-safe in-memory store for MOBAflow runtime settings exposed to MOBAsmart.
/// </summary>
public sealed class RuntimeSettingsCache : IRuntimeSettingsCache
{
    private readonly Lock _lock = new();
    private string? _z21IpAddress;
    private int _z21Port;

    /// <inheritdoc />
    public bool TryGetZ21Endpoint(out string? ipAddress, out int port)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(_z21IpAddress) || _z21Port <= 0)
            {
                ipAddress = null;
                port = 0;
                return false;
            }

            ipAddress = _z21IpAddress;
            port = _z21Port;
            return true;
        }
    }

    /// <inheritdoc />
    public void SetZ21Endpoint(string ipAddress, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        if (port <= 0 || port >= 65536)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        lock (_lock)
        {
            _z21IpAddress = ipAddress.Trim();
            _z21Port = port;
        }
    }
}
