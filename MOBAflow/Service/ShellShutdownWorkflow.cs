// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

/// <summary>
/// The shell's shutdown workflow together with the runtime observers it releases: host-owned states that follow
/// the session and the runtime without a shell binding (counters, traffic monitor, Solution page, signal-box
/// projection). The composition root builds it, so its users do not depend on the shell.
/// </summary>
internal sealed class ShellShutdownWorkflow
{
    private readonly Func<Action, Task<bool>> _shellWorkflow;
    private readonly IReadOnlyList<IDisposable> _runtimeObservers;

    /// <summary>Creates the workflow.</summary>
    /// <param name="shellWorkflow">
    /// Saves unsaved changes, invokes the given release action once shutdown is confirmed and before the runtimes
    /// stop, and returns <see langword="false"/> when the operator cancelled.
    /// </param>
    /// <param name="runtimeObservers">The observers to dispose, in this order.</param>
    public ShellShutdownWorkflow(Func<Action, Task<bool>> shellWorkflow, IReadOnlyList<IDisposable> runtimeObservers)
    {
        ArgumentNullException.ThrowIfNull(shellWorkflow);
        ArgumentNullException.ThrowIfNull(runtimeObservers);
        _shellWorkflow = shellWorkflow;
        _runtimeObservers = runtimeObservers;
    }

    /// <summary>
    /// Runs the shell workflow; it releases the runtime observers once shutdown is confirmed and before the
    /// runtimes stop, and not at all when the operator cancelled.
    /// </summary>
    /// <returns><see langword="false"/> when the operator cancelled.</returns>
    public Task<bool> RunAsync() => _shellWorkflow(ReleaseRuntimeObservers);

    /// <summary>
    /// The counter, traffic monitor, Solution page and signal-box projection stop following the session and the
    /// runtime. Their disposal is idempotent, so a second release after a confirmed shutdown changes nothing.
    /// </summary>
    public void ReleaseRuntimeObservers()
    {
        foreach (var observer in _runtimeObservers)
        {
            observer.Dispose();
        }
    }
}
