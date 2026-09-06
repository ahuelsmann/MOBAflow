// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Diagnostics;

using Moba.Common.Security;

namespace Moba.Test.Common;

[TestFixture]
[NonParallelizable]
internal sealed class HostBootstrapProtocolTests
{
    private static readonly string[] BootstrapEnvironmentVariables =
    [
        HostBootstrapProtocol.RequestPipeEnvironmentVariable,
        HostBootstrapProtocol.ResponsePipeEnvironmentVariable,
        HostBootstrapProtocol.ParentProcessEnvironmentVariable
    ];

    [Test]
    public void CreateSecret_Should_ReturnUrlSafeFixedLengthValue()
    {
        var secret = HostBootstrapProtocol.CreateSecret();

        Assert.That(secret, Has.Length.EqualTo(43));
        Assert.That(secret, Does.Match("^[A-Za-z0-9_-]{43}$"));
    }

    [Test]
    public void CreateSecret_Should_ReturnDifferentValues()
    {
        var first = HostBootstrapProtocol.CreateSecret();
        var second = HostBootstrapProtocol.CreateSecret();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public async Task ParentChannel_Should_RejectExchangeBeforeHandleTransfer()
    {
        await using var parent = new HostBootstrapParentChannel();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Assert.That(
            async () => await parent.ExchangeAsync(timeout.Token),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ParentChannel_Should_HandleTransferIdempotently_AndHonorCancellation()
    {
        await using var parent = new HostBootstrapParentChannel();
        parent.CompleteHandleTransfer();
        parent.CompleteHandleTransfer();

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.That(
            async () => await parent.ExchangeAsync(canceled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task ChildChannel_Should_HonorCancellationBeforePipeAccess()
    {
        await using var parent = new HostBootstrapParentChannel();
        var startInfo = new ProcessStartInfo();
        parent.Configure(startInfo);
        using var environment = ApplyBootstrapEnvironment(startInfo);
        await using var child = HostBootstrapChildChannel.TryOpenFromEnvironment();
        Assert.That(child, Is.Not.Null);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.That(
            async () => await child!.ReadRequestAsync(canceled.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(
            async () => await child!.WriteResponseAsync(
                new HostBootstrapPipeResponse("fingerprint", "instance"),
                canceled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void HostTokenResponse_Should_ExposeCredentialValues()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(1);
        var response = new HostTokenResponse("credential", "access", expiresAt, "renewal");

        Assert.Multiple(() =>
        {
            Assert.That(response.CredentialId, Is.EqualTo("credential"));
            Assert.That(response.AccessToken, Is.EqualTo("access"));
            Assert.That(response.AccessTokenExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(response.RenewalToken, Is.EqualTo("renewal"));
        });
    }

    [Test]
    public async Task ChildChannel_Should_ReturnNullAndClearPartialEnvironment()
    {
        var startInfo = new ProcessStartInfo();
        startInfo.Environment[HostBootstrapProtocol.RequestPipeEnvironmentVariable] = "missing-response";
        startInfo.Environment[HostBootstrapProtocol.ParentProcessEnvironmentVariable] = "123";
        using var environment = ApplyBootstrapEnvironment(startInfo);

        var child = HostBootstrapChildChannel.TryOpenFromEnvironment();

        Assert.That(child, Is.Null);
        AssertEnvironmentWasCleared();
        await Task.CompletedTask;
    }

    [Test]
    public async Task ParentChannel_Configure_Should_RejectNullStartInfo()
    {
        await using var parent = new HostBootstrapParentChannel();

        Assert.That(() => parent.Configure(null!), Throws.ArgumentNullException);
    }

    private static IDisposable ApplyBootstrapEnvironment(ProcessStartInfo startInfo)
    {
        var previous = BootstrapEnvironmentVariables
            .Select(name => (Name: name, Value: Environment.GetEnvironmentVariable(name)))
            .ToArray();

        foreach (var name in BootstrapEnvironmentVariables)
            Environment.SetEnvironmentVariable(name, startInfo.Environment.TryGetValue(name, out var value) ? value : null);

        return new DelegateDisposable(() =>
        {
            foreach (var item in previous)
                Environment.SetEnvironmentVariable(item.Name, item.Value);
        });
    }

    private static void AssertEnvironmentWasCleared()
    {
        Assert.Multiple(() =>
        {
            foreach (var name in BootstrapEnvironmentVariables)
                Assert.That(Environment.GetEnvironmentVariable(name), Is.Null, name);
        });
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}