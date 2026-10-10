// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

[TestFixture]
[Category("Integration")]
internal sealed class SessionLossObserverTests
{
    [Test]
    public async Task FramesOnTheFirstSession_LoseNoSession()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionLossObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        Assert.That(observer.SessionLosses, Is.Zero);
    }

    [Test]
    public async Task FrameRejectedWithWrongSession_CountsOneSessionLoss()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionLossObserver(endpoint);
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
            Assert.That(observer.SessionLosses, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task SessionRejectedOnTheLastFrame_IsCountedWithoutRenegotiation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionLossObserver(endpoint);
        using var client = new DisplayProtocolClient(observer);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.Reboot();

        // Act
        await CaptureExceptionAsync<DisplayProtocolOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);

        // Assert
        Assert.That(observer.SessionLosses, Is.EqualTo(1));
    }

    [Test]
    public async Task SessionLossFoundByTheAbort_IsCounted()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionLossObserver(endpoint);
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
            Assert.That(observer.SessionLosses, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ResponseWithAnotherSessionId_IsNoSessionLoss()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var observer = new SessionLossObserver(endpoint);
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
            Assert.That(observer.SessionLosses, Is.Zero);
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
