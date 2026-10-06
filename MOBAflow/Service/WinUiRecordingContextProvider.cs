// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Recording;

using SharedUI.Interface;

using System.Reflection;

/// <summary>
/// Supplies the WinUI application version and selected project identity to new recording sessions.
/// </summary>
internal sealed class WinUiRecordingContextProvider : IRecordingContextProvider
{
    private readonly ISolutionSession _solutionSession;

    public WinUiRecordingContextProvider(ISolutionSession solutionSession)
    {
        _solutionSession = solutionSession ?? throw new ArgumentNullException(nameof(solutionSession));
    }

    /// <inheritdoc />
    public string SourceApplicationVersion { get; } = GetApplicationVersion();

    /// <inheritdoc />
    public RecordingProjectIdentity? GetProjectIdentity()
    {
        var project = _solutionSession.SelectedProject?.Model;
        return project is null ? null : new RecordingProjectIdentity(project.Id, project.Name);
    }

    private static string GetApplicationVersion()
    {
        var assembly = typeof(App).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var buildMetadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return buildMetadataIndex > 0
                ? informationalVersion[..buildMetadataIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}