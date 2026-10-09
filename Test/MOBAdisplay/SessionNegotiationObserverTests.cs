// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

[TestFixture]
[Category("Integration")]
internal sealed class SessionNegotiationObserverTests
{
    [Test]
    public async Task FramesOnTheFirstSession_AreNoRenegotiation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionNegotiationObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        Assert.That(observer.Renegotiations, Is.Zero);
    }

    [Test]
    public async Task FrameRejectedWithWrongSession_CountsTheRenegotiation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionNegotiationObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.Reboot();

        // Act
        var exception = await CaptureExceptionAsync<DisplayProtocolOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ResultCode, Is.EqualTo(DisplayResultCode.WrongSession));
            Assert.That(observer.Renegotiations, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task SessionLossFoundByTheAbort_CountsTheRenegotiation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionNegotiationObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.RejectNextRequest(DisplayResultCode.ChecksumMismatch);
        endpoint.RejectNextRequest(DisplayResultCode.WrongSession);

        // Act
        var exception = await CaptureExceptionAsync<DisplayProtocolOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ResultCode, Is.EqualTo(DisplayResultCode.ChecksumMismatch));
            Assert.That(observer.Renegotiations, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ResponseWithAnotherSessionId_IsNoRenegotiation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionNegotiationObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.UseWrongSessionIdForNextResponse();

        // Act
        var exception = await CaptureExceptionAsync<DisplayProtocolOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.RequestFailure, Is.EqualTo(DisplayRequestFailure.WrongSessionId));
            Assert.That(observer.Renegotiations, Is.Zero);
        }
    }

    private static async Task<TException> CaptureExceptionAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new AssertionException($"Expected {typeof(TException).Name}.");
    }
}
