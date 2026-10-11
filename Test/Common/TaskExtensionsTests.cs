// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Extension;

/// <summary>
/// Tests for TaskExtensions.SafeFireAndForget (fire-and-forget with optional exception callback).
/// </summary>
[TestFixture]
[Category("Unit")]
internal class TaskExtensionsTests
{
    [Test]
    public void Observe_NullTask_ThrowsArgumentNullExceptionWithParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => TaskExtensions.Observe(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("task"));
    }

    [Test]
    public void SafeFireAndForget_NullTask_ThrowsArgumentNullExceptionWithParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => TaskExtensions.SafeFireAndForget(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("task"));
    }

    [Test]
    public void SafeFireAndForget_CompletedTask_DoesNotThrow()
    {
        var t = Task.CompletedTask;
        Assert.DoesNotThrow(() => t.SafeFireAndForget());
    }

    [Test]
    public async Task SafeFireAndForget_FaultedTask_InvokesOnException()
    {
        var ex = new InvalidOperationException("test");
        var t = Task.FromException(ex);
        var completion = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        t.SafeFireAndForget(e => completion.TrySetResult(e));

        var captured = await completion.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        Assert.That(captured, Is.SameAs(ex));
    }

    [Test]
    public void SafeFireAndForget_FaultedTask_WithNullCallback_DoesNotThrow()
    {
        var t = Task.FromException(new InvalidOperationException("test"));
        Assert.DoesNotThrow(() => t.SafeFireAndForget());
    }

    [Test]
    public async Task SafeFireAndForget_SuccessfulAsyncTask_CompletesWithoutCallback()
    {
        var completed = false;
        var t = Task.Run(async () =>
        {
            await Task.Delay(10);
            completed = true;
        });
        t.SafeFireAndForget();

        await t.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        Assert.That(completed, Is.True);
    }
}