// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Owns an isolated runtime copy of a built MOBApi output directory.
/// </summary>
public sealed class MobaApiRuntimeDeployment : IDisposable
{
    private int _disposed;

    private MobaApiRuntimeDeployment(string assemblyPath, string workingDirectory)
    {
        AssemblyPath = assemblyPath;
        WorkingDirectory = workingDirectory;
    }

    /// <summary>
    /// Gets the MOBApi assembly path inside the isolated runtime directory.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets the isolated runtime directory containing the complete MOBApi output.
    /// </summary>
    public string WorkingDirectory { get; }

    /// <summary>
    /// Copies a built MOBApi output directory to a unique child of <paramref name="runtimeRootDirectory"/>.
    /// </summary>
    /// <param name="sourceAssemblyPath">Path to the built MOBApi assembly.</param>
    /// <param name="runtimeRootDirectory">Root directory under which the isolated copy is created.</param>
    /// <returns>An owner for the isolated runtime copy.</returns>
    public static MobaApiRuntimeDeployment Create(string sourceAssemblyPath, string runtimeRootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAssemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeRootDirectory);

        var fullSourceAssemblyPath = System.IO.Path.GetFullPath(sourceAssemblyPath);
        if (!File.Exists(fullSourceAssemblyPath))
            throw new FileNotFoundException("The MOBApi assembly was not found.", fullSourceAssemblyPath);

        var sourceDirectory = System.IO.Path.GetDirectoryName(fullSourceAssemblyPath)
            ?? throw new InvalidOperationException("The MOBApi assembly path has no parent directory.");
        var deploymentDirectory = System.IO.Path.Combine(
            System.IO.Path.GetFullPath(runtimeRootDirectory),
            $"instance-{Guid.NewGuid():N}");

        Directory.CreateDirectory(deploymentDirectory);
        try
        {
            foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = System.IO.Path.GetRelativePath(sourceDirectory, sourceFile);
                var destinationFile = System.IO.Path.Combine(deploymentDirectory, relativePath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile);
            }

            var assemblyPath = System.IO.Path.Combine(
                deploymentDirectory,
                System.IO.Path.GetFileName(fullSourceAssemblyPath));
            return new MobaApiRuntimeDeployment(assemblyPath, deploymentDirectory);
        }
        catch
        {
            Directory.Delete(deploymentDirectory, recursive: true);
            throw;
        }
    }

    /// <summary>
    /// Deletes the isolated runtime copy after the MOBApi process has exited.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (Directory.Exists(WorkingDirectory))
            Directory.Delete(WorkingDirectory, recursive: true);
    }
}
