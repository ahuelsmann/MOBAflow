// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

/// <summary>The process helper of the build tests must not deadlock or lose output on large output.</summary>
[TestFixture]
internal sealed class BuildScriptProcessTests
{
    [Test]
    public async Task RunProcess_WithLargeOutputOnBothStreams_KeepsEveryLine()
    {
        const string script =
            "1..100000 | ForEach-Object { 'out-line ' + $_ }; "
            + "1..100000 | ForEach-Object { [Console]::Error.WriteLine('err-line ' + $_) }; "
            + "'out-last'; [Console]::Error.WriteLine('err-last')";
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);

        var result = await BuildScript.RunProcessAsync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-Command", script],
            TimeSpan.FromMinutes(2)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, result.Output.Length > 2000 ? result.Output[..2000] : result.Output);
            Assert.That(result.Output, Does.Contain("out-line 100000"));
            Assert.That(result.Output, Does.Contain("err-line 100000"));
            Assert.That(result.Output, Does.Contain("out-last"));
            Assert.That(result.Output, Does.Contain("err-last"));
        }
    }
}
