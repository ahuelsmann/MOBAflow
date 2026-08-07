// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Common.Discovery;
using Moba.Common.Events;
using Moba.Common.Security;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Microsoft.Extensions.Logging.Abstractions;

[TestFixture]
internal sealed class RemotePairingViewModelTests
{
    [Test]
    public async Task OpenScannerAsync_Should_RequestCameraAndShowScanner()
    {
        var permission = new FakeCameraPermission(true);
        var viewModel = CreateViewModel(permission: permission);

        await viewModel.OpenScannerCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(permission.RequestCount, Is.EqualTo(1));
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Scanning));
            Assert.That(viewModel.IsScannerVisible, Is.True);
        });
    }

    [Test]
    public async Task ScanQrCodeAsync_Should_RejectInvalidPayloadWithoutSubmittingSecret()
    {
        var transport = new FakeTransport();
        var viewModel = CreateViewModel(transport: transport);

        await viewModel.ScanQrCodeCommand.ExecuteAsync("not-a-mobaflow-code");

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Error));
            Assert.That(viewModel.StatusMessage, Does.Contain("not a valid"));
            Assert.That(transport.SubmitCount, Is.Zero);
        });
    }

    [Test]
    public async Task ScanQrCodeAsync_Should_RequestAdministratorPersistCredentialAndPublishEndpoint()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        RemotePairingCompletedEvent? completed = null;
        eventBus.Subscribe<RemotePairingCompletedEvent>(value => completed = value);
        var store = new FakeCredentialStore();
        var transport = new FakeTransport();
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(RemotePairingClaimStatus.PendingApproval));
        transport.ClaimResults.Enqueue(new RemotePairingClaimResult(
            RemotePairingClaimStatus.Succeeded,
            CreateTokenResponse(RemoteControlRole.RemoteControl)));
        var viewModel = CreateViewModel(store, transport, eventBus: eventBus);
        var payload = CreateQrPayload();

        await viewModel.ScanQrCodeCommand.ExecuteAsync(payload);

        Assert.Multiple(() =>
        {
            Assert.That(transport.RequestedRole, Is.EqualTo(RemoteControlRole.RemoteControl));
            Assert.That(transport.SubmitCount, Is.EqualTo(1));
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Paired));
            Assert.That(store.Saved?.Role, Is.EqualTo(RemoteControlRole.RemoteControl));
            Assert.That(completed?.IpAddress, Is.EqualTo("192.168.0.27"));
            Assert.That(completed?.HttpPort, Is.EqualTo(5001));
            Assert.That(viewModel.StatusMessage, Does.Not.Contain(new string('B', 43)));
        });
    }

    [Test]
    public async Task InitializeAsync_Should_RestoreAdministratorCredential()
    {
        var store = new FakeCredentialStore
        {
            Saved = CreateCredential(RemoteControlRole.RemoteControl)
        };
        var transport = new FakeTransport
        {
            RefreshResult = CreateTokenResponse(RemoteControlRole.RemoteControl)
        };
        var viewModel = CreateViewModel(store, transport);

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Paired));
            Assert.That(viewModel.StatusMessage, Does.Contain("Administrator"));
            Assert.That(store.ClearCount, Is.Zero);
        });
    }

    [Test]
    public async Task InitializeAsync_Should_ClearLegacyReadOnlyCredential()
    {
        var store = new FakeCredentialStore
        {
            Saved = CreateCredential(RemoteControlRole.ReadOnly)
        };
        var transport = new FakeTransport
        {
            RefreshResult = CreateTokenResponse(RemoteControlRole.ReadOnly)
        };
        var viewModel = CreateViewModel(store, transport);

        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RemotePairingUiState.Unpaired));
            Assert.That(viewModel.StatusMessage, Does.Contain("no longer supported"));
            Assert.That(store.ClearCount, Is.EqualTo(1));
        });
    }

    private static RemotePairingViewModel CreateViewModel(
        FakeCredentialStore? store = null,
        FakeTransport? transport = null,
        FakeCameraPermission? permission = null,
        IEventBus? eventBus = null) =>
        new(
            new RemoteControlSessionService(store ?? new FakeCredentialStore(), transport ?? new FakeTransport()),
            permission,
            eventBus,
            TimeProvider.System,
            new RemotePairingPollingOptions(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1)));

    private static string CreateQrPayload() => RemotePairingQrCode.Encode(new RemotePairingQrInvitation(
        "192.168.0.27",
        5001,
        5002,
        Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
        new string('A', 64),
        new string('B', 43),
        DateTimeOffset.UtcNow.AddMinutes(2)));

    private static RemoteControlCredential CreateCredential(RemoteControlRole role) => new(
        Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
        "192.168.0.27",
        5002,
        new string('A', 64),
        "credential-1",
        "refresh-token",
        role,
        1);

    private static RemoteControlTokenResponse CreateTokenResponse(RemoteControlRole role) => new(
        "credential-1",
        "access-token",
        DateTimeOffset.UtcNow.AddMinutes(5),
        "refresh-token-2",
        role,
        1);

    private sealed class FakeCameraPermission(bool granted) : IPairingCameraPermission
    {
        public int RequestCount { get; private set; }

        public Task<bool> RequestAsync(CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(granted);
        }
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

        public int SubmitCount { get; private set; }

        public RemoteControlRole? RequestedRole { get; private set; }

        public RemoteControlTokenResponse RefreshResult { get; set; } =
            CreateTokenResponse(RemoteControlRole.RemoteControl);

        public Task<RemotePairingSubmissionResult> SubmitPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string pairingSecret,
            string clientNonce,
            string displayName,
            RemoteControlRole requestedRole,
            CancellationToken cancellationToken = default)
        {
            SubmitCount++;
            RequestedRole = requestedRole;
            return Task.FromResult(new RemotePairingSubmissionResult(
                RemotePairingSubmissionStatus.Accepted,
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ToString("N"),
                "claim-secret",
                "123456"));
        }

        public Task<RemotePairingClaimResult> ClaimPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string requestId,
            string claimToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ClaimResults.Count > 0
                ? ClaimResults.Dequeue()
                : new RemotePairingClaimResult(RemotePairingClaimStatus.PendingApproval));

        public Task<RemoteControlTokenResponse> RefreshAsync(
            RemoteControlCredential credential,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(RefreshResult);
    }
}