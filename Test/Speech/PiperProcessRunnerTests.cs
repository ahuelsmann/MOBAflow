#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Moba.Speech.Piper;

/// <summary>
/// The Piper timeout covers the whole run, including writing the text, with a batch file standing in for Piper.
/// </summary>
[TestFixture]
[Platform("Win")]
internal sealed class PiperProcessRunnerTests
{
    // More text than the stdin pipe buffers, so the write blocks until the process reads it.
    private static readonly string LongText = new('a', 4 * 1024 * 1024);

    private readonly List<string> _scripts = [];

    [TearDown]
    public void DeleteScripts()
    {
        foreach (var script in _scripts)
            File.Delete(script);
        _scripts.Clear();
    }

    [Test]
    public async Task ProcessThatNeverReadsTheText_TimesOutWhileTheTextIsWritten()
    {
        // ping keeps the batch running for about 30 seconds without reading stdin.
        var executable = Script("@ping -n 30 127.0.0.1 >nul");
        var runner = new PiperProcessRunner();

        var result = await runner.SynthesizeAsync(Request(executable, LongText, TimeSpan.FromSeconds(1)))
            .WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorOutput, Is.EqualTo("Piper timed out after 1 seconds."));
        }
    }

    [Test]
    public async Task ProcessThatReadsTheText_ReturnsItsOutput()
    {
        var executable = Script("@more >nul", "@echo done");
        var runner = new PiperProcessRunner();

        var result = await runner.SynthesizeAsync(Request(executable, "Next stop Minden.", TimeSpan.FromSeconds(20)))
            .WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo("done"));
        }
    }

    [Test]
    public void CallerCancellation_WhileTheTextIsWritten_Throws()
    {
        var executable = Script("@ping -n 30 127.0.0.1 >nul");
        var runner = new PiperProcessRunner();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        Assert.That(
            async () => await runner.SynthesizeAsync(Request(executable, LongText, TimeSpan.FromSeconds(60)), cancellation.Token)
                .WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private string Script(params string[] lines)
    {
        var path = Path.Combine(Path.GetTempPath(), $"moba-piper-{Guid.NewGuid():N}.cmd");
        File.WriteAllLines(path, lines);
        _scripts.Add(path);
        return path;
    }

    private static PiperSynthesisRequest Request(string executable, string text, TimeSpan timeout) =>
        new(executable, "model.onnx", ConfigPath: null, "output.wav", text, LengthScale: 1, SentenceSilenceSeconds: 0, timeout);
}
#endif
