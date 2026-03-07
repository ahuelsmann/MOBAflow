// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Extension;

/// <summary>
/// Tests for TaskExtensions.SafeFireAndForget (fire-and-forget with optional exception callback).
/// </summary>
[TestFixture]
internal class TaskExtensionsTests
{
    [Test]
    public void SafeFireAndForget_CompletedTask_DoesNotThrow()
    {
        var t = Task.CompletedTask;
        Assert.DoesNotThrow(() => t.SafeFireAndForget());
    }

    [Test]
    public void SafeFireAndForget_FaultedTask_InvokesOnException()
    {
        var ex = new InvalidOperationException("test");
        var t = Task.FromException(ex);
        Exception? captured = null;
        t.SafeFireAndForget(e => captured = e);

        Thread.Sleep(100);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Message, Is.EqualTo("test"));
    }

    [Test]
    public void SafeFireAndForget_FaultedTask_WithNullCallback_DoesNotThrow()
    {
        var t = Task.FromException(new InvalidOperationException("test"));
        Assert.DoesNotThrow(() => t.SafeFireAndForget(null));

        Thread.Sleep(100);
    }

    [Test]
    public void SafeFireAndForget_SuccessfulAsyncTask_CompletesWithoutCallback()
    {
        var completed = false;
        var t = Task.Run(async () =>
        {
            await Task.Delay(10);
            completed = true;
        });
        t.SafeFireAndForget();

        Thread.Sleep(100);
        Assert.That(completed, Is.True);
    }
}
