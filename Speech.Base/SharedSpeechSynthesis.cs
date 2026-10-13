// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>One synthesis per key, with a separate token owned by all active waiters.</summary>
internal sealed class SharedSpeechSynthesis(CancellationToken shutdownToken)
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Flight> _flights = new(StringComparer.Ordinal);
    private readonly HashSet<Flight> _running = [];

    internal async Task<SpeechResult> RunAsync(string key, Func<CancellationToken, Task<SpeechResult>> synthesize, CancellationToken callerToken)
    {
        Flight flight;
        lock (_gate)
        {
            callerToken.ThrowIfCancellationRequested();
            shutdownToken.ThrowIfCancellationRequested();
            if (!_flights.TryGetValue(key, out flight!))
            {
                flight = new Flight(shutdownToken);
                _flights.Add(key, flight);
                _running.Add(flight);
                var work = Task.Run(() => SynthesizeAsync(flight, synthesize), CancellationToken.None);
                // Waiters receive failures through Completion; draining also observes the worker's rethrown fault.
                flight.Work = work.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            flight.Waiters++;
        }
        try { return await flight.Completion.Task.WaitAsync(callerToken).ConfigureAwait(false); }
        finally { await ReleaseAsync(key, flight).ConfigureAwait(false); }
    }

    internal Task DrainAsync()
    {
        lock (_gate) { return Task.WhenAll(_running.Select(flight => flight.Work)); }
    }

    private async Task SynthesizeAsync(Flight flight, Func<CancellationToken, Task<SpeechResult>> synthesize)
    {
        try
        {
            flight.Token.ThrowIfCancellationRequested();
            var result = await synthesize(flight.Token).ConfigureAwait(false);
            flight.Token.ThrowIfCancellationRequested();
            flight.Completion.TrySetResult(result);
        }
        catch (OperationCanceledException) when (flight.Token.IsCancellationRequested)
        {
            flight.Completion.TrySetCanceled(flight.Token);
        }
        catch (Exception ex)
        {
            flight.Completion.TrySetException(ex);
            // All waiters may already have cancelled; still observe the shared fault.
            _ = flight.Completion.Task.Exception;
            throw;
        }
        finally
        {
            flight.Dispose();
            lock (_gate) { _running.Remove(flight); }
        }
    }

    private async Task ReleaseAsync(string key, Flight flight)
    {
        var cancel = false;
        lock (_gate)
        {
            flight.Waiters--;
            if (flight.Waiters == 0)
            {
                if (_flights.GetValueOrDefault(key) == flight) _flights.Remove(key);
                cancel = !flight.Completion.Task.IsCompleted;
            }
        }
        if (cancel) await flight.CancelAsync().ConfigureAwait(false);
    }

    private sealed class Flight : IDisposable
    {
        private readonly Lock _gate = new();
        private readonly CancellationTokenSource _cancellation;
        private bool _disposed;

        internal Flight(CancellationToken shutdownToken)
        {
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            Token = _cancellation.Token;
        }

        internal CancellationToken Token { get; }
        internal TaskCompletionSource<SpeechResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task Work { get; set; } = Task.CompletedTask;
        internal int Waiters { get; set; }

        internal Task CancelAsync()
        {
            lock (_gate) { return _disposed ? Task.CompletedTask : _cancellation.CancelAsync(); }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                _cancellation.Dispose();
            }
        }
    }
}
