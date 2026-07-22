// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Moba.Common.Security;

/// <summary>
/// Verifies the SHA-256 fingerprint of a certificate's subject public key info.
/// </summary>
public static class ServerCertificatePinning
{
    public static bool Matches(X509Certificate? certificate, string? expectedFingerprint)
    {
        if (certificate is null ||
            expectedFingerprint?.Length != 64 ||
            !expectedFingerprint.All(Uri.IsHexDigit))
        {
            return false;
        }

        var actual = certificate is X509Certificate2 certificate2
            ? GetFingerprintBytes(certificate2)
            : GetFingerprintBytesFromCopy(certificate);
        var expected = Convert.FromHexString(expectedFingerprint);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static string GetFingerprint(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return Convert.ToHexString(GetFingerprintBytes(certificate));
    }

    private static byte[] GetFingerprintBytes(X509Certificate2 certificate) =>
        SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo());

    private static byte[] GetFingerprintBytesFromCopy(X509Certificate certificate)
    {
        using var copy = new X509Certificate2(certificate);
        return GetFingerprintBytes(copy);
    }
}