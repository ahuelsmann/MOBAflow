// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

using System.Diagnostics;
using System.Text;

/// <summary>Runs <c>MOBAdemo/build.ps1</c> the way a developer or CI does (issue #430 slice 3).</summary>
internal static class BuildScript
{
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Repository root: the folder that holds <c>Moba.slnx</c>.</summary>
    internal static string RepositoryRoot { get; } = FindRepositoryRoot();

    internal static string ScriptPath => Path.Combine(RepositoryRoot, "MOBAdemo", "build.ps1");

    /// <summary>Ignores the calling test with a clear reason when PowerShell 7, Node or npm is missing.</summary>
    internal static async Task RequireToolchainAsync()
    {
        var probe = await RunProcessAsync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-Command", "Get-Command node, npm -ErrorAction Stop | Out-Null"],
            TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        if (probe.ExitCode != 0)
        {
            Assert.Ignore("The MOBAdemo build script needs PowerShell 7 (pwsh), Node and npm on the PATH: "
                + probe.Output.Trim());
        }
    }

    /// <summary>
    /// Runs the script with <c>-OutputPath</c> (plus any <paramref name="extraArguments"/>) and fails with the
    /// script output when it does not succeed.
    /// </summary>
    internal static async Task RunAsync(string outputPath, params string[] extraArguments)
    {
        var result = await TryRunAsync(outputPath, extraArguments).ConfigureAwait(false);
        Assert.That(result.ExitCode, Is.Zero, "build.ps1 failed:" + Environment.NewLine + result.Output);
    }

    /// <summary>Runs the script like <see cref="RunAsync"/> but returns the exit code and output.</summary>
    internal static Task<(int ExitCode, string Output)> TryRunAsync(string outputPath, params string[] extraArguments) =>
        TryRunWithEnvironmentAsync(outputPath, environmentVariables: null, extraArguments);

    /// <summary>Runs the script like <see cref="TryRunAsync"/> with custom process environment variables.</summary>
    internal static Task<(int ExitCode, string Output)> TryRunWithEnvironmentAsync(
        string outputPath,
        IReadOnlyDictionary<string, string?>? environmentVariables,
        params string[] extraArguments) =>
        RunProcessAsync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-File", ScriptPath, "-OutputPath", outputPath, .. extraArguments],
            BuildTimeout,
            workingDirectory: null,
            environmentVariables);

    /// <summary>Starts a process, collects its output and kills it when <paramref name="timeout"/> elapses.</summary>
    internal static async Task<(int ExitCode, string Output)> RunProcessAsync(
        string fileName,
        string[] arguments,
        TimeSpan timeout,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? RepositoryRoot,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                if (value == null)
                {
                    startInfo.Environment.Remove(key);
                }
                else
                {
                    startInfo.Environment[key] = value;
                }
            }
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return (-1, "Cannot start " + fileName + ": " + exception.Message);
        }

        using (process)
        {
            if (process == null)
            {
                return (-1, "Cannot start " + fileName + ".");
            }

            process.StandardInput.Close();
            var output = new StringBuilder();
            var gate = new object();
            void Collect(object? sender, DataReceivedEventArgs line)
            {
                lock (gate)
                {
                    output.AppendLine(line.Data);
                }
            }

            process.OutputDataReceived += Collect;
            process.ErrorDataReceived += Collect;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cancellation = new CancellationTokenSource(timeout);
            try
            {
                await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                string partialOutput;
                lock (gate)
                {
                    partialOutput = output.ToString();
                }

                return (-2, "Timed out after " + timeout + "." + Environment.NewLine + partialOutput);
            }

            // With redirected streams the token-free wait returns only after the asynchronous output handlers saw
            // end of stream, so the tail of a large output is not lost.
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            lock (gate)
            {
                return (process.ExitCode, output.ToString());
            }
        }
    }

    /// <summary>Copies a folder tree; <paramref name="skip"/> may leave out entries by relative path.</summary>
    internal static void CopyDirectory(string source, string target, Func<string, bool>? skip = null)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            if (skip?.Invoke(relative) != true)
            {
                Directory.CreateDirectory(Path.Combine(target, relative));
            }
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (skip?.Invoke(relative) != true)
            {
                File.Copy(file, Path.Combine(target, relative), overwrite: true);
            }
        }
    }

    /// <summary>
    /// Creates a directory link (a junction on Windows, which needs no administrator rights; a symbolic link
    /// elsewhere). Ignores the calling test with a reason when the operating system refuses the link.
    /// </summary>
    internal static async Task CreateDirectoryLinkOrIgnoreAsync(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            var result = await RunProcessAsync("cmd", ["/c", "mklink", "/J", link, target], TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                Assert.Ignore("Windows refused to create a directory junction: " + result.Output.Trim());
            }

            return;
        }

        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Assert.Ignore("The operating system refused to create a symbolic link: " + exception.Message);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Moba.slnx) not found.");
    }
}
