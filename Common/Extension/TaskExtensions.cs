// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Extension;

/// <summary>
/// Extension methods for safer async Task handling.
/// Prevents unobserved exceptions from fire-and-forget <c>_ = SomeAsync()</c> patterns.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Safely executes a Task without awaiting, routing exceptions to the provided callback.
    /// Use instead of <c>_ = SomeAsync()</c> to ensure exceptions are never silently swallowed.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="onException">Callback invoked when the task faults. If null, exceptions are silently ignored.</param>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
        }
    }
}
