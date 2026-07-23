// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Common.Discovery;
using Moba.Common.Security;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

[TestFixture]
internal sealed class RemotePairingViewModelTests
{
    private const string PairingSecret = "1234567890123456789012345678901234567890123";

    [Test]
    public async Task DiscoverAsync_Should_RequireExplicitFingerprintConfirmation()
    {
        var viewModel = CreateViewModel(out _, out _);

        await viewModel.DiscoverCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.CandidateFound));
            Assert.That(viewModel.ServerAddress, Is.EqualTo("192.0.2.1:5443"));
            Assert.That(viewModel.Fingerprint, Has.Length.EqualTo(95));
            Assert.That(viewModel.IsFingerprintConfirmed, Is.False);
            Assert.That(viewModel.StartPairingCommand.CanExecute(null), Is.False);
        });

        viewModel.PairingSecret = PairingSecret;
        viewModel.IsFingerprintConfirmed = true;

        Assert.That(viewModel.StartPairingCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task StartPairingAsync_Should_ClearSecretPollApprovalAndPersistCredential()
    {
        var viewModel = CreateViewModel(out var store, out var transport);
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(RemotePairingClaimStatus.PendingApproval));
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(
            RemotePairingClaimStatus.Succeeded,
            CreateTokenResponse(RemoteControlRole.RemoteControl)));
        await viewModel.DiscoverCommand.ExecuteAsync(null);
        viewModel.PairingSecret = PairingSecret;
        viewModel.IsFingerprintConfirmed = true;

        await viewModel.StartPairingCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Paired));
            Assert.That(viewModel.PairingSecret, Is.Empty);
            Assert.That(viewModel.ConfirmationCode, Is.Empty);
            Assert.That(viewModel.PairedRole, Is.EqualTo("Remote control"));
            Assert.That(store.Saved?.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(transport.RequestedRole, Is.EqualTo(RemoteControlRole.RemoteControl));
            Assert.That(transport.ClaimCount, Is.EqualTo(2));
            Assert.That(viewModel.DiscoverCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.StatusMessage, Does.Not.Contain(PairingSecret));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("access-token"));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("refresh-token"));
        });
    }

    [Test]
    public async Task StartPairingAsync_Should_RequestReadOnlyRole()
    {
        var viewModel = CreateViewModel(out _, out var transport);
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(
            RemotePairingClaimStatus.Succeeded,
            CreateTokenResponse(RemoteControlRole.ReadOnly)));
        await viewModel.DiscoverCommand.ExecuteAsync(null);
        viewModel.IsReadOnlySelected = true;
        viewModel.PairingSecret = PairingSecret;
        viewModel.IsFingerprintConfirmed = true;

        await viewModel.StartPairingCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsRemoteControlSelected, Is.False);
            Assert.That(transport.RequestedRole, Is.EqualTo(RemoteControlRole.ReadOnly));
            Assert.That(viewModel.PairedRole, Is.EqualTo("Read only"));
        });
    }

    [Test]
    public async Task StartPairingAsync_Should_ReportRejectedClaimWithoutLeakingClaimSecret()
    {
        var viewModel = CreateViewModel(out _, out var transport);
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(RemotePairingClaimStatus.Rejected));
        await viewModel.DiscoverCommand.ExecuteAsync(null);
        viewModel.PairingSecret = PairingSecret;
        viewModel.IsFingerprintConfirmed = true;

        await viewModel.StartPairingCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Error));
            Assert.That(viewModel.PairingSecret, Is.Empty);
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("claim-secret"));
            Assert.That(viewModel.StatusMessage, Does.Contain("rejected"));
        });
    }

    [Test]
    public async Task InitializeAsync_Should_RestoreProtectedCredentialWithoutExposingToken()
    {
        var endpoint = CreateEndpoint();
        var store = new FakeCredentialStore
        {
            Saved = new RemoteControlCredential(
                endpoint.ServerInstanceId!,
                endpoint.IpAddress,
                endpoint.HttpsPort!.Value,
                endpoint.ServerPublicKeyFingerprint!,
                "credential-1",
                "stored-refresh",
                RemoteControlRole.ReadOnly,
                1)
        };
        var transport = new FakeTransport
        {
            RefreshResult = CreateTokenResponse(RemoteControlRole.ReadOnly)
        };
        var viewModel = CreateViewModel(new FakeDiscoveryService(endpoint), store, transport);

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Paired));
            Assert.That(viewModel.PairedRole, Is.EqualTo("Read only"));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("access-token"));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("refresh-token"));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain("stored-refresh"));
        });
    }

    [Test]
    public async Task ForgetAsync_Should_ClearProtectedCredential()
    {
        var viewModel = CreateViewModel(out var store, out var transport);
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(
            RemotePairingClaimStatus.Succeeded,
            CreateTokenResponse(RemoteControlRole.RemoteControl)));
        await viewModel.DiscoverCommand.ExecuteAsync(null);
        viewModel.PairingSecret = PairingSecret;
        viewModel.IsFingerprintConfirmed = true;
        await viewModel.StartPairingCommand.ExecuteAsync(null);

        await viewModel.ForgetCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(store.Saved, Is.Null);
            Assert.That(store.ClearCount, Is.EqualTo(1));
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.CandidateFound));
            Assert.That(viewModel.IsPaired, Is.False);
        });
    }

    private static RemotePairingViewModel CreateViewModel(
        out FakeCredentialStore store,
        out FakeTransport transport)
    {
        store = new FakeCredentialStore();
        transport = new FakeTransport();
        return CreateViewModel(new FakeDiscoveryService(CreateEndpoint()), store, transport);
    }

    private static RemotePairingViewModel CreateViewModel(
        IAuthenticatedRestDiscoveryService discoveryService,
        FakeCredentialStore store,
        FakeTransport transport) =>
        new(
            discoveryService,
            new RemoteControlSessionService(store, transport),
            TimeProvider.System,
            new RemotePairingPollingOptions(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1)));

    private static MobApiDiscoveryEndpoint CreateEndpoint() => new(
        "192.0.2.1",
        5001,
        5443,
        "11111111111111111111111111111111",
        new string('A', 64),
        DiscoveryResponseParser.CurrentProtocolVersion);

    private static RemoteControlTokenResponse CreateTokenResponse(RemoteControlRole role) => new(
        "credential-1",
        "access-token",
        DateTimeOffset.UtcNow.AddMinutes(5),
        "refresh-token",
        role,
        1);

    private sealed class FakeDiscoveryService(MobApiDiscoveryEndpoint? endpoint)
        : IAuthenticatedRestDiscoveryService
    {
        public Task<MobApiDiscoveryEndpoint?> DiscoverAuthenticatedServerAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(endpoint);
    }

    private sealed class FakeCredentialStore : IRemoteControlCredentialStore
    {
        public RemoteControlCredential? Saved { get; set; }

        public int ClearCount { get; private set; }

        public Task<RemoteControlCredential?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved);

        public Task SaveAsync(
            RemoteControlCredential credential,
            CancellationToken cancellationToken = default)
        {
            Saved = credential;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Saved = null;
            ClearCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransport : IRemoteControlTransport
    {
        public Queue<RemotePairingClaimResult> ClaimResults { get; } = [];

        public int ClaimCount { get; private set; }

        public RemoteControlRole? RequestedRole { get; private set; }

        public RemoteControlTokenResponse RefreshResult { get; set; } =
            CreateTokenResponse(RemoteControlRole.ReadOnly);

        public Task<RemotePairingSubmissionResult> SubmitPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string pairingSecret,
            string clientNonce,
            string displayName,
            RemoteControlRole requestedRole,
            CancellationToken cancellationToken = default)
        {
            RequestedRole = requestedRole;
            return Task.FromResult(new RemotePairingSubmissionResult(
                RemotePairingSubmissionStatus.Accepted,
                "request-1",
                "claim-secret",
                "123456"));
        }

        public Task<RemotePairingClaimResult> ClaimPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string requestId,
            string claimToken,
            CancellationToken cancellationToken = default)
        {
            ClaimCount++;
            var result = ClaimResults.Count > 0
                ? ClaimResults.Dequeue()
                : new RemotePairingClaimResult(RemotePairingClaimStatus.PendingApproval);
            return Task.FromResult(result);
        }

        public Task<RemoteControlTokenResponse> RefreshAsync(
            RemoteControlCredential credential,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(RefreshResult);
    }
}