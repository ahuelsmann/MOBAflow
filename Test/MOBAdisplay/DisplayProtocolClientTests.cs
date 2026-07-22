// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

[TestFixture]
[Category("Unit")]
internal sealed class DisplayProtocolClientTests
{
    private static readonly DisplayRequestOptions SingleAttempt =
        new(1, TimeSpan.FromSeconds(10), TimeSpan.Zero);

    [Test]
    public async Task SendRequestAsync_Should_NegotiateCapabilities()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(CreateHello(), options: SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(outcome.RequestId, Is.Not.Zero);
            Assert.That(outcome.AttemptCount, Is.EqualTo(1));
            Assert.That(outcome.Response, Is.TypeOf<CapabilitiesResponsePayload>());
            Assert.That(((CapabilitiesResponsePayload)outcome.Response!).SessionId, Is.EqualTo(endpoint.SessionId));
            Assert.That(endpoint.ReceivedPackets[0].Header.Flags, Is.EqualTo(DisplayProtocolFlags.AcknowledgementRequired));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_CorrelateConcurrentOutOfOrderResponses()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        endpoint.HoldNextResponse();

        // Act
        var healthTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);
        var clearTask = client.SendRequestAsync(
            new ClearPayload(0),
            endpoint.SessionId,
            options: SingleAttempt);
        endpoint.ReleaseHeldResponses();
        var outcomes = await Task.WhenAll(healthTask, clearTask);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcomes.All(outcome => outcome.IsSuccessful), Is.True);
            Assert.That(outcomes.Select(outcome => outcome.RequestId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(outcomes[0].Response, Is.TypeOf<HealthResponsePayload>());
            Assert.That(outcomes[1].Response, Is.TypeOf<ResultPayload>());
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_RetryWithSameIdentifier_AfterTimeout()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.DropNextResponse();
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var options = new DisplayRequestOptions(2, TimeSpan.FromSeconds(10), TimeSpan.Zero);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: options);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(options.ResponseTimeout);
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
            Assert.That(endpoint.ReceivedPackets, Has.Count.EqualTo(2));
            Assert.That(endpoint.ReceivedPackets.Select(packet => packet.Header.RequestId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(endpoint.ReceivedPackets[1].Header.Flags.HasFlag(DisplayProtocolFlags.Retry), Is.True);
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_ReturnTimeout_When_RetryBudgetIsExhausted()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.DropNextResponse();
        endpoint.DropNextResponse();
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var options = new DisplayRequestOptions(2, TimeSpan.FromSeconds(10), TimeSpan.Zero);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: options);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(options.ResponseTimeout);
        await WaitForPacketCountAsync(endpoint, 2);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(options.ResponseTimeout);
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.TimedOut));
            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
            Assert.That(endpoint.ReceivedPackets, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_BoundDeviceRetryDelay()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.RejectNextRequest(
            DisplayResultCode.Busy,
            DisplayResultFlags.Retryable,
            retryAfterMilliseconds: 30_000);
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var options = new DisplayRequestOptions(2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: options);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(options.MaximumRetryDelay);
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(2));
            Assert.That(outcome.Response, Is.TypeOf<HealthResponsePayload>());
            Assert.That(endpoint.ReceivedPackets, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_AcceptDeterministicallyDelayedResponse()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.DelayNextResponse(TimeSpan.FromSeconds(2));
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(outcome.AttemptCount, Is.EqualTo(1));
            Assert.That(outcome.Response, Is.TypeOf<HealthResponsePayload>());
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_ReturnCancelled_WithoutFurtherRetry()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.DropNextResponse();
        using var client = new DisplayProtocolClient(endpoint);
        using var cancellation = new CancellationTokenSource();
        var options = new DisplayRequestOptions(3, TimeSpan.FromMinutes(1), TimeSpan.Zero);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: options,
            cancellationToken: cancellation.Token);
        cancellation.Cancel();
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.Cancelled));
            Assert.That(outcome.AttemptCount, Is.EqualTo(1));
            Assert.That(endpoint.ReceivedPackets, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_TranslateTransportException()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.FailNextSend();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.TransportFailure));
            Assert.That(outcome.Diagnostic, Does.Contain(nameof(IOException)));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_ReportDuplicateResponseAnomaly()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.DuplicateNextResponse();
        using var client = new DisplayProtocolClient(endpoint);
        var anomalies = new List<DisplayTransportAnomalyEventArgs>();
        client.TransportAnomaly += (_, eventArgs) => anomalies.Add(eventArgs);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(
                anomalies.Select(anomaly => anomaly.Anomaly),
                Does.Contain(DisplayTransportAnomaly.DuplicateResponse));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_ReportLateResponseAsUnmatched()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.HoldNextResponse();
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var anomalies = new List<DisplayTransportAnomalyEventArgs>();
        client.TransportAnomaly += (_, eventArgs) => anomalies.Add(eventArgs);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(SingleAttempt.ResponseTimeout);
        var outcome = await requestTask;
        endpoint.ReleaseHeldResponses();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.TimedOut));
            Assert.That(
                anomalies.Select(anomaly => anomaly.Anomaly),
                Does.Contain(DisplayTransportAnomaly.UnmatchedResponse));
        });
    }

    [Test]
    public async Task SendRequestAsync_Should_RejectWrongFrameCorrelation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.UseWrongFrameIdForNextResponse();
        using var client = new DisplayProtocolClient(endpoint);
        var frame = new byte[] { 0xF8, 0x00 };
        var begin = new BeginFramePayload(
            1,
            1,
            DisplayPixelFormat.Rgb565BigEndian,
            DisplayRotation.Degrees0,
            2,
            DisplayPacketCodec.ComputeCrc32(frame));

        // Act
        var outcome = await client.SendRequestAsync(
            begin,
            endpoint.SessionId,
            frameId: 7,
            options: SingleAttempt);

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.WrongFrameId));
    }

    [Test]
    public async Task SendRequestAsync_Should_RejectWrongSessionCorrelation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.UseWrongSessionIdForNextResponse();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.WrongSessionId));
    }

    [Test]
    public async Task SendRequestAsync_Should_RejectResponseWithoutResponseFlag()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.OmitResponseFlagForNextResponse();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.MissingResponseFlag));
    }

    [Test]
    public async Task SendRequestAsync_Should_RejectUnexpectedResponseType()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.UseUnexpectedMessageTypeForNextResponse();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.UnexpectedMessageType));
    }

    [Test]
    public async Task SendRequestAsync_Should_RejectInvalidResponsePayload()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.InvalidateNextResponsePayload();
        using var client = new DisplayProtocolClient(endpoint);

        // Act
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.InvalidPayload));
    }

    [Test]
    public async Task SendRequestAsync_Should_ReportInvalidDatagram_ThenTimeout()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.CorruptNextResponse();
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var anomalies = new List<DisplayTransportAnomalyEventArgs>();
        client.TransportAnomaly += (_, eventArgs) => anomalies.Add(eventArgs);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);
        await WaitForTimerAsync(timeProvider);
        timeProvider.Advance(SingleAttempt.ResponseTimeout);
        var outcome = await requestTask;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.TimedOut));
            Assert.That(
                anomalies.Select(anomaly => anomaly.Anomaly),
                Does.Contain(DisplayTransportAnomaly.InvalidDatagram));
        });
    }

    [Test]
    public async Task Dispose_Should_CompleteActiveRequestAsDisposed()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.DropNextResponse();
        var client = new DisplayProtocolClient(endpoint);

        // Act
        var requestTask = client.SendRequestAsync(
            new HealthRequestPayload(),
            endpoint.SessionId,
            options: SingleAttempt);
        client.Dispose();
        var outcome = await requestTask;

        // Assert
        Assert.That(outcome.Failure, Is.EqualTo(DisplayRequestFailure.ClientDisposed));
    }

    [Test]
    public void SendRequestAsync_Should_RejectEnvelopeIdentifiers_When_MessageScopeDoesNotMatch()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);

        // Act and assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendRequestAsync(CreateHello(), sessionId: endpoint.SessionId));
    }

    private static HelloRequestPayload CreateHello() =>
        new(
            DisplayProtocol.CurrentVersion,
            DisplayProtocol.CurrentVersion,
            DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH);

    private static async Task WaitForTimerAsync(ManualTimeProvider timeProvider)
    {
        for (var attempt = 0; attempt < 100 && timeProvider.ScheduledTimerCount == 0; attempt++)
        {
            await Task.Yield();
        }

        Assert.That(timeProvider.ScheduledTimerCount, Is.GreaterThan(0));
    }

    private static async Task WaitForPacketCountAsync(FakeDisplayEndpoint endpoint, int expectedCount)
    {
        for (var attempt = 0; attempt < 100 && endpoint.ReceivedPacketCount < expectedCount; attempt++)
        {
            await Task.Yield();
        }

        Assert.That(endpoint.ReceivedPacketCount, Is.GreaterThanOrEqualTo(expectedCount));
    }
}