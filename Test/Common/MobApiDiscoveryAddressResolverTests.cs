// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;
using Moq;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Unit tests for MOBApi UDP discovery address selection.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed partial class MobApiDiscoveryAddressResolverTests
{
    private static readonly string[] ExpectedPrivateAddresses = ["10.20.30.40", "172.16.0.1", "192.168.10.2"];

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(65536)]
    [TestCase(int.MaxValue)]
    public void UdpResponder_InvalidPortIsRejectedWithoutOpeningSockets(int port)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MobApiUdpDiscoveryResponder(port));

        Assert.That(exception!.ParamName, Is.EqualTo("restApiPort"));
    }

    [TestCase(1)]
    [TestCase(5001)]
    [TestCase(65535)]
    public void UdpResponder_ValidPortIsAcceptedWithoutStartingListener(int port)
    {
        using var responder = new MobApiUdpDiscoveryResponder(port);

        Assert.DoesNotThrow(responder.Stop);
    }

    [Test]
    public void PrivateAddressSelection_FiltersAndDeduplicatesWithoutChangingInput()
    {
        var addresses = ParseAddresses(
            "::1|127.0.0.1|::ffff:10.20.30.40|198.51.100.1|10.20.30.40|172.15.255.255|172.16.0.1|172.32.0.1|192.168.10.2|10.20.30.40").ToList();
        var original = addresses.ToArray();

        var result = LanIpv4AddressHelper.SelectPrivateIpv4Addresses(addresses);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Select(address => address.ToString()), Is.EqualTo(ExpectedPrivateAddresses));
            Assert.That(addresses, Is.EqualTo(original));
        }
    }

    [Test]
    public void PrivateAddressSelection_EmptyInputProducesNoCandidates()
    {
        Assert.That(LanIpv4AddressHelper.SelectPrivateIpv4Addresses([]), Is.Empty);
    }

    [Test]
    public void PrivateAddressSelection_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => LanIpv4AddressHelper.SelectPrivateIpv4Addresses(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("addresses"));
    }

    [Test]
    public void InterfaceSelection_AcceptsActivePhysicalAdapters(
        [Values(NetworkInterfaceType.Ethernet, NetworkInterfaceType.Wireless80211)] NetworkInterfaceType type,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, type, "Physical adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.True);
    }

    [Test]
    public void InterfaceSelection_RejectsInactiveAdapters(
        [Values(OperationalStatus.Down, OperationalStatus.Dormant, OperationalStatus.Unknown)] OperationalStatus status,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(status, NetworkInterfaceType.Ethernet, "Physical adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_RejectsLoopbackOnEveryPlatform([Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Loopback, "Local adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_RejectsVpnNamesCaseInsensitively(
        [Values("Corporate VPN", "TUN0", "ppp-link", "vpN adapter")] string name, [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Ethernet, name);

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_AcceptsPhysicalAdapterWithoutName([Values(null, "")] string? name, [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Ethernet, name);

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.True);
    }

    [Test]
    public void InterfaceSelection_UsesAndroidFallbackForOtherActiveTypes(
        [Values(NetworkInterfaceType.Unknown, NetworkInterfaceType.Ppp, NetworkInterfaceType.Tunnel)] NetworkInterfaceType type,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, type, "Mobile adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.EqualTo(isAndroid));
    }

    [Test]
    public void InterfaceSelection_RejectsNullInterface()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => LanIpv4AddressHelper.IsDiscoveryCapableInterface(null!, false));

        Assert.That(exception!.ParamName, Is.EqualTo("ni"));
    }

    [TestCase("2001:db8::1|10.20.30.40|172.16.0.1|192.168.10.2", "192.168.10.2")]
    [TestCase("172.16.0.1|10.20.30.40", "10.20.30.40")]
    [TestCase("192.168.10.3|192.168.10.2", "192.168.10.3")]
    [TestCase("172.16.0.2|172.16.0.1", "172.16.0.2")]
    [TestCase("198.51.100.1|2001:db8::1|::ffff:192.168.10.2", "127.0.0.1")]
    [TestCase("", "127.0.0.1")]
    public void AddressSelection_PreservesRangePriorityAndFirstMatch(string addresses, string expected)
    {
        var result = MobApiDiscoveryAddressResolver.SelectLocalIpAddress(ParseAddresses(addresses));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void AddressSelection_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MobApiDiscoveryAddressResolver.SelectLocalIpAddress(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("localAddresses"));
    }

    [TestCase("10.20.30.50", "2001:db8::1|10.20.30.60", "10.20.30.60")]
    [TestCase("10.20.30.50", "10.20.40.60", "10.20.40.60")]
    [TestCase("10.20.30.50", "11.20.30.60|10.21.30.60", null)]
    [TestCase("192.168.20.1", "192.168.99.1|192.168.20.2", "192.168.99.1")]
    [TestCase("10.20.30.50", "2001:db8::1|::ffff:10.20.30.60", null)]
    [TestCase("2001:db8::1", "10.20.30.60", null)]
    [TestCase("10.20.30.50", "", null)]
    public void SubnetAddressSelection_UsesFirstIpv4MatchWithinExistingSubnetRules(string remote, string addresses, string? expected)
    {
        var result = MobApiDiscoveryAddressResolver.SelectLocalIpAddressInSubnet(IPAddress.Parse(remote), ParseAddresses(addresses));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void SubnetAddressSelection_RejectsNullArguments([Values] bool missingRemote)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            MobApiDiscoveryAddressResolver.SelectLocalIpAddressInSubnet(missingRemote ? null! : IPAddress.Loopback,
                missingRemote ? [] : null!));

        Assert.That(exception!.ParamName, Is.EqualTo(missingRemote ? "remoteIp" : "localAddresses"));
    }

    [Test]
    public void GetLocalIpAddressForRemote_ReturnsFallback_WhenRemoteIsNull()
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(null);
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GetLocalIpAddressInSubnet_ReturnsNull_ForNonIpv4Remote()
    {
        var remote = IPAddress.IPv6Loopback;
        Assert.That(MobApiDiscoveryAddressResolver.GetLocalIpAddressInSubnet(remote), Is.Null);
    }

    [Test]
    public void GetLocalIpAddress_PrefersPrivateRange_WhenAvailable()
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddress();
        Assert.That(IPAddress.TryParse(result, out var parsed), Is.True);
        Assert.That(parsed!.AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetwork));
    }

    [Test]
    public void DnsFailure_AdvertisementFallsBackToLoopback([Values] bool socketFailure)
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddress(() => throw CreateDnsFailure(socketFailure));

        Assert.That(result, Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void DnsFailure_SubnetLookupReturnsNoAddress([Values] bool socketFailure)
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressInSubnet(IPAddress.Parse("10.20.30.1"),
            () => throw CreateDnsFailure(socketFailure));

        Assert.That(result, Is.Null);
    }

    [TestCase(null)]
    [TestCase("10.20.30.1")]
    public void DnsFailure_RemoteLookupFallsBackToLoopback(string? remoteAddress)
    {
        var remote = remoteAddress == null ? null : new IPEndPoint(IPAddress.Parse(remoteAddress), 21106);
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(remote,
            () => throw new SocketException((int)SocketError.HostNotFound));

        Assert.That(result, Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void DnsLookup_RemoteSubnetMatchDoesNotPerformFallbackLookup()
    {
        var calls = 0;
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(new IPEndPoint(IPAddress.Parse("10.20.30.1"), 21106),
            () => ++calls == 1 ? [IPAddress.Parse("10.20.30.2"), IPAddress.Parse("192.168.1.2")] : throw new InvalidOperationException());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo("10.20.30.2"));
            Assert.That(calls, Is.EqualTo(1));
        }
    }

    [Test]
    public void DnsLookup_NoSubnetMatchUsesAdvertisementPreference()
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(new IPEndPoint(IPAddress.Parse("10.20.30.1"), 21106),
            () => [IPAddress.Parse("192.168.1.2"), IPAddress.Parse("172.16.1.2")]);

        Assert.That(result, Is.EqualTo("192.168.1.2"));
    }

    [Test]
    public void DnsLookup_FailedSubnetLookupCanRecoverDuringFallback()
    {
        var calls = 0;
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(new IPEndPoint(IPAddress.Parse("10.20.30.1"), 21106),
            () => ++calls == 1 ? throw new SocketException((int)SocketError.TryAgain) : [IPAddress.Parse("192.168.1.2")]);

        Assert.That(result, Is.EqualTo("192.168.1.2"));
    }

    [Test]
    public void DnsLookup_Ipv6SubnetRequestDoesNotQueryDns()
    {
        var calls = 0;
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressInSubnet(IPAddress.IPv6Loopback, () =>
        {
            calls++;
            return [IPAddress.Loopback];
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            Assert.That(calls, Is.Zero);
        }
    }

    [Test]
    public async Task UdpListener_PreCancelledRunInitializesAndClosesWithoutReceivingAsync()
    {
        using var transport = new DiscoveryTransportDouble();
        using var responder = CreateResponder(transport);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);

        await responder.RunAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.LocalEndPoint, Is.EqualTo(new IPEndPoint(IPAddress.Any, 21106)));
            Assert.That(transport.MulticastAddress, Is.EqualTo(IPAddress.Parse("239.255.42.99")));
            Assert.That(transport.ReceiveCalls, Is.Zero);
            Assert.That(transport.CloseCalls, Is.EqualTo(1));
        }
    }

    [TestCase("MOBAFLOW_DISCOVER", true)]
    [TestCase(" \r\nMOBAFLOW_DISCOVER\t ", true)]
    [TestCase("mobaflow_discover", false)]
    [TestCase("unknown", false)]
    [TestCase("", false)]
    public async Task UdpListener_RespondsOnlyToTrimmedDiscoveryRequestsAsync(string request, bool shouldRespond)
    {
        var remote = new IPEndPoint(IPAddress.Parse("10.20.30.40"), 43210);
        using var transport = new DiscoveryTransportDouble();
        transport.Receive = _ => transport.ReceiveCalls == 1
            ? ValueTask.FromResult(new UdpReceiveResult(Encoding.UTF8.GetBytes(request), remote))
            : ValueTask.FromException<UdpReceiveResult>(new OperationCanceledException());
        IPEndPoint? resolvedRemote = null;
        using var responder = new MobApiUdpDiscoveryResponder(5001, () => transport, endpoint =>
        {
            resolvedRemote = endpoint;
            return "192.168.10.2";
        });
        using var cancellation = new CancellationTokenSource();

        await responder.RunAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.Sent, Has.Count.EqualTo(shouldRespond ? 1 : 0));
            Assert.That(transport.ReceiveToken, Is.EqualTo(cancellation.Token));
            Assert.That(transport.CloseCalls, Is.EqualTo(1));
            Assert.That(resolvedRemote, Is.EqualTo(shouldRespond ? remote : null));
            if (shouldRespond)
            {
                Assert.That(Encoding.UTF8.GetString(transport.Sent[0].Payload), Is.EqualTo("MOBAFLOW_REST_API|192.168.10.2|5001"));
                Assert.That(transport.Sent[0].Remote, Is.EqualTo(remote));
                Assert.That(transport.Sent[0].Token, Is.EqualTo(cancellation.Token));
            }
        }
    }

    [TestCase(SocketError.Interrupted, false)]
    [TestCase(SocketError.Interrupted, true)]
    [TestCase(SocketError.ConnectionReset, false)]
    [TestCase(SocketError.ConnectionReset, true)]
    public async Task UdpListener_HandlesOnlyInterruptedSocketFailuresAsync(SocketError error, bool sending)
    {
        var expected = new SocketException((int)error);
        using var transport = new DiscoveryTransportDouble();
        transport.Receive = _ => sending ? ValueTask.FromResult(CreateDiscoveryRequest()) : ValueTask.FromException<UdpReceiveResult>(expected);
        transport.Send = _ => ValueTask.FromException<int>(expected);
        using var responder = CreateResponder(transport);

        if (error == SocketError.Interrupted)
        {
            await responder.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<SocketException>(async () =>
                await responder.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.That(exception, Is.SameAs(expected));
        }

        Assert.That(transport.CloseCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task UdpListener_HandlesCancellationDuringReceiveOrSendAsync([Values] bool sending)
    {
        using var transport = new DiscoveryTransportDouble();
        transport.Receive = _ => sending ? ValueTask.FromResult(CreateDiscoveryRequest())
            : ValueTask.FromException<UdpReceiveResult>(new OperationCanceledException());
        transport.Send = _ => ValueTask.FromException<int>(new OperationCanceledException());
        using var responder = CreateResponder(transport);

        await responder.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.That(transport.CloseCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task UdpListener_ClosesTransportWhenInitializationFailsAsync()
    {
        var expected = new SocketException((int)SocketError.AddressAlreadyInUse);
        using var transport = new DiscoveryTransportDouble { InitializationFailure = expected };
        using var responder = CreateResponder(transport);

        var exception = await Assert.ThrowsAsync<SocketException>(async () =>
            await responder.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.SameAs(expected));
            Assert.That(transport.ReceiveCalls, Is.Zero);
            Assert.That(transport.CloseCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UdpListener_ClosesTransportWhenAddressResolutionFailsAsync()
    {
        var expected = new InvalidOperationException("Expected address resolution failure.");
        using var transport = new DiscoveryTransportDouble { Receive = _ => ValueTask.FromResult(CreateDiscoveryRequest()) };
        using var responder = new MobApiUdpDiscoveryResponder(5001, () => transport, _ => throw expected);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await responder.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.SameAs(expected));
            Assert.That(transport.Sent, Is.Empty);
            Assert.That(transport.CloseCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UdpListener_DisposeClosesAndDisposesExactlyOnceAsync()
    {
        using var transport = new DiscoveryTransportDouble();
        using var responder = CreateResponder(transport);
        await responder.RunAsync(CancellationToken.None).ConfigureAwait(false);

        responder.Dispose();
        responder.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.CloseCalls, Is.EqualTo(2));
            Assert.That(transport.DisposeCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UdpListener_StartIsIdempotentAndStopCancelsOwnedReceiveAsync()
    {
        var receiving = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<UdpReceiveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transport = new DiscoveryTransportDouble
        {
            Receive = async token =>
            {
                receiving.TrySetResult(token);
                return await release.Task.WaitAsync(token).ConfigureAwait(false);
            }
        };
        var created = 0;
        using var responder = new MobApiUdpDiscoveryResponder(5001,
            () => { Interlocked.Increment(ref created); return transport; }, _ => "10.20.30.2");
        try
        {
            responder.Start();
            var token = await receiving.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            responder.Start();
            responder.Stop();
            await transport.ClosedTwice.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(created, Is.EqualTo(1));
                Assert.That(token.IsCancellationRequested, Is.True);
            }

            responder.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = token.WaitHandle);
        }
        finally
        {
            release.TrySetCanceled();
        }
    }

    private static Exception CreateDnsFailure(bool socketFailure) => socketFailure
        ? new SocketException((int)SocketError.HostNotFound)
        : new ArgumentException("Expected DNS failure.");

    [Test]
    public async Task UdpListener_DisposeCancelsRunningOwnedReceiveAndReleasesResourcesAsync()
    {
        var receiving = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<UdpReceiveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var transport = new DiscoveryTransportDouble
        {
            Receive = async token =>
            {
                receiving.TrySetResult(token);
                return await release.Task.WaitAsync(token).ConfigureAwait(false);
            }
        };
        using var responder = CreateResponder(transport);
        try
        {
            responder.Start();
            var token = await receiving.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            responder.Dispose();
            await transport.ClosedTwice.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            responder.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(token.IsCancellationRequested, Is.True);
                Assert.That(transport.CloseCalls, Is.EqualTo(2));
                Assert.That(transport.DisposeCalls, Is.EqualTo(1));
                Assert.Throws<ObjectDisposedException>(() => _ = token.WaitHandle);
            }
        }
        finally
        {
            release.TrySetCanceled();
        }
    }

    private static MobApiUdpDiscoveryResponder CreateResponder(DiscoveryTransportDouble transport) =>
        new(5001, () => transport, _ => "10.20.30.2");

    private static UdpReceiveResult CreateDiscoveryRequest() =>
        new(Encoding.UTF8.GetBytes("MOBAFLOW_DISCOVER"), new IPEndPoint(IPAddress.Parse("10.20.30.1"), 43210));

    private sealed partial class DiscoveryTransportDouble : IDiscoveryDatagramTransport
    {
        private int _closeCalls;

        public Func<CancellationToken, ValueTask<UdpReceiveResult>> Receive { get; set; } =
            _ => ValueTask.FromException<UdpReceiveResult>(new OperationCanceledException());
        public Func<CancellationToken, ValueTask<int>> Send { get; set; } = _ => ValueTask.FromResult(1);
        public Exception? InitializationFailure { get; init; }
        public IPEndPoint? LocalEndPoint { get; private set; }
        public IPAddress? MulticastAddress { get; private set; }
        public CancellationToken ReceiveToken { get; private set; }
        public int ReceiveCalls { get; private set; }
        public int CloseCalls => Volatile.Read(ref _closeCalls);
        public int DisposeCalls { get; private set; }
        public List<(byte[] Payload, IPEndPoint Remote, CancellationToken Token)> Sent { get; } = [];
        public TaskCompletionSource ClosedTwice { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void BindAndJoinMulticast(IPEndPoint localEndPoint, IPAddress multicastAddress)
        {
            LocalEndPoint = localEndPoint;
            MulticastAddress = multicastAddress;
            if (InitializationFailure != null)
                throw InitializationFailure;
        }

        public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            ReceiveCalls++;
            if (ReceiveCalls > 3)
                throw new InvalidOperationException("The listener did not stop after the supplied test sequence.");
            ReceiveToken = cancellationToken;
            return Receive(cancellationToken);
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> payload, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            Sent.Add((payload.ToArray(), remoteEndPoint, cancellationToken));
            return Send(cancellationToken);
        }

        public void Close()
        {
            if (Interlocked.Increment(ref _closeCalls) >= 2)
                ClosedTwice.TrySetResult();
        }

        public void Dispose() => DisposeCalls++;
    }

    private static IEnumerable<IPAddress> ParseAddresses(string input) =>
        input.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse);

    private static NetworkInterface CreateInterface(OperationalStatus status, NetworkInterfaceType type, string? name)
    {
        var network = new Mock<NetworkInterface>();
        network.SetupGet(adapter => adapter.OperationalStatus).Returns(status);
        network.SetupGet(adapter => adapter.NetworkInterfaceType).Returns(type);
        network.SetupGet(adapter => adapter.Name).Returns(name!);
        return network.Object;
    }
}
