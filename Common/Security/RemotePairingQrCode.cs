// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Security;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

/// <summary>
/// Contains the short-lived information transferred from MOBAflow to MOBAsmart by QR code.
/// </summary>
public sealed record RemotePairingQrInvitation(
    string IpAddress,
    int HttpPort,
    int HttpsPort,
    string ServerInstanceId,
    string ServerPublicKeyFingerprint,
    string PairingSecret,
    DateTimeOffset ExpiresAt)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"RemotePairingQrInvitation {{ IpAddress = {IpAddress}, HttpPort = {HttpPort}, HttpsPort = {HttpsPort}, " +
        $"ServerInstanceId = {ServerInstanceId}, ServerPublicKeyFingerprint = [REDACTED], " +
        $"PairingSecret = [REDACTED], ExpiresAt = {ExpiresAt:O} }}";
}

/// <summary>
/// Identifies why a scanned MOBAflow pairing QR code could not be accepted.
/// </summary>
public enum RemotePairingQrFailure
{
    None,
    Invalid,
    Expired
}

/// <summary>
/// Represents the validated result of decoding a MOBAflow pairing QR code.
/// </summary>
public sealed record RemotePairingQrDecodeResult(
    RemotePairingQrInvitation? Invitation,
    RemotePairingQrFailure Failure)
{
    public bool IsSuccess => Invitation is not null && Failure == RemotePairingQrFailure.None;
}

/// <summary>
/// Encodes and validates the versioned, secret-bearing MOBAflow pairing QR format.
/// </summary>
public static class RemotePairingQrCode
{
    private const string Prefix = "MOBAFLOW_PAIRING|1|";
    private const int PairingSecretLength = 43;
    private const int Sha256FingerprintLength = 64;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Encodes a validated invitation into the string written to the QR code.
    /// The returned value contains a credential and must not be logged or persisted.
    /// </summary>
    public static string Encode(RemotePairingQrInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        Validate(invitation);

        var json = JsonSerializer.SerializeToUtf8Bytes(invitation, JsonOptions);
        return Prefix + Convert.ToBase64String(json)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Decodes and validates a scanned invitation without exposing validation details or secrets.
    /// </summary>
    public static RemotePairingQrDecodeResult Decode(string? value, TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return new RemotePairingQrDecodeResult(null, RemotePairingQrFailure.Invalid);
        }

        try
        {
            var encoded = value[Prefix.Length..];
            var padding = encoded.Length % 4;
            if (padding != 0)
            {
                encoded = encoded.PadRight(encoded.Length + 4 - padding, '=');
            }

            var bytes = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/'));
            var invitation = JsonSerializer.Deserialize<RemotePairingQrInvitation>(bytes, JsonOptions);
            if (invitation is null)
            {
                return new RemotePairingQrDecodeResult(null, RemotePairingQrFailure.Invalid);
            }

            Validate(invitation);
            var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            return invitation.ExpiresAt <= now
                ? new RemotePairingQrDecodeResult(null, RemotePairingQrFailure.Expired)
                : new RemotePairingQrDecodeResult(invitation, RemotePairingQrFailure.None);
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException or JsonException or NotSupportedException)
        {
            return new RemotePairingQrDecodeResult(null, RemotePairingQrFailure.Invalid);
        }
    }

    private static void Validate(RemotePairingQrInvitation invitation)
    {
        if (!IPAddress.TryParse(invitation.IpAddress, out var ipAddress) ||
            ipAddress.AddressFamily != AddressFamily.InterNetwork ||
            !IsPrivate(ipAddress))
        {
            throw new ArgumentException("The pairing endpoint must be a private IPv4 address.", nameof(invitation));
        }

        if (invitation.HttpPort is <= 0 or >= 65536 || invitation.HttpsPort is <= 0 or >= 65536)
        {
            throw new ArgumentOutOfRangeException(nameof(invitation), "The pairing HTTPS port is invalid.");
        }

        if (!Guid.TryParseExact(invitation.ServerInstanceId, "N", out _))
        {
            throw new ArgumentException("The server instance ID is invalid.", nameof(invitation));
        }

        if (string.IsNullOrWhiteSpace(invitation.ServerPublicKeyFingerprint) ||
            invitation.ServerPublicKeyFingerprint.Length != Sha256FingerprintLength ||
            !invitation.ServerPublicKeyFingerprint.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("The server fingerprint is invalid.", nameof(invitation));
        }

        if (Encoding.UTF8.GetByteCount(invitation.PairingSecret) != PairingSecretLength)
        {
            throw new ArgumentException("The pairing secret is invalid.", nameof(invitation));
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               bytes[0] == 192 && bytes[1] == 168 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31;
    }
}