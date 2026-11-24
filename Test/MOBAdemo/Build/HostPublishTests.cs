// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

/// <summary>
/// Publishing the demo host into a reused folder (issue #430 slice 3): the web root of the folder is always exactly
/// the current one, either the built app of <c>wwwroot-build</c> or the committed placeholder, never a mix.
/// Each test publishes a temporary copy of the host project, so the repository folders stay untouched.
/// </summary>
[TestFixture]
internal sealed class HostPublishTests
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromMinutes(5);

    private string _root = string.Empty;
    private string _host = string.Empty;
    private string _publish = string.Empty;

    [SetUp]
    public void CopyHost()
    {
        _root = Path.Combine(Path.GetTempPath(), "mobademo-publish-" + Guid.NewGuid().ToString("N"));
        _host = Path.Combine(_root, "Host");
        _publish = Path.Combine(_root, "out");
        BuildScript.CopyDirectory(
            Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "Host"),
            _host,
            relative => relative.Split(Path.DirectorySeparatorChar)[0] is "bin" or "obj" or "wwwroot-build");
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

    [Test]
    public async Task Publish_AfterBuiltApp_WithoutIt_LeavesOnlyThePlaceholder()
    {
        WriteBuiltApp("old-111.js", "built one");
        await PublishAsync().ConfigureAwait(false);
        Assert.That(File.Exists(Path.Combine(_publish, "wwwroot", "assets", "old-111.js")), Is.True, "Precondition.");

        Directory.Delete(Path.Combine(_host, "wwwroot-build"), recursive: true);
        await PublishAsync().ConfigureAwait(false);

        var files = Directory.EnumerateFiles(Path.Combine(_publish, "wwwroot"), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Path.Combine(_publish, "wwwroot"), path))
            .ToList();
        var index = await File.ReadAllTextAsync(Path.Combine(_publish, "wwwroot", "index.html")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(files, Does.Contain("index.html"));
            Assert.That(
                files.Where(name => !name.StartsWith("index.html", StringComparison.Ordinal)),
                Is.Empty,
                "Only the placeholder and its compressed siblings may remain.");
            Assert.That(index, Does.Contain("MOBAdemo is being built"));
        }
    }

    [Test]
    public async Task Publish_TwiceWithDifferentBuilds_KeepsNoStaleAsset()
    {
        WriteBuiltApp("old-111.js", "built one");
        await PublishAsync().ConfigureAwait(false);

        Directory.Delete(Path.Combine(_host, "wwwroot-build"), recursive: true);
        WriteBuiltApp("new-222.js", "built two");
        await PublishAsync().ConfigureAwait(false);

        var assets = Directory.EnumerateFiles(Path.Combine(_publish, "wwwroot", "assets"))
            .Select(Path.GetFileName)
            .ToList();
        var index = await File.ReadAllTextAsync(Path.Combine(_publish, "wwwroot", "index.html")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(assets, Does.Contain("new-222.js"));
            Assert.That(
                assets.Where(name => !name!.StartsWith("new-222.js", StringComparison.Ordinal)),
                Is.Empty,
                "No file of the first build may remain.");
            Assert.That(index, Does.Contain("built two"));
        }
    }

    [Test]
    public async Task Build_AfterBuiltApp_WithoutIt_LeavesOnlyThePlaceholderInTheOutputFolder()
    {
        WriteBuiltApp("old-111.js", "built one");
        var output = Path.Combine(_root, "bin-out");
        await DotNetAsync("build", output).ConfigureAwait(false);
        Assert.That(File.Exists(Path.Combine(output, "wwwroot", "assets", "old-111.js")), Is.True, "Precondition.");

        Directory.Delete(Path.Combine(_host, "wwwroot-build"), recursive: true);
        await DotNetAsync("build", output).ConfigureAwait(false);

        var files = Directory.EnumerateFiles(Path.Combine(output, "wwwroot"), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Path.Combine(output, "wwwroot"), path))
            .ToList();
        var index = await File.ReadAllTextAsync(Path.Combine(output, "wwwroot", "index.html")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(files, Does.Contain("index.html"));
            Assert.That(
                files.Where(name => !name.StartsWith("index.html", StringComparison.Ordinal)),
                Is.Empty,
                "Only the placeholder and its compressed siblings may remain.");
            Assert.That(index, Does.Contain("MOBAdemo is being built"));
        }
    }

    [Test]
    public async Task PublishAndBuild_ExcludeOwnershipMarkerFromWebRoot()
    {
        WriteBuiltApp("app-111.js", "built app");
        var buildOutput = Path.Combine(_root, "bin-marker-out");

        await PublishAsync().ConfigureAwait(false);
        await DotNetAsync("build", buildOutput).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(Path.Combine(_publish, "wwwroot", "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(_publish, "wwwroot", ".mobademo-build-owned")), Is.False);
            Assert.That(File.Exists(Path.Combine(buildOutput, "wwwroot", "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(buildOutput, "wwwroot", ".mobademo-build-owned")), Is.False);
        }
    }

    [Test]
    public async Task BuildAndPublish_WhenOutDirOrPublishDirOverlapsHostDirectory_FailsAndPreservesTrackedWebRoot()
    {
        var cases = new OverlapCase[]
        {
            new("build", "-p:OutDir=.", false, false, false),
            new("build", "-p:OutDir=", true, false, false),
            new("build", "-o", false, false, false),
            new("build", "-p:OutDir=", false, false, true),
            new("publish", "-p:PublishDir=.", false, false, false),
            new("publish", "-p:PublishDir=", true, true, false),
            new("publish", "-o", false, false, false),
        };

        for (var i = 0; i < cases.Length; i++)
        {
            var overlapCase = cases[i];
            var caseHost = Path.Combine(_root, "overlap-" + i);
            BuildScript.CopyDirectory(_host, caseHost);
            WriteBuiltAppTo(caseHost, "built-999.js", "built-should-not-overwrite");
            var destination = overlapCase.SubWwwroot
                ? Path.Combine(caseHost, "wwwroot", "sub") + Path.DirectorySeparatorChar
                : caseHost;
            await AssertOverlapRejectedAsync(caseHost, destination, overlapCase).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task BuildAndPublish_WhenOutDirOrPublishDirOverlapsHostViaLink_FailsAndPreservesTrackedWebRoot()
    {
        var cases = new OverlapCase[]
        {
            new("build", "-p:OutDir=", true, false, false),
            new("build", "-o", false, false, false),
            new("publish", "-p:PublishDir=", true, false, false),
            new("publish", "-p:PublishDir=", true, true, false),
            new("publish", "-o", false, false, false),
        };

        for (var i = 0; i < cases.Length; i++)
        {
            var overlapCase = cases[i];
            var caseHost = Path.Combine(_root, "link-host-" + i);
            BuildScript.CopyDirectory(_host, caseHost);
            WriteBuiltAppTo(caseHost, "built-999.js", "built-should-not-overwrite");
            var alias = Path.Combine(_root, "host-alias-" + i);
            await BuildScript.CreateDirectoryLinkOrIgnoreAsync(alias, caseHost).ConfigureAwait(false);
            try
            {
                await AssertOverlapRejectedAsync(caseHost, alias, overlapCase).ConfigureAwait(false);
            }
            finally
            {
                if (new DirectoryInfo(alias).LinkTarget != null)
                {
                    Directory.Delete(alias);
                }
            }
        }
    }

    private static async Task AssertOverlapRejectedAsync(
        string caseHost,
        string destination,
        OverlapCase overlapCase)
    {
        var projectFile = Path.Combine(caseHost, "MOBAdemo.csproj");
        var indexPath = Path.Combine(caseHost, "wwwroot", "index.html");
        var expectedIndex = await File.ReadAllTextAsync(indexPath).ConfigureAwait(false);

        if (overlapCase.BuildFirst)
        {
            var buildResult = await BuildScript.RunProcessAsync(
                "dotnet",
                ["build", projectFile, "-c", "Release", "-nologo", "-v", "q"],
                PublishTimeout,
                caseHost).ConfigureAwait(false);
            Assert.That(
                buildResult.ExitCode,
                Is.Zero,
                "Pre-build failed:" + Environment.NewLine + buildResult.Output);
        }

        string[] arguments;
        if (overlapCase.Option == "-o")
        {
            arguments = [overlapCase.Command, projectFile, "-c", "Release", "-o", destination, "-nologo", "-v", "q"];
        }
        else if (overlapCase.UseSlashSuffix || overlapCase.SubWwwroot)
        {
            var targetValue = overlapCase.SubWwwroot
                ? destination
                : destination + Path.DirectorySeparatorChar;
            arguments =
            [
                overlapCase.Command,
                projectFile,
                "-c",
                "Release",
                .. overlapCase.BuildFirst ? new[] { "--no-build" } : Array.Empty<string>(),
                overlapCase.Option + targetValue,
                "-nologo",
                "-v",
                "q",
            ];
        }
        else
        {
            arguments = [overlapCase.Command, projectFile, "-c", "Release", overlapCase.Option, "-nologo", "-v", "q"];
        }

        var result = await BuildScript.RunProcessAsync("dotnet", arguments, PublishTimeout, caseHost)
            .ConfigureAwait(false);
        var label = string.Join(' ', arguments);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, "Expected overlap rejection for: " + label);
            Assert.That(result.Output, Does.Contain("Refusing"), label);
            Assert.That(File.Exists(indexPath), Is.True, "Tracked wwwroot/index.html was deleted by: " + label);
            Assert.That(
                await File.ReadAllTextAsync(indexPath).ConfigureAwait(false),
                Is.EqualTo(expectedIndex),
                "Tracked wwwroot/index.html was overwritten by: " + label);
            Assert.That(
                Directory.Exists(Path.Combine(caseHost, "wwwroot", "assets")),
                Is.False,
                "Staged assets were copied into tracked wwwroot by: " + label);
        }
    }

    private void WriteBuiltApp(string assetName, string marker) => WriteBuiltAppTo(_host, assetName, marker);

    private static void WriteBuiltAppTo(string hostRoot, string assetName, string marker)
    {
        var web = Path.Combine(hostRoot, "wwwroot-build");
        Directory.CreateDirectory(Path.Combine(web, "assets"));
        File.WriteAllText(Path.Combine(web, "assets", assetName), "export default 1;");
        File.WriteAllText(Path.Combine(web, "index.html"), "<!doctype html><title>" + marker + "</title>");
        File.WriteAllText(Path.Combine(web, ".mobademo-build-owned"), "MOBAdemo build output\n");
    }

    private sealed record OverlapCase(
        string Command,
        string Option,
        bool UseSlashSuffix,
        bool BuildFirst,
        bool SubWwwroot);

    private Task PublishAsync() => DotNetAsync("publish", _publish);

    private async Task DotNetAsync(string command, string outputFolder)
    {
        var result = await BuildScript.RunProcessAsync(
            "dotnet",
            [command, Path.Combine(_host, "MOBAdemo.csproj"), "-c", "Release", "-o", outputFolder, "-nologo", "-v", "q"],
            PublishTimeout,
            _host).ConfigureAwait(false);
        Assert.That(result.ExitCode, Is.Zero, "dotnet " + command + " failed:" + Environment.NewLine + result.Output);
    }
}
