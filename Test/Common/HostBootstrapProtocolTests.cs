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
    public async Task ParentChannel_Should_RejectExchangeBeforeProcessStart()
    {
        await using var parent = new HostBootstrapParentChannel();
        using var notStarted = new Process();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Assert.That(
            async () => await parent.ExchangeAsync(notStarted, timeout.Token).ConfigureAwait(false),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ParentChannel_Should_HonorCancellation()
    {
        await using var parent = new HostBootstrapParentChannel();
        using var currentProcess = Process.GetCurrentProcess();

        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync().ConfigureAwait(false);
        Assert.That(
            async () => await parent.ExchangeAsync(currentProcess, canceled.Token).ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task ParentAndChildChannels_Should_ExchangeBootstrapMaterialWithoutInheritedHandles()
    {
        var parent = new HostBootstrapParentChannel();
        await using var parentLifetime = parent.ConfigureAwait(false);
        var startInfo = new ProcessStartInfo();
        parent.Configure(startInfo);
        using var environment = ApplyBootstrapEnvironment(startInfo);
        var child = HostBootstrapChildChannel.TryOpenFromEnvironment();
        Assert.That(child, Is.Not.Null);
        var openedChild = child!;
        await using var childLifetime = openedChild.ConfigureAwait(false);
        using var currentProcess = Process.GetCurrentProcess();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var childSide = Task.Run(async () =>
        {
            var childRequest = await openedChild.ReadRequestAsync(timeout.Token).ConfigureAwait(false);
            await openedChild.WriteResponseAsync(new HostBootstrapPipeResponse("fingerprint", "instance"), timeout.Token)
                .ConfigureAwait(false);
            return childRequest;
        });
        var (secret, response) = await parent.ExchangeAsync(currentProcess, timeout.Token).ConfigureAwait(false);
        var request = await childSide.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Secret, Is.EqualTo(secret));
            Assert.That(request.ParentProcessId, Is.EqualTo(Environment.ProcessId));
            Assert.That(response, Is.EqualTo(new HostBootstrapPipeResponse("fingerprint", "instance")));
        }
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task ParentChannel_Should_FailFast_WhenChildProcessExitsBeforeConnecting()
    {
        var parent = new HostBootstrapParentChannel();
        await using var parentLifetime = parent.ConfigureAwait(false);
        using var exitedProcess = Process.Start(new ProcessStartInfo("dotnet", "--version")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!;
        await exitedProcess.WaitForExitAsync().ConfigureAwait(false);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        Assert.That(
            async () => await parent.ExchangeAsync(exitedProcess, timeout.Token).ConfigureAwait(false),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("exited with code"));
        Assert.That(timeout.IsCancellationRequested, Is.False);
    }

    [TestCase(100, null, 100, true)]
    [TestCase(200, 100, 100, true)]
    [TestCase(200, 300, 100, false)]
    [TestCase(200, null, 100, false)]
    [TestCase(0, 0, 0, false)]
    public void PipeClientVerifier_Should_AcceptOnlyLaunchedProcessOrItsDirectChild(
        int clientProcessId,
        int? clientParentProcessId,
        int expectedProcessId,
        bool expected)
    {
        Assert.That(
            HostBootstrapPipeClientVerifier.IsExpectedClient(clientProcessId, clientParentProcessId, expectedProcessId),
            Is.EqualTo(expected));
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

    [Test]
    public async Task ParentChannel_Configure_Should_UseProcessIndependentPipeNames()
    {
        var parent = new HostBootstrapParentChannel();
        await using var parentLifetime = parent.ConfigureAwait(false);
        var startInfo = new ProcessStartInfo();

        parent.Configure(startInfo);

        var requestPipe = startInfo.Environment[HostBootstrapProtocol.RequestPipeEnvironmentVariable];
        var responsePipe = startInfo.Environment[HostBootstrapProtocol.ResponsePipeEnvironmentVariable];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(requestPipe, Does.StartWith("mobaflow-host-bootstrap-request-"));
            Assert.That(responsePipe, Does.StartWith("mobaflow-host-bootstrap-response-"));
            Assert.That(requestPipe, Is.Not.EqualTo(responsePipe));
            Assert.That(long.TryParse(requestPipe, out _), Is.False);
            Assert.That(long.TryParse(responsePipe, out _), Is.False);
        }
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
