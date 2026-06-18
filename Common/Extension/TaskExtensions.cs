// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Extension;

/// <summary>
/// Extension methods for safer async Task handling.
/// Prevents unobserved exceptions from fire-and-forget <c>_ = SomeAsync()</c> patterns.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Observes a task without awaiting and routes faults to the provided callback.
    /// Use this for fire-and-forget calls in event handlers where no await chain is available.
    /// </summary>
    /// <param name="task">The task to observe.</param>
    /// <param name="onException">Callback invoked when the task faults. If null, exceptions are silently ignored.</param>
    public static void Observe(this Task task, Action<Exception>? onException = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        task.ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    onException?.Invoke(t.Exception.GetBaseException());
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Backward-compatible alias for <see cref="Observe(Task,Action{Exception}?)"/>.
    /// Use instead of <c>_ = SomeAsync()</c> to ensure exceptions are never silently swallowed.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="onException">Callback invoked when the task faults. If null, exceptions are silently ignored.</param>
    public static void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        task.Observe(onException);
    }
}