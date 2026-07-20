// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

/// <summary>
/// Loads or creates the persistent local TLS identity used for certificate pinning.
/// </summary>
public interface IServerIdentityProvider
{
    Task<ServerIdentity> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Exposes the server certificate and its SHA-256 public-key fingerprint.
/// </summary>
public sealed record ServerIdentity(X509Certificate2 Certificate, string PublicKeyFingerprint);

internal sealed class ServerIdentityProvider : IServerIdentityProvider, IDisposable
{
    private const string IdentityPurpose = "MOBApi.ControlPlane.ServerIdentity.v1";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ProtectedDocumentStore<ServerIdentityDocument> _store;
    private readonly TimeProvider _timeProvider;
    private ServerIdentity? _cachedIdentity;

    public ServerIdentityProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider)
    {
        var path = Path.Combine(options.Value.ResolveStorageDirectory(), "server-identity.dat");
        _store = new ProtectedDocumentStore<ServerIdentityDocument>(dataProtectionProvider, IdentityPurpose, path);
        _timeProvider = timeProvider;
    }

    public async Task<ServerIdentity> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedIdentity is not null)
            return _cachedIdentity;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cachedIdentity ??= await LoadOrCreateAsync(cancellationToken).ConfigureAwait(false);
            return _cachedIdentity;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _cachedIdentity?.Certificate.Dispose();
        _gate.Dispose();
    }

    private async Task<ServerIdentity> LoadOrCreateAsync(CancellationToken cancellationToken)
    {
        var document = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(document.Pkcs12))
            return FromPkcs12(Convert.FromBase64String(document.Pkcs12));

        using var certificate = CreateCertificate();
        var pkcs12 = certificate.Export(X509ContentType.Pkcs12);
        document.Pkcs12 = Convert.ToBase64String(pkcs12);
        await _store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return FromPkcs12(pkcs12);
    }

    private X509Certificate2 CreateCertificate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=MOBAflow Local Control Plane",
            key,
            HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        var now = _timeProvider.GetUtcNow();
        return request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(10));
    }

    private static ServerIdentity FromPkcs12(byte[] pkcs12)
    {
        var certificate = X509CertificateLoader.LoadPkcs12(
            pkcs12,
            null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        using var publicKey = certificate.GetECDsaPublicKey() ??
                              throw new InvalidDataException("Server identity does not contain an ECDSA public key.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
        return new ServerIdentity(certificate, fingerprint);
    }

    private sealed class ServerIdentityDocument
    {
        public string Pkcs12 { get; set; } = string.Empty;
    }
}