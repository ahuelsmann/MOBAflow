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

    /// <summary>Runs the script with <c>-OutputPath</c> and fails with the script output when it does not succeed.</summary>
    internal static async Task RunAsync(string outputPath)
    {
        var result = await RunProcessAsync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-File", ScriptPath, "-OutputPath", outputPath],
            BuildTimeout).ConfigureAwait(false);
        Assert.That(result.ExitCode, Is.Zero, "build.ps1 failed:" + Environment.NewLine + result.Output);
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(
        string fileName,
        string[] arguments,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryRoot,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
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
                return (-2, "Timed out after " + timeout + "." + Environment.NewLine + output);
            }

            // WaitForExitAsync also waits until the redirected output streams are drained.
            lock (gate)
            {
                return (process.ExitCode, output.ToString());
            }
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
