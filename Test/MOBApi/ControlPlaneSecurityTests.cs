// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Models;
using Moba.MOBApi.Security;
using Moba.MOBApi.Service;
using Moq;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed partial class ControlPlaneSecurityTests
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
    public async Task ValidateAsync_Should_ExposeCredentialAndExpiryForConnectionRevocation()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);

        var principal = await tokenService.ValidateAsync(token!.Token);

        Assert.Multiple(() =>
        {
            Assert.That(
                principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Is.EqualTo(issued.Credential.CredentialId));
            Assert.That(
                principal?.FindFirst(ControlPlaneCapabilities.AccessTokenExpiresAtClaimType)?.Value,
                Is.EqualTo(token.ExpiresAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
        });
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
                new SslServerAuthenticationOptions
                {
                    ServerCertificate = identity.Certificate,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                },
                cancellation.Token);
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

    [Test]
    public async Task ReadPolicy_Should_AllowAnonymousCompatibility_WhenNoCredentialIsPresented()
    {
        using var context = SecurityTestContext.Create();
        var authorization = context.GetRequiredService<IAuthorizationService>();
        var httpContext = new DefaultHttpContext();

        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            httpContext,
            ControlPlaneCapabilities.Read);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task ReadMigration_Should_NotBecomeReady_WhenAuthenticatedTrafficIsAbsent()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));

        var status = await migration.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsReadyForEnforcement, Is.False);
            Assert.That(status.AuthenticatedReadCount, Is.Zero);
            Assert.That(status.BlockingReason, Is.EqualTo(CompatibilityReadBlockingReason.NoAuthenticatedTraffic));
        });
    }

    [Test]
    public void ReadMigration_Should_RejectEvidenceOutsideIssue50()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();

        Assert.ThrowsAsync<ArgumentException>(() =>
            migration.RecordIssueEvidenceAsync("https://example.com/not-issue-50"));
        Assert.ThrowsAsync<ArgumentException>(() =>
            migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#readiness"));
    }

    [Test]
    public async Task ReadMigration_Should_BecomeReady_AfterObservedStableReleaseAndIssueEvidence()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");

        var status = await migration.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsReadyForEnforcement, Is.True);
            Assert.That(status.AuthenticatedReadCount, Is.EqualTo(2));
            Assert.That(status.BlockingReason, Is.EqualTo(CompatibilityReadBlockingReason.None));
        });
    }

    [Test]
    public async Task ReadMigration_Should_RequireMatchingStableReleaseOnRestAndSignalR()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 0.9.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 0.9.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));

        Assert.That((await migration.GetStatusAsync()).BlockingReason,
            Is.EqualTo(CompatibilityReadBlockingReason.NoAuthenticatedTraffic));

        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        Assert.That((await migration.GetStatusAsync()).IsReadyForEnforcement, Is.False);

        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That((await migration.GetStatusAsync()).IsReadyForEnforcement, Is.True);
    }

    [Test]
    public async Task ReadMigration_Should_RejectEvidenceThatVerifierCannotResolve()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));

        Assert.ThrowsAsync<InvalidOperationException>(() => migration.RecordIssueEvidenceAsync(
            "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-9999999999"));
        Assert.That((await migration.GetStatusAsync()).EvidenceRecorded, Is.False);
    }

    [Test]
    public void GitHubEvidenceVerifier_Should_RejectMissingComment()
    {
        using var services = CreateEvidenceVerifierServices(new HttpResponseMessage(HttpStatusCode.NotFound));
        var verifier = services.GetRequiredService<ICompatibilityReadEvidenceVerifier>();

        Assert.ThrowsAsync<InvalidOperationException>(() => verifier.VerifyAsync(
            new Uri("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890"),
            "MOBAsmart 1.0.0",
            new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
            CancellationToken.None));
    }

    [Test]
    public async Task GitHubEvidenceVerifier_Should_AcceptCurrentPostWindowEvidence()
    {
        const string body = """
                            {
                              "html_url": "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890",
                              "issue_url": "https://api.github.com/repos/ahuelsmann/MOBAflow/issues/50",
                              "created_at": "2026-08-03T12:01:00Z",
                              "body": "Slice 4e readiness evidence\nStable client release: MOBAsmart 1.0.0\nObservation result: passed"
                            }
                            """;
        using var services = CreateEvidenceVerifierServices(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

        await services.GetRequiredService<ICompatibilityReadEvidenceVerifier>().VerifyAsync(
            new Uri("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890"),
            "MOBAsmart 1.0.0",
            new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
    }

    [Test]
    public void GitHubEvidenceVerifier_Should_RejectCommentCreatedBeforeWindowCompleted()
    {
        const string body = """
                            {
                              "html_url": "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890",
                              "issue_url": "https://api.github.com/repos/ahuelsmann/MOBAflow/issues/50",
                              "created_at": "2026-08-03T11:59:59Z",
                              "body": "Slice 4e readiness evidence\nStable client release: MOBAsmart 1.0.0\nObservation result: passed"
                            }
                            """;
        using var services = CreateEvidenceVerifierServices(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.GetRequiredService<ICompatibilityReadEvidenceVerifier>().VerifyAsync(
                new Uri("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890"),
                "MOBAsmart 1.0.0",
                new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
                CancellationToken.None));
    }

    [Test]
    public void GitHubEvidenceVerifier_Should_RejectReleasePrefixInsteadOfExactLine()
    {
        const string body = """
                            {
                              "html_url": "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890",
                              "issue_url": "https://api.github.com/repos/ahuelsmann/MOBAflow/issues/50",
                              "created_at": "2026-08-03T12:01:00Z",
                              "body": "Slice 4e readiness evidence\nStable client release: MOBAsmart 1.0.0-rc\nObservation result: passed"
                            }
                            """;
        using var services = CreateEvidenceVerifierServices(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.GetRequiredService<ICompatibilityReadEvidenceVerifier>().VerifyAsync(
                new Uri("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890"),
                "MOBAsmart 1.0.0",
                new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
                CancellationToken.None));
    }

    [Test]
    public async Task ReadMigration_Should_RestartObservationWindow_AfterCriticalDefectFix()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That((await migration.GetStatusAsync()).IsReadyForEnforcement, Is.True);

        await migration.RecordCriticalDefectAsync("signalr-reconnect");
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.1");
        var blocked = await migration.GetStatusAsync();
        Assert.Multiple(() =>
        {
            Assert.That(blocked.IsReadyForEnforcement, Is.False);
            Assert.That(blocked.OpenCriticalDefectCount, Is.EqualTo(1));
            Assert.That(blocked.BlockingReason, Is.EqualTo(CompatibilityReadBlockingReason.CriticalDefectOpen));
        });

        await migration.RecordCriticalDefectFixedAsync("signalr-reconnect");
        var status = await migration.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsReadyForEnforcement, Is.False);
            Assert.That(status.AuthenticatedReadCount, Is.Zero);
            Assert.That(status.EvidenceRecorded, Is.False);
            Assert.That(status.BlockingReason, Is.EqualTo(CompatibilityReadBlockingReason.ObservationWindowIncomplete));
            Assert.That(status.ObservationStartedAt, Is.EqualTo(context.TimeProvider.GetUtcNow()));
        });
    }

    [Test]
    public async Task ReadMigration_Should_PreserveObservationState_AcrossServiceRestart()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                var migration = first.GetRequiredService<ICompatibilityReadMigration>();
                await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
                await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
            }

            using var restarted = SecurityTestContext.Create(root);
            var status = await restarted.GetRequiredService<ICompatibilityReadMigration>().GetStatusAsync();

            Assert.Multiple(() =>
            {
                Assert.That(status.StableClientRelease, Is.EqualTo("MOBAsmart 1.0.0"));
                Assert.That(status.AuthenticatedReadCount, Is.EqualTo(1));
                Assert.That(status.BlockingReason, Is.EqualTo(CompatibilityReadBlockingReason.ObservationWindowIncomplete));
            });
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ReadMigration_Should_NotPersistOnEveryReadAuthorization()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        var migrationPath = Path.Combine(context.Root, "store", "read-migration.dat");
        var persistedBeforeRepeatedReads = await File.ReadAllBytesAsync(migrationPath);

        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest);
        var persistedAfterRepeatedReads = await File.ReadAllBytesAsync(migrationPath);

        Assert.That(persistedAfterRepeatedReads, Is.EqualTo(persistedBeforeRepeatedReads));
    }

    [Test]
    public async Task ReadMigration_Should_NotEnableAuthenticatedOnlyMode_BeforeGateIsReady()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();

        var enabled = await migration.EnableAuthenticatedReadsAsync();
        var decision = await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest);

        Assert.Multiple(() =>
        {
            Assert.That(enabled, Is.False);
            Assert.That(decision, Is.EqualTo(CompatibilityReadDecision.AllowedCompatibility));
        });
    }

    [Test]
    public async Task ReadPolicy_Should_RejectAnonymousReads_WhenAuthenticatedOnlyModeIsEnabled()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);

        var result = await context.GetRequiredService<IAuthorizationService>().AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            new DefaultHttpContext(),
            ControlPlaneCapabilities.Read);

        Assert.That(result.Succeeded, Is.False);
    }

    [TestCase("/api/runtime", "rest")]
    [TestCase("/runtime-hub/negotiate", "signalr")]
    public async Task ReadPolicy_Should_ReturnSameMachineReadableUpgradeReason(
        string requestPath,
        string expectedTransport)
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);

        using var scope = context.Services.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            Response = { Body = new MemoryStream() }
        };
        httpContext.Request.Path = requestPath;
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.That(await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            httpContext,
            ControlPlaneCapabilities.Read), Is.Not.Null);
        var policy = await scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync(ControlPlaneCapabilities.Read)
            ?? throw new InvalidOperationException("Missing read policy.");

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().HandleAsync(
            _ => Task.CompletedTask,
            httpContext,
            policy,
            PolicyAuthorizationResult.Challenge());
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Multiple(() =>
        {
            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
            Assert.That(httpContext.Response.ContentType, Does.StartWith("application/problem+json"));
            Assert.That(body, Does.Contain("client_upgrade_required"));
            Assert.That(body, Does.Contain(expectedTransport));
        });
    }

    [Test]
    public async Task ReadMigration_Should_ExpirePersistedRollback_AfterAtMostSevenDays()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                var migration = first.GetRequiredService<ICompatibilityReadMigration>();
                await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
                await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
                await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
                first.TimeProvider.Advance(TimeSpan.FromDays(14));
                await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
                Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);
                Assert.That(await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(8)), Is.False);
                Assert.That(await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(7)), Is.True);
                Assert.That(
                    await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest),
                    Is.EqualTo(CompatibilityReadDecision.AllowedRollback));
            }

            using var restarted = SecurityTestContext.Create(root);
            restarted.TimeProvider.Advance(TimeSpan.FromDays(14));
            var migrationAfterRestart = restarted.GetRequiredService<ICompatibilityReadMigration>();
            Assert.That(
                await migrationAfterRestart.EvaluateAnonymousReadAsync(CompatibilityReadTransport.SignalR),
                Is.EqualTo(CompatibilityReadDecision.AllowedRollback));

            restarted.TimeProvider.Advance(TimeSpan.FromDays(7));
            Assert.That(
                await migrationAfterRestart.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest),
                Is.EqualTo(CompatibilityReadDecision.UpgradeRequired));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ReadMigration_Should_Not_RenewOrClearAnActivatedRollback()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await MakeReadMigrationReadyAsync(context, migration).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await migration.EnableAuthenticatedReadsAsync().ConfigureAwait(false), Is.True);
            Assert.That(
                await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromHours(24)).ConfigureAwait(false),
                Is.True);
            var originalExpiry = (await migration.GetStatusAsync().ConfigureAwait(false)).RollbackExpiresAt;

            context.TimeProvider.Advance(TimeSpan.FromHours(12));
            var secondActivation = await migration
                .ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(7))
                .ConfigureAwait(false);
            var secondEnforcement = await migration.EnableAuthenticatedReadsAsync().ConfigureAwait(false);
            var finalStatus = await migration.GetStatusAsync().ConfigureAwait(false);

            Assert.That(secondActivation, Is.False);
            Assert.That(secondEnforcement, Is.False);
            Assert.That(finalStatus.RollbackExpiresAt, Is.EqualTo(originalExpiry));
        }
    }

    [Test]
    public async Task ReadMigration_Should_RevokeCompatibilityConnections_WhenEnforcedAndRollbackExpires()
    {
        var revoker = new CapturingCompatibilityReadConnectionRevoker();
        using var context = SecurityTestContext.Create(connectionRevoker: revoker);
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await MakeReadMigrationReadyAsync(context, migration).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await migration.EnableAuthenticatedReadsAsync().ConfigureAwait(false), Is.True);
            Assert.That(revoker.RevocationCount, Is.EqualTo(1));
            Assert.That(
                await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(7)).ConfigureAwait(false),
                Is.True);

            context.TimeProvider.Advance(TimeSpan.FromDays(7));

            Assert.That(revoker.RevocationCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void CompatibilityReadConnectionRevoker_Should_AbortEveryTrackedConnectionOnce()
    {
        var revoker = new CompatibilityReadConnectionRevoker();
        var firstAbortCount = 0;
        var secondAbortCount = 0;
        revoker.Register("connection-1", () => firstAbortCount++);
        revoker.Register("connection-2", () => secondAbortCount++);

        revoker.RevokeAll();
        revoker.RevokeAll();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstAbortCount, Is.EqualTo(1));
            Assert.That(secondAbortCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void HubConnectionRegistry_Should_TrackAnonymousRemoteForCompatibilityRevocation()
    {
        var compatibilityRevoker = new CompatibilityReadConnectionRevoker();
        var callerContext = new Mock<HubCallerContext>();
        callerContext.SetupGet(candidate => candidate.ConnectionId).Returns("connection-1");
        var registry = new ControlPlaneHubConnectionRegistry(
            new Mock<IRuntimeRemoteRegistry>().Object,
            new Mock<IControlPlaneConnectionRevoker>().Object,
            compatibilityRevoker);
        registry.RegisterRemote(callerContext.Object, "legacy-client", isAnonymousCompatibility: true);

        compatibilityRevoker.RevokeAll();

        callerContext.Verify(candidate => candidate.Abort(), Times.Once);
    }

    [Test]
    public async Task ReadMigration_Should_FailClosed_WhenMigrationDocumentIsLostAfterEnforcement()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                var migration = first.GetRequiredService<ICompatibilityReadMigration>();
                await MakeReadMigrationReadyAsync(first, migration);
                Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);
            }

            File.Delete(Path.Combine(root, "store", "read-migration.dat"));
            using var restarted = SecurityTestContext.Create(root);

            Assert.That(
                await restarted.GetRequiredService<ICompatibilityReadMigration>()
                    .EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest),
                Is.EqualTo(CompatibilityReadDecision.UpgradeRequired));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ReadMigration_Should_LetEnforcementMarkerOverrideExistingCompatibilityDocument()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                await first.GetRequiredService<ICompatibilityReadMigration>()
                    .BeginReadinessWindowAsync("MOBAsmart 1.0.0");
                var clearMarker = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    AuthenticatedReadsEnforcedAt = first.TimeProvider.GetUtcNow()
                });
                var protectedMarker = first.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("MOBApi.ControlPlane.CompatibilityReadMigration.EnforcementMarker.v1")
                    .Protect(clearMarker);
                await File.WriteAllBytesAsync(
                    Path.Combine(root, "store", "read-migration-enforced.dat"),
                    protectedMarker);
            }

            using var restarted = SecurityTestContext.Create(root);
            Assert.That(
                await restarted.GetRequiredService<ICompatibilityReadMigration>()
                    .EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest),
                Is.EqualTo(CompatibilityReadDecision.UpgradeRequired));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ReadMigration_Should_NotBlockReads_WhileGitHubEvidenceIsVerified()
    {
        var blockingVerifier = new BlockingCompatibilityReadEvidenceVerifier();
        using var context = SecurityTestContext.Create(evidenceVerifier: blockingVerifier);
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0").ConfigureAwait(false);
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));

        var evidenceTask = migration.RecordIssueEvidenceAsync(
            "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        await blockingVerifier.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var decision = await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest)
            .WaitAsync(TimeSpan.FromSeconds(1));
        blockingVerifier.Release.SetResult();
        await evidenceTask;

        Assert.That(decision, Is.EqualTo(CompatibilityReadDecision.AllowedCompatibility));
    }

    [Test]
    public async Task ReadMigration_Should_NotActivateCachedRollback_WhenPersistenceFails()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await MakeReadMigrationReadyAsync(context, migration);
        Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);
        var migrationPath = Path.Combine(context.Root, "store", "read-migration.dat");
        File.Delete(migrationPath);
        Directory.CreateDirectory(migrationPath);

        var persistenceFailure = Assert.CatchAsync(() =>
            migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromHours(24)));
        Assert.That(persistenceFailure, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Assert.That(
            await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.Rest),
            Is.EqualTo(CompatibilityReadDecision.UpgradeRequired));
    }

    [Test]
    public async Task ReadMigration_Should_RecordPseudonymousTransportOutcomes_WithoutCredentialValues()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-sensitive-value", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-sensitive-value", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        Assert.That(
            await migration.EvaluateAnonymousReadAsync(CompatibilityReadTransport.SignalR),
            Is.EqualTo(CompatibilityReadDecision.AllowedCompatibility));

        var telemetry = await migration.GetTelemetryAsync();
        var serialized = System.Text.Json.JsonSerializer.Serialize(telemetry);

        Assert.Multiple(() =>
        {
            Assert.That(telemetry.Outcomes.Single(outcome =>
                outcome.Transport == CompatibilityReadTransport.Rest &&
                outcome.Outcome == CompatibilityReadOutcome.AuthenticatedAllowed).Count, Is.EqualTo(2));
            Assert.That(telemetry.Outcomes.Single(outcome =>
                outcome.Transport == CompatibilityReadTransport.SignalR &&
                outcome.Outcome == CompatibilityReadOutcome.AnonymousCompatibilityAllowed).Count, Is.EqualTo(1));
            Assert.That(serialized, Does.Not.Contain("credential-sensitive-value"));
            Assert.That(telemetry.Outcomes.Single(outcome =>
                outcome.Outcome == CompatibilityReadOutcome.AuthenticatedAllowed).PseudonymousClientId,
                Does.Match("^[0-9A-F]{16}$"));
        });
    }

    [Test]
    public async Task ReadMigration_Should_BoundPseudonymousTelemetryCardinality()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();

        for (var index = 0; index < 70; index++)
        {
            await migration.RecordAuthenticatedReadAsync(
                $"credential-{index}",
                CompatibilityReadTransport.Rest,
                "MOBAsmart 1.0.0");
        }

        var telemetry = await migration.GetTelemetryAsync();

        Assert.Multiple(() =>
        {
            Assert.That(telemetry.Outcomes, Has.Count.EqualTo(65));
            Assert.That(telemetry.Outcomes.Sum(outcome => outcome.Count), Is.EqualTo(70));
            Assert.That(telemetry.Outcomes.Any(outcome => outcome.PseudonymousClientId == "overflow"), Is.True);
        });
    }

    [Test]
    public async Task ReadPolicy_Should_Not_RecordAuthenticatedOutcome_BeforeEndpointSucceeds()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId)
            ?? throw new InvalidOperationException("Expected access token.");
        var principal = await tokenService.ValidateAsync(token.Token)
            ?? throw new InvalidOperationException("Expected access principal.");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {token.Token}";
        httpContext.Request.Headers[CompatibilityReadHeaders.ClientRelease] = "MOBAsmart 1.0.0";
        httpContext.Request.Path = "/api/runtime";
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");

        var result = await context.GetRequiredService<IAuthorizationService>().AuthorizeAsync(
            principal,
            httpContext,
            ControlPlaneCapabilities.Read);
        var telemetry = await migration.GetTelemetryAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(telemetry.Outcomes, Is.Empty);
        });
    }

    [TestCase(StatusCodes.Status204NoContent, 1)]
    [TestCase(StatusCodes.Status500InternalServerError, 0)]
    public async Task CompatibilityReadObservationMiddleware_Should_RecordOnlySuccessfulRestReads(
        int responseStatusCode,
        int expectedOutcomeCount)
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "credential-1")],
                ControlPlaneAuthenticationDefaults.Scheme))
        };
        httpContext.Request.Headers[CompatibilityReadHeaders.ClientRelease] = "MOBAsmart 1.0.0";
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute
            {
                Policy = ControlPlaneCapabilities.Read
            }),
            "authenticated read"));
        var middleware = new CompatibilityReadObservationMiddleware(
            next: request =>
            {
                request.Response.StatusCode = responseStatusCode;
                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(httpContext, migration).ConfigureAwait(false);

        var telemetry = await migration.GetTelemetryAsync().ConfigureAwait(false);
        Assert.That(telemetry.Outcomes.Sum(outcome => outcome.Count), Is.EqualTo(expectedOutcomeCount));
    }

    [Test]
    public async Task CompatibilityReadObservationMiddleware_Should_Not_RecordSignalRHandshakeAsRestRead()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0").ConfigureAwait(false);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "credential-1")],
                ControlPlaneAuthenticationDefaults.Scheme))
        };
        httpContext.Request.Path = "/runtime-hub/negotiate";
        httpContext.Request.Headers[CompatibilityReadHeaders.ClientRelease] = "MOBAsmart 1.0.0";
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute
            {
                Policy = ControlPlaneCapabilities.Read
            }),
            "SignalR negotiate"));
        var middleware = new CompatibilityReadObservationMiddleware(next: request =>
        {
            request.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext, migration).ConfigureAwait(false);

        Assert.That((await migration.GetTelemetryAsync().ConfigureAwait(false)).Outcomes, Is.Empty);
    }

    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public async Task RuntimeHub_Should_RecordSignalRReadOnlyAfterRemoteRegistrationCompletes(
        bool failRegistrationResponse,
        int expectedOutcomeCount)
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0").ConfigureAwait(false);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[CompatibilityReadHeaders.ClientRelease] = "MOBAsmart 1.0.0";
        var httpContextFeature = new Mock<IHttpContextFeature>();
        httpContextFeature.SetupGet(feature => feature.HttpContext).Returns(httpContext);
        var features = new FeatureCollection();
        features.Set(httpContextFeature.Object);
        var callerContext = new Mock<HubCallerContext>();
        callerContext.SetupGet(candidate => candidate.ConnectionId).Returns("connection-1");
        callerContext.SetupGet(candidate => candidate.UserIdentifier).Returns("credential-1");
        callerContext.SetupGet(candidate => candidate.Features).Returns(features);
        var caller = new Mock<ISingleClientProxy>();
        var send = caller.Setup(proxy => proxy.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()));
        if (failRegistrationResponse)
            send.ThrowsAsync(new InvalidOperationException("Caller disconnected."));
        else
            send.Returns(Task.CompletedTask);
        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(candidate => candidate.Caller).Returns(caller.Object);
        var groups = new Mock<IGroupManager>();
        groups.Setup(manager => manager.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var hub = new RuntimeHub(
            new Mock<IRuntimeSnapshotCache>().Object,
            new Mock<ISolutionCache>().Object,
            new Mock<IRuntimeHostRegistry>().Object,
            new Mock<IRuntimeBroadcastMetrics>().Object,
            new Mock<IRuntimeCommandQueue>().Object,
            new Mock<IControlPlaneHubConnectionRegistry>().Object,
            migration)
        {
            Context = callerContext.Object,
            Clients = clients.Object,
            Groups = groups.Object
        };

        using (Assert.EnterMultipleScope())
        {
            if (failRegistrationResponse)
                Assert.ThrowsAsync<InvalidOperationException>(() => hub.RegisterRemote("client-1"));
            else
                await hub.RegisterRemote("client-1").ConfigureAwait(false);

            var telemetry = await migration.GetTelemetryAsync().ConfigureAwait(false);
            Assert.That(telemetry.Outcomes.Sum(outcome => outcome.Count), Is.EqualTo(expectedOutcomeCount));
        }
    }

    [Test]
    public async Task ReadMigration_Should_PublishSafeOutcomeAndRollbackMetrics()
    {
        var measurements = new List<(string Name, long Value, Dictionary<string, string?> Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == CompatibilityReadMetrics.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            measurements.Add((
                instrument.Name,
                measurement,
                tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value?.ToString())));
        });
        listener.Start();

        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-sensitive-value", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-sensitive-value", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);
        Assert.That(await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(7)), Is.True);
        listener.RecordObservableInstruments();

        var readOutcome = measurements.Single(measurement =>
            measurement.Name == CompatibilityReadMetrics.ReadOutcomeMetricName &&
            measurement.Tags["transport"] == "rest");
        var rollback = measurements.Single(measurement =>
            measurement.Name == CompatibilityReadMetrics.RollbackActiveMetricName);
        Assert.Multiple(() =>
        {
            Assert.That(readOutcome.Tags.Values, Does.Not.Contain("credential-sensitive-value"));
            Assert.That(readOutcome.Tags.ContainsKey("client"), Is.False);
            Assert.That(rollback.Value, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ReadMigration_Should_LogAuditEvent_WhenRollbackIsActivated()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var context = SecurityTestContext.Create(loggerProvider: loggerProvider);
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
        Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);

        Assert.That(await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromHours(24)), Is.True);

        var audit = loggerProvider.Entries.Single(entry =>
            entry.Level == LogLevel.Warning &&
            entry.EventId.Name == "CompatibilityReadRollbackActivated");
        Assert.That(audit.Message, Does.Contain("activated for"));
    }

    [Test]
    public async Task ReadMigration_Should_LogStructuredWarning_WhenRollbackSurvivesRestart()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                var migration = first.GetRequiredService<ICompatibilityReadMigration>();
                await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
                await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
                await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
                first.TimeProvider.Advance(TimeSpan.FromDays(14));
                await migration.RecordIssueEvidenceAsync("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
                Assert.That(await migration.EnableAuthenticatedReadsAsync(), Is.True);
                Assert.That(await migration.ActivateAnonymousReadRollbackAsync(TimeSpan.FromDays(7)), Is.True);
            }

            var loggerProvider = new CapturingLoggerProvider();
            using var restarted = SecurityTestContext.Create(root, loggerProvider: loggerProvider);
            restarted.TimeProvider.Advance(TimeSpan.FromDays(14));

            foreach (var hostedService in restarted.Services.GetServices<IHostedService>())
                await hostedService.StartAsync(CancellationToken.None);

            var warning = loggerProvider.Entries.Single(entry =>
                entry.Level == LogLevel.Warning &&
                entry.EventId.Name == "CompatibilityReadRollbackActive");
            Assert.Multiple(() =>
            {
                Assert.That(warning.Message, Does.Contain("anonymous read-only rollback"));
                Assert.That(warning.Message, Does.Not.Contain("credential-1"));
            });
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ReadMigrationStartupReporter_Should_KeepHostAlive_WhenProtectedStateIsCorrupt()
    {
        var root = SecurityTestContext.CreateTemporaryRoot();
        try
        {
            using (var first = SecurityTestContext.Create(root))
            {
                await first.GetRequiredService<ICompatibilityReadMigration>()
                    .BeginReadinessWindowAsync("MOBAsmart 1.0.0")
                    .ConfigureAwait(false);
            }

            await File.WriteAllBytesAsync(
                Path.Combine(root, "store", "read-migration.dat"),
                [0x01, 0x02, 0x03]).ConfigureAwait(false);
            using var loggerProvider = new CapturingLoggerProvider();
            using var restarted = SecurityTestContext.Create(root, loggerProvider: loggerProvider);

            foreach (var hostedService in restarted.Services.GetServices<IHostedService>())
                await hostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);

            var warning = loggerProvider.Entries.Single(entry =>
                entry.Level == LogLevel.Error &&
                entry.EventId.Name == "CompatibilityReadStateUnavailable");
            Assert.That(warning.Message, Does.Contain("fail-closed"));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ControlPlaneSecurityController_Should_RejectPrematureReadEnforcement()
    {
        using var context = SecurityTestContext.Create();
        var controller = new ControlPlaneSecurityController(
            context.GetRequiredService<ICredentialRegistry>(),
            context.GetRequiredService<IPairingService>(),
            context.GetRequiredService<ICompatibilityReadMigration>());
        await controller.BeginReadinessWindow(
            new BeginReadinessWindowRequest("MOBAsmart 1.0.0"),
            CancellationToken.None);

        var result = await controller.EnableAuthenticatedReads(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task ControlPlaneSecurityController_Should_ReturnConflict_WhenEvidenceRevalidationFails()
    {
        var verifier = new FailingEvidenceRevalidationVerifier();
        using var context = SecurityTestContext.Create(evidenceVerifier: verifier);
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        await MakeReadMigrationReadyAsync(context, migration).ConfigureAwait(false);
        var controller = new ControlPlaneSecurityController(
            context.GetRequiredService<ICredentialRegistry>(),
            context.GetRequiredService<IPairingService>(),
            migration);

        var result = await controller
            .EnableAuthenticatedReads(CancellationToken.None)
            .ConfigureAwait(false);

        var conflict = result as ConflictObjectResult;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(conflict, Is.Not.Null);
            Assert.That(conflict?.Value, Is.InstanceOf<ProblemDetails>());
            Assert.That(((ProblemDetails?)conflict?.Value)?.Title, Does.Contain("evidence"));
        }
    }

    [Test]
    public async Task ControlPlaneSecurityController_Should_ManageEvidenceEnforcementAndRollback()
    {
        using var context = SecurityTestContext.Create();
        var migration = context.GetRequiredService<ICompatibilityReadMigration>();
        var controller = new ControlPlaneSecurityController(
            context.GetRequiredService<ICredentialRegistry>(),
            context.GetRequiredService<IPairingService>(),
            migration);
        await controller.BeginReadinessWindow(
            new BeginReadinessWindowRequest("MOBAsmart 1.0.0"),
            CancellationToken.None);
        var defect = await controller.RecordCriticalDefect(
            new RecordCriticalDefectRequest("rest-signalr-parity"),
            CancellationToken.None);
        var defectFixed = await controller.RecordCriticalDefectFixed(
            new RecordCriticalDefectFixedRequest("rest-signalr-parity"),
            CancellationToken.None);
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));

        var evidence = await controller.RecordReadinessEvidence(
            new RecordReadinessEvidenceRequest("https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890"),
            CancellationToken.None);
        var enforcement = await controller.EnableAuthenticatedReads(CancellationToken.None);
        var rollback = await controller.ActivateAnonymousReadRollback(
            new ActivateAnonymousReadRollbackRequest(168),
            CancellationToken.None);
        var status = await controller.GetReadMigrationStatus(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(defect, Is.InstanceOf<NoContentResult>());
            Assert.That(defectFixed, Is.InstanceOf<NoContentResult>());
            Assert.That(evidence, Is.InstanceOf<NoContentResult>());
            Assert.That(enforcement, Is.InstanceOf<NoContentResult>());
            Assert.That(rollback, Is.InstanceOf<NoContentResult>());
            Assert.That(status.Value?.RollbackExpiresAt, Is.EqualTo(context.TimeProvider.GetUtcNow().AddDays(7)));
        });
    }

    [Test]
    public async Task ReadPolicy_Should_RejectStalePrincipal_WhenPresentedCredentialWasRevoked()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var authorization = context.GetRequiredService<IAuthorizationService>();
        var issued = await registry.CreateAsync("Observer", ControlPlaneRole.ReadOnly);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);
        var stalePrincipal = await tokenService.ValidateAsync(token!.Token);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {token.Token}";
        await registry.RevokeAsync(issued.Credential.CredentialId, "operator_revoked");

        var result = await authorization.AuthorizeAsync(
            stalePrincipal!,
            httpContext,
            ControlPlaneCapabilities.Read);

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task ReadPolicy_Should_RejectMalformedPresentedCredential()
    {
        using var context = SecurityTestContext.Create();
        var authorization = context.GetRequiredService<IAuthorizationService>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer";

        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            httpContext,
            ControlPlaneCapabilities.Read);

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task RuntimeControlPolicy_Should_RejectStalePrincipalAfterDowngrade()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var tokenService = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var authorization = context.GetRequiredService<IAuthorizationService>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);
        var token = await tokenService.IssueAsync(issued.Credential.CredentialId);
        var stalePrincipal = await tokenService.ValidateAsync(token!.Token);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {token.Token}";
        await registry.ChangeRoleAsync(issued.Credential.CredentialId, ControlPlaneRole.ReadOnly);

        var result = await authorization.AuthorizeAsync(
            stalePrincipal!,
            httpContext,
            ControlPlaneCapabilities.RuntimeControl);

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task ChangeRoleAsync_Should_AbortActiveCredentialConnections()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var revoker = context.GetRequiredService<IControlPlaneConnectionRevoker>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);
        var aborted = false;
        revoker.Register(
            "connection-1",
            issued.Credential.CredentialId,
            context.TimeProvider.GetUtcNow().AddMinutes(5),
            () => aborted = true);

        var changed = await registry.ChangeRoleAsync(
            issued.Credential.CredentialId,
            ControlPlaneRole.ReadOnly);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(aborted, Is.True);
        });
    }

    [Test]
    public async Task RevokeAsync_Should_AbortActiveCredentialConnections()
    {
        using var context = SecurityTestContext.Create();
        var registry = context.GetRequiredService<ICredentialRegistry>();
        var revoker = context.GetRequiredService<IControlPlaneConnectionRevoker>();
        var issued = await registry.CreateAsync("Remote cab", ControlPlaneRole.RemoteControl);
        var aborted = false;
        revoker.Register(
            "connection-1",
            issued.Credential.CredentialId,
            context.TimeProvider.GetUtcNow().AddMinutes(5),
            () => aborted = true);

        var revoked = await registry.RevokeAsync(issued.Credential.CredentialId, "operator_revoked");

        Assert.Multiple(() =>
        {
            Assert.That(revoked, Is.True);
            Assert.That(aborted, Is.True);
        });
    }

    [Test]
    public void Register_Should_AbortConnection_WhenAccessTokenIsAlreadyExpired()
    {
        using var context = SecurityTestContext.Create();
        var revoker = context.GetRequiredService<IControlPlaneConnectionRevoker>();
        var aborted = false;

        revoker.Register(
            "connection-1",
            "credential-1",
            context.TimeProvider.GetUtcNow().AddSeconds(-1),
            () => aborted = true);

        Assert.That(aborted, Is.True);
    }

    [TestCase(typeof(FeedbackSequencesController), nameof(FeedbackSequencesController.Get))]
    [TestCase(typeof(JourneyProgressController), nameof(JourneyProgressController.Get))]
    [TestCase(typeof(PhotosController), nameof(PhotosController.GetFile))]
    [TestCase(typeof(RuntimeController), nameof(RuntimeController.GetMeta))]
    [TestCase(typeof(RuntimeController), nameof(RuntimeController.GetSnapshot))]
    [TestCase(typeof(RuntimeSettingsController), nameof(RuntimeSettingsController.GetRuntimeSettings))]
    [TestCase(typeof(SolutionController), nameof(SolutionController.GetMeta))]
    [TestCase(typeof(SolutionController), nameof(SolutionController.GetSolution))]
    [TestCase(typeof(StatusController), nameof(StatusController.GetStatus))]
    public void ReadOperation_Should_UseSharedReadPolicy(Type declaringType, string methodName)
    {
        var method = declaringType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Missing read operation {declaringType.Name}.{methodName}.");
        var attribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(candidate => candidate.Policy == ControlPlaneCapabilities.Read);

        Assert.That(attribute, Is.Not.Null);
    }

    [TestCase(typeof(ClientsController), nameof(ClientsController.Register), ControlPlaneCapabilities.ClientPresence)]
    [TestCase(typeof(ClientsController), nameof(ClientsController.Unregister), ControlPlaneCapabilities.ClientPresence)]
    [TestCase(typeof(PhotosController), nameof(PhotosController.Upload), ControlPlaneCapabilities.PhotoWrite)]
    [TestCase(typeof(FeedbackSequencesController), nameof(FeedbackSequencesController.Put), ControlPlaneCapabilities.HostPublish)]
    [TestCase(typeof(JourneyProgressController), nameof(JourneyProgressController.Reset), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeCommandsController), nameof(RuntimeCommandsController.EnqueueSignalAspect), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeCommandsController), nameof(RuntimeCommandsController.EnqueueLocomotiveDrive), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeCommandsController), nameof(RuntimeCommandsController.EnqueueLocomotiveFunction), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeHub), nameof(RuntimeHub.SetSignalAspect), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeHub), nameof(RuntimeHub.SetLocomotiveDrive), ControlPlaneCapabilities.RuntimeControl)]
    [TestCase(typeof(RuntimeHub), nameof(RuntimeHub.SetLocomotiveFunction), ControlPlaneCapabilities.RuntimeControl)]
    public void RestrictedOperation_Should_UseDesignedCapability(
        Type declaringType,
        string methodName,
        string capability)
    {
        var method = declaringType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Missing restricted operation {declaringType.Name}.{methodName}.");
        var attribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(candidate => candidate.Policy == capability);

        Assert.That(attribute, Is.Not.Null);
    }

    [Test]
    public void PhotoHub_Should_UseSharedReadPolicy()
    {
        var attribute = typeof(PhotoHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(candidate => candidate.Policy == ControlPlaneCapabilities.Read);

        Assert.That(attribute, Is.Not.Null);
    }

    [Test]
    public void RuntimeHub_Should_UseSharedReadPolicy_ForLegacyAndAuthenticatedConnections()
    {
        var hubAttribute = typeof(RuntimeHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(candidate => candidate.Policy == ControlPlaneCapabilities.Read);
        var registerRemoteAttribute = typeof(RuntimeHub)
            .GetMethod(nameof(RuntimeHub.RegisterRemote))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(candidate => candidate.Policy == ControlPlaneCapabilities.ClientPresence);

        Assert.Multiple(() =>
        {
            Assert.That(hubAttribute, Is.Not.Null);
            Assert.That(registerRemoteAttribute, Is.Null);
        });
    }

    [Test]
    public void ClientsController_Should_RegisterAuthenticatedCredentialIdentity()
    {
        var registry = new ClientRegistry();
        var controller = CreateClientsController(registry, "credential-1");

        controller.Register(new RegisterClientRequest("caller-selected-id", "Remote cab"));

        Assert.That(registry.GetAll().Single().ClientId, Is.EqualTo("credential-1"));
    }

    [Test]
    public void ClientsController_Should_UnregisterOnlyAuthenticatedCredentialIdentity()
    {
        var registry = new ClientRegistry();
        registry.Add(new ConnectedClientInfo { ClientId = "credential-1" });
        registry.Add(new ConnectedClientInfo { ClientId = "credential-2" });
        var controller = CreateClientsController(registry, "credential-1");

        controller.Unregister(new UnregisterClientRequest("credential-2"));

        Assert.That(registry.GetAll().Select(client => client.ClientId), Is.EqualTo(new[] { "credential-2" }));
    }

    private static ClientsController CreateClientsController(ClientRegistry registry, string credentialId)
    {
        var controller = new ClientsController(registry);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, credentialId)],
                ControlPlaneAuthenticationDefaults.Scheme))
        };
        return controller;
    }

    private static ServiceProvider CreateEvidenceVerifierServices(HttpResponseMessage response)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControlPlaneSecurity(new ConfigurationBuilder().Build());
        services.RemoveAll<IHttpClientFactory>();
        services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(response));
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task MakeReadMigrationReadyAsync(
        SecurityTestContext context,
        ICompatibilityReadMigration migration)
    {
        await migration.BeginReadinessWindowAsync("MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.Rest, "MOBAsmart 1.0.0");
        await migration.RecordAuthenticatedReadAsync("credential-1", CompatibilityReadTransport.SignalR, "MOBAsmart 1.0.0");
        context.TimeProvider.Advance(TimeSpan.FromDays(14));
        await migration.RecordIssueEvidenceAsync(
            "https://github.com/ahuelsmann/MOBAflow/issues/50#issuecomment-1234567890");
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

        private SecurityTestContext(
            string root,
            bool ownsRoot,
            TimeSpan accessTokenLifetime,
            int pairingMaximumFailedAttempts,
            ILoggerProvider? loggerProvider,
            ICompatibilityReadEvidenceVerifier? evidenceVerifier,
            ICompatibilityReadConnectionRevoker? connectionRevoker)
        {
            Root = root;
            _ownsRoot = ownsRoot;
            TimeProvider = new AdjustableTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero));
            _serviceProvider = CreateServiceProvider(
                root,
                accessTokenLifetime,
                pairingMaximumFailedAttempts,
                TimeProvider,
                loggerProvider,
                evidenceVerifier,
                connectionRevoker);
        }

        public string Root { get; }

        public AdjustableTimeProvider TimeProvider { get; }

        public IServiceProvider Services => _serviceProvider;

        public static SecurityTestContext Create(
            string? root = null,
            TimeSpan? accessTokenLifetime = null,
            int pairingMaximumFailedAttempts = 5,
            ILoggerProvider? loggerProvider = null,
            ICompatibilityReadEvidenceVerifier? evidenceVerifier = null,
            ICompatibilityReadConnectionRevoker? connectionRevoker = null)
        {
            var ownsRoot = root is null;
            return new SecurityTestContext(
                root ?? CreateTemporaryRoot(),
                ownsRoot,
                accessTokenLifetime ?? TimeSpan.FromMinutes(5),
                pairingMaximumFailedAttempts,
                loggerProvider,
                evidenceVerifier,
                connectionRevoker);
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
            TimeProvider timeProvider,
            ILoggerProvider? loggerProvider,
            ICompatibilityReadEvidenceVerifier? evidenceVerifier,
            ICompatibilityReadConnectionRevoker? connectionRevoker)
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
            services.AddLogging(builder =>
            {
                if (loggerProvider is not null)
                    builder.AddProvider(loggerProvider);
            });
            services.AddControlPlaneSecurity(configuration);
            services.RemoveAll<ICompatibilityReadEvidenceVerifier>();
            if (evidenceVerifier is null)
                services.AddSingleton<ICompatibilityReadEvidenceVerifier, TestCompatibilityReadEvidenceVerifier>();
            else
                services.AddSingleton(evidenceVerifier);
            if (connectionRevoker is not null)
            {
                services.RemoveAll<ICompatibilityReadConnectionRevoker>();
                services.AddSingleton(connectionRevoker);
            }
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "keys")));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(timeProvider);
            return services.BuildServiceProvider(validateScopes: true);
        }
    }

    private sealed class TestCompatibilityReadEvidenceVerifier : ICompatibilityReadEvidenceVerifier
    {
        public Task VerifyAsync(
            Uri evidenceUri,
            string stableClientRelease,
            DateTimeOffset observationCompletedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (evidenceUri.Fragment != "#issuecomment-1234567890")
                throw new InvalidOperationException("The test evidence comment does not exist.");
            if (string.IsNullOrWhiteSpace(stableClientRelease) || observationCompletedAt == default)
                throw new InvalidOperationException("The test evidence context is incomplete.");
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingCompatibilityReadEvidenceVerifier : ICompatibilityReadEvidenceVerifier
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task VerifyAsync(
            Uri evidenceUri,
            string stableClientRelease,
            DateTimeOffset observationCompletedAt,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class FailingEvidenceRevalidationVerifier : ICompatibilityReadEvidenceVerifier
    {
        private int _verificationCount;

        public Task VerifyAsync(
            Uri evidenceUri,
            string stableClientRelease,
            DateTimeOffset observationCompletedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Increment(ref _verificationCount) > 1)
                throw new InvalidOperationException("The issue evidence is no longer valid.");

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingCompatibilityReadConnectionRevoker : ICompatibilityReadConnectionRevoker
    {
        public int RevocationCount { get; private set; }

        public void Register(string connectionId, Action abort)
        {
        }

        public void Unregister(string connectionId)
        {
        }

        public void RevokeAll() => RevocationCount++;
    }

    private sealed class StubHttpClientFactory(HttpResponseMessage response) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(response))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }

    private sealed record CapturedLog(LogLevel Level, EventId EventId, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<CapturedLog> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<CapturedLog> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Add(new CapturedLog(logLevel, eventId, formatter(state, exception)));
            }
        }
    }

    public sealed partial class AdjustableTimeProvider : TimeProvider
    {
        private readonly Lock _sync = new();
        private readonly List<AdjustableTimer> _timers = [];
        private DateTimeOffset _utcNow;

        public AdjustableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
                return _utcNow;
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            var timer = new AdjustableTimer(this, callback, state);
            lock (_sync)
            {
                _timers.Add(timer);
                Schedule(timer, dueTime, period);
            }

            return timer;
        }

        public void Advance(TimeSpan duration)
        {
            List<AdjustableTimer> dueTimers;
            lock (_sync)
            {
                _utcNow = _utcNow.Add(duration);
                dueTimers = _timers
                    .Where(timer => timer.IsActive && timer.DueAt <= _utcNow)
                    .ToList();
                foreach (var timer in dueTimers)
                {
                    if (timer.Period == Timeout.InfiniteTimeSpan)
                        timer.IsActive = false;
                    else
                        timer.DueAt = _utcNow.Add(timer.Period);
                }
            }

            foreach (var timer in dueTimers)
                timer.Invoke();
        }

        private void Change(AdjustableTimer timer, TimeSpan dueTime, TimeSpan period)
        {
            lock (_sync)
                Schedule(timer, dueTime, period);
        }

        private void Remove(AdjustableTimer timer)
        {
            lock (_sync)
                _timers.Remove(timer);
        }

        private void Schedule(AdjustableTimer timer, TimeSpan dueTime, TimeSpan period)
        {
            timer.Period = period;
            timer.IsActive = dueTime != Timeout.InfiniteTimeSpan;
            timer.DueAt = timer.IsActive ? _utcNow.Add(dueTime) : DateTimeOffset.MaxValue;
        }

        private sealed partial class AdjustableTimer(
            AdjustableTimeProvider owner,
            TimerCallback callback,
            object? state) : ITimer
        {
            public DateTimeOffset DueAt { get; set; }

            public TimeSpan Period { get; set; }

            public bool IsActive { get; set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                owner.Change(this, dueTime, period);
                return true;
            }

            public void Dispose()
            {
                IsActive = false;
                owner.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void Invoke() => callback(state);
        }
    }
}
