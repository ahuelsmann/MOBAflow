// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Resolves paths to the standalone <c>MOBApi</c> host executable for WinUI process launch and MSBuild copy targets.
/// Centralizes naming conventions so build output layout and runtime lookup stay aligned.
/// </summary>
public static class MobaApiPathResolver
{
    public const string ProjectFolderName = "MOBApi";
    public const string AssemblyFileName = "MOBApi.dll";
    public const string ProjectFileName = "MOBApi.csproj";
    public const string DefaultTargetFramework = "net10.0";

    /// <summary>Build configurations checked when resolving a repo-local MOBApi output folder.</summary>
    public static readonly string[] BuildConfigurations = ["Debug", "Release", "FastDebug"];

    /// <summary>
    /// Tries to resolve MOBApi.dll copied next to the WinUI app output (see MOBAflow CopyMOBApiToOutput target).
    /// </summary>
    public static bool TryResolveAdjacentToApp(string appBaseDirectory, out string dllPath, out string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(appBaseDirectory);

        dllPath = System.IO.Path.Combine(appBaseDirectory, ProjectFolderName, AssemblyFileName);
        if (!File.Exists(dllPath))
        {
            workingDirectory = string.Empty;
            dllPath = string.Empty;
            return false;
        }

        workingDirectory = System.IO.Path.GetDirectoryName(dllPath) ?? appBaseDirectory;
        return true;
    }

    /// <summary>
    /// Tries to resolve a built MOBApi.dll under <c>MOBApi/bin/{Configuration}/{targetFramework}</c> in the repository.
    /// </summary>
    public static bool TryResolveBuiltOutput(
        string repositoryRoot,
        IEnumerable<string> configurations,
        string targetFramework,
        out string dllPath,
        out string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        ArgumentException.ThrowIfNullOrEmpty(targetFramework);

        foreach (var configuration in configurations)
        {
            if (string.IsNullOrWhiteSpace(configuration))
                continue;

            var outputDir = System.IO.Path.Combine(repositoryRoot, ProjectFolderName, "bin", configuration, targetFramework);
            var candidate = System.IO.Path.Combine(outputDir, AssemblyFileName);
            if (!File.Exists(candidate))
                continue;

            dllPath = candidate;
            workingDirectory = outputDir;
            return true;
        }

        dllPath = string.Empty;
        workingDirectory = string.Empty;
        return false;
    }

    /// <summary>
    /// Tries to resolve the MOBApi project file for <c>dotnet run --project</c> fallback when no build output exists yet.
    /// </summary>
    public static bool TryResolveProjectFile(string repositoryRoot, out string projectPath, out string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        projectPath = System.IO.Path.Combine(repositoryRoot, ProjectFolderName, ProjectFileName);
        if (!File.Exists(projectPath))
        {
            projectPath = string.Empty;
            workingDirectory = string.Empty;
            return false;
        }

        workingDirectory = repositoryRoot;
        return true;
    }
}