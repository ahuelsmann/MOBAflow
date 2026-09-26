// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

using System.Threading.Channels;

/// <summary>
/// Fallback command queue when SignalR host forwarding is unavailable.
/// </summary>
public interface IRuntimeCommandQueue
{
    /// <summary>
    /// Adds a command without waiting.
    /// </summary>
    /// <returns><see langword="false"/> when the queue is full; the command is not added.</returns>
    bool TryEnqueue(RuntimeCommandEnvelope command);

    bool TryDequeue(out RuntimeCommandEnvelope? command);
}

/// <summary>
/// Bounded first-in-first-out queue; a full queue rejects new commands instead of growing.
/// </summary>
public sealed class RuntimeCommandQueue : IRuntimeCommandQueue
{
    /// <summary>Maximum number of commands waiting for the host.</summary>
    public const int DefaultCapacity = 128;

    private readonly Channel<RuntimeCommandEnvelope> _channel;

    public RuntimeCommandQueue()
        : this(DefaultCapacity)
    {
    }

    internal RuntimeCommandQueue(int capacity)
    {
        _channel = Channel.CreateBounded<RuntimeCommandEnvelope>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public bool TryEnqueue(RuntimeCommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _channel.Writer.TryWrite(command);
    }

    public bool TryDequeue(out RuntimeCommandEnvelope? command)
    {
        return _channel.Reader.TryRead(out command);
    }
}
