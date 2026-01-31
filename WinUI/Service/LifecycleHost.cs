// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.WinUI.Service;

using Microsoft.UI.Xaml;
using SharedUI.Shell;

/// <summary>
/// Manages application lifecycle events for WinUI.
/// </summary>
public sealed class LifecycleHost : ILifecycleHost
{
    private readonly Application _application;

    public LifecycleHost(Application application)
    {
        _application = application;
    }

    /// <inheritdoc />
    public LifecycleState CurrentState { get; private set; } = LifecycleState.NotStarted;

    /// <inheritdoc />
    public event EventHandler? Starting;

    /// <inheritdoc />
    public event EventHandler? Started;

    /// <inheritdoc />
    public event EventHandler? Suspending;

    /// <inheritdoc />
    public event EventHandler? Resuming;

    /// <inheritdoc />
    public event EventHandler? Stopping;

    /// <inheritdoc />
    public event EventHandler? Stopped;

    /// <summary>
    /// Called by App.xaml.cs during startup.
    /// </summary>
    public void OnStarting()
    {
        CurrentState = LifecycleState.Starting;
        Starting?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by App.xaml.cs after startup completes.
    /// </summary>
    public void OnStarted()
    {
        CurrentState = LifecycleState.Running;
        Started?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when application suspends.
    /// </summary>
    public void OnSuspending()
    {
        CurrentState = LifecycleState.Suspended;
        Suspending?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when application resumes.
    /// </summary>
    public void OnResuming()
    {
        CurrentState = LifecycleState.Running;
        Resuming?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when application is stopping.
    /// </summary>
    public void OnStopping()
    {
        CurrentState = LifecycleState.Stopping;
        Stopping?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when application has stopped.
    /// </summary>
    public void OnStopped()
    {
        CurrentState = LifecycleState.Stopped;
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void RequestShutdown(int exitCode = 0)
    {
        OnStopping();
        _application.Exit();
        OnStopped();
    }
}
