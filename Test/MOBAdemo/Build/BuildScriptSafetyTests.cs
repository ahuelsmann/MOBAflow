// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

using System.Security.Cryptography;

/// <summary>
/// Safety checks of <c>MOBAdemo/build.ps1</c> (issue #430 slice 3): the script never deletes a repository folder
/// through a link, and a failed run leaves the previous output intact.
/// </summary>
[TestFixture]
internal sealed class BuildScriptSafetyTests
{
    private static readonly string[] OnlyOutputFolder = ["web"];

    private string _root = string.Empty;

    [SetUp]
    public void CreateRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "mobademo-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void CleanUp()
    {
        if (Directory.Exists(_root))
        {
            // A link is removed without following it, so the target survives until the test deletes it.
            foreach (var link in Directory.EnumerateDirectories(_root)
                         .Where(path => new DirectoryInfo(path).LinkTarget != null))
            {
                Directory.Delete(link);
            }

            Directory.Delete(_root, recursive: true);
        }
    }

    [TestCase("")]
    [TestCase("child")]
    public async Task Output_ThroughLinkIntoRepository_IsRefusedAndTargetSurvives(string subFolder)
    {
        // The victim is a throw-away folder inside the repository, so a missing guard cannot destroy real files.
        var victim = Path.Combine(
            BuildScript.RepositoryRoot, "MOBAdemo", "Host", "link-guard-victim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(victim);
        var sentinel = Path.Combine(victim, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "keep").ConfigureAwait(false);
        try
        {
            var link = Path.Combine(_root, "link");
            await BuildScript.CreateDirectoryLinkOrIgnoreAsync(link, victim).ConfigureAwait(false);
            var output = subFolder.Length == 0 ? link : Path.Combine(link, subFolder);

            var result = await BuildScript.TryRunAsync(output).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
                Assert.That(result.Output, Does.Contain("Refusing"));
                Assert.That(File.Exists(sentinel), Is.True, "The script deleted a repository folder through a link.");
            }
        }
        finally
        {
            Directory.Delete(victim, recursive: true);
        }
    }

    [TestCase(".")]
    [TestCase("MOBAdemo")]
    [TestCase("MOBAdemo/app")]
    [TestCase("MOBAdemo/app/src")]
    [TestCase("MOBAdemo/Host")]
    [TestCase("MOBAdemo/Host/wwwroot")]
    [TestCase("MOBAdemo/Host/wwwroot-build/../wwwroot")]
    public async Task Output_InsideRepositoryOtherThanDefault_IsRefusedAndRepositoryFilesSurvive(string outputPath)
    {
        // The script runs with the repository root as working directory, so a relative path points into the repository.
        var placeholder = Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "Host", "wwwroot", "index.html");
        var source = Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "app", "src", "main.tsx");
        var before = (await File.ReadAllTextAsync(placeholder).ConfigureAwait(false), await File.ReadAllTextAsync(source).ConfigureAwait(false));

        var result = await BuildScript.TryRunAsync(outputPath).ConfigureAwait(false);
        var after = (await File.ReadAllTextAsync(placeholder).ConfigureAwait(false), await File.ReadAllTextAsync(source).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("Refusing"));
            Assert.That(after, Is.EqualTo(before));
        }
    }

    [Test]
    public async Task FailureBeforePublish_KeepsPreviousOutputIntactAndLeavesNoTemporaryFolder()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var parent = Path.Combine(_root, "parent");
        var output = Path.Combine(parent, "web");
        Directory.CreateDirectory(Path.Combine(output, "assets"));
        await File.WriteAllTextAsync(Path.Combine(output, "index.html"), "previous").ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(output, "assets", "previous-abc123.js"), "previous asset")
            .ConfigureAwait(false);
        var before = Snapshot(output);

        var result = await BuildScript.TryRunAsync(output, "-FailBeforePublish").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("Simulated failure"));
            Assert.That(Snapshot(output), Is.EqualTo(before), "The previous output must survive a failed run.");
            Assert.That(
                Directory.EnumerateFileSystemEntries(parent).Select(Path.GetFileName),
                Is.EqualTo(OnlyOutputFolder),
                "No staging or backup folder may stay behind.");
        }
    }

    private static List<string> Snapshot(string folder) => Directory
        .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(folder, path) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))))
        .Order(StringComparer.Ordinal)
        .ToList();
}
