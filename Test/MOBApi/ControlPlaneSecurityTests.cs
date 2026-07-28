// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moba.MOBApi.Security;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class ControlPlaneSecurityTests
{
    [Test]
    public void ForRole_Should_ApplyLeastPrivilegeCapabilityTemplates()
    {
        var host = ControlPlaneCapabilities.ForRole(ControlPlaneRole.Host);
        var remote = ControlPlaneCapabilities.ForRole(ControlPlaneRole.RemoteControl);
        var readOnly = ControlPlaneCapabilities.ForRole(ControlPlaneRole.ReadOnly);

        Assert.Multiple(() =>
        {
            Assert.That(host, Does.Contain(ControlPlaneCapabilities.SecurityManage));
            Assert.That(host, Does.Not.Contain(ControlPlaneCapabilities.RuntimeControl));
            Assert.That(remote, Does.Contain(ControlPlaneCapabilities.RuntimeControl));
            Assert.That(remote, Does.Not.Contain(ControlPlaneCapabilities.SecurityManage));
            Assert.That(readOnly, Is.EquivalentTo(new[]
            {
                ControlPlaneCapabilities.Read,
                ControlPlaneCapabilities.ClientPresence
            }));
        });
    }

    [Test]
    public async Task RotateAsync_Should_RevokeCredentialFamily_WhenConsumedTokenIsReplayed()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);

        var rotation = await registry.RotateAsync(issued.Credential.CredentialId, issued.RefreshToken);
        var replay = await registry.RotateAsync(issued.Credential.CredentialId, issued.RefreshToken);
        var state = await registry.GetAuthorizationStateAsync(issued.Credential.CredentialId);

        Assert.Multiple(() =>
        {
            Assert.That(rotation.Status, Is.EqualTo(RefreshRotationStatus.Succeeded));
            Assert.That(rotation.RefreshToken, Is.Not.EqualTo(issued.RefreshToken));
            Assert.That(replay.Status, Is.EqualTo(RefreshRotationStatus.ReplayDetected));
            Assert.That(state, Is.Null);
        });
    }

    [Test]
    public async Task RotateAsync_Should_RejectCredential_AfterInactivityExpiry()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);

        context.TimeProvider.Advance(TimeSpan.FromDays(31));
        var rotation = await registry.RotateAsync(issued.Credential.CredentialId, issued.RefreshToken);

        Assert.That(rotation.Status, Is.EqualTo(RefreshRotationStatus.Expired));
    }

    [Test]
    public async Task CreateAsync_Should_NotPersistRefreshCredentialInClearText()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var protectedDocument = await File.ReadAllBytesAsync(Path.Combine(context.Root, "store", "credentials.dat"));

        Assert.That(ContainsSequence(protectedDocument, System.Text.Encoding.UTF8.GetBytes(issued.RefreshToken)), Is.False);
    }

    [Test]
    public async Task ValidateAsync_Should_RejectExistingToken_WhenRoleChanges()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);

        var beforeChange = await tokenService.ValidateAsync(token!.Token);
        await registry.ChangeRoleAsync(issued.Credential.CredentialId, ControlPlaneRole.RemoteControl);
        var afterChange = await tokenService.ValidateAsync(token.Token);

        Assert.Multiple(() =>
        {
            Assert.That(beforeChange, Is.Not.Null);
            Assert.That(afterChange, Is.Null);
        });
    }

    [Test]
    public async Task ValidateAsync_Should_RejectToken_WhenCredentialIsRevoked()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);

        await registry.RevokeAsync(issued.Credential.CredentialId, "operator_revoked");
        var principal = await tokenService.ValidateAsync(token!.Token);

        Assert.That(principal, Is.Null);
    }

    [Test]
    public async Task ValidateAsync_Should_RejectExpiredAccessToken()
    {
        using var context = SecurityTestContext.Create(accessTokenLifetime: TimeSpan.FromMinutes(5));
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);

        context.TimeProvider.Advance(TimeSpan.FromMinutes(6));
        var principal = await tokenService.ValidateAsync(token!.Token);

        Assert.That(principal, Is.Null);
    }

    [Test]
    public async Task Pairing_Should_RequireApproval_AndAllowSingleClaim()
    {
        using var context = SecurityTestContext.Create();
        var pairing = context.GetRequiredService<IPairingService>();
        var window = await pairing.OpenAsync(ControlPlaneRole.RemoteControl);
        var submission = await pairing.SubmitAsync(new PairingSubmission(
            window.PairingSecret,
            "client-nonce",
            "Remote cab",
            ControlPlaneRole.RemoteControl));

        var pendingClaim = await pairing.ClaimAsync(submission.RequestId!, submission.ClaimToken!);
        var approved = await pairing.ApproveAsync(submission.RequestId!);
        var successfulClaim = await pairing.ClaimAsync(submission.RequestId!, submission.ClaimToken!);
        var replayedClaim = await pairing.ClaimAsync(submission.RequestId!, submission.ClaimToken!);

        Assert.Multiple(() =>
        {
            Assert.That(submission.Status, Is.EqualTo(PairingSubmissionStatus.Accepted));
            Assert.That(pendingClaim.Status, Is.EqualTo(PairingClaimStatus.PendingApproval));
            Assert.That(approved, Is.True);
            Assert.That(successfulClaim.Status, Is.EqualTo(PairingClaimStatus.Succeeded));
            Assert.That(successfulClaim.Credential, Is.Not.Null);
            Assert.That(replayedClaim.Status, Is.EqualTo(PairingClaimStatus.AlreadyClaimed));
        });
    }

    [Test]
    public async Task SubmitAsync_Should_EnterCooldown_AfterMaximumFailedAttempts()
    {
        using var context = SecurityTestContext.Create(pairingMaximumFailedAttempts: 2);
        var pairing = context.GetRequiredService<IPairingService>();
        await pairing.OpenAsync(ControlPlaneRole.ReadOnly);
        var invalid = new PairingSubmission(new string('A', 43), "nonce", "Observer", ControlPlaneRole.ReadOnly);

        var first = await pairing.SubmitAsync(invalid);
        var second = await pairing.SubmitAsync(invalid);
        var third = await pairing.SubmitAsync(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(PairingSubmissionStatus.Invalid));
            Assert.That(second.Status, Is.EqualTo(PairingSubmissionStatus.Cooldown));
            Assert.That(third.Status, Is.EqualTo(PairingSubmissionStatus.Cooldown));
        });
    }

    [Test]
    public async Task ServerIdentity_Should_PreserveFingerprintAcrossServiceProviders()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            string firstFingerprint;
            string firstInstanceId;
            using (var first = SecurityTestContext.Create(root: root))
            {
                var firstIdentity = await first.GetRequiredService<IServerIdentityProvider>().GetAsync();
                firstFingerprint = firstIdentity.PublicKeyFingerprint;
                firstInstanceId = firstIdentity.InstanceId;
            }

            using var second = SecurityTestContext.Create(root: root);
            var secondIdentity = await second.GetRequiredService<IServerIdentityProvider>().GetAsync();

            Assert.Multiple(() =>
            {
                Assert.That(secondIdentity.PublicKeyFingerprint, Is.EqualTo(firstFingerprint));
                Assert.That(secondIdentity.InstanceId, Is.EqualTo(firstInstanceId));
                Assert.That(secondIdentity.InstanceId, Does.Match("^[a-f0-9]{32}$"));
                Assert.That(secondIdentity.Certificate.HasPrivateKey, Is.True);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ServerIdentity_Should_SupportTlsServerAuthentication()
    {
        using var context = SecurityTestContext.Create();
        var identity = await context.GetRequiredService<IServerIdentityProvider>().GetAsync().ConfigureAwait(false);
        var expectedCertificateHash = identity.Certificate.GetCertHash(HashAlgorithmName.SHA256);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var acceptTask = listener.AcceptTcpClientAsync(cancellation.Token);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, cancellation.Token).ConfigureAwait(false);
            using var server = await acceptTask.ConfigureAwait(false);
            using var clientTls = new SslStream(
                client.GetStream(),
                leaveInnerStreamOpen: false,
                (_, certificate, _, _) =>
                    certificate is not null &&
                    CryptographicOperations.FixedTimeEquals(
                        certificate.GetCertHash(HashAlgorithmName.SHA256),
                        expectedCertificateHash));
            using var serverTls = new SslStream(server.GetStream(), leaveInnerStreamOpen: false);

            var serverAuthenticationTask = serverTls.AuthenticateAsServerAsync(
                identity.Certificate,
                clientCertificateRequired: false,
                checkCertificateRevocation: false);
            var clientAuthenticationTask = clientTls.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions { TargetHost = "localhost" },
                cancellation.Token);
            await Task.WhenAll(serverAuthenticationTask, clientAuthenticationTask).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(clientTls.IsAuthenticated, Is.True);
                Assert.That(serverTls.IsAuthenticated, Is.True);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task AddControlPlaneSecurity_Should_RegisterNamedCapabilityPolicies()
    {
        using var context = SecurityTestContext.Create();
        var policyProvider = context.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var capability in ControlPlaneCapabilities.All)
        {
            var policy = await policyProvider.GetPolicyAsync(capability);
            Assert.That(policy, Is.Not.Null, $"Missing policy for {capability}");
        }
    }

    [Test]
    public async Task AuthenticationHandler_Should_AcceptQueryTokenOnlyForSignalRHubs()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);

        var controllerResult = await AuthenticateQueryTokenAsync(context.Services, "/api/solution", token!.Token);
        var hubResult = await AuthenticateQueryTokenAsync(context.Services, "/runtime-hub", token.Token);

        Assert.Multiple(() =>
        {
            Assert.That(controllerResult.Succeeded, Is.False);
            Assert.That(controllerResult.None, Is.True);
            Assert.That(hubResult.Succeeded, Is.True);
        });
    }

    [Test]
    public async Task AuthorizationPolicies_Should_EnforceIssuedCapabilities()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var authorization = context.GetRequiredService<IAuthorizationService>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);
        var principal = await tokenService.ValidateAsync(token!.Token);

        var control = await authorization.AuthorizeAsync(principal!, null, ControlPlaneCapabilities.RuntimeControl);
        var security = await authorization.AuthorizeAsync(principal!, null, ControlPlaneCapabilities.SecurityManage);

        Assert.Multiple(() =>
        {
            Assert.That(control.Succeeded, Is.True);
            Assert.That(security.Succeeded, Is.False);
        });
    }

    private static async Task<AuthenticateResult> AuthenticateQueryTokenAsync(
        IServiceProvider services,
        string path,
        string token)
    {
        using var scope = services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = new QueryString($"?access_token={Uri.EscapeDataString(token)}");
        return await httpContext.AuthenticateAsync(ControlPlaneAuthenticationDefaults.Scheme);
    }

    private static bool ContainsSequence(byte[] source, byte[] candidate)
    {
        for (var offset = 0; offset <= source.Length - candidate.Length; offset++)
        {
            if (source.AsSpan(offset, candidate.Length).SequenceEqual(candidate))
                return true;
        }

        return false;
    }

    private sealed class SecurityTestContext : IDisposable
    {
        private readonly bool _ownsRoot;
        private readonly ServiceProvider _serviceProvider;

        private SecurityTestContext(string root, bool ownsRoot, TimeSpan accessTokenLifetime, int pairingMaximumFailedAttempts)
        {
            Root = root;
            _ownsRoot = ownsRoot;
            TimeProvider = new AdjustableTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero));
            _serviceProvider = CreateServiceProvider(root, accessTokenLifetime, pairingMaximumFailedAttempts, TimeProvider);
        }

        public string Root { get; }

        public AdjustableTimeProvider TimeProvider { get; }

        public IServiceProvider Services => _serviceProvider;

        public static SecurityTestContext Create(
            string? root = null,
            TimeSpan? accessTokenLifetime = null,
            int pairingMaximumFailedAttempts = 5)
        {
            var ownsRoot = root is null;
            return new SecurityTestContext(
                root ?? CreateTemporaryRoot(),
                ownsRoot,
                accessTokenLifetime ?? TimeSpan.FromMinutes(5),
                pairingMaximumFailedAttempts);
        }

        public static string CreateTemporaryRoot() =>
            Path.Combine(Path.GetTempPath(), "mobaflow-control-plane-tests", Guid.NewGuid().ToString("N"));

        public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
            if (_ownsRoot && Directory.Exists(Root))
                Directory.Delete(Root, true);
        }

        private static ServiceProvider CreateServiceProvider(
            string root,
            TimeSpan accessTokenLifetime,
            int pairingMaximumFailedAttempts,
            TimeProvider timeProvider)
        {
            Directory.CreateDirectory(root);
            var settings = new Dictionary<string, string?>
            {
                [$"{ControlPlaneSecurityOptions.SectionName}:StorageDirectory"] = Path.Combine(root, "store"),
                [$"{ControlPlaneSecurityOptions.SectionName}:AccessTokenLifetime"] = accessTokenLifetime.ToString("c"),
                [$"{ControlPlaneSecurityOptions.SectionName}:PairingMaximumFailedAttempts"] = pairingMaximumFailedAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControlPlaneSecurity(configuration);
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "keys")));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(timeProvider);
            return services.BuildServiceProvider(validateScopes: true);
        }
    }

    public sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public AdjustableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}