// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;
using Domain;
using Interface;
using Microsoft.Extensions.Logging;

public interface ILocomotiveWhistleAutomationService : IDisposable
{
    IReadOnlyList<string> Validate(LocomotiveWhistleRule rule);

    void Activate(Project? project);

    Task HandleFeedbackAsync(int inPort, CancellationToken cancellationToken = default);
}

public sealed class LocomotiveWhistleAutomationService : ILocomotiveWhistleAutomationService
{
    private readonly object _gate = new();
    private readonly IEventBus _eventBus;
    private readonly ILocomotiveFunctionCommandGateway _gateway;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LocomotiveWhistleAutomationService> _logger;
    private readonly Dictionary<FunctionKey, FunctionExecution> _executions = [];
    private readonly Guid _subscriptionId;
    private CancellationTokenSource _lifetime = new();
    private Project? _project;
    private bool _disposed;

    public LocomotiveWhistleAutomationService(
        IEventBus eventBus,
        ILocomotiveFunctionCommandGateway gateway,
        ILogger<LocomotiveWhistleAutomationService> logger,
        TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _subscriptionId = _eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived);
    }

    public IReadOnlyList<string> Validate(LocomotiveWhistleRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var errors = new List<string>();
        if (rule.LocomotiveId == Guid.Empty)
            errors.Add("A locomotive is required.");
        if (rule.InPort <= 0)
            errors.Add("Feedback input must be positive.");
        if (rule.FunctionIndex is < 0 or > 31)
            errors.Add("Function must be between F0 and F31.");
        if (rule.DelayMilliseconds < 0)
            errors.Add("Delay must not be negative.");
        if (rule.ActiveDurationMilliseconds <= 0)
            errors.Add("Active duration must be positive.");
        return errors;
    }

    public void Activate(Project? project)
    {
        CancellationTokenSource previous;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            previous = _lifetime;
            _lifetime = new CancellationTokenSource();
            _project = project;
            _executions.Clear();
        }

        previous.Cancel();
        previous.Dispose();
    }

    public Task HandleFeedbackAsync(int inPort, CancellationToken cancellationToken = default)
    {
        if (inPort <= 0)
            throw new ArgumentOutOfRangeException(nameof(inPort));

        (LocomotiveWhistleRule Rule, int Address)[] matches;
        CancellationToken lifetimeToken;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            lifetimeToken = _lifetime.Token;
            matches = _project?.LocomotiveWhistleRules
                .Where(rule => rule.Enabled && rule.InPort == inPort)
                .Select(rule => (Rule: rule, Locomotive: _project.Locomotives.SingleOrDefault(locomotive => locomotive.Id == rule.LocomotiveId)))
                .Where(match => match.Locomotive?.DigitalAddress is not null)
                .Select(match => (match.Rule, Address: checked((int)match.Locomotive!.DigitalAddress!.Value)))
                .ToArray() ?? [];
        }

        if (matches.Length == 0)
        {
            _logger.LogWarning("Feedback input {InPort} has no enabled whistle rule with an addressable locomotive", inPort);
            return Task.CompletedTask;
        }

        return Task.WhenAll(matches.Select(match => TriggerAsync(match.Rule, match.Address, lifetimeToken, cancellationToken)));
    }

    private Task TriggerAsync(LocomotiveWhistleRule rule, int address, CancellationToken lifetimeToken, CancellationToken callerToken)
    {
        var errors = Validate(rule);
        if (errors.Count != 0)
        {
            _logger.LogWarning("Whistle rule {RuleId} was ignored: {Errors}", rule.Id, string.Join(" ", errors));
            return Task.CompletedTask;
        }

        FunctionExecution execution;
        lock (_gate)
        {
            var key = new FunctionKey(address, rule.FunctionIndex);
            if (!_executions.TryGetValue(key, out execution!))
            {
                execution = new FunctionExecution(key, rule.Id);
                _executions.Add(key, execution);
            }

            var now = _timeProvider.GetUtcNow();
            var requestedActivateAt = now.AddMilliseconds(rule.DelayMilliseconds);
            var requestedActiveUntil = requestedActivateAt.AddMilliseconds(rule.ActiveDurationMilliseconds);
            var previousActivateAt = execution.ActivateAt;
            var previousActiveUntil = execution.ActiveUntil;
            execution.ActivateAt = execution.IsFunctionOn
                ? execution.ActivateAt
                : requestedActivateAt;
            execution.ActiveUntil = execution.ActiveUntil > requestedActiveUntil
                ? execution.ActiveUntil
                : requestedActiveUntil;
            execution.LatestRuleId = rule.Id;
            if (execution.ActivateAt != previousActivateAt || execution.ActiveUntil != previousActiveUntil)
            {
                execution.Pulse.Cancel();
                execution.Pulse.Dispose();
                execution.Pulse = new CancellationTokenSource();
            }
            if (execution.Runner is null || execution.Runner.IsCompleted)
                execution.Runner = RunExecutionAsync(execution, lifetimeToken, callerToken);
            return execution.Runner;
        }
    }

    private async Task RunExecutionAsync(FunctionExecution execution, CancellationToken lifetimeToken, CancellationToken callerToken)
    {
        using var linkedLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken, callerToken);
        var functionOn = false;
        try
        {
            while (!linkedLifetime.IsCancellationRequested)
            {
                DateTimeOffset activateAt;
                CancellationToken pulse;
                lock (_gate)
                {
                    activateAt = execution.ActivateAt;
                    pulse = execution.Pulse.Token;
                }

                if (!functionOn)
                {
                    await DelayUntilAsync(activateAt, linkedLifetime.Token, pulse).ConfigureAwait(false);
                    lock (_gate)
                    {
                        if (activateAt != execution.ActivateAt)
                            continue;
                    }
                    if (!_gateway.IsConnected)
                    {
                        _logger.LogWarning("Whistle rule {RuleId} was skipped because the runtime is disconnected", execution.LatestRuleId);
                        return;
                    }
                    await _gateway.SetFunctionAsync(execution.Address, execution.FunctionIndex, true, linkedLifetime.Token).ConfigureAwait(false);
                    functionOn = true;
                    lock (_gate)
                    {
                        execution.IsFunctionOn = true;
                    }
                }

                DateTimeOffset activeUntil;
                lock (_gate)
                {
                    activeUntil = execution.ActiveUntil;
                    pulse = execution.Pulse.Token;
                }
                await DelayUntilAsync(activeUntil, linkedLifetime.Token, pulse).ConfigureAwait(false);
                lock (_gate)
                {
                    if (activeUntil != execution.ActiveUntil)
                        continue;
                }
                await _gateway.SetFunctionAsync(execution.Address, execution.FunctionIndex, false, CancellationToken.None).ConfigureAwait(false);
                functionOn = false;
                lock (_gate)
                {
                    execution.IsFunctionOn = false;
                }
                return;
            }
        }
        catch (OperationCanceledException) when (linkedLifetime.IsCancellationRequested)
        {
        }
        finally
        {
            if (functionOn && _gateway.IsConnected)
            {
                try
                {
                    await _gateway.SetFunctionAsync(execution.Address, execution.FunctionIndex, false, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to turn off locomotive function for whistle rule {RuleId}", execution.LatestRuleId);
                }
            }
            lock (_gate)
            {
                execution.IsFunctionOn = false;
                if (_executions.TryGetValue(execution.Key, out var current) && ReferenceEquals(current, execution))
                    _executions.Remove(execution.Key);
            }
        }
    }

    private async Task DelayUntilAsync(DateTimeOffset deadline, CancellationToken lifetime, CancellationToken pulse)
    {
        var delay = deadline - _timeProvider.GetUtcNow();
        if (delay <= TimeSpan.Zero)
            return;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, pulse);
        try
        {
            await Task.Delay(delay, _timeProvider, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (pulse.IsCancellationRequested && !lifetime.IsCancellationRequested)
        {
        }
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent feedback)
    {
        _ = ObserveAsync(HandleFeedbackAsync(feedback.InPort));
    }

    private async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Feedback-triggered locomotive function failed");
        }
    }

    public void Dispose()
    {
        CancellationTokenSource lifetime;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            lifetime = _lifetime;
            _project = null;
        }
        _eventBus.Unsubscribe(_subscriptionId);
        lifetime.Cancel();
        lifetime.Dispose();
    }

    private readonly record struct FunctionKey(int Address, int FunctionIndex);

    private sealed class FunctionExecution(FunctionKey key, Guid ruleId)
    {
        public FunctionKey Key { get; } = key;
        public Guid LatestRuleId { get; set; } = ruleId;
        public int Address => Key.Address;
        public int FunctionIndex => Key.FunctionIndex;
        public bool IsFunctionOn { get; set; }
        public DateTimeOffset ActivateAt { get; set; }
        public DateTimeOffset ActiveUntil { get; set; }
        public CancellationTokenSource Pulse { get; set; } = new();
        public Task? Runner { get; set; }
    }
}
