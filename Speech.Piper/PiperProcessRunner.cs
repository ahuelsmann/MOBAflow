// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Piper;

using System.Diagnostics;

/// <summary>
/// Executes Piper synthesis requests.
/// </summary>
public interface IPiperProcessRunner
{
    /// <summary>
    /// Runs Piper and returns process output.
    /// </summary>
    Task<PiperProcessResult> SynthesizeAsync(
        PiperSynthesisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether the executable starts successfully without synthesis.</summary>
    Task<PiperProcessResult> ProbeAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>
/// Piper process input.
/// </summary>
public sealed record PiperSynthesisRequest(
    string ExecutablePath,
    string ModelPath,
    string? ConfigPath,
    string OutputPath,
    string Text,
    double LengthScale,
    double SentenceSilenceSeconds,
    TimeSpan Timeout);

/// <summary>
/// Piper process output.
/// </summary>
public sealed record PiperProcessResult(int ExitCode, string StandardOutput, string ErrorOutput)
{
    /// <summary>
    /// Gets whether Piper completed successfully.
    /// </summary>
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Default Piper process runner.
/// </summary>
public sealed class PiperProcessRunner(TimeProvider? timeProvider = null) : IPiperProcessRunner
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Piper CLI option for a pause between sentences.
    /// </summary>
    public const string SentenceSilenceOption = "--sentence-silence";

    /// <inheritdoc />
    public async Task<PiperProcessResult> SynthesizeAsync(
        PiperSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.StartInfo.ArgumentList.Add("--model");
        process.StartInfo.ArgumentList.Add(request.ModelPath);
        process.StartInfo.ArgumentList.Add("--output_file");
        process.StartInfo.ArgumentList.Add(request.OutputPath);
        process.StartInfo.ArgumentList.Add("--length_scale");
        process.StartInfo.ArgumentList.Add(request.LengthScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));

        if (request.SentenceSilenceSeconds > 0)
        {
            process.StartInfo.ArgumentList.Add(SentenceSilenceOption);
            process.StartInfo.ArgumentList.Add(request.SentenceSilenceSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(request.ConfigPath))
        {
            process.StartInfo.ArgumentList.Add("--config");
            process.StartInfo.ArgumentList.Add(request.ConfigPath);
        }

        return await RunAsync(process, request.Text, request.Timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PiperProcessResult> ProbeAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("--help");
        return await RunAsync(process, null, timeout, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PiperProcessResult> RunAsync(Process process, string? text, TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The timeout covers the whole run, including writing the text: a Piper process that stops reading stdin
        // would otherwise block the write forever. Timeout and caller cancellation both kill the process tree.
        using var timeoutSource = new CancellationTokenSource(timeout, _timeProvider);
        using var runSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        process.Start();
        using var killRegistration = runSource.Token.Register(
            static state => TryKill((Process)state!),
            process);

        try
        {
            if (text is not null)
            {
                await process.StandardInput.WriteLineAsync(text.AsMemory(), runSource.Token).ConfigureAwait(false);
            }
            process.StandardInput.Close();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(runSource.Token);
            var errorOutputTask = process.StandardError.ReadToEndAsync(runSource.Token);
            await process.WaitForExitAsync(runSource.Token).ConfigureAwait(false);

            return new PiperProcessResult(
                process.ExitCode,
                await standardOutputTask.ConfigureAwait(false),
                await errorOutputTask.ConfigureAwait(false));
        }
        catch (Exception ex) when ((ex is OperationCanceledException or IOException) && !cancellationToken.IsCancellationRequested
                                   && timeoutSource.IsCancellationRequested)
        {
            // A killed process ends a blocked write with a broken pipe; both mean the run timed out.
            return new PiperProcessResult(-1, string.Empty, $"Piper timed out after {timeout.TotalSeconds:0} seconds.");
        }
        catch (IOException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Piper was cancelled.", ex, cancellationToken);
        }
    }
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // Best-effort process cleanup after timeout.
        }
    }
}

