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
            Directory.Delete(_root, recursive: true);
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

    private void WriteBuiltApp(string assetName, string marker)
    {
        var web = Path.Combine(_host, "wwwroot-build");
        Directory.CreateDirectory(Path.Combine(web, "assets"));
        File.WriteAllText(Path.Combine(web, "assets", assetName), "export default 1;");
        File.WriteAllText(Path.Combine(web, "index.html"), "<!doctype html><title>" + marker + "</title>");
    }

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
