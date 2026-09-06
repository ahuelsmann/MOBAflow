// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Identifies why an explicitly configured display endpoint is unavailable.
/// </summary>
public enum DisplayEndpointValidationError
{
    /// <summary>The endpoint is valid.</summary>
    None,
    /// <summary>No address was configured.</summary>
    MissingAddress,
    /// <summary>The address is not a valid IPv4 or IPv6 literal.</summary>
    InvalidAddress,
    /// <summary>The address is an unspecified local address.</summary>
    UnspecifiedAddress,
    /// <summary>The UDP port is outside the supported range.</summary>
    InvalidPort
}

/// <summary>
/// Represents one validated IP endpoint for display protocol v1.0.
/// </summary>
/// <param name="Address">Concrete IPv4 or IPv6 device address.</param>
/// <param name="Port">UDP destination port.</param>
public sealed record DisplayEndpoint(IPAddress Address, int Port)
{
    /// <summary>
    /// Validates user-provided endpoint fields without opening a network connection.
    /// </summary>
    /// <param name="addressText">IPv4 or IPv6 address text.</param>
    /// <param name="port">UDP destination port.</param>
    /// <param name="endpoint">Validated endpoint when the method succeeds.</param>
    /// <param name="error">Validation result.</param>
    /// <returns><see langword="true"/> when both endpoint fields are valid.</returns>
    public static bool TryCreate(
        string? addressText,
        int port,
        out DisplayEndpoint? endpoint,
        out DisplayEndpointValidationError error)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(addressText))
        {
            error = DisplayEndpointValidationError.MissingAddress;
            return false;
        }

        if (!IPAddress.TryParse(addressText.Trim(), out var address))
        {
            error = DisplayEndpointValidationError.InvalidAddress;
            return false;
        }

        if (IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address))
        {
            error = DisplayEndpointValidationError.UnspecifiedAddress;
            return false;
        }

        if (port is < 1 or > IPEndPoint.MaxPort)
        {
            error = DisplayEndpointValidationError.InvalidPort;
            return false;
        }

        endpoint = new DisplayEndpoint(address, port);
        error = DisplayEndpointValidationError.None;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() =>
        Address.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{Address}]:{Port}"
            : $"{Address}:{Port}";
}
