// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

/// <summary>
/// Tests for MOBApi path resolution used by WinUI process launch and MSBuild copy targets.
/// </summary>
[TestFixture]
internal sealed class MobaApiPathResolverTests
{
    [Test]
    public void TryResolveAdjacentToApp_ReturnsTrue_WhenDllExistsNextToApp()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var mobApiDir = Path.Combine(tempRoot, MobaApiPathResolver.ProjectFolderName);
        Directory.CreateDirectory(mobApiDir);
        var dllPath = Path.Combine(mobApiDir, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(dllPath, string.Empty);

        try
        {
            var resolved = MobaApiPathResolver.TryResolveAdjacentToApp(tempRoot, out var resolvedDll, out var workingDir);

            Assert.That(resolved, Is.True);
            Assert.That(resolvedDll, Is.EqualTo(dllPath));
            Assert.That(workingDir, Is.EqualTo(mobApiDir));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Test]
    public void TryResolveBuiltOutput_PrefersFirstMatchingConfiguration()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var releaseDir = Path.Combine(
            tempRoot,
            MobaApiPathResolver.ProjectFolderName,
            "bin",
            "Release",
            MobaApiPathResolver.DefaultTargetFramework);
        Directory.CreateDirectory(releaseDir);
        var dllPath = Path.Combine(releaseDir, MobaApiPathResolver.AssemblyFileName);
        File.WriteAllText(dllPath, string.Empty);

        try
        {
            var resolved = MobaApiPathResolver.TryResolveBuiltOutput(
                tempRoot,
                MobaApiPathResolver.BuildConfigurations,
                MobaApiPathResolver.DefaultTargetFramework,
                out var resolvedDll,
                out var workingDir);

            Assert.That(resolved, Is.True);
            Assert.That(resolvedDll, Is.EqualTo(dllPath));
            Assert.That(workingDir, Is.EqualTo(releaseDir));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Test]
    public void TryResolveProjectFile_ReturnsCsproj_WhenPresent()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var projectDir = Path.Combine(tempRoot, MobaApiPathResolver.ProjectFolderName);
        Directory.CreateDirectory(projectDir);
        var projectPath = Path.Combine(projectDir, MobaApiPathResolver.ProjectFileName);
        File.WriteAllText(projectPath, string.Empty);

        try
        {
            var resolved = MobaApiPathResolver.TryResolveProjectFile(tempRoot, out var resolvedProject, out var workingDir);

            Assert.That(resolved, Is.True);
            Assert.That(resolvedProject, Is.EqualTo(projectPath));
            Assert.That(workingDir, Is.EqualTo(tempRoot));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}