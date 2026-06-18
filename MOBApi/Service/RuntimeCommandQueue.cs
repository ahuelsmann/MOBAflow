// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

using System.Collections.Concurrent;

/// <summary>
/// Fallback command queue when SignalR host forwarding is unavailable.
/// </summary>
public interface IRuntimeCommandQueue
{
    void Enqueue(RuntimeCommandEnvelope command);

    bool TryDequeue(out RuntimeCommandEnvelope? command);
}

public sealed class RuntimeCommandQueue : IRuntimeCommandQueue
{
    private readonly ConcurrentQueue<RuntimeCommandEnvelope> _queue = new();

    public void Enqueue(RuntimeCommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _queue.Enqueue(command);
    }

    public bool TryDequeue(out RuntimeCommandEnvelope? command)
    {
        return _queue.TryDequeue(out command);
    }
}
