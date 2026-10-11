// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

/// <summary>
/// Creates solution sessions for ViewModel tests with the same file access and dispatcher as the ViewModel.
/// </summary>
internal static class TestSolutionSessions
{
    /// <summary>
    /// Creates a session whose project runtime updates reach <paramref name="runtimeConnection"/>, the runtime the
    /// ViewModel under test also uses.
    /// </summary>
    public static SolutionSession Create(
        Solution solution,
        IUiDispatcher uiDispatcher,
        IConnectionRuntime runtimeConnection,
        IIoService? ioService = null) =>
        new(
            solution,
            ioService ?? new NullIoService(),
            uiDispatcher,
            new SingleRuntimeProjectHost(runtimeConnection),
            NullLogger<SolutionSession>.Instance);

    /// <summary>
    /// A project runtime host for ViewModel tests that routes snapshot updates to one runtime.
    /// </summary>
    private sealed class SingleRuntimeProjectHost(IConnectionRuntime runtime) : IProjectRuntimeHost
    {
        public IReadOnlyCollection<Guid> ConnectedProjectIds => [];

        public Task LoadAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(Project project, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReplaceAsync(Project project, CancellationToken cancellationToken = default) =>
            runtime.UpdateProjectAsync(project, cancellationToken);

        public Task UpdateAsync(Project project, CancellationToken cancellationToken = default) =>
            runtime.UpdateProjectAsync(project, cancellationToken);

        public void SelectProject(Guid? projectId)
        {
            // The ViewModel tests use one runtime for every project.
        }
    }
}
