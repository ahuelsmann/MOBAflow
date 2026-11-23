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
            DeleteReparsePoints(_root);
            Directory.Delete(_root, recursive: true);
        }
    }

    private static void DeleteReparsePoints(string directory)
    {
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (new DirectoryInfo(child).LinkTarget != null)
            {
                Directory.Delete(child);
            }
            else
            {
                DeleteReparsePoints(child);
            }
        }
    }

    [TestCase("")]
    [TestCase("child")]
    public async Task Output_ThroughLinkIntoRepository_IsRefusedAndTargetSurvives(string subFolder)
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);

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

    [TestCase("")]
    [TestCase("child")]
    public async Task Output_ThroughExternalLinkedParent_IsRefusedAndTargetSurvives(string subFolder)
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var target = Path.Combine(_root, "external-target");
        Directory.CreateDirectory(target);
        var sentinel = Path.Combine(target, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "keep-external").ConfigureAwait(false);
        var link = Path.Combine(_root, "external-link");
        await BuildScript.CreateDirectoryLinkOrIgnoreAsync(link, target).ConfigureAwait(false);
        var output = subFolder.Length == 0 ? link : Path.Combine(link, subFolder);

        var result = await BuildScript.TryRunAsync(output).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("Refusing"));
            Assert.That(File.Exists(sentinel), Is.True);
            Assert.That(await File.ReadAllTextAsync(sentinel).ConfigureAwait(false), Is.EqualTo("keep-external"));
        }
    }

    [Test]
    public async Task Output_LinkedDefaultWwwrootBuild_IsRefusedAndTargetSurvives()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var fakeRepo = Path.Combine(_root, "repo-default-link");
        var demoDir = Path.Combine(fakeRepo, "MOBAdemo");
        var hostDir = Path.Combine(demoDir, "Host");
        var wwwrootDir = Path.Combine(hostDir, "wwwroot");
        Directory.CreateDirectory(wwwrootDir);
        Directory.CreateDirectory(Path.Combine(demoDir, "app"));
        var copiedScript = Path.Combine(demoDir, "build.ps1");
        File.Copy(BuildScript.ScriptPath, copiedScript);

        var target = Path.Combine(_root, "default-link-target");
        Directory.CreateDirectory(target);
        var sentinel = Path.Combine(target, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "keep-default-target").ConfigureAwait(false);

        var defaultOutput = Path.Combine(hostDir, "wwwroot-build");
        await BuildScript.CreateDirectoryLinkOrIgnoreAsync(defaultOutput, target).ConfigureAwait(false);
        try
        {
            var result = await BuildScript.RunProcessAsync(
                "pwsh",
                ["-NoProfile", "-NonInteractive", "-File", copiedScript],
                TimeSpan.FromMinutes(1),
                fakeRepo).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
                Assert.That(result.Output, Does.Contain("Refusing"));
                Assert.That(File.Exists(sentinel), Is.True);
                Assert.That(await File.ReadAllTextAsync(sentinel).ConfigureAwait(false), Is.EqualTo("keep-default-target"));
            }
        }
        finally
        {
            if (new DirectoryInfo(defaultOutput).LinkTarget != null)
            {
                Directory.Delete(defaultOutput);
            }
        }
    }

    [Test]
    public async Task Output_LinkedParentOfDefaultWwwrootBuild_IsRefusedAndTargetSurvives()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var fakeRepo = Path.Combine(_root, "repo-parent-link");
        var demoDir = Path.Combine(fakeRepo, "MOBAdemo");
        Directory.CreateDirectory(Path.Combine(demoDir, "app"));
        var copiedScript = Path.Combine(demoDir, "build.ps1");
        File.Copy(BuildScript.ScriptPath, copiedScript);

        var target = Path.Combine(_root, "host-link-target");
        var targetBuildDir = Path.Combine(target, "wwwroot-build");
        Directory.CreateDirectory(targetBuildDir);
        var sentinel = Path.Combine(targetBuildDir, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "keep-parent-target").ConfigureAwait(false);

        var hostLink = Path.Combine(demoDir, "Host");
        await BuildScript.CreateDirectoryLinkOrIgnoreAsync(hostLink, target).ConfigureAwait(false);
        try
        {
            var result = await BuildScript.RunProcessAsync(
                "pwsh",
                ["-NoProfile", "-NonInteractive", "-File", copiedScript],
                TimeSpan.FromMinutes(1),
                fakeRepo).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
                Assert.That(result.Output, Does.Contain("Refusing"));
                Assert.That(File.Exists(sentinel), Is.True);
                Assert.That(await File.ReadAllTextAsync(sentinel).ConfigureAwait(false), Is.EqualTo("keep-parent-target"));
            }
        }
        finally
        {
            if (new DirectoryInfo(hostLink).LinkTarget != null)
            {
                Directory.Delete(hostLink);
            }
        }
    }

    [Test]
    public async Task Output_WhenDefaultWwwrootBuildLinksToWwwroot_RefusesWwwroot()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var fakeRepo = Path.Combine(_root, "repo-exempt-link");
        var demoDir = Path.Combine(fakeRepo, "MOBAdemo");
        var hostDir = Path.Combine(demoDir, "Host");
        var wwwrootDir = Path.Combine(hostDir, "wwwroot");
        Directory.CreateDirectory(wwwrootDir);
        Directory.CreateDirectory(Path.Combine(demoDir, "app"));
        var copiedScript = Path.Combine(demoDir, "build.ps1");
        File.Copy(BuildScript.ScriptPath, copiedScript);

        var placeholder = Path.Combine(wwwrootDir, "index.html");
        await File.WriteAllTextAsync(placeholder, "keep-placeholder").ConfigureAwait(false);

        var defaultOutput = Path.Combine(hostDir, "wwwroot-build");
        await BuildScript.CreateDirectoryLinkOrIgnoreAsync(defaultOutput, wwwrootDir).ConfigureAwait(false);
        try
        {
            var result = await BuildScript.RunProcessAsync(
                "pwsh",
                ["-NoProfile", "-NonInteractive", "-File", copiedScript, "-OutputPath", "MOBAdemo/Host/wwwroot"],
                TimeSpan.FromMinutes(1),
                fakeRepo).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
                Assert.That(result.Output, Does.Contain("Refusing"));
                Assert.That(File.Exists(placeholder), Is.True);
                Assert.That(await File.ReadAllTextAsync(placeholder).ConfigureAwait(false), Is.EqualTo("keep-placeholder"));
            }
        }
        finally
        {
            if (new DirectoryInfo(defaultOutput).LinkTarget != null)
            {
                Directory.Delete(defaultOutput);
            }
        }
    }

    [Test]
    public async Task Output_NonEmptyExternalDirectoryWithoutMarker_IsRefusedAndSentinelSurvives()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var output = Path.Combine(_root, "external-non-empty");
        Directory.CreateDirectory(output);
        var sentinel = Path.Combine(output, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "do-not-delete").ConfigureAwait(false);

        var result = await BuildScript.TryRunAsync(output).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("Refusing"));
            Assert.That(File.Exists(sentinel), Is.True);
            Assert.That(await File.ReadAllTextAsync(sentinel).ConfigureAwait(false), Is.EqualTo("do-not-delete"));
        }
    }

    [Test]
    public async Task Output_ExistingFile_IsRefusedAndFileSurvives()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        var output = Path.Combine(_root, "existing-file.txt");
        await File.WriteAllTextAsync(output, "keep-file").ConfigureAwait(false);

        var result = await BuildScript.TryRunAsync(output).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("Refusing"));
            Assert.That(File.Exists(output), Is.True);
            Assert.That(await File.ReadAllTextAsync(output).ConfigureAwait(false), Is.EqualTo("keep-file"));
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
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);

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
        await File.WriteAllTextAsync(Path.Combine(output, ".mobademo-build-owned"), "MOBAdemo build output\n")
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
